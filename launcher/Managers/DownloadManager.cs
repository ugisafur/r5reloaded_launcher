using Polly;
using Polly.Retry;
using System.IO;
using System.Net;
using static launcher.Utilities.Logger;
using System.Windows;
using System.Net.Http;
using static launcher.Global.References;
using launcher.CDN;
using ZstdSharp;
using launcher.Network;
using launcher.Game;

using launcher.Network;

using launcher.Global;
using launcher.Utilities;
using launcher.BranchUtils;

namespace launcher.Managers
{
    /// <summary>
    /// Manages file downloads within the launcher application, providing functionalities such as
    /// concurrent downloads, retry policies, speed throttling, and UI updates.
    /// </summary>
    public static class DownloadManager
    {
        public static long _downloadSpeedLimit = 0;
        public static SemaphoreSlim _downloadSemaphore;
        public static DownloadSpeedMonitor _speedMonitor;

        public static void CreateDownloadMontior()
        {
            if (_speedMonitor != null)
            {
                _speedMonitor.OnSpeedUpdated -= UpdateDownloadSpeedUI;
                _speedMonitor.Dispose();
                _speedMonitor = null;
            }

            _speedMonitor = new DownloadSpeedMonitor();
            _speedMonitor.OnSpeedUpdated += UpdateDownloadSpeedUI;
        }

        private static void UpdateDownloadSpeedUI(double speedBytesPerSecond)
        {
            // Convert bytes per second to a more readable format (e.g., KB/s or MB/s)
            string speedText;
            double speed = speedBytesPerSecond;

            if (speed >= 1024 * 1024)
            {
                speed /= 1024 * 1024;
                speedText = $"{speed:F2} MB/s";
            }
            else if (speed >= 1024)
            {
                speed /= 1024;
                speedText = $"{speed:F2} KB/s";
            }
            else
            {
                speedText = $"{speed} B/s";
            }

            // Update your UI element (e.g., a label)
            appDispatcher.Invoke(() =>
            {
                Speed_Label.Text = $"{speedText}";
                Downloads_Control.Speed_Label.Text = $"{speedText}";
            });
        }

        public static void ConfigureConcurrency()
        {
            if (AppState.IsInstalling)
                return;

            int maxConcurrentDownloads = (int)Ini.Get(Ini.Vars.Concurrent_Downloads);
            _downloadSemaphore?.Dispose();
            _downloadSemaphore = new SemaphoreSlim(maxConcurrentDownloads);
        }

        public static void ConfigureDownloadSpeed()
        {
            int speedLimitKb = (int)Ini.Get(Ini.Vars.Download_Speed_Limit);
            _downloadSpeedLimit = speedLimitKb > 0 ? speedLimitKb * 1024 : 0;
            GlobalBandwidthLimiter.Instance.UpdateLimit(_downloadSpeedLimit);
        }

        public static List<Task<string>> CreateDownloadTasks(GameFiles gameFiles, string branchDirectory)
        {
            var downloadTasks = new List<Task<string>>(gameFiles.files.Count);
            ConfigureProgress(gameFiles.files.Count);

            foreach (var file in gameFiles.files)
            {
                string fileUrl = $"{GetBranch.GameURL()}/{file.name}";
                string destinationPath = Path.Combine(branchDirectory, file.name);

                Directory.CreateDirectory(destinationPath);

                downloadTasks.Add(DownloadFile(fileUrl, destinationPath, file.name, file.checksum, checkForExistingFiles: true));
            }

            return downloadTasks;
        }

        public static List<Task<string>> CreateRepairTasks(string branchDirectory)
        {
            int badFilesCount = DataCollections.BadFiles.Count;
            ConfigureProgress(badFilesCount);

            var downloadTasks = new List<Task<string>>(badFilesCount);

            foreach (var file in DataCollections.BadFiles)
            {
                string fileUrl = $"{GetBranch.GameURL()}/{file}";
                string destinationPath = Path.Combine(branchDirectory, file);

                Directory.CreateDirectory(destinationPath);

                downloadTasks.Add(DownloadFile(fileUrl, destinationPath, file, checkForExistingFiles: false));
            }

            return downloadTasks;
        }

        private static AsyncRetryPolicy CreateRetryPolicy(string fileUrl, int maxRetryAttempts)
        {
            const double exponentialBackoffFactor = 2.0;

            return Policy.Handle<WebException>().Or<TimeoutException>().WaitAndRetryAsync(
                retryCount: maxRetryAttempts,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(exponentialBackoffFactor, retryAttempt)),
                onRetry: (exception, timeSpan, retryNumber, context) =>
                {
                    Log(
                        Logger.Type.Warning,
                        Source.DownloadManager,
                        $"Retry #{retryNumber} for '{fileUrl}' due to: {exception.Message}. " +
                        $"Waiting {timeSpan.TotalSeconds:F2} seconds before next attempt."
                    );
                }
            );
        }

