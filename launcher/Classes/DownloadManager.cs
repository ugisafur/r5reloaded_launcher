using Octodiff.Core;
using Octodiff.Diagnostics;
using Polly;
using System.IO;
using System.Net;
using static launcher.Global;
using static launcher.ControlReferences;
using static launcher.Logger;

namespace launcher
{
    /// <summary>
    /// The DownloadManager class provides functionality for managing file downloads in a concurrent and controlled manner.
    /// It includes methods to set download limits, download files with retry policies, and update the UI with download progress.
    /// Additionally, it supports preparing download tasks for different scenarios such as base game files, repair tasks, and patch tasks.
    /// The class also includes methods for file operations like delete, update, and patch.
    /// </summary>
    public static class DownloadManager
    {
        private static long downloadSpeedLimit = ThrottledStream.Infinite;

        public static void SetSemaphoreLimit()
        {
            int limit = Ini.Get(Ini.Vars.Concurrent_Downloads, 1000);

            DOWNLOAD_SEMAPHORE = new SemaphoreSlim(limit);
        }

        public static void SetDownloadSpeedLimit()
        {
            int limit = Ini.Get(Ini.Vars.Download_Speed_Limit, 0);

            //Convert KB/s to B/s
            downloadSpeedLimit = limit * 1024;
        }

        public static async Task<string> DownloadAndReturnFilePathAsync(string fileUrl, string destinationPath, string fileName, string checksum = "", bool checkForExistingFiles = false)  // 1MB per second
        {
            long maxDownloadSpeedBytesPerSecond = downloadSpeedLimit; //Todo: Set appropriate download speed limit

            DownloadItem downloadItem = null;
            long downloadedBytes = 0;
            long totalBytes = -1;
            DateTime lastUpdate = DateTime.Now;

            // Wait for an available semaphore slot
            await DOWNLOAD_SEMAPHORE.WaitAsync();

            try
            {
                // Check if file exists and checksum matches
                if (checkForExistingFiles && !string.IsNullOrEmpty(checksum) && ShouldSkipFileDownload(destinationPath, checksum))
                {
                    UpdateProgressBar(--FILES_LEFT);

                    return destinationPath;
                }

                // Add download item to the popup
                downloadItem = CreateDownloadItem(fileName);
                var retryPolicy = CreatePolicy(fileUrl);

                await retryPolicy.ExecuteAsync(async () =>
                {
                    var request = (HttpWebRequest)WebRequest.Create(fileUrl);
                    request.Method = "GET";
                    request.Timeout = 30000; // Set appropriate timeout
                    request.AllowAutoRedirect = true;

                    using var response = (HttpWebResponse)await request.GetResponseAsync();
                    if (response.StatusCode != HttpStatusCode.OK)
                        throw new WebException($"Failed to download: {response.StatusCode}");

                    totalBytes = response.ContentLength;
                    downloadedBytes = 0L;

                    using var responseStream = response.GetResponseStream();
                    using var throttledStream = new ThrottledStream(responseStream, maxDownloadSpeedBytesPerSecond);  // Throttling applied here
                    using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);

                    var buffer = new byte[64 * 1024]; // 128KB buffer
                    int bytesRead;
                    var stopwatch = new System.Diagnostics.Stopwatch();
                    stopwatch.Start();

                    while ((bytesRead = await throttledStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        downloadedBytes += bytesRead;

                        if ((DateTime.Now - lastUpdate).TotalMilliseconds > 200)
                        {
                            lastUpdate = DateTime.Now;

                            if (downloadItem != null && totalBytes > 0)
                            {
                                var progress = (double)downloadedBytes / totalBytes * 100;
                                UpdateDownloadsItem(downloadItem, progress);
                            }
                        }
                    }

                    await fileStream.FlushAsync();
                });

                UpdateProgressBar(--FILES_LEFT);

                return destinationPath;
            }
            catch (Exception ex)
            {
                Log(Logger.Type.Error, Source.DownloadManager, $"All retries failed for {fileUrl}: {ex.Message}");
                BAD_FILES_DETECTED = true;
                return string.Empty;
            }
            finally
            {
                // Remove the download item from the popup
                if (downloadItem != null)
                    RemoveDownloadsItem(downloadItem);

                // Release the semaphore slot
                DOWNLOAD_SEMAPHORE.Release();
            }
        }

        private static Polly.Retry.AsyncRetryPolicy CreatePolicy(string fileUrl)
        {
            var retryPolicy = Policy
                .Handle<Exception>(ex => ex is WebException || ex is TimeoutException)
                .WaitAndRetryAsync(5,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        // Log each retry attempt
                        Log(Logger.Type.Warning, Source.DownloadManager,
                            $"Retry #{retryCount} for {fileUrl} due to: {exception.Message}. Waiting {timeSpan.TotalSeconds:F2}s before next attempt.");
                    });

            return retryPolicy;
        }

        private static DownloadItem CreateDownloadItem(string fileName)
        {
            DownloadItem newItem = null;

            appDispatcher.InvokeAsync(() =>
            {
                newItem = downloadsPopupControl.AddDownloadItem(fileName);
            });

            return newItem;
        }

        private static async void UpdateDownloadsItem(DownloadItem downloadItem, double progress)
        {
            await appDispatcher.InvokeAsync(() =>
            {
                downloadItem.downloadFilePercent.Text = $"{progress:F2}%";
                downloadItem.downloadFileProgress.Value = progress;
            });
        }

        private static async void RemoveDownloadsItem(DownloadItem downloadItem)
        {
            await appDispatcher.InvokeAsync(() =>
            {
                downloadsPopupControl.RemoveDownloadItem(downloadItem);
            });
        }

