using Octodiff.Core;
using Octodiff.Diagnostics;
using Polly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

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
        public static void SetSemaphoreLimit(int limit)
        {
            Global.downloadSemaphore = new SemaphoreSlim(limit);
        }

        public static async Task<string> DownloadAndReturnFilePathAsync(string fileUrl, string destinationPath, string fileName, string checksum = "", bool checkForExistingFiles = false)  // 1MB per second
        {
            long maxDownloadSpeedBytesPerSecond = ThrottledStream.Infinite; //Todo: Set appropriate download speed limit

            var cancellationSource = new CancellationTokenSource();
            var cancellationToken = cancellationSource.Token;

            DownloadItem downloadItem = null;
            long downloadedBytes = 0;
            long totalBytes = -1;
            DateTime lastUpdate = DateTime.Now;

            // Wait for an available semaphore slot
            await Global.downloadSemaphore.WaitAsync();

            try
            {
                // Check if file exists and checksum matches
                if (checkForExistingFiles && !string.IsNullOrEmpty(checksum) && ShouldSkipFileDownload(destinationPath, checksum))
                {
                    await ControlReferences.dispatcher.InvokeAsync(() =>
                    {
                        ControlReferences.progressBar.Value++;
                        ControlReferences.lblFilesLeft.Text = $"{--Global.filesLeft} files left";
                    });

                    return destinationPath;
                }

                // Add download item to the popup
                await ControlReferences.dispatcher.InvokeAsync(() =>
                {
                    downloadItem = ControlReferences.downloadsPopupControl.AddDownloadItem(fileName);
                });

                var retryPolicy = Policy
                .Handle<Exception>(ex => ex is WebException || ex is TimeoutException)
                .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

                await retryPolicy.ExecuteAsync(async () =>
                {
                    var request = (HttpWebRequest)WebRequest.Create(fileUrl);
                    request.Method = "GET";
                    request.Timeout = 30000; // Set appropriate timeout
                    request.AllowAutoRedirect = true;

                    using (var response = (HttpWebResponse)await request.GetResponseAsync())
                    {
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

                            // Update progress in the popup
                            if ((DateTime.Now - lastUpdate).TotalMilliseconds > 200)
                            {
                                lastUpdate = DateTime.Now;

                                if (downloadItem != null && totalBytes > 0)
                                {
                                    var progress = (double)downloadedBytes / totalBytes * 100;
                                    await ControlReferences.dispatcher.InvokeAsync(() =>
                                    {
                                        downloadItem.downloadFilePercent.Text = $"{progress:F2}%";
                                        downloadItem.downloadFileProgress.Value = progress;
                                    });
                                }
                            }
                        }

                        await fileStream.FlushAsync();
                    }
                });

                Utilities.Log($"Downloaded: {destinationPath}");

                // Update global progress
                await ControlReferences.dispatcher.InvokeAsync(() =>
                {
                    ControlReferences.progressBar.Value++;
                    ControlReferences.lblFilesLeft.Text = $"{--Global.filesLeft} files left";
                });

                return destinationPath;
            }
            catch (OperationCanceledException)
            {
                Utilities.Log($"Download cancelled for {fileUrl}");
                Global.badFilesDetected = true;
                return string.Empty;
            }
            catch (Exception ex)
            {
                Utilities.Log($"All retries failed for {fileUrl}: {ex.Message}");
                Global.badFilesDetected = true;
                return string.Empty;
            }
            finally
            {
                // Remove the download item from the popup
                if (downloadItem != null)
                {
                    await ControlReferences.dispatcher.InvokeAsync(() =>
                    {
                        ControlReferences.downloadsPopupControl.RemoveDownloadItem(downloadItem);
                    });
                }

                // Release the semaphore slot
                Global.downloadSemaphore.Release();
            }
        }

        public static List<Task<string>> PrepareDownloadTasks(BaseGameFiles baseGameFiles, string tempDirectory)
        {
            var downloadTasks = new List<Task<string>>();

            ControlReferences.dispatcher.Invoke(() =>
            {
                ControlReferences.progressBar.Maximum = baseGameFiles.files.Count;
                ControlReferences.progressBar.Value = 0;
            });

            Global.filesLeft = baseGameFiles.files.Count;

            foreach (var file in baseGameFiles.files)
            {
                string fileUrl = $"{Global.serverConfig.base_game_url}/{file.name}";
                string destinationPath = Path.Combine(tempDirectory, file.name);

                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

                downloadTasks.Add(DownloadAndReturnFilePathAsync(fileUrl, destinationPath, file.name, file.checksum, true));
            }

            return downloadTasks;
        }

        public static List<Task<string>> PrepareRepairDownloadTasks(string tempDirectory)
        {
            Global.filesLeft = Global.badFiles.Count;

            var downloadTasks = new List<Task<string>>();

            ControlReferences.dispatcher.Invoke(() =>
            {
                ControlReferences.progressBar.Maximum = Global.badFiles.Count;
                ControlReferences.progressBar.Value = 0;
            });

            Global.filesLeft = Global.badFiles.Count;

            foreach (var file in Global.badFiles)
            {
                string fileUrl = $"{Global.serverConfig.base_game_url}/{file}";
                string destinationPath = Path.Combine(tempDirectory, file);

                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

                downloadTasks.Add(DownloadAndReturnFilePathAsync(fileUrl, destinationPath, file));
            }

            return downloadTasks;
        }

        public static List<Task<string>> PreparePatchDownloadTasks(GamePatch patchFiles, string tempDirectory)
        {
            var downloadTasks = new List<Task<string>>();

            ControlReferences.dispatcher.Invoke(() =>
            {
                ControlReferences.progressBar.Maximum = patchFiles.files.Count;
                ControlReferences.progressBar.Value = 0;
            });

            Global.filesLeft = patchFiles.files.Count;

            int selectedBranchIndex = Utilities.GetCmbBranchIndex();

            foreach (var file in patchFiles.files)
            {
                if (file.Action.ToLower() == "delete")
                    continue;

                string fileUrl = $"{Global.serverConfig.branches[selectedBranchIndex].patch_url}/{file.Name}";
                string destinationPath = Path.Combine(tempDirectory, file.Name);

                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

                downloadTasks.Add(DownloadAndReturnFilePathAsync(fileUrl, destinationPath, file.Name));
            }

            return downloadTasks;
        }

        public static List<Task> PrepareFilePatchTasks(GamePatch patchFiles, string tempDirectory)
        {
            var tasks = new List<Task>();
            Global.filesLeft = patchFiles.files.Count;

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

                    // Update UI thread-safe
                    ControlReferences.dispatcher.Invoke(() =>
                    {
                        ControlReferences.progressBar.Value++;
                        ControlReferences.lblFilesLeft.Text = $"{--Global.filesLeft} files left";
                    });
                }));
            }

            return tasks;
        }

        private static void Delete(string file)
        {
            string fullPath = Path.Combine(Global.launcherPath, file);
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }

        private static async void Update(string file, string tempDirectory)
        {
            string sourceCompressedFile = Path.Combine(tempDirectory, file);
            string destinationFile = Path.Combine(Global.launcherPath, file.Replace(".zst", ""));
            await DecompressionManager.DecompressFileAsync(sourceCompressedFile, destinationFile);
        }

        private static async void Patch(string file, string tempDirectory)
        {
            string sourceCompressedDeltaFile = Path.Combine(tempDirectory, file);
            string sourceDecompressedDeltaFile = Path.Combine(tempDirectory, file.Replace(".zst", ""));
            string destinationFile = Path.Combine(Global.launcherPath, file.Replace(".delta.zst", ""));
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
                Utilities.Log($"Checking existing file: {destinationPath}");
                string checksum = FileManager.CalculateChecksum(destinationPath);
                if (checksum == expectedChecksum)
                {
                    ControlReferences.dispatcher.Invoke(() => { ControlReferences.progressBar.Value++; });
                    return true;
                }
            }
            return false;
        }
    }
}