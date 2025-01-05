using Newtonsoft.Json;
using Octodiff.Core;
using Octodiff.Diagnostics;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using ZstdSharp;

namespace launcher
{
    public static class Helper
    {
        public const string launcherVersion = "0.2.4";

        public static ProgressBar progressBar = new ProgressBar();
        public static TextBlock lblStatus = new TextBlock();
        public static TextBlock lblFilesLeft = new TextBlock();
        public static MainWindow App = new MainWindow();

        public static GameRepair gameRepair = new GameRepair();
        public static GameInstall gameInstall = new GameInstall();
        public static GameUpdate gameUpdate = new GameUpdate();

        public static ServerConfig? serverConfig;
        public static LauncherConfig? launcherConfig;

        public static HttpClient client = new HttpClient();

        public static string launcherPath = "";
        public const int MAX_REPAIR_ATTEMPTS = 5;
        public static int filesLeft = 0;
        public static bool isInstalling = false;
        public static bool isInstalled = false;
        public static bool updateRequired = false;
        public static bool updateCheckLoop = false;
        public static List<string> badFiles = new List<string>();
        public static bool badFilesDetected = false;

        private static SemaphoreSlim downloadSemaphore = new SemaphoreSlim(50);

        public static void SetupApp(MainWindow mainWindow)
        {
            Console.WriteLine("Setting up app");

            App = mainWindow;
            progressBar = mainWindow.progressBar;
            lblStatus = mainWindow.lblStatus;
            lblFilesLeft = mainWindow.lblFilesLeft;

            App.launcherVersionlbl.Text = launcherVersion;

            progressBar.Visibility = Visibility.Hidden;
            lblStatus.Visibility = Visibility.Hidden;
            lblFilesLeft.Visibility = Visibility.Hidden;

            launcherPath = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);

            GetServerConfig();
            isInstalled = GetLauncherConfig();

            App.cmbBranch.ItemsSource = SetupGameBranches();
            App.cmbBranch.SelectedIndex = 0;
        }

        public static List<ComboBranch> SetupGameBranches()
        {
            return serverConfig.branches
                .Select(branch => new ComboBranch
                {
                    title = branch.branch,
                    subtext = branch.enabled ? branch.currentVersion : "branch disabled"
                })
                .ToList();
        }

