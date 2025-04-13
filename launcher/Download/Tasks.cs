using launcher.Game;
using launcher.Global;
using Polly.Retry;
using Polly;
using System.IO;
using System.Net;
using launcher.Network;
using static launcher.Global.Logger;
using System.Net.Http;
using ZstdSharp;
using static launcher.Global.References;
using System.Windows;

namespace launcher.Download
{
    public static class GlobalDownloadStats
    {
        // Total size in bytes for all files (set this at the start if known)
        public static long TotalBytes = 0;

        // Total downloaded bytes so far (across all files)
        public static long DownloadedBytes = 0;

        // Overall start time for the complete download operation
        public static DateTime StartTime;
    }

    public static class Tasks
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
            string speedText;
            double speed = speedBytesPerSecond;

            if (speed >= 1024 * 1024)
            {
                speed /= (1024 * 1024);
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

            appDispatcher.Invoke(() =>
            {
                Speed_Label.Text = $"{speedText}";
                Downloads_Control.Speed_Label.Text = $"{speedText}";
            });
        }

        public static void ConfigureConcurrency()
        {
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
        public static async Task UpdateGlobalDownloadProgressAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                //var elapsed = DateTime.Now - GlobalDownloadStats.StartTime;
                //double avgSpeed = elapsed.TotalSeconds > 0 ? (double)GlobalDownloadStats.DownloadedBytes / elapsed.TotalSeconds : 0;
                //long remainingBytes = GlobalDownloadStats.TotalBytes - GlobalDownloadStats.DownloadedBytes;
                //TimeSpan estimatedRemaining = avgSpeed > 0 ? TimeSpan.FromSeconds(remainingBytes / avgSpeed) : TimeSpan.Zero;

                await appDispatcher.InvokeAsync(() =>
                {
                    double totalSize = GlobalDownloadStats.TotalBytes >= 1024L * 1024 * 1024 ? GlobalDownloadStats.TotalBytes / (1024.0 * 1024 * 1024) : GlobalDownloadStats.TotalBytes / (1024.0 * 1024.0);
                    string totalText = GlobalDownloadStats.TotalBytes >= 1024L * 1024 * 1024 ? $"{totalSize:F2} GB" : $"{totalSize:F2} MB";

                    double downloadedSize = GlobalDownloadStats.DownloadedBytes >= 1024L * 1024 * 1024 ? GlobalDownloadStats.DownloadedBytes / (1024.0 * 1024 * 1024) : GlobalDownloadStats.DownloadedBytes / (1024.0 * 1024.0);
                    string downloadedText = GlobalDownloadStats.DownloadedBytes >= 1024L * 1024 * 1024 ? $"{downloadedSize:F2} GB" : $"{downloadedSize:F2} MB";

                    Main_Window.TimeLeft_Label.Text = $"{downloadedText}/{totalText} "; //+ $"- Estimated time remaining: {estimatedRemaining:hh\\:mm\\:ss}";
                });

                // Update every second
                await Task.Delay(1000, token);
            }

