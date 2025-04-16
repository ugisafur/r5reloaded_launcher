using Newtonsoft.Json;
using System.Security.Cryptography;
using ZstdSharp;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Text;
using System.Net.Http.Headers;
using System.IO;

namespace patch_creator
{
    public partial class Form1 : Form
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        private readonly string[] whitelistPatchPaths = new string[] {
            "vpk",
            "stbsp",
            "paks\\Win64",
            "audio\\ship",
        };

        private string? AUTH_TOKEN;
        private string? ZONE_ID;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            AllocConsole();

            var response = Global.HTTP_CLIENT.GetAsync("https://cdn.r5r.org/launcher/config.json").Result;
            var responseString = response.Content.ReadAsStringAsync().Result;
            Global.SERVER_CONFIG = JsonConvert.DeserializeObject<ServerConfig>(responseString);

            foreach (Branch branch in Global.SERVER_CONFIG.branches)
            {
                comboBox1.Items.Add(branch.branch);
            }

            comboBox1.SelectedIndex = 0;

            LoadConfig();
        }

        private void LoadConfig()
        {
            string configPath = Application.StartupPath + "config.json";
            if (!File.Exists(configPath))
            {
                Log("Config file not found, creating default config");
                Log("Please fill in the required fields in the config file");

                CFConfig cFConfig = new CFConfig
                {
                    zoneID = "",
                    authKey = ""
                };

                string defaultConfig = JsonConvert.SerializeObject(cFConfig, Formatting.Indented);
                File.WriteAllText(configPath, defaultConfig);
            }

            string config = File.ReadAllText(configPath);
            CFConfig cfConfig = JsonConvert.DeserializeObject<CFConfig>(config);

            if (!string.IsNullOrEmpty(cfConfig.zoneID) || !string.IsNullOrEmpty(cfConfig.authKey))
            {
                AUTH_TOKEN = cfConfig.authKey;
                ZONE_ID = cfConfig.zoneID;
            }
            else
            {
                PurgeAllBtn.Enabled = false;
                PurgeListBtn.Enabled = false;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var directoryDialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Select Folder"
            };

            if (directoryDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                textBox1.Text = directoryDialog.FileName;

                if (string.IsNullOrEmpty(textBox2.Text))
                {
                    DirectoryInfo parentDir = Directory.GetParent(directoryDialog.FileName.TrimEnd(Path.DirectorySeparatorChar));
                    textBox2.Text = Path.Combine(parentDir.FullName, $"{Global.SERVER_CONFIG.branches[comboBox1.SelectedIndex].branch}_update");
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var directoryDialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Select Folder"
            };

            if (directoryDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                textBox2.Text = directoryDialog.FileName;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Task.Run(() => CreatePatch());
        }

        private async void CreatePatch()
        {
            SetAppState(true);
            Log("---------- Patch creation started ----------");

            // Get the selected branch index
            int selected_index = 0;
            comboBox1.Invoke(() => { selected_index = comboBox1.SelectedIndex; });

            // Create the final game directory
            UpdateProgressLabel("Creating directory");
            var final_game_dir = textBox2.Text;
            Directory.CreateDirectory(final_game_dir);

            string[] overrideStrings = richTextBox2.Lines;

            //Get current checksums.json file
            UpdateProgressLabel("Getting server checksums");
            var response = await Global.HTTP_CLIENT.GetStringAsync(Path.Combine(Global.SERVER_CONFIG.branches[selected_index].game_url, "checksums.json"));
            GameChecksums server_checksums = JsonConvert.DeserializeObject<GameChecksums>(response);

            //Get updated checksums.json file
            UpdateProgressLabel("Generating local checksums");
            GameChecksums local_checksums = await GenerateMetadataAsync(textBox1.Text);

            //Find the changed files
            UpdateProgressLabel("Finding changed files");
            List<GameFile> changedFiles = local_checksums.files.Where(updatedFile => !server_checksums.files.Any(currentFile => currentFile.name == updatedFile.name && currentFile.checksum == updatedFile.checksum) || overrideStrings.Contains(updatedFile.name)).ToList();
            GameChecksums new_checksums = local_checksums;//UpdateGameChecksums(server_checksums, local_checksums);

            //Compress and move the changed files to the game directory
            UpdateProgressLabel("Compressing files");
            await CompressNewFilesAsync(final_game_dir, changedFiles);

            //Get server checksums.json file
            UpdateProgressLabel("Creating new checksums.json");
            local_checksums.compression_level = (int)compressionLevel.Value;
            local_checksums.game_version = versionTxt.Text;
            var game_checksums_file = JsonSerializer.Serialize(local_checksums, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(final_game_dir + "\\checksums.json", game_checksums_file);

            //Get local compressed files checksums
            UpdateProgressLabel("Generating local zst checksums");
            GameChecksums local_zst_checksums = await GetCompressedChecksums(final_game_dir);

            //Get checksums_zst.json file from the server
            UpdateProgressLabel("Getting server zst checksums");
            string checksums_zst_response = await Global.HTTP_CLIENT.GetStringAsync(Path.Combine(Global.SERVER_CONFIG.branches[selected_index].game_url, "checksums_zst.json"));
            GameChecksums server_zst_checksums = JsonConvert.DeserializeObject<GameChecksums>(checksums_zst_response);

            //Find the changed files
            UpdateProgressLabel("Finding changed zst files");
            List<GameFile> compressed_changedFiles = local_zst_checksums.files.Where(updatedFile => !server_zst_checksums.files.Any(currentFile => currentFile.name == updatedFile.name && currentFile.checksum == updatedFile.checksum)).ToList();

            // Update the server checksums
            UpdateProgressLabel("Creating new checksums_zst.json");
            GameChecksums new_zst_checksums = UpdateZSTChecksums(local_zst_checksums, server_zst_checksums);

            new_zst_checksums.compression_level = (int)compressionLevel.Value;
            new_zst_checksums.game_version = versionTxt.Text;
            var new_zst_checksums_file = JsonSerializer.Serialize(new_zst_checksums, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(final_game_dir + "\\checksums_zst.json", new_zst_checksums_file);

            //Update the clear cache list
            UpdateProgressLabel("Updating clear cache list");
            UpdateClearCacheList(selected_index, compressed_changedFiles, final_game_dir);

            if (!string.IsNullOrEmpty(versionTxt.Text))
            {
                File.WriteAllText(final_game_dir + "\\version.txt", versionTxt.Text);
            }

            UpdateProgressLabel("Patch creation complete");
            Log("---------- Patch creation finished ----------");
            SetAppState(false);
        }

        private void SetAppState(bool running)
        {
            this.Invoke(() =>
            {
                textBox1.Enabled = !running;
                textBox2.Enabled = !running;
                button1.Enabled = !running;
                button2.Enabled = !running;
                button3.Enabled = !running;
                comboBox1.Enabled = !running;
            });
        }

        private void SetProgressBarValue(int value)
        {
            progressBar1.Invoke(() =>
            {
                progressBar1.Value = value;
                totalFilesLeft.Text = $"{value}/{progressBar1.Maximum}";
            });
        }

        private void SetProgressBarMax(int value)
        {
            progressBar1.Invoke(() =>
            {
                progressBar1.Maximum = value;
            });
        }

        private void UpdateProgressLabel(string text)
        {
            progressLabel.Invoke(() =>
            {
                progressLabel.Text = text;
            });
        }

        private GameChecksums UpdateGameChecksums(GameChecksums server_checksums, GameChecksums local_checksums)
        {
            SetProgressBarMax(local_checksums.files.Count);
            SetProgressBarValue(0);

            // Find removed files (present in the current checksums but not in the updated one)
            List<GameFile> removedFiles = server_checksums.files.Where(currentFile => !local_checksums.files.Any(updatedFile => updatedFile.name == currentFile.name)).ToList();

            List<GameFile> finalFiles = server_checksums.files.Where(file => !removedFiles.Any(removed => removed.name == file.name)).ToList();

            int i = 0;
            foreach (var updatedFile in local_checksums.files)
            {
                // Check if the file already exists in the final list
                var existingFile = finalFiles.FirstOrDefault(f => f.name == updatedFile.name);
                if (existingFile != null)
                {
                    // Replace the existing file
                    finalFiles.Remove(existingFile);
                }
                // Add the updated file
                finalFiles.Add(updatedFile);

                SetProgressBarValue(i++);
            }

            GameChecksums new_checksums = new()
            {
                files = finalFiles
            };

            return new_checksums;
        }

        private async Task CompressNewFilesAsync(string final_game_dir, List<GameFile> changedFiles)
        {
            SetProgressBarMax(changedFiles.Count);
            SetProgressBarValue(0);

            int i = 0;
            var tasks = changedFiles.Select(file => Task.Run(async () =>
            {
                try
                {
                    // Normalize the path separators to ensure consistent comparison
                    string normalizedPath = file.name.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

                    // Copy the file to the base game directory
                    var sourceFile = Path.Combine(textBox1.Text, file.name);
                    var destFile = Path.Combine(final_game_dir, file.name + ".zst");

                    if (!File.Exists(sourceFile))
                    {
                        Log($"File not found: {sourceFile}");
                        return;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);

                    // Compress and move the file asynchronously
                    await CompressFileAsync(sourceFile, destFile);

                    SetProgressBarValue(i++);
                    Log($"Compressed file: {sourceFile}");
                }
                catch (Exception ex)
                {
                    SetProgressBarValue(i++);
                    Log($"Error compressing file {file.name}: {ex.Message}");
                }
            }));

            // Wait for all tasks to complete
            await Task.WhenAll(tasks);
        }

        private GameChecksums UpdateZSTChecksums(GameChecksums local_zst_checksums, GameChecksums server_zst_checksums)
        {
            SetProgressBarMax(local_zst_checksums.files.Count);
            SetProgressBarValue(0);

            List<GameFile> compressed_removedFiles = server_zst_checksums.files.Where(currentFile => !local_zst_checksums.files.Any(updatedFile => updatedFile.name == currentFile.name)).ToList();

            List<GameFile> compressed_finalFiles = server_zst_checksums.files.Where(file => !compressed_removedFiles.Any(removed => removed.name + ".zst" == file.name)).ToList();

            int i = 0;
            foreach (var updatedFile in local_zst_checksums.files)
            {
                // Check if the file already exists in the final list
                var existingFile = compressed_finalFiles.FirstOrDefault(f => f.name == updatedFile.name);
                if (existingFile != null)
                {
                    // Replace the existing file
                    compressed_finalFiles.Remove(existingFile);
                }
                // Add the updated file
                compressed_finalFiles.Add(updatedFile);
                SetProgressBarValue(i++);
            }

            GameChecksums new_compressed_checksums = new()
            {
                files = compressed_finalFiles
            };

            return new_compressed_checksums;
        }

        private async Task<GameChecksums> GetCompressedChecksums(string final_game_dir)
        {
            var compressedFiles = Directory.GetFiles(final_game_dir, "*.zst", SearchOption.AllDirectories)
                .Where(file => !Global.ignoredFiles.Any(ignored =>
                    Path.GetFileName(file).Equals(ignored, StringComparison.OrdinalIgnoreCase)))
                .ToArray();

            SetProgressBarMax(compressedFiles.Length);
            SetProgressBarValue(0);

            int i = 0;
            // Process files asynchronously
            var tasks = compressedFiles.Select(filePath => Task.Run(async () =>
            {
                try
                {
                    // Compute checksum asynchronously
                    string relativePath = Path.GetRelativePath(final_game_dir, filePath);
                    string checksum = await CalculateChecksumAsync(filePath);

                    Log($"Processed file: {relativePath} ({checksum})");
                    SetProgressBarValue(i++);

                    return new GameFile
                    {
                        name = relativePath,
                        checksum = checksum,
                        size = new FileInfo(filePath).Length
                    };
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing file {filePath}: {ex.Message}");
                    SetProgressBarValue(i++);
                    return null; // Return null if processing fails
                }
            }));

            // Wait for all tasks to complete and add results to the checksum list
            var processedFiles = await Task.WhenAll(tasks);

            GameChecksums new_compressed_checksums_resault = new()
            {
                files = processedFiles.ToList()
            };

            return new_compressed_checksums_resault;
        }

        private void UpdateClearCacheList(int selected_index, List<GameFile> changed_files, string final_dir)
        {
            SetProgressBarMax(changed_files.Count);
            SetProgressBarValue(0);

            List<string> changed_files_txt = [];
            foreach (var file in changed_files)
            {
                changed_files_txt.Add(file.name);
            }

            File.WriteAllLines(final_dir + "\\clearcache.txt", changed_files_txt);

            richTextBox1.Invoke(() =>
            {
                richTextBox1.Lines = changed_files_txt.ToArray();
            });
        }

        public void Log(string message)
        {
            Console.WriteLine(message);
        }

        public async Task<GameChecksums> GenerateMetadataAsync(string directory)
        {
            string[] files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
                .Where(file => !Global.BLACKLIST.Any(blacklistItem =>
                    file.Contains(blacklistItem, StringComparison.OrdinalIgnoreCase)))
                .ToArray();

            SetProgressBarMax(files.Length);
            SetProgressBarValue(0);

            // Create tasks to process files concurrently
            int i = 0;
            var tasks = files.Select(filePath => Task.Run(async () =>
            {
                try
                {
                    // Compute checksum asynchronously
                    string relativePath = Path.GetRelativePath(directory, filePath);
                    string checksum = await CalculateChecksumAsync(filePath);

                    Log($"Processed file: {relativePath} ({checksum})");
                    SetProgressBarValue(i++);

                    return new GameFile
                    {
                        name = relativePath,
                        checksum = checksum,
                        size = new FileInfo(filePath).Length
                    };
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing file {filePath}: {ex.Message}");
                    SetProgressBarValue(i++);
                    return null; // Return null in case of an error
                }
            }));

            // Wait for all tasks to complete
            var results = await Task.WhenAll(tasks);

            // Filter out null results and construct the GameChecksums object
            var gameChecksums = new GameChecksums
            {
                files = results.ToList()
            };

            return gameChecksums;
        }

        private static async Task<string> CalculateChecksumAsync(string filePath)
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
            using (var sha256 = SHA256.Create())
            {
                var hash = await sha256.ComputeHashAsync(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        public async Task CompressFileAsync(string input_file, string output_file)
        {
            using var delta_temp_input = File.OpenRead(input_file);
            using var delta_compressed_output = File.OpenWrite(output_file);
            using var compressionStream = new CompressionStream(delta_compressed_output, (int)compressionLevel.Value);
            await delta_temp_input.CopyToAsync(compressionStream);
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
        }

        private async void button4_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(AUTH_TOKEN) || string.IsNullOrEmpty(ZONE_ID))
                return;

            string url = $"https://api.cloudflare.com/client/v4/zones/{ZONE_ID}/purge_cache";

            var payload = new
            {
                purge_everything = true
            };

            string jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);

            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AUTH_TOKEN);

                HttpResponseMessage response = await Global.HTTP_CLIENT.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Cache has been purged");
                    string responseBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Response from Cloudflare:");
                    Console.WriteLine(responseBody);
                }
                else
                {
                    MessageBox.Show("Failed to purge cache");
                    string errorBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error: {response.StatusCode}");
                    Console.WriteLine(errorBody);
                }
            }
        }

        private async void PurgeListBtn_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(AUTH_TOKEN) || string.IsNullOrEmpty(ZONE_ID))
                return;

            string[] purge_list = richTextBox1.Lines;

            // Split the purge list into chunks of 30
            List<List<string>> purge_lists = new List<List<string>>();

            for (int i = 0; i < purge_list.Length; i += 30)
            {
                purge_lists.Add(purge_list.Skip(i).Take(30).ToList());
            }

            string url = $"https://api.cloudflare.com/client/v4/zones/{ZONE_ID}/purge_cache";

            bool did_fail = false;

            foreach (var list in purge_lists)
            {
                var payload = new
                {
                    files = list
                };

                string jsonPayload = JsonSerializer.Serialize(payload);

                using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AUTH_TOKEN);

                    HttpResponseMessage response = await Global.HTTP_CLIENT.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        Console.WriteLine("Response from Cloudflare:");
                        Console.WriteLine(responseBody);
                    }
                    else
                    {
                        string errorBody = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Error: {response.StatusCode}");
                        Console.WriteLine(errorBody);
                        did_fail = true;
                    }
                }
            }

            if (!did_fail)
            {
                MessageBox.Show("Files have been purged from cache");
            }
            else
            {
                MessageBox.Show("Failed to purge cache");
            }
        }
    }

    public class CFConfig
    {
        public string zoneID { get; set; }
        public string authKey { get; set; }
    }
}