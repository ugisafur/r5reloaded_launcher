using System;
using System.Collections.Generic;
using System.IO;
using static launcher.Global;
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

            FILES_LEFT = allTasks.Count;

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
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(decompressedFilePath));

                using var input = File.OpenRead(compressedFilePath);
                using var output = File.OpenWrite(decompressedFilePath);
                using var decompressionStream = new DecompressionStream(input);

                await decompressionStream.CopyToAsync(output);

                await appDispatcher.InvokeAsync(() =>
                {
                    Progress_Bar.Value++;
                    Files_Label.Text = $"{--FILES_LEFT} files left";
                });

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
            }
        }
    }
}