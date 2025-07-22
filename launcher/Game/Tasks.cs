using DiscordRPC;
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
using static launcher.Network.DownloadTracker;

namespace launcher.Game
{
    public static class Tasks
    {
        private static readonly HttpClient httpClient = new HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        public static List<Task<string>> InitializeDownloadTasks(GameFiles gameFiles, string branchDirectory)
        {
            if (gameFiles == null) throw new ArgumentNullException(nameof(gameFiles));
            return CreateDownloadTasks(gameFiles.files, branchDirectory, checkForExistingFiles: true);
        }

        public static List<Task<string>> InitializeRepairTasks(string branchDirectory)
        {
            return CreateDownloadTasks(DataCollections.BadFiles, branchDirectory, checkForExistingFiles: false);
        }

        private static List<Task<string>> CreateDownloadTasks(IEnumerable<GameFile> files, string branchDirectory, bool checkForExistingFiles)
        {
            if (string.IsNullOrWhiteSpace(branchDirectory)) throw new ArgumentException("Branch directory cannot be null or empty.", nameof(branchDirectory));

            var downloadTasks = files
                .Where(file => !IsUserGeneratedContent(file))
                .Select(file =>
                {
                    file.downloadMetadata.fileUrl = $"{GetBranch.GameURL()}/{file.path}";
                    file.downloadMetadata.finalPath = Path.Combine(branchDirectory, file.path);
                    EnsureDirectoryExists(file);

                    return DownloadFileAsync(file, checkForExistingFiles);
                })
                .ToList();

            long totalSize = files.Sum(f => f.size);
            SetGlobalDownloadStats(totalSize, 0, DateTime.Now);

            return downloadTasks;
        }

        private static bool IsUserGeneratedContent(GameFile file)
        {
            string path = file.path;
            return path.Contains("platform\\cfg\\user", StringComparison.OrdinalIgnoreCase) ||
                   path.Contains("platform\\screenshots", StringComparison.OrdinalIgnoreCase) ||
                   path.Contains("platform\\logs", StringComparison.OrdinalIgnoreCase);
        }

        private static async Task<string> DownloadFileAsync(GameFile file, bool checkForExistingFiles = false)
        {
            // ✅ For multi-part files, we skip the semaphore here and let each part get one.
            if (file.parts.Count > 0)
            {
                // This is now an orchestrator task.
                return await DownloadFileInPartsAsync(file, checkForExistingFiles);
            }

            // For single files, the logic remains the same: acquire a semaphore and download.
            await GetSemaphoreSlim().WaitAsync();
            try
            {
                file.downloadMetadata.downloadItem = await appDispatcher.InvokeAsync(() => Downloads_Control.AddDownloadItem(file));

                bool isSkipped = checkForExistingFiles && await ShouldSkipDownloadAsync(file.path, file.checksum);
                if (isSkipped)
                {
                    AddDownloadedBytes(file.size, file);
                }
                else
                {
                    var retryPolicy = CreateRetryPolicy(file, 15);
                    await retryPolicy.ExecuteAsync(() => DownloadSingleStreamAsync(file));
                }
                return file.downloadMetadata.finalPath;
            }
            catch (Exception ex)
            {
                LogException($"All retries failed for {file.downloadMetadata.fileUrl}", LogSource.Download, ex);
                AppState.BadFilesDetected = true;
                return string.Empty;
            }
            finally
            {
                GetSemaphoreSlim().Release();
                if (file.downloadMetadata.downloadItem != null)
                    await appDispatcher.InvokeAsync(() => Downloads_Control.RemoveDownloadItem(file.downloadMetadata.downloadItem));
            }
        }

