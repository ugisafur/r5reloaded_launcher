using launcher.Game;
using launcher.Global;
using launcher.Network;
using Polly;
using Polly.Retry;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Net.Http;
using System.Windows;
using System.Windows.Shell;
using ZstdSharp;
using static launcher.Global.Logger;
using static launcher.Global.References;

namespace launcher.Download
{
    public static class GlobalDownloadStats
    {
        public static long TotalBytes = 0;
        public static long DownloadedBytes = 0;
        public static DateTime StartTime;
        public static string totalText = "";
        public static string downloadedText = "";
        public static string timeLeftText = "";
    }

    public class FileDownload
    {
        public long downloadedBytes = 0;
    }

    public class MultiPartFile
    {
        public long totalBytes = 0;
        public long downloadedBytes = 0;
        public DateTime lastUpdate = DateTime.Now;
    }

    public static class Tasks
    {
        public static long _downloadSpeedLimit = 0;
        public static SemaphoreSlim _downloadSemaphore;
        public static DownloadSpeedMonitor _speedMonitor;
        public static double currentDownloadSpeed = 0;
        public static int UpdateType = 0; // 0 = install, 1 = repair, 2 = uninstall

        public static void CreateDownloadMonitor()
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
            currentDownloadSpeed = speed;

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
                Downloads_Control.Speed_Label.Text = $"{GlobalDownloadStats.timeLeftText}  |  {GlobalDownloadStats.downloadedText}/{GlobalDownloadStats.totalText}  |  {speedText}";
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
            DateTime lastPresenceUpdate = DateTime.MinValue;

            while (!token.IsCancellationRequested)
            {
                var elapsed = DateTime.Now - GlobalDownloadStats.StartTime;
                double avgSpeed = elapsed.TotalSeconds > 0
                    ? GlobalDownloadStats.DownloadedBytes / elapsed.TotalSeconds
                    : 0;

                // Use the current speed if available, otherwise fall back to average
                double currentSpeed = Tasks.currentDownloadSpeed; // in bytes/sec
                double effectiveSpeed = currentSpeed > 0
                    ? (avgSpeed + currentSpeed) / 2   // blend average + current
                    : avgSpeed;

                long remainingBytes = GlobalDownloadStats.TotalBytes - GlobalDownloadStats.DownloadedBytes;
                TimeSpan estimatedRemaining = effectiveSpeed > 0
                    ? TimeSpan.FromSeconds(remainingBytes / effectiveSpeed)
                    : TimeSpan.Zero;

                await appDispatcher.InvokeAsync(() =>
                {
                    Progress_Bar.Value = GlobalDownloadStats.DownloadedBytes;
                    Progress_Bar.Maximum = GlobalDownloadStats.TotalBytes;
                    Percent_Label.Text = $"{(Math.Min(GlobalDownloadStats.DownloadedBytes / (double)GlobalDownloadStats.TotalBytes * 100, 99)):F2}%";

                    double totalSize = GlobalDownloadStats.TotalBytes >= 1024L * 1024 * 1024 ? GlobalDownloadStats.TotalBytes / (1024.0 * 1024 * 1024) : GlobalDownloadStats.TotalBytes / (1024.0 * 1024.0);
                    string totalText = GlobalDownloadStats.TotalBytes >= 1024L * 1024 * 1024 ? $"{totalSize:F2} GB" : $"{totalSize:F2} MB";

                    double downloadedSize = GlobalDownloadStats.DownloadedBytes >= 1024L * 1024 * 1024 ? GlobalDownloadStats.DownloadedBytes / (1024.0 * 1024 * 1024) : GlobalDownloadStats.DownloadedBytes / (1024.0 * 1024.0);
                    string downloadedText = GlobalDownloadStats.DownloadedBytes >= 1024L * 1024 * 1024 ? $"{downloadedSize:F2} GB" : $"{downloadedSize:F2} MB";

                    string timeLeft = estimatedRemaining.TotalHours >= 1 ? estimatedRemaining.ToString(@"h\:mm\:ss") : estimatedRemaining.ToString(@"m\:ss");

                    Main_Window.TimeLeft_Label.Text = $"{downloadedText}/{totalText} - Time Left: {timeLeft}";

                    GlobalDownloadStats.timeLeftText = $"Time Left: {timeLeft}";
                    GlobalDownloadStats.totalText = totalText;
                    GlobalDownloadStats.downloadedText = downloadedText;

                    if ((DateTime.UtcNow - lastPresenceUpdate).TotalSeconds >= 5)
                    {

                        string UpdateTypeString = UpdateType == 0 ? "Downloading" : "Repairing";

                        AppState.SetRichPresence($"{UpdateTypeString} {GetBranch.Name()}", $"{downloadedText}/{totalText} - Time Left: {estimatedRemaining:hh\\:mm\\:ss}");
                        lastPresenceUpdate = DateTime.UtcNow;
                    }
                });

                await Task.Delay(100, token);
            }

