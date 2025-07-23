using Hardcodet.Wpf.TaskbarNotification;
using launcher.Core;
using launcher.Core.Models;
using launcher.Networking;
using launcher.Services;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static launcher.Utils.Logger;
using static launcher.Core.UiReferences;
using static launcher.Core.AppController;

namespace launcher.GameManagement
{
    public static class GameRepairer
    {
        public static async Task<bool> Start()
        {
            try
            {
                if (!await RunPreRepairChecksAsync()) return false;

                GameTasks.SetInstallState(true, "REPAIRING");

                bool repairNeeded = await ExecuteMainRepairAsync();
                await PerformPostRepairActionsAsync();

                return !repairNeeded || !Launcher.BadFilesDetected;
            }
            catch (Exception ex)
            {
                LogError(LogSource.Repair, $"A critical error occurred during repair: {ex.Message}");
                return false;
            }
            finally
            {
                GameTasks.SetInstallState(false);
                DiscordService.SetRichPresence("", "Idle");
            }
        }

        // ============================================================================================
        // Private Helper Methods
        // ============================================================================================
        private static async Task<bool> RunRepairProcessAsync(string branchDirectory, Func<Task<Task<LocalFileChecksum[]>>> prepareChecksums, Func<Task<GameManifest>> fetchFileManifest, string checkStatus, string compareStatus, string downloadStatus)
        {
            GameTasks.UpdateStatusLabel(checkStatus, LogSource.Repair);
            var checksumTasks = await prepareChecksums();

            GameTasks.UpdateStatusLabel(compareStatus, LogSource.Repair);
            var GameManifest = await fetchFileManifest();
            int badFileCount = await ChecksumManager.IdentifyBadFiles(GameManifest, checksumTasks, branchDirectory);

            if (badFileCount > 0)
            {
                GameTasks.UpdateStatusLabel(downloadStatus, LogSource.Repair);
                var downloadTasks = GameTasks.InitializeRepairTasks(branchDirectory);

                using var cts = new CancellationTokenSource();
                Task progressUpdateTask = DownloadService.UpdateGlobalDownloadProgressAsync(cts.Token);

                GameTasks.ShowSpeedLabels(true, true);
                await Task.WhenAll(downloadTasks);
                GameTasks.ShowSpeedLabels(false, false);
                await cts.CancelAsync();
                return true; // Indicates that a repair was attempted.
            }

            return false; // No repair was needed.
        }

        private static async Task<bool> RunPreRepairChecksAsync()
        {
            await Task.Delay(1);

            if (Launcher.IsInstalling || !Launcher.IsOnline || ReleaseChannelService.IsLocal()) return false;

            if (IsR5ApexOpen())
            {
                var result = MessageBox.Show("R5Reloaded must be closed to repair.\n\nClose the game now?", "R5Reloaded", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    CloseR5Apex();
                }
                else
                {
                    return false;
                }
            }

            if (ReleaseChannelService.IsUpdateAvailable())
            {
                Update_Button.Visibility = Visibility.Hidden;
                ReleaseChannelService.SetUpdateAvailable(false);
            }
            return true;
        }

        private static async Task<bool> ExecuteMainRepairAsync()
        {
            string branchDirectory = ReleaseChannelService.GetDirectory();
            DownloadService.CreateDownloadMonitor();
            DownloadService.ConfigureConcurrency();
            DownloadService.ConfigureDownloadSpeed();

            return await RunRepairProcessAsync(
                branchDirectory,
                () => Task.FromResult(Task.WhenAll(ChecksumManager.PrepareBranchChecksumTasks(branchDirectory))),
                () => ApiClient.GetGameManifestAsync(optional: false),
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
            ReleaseChannelService.SetInstalled(true);
            ReleaseChannelService.SetVersion(ReleaseChannelService.GetServerVersion());

            // Clean up cache files.
            string sigCacheFile = Path.Combine(ReleaseChannelService.GetDirectory(), "cfg", "startup.bin");
            if (File.Exists(sigCacheFile)) File.Delete(sigCacheFile);

            // Update UI and send notification.
            SetupAdvancedMenu();
            SendNotification($"R5Reloaded ({ReleaseChannelService.GetName()}) has been repaired!", BalloonIcon.Info);

            // Check for existing HD Textures.
            if (CheckForHDTextures(ReleaseChannelService.GetDirectory()))
            {
                ReleaseChannelService.SetDownloadHDTextures(true);
                // Asynchronously repair optional files without waiting.
                await RepairOptionalFilesAsync();
            }
        }

        private static async Task RepairOptionalFilesAsync()
        {
            await RunRepairProcessAsync(
                ReleaseChannelService.GetDirectory(),
                () => Task.FromResult(Task.WhenAll(ChecksumManager.PrepareOptChecksumTasks(ReleaseChannelService.GetDirectory()))),
                () => ApiClient.GetGameManifestAsync(optional: true),
                "Checking optional files...",
                "Comparing optional files...",
                "Downloading optional files..."
            );
            SendNotification($"R5Reloaded ({ReleaseChannelService.GetName()}) optional files have been repaired!", BalloonIcon.Info);
        }

        private static async Task RepairLanguageFilesAsync()
        {
            if (!Launcher.IsOnline) return;

            string branchDirectory = ReleaseChannelService.GetDirectory();

            GameManifest serverManifest = await ApiClient.GetLanguageFilesAsync();
            GameManifest manifestForRepair = new GameManifest
            {
                files = serverManifest.files
                    .Where(file => File.Exists(Path.Combine(branchDirectory, file.path)))
                    .ToList()
            };

            if (!manifestForRepair.files.Any())
            {
                LogInfo(LogSource.Repair, "No local language files found to verify.");
                return;
            }

            Func<Task<Task<LocalFileChecksum[]>>> prepareChecksums = async () =>
            {
                var checksumTasks = await ChecksumManager.PrepareLangChecksumTasksAsync(branchDirectory);
                return Task.WhenAll(checksumTasks);
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