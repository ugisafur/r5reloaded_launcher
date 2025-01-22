using System;
using System.Collections.Generic;
using System.IO;

using ZstdSharp;
using static launcher.ControlReferences;
using static launcher.Logger;

namespace launcher
{
    /// <summary>
    /// This file contains the DecompressionManager class, which is responsible for managing the decompression of files
    /// in the launcher application. The class provides methods to prepare and execute decompression tasks for a list of
    /// downloaded files. It utilizes the ZstdSharp library for decompression and updates the UI to reflect the progress
    /// of the decompression process.
    ///
    /// Key functionalities include:
    /// - PrepareTasks: Prepares a list of decompression tasks for the provided list of downloaded file paths.
    /// - DecompressFileAsync: Asynchronously decompresses a single file and updates the progress bar and label in the UI.
    ///
    /// The class interacts with global variables and UI elements to keep track of the number of files left to decompress
    /// and to update the progress bar accordingly.
    /// </summary>
    public static class DecompressionManager
    {
        public static List<Task> PrepareTasks(List<Task<string>> allTasks)
        {
            List<Task> decompressionTasks = [];

            AppState.FilesLeft = allTasks.Count;

            appDispatcher.Invoke(() =>
            {
                Progress_Bar.Maximum = allTasks.Count;
                Progress_Bar.Value = 0;
            });

            foreach (var downloadTask in allTasks)
            {
                string compressedFilePath = downloadTask.Result;
                if (string.IsNullOrEmpty(compressedFilePath))
                    continue;

                string decompressedFilePath = compressedFilePath.Replace(".zst", "");
                decompressionTasks.Add(DecompressFileAsync(compressedFilePath, decompressedFilePath));
            }

            return decompressionTasks;
        }

        public static async Task DecompressFileAsync(string compressedFilePath, string decompressedFilePath)
        {
            DownloadItem downloadItem = await AddDownloadItemAsync(Path.GetFileName(compressedFilePath));

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(decompressedFilePath));

                // Get the total size of the compressed file
                long totalBytes = new FileInfo(compressedFilePath).Length;
                long processedBytes = 0;
                DateTime lastUpdate = DateTime.Now;

                using var input = File.OpenRead(compressedFilePath);
                using var output = File.OpenWrite(decompressedFilePath);
                using var decompressionStream = new DecompressionStream(input);

                // Wrap the output stream with a progress handler
                byte[] buffer = new byte[8192]; // 8KB buffer size
                int bytesRead;
                while ((bytesRead = await decompressionStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await output.WriteAsync(buffer, 0, bytesRead);
                    processedBytes += bytesRead;

                    if ((DateTime.Now - lastUpdate).TotalMilliseconds > 200)
                    {
                        lastUpdate = DateTime.Now;

                        double totalSize = totalBytes >= (1024L * 1024 * 1024) ? totalBytes / (1024.0 * 1024 * 1024) : totalBytes / (1024.0 * 1024.0);
                        string totalText = totalBytes >= (1024L * 1024 * 1024) ? $"{totalSize:F2} GB" : $"{totalSize:F2} MB";

                        double downloadedSize = processedBytes >= (1024L * 1024 * 1024) ? processedBytes / (1024.0 * 1024 * 1024) : processedBytes / (1024.0 * 1024.0);
                        string downloadedText = processedBytes >= (1024L * 1024 * 1024) ? $"{downloadedSize:F2} GB" : $"{downloadedSize:F2} MB";

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
            catch (Exception ex)
            {
                LogError(Source.Decompression, $"Failed to decompress {compressedFilePath}: {ex.Message}");
            }
            finally
            {
                if (File.Exists(compressedFilePath))
                    File.Delete(compressedFilePath);

                await appDispatcher.InvokeAsync(() =>
                {
                    Progress_Bar.Value++;
                    Files_Label.Text = $"{--AppState.FilesLeft} files left";
                });

                await RemoveDownloadItemAsync(downloadItem);
            }
        }

        private static async Task RemoveDownloadItemAsync(DownloadItem downloadItem)
        {
            if (downloadItem != null)
            {
                await appDispatcher.InvokeAsync(() => Downloads_Control.RemoveDownloadItem(downloadItem));
            }
        }

        private static async Task<DownloadItem> AddDownloadItemAsync(string fileName)
        {
            return await appDispatcher.InvokeAsync(() => Downloads_Control.AddDownloadItem(fileName));
        }
    }
}