        private static async Task<bool> ShouldSkipDownloadAsync(string destinationPath, string checksum)
        {
            await Task.Delay(1);
            if (File.Exists(destinationPath))
            {
                // Use `await` to get the string result from the Task<string>.
                string actualChecksum = Checksums.CalculateChecksum(destinationPath);

                // Check for null in case the checksum calculation failed.
                if (actualChecksum != null && string.Equals(actualChecksum, checksum, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static void EnsureDirectoryExists(GameFile file)
        {
            string directory = Path.GetDirectoryName(file.downloadMetadata.finalPath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }

        private static AsyncRetryPolicy CreateRetryPolicy(GameFile file, int maxRetryAttempts)
        {
            var random = new Random();

            return Policy
                .Handle<Exception>(ex => ShouldRetry(ex, file))
                .WaitAndRetryAsync(
                    retryCount: maxRetryAttempts,
                    sleepDurationProvider: retryAttempt =>
                        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) + TimeSpan.FromMilliseconds(random.Next(0, 500)),

                    onRetryAsync: async (exception, calculatedDelay, retryNumber, context) =>
                    {
                        LogWarning(LogSource.Download, $"Retry #{retryNumber} for '{file.downloadMetadata.fileUrl}' in {calculatedDelay.TotalSeconds:F1}s due to: {exception.Message}");

                        RemoveDownloadedBytes(file.downloadMetadata.fileDownload.downloadedBytes);
                        file.downloadMetadata.fileDownload.downloadedBytes = 0;

                        await appDispatcher.InvokeAsync(() =>
                        {
                            file.downloadMetadata.downloadItem.downloadFilePercent.Text = $"Download failed. Retrying...";
                            file.downloadMetadata.downloadItem.downloadFileProgress.Value = 0;
                        });
                    }
                );
        }

        private static bool ShouldRetry(Exception ex, GameFile file)
        {
            if (ex is HttpRequestException { StatusCode: HttpStatusCode.NotFound } ||
                ex is WebException { Response: HttpWebResponse { StatusCode: HttpStatusCode.NotFound } })
            {
                LogWarning(LogSource.Download, $"(404) Not Found, will not retry: {file.downloadMetadata.fileUrl}");
                return false; // Do NOT retry for 404 errors.
            }

            return true; // Retry for all other exceptions.
        }

        private static async Task<string> DownloadFileInPartsAsync(GameFile file, bool checkForExistingFiles)
        {
            try
            {
                file.downloadMetadata.downloadItem = await appDispatcher.InvokeAsync(() => Downloads_Control.AddDownloadItem(file));
                file.downloadMetadata.fileDownload.totalBytes = file.size;

                await DownloadMissingPartsAsync(file, checkForExistingFiles);
                await MergePartsAsync(file);
                CleanupPartFiles(file);

                return file.downloadMetadata.finalPath;
            }
            catch (Exception ex)
            {
                LogException($"Failed to process multi-part file {file.path}", LogSource.Download, ex);
                AppState.BadFilesDetected = true;
                return string.Empty;
            }
            finally
            {
                if (file.downloadMetadata.downloadItem != null)
                    await appDispatcher.InvokeAsync(() => Downloads_Control.RemoveDownloadItem(file.downloadMetadata.downloadItem));
            }
        }

        private static async Task DownloadPartAsync(GameFile parentFile, FilePart part, string partUrl, string partPath)
        {
            await GetSemaphoreSlim().WaitAsync();
            try
            {
                // Each part gets its own retry policy.
                var retryPolicy = CreateRetryPolicy(parentFile, 15); // Pass parent file for logging context.
                await retryPolicy.ExecuteAsync(() => DownloadMultiStreamAsync(partUrl, partPath, parentFile));
            }
            catch (Exception ex)
            {
                LogException($"Failed to download part {part.path}: {ex.Message}", LogSource.Download, ex);
                // Re-throw to fail the Task.WhenAll in the caller.
                throw;
            }
            finally
            {
                GetSemaphoreSlim().Release();
            }
        }

        private static Task DownloadMissingPartsAsync(GameFile file, bool checkForExistingFiles)
        {
            string branchDirectory = GetBranch.Directory();
            string gameUrl = GetBranch.GameURL();

            // ✅ Each part is now mapped to its own concurrent download task.
            var downloadTasks = file.parts.Select(async part =>
            {
                string partPath = Path.Combine(branchDirectory, part.path);
                if (checkForExistingFiles && await ShouldSkipDownloadAsync(partPath, part.checksum))
                {
                    AddDownloadedBytes(part.size, file);
                }
                else
                {
                    string partUrl = $"{gameUrl}/{part.path}";
                    await DownloadPartAsync(file, part, partUrl, partPath);
                }
            }).ToList();

            return Task.WhenAll(downloadTasks);
        }

        private static async Task MergePartsAsync(GameFile file)
        {
            string branchDirectory = GetBranch.Directory();
            using var finalStream = new FileStream(file.downloadMetadata.finalPath, FileMode.Create, FileAccess.Write, FileShare.None);

            for (int i = 0; i < file.parts.Count; i++)
            {
                var part = file.parts[i];
                int currentPartNumber = i + 1;

                await appDispatcher.InvokeAsync(() =>
                {
                    file.downloadMetadata.downloadItem.downloadFilePercent.Text = $"Merging Parts: {currentPartNumber} / {file.parts.Count}";
                    file.downloadMetadata.downloadItem.downloadFileProgress.Value = (double)currentPartNumber / file.parts.Count * 100;
                });

                string partPath = Path.Combine(branchDirectory, part.path);
                using var partStream = new FileStream(partPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                await partStream.CopyToAsync(finalStream);
            }
        }

        private static void CleanupPartFiles(GameFile file)
        {
            string branchDirectory = GetBranch.Directory();
            foreach (var part in file.parts)
            {
                string partPath = Path.Combine(branchDirectory, part.path);
                if (File.Exists(partPath))
                {
                    File.Delete(partPath);
                }
            }
        }

        private static async Task DownloadMultiStreamAsync(string fileUrl, string destinationPath, GameFile file)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, fileUrl);
            request.Headers.UserAgent.ParseAdd($"R5Reloaded-Launcher/{Launcher.VERSION} (+https://r5reloaded.com)");

            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using var responseStream = await response.Content.ReadAsStreamAsync();
            await ProcessDownloadStreamAsync(file, responseStream, destinationPath, file.downloadMetadata.fileDownload.totalBytes);
        }

        private static async Task DownloadSingleStreamAsync(GameFile file)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, file.downloadMetadata.fileUrl);
            request.Headers.UserAgent.ParseAdd($"R5Reloaded-Launcher/{Launcher.VERSION} (+https://r5reloaded.com)");

            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            long totalBytes = response.Content.Headers.ContentLength ?? 0;
            using var responseStream = await response.Content.ReadAsStreamAsync();
            await ProcessDownloadStreamAsync(file, responseStream, file.downloadMetadata.finalPath, totalBytes);
        }

        private static async Task ProcessDownloadStreamAsync(GameFile file, Stream responseStream, string destinationPath, long totalBytes)
        {
            long bytesAtStart = 0;
            DateTime speedCheckStart = DateTime.Now;
            var metadata = file.downloadMetadata;

            using var throttledStream = new ThrottledStream(responseStream, GlobalBandwidthLimiter.Instance);
            using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);

            byte[] buffer = new byte[8192]; // Using a slightly larger buffer (e.g., 8KB) can be more efficient.
            int bytesRead;

            while ((bytesRead = await throttledStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead);

                // Update total downloaded bytes
                AddDownloadedBytes(bytesRead, file);

                // --- Progress Reporting (Throttled to 200ms) ---
                if ((DateTime.Now - metadata.fileDownload.lastUpdate).TotalMilliseconds > 200)
                {
                    metadata.fileDownload.lastUpdate = DateTime.Now;
                    if (metadata.downloadItem != null && totalBytes > 0)
                    {
                        string downloadedText = FormatBytes(metadata.fileDownload.downloadedBytes);
                        string totalText = FormatBytes(totalBytes);
                        double percentage = (double)metadata.fileDownload.downloadedBytes / totalBytes * 100;

                        await appDispatcher.InvokeAsync(() =>
                        {
                            metadata.downloadItem.downloadFilePercent.Text = $"{downloadedText} / {totalText}";
                            metadata.downloadItem.downloadFileProgress.Value = percentage;
                        });
                    }
                }

                // --- Stall Detection (Checked every 5 seconds) ---
                if ((DateTime.Now - speedCheckStart).TotalSeconds >= 5)
                {
                    long delta = metadata.fileDownload.downloadedBytes - bytesAtStart;
                    if (delta == 0)
                        throw new TimeoutException("Download stalled (no data received for 5s).");

                    bytesAtStart = metadata.fileDownload.downloadedBytes;
                    speedCheckStart = DateTime.Now;
                }
            }

            await fileStream.FlushAsync();
        }

