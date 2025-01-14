using Octodiff.Core;
using Octodiff.Diagnostics;
using Polly;
using System.IO;
using System.Net;
using static launcher.Global;
using static launcher.ControlReferences;
using static launcher.Logger;
using Polly.Retry;

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
            if (!IS_INSTALLING)
                DOWNLOAD_SEMAPHORE = new SemaphoreSlim(Ini.Get(Ini.Vars.Concurrent_Downloads, 1000));
        }

        public static void SetDownloadSpeedLimit()
        {
            downloadSpeedLimit = Ini.Get(Ini.Vars.Download_Speed_Limit, 0) * 1024;
        }

        private static async Task<DownloadItem> AddDownloadItemAsync(string fileName)
        {
            return await appDispatcher.InvokeAsync(() => downloadsPopupControl.AddDownloadItem(fileName));
        }

        private static async Task RemoveDownloadItemAsync(DownloadItem downloadItem)
        {
            if (downloadItem != null)
                await appDispatcher.InvokeAsync(() => downloadsPopupControl.RemoveDownloadItem(downloadItem));
        }

        public static async Task<string> GetOrDownloadFileAsync(string fileUrl, string destinationPath, string fileName, string checksum = "", bool checkForExistingFiles = false)  // 1MB per second
        {
            // Wait for an available semaphore slot
            await DOWNLOAD_SEMAPHORE.WaitAsync();

            // Check if the file already exists and has the correct checksum
            if (checkForExistingFiles && !string.IsNullOrEmpty(checksum) && ShouldSkipFile(destinationPath, checksum))
            {
                UpdateProgressBar(--FILES_LEFT);
                return destinationPath;
            }

            // Add the download item to the popup
            DownloadItem downloadItem = await AddDownloadItemAsync(fileName);

            try
            {
                // Execute the download operation with retry policy
                await CreateRetryPolicy(fileUrl).ExecuteAsync(async () =>
                {
                    // Create a new HTTP request
                    var request = (HttpWebRequest)WebRequest.Create(fileUrl);
                    request.Method = "GET";
                    request.Timeout = 30000;
                    request.AllowAutoRedirect = true;

                    // Get the response from the server
                    using var response = (HttpWebResponse)await request.GetResponseAsync();

                    // Check if the response is OK
                    if (response.StatusCode != HttpStatusCode.OK)
                        throw new WebException($"Failed to download: {response.StatusCode}");

                    // Initialize variables for download progress tracking
                    DateTime lastUpdate = DateTime.Now;
                    long totalBytes = response.ContentLength;
                    long downloadedBytes = 0L;

                    // Open the response stream and create a throttled stream for download speed control
                    using var responseStream = response.GetResponseStream();
                    using var throttledStream = new ThrottledStream(responseStream, downloadSpeedLimit);  // Throttling applied here
                    using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);

                    // Initialize a buffer for reading data from the stream
                    var buffer = new byte[64 * 1024]; // 128KB buffer
                    int bytesRead;
                    var stopwatch = new System.Diagnostics.Stopwatch();
                    stopwatch.Start();

                    // Read data from the stream and write it to the file
                    while ((bytesRead = await throttledStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        // Write the data to the file stream
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        downloadedBytes += bytesRead;

                        // Update the download progress every 200 milliseconds, this is to prevent lagging the UI
                        if ((DateTime.Now - lastUpdate).TotalMilliseconds > 200)
                        {
                            lastUpdate = DateTime.Now;

                            if (downloadItem != null && totalBytes > 0)
                            {
                                var progress = (double)downloadedBytes / totalBytes * 100;
                                await appDispatcher.InvokeAsync(() =>
                                {
                                    downloadItem.downloadFilePercent.Text = $"{progress:F2}%";
                                    downloadItem.downloadFileProgress.Value = progress;
                                });
                            }
                        }
                    }

                    await fileStream.FlushAsync();
                });

                // Update the progress bar and return the destination path
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
                await RemoveDownloadItemAsync(downloadItem);

                // Release the semaphore slot
                DOWNLOAD_SEMAPHORE.Release();
            }
        }

        private static AsyncRetryPolicy CreateRetryPolicy(string fileUrl)
        {
            const int maxRetryAttempts = 5;
            const double exponentialBackoffFactor = 2.0;

            // Define the retry policy for handling specific exceptions
            AsyncRetryPolicy retryPolicy = Policy
                .Handle<WebException>()
                .Or<TimeoutException>()
                .WaitAndRetryAsync(
                    retryCount: maxRetryAttempts,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(exponentialBackoffFactor, retryAttempt)),
                    onRetry: (exception, timeSpan, retryNumber, context) =>
                    {
                        // Log each retry attempt with detailed information
                        Log(
                            Logger.Type.Warning,
                            Source.DownloadManager,
                            $"Retry #{retryNumber} for '{fileUrl}' due to: {exception.Message}. " +
                            $"Waiting {timeSpan.TotalSeconds:F2} seconds before next attempt."
                        );
                    });

            return retryPolicy;
        }

        public static List<Task<string>> InitializeDownloadTasks(BaseGameFiles baseGameFiles, string branchDirectory)
        {
            // Initialize the list to hold download tasks
            var downloadTasks = new List<Task<string>>(baseGameFiles.files.Count);

            // Set up progress indicators
            SetProgressBar(baseGameFiles.files.Count);
            FILES_LEFT = baseGameFiles.files.Count;

            // Retrieve the branch configuration once
            var currentBranch = SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()];
            string baseUrl = currentBranch.game_url;

            foreach (var file in baseGameFiles.files)
            {
                // Construct the full URL for the file
                string fileUrl = $"{baseUrl}/{file.name}";

                // Determine the destination path for the file
                string destinationPath = Path.Combine(branchDirectory, file.name);

                // Ensure the destination directory exists
                string destinationDir = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(destinationDir))
                {
                    Directory.CreateDirectory(destinationDir);
                }

                // Add the download task to the list
                downloadTasks.Add(
                    GetOrDownloadFileAsync(
                        fileUrl,
                        destinationPath,
                        file.name,
                        file.checksum,
                        true
                    )
                );
            }

            return downloadTasks;
        }

        public static List<Task<string>> InitializeRepairTasks(string tempDirectory)
        {
            // Initialize progress indicators
            int badFilesCount = BAD_FILES.Count;
            SetProgressBar(badFilesCount);
            FILES_LEFT = badFilesCount;

            // Initialize the list with a predefined capacity
            var downloadTasks = new List<Task<string>>(badFilesCount);

            // Retrieve the branch configuration once to avoid repeated lookups
            var currentBranch = SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()];
            string baseUrl = currentBranch.game_url;

            foreach (var file in BAD_FILES)
            {
                // Construct the full URL for the file
                string fileUrl = $"{baseUrl}/{file}";

                // Determine the destination path for the file
                string destinationPath = Path.Combine(tempDirectory, file);

                // Ensure the destination directory exists
                string destinationDir = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(destinationDir))
                {
                    Directory.CreateDirectory(destinationDir);
                }

                // Add the download task to the list
                downloadTasks.Add(
                    GetOrDownloadFileAsync(
                        fileUrl,
                        destinationPath,
                        file
                    )
                );
            }

            return downloadTasks;
        }

        public static List<Task<string>> InitializeUpdateTasks(GamePatch patchFiles, string tempDirectory)
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

                downloadTasks.Add(GetOrDownloadFileAsync(fileUrl, destinationPath, file.Name));
            }

            return downloadTasks;
        }

        public static List<Task> InitializeFileUpdateTasks(GamePatch patchFiles, string tempDirectory)
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
                            DeleteFile(file.Name);
                            break;

                        case "update":
                            ReplaceFile(file.Name, tempDirectory);
                            break;

                        case "patch":
                            PatchFile(file.Name, tempDirectory);
                            break;
                    }

                    UpdateProgressBar(--FILES_LEFT);
                }));
            }

            return tasks;
        }

        private static void DeleteFile(string file)
        {
            string fullPath = Path.Combine(LAUNCHER_PATH, file);
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }

        private static async void ReplaceFile(string file, string tempDirectory)
        {
            string sourceCompressedFile = Path.Combine(tempDirectory, file);
            string destinationFile = Path.Combine(LAUNCHER_PATH, file.Replace(".zst", ""));
            await DecompressionManager.DecompressFileAsync(sourceCompressedFile, destinationFile);
        }

        private static async void PatchFile(string file, string tempDirectory)
        {
            string sourceCompressedDeltaFile = Path.Combine(tempDirectory, file);
            string deltaFile = Path.Combine(tempDirectory, file.Replace(".zst", ""));
            string originalFile = Path.Combine(LAUNCHER_PATH, file.Replace(".delta.zst", ""));

            await DecompressionManager.DecompressFileAsync(sourceCompressedDeltaFile, deltaFile);

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

        private static bool ShouldSkipFile(string destinationPath, string expectedChecksum)
        {
            if (File.Exists(destinationPath))
            {
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