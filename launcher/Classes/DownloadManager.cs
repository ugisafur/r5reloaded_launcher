using Octodiff.Core;
using Octodiff.Diagnostics;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using static launcher.ControlReferences;
using static launcher.Logger;
using System.Windows;
using System.Net.Http;

namespace launcher
{
    /// <summary>
    /// Manages file downloads within the launcher application, providing functionalities such as
    /// concurrent downloads, retry policies, speed throttling, and UI updates.
    /// </summary>
    public static class DownloadManager
    {
        private static long _downloadSpeedLimit = ThrottledStream.Infinite;
        private static SemaphoreSlim _downloadSemaphore;

        /// <summary>
        /// Configures the maximum number of concurrent downloads based on configuration settings.
        /// </summary>
        public static void ConfigureConcurrency()
        {
            int maxConcurrentDownloads = (int)Ini.Get(Ini.Vars.Concurrent_Downloads);
            _downloadSemaphore = new SemaphoreSlim(maxConcurrentDownloads);
        }

        /// <summary>
        /// Sets the download speed limit based on configuration settings.
        /// </summary>
        public static void ConfigureDownloadSpeed()
        {
            int speedLimitKb = (int)Ini.Get(Ini.Vars.Download_Speed_Limit);
            _downloadSpeedLimit = speedLimitKb > 0 ? speedLimitKb * 1024 : ThrottledStream.Infinite;
        }

