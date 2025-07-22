using Hardcodet.Wpf.TaskbarNotification;
using launcher.Global;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static launcher.Global.Logger;

namespace launcher.Game
{
    // NEW: Enum to represent the type of files being processed.
    // This is much clearer and more scalable than using a boolean.
    public enum UpdateFileType { Main, Optional, Language }

    public static class Update
    {
        public static async Task Start()
        {
            try
            {
                if (!await RunPreUpdateChecksAsync()) return;

                Tasks.SetInstallState(true, "UPDATING");

                await ExecuteMainUpdateAsync();
                await PerformPostUpdateActionsAsync();
            }
            catch (Exception ex)
            {
                LogError(LogSource.Update, $"A critical error occurred during update: {ex.Message}");
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

        // REFACTORED: This method now uses the UpdateFileType enum.
        // REFACTORED: This method now uses the UpdateFileType enum.
        private static async Task RunUpdateProcessAsync(UpdateFileType fileType)
        {
            string branchDirectory = GetBranch.Directory();

            await CheckForDeletedFilesAsync(fileType);

            Tasks.UpdateStatusLabel($"Checking {fileType} files", LogSource.Update);

            List<Task<FileChecksum>> checksumTasks;
            switch (fileType)
            {
                case UpdateFileType.Main:
                    checksumTasks = Checksums.PrepareBranchChecksumTasks(branchDirectory);
                    break;
                case UpdateFileType.Optional:
                    checksumTasks = Checksums.PrepareOptChecksumTasks(branchDirectory);
                    break;
                case UpdateFileType.Language:
                    checksumTasks = Checksums.PrepareLangChecksumTasks(branchDirectory);
                    break;
                default:
                    return;
            }

            Tasks.UpdateStatusLabel($"Fetching latest {fileType} files", LogSource.Update);

            GameFiles gameFiles;
            switch (fileType)
            {
                case UpdateFileType.Main:
                    gameFiles = await Fetch.GameFiles(optional: false);
                    break;
                case UpdateFileType.Optional:
                    gameFiles = await Fetch.GameFiles(optional: true);
                    break;
                case UpdateFileType.Language:
                    GameFiles serverManifest = await Fetch.LanguageFiles();

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

            Tasks.UpdateStatusLabel($"Finding updated {fileType} files", LogSource.Update);
            int changedFileCount = Checksums.IdentifyBadFiles(gameFiles, checksumTasks, branchDirectory, true);

            if (changedFileCount > 0)
            {
                Tasks.UpdateStatusLabel($"Downloading updated {fileType} files", LogSource.Update);
                var downloadTasks = Tasks.InitializeRepairTasks(branchDirectory);

                using var cts = new CancellationTokenSource();
                Task progressUpdateTask = Network.DownloadSpeedTracker.UpdateGlobalDownloadProgressAsync(cts.Token);

                Tasks.ShowSpeedLabels(true, true);
                await Task.WhenAll(downloadTasks);
                Tasks.ShowSpeedLabels(false, false);
                await cts.CancelAsync();
            }
        }

        private static async Task<bool> RunPreUpdateChecksAsync()
        {
            await Task.Delay(1);

            if (AppState.IsInstalling || !AppState.IsOnline || GetBranch.IsLocalBranch() || !GetBranch.UpdateAvailable() || GetBranch.LocalVersion() == GetBranch.ServerVersion())
                return false;

            if (Managers.App.IsR5ApexOpen())
            {
                var result = MessageBox.Show("R5Reloaded must be closed to update.\n\nClose the game now?", "R5Reloaded", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    Managers.App.CloseR5Apex();
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
            Network.DownloadSpeedTracker.CreateDownloadMonitor();
            Network.DownloadSpeedTracker.ConfigureConcurrency();
            Network.DownloadSpeedTracker.ConfigureDownloadSpeed();

            await RunUpdateProcessAsync(UpdateFileType.Main);
        }

        private static async Task PerformPostUpdateActionsAsync()
        {
            await Task.Delay(1);

            SetBranch.Installed(true);
            SetBranch.Version(GetBranch.ServerVersion());

            string sigCacheFile = Path.Combine(GetBranch.Directory(), "cfg", "startup.bin");
            if (File.Exists(sigCacheFile)) File.Delete(sigCacheFile);

            Managers.App.SendNotification($"R5Reloaded ({GetBranch.Name()}) has been updated!", BalloonIcon.Info);
            Managers.App.SetupAdvancedMenu();

            if (GetBranch.DownloadHDTextures())
            {
                await UpdateOptionalFilesAsync();
            }

            await UpdateLanguageFilesAsync();
        }

        private static async Task UpdateOptionalFilesAsync()
        {
            await RunUpdateProcessAsync(UpdateFileType.Optional);
            Managers.App.SendNotification($"R5Reloaded ({GetBranch.Name()}) optional files have been updated!", BalloonIcon.Info);
        }

        private static async Task UpdateLanguageFilesAsync()
        {
            await RunUpdateProcessAsync(UpdateFileType.Language);
            Managers.App.SendNotification($"R5Reloaded ({GetBranch.Name()}) language files have been updated!", BalloonIcon.Info);
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
                    serverFileManifest = await Fetch.GameFiles(optional: false);
                    fileTypeFilter = path => !path.EndsWith("opt.starpak", StringComparison.OrdinalIgnoreCase) && !path.Contains(Path.Combine("audio", "ship"));
                    break;
                case UpdateFileType.Optional:
                    serverFileManifest = await Fetch.GameFiles(optional: true);
                    fileTypeFilter = path => path.EndsWith("opt.starpak", StringComparison.OrdinalIgnoreCase);
                    break;
                case UpdateFileType.Language:
                    serverFileManifest = await Fetch.LanguageFiles();
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