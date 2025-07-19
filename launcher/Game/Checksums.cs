using launcher.Global;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using static launcher.Global.Logger;
using static launcher.Global.References;

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
                string filePath = Path.Combine(branchDirectory, file.destinationPath);

                if (!File.Exists(filePath) || !checksumDict.TryGetValue(file.destinationPath, out var calculatedChecksum) || file.checksum != calculatedChecksum)
                {
                    LogWarning(isUpdate ? Source.Update : Source.Repair, isUpdate ? $"Updated file found: {file.destinationPath}" : $"Bad file found: {file.destinationPath}");

                    GameFile gameFile = new GameFile
                    {
                        destinationPath = $"{file.destinationPath}",
                        checksum = file.checksum,
                        sizeInBytes = file.sizeInBytes,
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

        public static List<Task<FileChecksum>> PrepareLangChecksumTasks(string branchFolder, List<string> lang)
        {
            var checksumTasks = new List<Task<FileChecksum>>();

            List<string> excludedLanguages = GetBranch.Branch().mstr_languages;
            excludedLanguages.Remove("english");

            string languagesPattern = string.Join("|", excludedLanguages.Select(Regex.Escape));
            Regex excludeLangRegex = new Regex($"general_({languagesPattern})(?:_|\\.)", RegexOptions.IgnoreCase);

            var allFiles = Directory.GetFiles(branchFolder, "*", SearchOption.AllDirectories).Where(
                f => excludeLangRegex.IsMatch(f)).ToArray();

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
                await Download.Tasks._downloadSemaphore.WaitAsync();

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
                    LogException($"Failed Generating Checksum For {file}", Source.Checksums, ex);
                    return fileChecksum;
                }
                finally
                {
                    Download.Tasks._downloadSemaphore.Release();
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