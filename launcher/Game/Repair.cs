using Hardcodet.Wpf.TaskbarNotification;
using launcher.Global;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static launcher.Global.Logger;
using static launcher.Global.References;

namespace launcher.Game
{
    public static class Repair
    {
        public static async Task<bool> Start()
        {
            try
            {
                if (!await RunPreRepairChecksAsync()) return false;

                Tasks.SetInstallState(true, "REPAIRING");

                bool repairNeeded = await ExecuteMainRepairAsync();
                await PerformPostRepairActionsAsync();

                return !repairNeeded || !AppState.BadFilesDetected;
            }
            catch (Exception ex)
            {
                LogError(LogSource.Repair, $"A critical error occurred during repair: {ex.Message}");
                return false;
            }
            finally
            {
                Tasks.SetInstallState(false);
                AppState.SetRichPresence("", "Idle");
            }
        }

        // ============================================================================================
        // Private Helper Methods
        // ============================================================================================
        private static async Task<bool> RunRepairProcessAsync(string branchDirectory, Func<Task<List<Task<FileChecksum>>>> prepareChecksums, Func<Task<GameFiles>> fetchFileManifest, string checkStatus, string compareStatus, string downloadStatus)
        {
            Tasks.UpdateStatusLabel(checkStatus, LogSource.Repair);
            var checksumTasks = await prepareChecksums();
            await Task.WhenAll(checksumTasks);

            Tasks.UpdateStatusLabel(compareStatus, LogSource.Repair);
            var gameFiles = await fetchFileManifest();
            int badFileCount = Checksums.IdentifyBadFiles(gameFiles, checksumTasks, branchDirectory);

            if (badFileCount > 0)
            {
                Tasks.UpdateStatusLabel(downloadStatus, LogSource.Repair);
                var downloadTasks = Tasks.InitializeRepairTasks(branchDirectory);

                using var cts = new CancellationTokenSource();
                Task progressUpdateTask = Network.DownloadTracker.UpdateGlobalDownloadProgressAsync(cts.Token);

                Tasks.ShowSpeedLabels(true, true);
                await Task.WhenAll(downloadTasks);
                Tasks.ShowSpeedLabels(false, false);
                await cts.CancelAsync();
                return true; // Indicates that a repair was attempted.
            }

            return false; // No repair was needed.
        }

        private static async Task<bool> RunPreRepairChecksAsync()
        {
            await Task.Delay(1);

            if (AppState.IsInstalling || !AppState.IsOnline || GetBranch.IsLocalBranch()) return false;

            if (Managers.App.IsR5ApexOpen())
            {
                var result = MessageBox.Show("R5Reloaded must be closed to repair.\n\nClose the game now?", "R5Reloaded", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    Managers.App.CloseR5Apex();
                }
                else
                {
                    return false;
                }
            }

            if (GetBranch.UpdateAvailable())
            {
                Update_Button.Visibility = Visibility.Hidden;
                SetBranch.UpdateAvailable(false);
            }
            return true;
        }

        private static async Task<bool> ExecuteMainRepairAsync()
        {
            string branchDirectory = GetBranch.Directory();
            Network.DownloadTracker.CreateDownloadMonitor();
            Network.DownloadTracker.ConfigureConcurrency();
            Network.DownloadTracker.ConfigureDownloadSpeed();

            return await RunRepairProcessAsync(
                branchDirectory,
                () => Task.FromResult(Checksums.PrepareBranchChecksumTasks(branchDirectory)),
                () => Fetch.GameFiles(optional: false),
                "Checking core files...",
                "Comparing core files...",
                "Downloading core files..."
            );
        }

        private static async Task PerformPostRepairActionsAsync()
        {
            // Check and repair language files.
            await RepairLanguageFilesAsync();

            // Update local state.
            SetBranch.Installed(true);
            SetBranch.Version(GetBranch.ServerVersion());

            // Clean up cache files.
            string sigCacheFile = Path.Combine(GetBranch.Directory(), "cfg", "startup.bin");
            if (File.Exists(sigCacheFile)) File.Delete(sigCacheFile);

            // Update UI and send notification.
            Managers.App.SetupAdvancedMenu();
            Managers.App.SendNotification($"R5Reloaded ({GetBranch.Name()}) has been repaired!", BalloonIcon.Info);

            // Check for existing HD Textures.
            if (CheckForHDTextures(GetBranch.Directory()))
            {
                SetBranch.DownloadHDTextures(true);
                // Asynchronously repair optional files without waiting.
                await RepairOptionalFilesAsync();
            }
        }

        private static async Task RepairOptionalFilesAsync()
        {
            await RunRepairProcessAsync(
                GetBranch.Directory(),
                () => Task.FromResult(Checksums.PrepareOptChecksumTasks(GetBranch.Directory())),
                () => Fetch.GameFiles(optional: true),
                "Checking optional files...",
                "Comparing optional files...",
                "Downloading optional files..."
            );
            Managers.App.SendNotification($"R5Reloaded ({GetBranch.Name()}) optional files have been repaired!", BalloonIcon.Info);
        }

        private static async Task RepairLanguageFilesAsync()
        {
            if (!AppState.IsOnline) return;

            string branchDirectory = GetBranch.Directory();

            GameFiles serverManifest = await Fetch.LanguageFiles();

            var existingLocalFiles = serverManifest.files
                .Where(file => File.Exists(Path.Combine(branchDirectory, file.path)))
                .ToList();

            if (!existingLocalFiles.Any())
            {
                LogInfo(LogSource.Repair, "No local language files found to verify.");
                return;
            }

            var manifestForRepair = new GameFiles { files = existingLocalFiles };

            Func<Task<List<Task<FileChecksum>>>> prepareChecksums = () =>
            {
                appDispatcher.Invoke(() =>
                {
                    Progress_Bar.Maximum = existingLocalFiles.Count;
                    Progress_Bar.Value = 0;
                    Percent_Label.Text = "0%";
                });
                AppState.FilesLeft = existingLocalFiles.Count;

                var checksumTasks = new List<Task<FileChecksum>>();
                foreach (var file in existingLocalFiles)
                {
                    string fullPath = Path.Combine(branchDirectory, file.path);
                    checksumTasks.Add(Checksums.GenerateAndReturnFileChecksum(fullPath, branchDirectory));
                }
                return Task.FromResult(checksumTasks);
            };

            await RunRepairProcessAsync(
                branchDirectory,
                prepareChecksums,
                () => Task.FromResult(manifestForRepair),
                "Checking language files...",
                "Comparing language files...",
                "Downloading language files..."
            );
        }

        private static bool CheckForHDTextures(string branchDirectory)
        {
            return Directory.EnumerateFiles(branchDirectory, "*.opt.starpak", SearchOption.AllDirectories)
                .Any(path => !path.Contains(Path.DirectorySeparatorChar + "mods" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));
        }
    }
}