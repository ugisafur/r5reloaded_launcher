using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using patch_creator.Models;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms.VisualStyles;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace patch_creator
{
    public partial class MainWIndow : Form
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        private string? AUTH_TOKEN;
        private string? ZONE_ID;

        public static HttpClient HTTP_CLIENT = new();
        public static RemoteConfig REMOTE_CONFIG = new();

        public static readonly string[] BLACKLIST = {
            "platform\\logs",
            "platform\\screenshots",
            "platform\\user",
            "platform\\cfg\\user",
            "launcher.exe",
            "bin\\updater.exe",
            "cfg\\startup.bin",
            "launcher_data"
        };

        public static List<string> ignoredFiles = new()
        {
            "checksums.json",
            "checksums_zst.json",
            "clearcache.txt"
        };

        public static List<string> audioFiles = new()
        {
            "audio\\ship\\audio.mprj",
            "audio\\ship\\general.mbnk",
            "audio\\ship\\general.mbnk_digest",
            "audio\\ship\\general_stream.mstr",
            "audio\\ship\\general_stream_patch_1.mstr",
            "audio\\ship\\general_english.mstr",
            "audio\\ship\\general_english_patch_1.mstr"
        };

        const long PartSize = 490 * 1024 * 1024;

        ParallelOptions parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = 500 // Start with half the CPU cores, minimum of 1
        };

        public MainWIndow()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            AllocConsole();

            var response = HTTP_CLIENT.GetAsync("https://cdn.r5r.org/launcher/config.json").Result;
            var responseString = response.Content.ReadAsStringAsync().Result;
            REMOTE_CONFIG = JsonConvert.DeserializeObject<RemoteConfig>(responseString);

            foreach (ReleaseChannel channel in REMOTE_CONFIG.channels)
            {
                comboBox1.Items.Add(channel.name);
            }

            comboBox1.SelectedIndexChanged +=ComboBox1_SelectedIndexChanged;
            comboBox1.SelectedIndex = 0;

            concurrentTasks.Value = 500;

            LoadConfig();
        }

        private void ComboBox1_SelectedIndexChanged(object? sender, EventArgs e)
        {
            var response = HTTP_CLIENT.GetStringAsync(Path.Combine(REMOTE_CONFIG.channels[comboBox1.SelectedIndex].game_url, "checksums.json")).Result;
            GameManifest server_checksums = JsonConvert.DeserializeObject<GameManifest>(response);

            versionTxt.Text = server_checksums.game_version;
            blogslugTxt.Text = server_checksums.blog_slug;
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
                    textBox2.Text = Path.Combine(parentDir.FullName, $"{REMOTE_CONFIG.channels[comboBox1.SelectedIndex].name}_update");
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

            parallelOptions.MaxDegreeOfParallelism = (int)concurrentTasks.Value;

            int selected_index = 0;
            comboBox1.Invoke(() => { selected_index = comboBox1.SelectedIndex; });

            UpdateProgressLabel("Creating directory");
            var final_game_dir = textBox2.Text;
            Directory.CreateDirectory(final_game_dir);

            string[] IgnoreStrings = [];
            richTextBox2.Invoke(() => { IgnoreStrings = richTextBox2.Lines; });

            UpdateProgressLabel("Getting server checksums");
            var response = await HTTP_CLIENT.GetStringAsync(Path.Combine(REMOTE_CONFIG.channels[selected_index].game_url, "checksums.json"));
            GameManifest server_checksums = JsonConvert.DeserializeObject<GameManifest>(response);

            UpdateProgressLabel("Generating local checksums");
            GameManifest local_checksums = await GenerateMetadataAsync(textBox1.Text, IgnoreStrings);

            UpdateProgressLabel("Finding changed files");
            List<ManifestEntry> changedFiles = local_checksums.files.Where(updatedFile => !server_checksums.files.Any(currentFile => currentFile.path == updatedFile.path && currentFile.checksum == updatedFile.checksum)).ToList();

            UpdateProgressLabel("Copying over files");
            int processedCount = 0;
            await Parallel.ForEachAsync(local_checksums.files, parallelOptions, async (file, cancellationToken) =>
            {
                if (!changedFiles.Any(f => f.path == file.path) || file.checksum == "ignore")
                {
                    ManifestEntry serverFile = server_checksums.files.FirstOrDefault(f => f.path == file.path);

                    if (serverFile == null)
                        return;

                    file.checksum = serverFile.checksum;
                    file.size = serverFile.size;

                    if (serverFile.optional != null && (bool)serverFile.optional)
                        file.optional = serverFile.optional;
                    else
                        file.optional = null;

                    if (serverFile.parts != null && serverFile.parts.Count > 0)
                        file.parts = serverFile.parts;
                    else
                        file.parts = null;

                    if (file.path.Contains("audio\\ship\\") && !audioFiles.Contains(file.path))
                    {
                        string lang_name = Path.GetFileNameWithoutExtension(file.path).Replace("general_", "").Replace("_patch_1", "").Replace("_patch_2", "").Replace("_patch_3", "").Replace("_patch_4", "");
                        if (!local_checksums.languages.Contains(lang_name))
                        {
                            Console.WriteLine($"Adding language: {lang_name}");
                            local_checksums.languages.Add(lang_name);
                        }

                        file.language = lang_name;
                    }
                    else
                    {
                        file.language = null;
                    }

                    return;
                }

                List<FileChunk> FileChunks = new List<FileChunk>();
                string sourceFilePath = Path.Combine(textBox1.Text, file.path);

                try
                {
                    if (!File.Exists(sourceFilePath))
                    {
                        Console.WriteLine($"Error: Source file not found: {sourceFilePath}");
                        return;
                    }

                    if (file.size > PartSize)
                    {
                        await using var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
                        long remainingBytes = (long)file.size;
                        int partNumber = 0;
                        while (remainingBytes > 0)
                        {
                            long bytesToReadForPart = Math.Min(remainingBytes, PartSize);
                            string partFileName = $"{file.path}.p{partNumber}";
                            string partFilePath = Path.Combine(final_game_dir, partFileName);

                            Directory.CreateDirectory(Path.GetDirectoryName(partFilePath));

                            await using (var destinationStream = new FileStream(partFilePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
                            {
                                await CopyStreamSegmentAsync(sourceStream, destinationStream, bytesToReadForPart);
                            }

                            string part_checksum = await CalculateChecksumAsync(partFilePath);

                            FileChunks.Add(new FileChunk
                            {
                                path = partFileName,
                                checksum = part_checksum,
                                size = bytesToReadForPart
                            });

                            Console.WriteLine($"Created part: {partFileName} ({bytesToReadForPart / 1024 / 1024} MB)");
                            remainingBytes -= bytesToReadForPart;
                            partNumber++;
                        }
                    }
                    else
                    {
                        string partFilePath = Path.Combine(final_game_dir, file.path);
                        Directory.CreateDirectory(Path.GetDirectoryName(partFilePath));

                        File.Copy(sourceFilePath, partFilePath, true); 

                        Console.WriteLine($"Copied file: {file.path}");
                    }


                    if (FileChunks.Count > 0)
                        file.parts = FileChunks;
                    else
                        file.parts = null;

                    if (file.path.Contains("audio\\ship\\") && !audioFiles.Contains(file.path))
                    {
                        string lang_name = Path.GetFileNameWithoutExtension(file.path).Replace("general_", "").Replace("_patch_1", "");
                        if (!local_checksums.languages.Contains(lang_name))
                        {
                            Console.WriteLine($"Adding language: {lang_name}");
                            local_checksums.languages.Add(lang_name);
                        }

                        file.language = lang_name;
                    }
                    else
                    {
                        file.language = null;
                    }

                    int currentCount = Interlocked.Increment(ref processedCount);
                    SetProgressBarValue(currentCount);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"!! FAILED to process file {sourceFilePath}. Error: {ex.Message}");
                }
            });

            local_checksums.game_version = versionTxt.Text;
            local_checksums.blog_slug = blogslugTxt.Text;

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var game_checksums_file = JsonSerializer.Serialize(local_checksums, options);
            await File.WriteAllTextAsync(final_game_dir + "\\checksums.json", game_checksums_file);
     
            UpdateProgressLabel("Updating clear cache list");
            UpdateClearCacheList(selected_index, changedFiles, final_game_dir, IgnoreStrings);

            if (!string.IsNullOrEmpty(versionTxt.Text))
                File.WriteAllText(final_game_dir + "\\version.txt", versionTxt.Text);

            UpdateProgressLabel("Patch creation complete");
            Log("---------- Patch creation finished ----------");
            SetAppState(false);
        }

        private async Task CopyStreamSegmentAsync(Stream source, Stream destination, long count)
        {
            byte[] buffer = new byte[81920];
            long totalBytesRead = 0;

            while (totalBytesRead < count)
            {
                int bytesToRead = (int)Math.Min(buffer.Length, count - totalBytesRead);
                int bytesRead = await source.ReadAsync(buffer, 0, bytesToRead);

                if (bytesRead == 0)
                {
                    break;
                }

                await destination.WriteAsync(buffer, 0, bytesRead);

                totalBytesRead += bytesRead;
            }
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
                richTextBox2.ReadOnly = running;
                versionTxt.ReadOnly = running;
                blogslugTxt.ReadOnly = running;
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

        private void UpdateClearCacheList(int selected_index, List<ManifestEntry> changed_files, string final_dir, string[] IgnoreStrings)
        {
            SetProgressBarMax(changed_files.Count - 1);
            SetProgressBarValue(0);

            List<string> changed_files_txt = [
                $"{REMOTE_CONFIG.channels[selected_index].game_url}/checksums.json", 
                $"{REMOTE_CONFIG.channels[selected_index].game_url}/version.txt"
            ];

            int i = 0;
            foreach (var file in changed_files)
            {
                bool shouldIgnore = IgnoreStrings.Any(s => file.path.Contains(s.Trim(), StringComparison.OrdinalIgnoreCase));

                if(!shouldIgnore)
                    changed_files_txt.Add($"{REMOTE_CONFIG.channels[selected_index].game_url}/{file.path}");

                SetProgressBarValue(i++);
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

        public async Task<GameManifest> GenerateMetadataAsync(string directory, string[] IgnoreStrings)
        {
            string[] files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
                .Where(file => !BLACKLIST.Any(blacklistItem => file.Contains(blacklistItem, StringComparison.OrdinalIgnoreCase)))
                .ToArray();

            SetProgressBarMax(files.Length);
            SetProgressBarValue(0);

            var resultsBag = new ConcurrentBag<ManifestEntry>();

            int processedCount = 0;
            await Parallel.ForEachAsync(files, parallelOptions, async (filePath, cancellationToken) =>
            {
                try
                {
                    string relativePath = Path.GetRelativePath(directory, filePath);
                    string filename = Path.GetFileName($"{directory}\\{filePath}");
                    string checksum = "ignore";

                    bool shouldIgnore = IgnoreStrings.Any(s => relativePath.Contains(s.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (!shouldIgnore)
                        checksum = await CalculateChecksumAsync(filePath);

                    var ManifestEntry = new ManifestEntry
                    {
                        path = relativePath,
                        checksum = checksum,
                        optional = filename.Contains(".opt.starpak") ? true : null,
                        size = new FileInfo(filePath).Length
                    };

                    resultsBag.Add(ManifestEntry);

                    if(shouldIgnore)
                    {
                        Log($"Ignoring Checksum check on file: {relativePath}");
                    }
                    else
                    {
                        Log($"Processed file: {relativePath} ({checksum})");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing file {filePath}: {ex.Message}");
                }
                finally
                {
                    int currentCount = Interlocked.Increment(ref processedCount);
                    SetProgressBarValue(currentCount);
                }
            });

            var GameManifest = new GameManifest
            {
                files = resultsBag.ToList()
            };

            return GameManifest;
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

                HttpResponseMessage response = await HTTP_CLIENT.SendAsync(request);

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
            List<List<string>> purge_lists = new();

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

                    HttpResponseMessage response = await HTTP_CLIENT.SendAsync(request);

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
        public string? zoneID { get; set; }
        public string? authKey { get; set; }
    }
}