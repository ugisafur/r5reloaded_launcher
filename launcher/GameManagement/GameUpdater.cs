using Hardcodet.Wpf.TaskbarNotification;
using launcher.Core;
using launcher.Core.Models;
using launcher.Networking;
using launcher.Services;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static launcher.Utils.Logger;
using static launcher.Core.AppController;

namespace launcher.GameManagement
{
    public enum UpdateFileType { Main, Optional, Language }

    public static class GameUpdater
    {
        public static async Task Start()
        {
            try
            {
                if (!await RunPreUpdateChecksAsync()) return;

                GameTasks.SetInstallState(true, "UPDATING");

                await ExecuteMainUpdateAsync();
                await PerformPostUpdateActionsAsync();
            }
            catch (Exception ex)
            {
                LogError(LogSource.Update, $"A critical error occurred during update: {ex.Message}");
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
        private static async Task RunUpdateProcessAsync(UpdateFileType fileType)
        {
            string branchDirectory = ReleaseChannelService.GetDirectory();

            await CheckForDeletedFilesAsync(fileType);

            GameTasks.UpdateStatusLabel($"Checking {fileType} files", LogSource.Update);

            Task<LocalFileChecksum[]> checksumTasks;
            switch (fileType)
            {
                case UpdateFileType.Main:
                    checksumTasks = Task.WhenAll(ChecksumManager.PrepareBranchChecksumTasks(branchDirectory));
                    break;
                case UpdateFileType.Optional:
                    checksumTasks = Task.WhenAll(ChecksumManager.PrepareOptChecksumTasks(branchDirectory));
                    break;
                case UpdateFileType.Language:
                    checksumTasks = Task.WhenAll(await ChecksumManager.PrepareLangChecksumTasksAsync(branchDirectory));
                    break;
                default:
                    return;
            }

            GameTasks.UpdateStatusLabel($"Fetching latest {fileType} files", LogSource.Update);

            GameManifest GameManifest;
            switch (fileType)
            {
                case UpdateFileType.Main:
                    GameManifest = await ApiClient.GetGameManifestAsync(optional: false);
                    break;
                case UpdateFileType.Optional:
                    GameManifest = await ApiClient.GetGameManifestAsync(optional: true);
                    break;
                case UpdateFileType.Language:
                    GameManifest serverManifest = await ApiClient.GetLanguageFilesAsync();

                    GameManifest = new GameManifest
                    {
                        files = serverManifest.files
                            .Where(f => File.Exists(Path.Combine(branchDirectory, f.path)))
                            .ToList()
                    };
                    break;
                default:
                    return;
            }

            await Task.WhenAll(checksumTasks);

            GameTasks.UpdateStatusLabel($"Finding updated {fileType} files", LogSource.Update);
            int changedFileCount = await ChecksumManager.IdentifyBadFiles(GameManifest, checksumTasks, branchDirectory, true);

            if (changedFileCount > 0)
            {
                GameTasks.UpdateStatusLabel($"Downloading updated {fileType} files", LogSource.Update);
                var downloadTasks = GameTasks.InitializeRepairTasks(branchDirectory);

                using var cts = new CancellationTokenSource();
                Task progressUpdateTask = DownloadService.UpdateGlobalDownloadProgressAsync(cts.Token);

                GameTasks.ShowSpeedLabels(true, true);
                await Task.WhenAll(downloadTasks);
                GameTasks.ShowSpeedLabels(false, false);
                await cts.CancelAsync();
            }
        }

        private static async Task<bool> RunPreUpdateChecksAsync()
        {
            await Task.Delay(1);

            if (Launcher.IsInstalling || !Launcher.IsOnline || ReleaseChannelService.IsLocal() || !ReleaseChannelService.IsUpdateAvailable() || ReleaseChannelService.GetLocalVersion() == ReleaseChannelService.GetServerVersion())
                return false;

            if (IsR5ApexOpen())
            {
                var result = MessageBox.Show("R5Reloaded must be closed to update.\n\nClose the game now?", "R5Reloaded", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    CloseR5Apex();
                }
                else
                {
                    return false;
                }
            }

            ReleaseChannelService.SetUpdateAvailable(false);
            return true;
        }

        private static async Task ExecuteMainUpdateAsync()
        {
            DownloadService.CreateDownloadMonitor();
            DownloadService.ConfigureConcurrency();
            DownloadService.ConfigureDownloadSpeed();

            await RunUpdateProcessAsync(UpdateFileType.Main);
        }

        private static async Task PerformPostUpdateActionsAsync()
        {
            await Task.Delay(1);

            ReleaseChannelService.SetInstalled(true);
            ReleaseChannelService.SetVersion(ReleaseChannelService.GetServerVersion());

            string sigCacheFile = Path.Combine(ReleaseChannelService.GetDirectory(), "cfg", "startup.bin");
            if (File.Exists(sigCacheFile)) File.Delete(sigCacheFile);

            SendNotification($"R5Reloaded ({ReleaseChannelService.GetName()}) has been updated!", BalloonIcon.Info);
            SetupAdvancedMenu();

            if (ReleaseChannelService.ShouldDownloadHDTextures())
            {
                await UpdateOptionalFilesAsync();
            }

            await UpdateLanguageFilesAsync();
        }

        private static async Task UpdateOptionalFilesAsync()
        {
            await RunUpdateProcessAsync(UpdateFileType.Optional);
            SendNotification($"R5Reloaded ({ReleaseChannelService.GetName()}) optional files have been updated!", BalloonIcon.Info);
        }

        private static async Task UpdateLanguageFilesAsync()
        {
            await RunUpdateProcessAsync(UpdateFileType.Language);
            SendNotification($"R5Reloaded ({ReleaseChannelService.GetName()}) language files have been updated!", BalloonIcon.Info);
        }

        private static async Task CheckForDeletedFilesAsync(UpdateFileType fileType)
        {
            string branchDirectory = ReleaseChannelService.GetDirectory();
            var allLocalFiles = Directory.GetFiles(branchDirectory, "*", SearchOption.AllDirectories)
                .Select(f => Path.GetRelativePath(branchDirectory, f))
                .ToList();

            GameManifest serverFileManifest;
            Func<string, bool> fileTypeFilter;

            switch (fileType)
            {
                case UpdateFileType.Main:
                    serverFileManifest = await ApiClient.GetGameManifestAsync(optional: false);
                    fileTypeFilter = path => !path.EndsWith("opt.starpak", StringComparison.OrdinalIgnoreCase) && !path.Contains(Path.Combine("audio", "ship"));
                    break;
                case UpdateFileType.Optional:
                    serverFileManifest = await ApiClient.GetGameManifestAsync(optional: true);
                    fileTypeFilter = path => path.EndsWith("opt.starpak", StringComparison.OrdinalIgnoreCase);
                    break;
                case UpdateFileType.Language:
                    serverFileManifest = await ApiClient.GetLanguageFilesAsync();
                    fileTypeFilter = path => path.Contains(Path.Combine("audio", "ship"));
                    break;
                default:
                    return;
            }

            var serverFilesSet = serverFileManifest.files
                .Select(f => f.path.Replace('/', '\\'))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var filesToDelete = allLocalFiles
                .Where(fileTypeFilter)
                .Where(localFile => !serverFilesSet.Contains(localFile));

            foreach (var relativePath in filesToDelete)
            {
                try
                {
                    string fullPath = Path.Combine(branchDirectory, relativePath);
                    if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                    }
                }
                catch (Exception ex)
                {
                    LogException($"Failed to delete obsolete file: {relativePath}", LogSource.Update, ex);
                }
            }
        }
    }
}