        /// <summary>
        /// Initializes and starts download tasks for the base game files.
        /// </summary>
        /// <param name="baseGameFiles">The base game files to download.</param>
        /// <param name="branchDirectory">The directory where files will be downloaded.</param>
        /// <returns>A list of download tasks.</returns>
        public static List<Task<string>> InitializeDownloadTasks(GameFiles gameFiles, string branchDirectory)
        {
            if (gameFiles == null) throw new ArgumentNullException(nameof(gameFiles));
            if (string.IsNullOrWhiteSpace(branchDirectory)) throw new ArgumentException("Branch directory cannot be null or empty.", nameof(branchDirectory));

            var downloadTasks = new List<Task<string>>(gameFiles.files.Count);
            ConfigureProgress(gameFiles.files.Count);

            string baseUrl = Utilities.GetCurrentBranch().game_url;

            foreach (var file in gameFiles.files)
            {
                string fileUrl = $"{baseUrl}/{file.name}";
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

            return downloadTasks;
        }

        /// <summary>
        /// Initializes and starts download tasks for repairing bad files.
        /// </summary>
        /// <param name="branchDirectory">The directory where files will be downloaded.</param>
        /// <returns>A list of download tasks.</returns>
        public static List<Task<string>> InitializeRepairTasks(string branchDirectory)
        {
            if (string.IsNullOrWhiteSpace(branchDirectory)) throw new ArgumentException("Temporary directory cannot be null or empty.", nameof(branchDirectory));

            int badFilesCount = DataCollections.BadFiles.Count;
            ConfigureProgress(badFilesCount);

            var downloadTasks = new List<Task<string>>(badFilesCount);
            string baseUrl = Utilities.GetCurrentBranch().game_url;

            foreach (var file in DataCollections.BadFiles)
            {
                string fileUrl = $"{baseUrl}/{file}";
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

            return downloadTasks;
        }

        /// <summary>
        /// Downloads a file with optional checksum verification and updates the UI accordingly.
        /// </summary>
        /// <param name="fileUrl">The URL of the file to download.</param>
        /// <param name="destinationPath">The local path where the file will be saved.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="checksum">Optional checksum for file verification.</param>
        /// <param name="checkForExistingFiles">Whether to check for existing files before downloading.</param>
        /// <returns>The path to the downloaded file, or an empty string if the download failed.</returns>
        private static async Task<string> DownloadFileAsync(string fileUrl, string destinationPath, string fileName, string checksum = "", bool checkForExistingFiles = false)
        {
            await _downloadSemaphore.WaitAsync();

            DownloadItem downloadItem = await AddDownloadItemAsync(fileName);

            try
            {
                if (checkForExistingFiles && !string.IsNullOrWhiteSpace(checksum) && ShouldSkipDownload(destinationPath, checksum))
                    return destinationPath;

                await CreateRetryPolicy(fileUrl).ExecuteAsync(async () =>
                {
                    await DownloadWithThrottlingAsync(fileUrl, destinationPath, downloadItem);
                });

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
                appDispatcher.Invoke(() =>
                {
                    Progress_Bar.Value++;
                    Files_Label.Text = $"{--AppState.FilesLeft} files left";
                });

                await RemoveDownloadItemAsync(downloadItem);
                _downloadSemaphore.Release();
            }
        }

        /// <summary>
        /// Determines whether a file should be skipped based on its existence and checksum.
        /// </summary>
        /// <param name="destinationPath">The path to the destination file.</param>
        /// <param name="expectedChecksum">The expected checksum of the file.</param>
        /// <returns>True if the file exists and matches the checksum; otherwise, false.</returns>
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

        /// <summary>
        /// Ensures that the directory for the specified file path exists.
        /// </summary>
        /// <param name="filePath">The file path whose directory should be checked.</param>
        private static void EnsureDirectoryExists(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        /// <summary>
        /// Configures the progress bar and related UI elements based on the total number of files.
        /// </summary>
        /// <param name="totalFiles">The total number of files to process.</param>
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

        /// <summary>
        /// Creates a retry policy using Polly for handling transient download errors.
        /// </summary>
        /// <param name="fileUrl">The URL of the file being downloaded.</param>
        /// <returns>An asynchronous retry policy.</returns>
        private static AsyncRetryPolicy CreateRetryPolicy(string fileUrl)
        {
            const int maxRetryAttempts = 30;
            const double exponentialBackoffFactor = 2.0;

            return Policy
                .Handle<WebException>()
                .Or<TimeoutException>()
                .WaitAndRetryAsync(
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

        /// <summary>
        /// Adds a download item to the UI.
        /// </summary>
        /// <param name="fileName">The name of the file being downloaded.</param>
        /// <returns>The added <see cref="DownloadItem"/>.</returns>
        private static async Task<DownloadItem> AddDownloadItemAsync(string fileName)
        {
            return await appDispatcher.InvokeAsync(() => Downloads_Control.AddDownloadItem(fileName));
        }

        /// <summary>
        /// Removes a download item from the UI.
        /// </summary>
        /// <param name="downloadItem">The download item to remove.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private static async Task RemoveDownloadItemAsync(DownloadItem downloadItem)
        {
            if (downloadItem != null)
            {
                await appDispatcher.InvokeAsync(() => Downloads_Control.RemoveDownloadItem(downloadItem));
            }
        }

        /// <summary>
        /// Downloads a file with speed throttling and updates the UI with download progress.
        /// </summary>
        /// <param name="fileUrl">The URL of the file to download.</param>
        /// <param name="destinationPath">The local path where the file will be saved.</param>
        /// <param name="downloadItem">The UI download item to update.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private static async Task DownloadWithThrottlingAsync(string fileUrl, string destinationPath, DownloadItem downloadItem)
        {
            using var response = await Networking.HttpClient.GetAsync(fileUrl, HttpCompletionOption.ResponseHeadersRead);

            if (response.StatusCode != HttpStatusCode.OK)
                throw new WebException($"Failed to download: {response.StatusCode}");

            long totalBytes = response.Content.Headers.ContentLength ?? -1;
            long downloadedBytes = 0;
            long lastDownloadedBytes = 0; // Tracks bytes downloaded in the last interval
            DateTime lastUpdate = DateTime.Now;
            DateTime timeoutlastUpdate = DateTime.Now;
            TimeSpan timeoutThreshold = TimeSpan.FromSeconds(30);

            using var responseStream = await response.Content.ReadAsStreamAsync();
            using var throttledStream = new ThrottledStream(responseStream, _downloadSpeedLimit);
            using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);

            byte[] buffer = new byte[64 * 1024]; // 64KB buffer
            int bytesRead;

            while ((bytesRead = await throttledStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead);
                downloadedBytes += bytesRead;

                // Update the lastUpdate time if new data is downloaded
                if (downloadedBytes > lastDownloadedBytes)
                {
                    lastDownloadedBytes = downloadedBytes;
                    timeoutlastUpdate = DateTime.Now;
                }

                // Check for timeout
                if ((DateTime.Now - timeoutlastUpdate) > timeoutThreshold)
                {
                    throw new TimeoutException($"Download stalled for {timeoutThreshold.TotalSeconds} seconds. Retrying...");
                }

                if ((DateTime.Now - lastUpdate).TotalMilliseconds > 200)
                {
                    lastUpdate = DateTime.Now;
                    if (downloadItem != null && totalBytes > 0)
                    {
                        double totalSize = totalBytes >= (1024L * 1024 * 1024) ? totalBytes / (1024.0 * 1024 * 1024) : totalBytes / (1024.0 * 1024.0);
                        string totalText = totalBytes >= (1024L * 1024 * 1024) ? $"{totalSize:F2} GB" : $"{totalSize:F2} MB";

                        double downloadedSize = downloadedBytes >= (1024L * 1024 * 1024) ? downloadedBytes / (1024.0 * 1024 * 1024) : downloadedBytes / (1024.0 * 1024.0);
                        string downloadedText = downloadedBytes >= (1024L * 1024 * 1024) ? $"{downloadedSize:F2} GB" : $"{downloadedSize:F2} MB";

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

        public static void SetInstallState(bool installing, string buttonText = "PLAY")
        {
            LogInfo(Source.Launcher, $"Setting install state to: {installing}");

            appDispatcher.Invoke(() =>
            {
                AppState.IsInstalling = installing;

                Play_Button.Content = buttonText;
                Branch_Combobox.IsEnabled = !installing;
                Play_Button.IsEnabled = !installing;
                Status_Label.Text = "";
                Files_Label.Text = "";

                GameSettings_Control.RepairGame_Button.IsEnabled = !installing && Utilities.IsBranchInstalled();
                GameSettings_Control.UninstallGame_Button.IsEnabled = !installing && Utilities.IsBranchInstalled();
                GameSettings_Control.OpenDir_Button.IsEnabled = !installing && Utilities.IsBranchInstalled();
                GameSettings_Control.AdvancedMenu_Button.IsEnabled = !installing && Utilities.IsBranchInstalled();
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

                GameSettings_Control.RepairGame_Button.IsEnabled = !installing && Utilities.IsBranchInstalled();
                GameSettings_Control.UninstallGame_Button.IsEnabled = !installing && Utilities.IsBranchInstalled();
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
            });
        }
    }
}