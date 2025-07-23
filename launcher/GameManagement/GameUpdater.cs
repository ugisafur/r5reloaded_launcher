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
using static launcher.Core.Application;

namespace launcher.GameManagement
{
    // NEW: Enum to represent the type of files being processed.
    // This is much clearer and more scalable than using a boolean.
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
                AppState.SetRichPresence("", "Idle");
            }
        }

        // ============================================================================================
        // Private Helper Methods
        // ============================================================================================

        // REFACTORED: This method now uses the UpdateFileType enum.
        private static async Task RunUpdateProcessAsync(UpdateFileType fileType)
        {
            string branchDirectory = GetBranch.Directory();

            await CheckForDeletedFilesAsync(fileType);

            GameTasks.UpdateStatusLabel($"Checking {fileType} files", LogSource.Update);

            Task<FileChecksum[]> checksumTasks;
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

            GameFiles gameFiles;
            switch (fileType)
            {
                case UpdateFileType.Main:
                    gameFiles = await ApiClient.GetGameFilesAsync(optional: false);
                    break;
                case UpdateFileType.Optional:
                    gameFiles = await ApiClient.GetGameFilesAsync(optional: true);
                    break;
                case UpdateFileType.Language:
                    GameFiles serverManifest = await ApiClient.GetLanguageFilesAsync();

                    gameFiles = new GameFiles
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
            int changedFileCount = await ChecksumManager.IdentifyBadFiles(gameFiles, checksumTasks, branchDirectory, true);

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

            if (AppState.IsInstalling || !AppState.IsOnline || GetBranch.IsLocalBranch() || !GetBranch.UpdateAvailable() || GetBranch.LocalVersion() == GetBranch.ServerVersion())
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

            SetBranch.UpdateAvailable(false);
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

            SetBranch.Installed(true);
            SetBranch.Version(GetBranch.ServerVersion());

            string sigCacheFile = Path.Combine(GetBranch.Directory(), "cfg", "startup.bin");
            if (File.Exists(sigCacheFile)) File.Delete(sigCacheFile);

            SendNotification($"R5Reloaded ({GetBranch.Name()}) has been updated!", BalloonIcon.Info);
            SetupAdvancedMenu();

            if (GetBranch.DownloadHDTextures())
            {
                await UpdateOptionalFilesAsync();
            }

            await UpdateLanguageFilesAsync();
        }

        private static async Task UpdateOptionalFilesAsync()
        {
            await RunUpdateProcessAsync(UpdateFileType.Optional);
            SendNotification($"R5Reloaded ({GetBranch.Name()}) optional files have been updated!", BalloonIcon.Info);
        }

        private static async Task UpdateLanguageFilesAsync()
        {
            await RunUpdateProcessAsync(UpdateFileType.Language);
            SendNotification($"R5Reloaded ({GetBranch.Name()}) language files have been updated!", BalloonIcon.Info);
        }

        private static async Task CheckForDeletedFilesAsync(UpdateFileType fileType)
        {
            string branchDirectory = GetBranch.Directory();
            var allLocalFiles = Directory.GetFiles(branchDirectory, "*", SearchOption.AllDirectories)
                .Select(f => Path.GetRelativePath(branchDirectory, f))
                .ToList();

            GameFiles serverFileManifest;
            Func<string, bool> fileTypeFilter;

            switch (fileType)
            {
                case UpdateFileType.Main:
                    serverFileManifest = await ApiClient.GetGameFilesAsync(optional: false);
                    fileTypeFilter = path => !path.EndsWith("opt.starpak", StringComparison.OrdinalIgnoreCase) && !path.Contains(Path.Combine("audio", "ship"));
                    break;
                case UpdateFileType.Optional:
                    serverFileManifest = await ApiClient.GetGameFilesAsync(optional: true);
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