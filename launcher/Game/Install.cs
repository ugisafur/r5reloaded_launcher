using Hardcodet.Wpf.TaskbarNotification;
using launcher.Global;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.VisualStyles;
using static launcher.Global.Logger;
using static launcher.Global.References;

namespace launcher.Game
{
    public static class Install
    {
        public static async Task Start()
        {
            try
            {
                if (!await RunPreFlightChecksAsync()) return;

                Download.Tasks.SetInstallState(true, "INSTALLING");

                await ExecuteDownloadAndRepairAsync();
                await PerformPostInstallActionsAsync();
            }
            catch (Exception ex)
            {
                LogError(LogSource.Installer, $"A critical error occurred during installation: {ex.Message}");
            }
            finally
            {
                Download.Tasks.SetInstallState(false);
                AppState.SetRichPresence("", "Idle");
            }
        }

        public static async Task HDTextures()
        {
            if (AppState.IsInstalling || !AppState.IsOnline || GetBranch.IsLocalBranch()) return;

            GameFiles gameFiles = await Fetch.GameFiles(optional: true);
            if (!await CheckForSufficientSpaceAsync(gameFiles, "HD Textures")) return;

            Download.Tasks.SetInstallState(true);
            try
            {
                await RunDownloadProcessAsync(gameFiles, "Downloading optional files");

                SetBranch.DownloadHDTextures(true);
                appDispatcher.Invoke(() => Settings_Control.gameInstalls.UpdateGameItems());
                Managers.App.SendNotification($"R5Reloaded ({GetBranch.Name()}) optional files have been installed!", BalloonIcon.Info);
            }
            finally
            {
                Download.Tasks.SetInstallState(false);
                AppState.SetRichPresence("", "Idle");
            }
        }

        public static async Task LangFile(CheckBox checkBox, string[] langs, bool bypass_block = false)
        {
            if (!AppState.IsOnline || (AppState.BlockLanguageInstall && !bypass_block)) return;

            GameFiles gameFiles = await Fetch.LanguageFiles([.. langs]);
            if (!await CheckForSufficientSpaceAsync(gameFiles, "Language File")) return;

            appDispatcher.Invoke(() => { if (checkBox != null) checkBox.IsEnabled = false; });
            try
            {
                await RunDownloadProcessAsync(gameFiles, "Downloading language files", showMainSpeed: false);
            }
            finally
            {
                appDispatcher.Invoke(() => { if (checkBox != null) checkBox.IsEnabled = true; });
            }
        }

        // ============================================================================================
        // Private Helper Methods
        // ============================================================================================
        private static async Task RunDownloadProcessAsync(GameFiles gameFiles, string statusLabel, bool showMainSpeed = true)
        {
            Network.DownloadSpeedTracker.CreateDownloadMonitor();
            Network.DownloadSpeedTracker.ConfigureConcurrency();
            Network.DownloadSpeedTracker.ConfigureDownloadSpeed();

            string branchDirectory = GetBranch.Directory();
            var downloadTasks = Download.Tasks.InitializeDownloadTasks(gameFiles, branchDirectory);

            using var cts = new CancellationTokenSource();
            Task progressUpdateTask = Network.DownloadSpeedTracker.UpdateGlobalDownloadProgressAsync(cts.Token);

            Download.Tasks.ShowSpeedLabels(showMainSpeed, true);
            Download.Tasks.UpdateStatusLabel(statusLabel, LogSource.Installer);

            await Task.WhenAll(downloadTasks);

            Download.Tasks.ShowSpeedLabels(false, false);
            await cts.CancelAsync();
        }

        private static async Task<bool> RunPreFlightChecksAsync()
        {
            if (AppState.IsInstalling || !AppState.IsOnline || GetBranch.IsLocalBranch()) return false;

            if (string.IsNullOrEmpty((string)Ini.Get(Ini.Vars.Library_Location)))
            {
                appDispatcher.Invoke(() => Managers.App.ShowInstallLocation());
                return false;
            }

            if (!GetBranch.EULAAccepted())
            {
                appDispatcher.Invoke(() => Managers.App.ShowEULA());
                return false;
            }

            if (GetBranch.ExeExists())
            {
                await Task.Run(() => Repair.Start());
                return false; // Pivoted to repair, so stop the install flow.
            }

            GameFiles gameFiles = await Fetch.GameFiles(optional: false);
            const long extraSpaceBuffer = 30L * 1024 * 1024 * 1024; // 30 GB
            return await CheckForSufficientSpaceAsync(gameFiles, "R5Reloaded", extraSpaceBuffer);
        }

        private static async Task<bool> CheckForSufficientSpaceAsync(GameFiles gameFiles, string installName, long extraBuffer = 0)
        {
            await Task.Delay(1);

            long requiredSpace = gameFiles.files.Sum(f => f.sizeInBytes) + extraBuffer;
            string libraryLocation = (string)Ini.Get(Ini.Vars.Library_Location);

            if (string.IsNullOrEmpty(libraryLocation))
            {
                appDispatcher.Invoke(() => Managers.App.ShowInstallLocation());
                return false;
            }

            if (!Managers.App.HasEnoughFreeSpace(libraryLocation, requiredSpace))
            {
                MessageBox.Show($"Not enough free space to install {installName}.\n\nRequired: {FormatBytes(requiredSpace)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
        }

        private static async Task ExecuteDownloadAndRepairAsync()
        {
            GameFiles gameFiles = await Fetch.GameFiles(optional: false);
            await RunDownloadProcessAsync(gameFiles, "Downloading game files");

            if (AppState.BadFilesDetected)
            {
                Download.Tasks.UpdateStatusLabel("Repairing game files", LogSource.Installer);
                await AttemptGameRepair();
            }
        }

        private static async Task PerformPostInstallActionsAsync()
        {
            bool languageAvailable = GetBranch.Branch().mstr_languages.Contains(Launcher.language_name, StringComparer.OrdinalIgnoreCase);
            if (languageAvailable && Launcher.language_name != "english")
            {
                await LangFile(null, new[] { Launcher.language_name }, bypass_block: true);
            }

            SetBranch.Installed(true);
            SetBranch.Version(GetBranch.ServerVersion());
            appDispatcher.Invoke(() => Managers.App.SetupAdvancedMenu());
            Managers.App.SendNotification($"R5Reloaded ({GetBranch.Name()}) has been installed!", BalloonIcon.Info);

            GameFiles optFiles = await Fetch.GameFiles(optional: true);
            appDispatcher.Invoke(() =>
            {
                OptFiles_Control.SetDownloadSize(optFiles);
                Managers.App.ShowDownloadOptlFiles();
            });
        }

        private static async Task AttemptGameRepair()
        {
            bool isRepaired = false;
            for (int i = 0; i < Launcher.MAX_REPAIR_ATTEMPTS && !isRepaired; i++)
            {
                isRepaired = await Repair.Start();
            }
            AppState.BadFilesDetected = !isRepaired;
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes == 0) return "0 B";
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 2);
            return $"{num} {suffixes[place]}";
        }
    }
}