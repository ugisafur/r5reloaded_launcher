using Hardcodet.Wpf.TaskbarNotification;
using launcher.Global;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static launcher.Global.Logger;
using static launcher.Global.References;

namespace launcher.Game
{
    public static class Uninstall
    {
        public static async Task Start()
        {
            if (!await RunPreUninstallChecksAsync()) return;

            Tasks.SetInstallState(true, "UNINSTALLING");
            try
            {
                var allFiles = Directory.GetFiles(GetBranch.Directory(), "*", SearchOption.AllDirectories);
                await RunUninstallProcessAsync(allFiles, "Removing game files");

                // After deleting files, remove the now-empty directories.
                Directory.Delete(GetBranch.Directory(), true);

                // Reset all branch-specific settings.
                SetBranch.Installed(false);
                SetBranch.DownloadHDTextures(false);
                SetBranch.Version("");

                Managers.App.SendNotification($"R5Reloaded ({GetBranch.Name()}) has been uninstalled!", BalloonIcon.Info);
            }
            catch (Exception ex)
            {
                LogError(LogSource.Uninstaller, $"A critical error occurred during uninstall: {ex.Message}");
            }
            finally
            {
                // ✅ Ensures the UI is always reset.
                Tasks.SetInstallState(false, "INSTALL");
                AppState.SetRichPresence("", "Idle");
            }
        }

        public static async Task LangFile(CheckBox checkBox, List<string> langs)
        {
            if (!GetBranch.Installed() || !Directory.Exists(GetBranch.Directory())) return;

            appDispatcher.Invoke(() => { if (checkBox != null) checkBox.IsEnabled = false; });
            Tasks.SetInstallState(true, "UNINSTALLING");
            try
            {
                GameFiles langFilesManifest = await Fetch.LanguageFiles(langs);
                var filesToDelete = langFilesManifest.files
                    .Select(f => Path.Combine(GetBranch.Directory(), f.destinationPath))
                    .Where(File.Exists)
                    .ToArray();

                await RunUninstallProcessAsync(filesToDelete, "Removing language files");
            }
            finally
            {
                Tasks.SetInstallState(false);
                appDispatcher.Invoke(() => { if (checkBox != null) checkBox.IsEnabled = true; });
            }
        }

        public static async Task HDTextures(Branch branch)
        {
            if (!GetBranch.Installed(branch) || !Directory.Exists(GetBranch.Directory(branch))) return;

            Tasks.SetInstallState(true, "UNINSTALLING");
            try
            {
                var optFiles = Directory.GetFiles(GetBranch.Directory(branch), "*.opt.starpak", SearchOption.AllDirectories);
                await RunUninstallProcessAsync(optFiles, "Removing HD textures");

                SetBranch.DownloadHDTextures(false, branch);
                Managers.App.SendNotification($"HD Textures ({GetBranch.Name(true, branch)}) have been uninstalled!", BalloonIcon.Info);
            }
            finally
            {
                Tasks.SetInstallState(false, "PLAY");
            }
        }

        // ============================================================================================
        // Private Helper Methods
        // ============================================================================================

        private static async Task RunUninstallProcessAsync(IReadOnlyCollection<string> filesToDelete, string statusLabel)
        {
            Tasks.UpdateStatusLabel(statusLabel,LogSource.Uninstaller);

            await appDispatcher.InvokeAsync(() => { Progress_Bar.Maximum = filesToDelete.Count; Progress_Bar.Value = 0; });

            await Task.Run(() =>
            {
                Parallel.ForEach(filesToDelete, file =>
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        LogException($"Failed to delete file: {file}",LogSource.Uninstaller, ex);
                    }
                    finally
                    {
                        // Safely update the progress bar on the UI thread.
                        appDispatcher.Invoke(() => Progress_Bar.Value++);
                    }
                });
            });
        }

        private static async Task<bool> RunPreUninstallChecksAsync()
        {
            await Task.Delay(1);

            string branchDir = GetBranch.Directory();
            if (!Directory.Exists(branchDir))
            {
                // If directory is already gone, just clean up the state.
                SetBranch.Installed(false);
                SetBranch.DownloadHDTextures(false);
                SetBranch.Version("");
                return false;
            }

            if (Managers.App.IsR5ApexOpen())
            {
                var result = MessageBox.Show("R5Reloaded must be closed to uninstall.\n\nClose the game now?", "R5Reloaded", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    Managers.App.CloseR5Apex();
                }
                else
                {
                    return false;
                }
            }

            return !IsAnyFileLocked(branchDir);
        }

        private static bool IsAnyFileLocked(string directoryPath)
        {
            // This check can be slow on large directories. Consider if it's essential.
            foreach (string file in Directory.GetFiles(directoryPath))
            {
                if (IsFileLocked(file))
                {
                    MessageBox.Show($"The file '{Path.GetFileName(file)}' is in use. Please close any programs using it.", "File In Use", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return true;
                }
            }
            return false;
        }

        private static bool IsFileLocked(string filePath)
        {
            try
            {
                using (new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
            }
            catch (IOException)
            {
                // The file is unavailable because it is still being written to,
                // or being processed by another thread, or does not exist.
                return true;
            }
            return false;
        }
    }
}