        private static async Task<string> DownloadFile(string fileUrl, string destinationPath, string fileName, string checksum = "", bool checkForExistingFiles = false)
        {
            await _downloadSemaphore.WaitAsync();

            DownloadItem downloadItem = await AddDownloadItemAsync(fileName);

            try
            {
                //Check if the file already exists and has the correct checksum, if so skip the download and only decompress
                if (checkForExistingFiles && !string.IsNullOrWhiteSpace(checksum) && ShouldSkipDownload(destinationPath, checksum))
                {
                    await CreateRetryPolicy(destinationPath, 5).ExecuteAsync(async () => { await Decompress(destinationPath, destinationPath.Replace(".zst", ""), downloadItem); });
                    return destinationPath;
                }

                //Download the file
                await CreateRetryPolicy(destinationPath, 30).ExecuteAsync(async () => { await Download(fileUrl, destinationPath, downloadItem); });

                //Decompress the file
                await CreateRetryPolicy(destinationPath, 5).ExecuteAsync(async () => { await Decompress(destinationPath, destinationPath.Replace(".zst", ""), downloadItem); });

                return destinationPath;
            }
            catch (Exception ex)
            {
                LogError(Source.DownloadManager, $"All retries failed for {fileUrl}: {ex.Message}");
                AppState.BadFilesDetected = true;
                return string.Empty;
            }
            finally
            {
                UpdateProgress();

                await RemoveDownloadItemAsync(downloadItem);

                if (File.Exists(destinationPath))
                    File.Delete(destinationPath);

                _downloadSemaphore.Release();
            }
        }

        private static async Task Download(string fileUrl, string destinationPath, DownloadItem downloadItem)
        {
            using var response = await Networking.HttpClient.GetAsync(fileUrl, HttpCompletionOption.ResponseHeadersRead);

            if (response.StatusCode != HttpStatusCode.OK)
                throw new WebException($"Failed to download: {response.StatusCode}");

            long totalBytes = response.Content.Headers.ContentLength ?? -1;
            long downloadedBytes = 0;

            DateTime lastUpdate = DateTime.Now;
            DateTime timeoutLastUpdate = DateTime.Now;
            TimeSpan timeoutThreshold = TimeSpan.FromSeconds(30);

            // Create a throttled stream to limit the download speed
            using var responseStream = await response.Content.ReadAsStreamAsync();
            using var throttledStream = new ThrottledStream(responseStream, GlobalBandwidthLimiter.Instance);
            using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);

            byte[] buffer = new byte[4096];
            int bytesRead;

            while ((bytesRead = await throttledStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                // Write the downloaded data to the file
                await fileStream.WriteAsync(buffer, 0, bytesRead);
                downloadedBytes += bytesRead;

                // Update the global downloaded bytes counter
                DownloadSpeedTracker.AddDownloadedBytes(bytesRead);

                // Update the lastUpdate time if new data is downloaded
                if (bytesRead > 0)
                    timeoutLastUpdate = DateTime.Now;

                // Check for timeout
                if (DateTime.Now - timeoutLastUpdate > timeoutThreshold)
                    throw new TimeoutException($"Download stalled for {timeoutThreshold.TotalSeconds} seconds. Retrying...");

                // Update the UI every 200ms to avoid excessive updates
                if ((DateTime.Now - lastUpdate).TotalMilliseconds > 200)
                {
                    lastUpdate = DateTime.Now;
                    if (downloadItem != null && totalBytes > 0)
                    {
                        double totalSize = totalBytes >= 1024L * 1024 * 1024 ? totalBytes / (1024.0 * 1024 * 1024) : totalBytes / (1024.0 * 1024.0);
                        string totalText = totalBytes >= 1024L * 1024 * 1024 ? $"{totalSize:F2} GB" : $"{totalSize:F2} MB";

                        double downloadedSize = downloadedBytes >= 1024L * 1024 * 1024 ? downloadedBytes / (1024.0 * 1024 * 1024) : downloadedBytes / (1024.0 * 1024.0);
                        string downloadedText = downloadedBytes >= 1024L * 1024 * 1024 ? $"{downloadedSize:F2} GB" : $"{downloadedSize:F2} MB";

                        await appDispatcher.InvokeAsync(() =>
                        {
                            downloadItem.downloadFilePercent.Text = $"{downloadedText} / {totalText}";
                            downloadItem.downloadFileProgress.Value = (double)downloadedBytes / totalBytes * 100;
                        });
                    }
                }
            }

            await fileStream.FlushAsync();
        }