            Main_Window.TimeLeft_Label.Text = "";
        }


        public static List<Task<string>> InitializeDownloadTasks(GameFiles gameFiles, string branchDirectory)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            if (gameFiles == null) throw new ArgumentNullException(nameof(gameFiles));
            if (string.IsNullOrWhiteSpace(branchDirectory)) throw new ArgumentException("Branch directory cannot be null or empty.", nameof(branchDirectory));

            var downloadTasks = new List<Task<string>>(gameFiles.files.Count);

            foreach (var file in gameFiles.files)
            {
                if (file.destinationPath.Contains("platform\\cfg\\user", StringComparison.OrdinalIgnoreCase) || file.destinationPath.Contains("platform\\screenshots", StringComparison.OrdinalIgnoreCase) || file.destinationPath.Contains("platform\\logs", StringComparison.OrdinalIgnoreCase))
                    continue;

                string fileUrl = $"{GetBranch.GameURL()}/{file.destinationPath}";
                string finalPath = Path.Combine(branchDirectory, file.destinationPath);

                EnsureDirectoryExists(finalPath);

                downloadTasks.Add(
                    DownloadFileAsync(
                        fileUrl,
                        finalPath,
                        file,
                        checkForExistingFiles: true
                    )
                );
            }

            SetGlobalDownloadStats(gameFiles.files.Sum(f => f.sizeInBytes), 0, DateTime.Now);

