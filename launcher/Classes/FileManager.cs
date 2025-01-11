using Newtonsoft.Json;
using SoftCircuits.IniFileParser;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Shapes;
using Path = System.IO.Path;

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
        public static int IdentifyBadFiles(BaseGameFiles baseGameFiles, List<Task<FileChecksum>> checksumTasks)
        {
            var fileChecksums = Task.WhenAll(checksumTasks).Result;
            var checksumDict = fileChecksums.ToDictionary(fc => fc.name, fc => fc.checksum);

            Global.filesLeft = baseGameFiles.files.Count;
            Global.badFiles.Clear();

            foreach (var file in baseGameFiles.files)
            {
                string filePath = Path.Combine(Global.launcherPath, file.name);

                if (!File.Exists(filePath) || !checksumDict.TryGetValue(file.name, out var calculatedChecksum) || file.checksum != calculatedChecksum)
                {
                    if (!filePath.Contains("launcher.exe"))
                    {
                        Logger.Log(Logger.Type.Warning, Logger.Source.Repair, $"Bad file found: {file.name}");
                        Global.badFiles.Add($"{file.name}.zst");
                    }
                }

                ControlReferences.dispatcher.Invoke(() =>
                {
                    ControlReferences.progressBar.Value++;
                    ControlReferences.lblFilesLeft.Text = $"{--Global.filesLeft} files left";
                });
            }

            return Global.badFiles.Count;
        }

        public static async Task CleanUpTempDirectory(string tempDirectory, int maxConcurrency = 10)
        {
            try
            {
                string[] files = Directory.GetFiles(tempDirectory, "*", SearchOption.AllDirectories);

                using var semaphore = new SemaphoreSlim(maxConcurrency);

                var deleteTasks = files.Select(async file =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        TryDeleteFile(file, TimeSpan.FromSeconds(30), TimeSpan.FromMilliseconds(500));
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                await Task.WhenAll(deleteTasks);

                if (Directory.Exists(tempDirectory))
                {
                    try
                    {
                        Directory.Delete(tempDirectory, true);
                        Logger.Log(Logger.Type.Info, Logger.Source.FileManager, $"Deleted temp directory: {tempDirectory}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(Logger.Type.Error, Logger.Source.FileManager, $"Error deleting temp directory: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(Logger.Type.Error, Logger.Source.FileManager, $"Error cleaning up temp directory: {ex.Message}");
            }
        }

        private static void TryDeleteFile(string filePath, TimeSpan timeout, TimeSpan retryInterval)
        {
            DateTime endTime = DateTime.Now.Add(timeout);

            while (DateTime.Now < endTime)
            {
                try
                {
                    using (FileStream fs = new(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    {
                        // If we get here, the file is not in use and can be safely deleted
                    }

                    File.Delete(filePath);
                    //Logger.Log(Logger.Type.Info, Logger.Source.FileManager, $"Deleted file: {filePath}");
                    return;
                }
                catch (IOException)
                {
                    Logger.Log(Logger.Type.Warning, Logger.Source.FileManager, $"File in use, retrying: {filePath}");
                }
                catch (UnauthorizedAccessException)
                {
                    Logger.Log(Logger.Type.Warning, Logger.Source.FileManager, $"Access denied, skipping: {filePath}");
                    return;
                }

                Thread.Sleep(retryInterval);
            }

            Logger.Log(Logger.Type.Error, Logger.Source.FileManager, $"Failed to delete file after retries: {filePath}");
        }

        public static IniFile GetLauncherConfig()
        {
            string configPath = Path.Combine(Global.launcherPath, "platform\\cfg\\user\\launcherConfig.ini");

            if (!File.Exists(configPath))
                return null;

            IniFile file = new();
            file.Load(configPath);

            Logger.Log(Logger.Type.Info, Logger.Source.FileManager, "Loaded launcher ini");

            return file;
        }

        public static string CreateTempDirectory()
        {
            string tempDirectory = Path.Combine(Global.launcherPath, "temp");
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        public static List<Task<FileChecksum>> PrepareBaseGameChecksumTasks()
        {
            var checksumTasks = new List<Task<FileChecksum>>();

            var allFiles = Directory.GetFiles(Global.launcherPath, "*", SearchOption.AllDirectories)
                        .Where(f => !f.Contains("\\temp\\") && !f.Contains("opt.starpak", StringComparison.OrdinalIgnoreCase))
                        .ToArray();

            ControlReferences.dispatcher.Invoke(() =>
            {
                ControlReferences.progressBar.Maximum = allFiles.Length;
                ControlReferences.progressBar.Value = 0;
            });

            Global.filesLeft = allFiles.Length;

            foreach (var file in allFiles)
            {
                checksumTasks.Add(GenerateAndReturnFileChecksum(file));
            }

            return checksumTasks;
        }

        public static List<Task<FileChecksum>> PrepareOptionalGameChecksumTasks()
        {
            var checksumTasks = new List<Task<FileChecksum>>();

            var allFiles = Directory.GetFiles(Global.launcherPath, "*", SearchOption.AllDirectories)
                        .Where(f => !f.Contains("\\temp\\") && f.Contains("opt.starpak", StringComparison.OrdinalIgnoreCase))
                        .ToArray();

            ControlReferences.dispatcher.Invoke(() =>
            {
                ControlReferences.progressBar.Maximum = allFiles.Length;
                ControlReferences.progressBar.Value = 0;
            });

            Global.filesLeft = allFiles.Length;

            foreach (var file in allFiles)
            {
                checksumTasks.Add(GenerateAndReturnFileChecksum(file));
            }

            return checksumTasks;
        }

        public static Task<FileChecksum> GenerateAndReturnFileChecksum(string file)
        {
            return Task.Run(() =>
            {
                var fileChecksum = new FileChecksum
                {
                    name = file.Replace(Global.launcherPath + "\\", ""),
                    checksum = CalculateChecksum(file)
                };

                //Logger.Log(Logger.Type.Info, Logger.Source.Repair, $"Calculated checksum for {file}: {fileChecksum.checksum}");

                ControlReferences.dispatcher.Invoke(() =>
                {
                    ControlReferences.progressBar.Value++;
                    ControlReferences.lblFilesLeft.Text = $"{--Global.filesLeft} files left";
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
            string configPath = Path.Combine(Global.launcherPath, "platform\\cfg\\user\\launcherConfig.json");
            string config_json = JsonConvert.SerializeObject(Global.launcherConfig);
            File.WriteAllText(configPath, config_json);

            Logger.Log(Logger.Type.Info, Logger.Source.FileManager, "Saved launcher config");
        }
    }
}