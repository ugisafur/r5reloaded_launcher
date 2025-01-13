using Newtonsoft.Json;
using SoftCircuits.IniFileParser;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Shapes;
using Path = System.IO.Path;
using static launcher.Global;
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

            FILES_LEFT = baseGameFiles.files.Count;
            BAD_FILES.Clear();

            foreach (var file in baseGameFiles.files)
            {
                string filePath = Path.Combine(branchDirectory, file.name);

                if (!File.Exists(filePath) || !checksumDict.TryGetValue(file.name, out var calculatedChecksum) || file.checksum != calculatedChecksum)
                {
                    if (!filePath.Contains("launcher.exe"))
                    {
                        Log(Logger.Type.Warning, Source.Repair, $"Bad file found: {file.name}");
                        BAD_FILES.Add($"{file.name}.zst");
                    }
                }

                appDispatcher.Invoke(() =>
                {
                    progressBar.Value++;
                    lblFilesLeft.Text = $"{--FILES_LEFT} files left";
                });
            }

            return BAD_FILES.Count;
        }

        public static IniFile GetLauncherConfig()
        {
            string configPath = Path.Combine(LAUNCHER_PATH, "launcher_data\\cfg\\launcherConfig.ini");

            if (!File.Exists(configPath))
                return null;

            IniFile file = new();
            file.Load(configPath);

            Log(Logger.Type.Info, Source.FileManager, "Loaded launcher ini");

            return file;
        }

        public static string GetBranchDirectory()
        {
            string branchName = SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].branch.ToUpper();
            string libraryPath = Ini.Get(Ini.Vars.Library_Location, "C:\\Program Files\\R5Reloaded\\");
            string finalDirectory = Path.Combine(libraryPath, "R5R Library", branchName);

            Directory.CreateDirectory(finalDirectory);

            return finalDirectory;
        }

        public static string GetLibraryPathDirectory()
        {
            string libraryPath = Ini.Get(Ini.Vars.Library_Location, "C:\\Program Files\\R5Reloaded\\");
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
                progressBar.Maximum = allFiles.Length;
                progressBar.Value = 0;
            });

            FILES_LEFT = allFiles.Length;

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
                progressBar.Maximum = allFiles.Length;
                progressBar.Value = 0;
            });

            FILES_LEFT = allFiles.Length;

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
                    progressBar.Value++;
                    lblFilesLeft.Text = $"{--FILES_LEFT} files left";
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
            string configPath = Path.Combine(LAUNCHER_PATH, "launcher_data\\cfg\\launcherConfig.json");
            string config_json = JsonConvert.SerializeObject(LAUNCHER_CONFIG);
            File.WriteAllText(configPath, config_json);

            Log(Logger.Type.Info, Source.FileManager, "Saved launcher config");
        }
    }
}