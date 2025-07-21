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
        private static async Task RunUpdateProcessAsync(bool forOptionalFiles)
        {
            string branchDirectory = GetBranch.Directory();
            string fileType = forOptionalFiles ? "optional" : "local";

            await CheckForDeletedFilesAsync(forOptionalFiles);

            Tasks.UpdateStatusLabel($"Checking {fileType} files", LogSource.Update);
            var checksumTasks = forOptionalFiles
                ? Checksums.PrepareOptChecksumTasks(branchDirectory)
                : Checksums.PrepareBranchChecksumTasks(branchDirectory);
            await Task.WhenAll(checksumTasks);

            Tasks.UpdateStatusLabel($"Fetching latest {fileType} files", LogSource.Update);
            var gameFiles = await Fetch.GameFiles(forOptionalFiles);

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
            await RunUpdateProcessAsync(forOptionalFiles: false);
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
                // Asynchronously update optional files without waiting.
                await UpdateOptionalFilesAsync();
            }
        }

        private static async Task UpdateOptionalFilesAsync()
        {
            await RunUpdateProcessAsync(forOptionalFiles: true);
            Managers.App.SendNotification($"R5Reloaded ({GetBranch.Name()}) optional files have been updated!", BalloonIcon.Info);
        }

        private static async Task CheckForDeletedFilesAsync(bool forOptionalFiles)
        {
            string branchDirectory = GetBranch.Directory();
            var allLocalFiles = Directory.GetFiles(branchDirectory, "*", SearchOption.AllDirectories);
            var serverFileManifest = await Fetch.GameFiles(forOptionalFiles);

            // Pre-compile the regex for performance if there are many languages.
            string languagesPattern = string.Join("|", GetBranch.Branch().mstr_languages.Select(Regex.Escape));
            var excludeLangRegex = new Regex($"general_({languagesPattern})(?:_|\\.)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            foreach (var localFile in allLocalFiles)
            {
                string relativePath = Path.GetRelativePath(branchDirectory, localFile);
                bool isOptFile = relativePath.EndsWith("opt.starpak", StringComparison.OrdinalIgnoreCase);

                // Skip files that don't match the current mode (optional vs. non-optional).
                if (forOptionalFiles != isOptFile) continue;

                // If the file exists locally but not on the server manifest, delete it.
                bool existsOnServer = serverFileManifest.files.Exists(f => f.path.Equals(relativePath, StringComparison.OrdinalIgnoreCase));
                if (!existsOnServer)
                {
                    try
                    {
                        // Extra check to avoid deleting language files that weren't fetched in the manifest.
                        if (!excludeLangRegex.IsMatch(Path.GetFileName(localFile)) && File.Exists(localFile))
                        {
                            File.Delete(localFile);
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
}