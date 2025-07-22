using launcher.Global;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Windows;
using static launcher.Global.Logger;
using static launcher.Global.References;
using static launcher.Network.DownloadSpeedTracker;

namespace launcher.Game
{
    public static class Checksums
    {
        public static int IdentifyBadFiles(GameFiles gameFiles, List<Task<FileChecksum>> checksumTasks, string branchDirectory, bool isUpdate = false)
        {
            var fileChecksums = Task.WhenAll(checksumTasks).Result;
            var checksumDict = fileChecksums.ToDictionary(fc => fc.name, fc => fc.checksum);

            appDispatcher.Invoke(() =>
            {
                Progress_Bar.Maximum = gameFiles.files.Count;
                Progress_Bar.Value = 0;
            });

            AppState.FilesLeft = gameFiles.files.Count;
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

                appDispatcher.Invoke(() =>
                {
                    Progress_Bar.Value++;
                    //Files_Label.Text = $"{--AppState.FilesLeft} files left";
                    Percent_Label.Text = $"{(Progress_Bar.Value / Progress_Bar.Maximum * 100):F2}%";
                });
            }

            return DataCollections.BadFiles.Count;
        }

        public static List<Task<FileChecksum>> PrepareLangChecksumTasks(string branchFolder)
        {
            GameFiles languageManifest = Fetch.LanguageFiles().Result;

            var existingFilePaths = new List<string>();
            foreach (var lang in languageManifest.languages)
            {
                string langLower = lang.ToLower(CultureInfo.InvariantCulture);
                string path1 = Path.Combine(branchFolder, "audio", "ship", $"general_{langLower}.mstr");
                string path2 = Path.Combine(branchFolder, "audio", "ship", $"general_{langLower}_patch_1.mstr");

                if (File.Exists(path1) && File.Exists(path2))
                {
                    existingFilePaths.Add(path1);
                    existingFilePaths.Add(path2);
                }
            }

            appDispatcher.Invoke(() =>
            {
                Progress_Bar.Maximum = existingFilePaths.Count;
                Progress_Bar.Value = 0;
                Percent_Label.Text = "0%";
            });
            AppState.FilesLeft = existingFilePaths.Count;

            var checksumTasks = existingFilePaths
                .Select(filePath => GenerateAndReturnFileChecksum(filePath, branchFolder))
                .ToList();

            return checksumTasks;
        }

        public static List<Task<FileChecksum>> PrepareBranchChecksumTasks(string branchFolder)
        {
            var checksumTasks = new List<Task<FileChecksum>>();

            var allFiles = Directory.GetFiles(branchFolder, "*", SearchOption.AllDirectories).Where(f => !f.Contains("opt.starpak", StringComparison.OrdinalIgnoreCase)).ToArray();

            appDispatcher.Invoke(() =>
            {
                Progress_Bar.Maximum = allFiles.Length;
                Progress_Bar.Value = 0;
                Percent_Label.Text = "0%";
            });

            AppState.FilesLeft = allFiles.Length;

            foreach (var file in allFiles)
            {
                if(file.Contains("platform\\cfg\\user", StringComparison.OrdinalIgnoreCase) || file.Contains("platform\\screenshots", StringComparison.OrdinalIgnoreCase) || file.Contains("platform\\logs", StringComparison.OrdinalIgnoreCase))
                    continue;

                checksumTasks.Add(GenerateAndReturnFileChecksum(file, branchFolder));
            }

            return checksumTasks;
        }

        public static List<Task<FileChecksum>> PrepareOptChecksumTasks(string branchFolder)
        {
            var checksumTasks = new List<Task<FileChecksum>>();

            var allFiles = Directory.GetFiles(branchFolder, "*", SearchOption.AllDirectories).Where(f => f.Contains("opt.starpak", StringComparison.OrdinalIgnoreCase)).ToArray();

            appDispatcher.Invoke(() =>
            {
                Progress_Bar.Maximum = allFiles.Length;
                Progress_Bar.Value = 0;
                Percent_Label.Text = "0%";
            });

            AppState.FilesLeft = allFiles.Length;

            foreach (var file in allFiles)
            {
                checksumTasks.Add(GenerateAndReturnFileChecksum(file, branchFolder));
            }

            return checksumTasks;
        }

        public static Task<FileChecksum> GenerateAndReturnFileChecksum(string file, string branchFolder)
        {
            return Task.Run(async () =>
            {
                await _downloadSemaphore.WaitAsync();

                var fileChecksum = new FileChecksum();
                try
                {
                    fileChecksum.name = file.Replace(branchFolder + "\\", "");
                    fileChecksum.checksum = CalculateChecksum(file);

                    appDispatcher.Invoke(() =>
                    {
                        Progress_Bar.Value++;
                        //Files_Label.Text = $"{--AppState.FilesLeft} files left";
                        Percent_Label.Text = $"{(Progress_Bar.Value / Progress_Bar.Maximum * 100):F2}%";
                    });

                    return fileChecksum;
                }
                catch (Exception ex)
                {
                    LogException($"Failed Generating Checksum For {file}", LogSource.Checksums, ex);
                    return fileChecksum;
                }
                finally
                {
                    _downloadSemaphore.Release();
                }
            });
        }

        public static string CalculateChecksum(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(stream);
            stream.Close();
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}