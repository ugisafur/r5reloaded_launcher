using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using ZstdSharp;

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
            var decompressionTasks = new List<Task>();

            Global.filesLeft = allTasks.Count;

            ControlReferences.dispatcher.Invoke(() =>
            {
                ControlReferences.progressBar.Maximum = allTasks.Count;
                ControlReferences.progressBar.Value = 0;
            });

            foreach (var downloadTask in allTasks)
            {
                string compressedFilePath = downloadTask.Result;
                if (string.IsNullOrEmpty(compressedFilePath))
                {
                    continue;
                }

                string decompressedFilePath = compressedFilePath.Replace("\\temp\\", "\\").Replace(".zst", "");
                decompressionTasks.Add(DecompressFileAsync(compressedFilePath, decompressedFilePath));
            }

            return decompressionTasks;
        }

        public static async Task DecompressFileAsync(string compressedFilePath, string decompressedFilePath)
        {
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(decompressedFilePath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(decompressedFilePath));

                using var input = File.OpenRead(compressedFilePath);
                using var output = File.OpenWrite(decompressedFilePath);
                using var decompressionStream = new DecompressionStream(input);

                await decompressionStream.CopyToAsync(output);

                await ControlReferences.dispatcher.InvokeAsync(() =>
                {
                    ControlReferences.progressBar.Value++;
                    ControlReferences.lblFilesLeft.Text = $"{--Global.filesLeft} files left";
                });

                Utilities.Log($"Decompressed: {compressedFilePath} to {decompressedFilePath}");
            }
            catch (Exception ex)
            {
                Utilities.Log($"Failed to decompress {compressedFilePath}: {ex.Message}");
            }
        }
    }
}