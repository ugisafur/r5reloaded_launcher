using launcher.Global;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using static launcher.Global.Logger;
using static launcher.Global.References;
using static launcher.Network.DownloadTracker;

namespace launcher.Game
{
    public static class Checksums
    {
        public static async Task<int> IdentifyBadFiles(GameFiles gameFiles, Task<FileChecksum[]> checksumTasks, string branchDirectory, bool isUpdate = false)
        {
            var fileChecksums = await checksumTasks;
            var checksumDict = fileChecksums.ToDictionary(fc => fc.name, fc => fc.checksum);

            InitializeProgressBar(gameFiles.files.Count);

            DataCollections.BadFiles.Clear();

            foreach (var file in gameFiles.files)
            {
                string filePath = Path.Combine(branchDirectory, file.path);

                if (!File.Exists(filePath) || !checksumDict.TryGetValue(file.path, out var calculatedChecksum) || file.checksum != calculatedChecksum)
                {
                    LogWarning(isUpdate ? LogSource.Update : LogSource.Repair, isUpdate ? $"Updated file found: {file.path}" : $"Bad file found: {file.path}");

                    GameFile gameFile = new GameFile
                    {
                        path = $"{file.path}",
                        checksum = file.checksum,
                        size = file.size,
                        optional = file.optional,
                        parts = file.parts
                    };

                    DataCollections.BadFiles.Add(gameFile);
                }
                UpdateProgress();
            }

            return DataCollections.BadFiles.Count;
        }

        public static async Task<List<Task<FileChecksum>>> PrepareLangChecksumTasksAsync(string branchFolder)
        {
            GameFiles languageManifest = await Fetch.LanguageFiles();

            var filePaths = languageManifest.languages
                .Select(lang => new
                {
                    path1 = Path.Combine(branchFolder, "audio", "ship", $"general_{lang.ToLower(CultureInfo.InvariantCulture)}.mstr"),
                    path2 = Path.Combine(branchFolder, "audio", "ship", $"general_{lang.ToLower(CultureInfo.InvariantCulture)}_patch_1.mstr")
                })
                .Where(p => File.Exists(p.path1) && File.Exists(p.path2))
                .SelectMany(p => new[] { p.path1, p.path2 })
                .ToList();

            return PrepareChecksumTasksForFiles(filePaths, branchFolder);
        }

        public static List<Task<FileChecksum>> PrepareBranchChecksumTasks(string branchFolder)
        {
            var excludedPaths = new[] { "platform\\cfg\\user", "platform\\screenshots", "platform\\logs" };
            var allFiles = Directory.GetFiles(branchFolder, "*", SearchOption.AllDirectories)
                .Where(f => !f.Contains("opt.starpak", StringComparison.OrdinalIgnoreCase) &&
                            !excludedPaths.Any(p => f.Contains(p, StringComparison.OrdinalIgnoreCase)));

            return PrepareChecksumTasksForFiles(allFiles, branchFolder);
        }

        public static List<Task<FileChecksum>> PrepareOptChecksumTasks(string branchFolder)
        {
            var allFiles = Directory.GetFiles(branchFolder, "*", SearchOption.AllDirectories)
                .Where(f => f.Contains("opt.starpak", StringComparison.OrdinalIgnoreCase));

            return PrepareChecksumTasksForFiles(allFiles, branchFolder);
        }

        private static List<Task<FileChecksum>> PrepareChecksumTasksForFiles(IEnumerable<string> files, string branchFolder)
        {
            var fileList = files.ToList();
            InitializeProgressBar(fileList.Count);
            return fileList.Select(file => GenerateAndReturnFileChecksumAsync(file, branchFolder)).ToList();
        }

        public static async Task<FileChecksum> GenerateAndReturnFileChecksumAsync(string file, string branchFolder)
        {
            //await _downloadSemaphore.WaitAsync();

            var fileChecksum = new FileChecksum();
            try
            {
                fileChecksum.name = file.Replace(branchFolder + Path.DirectorySeparatorChar, "");
                fileChecksum.checksum = await CalculateChecksumAsync(file);

                UpdateProgress();

                return fileChecksum;
            }
            catch (Exception ex)
            {
                LogException($"Failed Generating Checksum For {file}", LogSource.Checksums, ex);
                return fileChecksum;
            }
            finally
            {
                //_downloadSemaphore.Release();
            }
        }

        public static async Task<string> CalculateChecksumAsync(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            using var sha256 = SHA256.Create();
            var hash = await sha256.ComputeHashAsync(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private static void InitializeProgressBar(int count)
        {
            appDispatcher.Invoke(() =>
            {
                Progress_Bar.Maximum = count;
                Progress_Bar.Value = 0;
                Percent_Label.Text = "0%";
            });
            AppState.FilesLeft = count;
        }

        private static void UpdateProgress()
        {
            appDispatcher.Invoke(() =>
            {
                if (Progress_Bar.Maximum > 0)
                {
                    Progress_Bar.Value++;
                    Percent_Label.Text = $"{(Progress_Bar.Value / Progress_Bar.Maximum * 100):F2}%";
                }
            });
        }
    }
}