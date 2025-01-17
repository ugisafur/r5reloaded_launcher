using Newtonsoft.Json;
using SoftCircuits.IniFileParser;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Shapes;
using Path = System.IO.Path;
using static launcher.ControlReferences;
using static launcher.Logger;

namespace launcher
{
    /// <summary>
    /// The FileManager class provides various static methods for managing files within the launcher application.
    /// It includes functionalities for identifying bad files, cleaning up temporary directories, generating file checksums,
    /// and managing the launcher configuration. This class is essential for ensuring the integrity and proper functioning
    /// of the launcher by handling file operations and configurations.
    /// </summary>
    public static class FileManager
    {
        public static int IdentifyBadFiles(BaseGameFiles baseGameFiles, List<Task<FileChecksum>> checksumTasks, string branchDirectory)
        {
            var fileChecksums = Task.WhenAll(checksumTasks).Result;
            var checksumDict = fileChecksums.ToDictionary(fc => fc.name, fc => fc.checksum);

            appDispatcher.Invoke(() =>
            {
                Progress_Bar.Maximum = baseGameFiles.files.Count;
                Progress_Bar.Value = 0;
            });

            AppState.FilesLeft = baseGameFiles.files.Count;
            DataCollections.BadFiles.Clear();

            foreach (var file in baseGameFiles.files)
            {
                string filePath = Path.Combine(branchDirectory, file.name);

                if (!File.Exists(filePath) || !checksumDict.TryGetValue(file.name, out var calculatedChecksum) || file.checksum != calculatedChecksum)
                {
                    LogWarning(Source.Repair, $"Bad file found: {file.name}");
                    DataCollections.BadFiles.Add($"{file.name}.zst");
                }

                appDispatcher.Invoke(() =>
                {
                    Progress_Bar.Value++;
                    Files_Label.Text = $"{--AppState.FilesLeft} files left";
                });
            }

            return DataCollections.BadFiles.Count;
        }

        public static string GetBranchDirectory()
        {
            string branchName = Configuration.ServerConfig.branches[Utilities.GetCmbBranchIndex()].branch.ToUpper();
            string libraryPath = (string)Ini.Get(Ini.Vars.Library_Location);
            string finalDirectory = Path.Combine(libraryPath, "R5R Library", branchName);

            Directory.CreateDirectory(finalDirectory);

            return finalDirectory;
        }

        public static string GetLibraryPathDirectory()
        {
            string libraryPath = (string)Ini.Get(Ini.Vars.Library_Location);
            string finalDirectory = Path.Combine(libraryPath, "R5R Library");

            Directory.CreateDirectory(finalDirectory);

            return finalDirectory;
        }

        public static List<Task<FileChecksum>> PrepareBaseGameChecksumTasks(string branchFolder)
        {
            var checksumTasks = new List<Task<FileChecksum>>();

            var allFiles = Directory.GetFiles(branchFolder, "*", SearchOption.AllDirectories)
                        .Where(f => !f.Contains("opt.starpak", StringComparison.OrdinalIgnoreCase) &&
                        !f.Contains(".zst", StringComparison.OrdinalIgnoreCase) &&
                        !f.Contains(".delta", StringComparison.OrdinalIgnoreCase)).ToArray();

            appDispatcher.Invoke(() =>
            {
                Progress_Bar.Maximum = allFiles.Length;
                Progress_Bar.Value = 0;
            });

            AppState.FilesLeft = allFiles.Length;

            foreach (var file in allFiles)
            {
                checksumTasks.Add(GenerateAndReturnFileChecksum(file, branchFolder));
            }

            return checksumTasks;
        }

        public static List<Task<FileChecksum>> PrepareOptionalGameChecksumTasks(string branchFolder)
        {
            var checksumTasks = new List<Task<FileChecksum>>();

            var allFiles = Directory.GetFiles(branchFolder, "*", SearchOption.AllDirectories)
                        .Where(f => f.Contains("opt.starpak", StringComparison.OrdinalIgnoreCase) &&
                        !f.Contains(".zst", StringComparison.OrdinalIgnoreCase) &&
                        !f.Contains(".delta", StringComparison.OrdinalIgnoreCase)).ToArray();

            appDispatcher.Invoke(() =>
            {
                Progress_Bar.Maximum = allFiles.Length;
                Progress_Bar.Value = 0;
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
            return Task.Run(() =>
            {
                var fileChecksum = new FileChecksum
                {
                    name = file.Replace(branchFolder + "\\", ""),
                    checksum = CalculateChecksum(file)
                };

                appDispatcher.Invoke(() =>
                {
                    Progress_Bar.Value++;
                    Files_Label.Text = $"{--AppState.FilesLeft} files left";
                });

                return fileChecksum;
            });
        }

        public static string CalculateChecksum(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        public static void SaveLauncherConfig()
        {
            string configPath = Path.Combine(Constants.Paths.LauncherPath, "launcher_data\\cfg\\launcherConfig.json");
            string config_json = JsonConvert.SerializeObject(Configuration.LauncherConfig);
            File.WriteAllText(configPath, config_json);

            LogInfo(Source.FileManager, "Saved launcher config");
        }
    }
}