        public static void LaunchGame()
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c start \"\" \"{launcherPath}\\r5apex.exe\""
            };

            // Start the new process via cmd
            Process.Start(startInfo);
        }

        public static bool IsNewVersion(string currentVersion, string newVersion)
        {
            var currentParts = currentVersion.Split('.').Select(int.Parse).ToArray();
            var newParts = newVersion.Split('.').Select(int.Parse).ToArray();

            for (int i = 0; i < Math.Max(currentParts.Length, newParts.Length); i++)
            {
                int currentPart = i < currentParts.Length ? currentParts[i] : 0;
                int newPart = i < newParts.Length ? newParts[i] : 0;

                if (currentPart < newPart)
                    return true;
                if (currentPart > newPart)
                    return false;
            }

            return false; // Versions are the same
        }

        public static void SetInstallState(bool installing, string buttonText = "PLAY")
        {
            App.Dispatcher.Invoke(() =>
            {
                isInstalling = installing;

                App.btnPlay.Content = buttonText;
                App.cmbBranch.IsEnabled = !installing;
                App.btnPlay.IsEnabled = !installing;
                lblStatus.Text = "";
                lblFilesLeft.Text = "";
            });

            ShowProgressBar(installing);
        }

        public static void UpdateStatusLabel(string statusText)
        {
            App.Dispatcher.Invoke(() =>
            {
                lblStatus.Text = statusText;
            });
        }

        public static async Task<BaseGameFiles> FetchBaseGameFiles(bool compressed)
        {
            string fileName = compressed ? "checksums_zst.json" : "checksums.json";

            string baseGameChecksumUrl = $"{serverConfig.base_game_url}\\{fileName}";
            string baseGameZstChecksums = await FetchJson(baseGameChecksumUrl);
            return JsonConvert.DeserializeObject<BaseGameFiles>(baseGameZstChecksums);
        }

        public static void ShowProgressBar(bool isVisible)
        {
            App.Dispatcher.Invoke(() =>
            {
                progressBar.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
                lblStatus.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
                lblFilesLeft.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
            });
        }

        public static string CreateTempDirectory()
        {
            string tempDirectory = Path.Combine(launcherPath, "temp");
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        public static void DeleteTempDirectory()
        {
            string tempDirectory = Path.Combine(launcherPath, "temp");
            Directory.Delete(tempDirectory, true);
        }

        public static bool ShouldSkipFileDownload(string destinationPath, string expectedChecksum)
        {
            if (File.Exists(destinationPath))
            {
                Console.WriteLine($"Checking existing file: {destinationPath}");
                string checksum = CalculateChecksum(destinationPath);
                if (checksum == expectedChecksum)
                {
                    App.Dispatcher.Invoke(() => { progressBar.Value++; });
                    return true;
                }
            }
            return false;
        }

        public static List<Task<string>> PrepareDownloadTasks(BaseGameFiles baseGameFiles, string tempDirectory)
        {
            var downloadTasks = new List<Task<string>>();

            App.Dispatcher.Invoke(() =>
            {
                progressBar.Maximum = baseGameFiles.files.Count;
                progressBar.Value = 0;
            });

            filesLeft = baseGameFiles.files.Count;

            foreach (var file in baseGameFiles.files)
            {
                string fileUrl = $"{serverConfig.base_game_url}/{file.name}";
                string destinationPath = Path.Combine(tempDirectory, file.name);

                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

                downloadTasks.Add(DownloadAndReturnFilePathAsync(fileUrl, destinationPath, file.name, file.checksum, true));
            }

            return downloadTasks;
        }

        public static List<Task<string>> PrepareRepairDownloadTasks(string tempDirectory)
        {
            filesLeft = badFiles.Count;

            var downloadTasks = new List<Task<string>>();

            App.Dispatcher.Invoke(() =>
            {
                progressBar.Maximum = badFiles.Count;
                progressBar.Value = 0;
            });

            filesLeft = badFiles.Count;

            foreach (var file in badFiles)
            {
                string fileUrl = $"{serverConfig.base_game_url}/{file}";
                string destinationPath = Path.Combine(tempDirectory, file);

                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

                downloadTasks.Add(DownloadAndReturnFilePathAsync(fileUrl, destinationPath, file));
            }

            return downloadTasks;
        }

        public static async Task<string> DownloadAndReturnFilePathAsync(string fileUrl, string destinationPath, string fileName, string checksum = "", bool checkForExistingFiles = false)
        {
            DownloadItem downloadItem = null;
            long downloadedBytes = 0;
            long totalBytes = -1;
            DateTime lastUpdate = DateTime.Now;

            // Wait for an available semaphore slot
            await downloadSemaphore.WaitAsync();

            try
            {
                // Check if file exists and checksum matches
                if (checkForExistingFiles && !string.IsNullOrEmpty(checksum) && ShouldSkipFileDownload(destinationPath, checksum))
                {
                    App.Dispatcher.Invoke(() =>
                    {
                        progressBar.Value++;
                        lblFilesLeft.Text = $"{--filesLeft} files left";
                    });

                    return destinationPath;
                }

                // Add download item to the popup
                App.Dispatcher.Invoke(() =>
                {
                    downloadItem = App.DownloadsPopupControl.AddDownloadItem(fileName);
                });

                using var response = await client.GetAsync(fileUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                totalBytes = response.Content.Headers.ContentLength ?? -1L;
                downloadedBytes = 0L;

                using var stream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);

                var buffer = new byte[8192];
                int bytesRead;

                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    downloadedBytes += bytesRead;

                    if ((DateTime.Now - lastUpdate).TotalMilliseconds > 100)
                    {
                        lastUpdate = DateTime.Now;

                        // Update progress in the popup
                        if (downloadItem != null && totalBytes > 0)
                        {
                            var progress = (double)downloadedBytes / totalBytes * 100;
                            App.Dispatcher.Invoke(() =>
                            {
                                downloadItem.downloadFilePercent.Text = $"{progress:F2}%";
                                downloadItem.downloadFileProgress.Value = progress;
                            });
                        }
                    }
                }

                Console.WriteLine($"Downloaded: {destinationPath}");

                // Update global progress
                App.Dispatcher.Invoke(() =>
                {
                    progressBar.Value++;
                    lblFilesLeft.Text = $"{--filesLeft} files left";
                });

                return destinationPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to download {fileUrl}: {ex.Message}");
                badFilesDetected = true;
                return string.Empty;
            }
            finally
            {
                // Remove the download item from the popup
                if (downloadItem != null)
                {
                    App.Dispatcher.Invoke(() =>
                    {
                        App.DownloadsPopupControl.RemoveDownloadItem(downloadItem);
                    });
                }

                // Release the semaphore slot
                downloadSemaphore.Release();
            }
        }

        public static List<Task> PrepareDecompressionTasks(List<Task<string>> downloadTasks)
        {
            var decompressionTasks = new List<Task>();

            filesLeft = downloadTasks.Count;

            App.Dispatcher.Invoke(() =>
            {
                progressBar.Maximum = downloadTasks.Count;
                progressBar.Value = 0;
            });

            foreach (var downloadTask in downloadTasks)
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

        public static List<Task<FileChecksum>> PrepareChecksumTasks()
        {
            var checksumTasks = new List<Task<FileChecksum>>();

            var allFiles = Directory.GetFiles(launcherPath, "*", SearchOption.AllDirectories)
                                    .Where(f => !f.Contains("\\temp\\"))
                                    .ToArray();

            App.Dispatcher.Invoke(() =>
            {
                progressBar.Maximum = allFiles.Length;
                progressBar.Value = 0;
            });

            filesLeft = allFiles.Length;

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
                    name = file.Replace(launcherPath + "\\", ""),
                    checksum = CalculateChecksum(file)
                };

                Console.WriteLine($"Calculated checksum for {file}: {fileChecksum.checksum}");

                App.Dispatcher.Invoke(() =>
                {
                    progressBar.Value++;
                    lblFilesLeft.Text = $"{--filesLeft} files left";
                });

                return fileChecksum;
            });
        }

        public static async Task<string> FetchJson(string url)
        {
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
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

                App.Dispatcher.Invoke(() =>
                {
                    progressBar.Value++;
                    lblFilesLeft.Text = $"{--filesLeft} files left";
                });

                Console.WriteLine($"Decompressed: {compressedFilePath} to {decompressedFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to decompress {compressedFilePath}: {ex.Message}");
            }
        }

        public static bool GetLauncherConfig()
        {
            string configPath = Path.Combine(launcherPath, "platform\\cfg\\user\\launcherConfig.json");

            if (!File.Exists(configPath))
                return false;

            Console.WriteLine("Config Exists");

            string config_json = File.ReadAllText(configPath);

            if (string.IsNullOrEmpty(config_json))
                return false;

            Console.WriteLine("Config JSON: " + config_json);

            launcherConfig = JsonConvert.DeserializeObject<LauncherConfig>(config_json);

            return launcherConfig != null;
        }

        public static void GetServerConfig()
        {
            var response = client.GetAsync("https://cdn.r5r.org/launcher/config.json").Result;
            var responseString = response.Content.ReadAsStringAsync().Result;
            serverConfig = JsonConvert.DeserializeObject<ServerConfig>(responseString);

            Console.WriteLine("Server Config Response\n" + responseString);
        }

        public static void UpdateOrCreateLauncherConfig()
        {
            Directory.CreateDirectory(Path.Combine(launcherPath, "platform\\cfg\\user"));

            string configPath = Path.Combine(launcherPath, "platform\\cfg\\user\\launcherConfig.json");
            if (File.Exists(configPath))
            {
                launcherConfig.currentUpdateBranch = serverConfig.branches[0].branch;
                launcherConfig.currentUpdateVersion = serverConfig.branches[0].currentVersion;
                SaveLauncherConfig();
            }
            else
            {
                launcherConfig = new LauncherConfig
                {
                    currentUpdateVersion = serverConfig.branches[0].currentVersion,
                    currentUpdateBranch = serverConfig.branches[0].branch
                };
                SaveLauncherConfig();
            }
        }

        public static void SaveLauncherConfig()
        {
            string configPath = Path.Combine(launcherPath, "platform\\cfg\\user\\launcherConfig.json");
            string config_json = JsonConvert.SerializeObject(launcherConfig);
            File.WriteAllText(configPath, config_json);

            Console.WriteLine("Saved Launcher Config\n" + config_json);
        }

        public static void UpdateLauncher()
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c start \"\" \"{launcherPath}\\bin\\selfupdater.exe\""
            };

            // Start the new process via cmd
            Process.Start(startInfo);

            Environment.Exit(0);
        }

        public static int IdentifyBadFiles(BaseGameFiles baseGameFiles, List<Task<FileChecksum>> checksumTasks)
        {
            var fileChecksums = Task.WhenAll(checksumTasks).Result;
            var checksumDict = fileChecksums.ToDictionary(fc => fc.name, fc => fc.checksum);

            filesLeft = baseGameFiles.files.Count;
            badFiles.Clear();

            foreach (var file in baseGameFiles.files)
            {
                string filePath = Path.Combine(launcherPath, file.name);

                if (!File.Exists(filePath) || !checksumDict.TryGetValue(file.name, out var calculatedChecksum) || file.checksum != calculatedChecksum)
                {
                    Console.WriteLine($"Bad file found: {file.name} | {filePath}");
                    badFiles.Add($"{file.name}.zst");
                }

                App.Dispatcher.Invoke(() =>
                {
                    progressBar.Value++;
                    lblFilesLeft.Text = $"{--filesLeft} files left";
                });
            }

            return badFiles.Count;
        }

        public static async Task<GamePatch> FetchPatchFiles()
        {
            int selectedBranchIndex = GetCmbBranchIndex();

            string patchURL = serverConfig.branches[selectedBranchIndex].patch_url + "\\patch.json";
            string patchFile = await FetchJson(patchURL);
            return JsonConvert.DeserializeObject<GamePatch>(patchFile);
        }

        public static void Delete(string file)
        {
            string fullPath = Path.Combine(launcherPath, file);
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }

        public static void Update(string file, string tempDirectory)
        {
            string sourceCompressedFile = Path.Combine(tempDirectory, file);
            string destinationFile = Path.Combine(launcherPath, file.Replace(".zst", ""));
            DecompressFileAsync(sourceCompressedFile, destinationFile);
        }

        public static void Patch(string file, string tempDirectory)
        {
            string sourceCompressedDeltaFile = Path.Combine(tempDirectory, file);
            string sourceDecompressedDeltaFile = Path.Combine(tempDirectory, file.Replace(".zst", ""));
            string destinationFile = Path.Combine(launcherPath, file.Replace(".delta.zst", ""));
            DecompressFileAsync(sourceCompressedDeltaFile, sourceDecompressedDeltaFile);
            PatchFile(destinationFile, sourceDecompressedDeltaFile);
        }

        public static void PatchFile(string originalFile, string deltaFile)
        {
            var signatureFile = Path.GetTempFileName();

            var signatureBuilder = new SignatureBuilder();
            using (var basisStream = new FileStream(originalFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var signatureStream = new FileStream(signatureFile, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                signatureBuilder.Build(basisStream, new SignatureWriter(signatureStream));
            }

            if (File.Exists(originalFile))
                File.Delete(originalFile);

            var deltaApplier = new DeltaApplier { SkipHashCheck = false };
            using (var basisStream = new FileStream(signatureFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var deltaStream = new FileStream(deltaFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var newFileStream = new FileStream(originalFile, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
            {
                deltaApplier.Apply(basisStream, new BinaryDeltaReader(deltaStream, new ConsoleProgressReporter()), newFileStream);
            }
        }

        public static List<Task<string>> PreparePatchDownloadTasks(GamePatch patchFiles, string tempDirectory)
        {
            var downloadTasks = new List<Task<string>>();

            App.Dispatcher.Invoke(() =>
            {
                progressBar.Maximum = patchFiles.files.Count;
                progressBar.Value = 0;
            });

            filesLeft = patchFiles.files.Count;

            int selectedBranchIndex = GetCmbBranchIndex();

            foreach (var file in patchFiles.files)
            {
                if (file.Action.ToLower() == "delete")
                    continue;

                string fileUrl = $"{serverConfig.branches[selectedBranchIndex].patch_url}/{file.Name}";
                string destinationPath = Path.Combine(tempDirectory, file.Name);

                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

                downloadTasks.Add(DownloadAndReturnFilePathAsync(fileUrl, destinationPath, file.Name));
            }

            return downloadTasks;
        }

        public static List<Task> PrepareFilePatchTasks(GamePatch patchFiles, string tempDirectory)
        {
            var tasks = new List<Task>();
            filesLeft = patchFiles.files.Count;

            foreach (var file in patchFiles.files)
            {
                tasks.Add(Task.Run(() =>
                {
                    switch (file.Action.ToLower())
                    {
                        case "delete":
                            Delete(file.Name);
                            break;

                        case "update":
                            Update(file.Name, tempDirectory);
                            break;

                        case "patch":
                            Patch(file.Name, tempDirectory);
                            break;
                    }

                    // Update UI thread-safe
                    App.Dispatcher.Invoke(() =>
                    {
                        progressBar.Value++;
                        lblFilesLeft.Text = $"{--filesLeft} files left";
                    });
                }));
            }

            return tasks;
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

        public static void TryDeleteFile(string filePath, TimeSpan timeout, TimeSpan retryInterval)
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

        public static int GetCmbBranchIndex()
        {
            int cmbSelectedIndex = 0;

            //mainWindow.Dispatcher.Invoke(() =>
            //{
            //    cmbSelectedIndex = mainWindow.cmbBranch.SelectedIndex;
            //});

            return cmbSelectedIndex;
        }
    }
}