            return downloadTasks;
        }

        public static List<Task<string>> InitializeRepairTasks(string branchDirectory)
        {
            var downloadTasks = new List<Task<string>>(DataCollections.BadFiles.Count);

            foreach (var file in DataCollections.BadFiles)
            {
                if (file.destinationPath.Contains("platform\\cfg\\user", StringComparison.OrdinalIgnoreCase) || file.destinationPath.Contains("platform\\screenshots", StringComparison.OrdinalIgnoreCase) || file.destinationPath.Contains("platform\\logs", StringComparison.OrdinalIgnoreCase))
                    continue;

                string fileUrl = $"{GetBranch.GameURL()}/{file.destinationPath}";
                string finalPath = Path.Combine(branchDirectory, file.destinationPath);

                EnsureDirectoryExists(finalPath);

                downloadTasks.Add(
                    DownloadFileAsync(
                        fileUrl,
                        finalPath,
                        file,
                        checkForExistingFiles: false
                    )
                );
            }

            SetGlobalDownloadStats(DataCollections.BadFiles.Sum(f => f.sizeInBytes), 0, DateTime.Now);

            return downloadTasks;
        }

        private static void SetGlobalDownloadStats(long totalBytes, long downloadedBytes, DateTime startTime)
        {
            GlobalDownloadStats.TotalBytes = totalBytes;
            GlobalDownloadStats.DownloadedBytes = downloadedBytes;
            GlobalDownloadStats.StartTime = startTime;
        }

        private static async Task<string> DownloadFileAsync(string fileUrl, string finalPath, GameFile file, bool checkForExistingFiles = false)
        {
            await _downloadSemaphore.WaitAsync();

            DownloadItem downloadItem = await appDispatcher.InvokeAsync(() => Downloads_Control.AddDownloadItem(file.destinationPath));

            await Task.Delay(2000);

            try
            {
                if (checkForExistingFiles && !string.IsNullOrWhiteSpace(file.checksum) && ShouldSkipDownload(finalPath, file.checksum))
                    return finalPath;

                FileDownload fileDownload = new();

                await CreateRetryPolicy(finalPath, 15, downloadItem, fileDownload).ExecuteAsync(async () =>
                {
                    await (file.parts.Count > 0
                        ? DownloadFileInPartsAsync(fileUrl, finalPath, file, downloadItem, fileDownload)
                        : DownloadSingleStreamAsync(fileUrl, finalPath, downloadItem, fileDownload));
                });

                return finalPath;
            }
            catch (Exception ex)
            {
                LogException($"All retries failed for {fileUrl}", Source.Download, ex);

                AppState.BadFilesDetected = true;
                return string.Empty;
            }
            finally
            {
                _downloadSemaphore.Release();
                await appDispatcher.InvokeAsync(() => Downloads_Control.RemoveDownloadItem(downloadItem));
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

        private static AsyncRetryPolicy CreateRetryPolicy(string fileUrl, int maxRetryAttempts, DownloadItem downloadItem, FileDownload fileDownload)
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

                        DownloadSpeedTracker.RemoveDownloadedBytes(fileDownload.downloadedBytes);
                        Interlocked.Add(ref GlobalDownloadStats.DownloadedBytes, -fileDownload.downloadedBytes);
                        fileDownload.downloadedBytes = 0;

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

        private static async Task DownloadFileInPartsAsync(string fileUrl, string destinationPath, GameFile file, DownloadItem downloadItem, FileDownload fileDownload)
        {
            int partCount = file.parts.Count;
            var partTasks = new List<Task>();

            MultiPartFile multiPartFile = new();
            multiPartFile.totalBytes = file.sizeInBytes;

            foreach (Part part in file.parts)
            {
                if (ShouldSkipDownload(Path.Combine(GetBranch.Directory(), part.path), part.checksum))
                {
                    multiPartFile.downloadedBytes += part.sizeInBytes;
                    fileDownload.downloadedBytes += part.sizeInBytes;
                    DownloadSpeedTracker.AddDownloadedBytes(part.sizeInBytes);
                    Interlocked.Add(ref GlobalDownloadStats.DownloadedBytes, part.sizeInBytes);
                    continue;
                }

                partTasks.Add(DownloadMultiStreamAsync($"{GetBranch.GameURL()}/{part.path}", Path.Combine(GetBranch.Directory(), part.path), downloadItem, multiPartFile, fileDownload));
            }

            await Task.WhenAll(partTasks);

            using var dest = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
            {
                int currentPart = 0;
                foreach (Part part in file.parts)
                {
                    await appDispatcher.InvokeAsync(() =>
                    {
                        downloadItem.downloadFilePercent.Text = $"Merging Parts: {currentPart++} / {partCount + 1}";
                        downloadItem.downloadFileProgress.Value = currentPart / partCount + 1;
                    });

                    using var partStream = new FileStream(Path.Combine(GetBranch.Directory(), part.path), FileMode.Open, FileAccess.Read, FileShare.Read);
                    {
                        await partStream.CopyToAsync(dest);
                    }

                    await Task.Delay(100);
                }
            }

            foreach (Part part in file.parts)
            {
                File.Delete(Path.Combine(GetBranch.Directory(), part.path));
            }
        }

        private static async Task DownloadMultiStreamAsync(string fileUrl, string destinationPath, DownloadItem downloadItem, MultiPartFile multiPartFile, FileDownload fileDownload)
        {
            Random r = new Random();

            var request = (HttpWebRequest)WebRequest.Create(fileUrl);
            request.Method = "GET";
            request.Timeout = 10000;
            request.AllowAutoRedirect = true;
            request.Host = request.RequestUri.Host;
            request.UserAgent = $"R5Reloaded-Launcher/{Launcher.VERSION} (+https://r5reloaded.com)";

            using (var response = (HttpWebResponse)await request.GetResponseAsync())
            {
                if (response.StatusCode != HttpStatusCode.OK)
                    throw new WebException($"Failed to download: {response.StatusCode}");

                DateTime lastUpdate = DateTime.Now;
                DateTime speedCheckStart = DateTime.Now;
                long bytesAtStart = 0;

                using var responseStream = response.GetResponseStream();

                using var throttledStream = new ThrottledStream(responseStream, GlobalBandwidthLimiter.Instance);

                using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);

                byte[] buffer = new byte[4096];
                int bytesRead;

                while ((bytesRead = await throttledStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    multiPartFile.downloadedBytes += bytesRead;
                    fileDownload.downloadedBytes += bytesRead;

                    DownloadSpeedTracker.AddDownloadedBytes(bytesRead);

                    Interlocked.Add(ref GlobalDownloadStats.DownloadedBytes, bytesRead);

                    if ((DateTime.Now - multiPartFile.lastUpdate).TotalMilliseconds > 200)
                    {
                        multiPartFile.lastUpdate = DateTime.Now;
                        if (downloadItem != null && multiPartFile.totalBytes > 0)
                        {
                            double totalSize = multiPartFile.totalBytes >= 1024L * 1024 * 1024 ? multiPartFile.totalBytes / (1024.0 * 1024 * 1024) : multiPartFile.totalBytes / (1024.0 * 1024.0);
                            string totalText = multiPartFile.totalBytes >= 1024L * 1024 * 1024 ? $"{totalSize:F2} GB" : $"{totalSize:F2} MB";

                            double downloadedSize = multiPartFile.downloadedBytes >= 1024L * 1024 * 1024 ? multiPartFile.downloadedBytes / (1024.0 * 1024 * 1024) : multiPartFile.downloadedBytes / (1024.0 * 1024.0);
                            string downloadedText = multiPartFile.downloadedBytes >= 1024L * 1024 * 1024 ? $"{downloadedSize:F2} GB" : $"{downloadedSize:F2} MB";

                            await appDispatcher.InvokeAsync(() =>
                            {
                                downloadItem.downloadFilePercent.Text = $"{downloadedText} / {totalText}";
                                downloadItem.downloadFileProgress.Value = (double)multiPartFile.downloadedBytes / multiPartFile.totalBytes * 100;
                            });
                        }

                        if ((DateTime.Now - speedCheckStart).TotalSeconds >= 5)
                        {
                            long delta = multiPartFile.downloadedBytes - bytesAtStart;
                            if (delta == 0)
                                throw new TimeoutException("Download stalled (no data received for 5s).");

                            bytesAtStart = multiPartFile.downloadedBytes;
                            speedCheckStart  = DateTime.Now;
                        }
                    }
                }

                await fileStream.FlushAsync();
            }
        }

        private static async Task DownloadSingleStreamAsync(string fileUrl, string destinationPath, DownloadItem downloadItem, FileDownload fileDownload)
        {
            Random r = new Random();

            var request = (HttpWebRequest)WebRequest.Create(fileUrl);
            request.Method = "GET";
            request.Timeout = 10000;
            request.AllowAutoRedirect = true;
            request.Host = request.RequestUri.Host;
            request.UserAgent = $"R5Reloaded-Launcher/{Launcher.VERSION} (+https://r5reloaded.com)";

            using (var response = (HttpWebResponse)await request.GetResponseAsync())
            {
                if (response.StatusCode != HttpStatusCode.OK)
                    throw new WebException($"Failed to download: {response.StatusCode}");

                long totalBytes = response.ContentLength;
                long downloadedBytes = 0;
                DateTime lastUpdate = DateTime.Now;
                DateTime speedCheckStart = DateTime.Now;
                long bytesAtStart = 0;

                using var responseStream = response.GetResponseStream();

                using var throttledStream = new ThrottledStream(responseStream, GlobalBandwidthLimiter.Instance);

                using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);

                byte[] buffer = new byte[4096];
                int bytesRead;

                while ((bytesRead = await throttledStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    downloadedBytes += bytesRead;
                    fileDownload.downloadedBytes += bytesRead;

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

                    if ((DateTime.Now - speedCheckStart).TotalSeconds >= 5)
                    {
                        long delta = downloadedBytes - bytesAtStart;
                        if (delta == 0)
                            throw new TimeoutException("Download stalled (no data received for 5s).");

                        bytesAtStart = downloadedBytes;
                        speedCheckStart  = DateTime.Now;
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

                GameSettings_Control.RepairGame_Button.IsEnabled = !installing && GetBranch.Installed();
                GameSettings_Control.UninstallGame_Button.IsEnabled = !installing && GetBranch.Installed();

                Settings_Control.gameInstalls.UpdateGameItems();
            });

            ShowProgressBar(installing);
        }

        public static void UpdateStatusLabel(string statusText, Source source)
        {
            AppState.SetRichPresence($"Branch: {GetBranch.Name()}", statusText);
            LogInfo(source, $"Updating status label: {statusText}");
            appDispatcher.Invoke(() => {  Status_Label.Text = statusText; });
        }

        private static void ShowProgressBar(bool isVisible)
        {
            appDispatcher.Invoke(() =>
            {
                Progress_Bar.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
                Status_Label.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
                Percent_Label.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
                Main_Window.TimeLeft_Label.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
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
    }
}