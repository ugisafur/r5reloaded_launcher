using launcher.GameLifecycle.Models;
using launcher.Services;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;

using static launcher.Core.AppContext;
using static launcher.Services.LoggerService;

namespace launcher.Game
{
    public static class ChecksumManager
    {
        public static List<ManifestEntry> MismatchedFiles { get; } = [];

        public static async Task<int> VerifyFileIntegrity(GameManifest GameManifest, Task<LocalFileChecksum[]> checksumTasks, string releaseChannelDirectory, bool isUpdate = false)
        {
            var fileChecksums = await checksumTasks;
            var checksumDict = fileChecksums.ToDictionary(fc => fc.name, fc => fc.checksum);

            InitializeProgressBar(GameManifest.files.Count);

            MismatchedFiles.Clear();

            foreach (var file in GameManifest.files)
            {
                string filePath = Path.Combine(releaseChannelDirectory, file.path);

                if (!File.Exists(filePath) || !checksumDict.TryGetValue(file.path, out var calculatedChecksum) || file.checksum != calculatedChecksum)
                {
                    LogSource logSource = isUpdate ? LogSource.Update : LogSource.Repair;
                    string messageAction = isUpdate ? "Outdated" : "Mismatched";
                    LogWarning(logSource, $"{messageAction} file found: {file.path}");

                    ManifestEntry ManifestEntry = new ManifestEntry
                    {
                        path = $"{file.path}",
                        checksum = file.checksum,
                        size = file.size,
                        optional = file.optional,
                        parts = file.parts
                    };

                    MismatchedFiles.Add(ManifestEntry);
                }
                UpdateProgress();
            }

            return MismatchedFiles.Count;
        }

        public static async Task<List<Task<LocalFileChecksum>>> PrepareLangChecksumTasksAsync(string folder)
        {
            GameManifest languageManifest = await ApiService.GetLanguageFilesAsync();

            var filePaths = languageManifest.languages
                .Select(lang => new
                {
                    path1 = Path.Combine(folder, "audio", "ship", $"general_{lang.ToLower(CultureInfo.InvariantCulture)}.mstr"),
                    path2 = Path.Combine(folder, "audio", "ship", $"general_{lang.ToLower(CultureInfo.InvariantCulture)}_patch_1.mstr")
                })
                .Where(p => File.Exists(p.path1) && File.Exists(p.path2))
                .SelectMany(p => new[] { p.path1, p.path2 })
                .ToList();

            return PrepareChecksumTasksForFiles(filePaths, folder);
        }

        public static List<Task<LocalFileChecksum>> PrepareCoreFileChecksumTasks(string folder)
        {
            var excludedPaths = new[] { "platform\\cfg\\user", "platform\\screenshots", "platform\\logs" };
            var allFiles = Directory.GetFiles(folder, "*", SearchOption.AllDirectories)
                .Where(f => !f.Contains("opt.starpak", StringComparison.OrdinalIgnoreCase) &&
                            !excludedPaths.Any(p => f.Contains(p, StringComparison.OrdinalIgnoreCase)));

            return PrepareChecksumTasksForFiles(allFiles, folder);
        }

        public static List<Task<LocalFileChecksum>> PrepareOptChecksumTasks(string folder)
        {
            var allFiles = Directory.GetFiles(folder, "*", SearchOption.AllDirectories)
                .Where(f => f.Contains("opt.starpak", StringComparison.OrdinalIgnoreCase));

            return PrepareChecksumTasksForFiles(allFiles, folder);
        }

        private static List<Task<LocalFileChecksum>> PrepareChecksumTasksForFiles(IEnumerable<string> files, string folder)
        {
            var fileList = files.ToList();
            InitializeProgressBar(fileList.Count);

            return fileList.Select(file => GenerateFileChecksumAsync(file, folder)).ToList();
        }

        public static async Task<LocalFileChecksum> GenerateFileChecksumAsync(string file, string folder)
        {
            var fileChecksum = new LocalFileChecksum();
            try
            {
                fileChecksum.name = file.Replace(folder + Path.DirectorySeparatorChar, "");
                fileChecksum.checksum = await CalculateChecksumAsync(file);

                UpdateProgress();

                return fileChecksum;
            }
            catch (Exception ex)
            {
                LogException($"Failed Generating Checksum For {file}", LogSource.Checksums, ex);
                return fileChecksum;
            }
        }

        public static async Task<string> CalculateChecksumAsync(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            using var sha256 = SHA256.Create();
            var hash = await sha256.ComputeHashAsync(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private static void InitializeProgressBar(int count)
        {
            appDispatcher.Invoke(() =>
            {
                Progress_Bar.Maximum = count;
                Progress_Bar.Value = 0;
                Percent_Label.Text = "0%";
            });
        }

        private static void UpdateProgress()
        {
            appDispatcher.Invoke(() =>
            {
                if (Progress_Bar.Maximum > 0)
                {
                    Progress_Bar.Value++;
                    Percent_Label.Text = $"{(Progress_Bar.Value / Progress_Bar.Maximum * 100):F2}%";
                }
            });
        }
    }
}