        public static async Task Decompress(string compressedFilePath, string decompressedFilePath, DownloadItem downloadItem)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(decompressedFilePath));

            long totalBytes = new FileInfo(compressedFilePath).Length;
            long processedBytes = 0;
            DateTime lastUpdate = DateTime.Now;

            using var input = File.OpenRead(compressedFilePath);
            using var output = File.OpenWrite(decompressedFilePath);
            using var decompressionStream = new DecompressionStream(input);

            byte[] buffer = new byte[8192];
            int bytesRead;
            while ((bytesRead = await decompressionStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await output.WriteAsync(buffer, 0, bytesRead);
                processedBytes += bytesRead;

                if ((DateTime.Now - lastUpdate).TotalMilliseconds > 200)
                {
                    lastUpdate = DateTime.Now;

                    double totalSize = totalBytes >= 1024L * 1024 * 1024 ? totalBytes / (1024.0 * 1024 * 1024) : totalBytes / (1024.0 * 1024.0);
                    string totalText = totalBytes >= 1024L * 1024 * 1024 ? $"{totalSize:F2} GB" : $"{totalSize:F2} MB";

                    double downloadedSize = processedBytes >= 1024L * 1024 * 1024 ? processedBytes / (1024.0 * 1024 * 1024) : processedBytes / (1024.0 * 1024.0);
                    string downloadedText = processedBytes >= 1024L * 1024 * 1024 ? $"{downloadedSize:F2} GB" : $"{downloadedSize:F2} MB";

                    await appDispatcher.InvokeAsync(() =>
                    {
                        downloadItem.downloadFilePercent.Text = $"decompressing...";
                        downloadItem.downloadFileProgress.Value = (double)processedBytes / totalBytes * 100;
                    });
                }
            }

            decompressionStream.Close();
            output.Close();
            input.Close();
        }

        private static async Task<DownloadItem> AddDownloadItemAsync(string fileName)
        {
            return await appDispatcher.InvokeAsync(() => Downloads_Control.AddDownloadItem(fileName));
        }

        private static async Task RemoveDownloadItemAsync(DownloadItem downloadItem)
        {
            if (downloadItem != null)
                await appDispatcher.InvokeAsync(() => Downloads_Control.RemoveDownloadItem(downloadItem));
        }

        public static void SetInstallState(bool installing, string buttonText = "PLAY")
        {
            LogInfo(Source.Launcher, $"Setting install state to: {installing}");

            appDispatcher.Invoke(() =>
            {
                AppState.IsInstalling = installing;
                AppState.BlockLanguageInstall = installing;

                Play_Button.Content = buttonText;
                Branch_Combobox.IsEnabled = !installing;
                Play_Button.IsEnabled = !installing;
                Status_Label.Text = "";
                Files_Label.Text = "";
                Speed_Label.Text = "";
                Downloads_Control.Speed_Label.Text = "";

                GameSettings_Control.RepairGame_Button.IsEnabled = !installing && GetBranch.Installed();
                GameSettings_Control.UninstallGame_Button.IsEnabled = !installing && GetBranch.Installed();
                GameSettings_Control.OpenDir_Button.IsEnabled = !installing && GetBranch.Installed();
                GameSettings_Control.AdvancedMenu_Button.IsEnabled = !installing && GetBranch.Installed();

                Settings_Control.gameInstalls.UpdateGameItems();
            });

            ShowProgressBar(installing);
        }

        public static void SetOptionalInstallState(bool installing)
        {
            LogInfo(Source.Launcher, $"Setting optional install state to: {installing}");

            appDispatcher.Invoke(() =>
            {
                AppState.IsInstalling = installing;
                Status_Label.Text = "";
                Files_Label.Text = "";
                Speed_Label.Text = "";
                Downloads_Control.Speed_Label.Text = "";

                GameSettings_Control.RepairGame_Button.IsEnabled = !installing && GetBranch.Installed();
                GameSettings_Control.UninstallGame_Button.IsEnabled = !installing && GetBranch.Installed();

                Settings_Control.gameInstalls.UpdateGameItems();
            });

            ShowProgressBar(installing);
        }

        public static void UpdateStatusLabel(string statusText, Source source)
        {
            LogInfo(source, $"Updating status label: {statusText}");
            appDispatcher.Invoke(() =>
            {
                Status_Label.Text = statusText;
            });
        }

        private static void ShowProgressBar(bool isVisible)
        {
            appDispatcher.Invoke(() =>
            {
                Progress_Bar.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
                Status_Label.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
                Files_Label.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
                Speed_Label.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
                Downloads_Control.Speed_Label.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
                ReadMore_Label.Visibility = isVisible ? Visibility.Hidden : Visibility.Visible;
            });
        }

        private static bool ShouldSkipDownload(string destinationPath, string expectedChecksum)
        {
            if (File.Exists(destinationPath))
            {
                string actualChecksum = FileManager.CalculateChecksum(destinationPath);
                if (string.Equals(actualChecksum, expectedChecksum, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static void ConfigureProgress(int totalFiles)
        {
            AppState.FilesLeft = totalFiles;

            appDispatcher.Invoke(() =>
            {
                Progress_Bar.Maximum = totalFiles;
                Progress_Bar.Value = 0;
                Files_Label.Text = $"{totalFiles} files left";
            });
        }

        private static void UpdateProgress()
        {
            appDispatcher.Invoke(() =>
            {
                Progress_Bar.Value++;
                Files_Label.Text = $"{--AppState.FilesLeft} files left";
            });
        }
    }
}