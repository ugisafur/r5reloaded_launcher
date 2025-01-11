using Newtonsoft.Json;
using Octodiff.Core;
using Octodiff.Diagnostics;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using ZstdSharp;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace patch_creator
{
    public partial class Form1 : Form
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        private List<string> patchFiles = new List<string>();
        private HttpClient client = new HttpClient();

        private readonly string[] whitelistPatchPaths = new string[] {
            "vpk",
            "stbsp",
            "paks\\Win64",
            "audio\\ship",
        };

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            AllocConsole();
        }

        private void button3_Click(object sender, EventArgs e)
        {
        }

        private void button2_Click(object sender, EventArgs e)
        {
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Task.Run(() => CreatePatch());
        }

        private async void CreatePatch()
        {
            Log("---------- Patch creation started ----------");
            Patch patch = new Patch();
            patch.files = new List<PatchFile>();

            //Setup final directories for the base game and current patch
            var final_patch_dir = textBox2.Text + "\\patch";
            var final_game_dir = textBox2.Text + "\\game";

            // Create the final directories if they don't exist
            Directory.CreateDirectory(final_patch_dir);
            Directory.CreateDirectory(final_game_dir);

            //Get current checksums.json file
            var response = await client.GetStringAsync("https://cdn.r5r.org/launcher/base_game/checksums.json");
            GameChecksums checksums = JsonConvert.DeserializeObject<GameChecksums>(response);

            //Get updated checksums.json file
            var updated_checksums = Task.Run(() => GenerateMetadata(textBox1.Text));
            GameChecksums updated_checksums_result = await updated_checksums;

            //Find the changed files
            var changedFiles = updated_checksums_result.files.Where(updatedFile => !checksums.files.Any(currentFile => currentFile.name == updatedFile.name && currentFile.checksum == updatedFile.checksum)).ToList();

            // Find removed files (present in the current checksums but not in the updated one)
            var removedFiles = checksums.files.Where(currentFile => !updated_checksums_result.files.Any(updatedFile => updatedFile.name == currentFile.name)).ToList();

            var finalFiles = checksums.files.Where(file => !removedFiles.Any(removed => removed.name == file.name)).ToList();

            foreach (var updatedFile in updated_checksums_result.files)
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
            }

            GameChecksums new_checksums = new();
            new_checksums.files = finalFiles;

            //Compress and move the changed files to the base game directory
            foreach (var file in changedFiles)
            {
                // Normalize the path separators to ensure consistent comparison
                string normalizedPath = file.name.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

                // Copy the file to the base game directory
                var sourceFile = Path.Combine(textBox1.Text, file.name);
                var destFile = Path.Combine(final_game_dir, file.name + ".zst");

                if (!File.Exists(sourceFile))
                {
                    Log($"File not found: {sourceFile}");
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destFile));

                //compress and move the file
                CompressFile(sourceFile, destFile);

                //Copy destFile to patch directory
                Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(final_patch_dir, file.name + ".zst")));
                File.Copy(destFile, Path.Combine(final_patch_dir, file.name + ".zst"), overwrite: true);

                Log("Compressing file: " + sourceFile);
            }

            //Add the changed files to the patch
            foreach (var file in changedFiles)
            {
                PatchFile patchFile = new PatchFile
                {
                    Name = file.name,
                    Action = "update"
                };

                patch.files.Add(patchFile);
            }

            //Add the removed files to the patch
            foreach (var file in removedFiles)
            {
                PatchFile patchFile = new PatchFile();
                patchFile.Name = file.name;
                patchFile.Action = "delete";

                patch.files.Add(patchFile);
            }

            //Create a json file with the list of patched files
            var json_patch_file = JsonSerializer.Serialize(patch, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(final_patch_dir + "\\patch.json", json_patch_file);

            var game_checksums_file = JsonSerializer.Serialize(new_checksums, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(final_game_dir + "\\checksums.json", game_checksums_file);

            //Get Compressed File Checksums
            var ignoredFiles = new List<string>
            {
                "checksums.json",
                "checksums_zst.json"
            };

            var compressedFiles = Directory.GetFiles(final_game_dir, "*.zst", SearchOption.AllDirectories)
            .Where(file => !ignoredFiles.Any(ignored =>
                Path.GetFileName(file).Equals(ignored, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

            GameChecksums new_compressed_checksums_resault = new();
            new_compressed_checksums_resault.files = new List<GameFile>();

            Parallel.ForEach(compressedFiles, filePath =>
            {
                try
                {
                    // Compute checksum
                    string relativePath = Path.GetRelativePath(final_game_dir, filePath);
                    string checksum = CalculateChecksum(filePath);

                    Log($"Processed file: {relativePath} ({checksum})");

                    GameFile gameFile = new GameFile();
                    gameFile.name = relativePath;
                    gameFile.checksum = checksum;

                    new_compressed_checksums_resault.files.Add(gameFile);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing file {filePath}: {ex.Message}");
                }
            });

            var compressedresponse = await client.GetStringAsync("https://cdn.r5r.org/launcher/base_game/checksums_zst.json");
            GameChecksums conmpressed_checksums = JsonConvert.DeserializeObject<GameChecksums>(compressedresponse);

            //Find the changed files
            var compressed_changedFiles = new_compressed_checksums_resault.files.Where(updatedFile => !conmpressed_checksums.files.Any(currentFile => currentFile.name == updatedFile.name && currentFile.checksum == updatedFile.checksum)).ToList();

            // Find removed files (present in the current checksums but not in the updated one)
            var compressed_removedFiles = conmpressed_checksums.files.Where(currentFile => !new_compressed_checksums_resault.files.Any(updatedFile => updatedFile.name == currentFile.name)).ToList();

            var compressed_finalFiles = conmpressed_checksums.files.Where(file => !removedFiles.Any(removed => removed.name + ".zst" == file.name)).ToList();

            foreach (var updatedFile in new_compressed_checksums_resault.files)
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
            }

            GameChecksums new_compressed_checksums = new();
            new_compressed_checksums.files = compressed_finalFiles;

            var compressed_game_checksums_file = JsonSerializer.Serialize(new_compressed_checksums, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(final_game_dir + "\\checksums_zst.json", compressed_game_checksums_file);
        }

        public void ProcessDeltasWithProgress(List<string> changedFiles, string patch_files)
        {
            // Use Parallel.ForEach for multithreading
            Parallel.ForEach(changedFiles, (changedFile) =>
            {
                // Create the delta for the current file
                CreateFileDelta(
                    Path.Combine(textBox1.Text, changedFile),
                    Path.Combine(textBox2.Text, changedFile),
                    Path.Combine(patch_files, changedFile + ".delta")
                );
            });
        }

        public void Log(string message)
        {
            /*logBox.Invoke((Action)(() =>
            {
                logBox.AppendText(message + Environment.NewLine);
                logBox.SelectionStart = logBox.Text.Length;
                logBox.ScrollToCaret();
            }));*/

            Console.WriteLine(message);
        }

        public GameChecksums GenerateMetadata(string directory)
        {
            var metadata = new ConcurrentDictionary<string, string>();

            var normalizedIgnorePaths = whitelistPatchPaths
                .Select(p => Path.GetFullPath(Path.Combine(directory, p)).TrimEnd(Path.DirectorySeparatorChar))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
    .Where(file => !file.Contains(Path.Combine("platform", "logs")))
    .Where(file => !file.Contains(Path.Combine("platform", "cfg", "user")))
    .Where(file => !file.Contains("layout.ini"))
    .Where(file => !file.Contains("startup.bin"))
    .Where(file => !file.Contains("launcher.vdf"))
    .Where(file => !file.Contains("launcherConfig.ini"))
    .ToArray();

            GameChecksums gameChecksums = new GameChecksums();
            gameChecksums.files = new List<GameFile>();

            Parallel.ForEach(files, filePath =>
            {
                try
                {
                    // Compute checksum
                    string relativePath = Path.GetRelativePath(directory, filePath);
                    string checksum = CalculateChecksum(filePath);

                    Log($"Processed file: {relativePath} ({checksum})");

                    GameFile gameFile = new GameFile();
                    gameFile.name = relativePath;
                    gameFile.checksum = checksum;

                    gameChecksums.files.Add(gameFile);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing file {filePath}: {ex.Message}");
                }
            });

            return gameChecksums;
        }

        private static string CalculateChecksum(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        public void CompressFile(string input_file, string output_file)
        {
            using var delta_temp_input = File.OpenRead(input_file);
            using var delta_compressed_output = File.OpenWrite(output_file);
            using var compressionStream = new CompressionStream(delta_compressed_output, 12);
            delta_temp_input.CopyTo(compressionStream);
        }

        public void CreateFileDelta(string originalFile, string updatedFile, string deltaFile)
        {
            // Ensure the delta directory exists
            if (!Directory.Exists(Path.GetDirectoryName(deltaFile)))
                Directory.CreateDirectory(Path.GetDirectoryName(deltaFile));

            // Check if the original file exists
            if (!File.Exists(originalFile))
            {
                // If the original file does not exist, it's a new file
                Log($"New file detected: {updatedFile}. Copying instead of creating delta.");
                CompressFile(updatedFile, deltaFile);
                return;
            }

            // Create a temporary signature file
            var signatureFile = Path.GetTempFileName();

            // Step 1: Generate the signature file from the original file
            var signatureBuilder = new SignatureBuilder();
            using (var basisStream = new FileStream(originalFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var signatureStream = new FileStream(signatureFile, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                signatureBuilder.Build(basisStream, new SignatureWriter(signatureStream));
            }

            Log($"Created signature file: {signatureFile}");

            // Step 2: Create the delta file
            var deltaBuilder = new DeltaBuilder();
            using (var newFileStream = new FileStream(updatedFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var signatureFileStream = new FileStream(signatureFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var deltaStream = new FileStream(deltaFile + "_temp", FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                var deltaWriter = new BinaryDeltaWriter(deltaStream);
                var compressedWriter = new AggregateCopyOperationsDecorator(deltaWriter);
                deltaBuilder.BuildDelta(newFileStream, new SignatureReader(signatureFileStream, new ConsoleProgressReporter()), compressedWriter);
            }

            Log($"Created delta file: {deltaFile}");

            // Step 3: Compress the delta file
            CompressFile(deltaFile + "_temp", deltaFile);

            Log($"Compressed delta file: {deltaFile}");

            // Clean up the temporary delta file
            File.Delete(deltaFile + "_temp");

            // Clean up the temporary signature file
            File.Delete(signatureFile);
        }
    }

    internal class Patch
    {
        public List<PatchFile> files { get; set; }
    }

    internal class PatchFile
    {
        public string Name { get; set; }
        public string Action { get; set; }
    }

    public class GameChecksums
    {
        public List<GameFile> files { get; set; }
    }

    public class GameFile
    {
        public string name { get; set; }
        public string checksum { get; set; }
    }
}