        public static List<Task<string>> PrepareDownloadTasks(BaseGameFiles baseGameFiles, string tempDirectory)
        {
            var downloadTasks = new List<Task<string>>();

            SetProgressBar(baseGameFiles.files.Count);

            FILES_LEFT = baseGameFiles.files.Count;

            foreach (var file in baseGameFiles.files)
            {
                string fileUrl = $"{SERVER_CONFIG.base_game_url}/{file.name}";
                string destinationPath = Path.Combine(tempDirectory, file.name);

                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

                downloadTasks.Add(DownloadAndReturnFilePathAsync(fileUrl, destinationPath, file.name, file.checksum, true));
            }

            return downloadTasks;
        }

        public static List<Task<string>> PrepareRepairDownloadTasks(string tempDirectory)
        {
            FILES_LEFT = BAD_FILES.Count;

            var downloadTasks = new List<Task<string>>();

            SetProgressBar(BAD_FILES.Count);

            FILES_LEFT = BAD_FILES.Count;

            foreach (var file in BAD_FILES)
            {
                string fileUrl = $"{SERVER_CONFIG.base_game_url}/{file}";
                string destinationPath = Path.Combine(tempDirectory, file);

                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

                downloadTasks.Add(DownloadAndReturnFilePathAsync(fileUrl, destinationPath, file));
            }

            return downloadTasks;
        }

        public static List<Task<string>> PreparePatchDownloadTasks(GamePatch patchFiles, string tempDirectory)
        {
            var downloadTasks = new List<Task<string>>();

            SetProgressBar(patchFiles.files.Count);

            FILES_LEFT = patchFiles.files.Count;

            int selectedBranchIndex = Utilities.GetCmbBranchIndex();

            foreach (var file in patchFiles.files)
            {
                if (file.Action.Equals("delete", StringComparison.CurrentCultureIgnoreCase))
                    continue;

                string fileUrl = $"{SERVER_CONFIG.branches[selectedBranchIndex].patch_url}/{file.Name}";
                string destinationPath = Path.Combine(tempDirectory, file.Name);

                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

                downloadTasks.Add(DownloadAndReturnFilePathAsync(fileUrl, destinationPath, file.Name));
            }

            return downloadTasks;
        }

        public static List<Task> PrepareFilePatchTasks(GamePatch patchFiles, string tempDirectory)
        {
            var tasks = new List<Task>();
            FILES_LEFT = patchFiles.files.Count;

            foreach (var file in patchFiles.files)
            {
                tasks.Add(Task.Run(() =>
                {
                    switch (file.Action.ToLower())
                    {
                        case "delete":
                            Delete(file.Name);
                            break;

                        case "update":
                            Update(file.Name, tempDirectory);
                            break;

                        case "patch":
                            Patch(file.Name, tempDirectory);
                            break;
                    }

                    UpdateProgressBar(--FILES_LEFT);
                }));
            }

            return tasks;
        }

        private static void Delete(string file)
        {
            string fullPath = Path.Combine(LAUNCHER_PATH, file);
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }

        private static async void Update(string file, string tempDirectory)
        {
            string sourceCompressedFile = Path.Combine(tempDirectory, file);
            string destinationFile = Path.Combine(LAUNCHER_PATH, file.Replace(".zst", ""));
            await DecompressionManager.DecompressFileAsync(sourceCompressedFile, destinationFile);
        }

        private static async void Patch(string file, string tempDirectory)
        {
            string sourceCompressedDeltaFile = Path.Combine(tempDirectory, file);
            string sourceDecompressedDeltaFile = Path.Combine(tempDirectory, file.Replace(".zst", ""));
            string destinationFile = Path.Combine(LAUNCHER_PATH, file.Replace(".delta.zst", ""));
            await DecompressionManager.DecompressFileAsync(sourceCompressedDeltaFile, sourceDecompressedDeltaFile);
            PatchFile(destinationFile, sourceDecompressedDeltaFile);
        }

        private static void PatchFile(string originalFile, string deltaFile)
        {
            var signatureFile = Path.GetTempFileName();

            var signatureBuilder = new SignatureBuilder();
            using (var basisStream = new FileStream(originalFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var signatureStream = new FileStream(signatureFile, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                signatureBuilder.Build(basisStream, new SignatureWriter(signatureStream));
            }

            if (File.Exists(originalFile))
                File.Delete(originalFile);

            var deltaApplier = new DeltaApplier { SkipHashCheck = false };
            using (var basisStream = new FileStream(signatureFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var deltaStream = new FileStream(deltaFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var newFileStream = new FileStream(originalFile, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
            {
                deltaApplier.Apply(basisStream, new BinaryDeltaReader(deltaStream, new ConsoleProgressReporter()), newFileStream);
            }
        }

        private static bool ShouldSkipFileDownload(string destinationPath, string expectedChecksum)
        {
            if (File.Exists(destinationPath))
            {
                //Log(Logger.Type.Info, Source.DownloadManager, $"Checking existing file: {destinationPath}");
                string checksum = FileManager.CalculateChecksum(destinationPath);
                if (checksum == expectedChecksum)
                {
                    UpdateProgressBar();
                    return true;
                }
            }
            return false;
        }

        private static void SetProgressBar(int max)
        {
            appDispatcher.Invoke(() =>
            {
                progressBar.Maximum = max;
                progressBar.Value = 0;
            });
        }

        private static void UpdateProgressBar(int filesLeft = -1)
        {
            appDispatcher.Invoke(() =>
            {
                progressBar.Value++;

                if (filesLeft != -1)
                    lblFilesLeft.Text = $"{filesLeft} files left";
            });
        }
    }
}