        private static string FormatBytes(long bytes)
        {
            const long GIGABYTE = 1024L * 1024 * 1024;
            const long MEGABYTE = 1024L * 1024;

            if (bytes >= GIGABYTE)
            {
                return $"{(double)bytes / GIGABYTE:F2} GB";
            }
            return $"{(double)bytes / MEGABYTE:F2} MB";
        }

        public static void SetInstallState(bool isInstalling, string buttonText = "PLAY")
        {
            LogInfo(LogSource.Launcher, $"Setting install state to: {isInstalling}");

            appDispatcher.Invoke(() =>
            {
                bool isUiEnabled = !isInstalling;
                bool areGameOptionsEnabled = isUiEnabled && GetBranch.Installed();

                // --- Update Application State ---
                AppState.IsInstalling = isInstalling;
                AppState.BlockLanguageInstall = isInstalling;

                // --- Update Main UI Controls ---
                Play_Button.Content = buttonText;
                Play_Button.IsEnabled = isUiEnabled;
                Branch_Combobox.IsEnabled = isUiEnabled;
                Status_Label.Text = "";

                // --- Update Game Settings Controls ---
                GameSettings_Control.RepairGame_Button.IsEnabled = areGameOptionsEnabled;
                GameSettings_Control.UninstallGame_Button.IsEnabled = areGameOptionsEnabled;
                GameSettings_Control.OpenDir_Button.IsEnabled = areGameOptionsEnabled;
                GameSettings_Control.AdvancedMenu_Button.IsEnabled = areGameOptionsEnabled;

                // --- Update Other Components ---
                Settings_Control.gameInstalls.UpdateGameItems();
            });

            ShowProgressBar(isInstalling);
        }

