using Newtonsoft.Json;
using System.IO;
using System.Security.Cryptography;

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
                        Console.WriteLine($"Bad file found: {file.name} | {filePath}");
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
                        Console.WriteLine($"Deleted temp directory: {tempDirectory}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting temp directory: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cleaning up temp directory: {ex.Message}");
            }
        }

        private static void TryDeleteFile(string filePath, TimeSpan timeout, TimeSpan retryInterval)
        {
            DateTime endTime = DateTime.Now.Add(timeout);

            while (DateTime.Now < endTime)
            {
                try
                {
                    using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    {
                        // If we get here, the file is not in use and can be safely deleted
                    }

                    File.Delete(filePath);
                    Console.WriteLine($"Deleted: {filePath}");
                    return;
                }
                catch (IOException)
                {
                    Console.WriteLine($"File in use, retrying: {filePath}");
                }
                catch (UnauthorizedAccessException)
                {
                    Console.WriteLine($"Access denied, skipping: {filePath}");
                    return;
                }

                Thread.Sleep(retryInterval);
            }

            Console.WriteLine($"Failed to delete file after retries: {filePath}");
        }

        public static LauncherConfig GetLauncherConfig()
        {
            string configPath = Path.Combine(Global.launcherPath, "platform\\cfg\\user\\launcherConfig.json");

            if (!File.Exists(configPath))
                return null;

            Console.WriteLine("Config Exists");

            string config_json = File.ReadAllText(configPath);

            if (string.IsNullOrEmpty(config_json))
                return null;

            Console.WriteLine("Config JSON: " + config_json);

            return JsonConvert.DeserializeObject<LauncherConfig>(config_json);
        }

        public static string CreateTempDirectory()
        {
            string tempDirectory = Path.Combine(Global.launcherPath, "temp");
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        public static List<Task<FileChecksum>> PrepareChecksumTasks()
        {
            var checksumTasks = new List<Task<FileChecksum>>();

            var allFiles = Directory.GetFiles(Global.launcherPath, "*", SearchOption.AllDirectories)
                                    .Where(f => !f.Contains("\\temp\\"))
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

                Console.WriteLine($"Calculated checksum for {file}: {fileChecksum.checksum}");

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
            using (var stream = File.OpenRead(filePath))
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        public static void UpdateOrCreateLauncherConfig()
        {
            Directory.CreateDirectory(Path.Combine(Global.launcherPath, "platform\\cfg\\user"));

            string configPath = Path.Combine(Global.launcherPath, "platform\\cfg\\user\\launcherConfig.json");
            if (File.Exists(configPath))
            {
                Global.launcherConfig.currentUpdateBranch = Global.serverConfig.branches[0].branch;
                Global.launcherConfig.currentUpdateVersion = Global.serverConfig.branches[0].currentVersion;
                SaveLauncherConfig();
            }
            else
            {
                Global.launcherConfig = new LauncherConfig
                {
                    currentUpdateVersion = Global.serverConfig.branches[0].currentVersion,
                    currentUpdateBranch = Global.serverConfig.branches[0].branch
                };
                SaveLauncherConfig();
            }
        }

        public static void SaveLauncherConfig()
        {
            string configPath = Path.Combine(Global.launcherPath, "platform\\cfg\\user\\launcherConfig.json");
            string config_json = JsonConvert.SerializeObject(Global.launcherConfig);
            File.WriteAllText(configPath, config_json);

            Console.WriteLine("Saved Launcher Config\n" + config_json);
        }
    }
}