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
        public static int IdentifyBadFiles(BaseGameFiles baseGameFiles, List<Task<FileChecksum>> checksumTasks)
        {
            var fileChecksums = Task.WhenAll(checksumTasks).Result;
            var checksumDict = fileChecksums.ToDictionary(fc => fc.name, fc => fc.checksum);

            FILES_LEFT = baseGameFiles.files.Count;
            BAD_FILES.Clear();

            foreach (var file in baseGameFiles.files)
            {
                string filePath = Path.Combine(LAUNCHER_PATH, file.name);

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
                        Log(Logger.Type.Info, Source.FileManager, $"Deleted temp directory: {tempDirectory}");
                    }
                    catch (Exception ex)
                    {
                        Log(Logger.Type.Error, Source.FileManager, $"Error deleting temp directory: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log(Logger.Type.Error, Source.FileManager, $"Error cleaning up temp directory: {ex.Message}");
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
                    //Log(Logger.Type.Info, Source.FileManager, $"Deleted file: {filePath}");
                    return;
                }
                catch (IOException)
                {
                    Log(Logger.Type.Warning, Source.FileManager, $"File in use, retrying: {filePath}");
                }
                catch (UnauthorizedAccessException)
                {
                    Log(Logger.Type.Warning, Source.FileManager, $"Access denied, skipping: {filePath}");
                    return;
                }

                Thread.Sleep(retryInterval);
            }

            Log(Logger.Type.Error, Source.FileManager, $"Failed to delete file after retries: {filePath}");
        }

        public static IniFile GetLauncherConfig()
        {
            string configPath = Path.Combine(LAUNCHER_PATH, "platform\\cfg\\user\\launcherConfig.ini");

            if (!File.Exists(configPath))
                return null;

            IniFile file = new();
            file.Load(configPath);

            Log(Logger.Type.Info, Source.FileManager, "Loaded launcher ini");

            return file;
        }

        public static string CreateTempDirectory()
        {
            string tempDirectory = Path.Combine(LAUNCHER_PATH, "temp");
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        public static List<Task<FileChecksum>> PrepareBaseGameChecksumTasks()
        {
            var checksumTasks = new List<Task<FileChecksum>>();

            var allFiles = Directory.GetFiles(LAUNCHER_PATH, "*", SearchOption.AllDirectories)
                        .Where(f => !f.Contains("\\temp\\") && !f.Contains("opt.starpak", StringComparison.OrdinalIgnoreCase))
                        .ToArray();

            appDispatcher.Invoke(() =>
            {
                progressBar.Maximum = allFiles.Length;
                progressBar.Value = 0;
            });

            FILES_LEFT = allFiles.Length;

            foreach (var file in allFiles)
            {
                checksumTasks.Add(GenerateAndReturnFileChecksum(file));
            }

            return checksumTasks;
        }

        public static List<Task<FileChecksum>> PrepareOptionalGameChecksumTasks()
        {
            var checksumTasks = new List<Task<FileChecksum>>();

            var allFiles = Directory.GetFiles(LAUNCHER_PATH, "*", SearchOption.AllDirectories)
                        .Where(f => !f.Contains("\\temp\\") && f.Contains("opt.starpak", StringComparison.OrdinalIgnoreCase))
                        .ToArray();

            appDispatcher.Invoke(() =>
            {
                progressBar.Maximum = allFiles.Length;
                progressBar.Value = 0;
            });

            FILES_LEFT = allFiles.Length;

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
                    name = file.Replace(LAUNCHER_PATH + "\\", ""),
                    checksum = CalculateChecksum(file)
                };

                //Log(Logger.Type.Info, Source.Repair, $"Calculated checksum for {file}: {fileChecksum.checksum}");

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
            string configPath = Path.Combine(LAUNCHER_PATH, "platform\\cfg\\user\\launcherConfig.json");
            string config_json = JsonConvert.SerializeObject(LAUNCHER_CONFIG);
            File.WriteAllText(configPath, config_json);

            Log(Logger.Type.Info, Source.FileManager, "Saved launcher config");
        }
    }
}