            Main_Window.TimeLeft_Label.Text = "";
        }
        private static async Task<long> GetContentLengthAsync(string fileUrl)
        {
            var request = (HttpWebRequest)WebRequest.Create(fileUrl);
            request.Method = "HEAD"; // Use the HEAD method to fetch only headers
            request.Timeout = 10000; // Optional: set a timeout as needed

            using (var response = (HttpWebResponse)await request.GetResponseAsync())
            {
                return response.ContentLength;  // Returns the Content-Length header value
            }
        }

        public static List<Task<string>> InitializeDownloadTasks(GameFiles gameFiles, string branchDirectory)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            if (gameFiles == null) throw new ArgumentNullException(nameof(gameFiles));
            if (string.IsNullOrWhiteSpace(branchDirectory)) throw new ArgumentException("Branch directory cannot be null or empty.", nameof(branchDirectory));

            var downloadTasks = new List<Task<string>>(gameFiles.files.Count);
            ConfigureProgress(gameFiles.files.Count);

            foreach (var file in gameFiles.files)
            {
                string fileUrl = $"{GetBranch.GameURL()}/{file.name}";
                string destinationPath = Path.Combine(branchDirectory, file.name);

                EnsureDirectoryExists(destinationPath);

                downloadTasks.Add(
                    DownloadFileAsync(
                        fileUrl,
                        destinationPath,
                        file.name,
                        file.checksum,
                        checkForExistingFiles: true
                    )
                );
            }

            GlobalDownloadStats.TotalBytes = 0;
            GlobalDownloadStats.DownloadedBytes = 0;
            GlobalDownloadStats.StartTime = DateTime.Now;

            return downloadTasks;
        }

        public static List<Task<string>> InitializeRepairTasks(string branchDirectory)
        {
            if (string.IsNullOrWhiteSpace(branchDirectory)) throw new ArgumentException("Temporary directory cannot be null or empty.", nameof(branchDirectory));

            int badFilesCount = DataCollections.BadFiles.Count;
            ConfigureProgress(badFilesCount);

            var downloadTasks = new List<Task<string>>(badFilesCount);

            foreach (var file in DataCollections.BadFiles)
            {
                string fileUrl = $"{GetBranch.GameURL()}/{file}";
                string destinationPath = Path.Combine(branchDirectory, file);

                EnsureDirectoryExists(destinationPath);

                downloadTasks.Add(
                    DownloadFileAsync(
                        fileUrl,
                        destinationPath,
                        file,
                        checkForExistingFiles: false
                    )
                );
            }

            GlobalDownloadStats.TotalBytes = 0;
            GlobalDownloadStats.DownloadedBytes = 0;
            GlobalDownloadStats.StartTime = DateTime.Now;

            return downloadTasks;
        }

        private static async Task<string> DownloadFileAsync(string fileUrl, string destinationPath, string fileName, string checksum = "", bool checkForExistingFiles = false)
        {
            await _downloadSemaphore.WaitAsync();

            DownloadItem downloadItem = await AddDownloadItemAsync(fileName);

            await Task.Delay(2000);

            try
            {
                if (File.Exists(destinationPath.Replace(".zst", "")))
                    File.Delete(destinationPath.Replace(".zst", ""));

                if (checkForExistingFiles && !string.IsNullOrWhiteSpace(checksum) && ShouldSkipDownload(destinationPath, checksum))
                {
                    //Decompress the file
                    await CreateRetryPolicy(destinationPath, 5, downloadItem).ExecuteAsync(async () =>
                    {
                        await DecompressFileAsync(destinationPath, destinationPath.Replace(".zst", ""), downloadItem);
                    });

                    return destinationPath;
                }

                //Download the file
                await CreateRetryPolicy(destinationPath, 15, downloadItem).ExecuteAsync(async () =>
                {
                    await DownloadWithThrottlingAsync(fileUrl, destinationPath, downloadItem);
                });

                //Decompress the file
                await CreateRetryPolicy(destinationPath, 5, downloadItem).ExecuteAsync(async () =>
                {
                    await DecompressFileAsync(destinationPath, destinationPath.Replace(".zst", ""), downloadItem);
                });

                return destinationPath;
            }
            catch (Exception ex)
            {
                LogException($"All retries failed for {fileUrl}", Source.Download, ex);

                AppState.BadFilesDetected = true;
                return string.Empty;
            }
            finally
            {
                appDispatcher.Invoke(() =>
                {
                    Progress_Bar.Value++;
                    Files_Label.Text = $"{--AppState.FilesLeft} files left";
                    Percent_Label.Text = $"{(Progress_Bar.Value / Progress_Bar.Maximum * 100):F2}%";
                });

                if (File.Exists(destinationPath))
                    File.Delete(destinationPath);

                _downloadSemaphore.Release();

                await RemoveDownloadItemAsync(downloadItem);
            }
        }

        private static bool ShouldSkipDownload(string destinationPath, string expectedChecksum)
        {
            if (File.Exists(destinationPath))
            {
                string actualChecksum = Checksums.CalculateChecksum(destinationPath);
                if (string.Equals(actualChecksum, expectedChecksum, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static void EnsureDirectoryExists(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private static void ConfigureProgress(int totalFiles)
        {
            AppState.FilesLeft = totalFiles;

            appDispatcher.Invoke(() =>
            {
                Progress_Bar.Maximum = totalFiles;
                Progress_Bar.Value = 0;
                Files_Label.Text = $"{totalFiles} files left";
                Percent_Label.Text = "0%";
            });
        }

        private static AsyncRetryPolicy CreateRetryPolicy(string fileUrl, int maxRetryAttempts, DownloadItem downloadItem)
        {
            int retryDelaySeconds = 5;

            return Policy
                .Handle<Exception>(ex =>
                {
                    if (ex is WebException webEx && webEx.Response is HttpWebResponse httpWebResponse)
                    {
                        if (httpWebResponse.StatusCode == HttpStatusCode.NotFound)
                        {
                            LogWarning(Source.Download, $"(404) Not Found: {fileUrl}");
                            return false;
                        }
                    }
                    else if (ex is HttpRequestException httpEx && httpEx.StatusCode == HttpStatusCode.NotFound)
                    {
                        LogWarning(Source.Download, $"(404) Not Found: {fileUrl}");
                        return false;
                    }

                    // Handle all other exceptions
                    return true;
                })
                .WaitAndRetryAsync(
                    retryCount: maxRetryAttempts,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(1),
                    onRetryAsync: async (exception, timeSpan, retryNumber, context) =>
                    {
                        Log(
                            Logger.Type.Warning,
                            Source.Download,
                            $"Retry #{retryNumber} for '{fileUrl}' due to: {exception.Message}."
                        );

                        for (int remaining = retryDelaySeconds; remaining > 0; remaining--)
                        {
                            appDispatcher.Invoke(() =>
                            {
                                downloadItem.downloadFilePercent.Text = $"Retrying in {remaining} second{(remaining > 1 ? "s" : "")}...";
                                downloadItem.downloadFileProgress.Value = 0;
                            });

                            await Task.Delay(1000);
                        }
                    }
                );
        }

        private static async Task<DownloadItem> AddDownloadItemAsync(string fileName)
        {
            return await appDispatcher.InvokeAsync(() => Downloads_Control.AddDownloadItem(fileName));
        }

        private static async Task RemoveDownloadItemAsync(DownloadItem downloadItem)
        {
            try
            {
                await appDispatcher.InvokeAsync(() => Downloads_Control.RemoveDownloadItem(downloadItem));
            }
            catch
            {
            }
        }

        private static List<string> UserAgents = new List<string>()
        {
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:124.0) Gecko/20100101 Firefox/124.0",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36 Edg/123.0.2420.81",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36 OPR/109.0.0.0",
        };

        private static async Task DownloadWithThrottlingAsync(string fileUrl, string destinationPath, DownloadItem downloadItem)
        {
            Random r = new Random();

            var request = (HttpWebRequest)WebRequest.Create(fileUrl);
            request.Method = "GET";
            request.Timeout = 10000;
            request.AllowAutoRedirect = true;
            request.Host = request.RequestUri.Host;
            request.UserAgent = UserAgents[r.Next(0, UserAgents.Count - 1)];

            using (var response = (HttpWebResponse)await request.GetResponseAsync())
            {
                if (response.StatusCode != HttpStatusCode.OK)
                    throw new WebException($"Failed to download: {response.StatusCode}");

                long totalBytes = response.ContentLength;
                long downloadedBytes = 0;
                DateTime lastUpdate = DateTime.Now;

                GlobalDownloadStats.TotalBytes += totalBytes;

                using var responseStream = response.GetResponseStream();

                using var throttledStream = new ThrottledStream(responseStream, GlobalBandwidthLimiter.Instance);

                using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);

                byte[] buffer = new byte[4096];
                int bytesRead;

                while ((bytesRead = await throttledStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    downloadedBytes += bytesRead;

                    DownloadSpeedTracker.AddDownloadedBytes(bytesRead);

                    Interlocked.Add(ref GlobalDownloadStats.DownloadedBytes, bytesRead);

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
                Percent_Label.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
                ReadMore_Label.Visibility = isVisible ? Visibility.Hidden : Visibility.Visible;
            });
        }

        public static void ShowSpeedLabels(bool Speed_Label_isVisible, bool Downloads_Control_Speed_Label_isVisible)
        {
            appDispatcher.Invoke(() =>
            {
                Speed_Label.Visibility = Speed_Label_isVisible ? Visibility.Visible : Visibility.Hidden;
                Downloads_Control.Speed_Label.Visibility = Downloads_Control_Speed_Label_isVisible ? Visibility.Visible : Visibility.Hidden;
                Speed_Label.Text = "";
                Downloads_Control.Speed_Label.Text = "";
                Main_Window.TimeLeft_Label.Text = "";
            });
        }

        public static async Task DecompressFileAsync(string compressedFilePath, string decompressedFilePath, DownloadItem downloadItem)
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
    }
}