        public static void UpdateStatusLabel(string statusText, LogSource source)
        {
            AppState.SetRichPresence($"Branch: {GetBranch.Name()}", statusText);
            appDispatcher.Invoke(() => {  Status_Label.Text = statusText; });
            LogInfo(source, $"Updating status label: {statusText}");
        }

        private static void ShowProgressBar(bool isVisible)
        {
            appDispatcher.Invoke(() =>
            {
                var primaryVisibility = isVisible ? Visibility.Visible : Visibility.Hidden;
                var inverseVisibility = isVisible ? Visibility.Hidden : Visibility.Visible;

                Progress_Bar.Visibility = primaryVisibility;
                Status_Label.Visibility = primaryVisibility;
                Percent_Label.Visibility = primaryVisibility;
                Main_Window.TimeLeft_Label.Visibility = primaryVisibility;
                ReadMore_Label.Visibility = inverseVisibility;
            });
        }

        public static void ShowSpeedLabels(bool isMainSpeedVisible, bool isDownloadSpeedVisible)
        {
            appDispatcher.Invoke(() =>
            {
                // --- Set Visibility ---
                Speed_Label.Visibility = isMainSpeedVisible ? Visibility.Visible : Visibility.Hidden;
                Downloads_Control.Speed_Label.Visibility = isDownloadSpeedVisible ? Visibility.Visible : Visibility.Hidden;

                // --- Clear Text ---
                Speed_Label.Text = "";
                Downloads_Control.Speed_Label.Text = "";
                Main_Window.TimeLeft_Label.Text = "";
            });
        }
    }
}