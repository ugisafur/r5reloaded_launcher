using patch_creator.Models;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace patch_creator.Services
{
    public class PatchService
    {
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

        private readonly Action<string> _log;
        private readonly Action<int> _setProgressBarMax;
        private readonly Action<int> _setProgressBarValue;
        private readonly Action<string> _updateProgressLabel;

        public RichTextBox cloudflarePurgeList;

        public PatchService(Action<string> log, Action<int> setProgressBarMax, Action<int> setProgressBarValue, Action<string> updateProgressLabel)
        {
            _log = log;
            _setProgressBarMax = setProgressBarMax;
            _setProgressBarValue = setProgressBarValue;
            _updateProgressLabel = updateProgressLabel;
        }

        public async Task CreatePatchAsync(string sourceDir, string outputDir, GameManifest serverChecksums, string[] ignoreStrings, int maxDop, string gameVersion, string blogSlug, ReleaseChannel releaseChannel)
        {
            _updateProgressLabel("Creating directory");
            Directory.CreateDirectory(outputDir);

            _updateProgressLabel("Generating local checksums");
            GameManifest localChecksums = await GenerateMetadataAsync(sourceDir, ignoreStrings, maxDop);

            _updateProgressLabel("Finding changed files");
            List<ManifestEntry> changedFiles = localChecksums.files.Where(updatedFile => !serverChecksums.files.Any(currentFile => currentFile.path == updatedFile.path && currentFile.checksum == updatedFile.checksum)).ToList();

            _setProgressBarMax(localChecksums.files.Count);
            _setProgressBarValue(0);

            _updateProgressLabel("Copying over files");
            int processedCount = 0;
            await Parallel.ForEachAsync(localChecksums.files, new ParallelOptions { MaxDegreeOfParallelism = maxDop }, async (file, cancellationToken) =>
            {
                if (!changedFiles.Any(f => f.path == file.path) || file.checksum == "ignore")
                {
                    ManifestEntry serverFile = serverChecksums.files.FirstOrDefault(f => f.path == file.path);

                    if (serverFile == null)
                        return;

                    file.checksum = serverFile.checksum;
                    file.size = serverFile.size;
                    file.optional = serverFile.optional;
                    file.parts = serverFile.parts;
                    file.language = null;

                    if (file.path.Contains("audio\\ship\\") && !audioFiles.Contains(file.path))
                    {
                        string lang_name = Path.GetFileNameWithoutExtension(file.path).Replace("general_", "").Replace("_patch_1", "").Replace("_patch_2", "").Replace("_patch_3", "").Replace("_patch_4", "");
                        if (!localChecksums.languages.Contains(lang_name))
                        {
                            localChecksums.languages.Add(lang_name);
                        }
                        file.language = lang_name;
                    }
                    return;
                }

                await ProcessFileAsync(file, sourceDir, outputDir, localChecksums);

                int currentCount = Interlocked.Increment(ref processedCount);
                _setProgressBarValue(currentCount);
            });

            localChecksums.game_version = gameVersion;
            localChecksums.blog_slug = blogSlug;

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var game_checksums_file = JsonSerializer.Serialize(localChecksums, options);
            await File.WriteAllTextAsync(Path.Combine(outputDir, "checksums.json"), game_checksums_file);

            UpdateClearCacheList(releaseChannel, changedFiles, outputDir, ignoreStrings);

            if (!string.IsNullOrEmpty(gameVersion))
                await File.WriteAllTextAsync(Path.Combine(outputDir, "version.txt"), gameVersion);
        }

        private async Task ProcessFileAsync(ManifestEntry file, string sourceDir, string outputDir, GameManifest localChecksums)
        {
            List<FileChunk> fileChunks = new List<FileChunk>();
            string sourceFilePath = Path.Combine(sourceDir, file.path);

            try
            {
                if (!File.Exists(sourceFilePath))
                {
                    _log($"Error: Source file not found: {sourceFilePath}");
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
                        string partFilePath = Path.Combine(outputDir, partFileName);

                        Directory.CreateDirectory(Path.GetDirectoryName(partFilePath));

                        await using (var destinationStream = new FileStream(partFilePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
                        {
                            await CopyStreamSegmentAsync(sourceStream, destinationStream, bytesToReadForPart);
                        }

                        string part_checksum = await CalculateChecksumAsync(partFilePath);

                        fileChunks.Add(new FileChunk
                        {
                            path = partFileName,
                            checksum = part_checksum,
                            size = bytesToReadForPart
                        });

                        _log($"Created part: {partFileName} ({bytesToReadForPart / 1024 / 1024} MB)");
                        remainingBytes -= bytesToReadForPart;
                        partNumber++;
                    }
                }
                else
                {
                    string partFilePath = Path.Combine(outputDir, file.path);
                    Directory.CreateDirectory(Path.GetDirectoryName(partFilePath));
                    File.Copy(sourceFilePath, partFilePath, true);
                    _log($"Copied file: {file.path}");
                }

                file.parts = fileChunks.Count > 0 ? fileChunks : null;

                if (file.path.Contains("audio\\ship\\") && !audioFiles.Contains(file.path))
                {
                    string lang_name = Path.GetFileNameWithoutExtension(file.path).Replace("general_", "").Replace("_patch_1", "");
                    if (!localChecksums.languages.Contains(lang_name))
                    {
                        _log($"Adding language: {lang_name}");
                        localChecksums.languages.Add(lang_name);
                    }
                    file.language = lang_name;
                }
                else
                {
                    file.language = null;
                }
            }
            catch (Exception ex)
            {
                _log($"!! FAILED to process file {sourceFilePath}. Error: {ex.Message}");
            }
        }

        private async Task CopyStreamSegmentAsync(Stream source, Stream destination, long count)
        {
            byte[] buffer = new byte[81920];
            long totalBytesRead = 0;

            while (totalBytesRead < count)
            {
                int bytesToRead = (int)Math.Min(buffer.Length, count - totalBytesRead);
                int bytesRead = await source.ReadAsync(buffer, 0, bytesToRead);

                if (bytesRead == 0) break;

                await destination.WriteAsync(buffer, 0, bytesRead);
                totalBytesRead += bytesRead;
            }
        }

        public async Task<GameManifest> GenerateMetadataAsync(string directory, string[] ignoreStrings, int maxDop)
        {
            string[] files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
                .Where(file => !BLACKLIST.Any(blacklistItem => file.Contains(blacklistItem, StringComparison.OrdinalIgnoreCase)))
                .ToArray();

            _setProgressBarMax(files.Length);
            _setProgressBarValue(0);

            var resultsBag = new ConcurrentBag<ManifestEntry>();
            int processedCount = 0;

            await Parallel.ForEachAsync(files, new ParallelOptions { MaxDegreeOfParallelism = maxDop }, async (filePath, cancellationToken) =>
            {
                try
                {
                    string relativePath = Path.GetRelativePath(directory, filePath);
                    string filename = Path.GetFileName($"{directory}\\{filePath}");
                    string checksum = "ignore";

                    bool shouldIgnore = ignoreStrings.Any(s => relativePath.Contains(s.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (!shouldIgnore)
                        checksum = await CalculateChecksumAsync(filePath);

                    resultsBag.Add(new ManifestEntry
                    {
                        path = relativePath,
                        checksum = checksum,
                        optional = filename.Contains(".opt.starpak") ? true : null,
                        size = new FileInfo(filePath).Length
                    });

                    _log(shouldIgnore ? $"Ignoring Checksum check on file: {relativePath}" : $"Processed file: {relativePath} ({checksum})");
                }
                catch (Exception ex)
                {
                    _log($"Error processing file {filePath}: {ex.Message}");
                }
                finally
                {
                    int currentCount = Interlocked.Increment(ref processedCount);
                    _setProgressBarValue(currentCount);
                }
            });

            return new GameManifest { files = resultsBag.ToList() };
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

        public void UpdateClearCacheList(ReleaseChannel releaseChannel, List<ManifestEntry> changedFiles, string finalDir, string[] ignoreStrings)
        {
            _updateProgressLabel("Updating clear cache list");
            _setProgressBarMax(changedFiles.Count);
            _setProgressBarValue(0);

            List<string> changedFilesTxt = new List<string>
            {
                $"{releaseChannel.game_url}/checksums.json",
                $"{releaseChannel.game_url}/version.txt"
            };

            for (int i = 0; i < changedFiles.Count; i++)
            {
                var file = changedFiles[i];
                bool shouldIgnore = ignoreStrings.Any(s => file.path.Contains(s.Trim(), StringComparison.OrdinalIgnoreCase));

                if (!shouldIgnore)
                    changedFilesTxt.Add($"{releaseChannel.game_url}/{file.path}");

                _setProgressBarValue(i + 1);
            }

            File.WriteAllLines(Path.Combine(finalDir, "clearcache.txt"), changedFilesTxt);

            cloudflarePurgeList.Invoke(() => { cloudflarePurgeList.Lines = changedFilesTxt.ToArray(); });
        }
    }
} 