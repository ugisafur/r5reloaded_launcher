using Hardcodet.Wpf.TaskbarNotification;
using launcher.Core;
using launcher.Core.Models;
using launcher.GameManagement;
using launcher.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.VisualStyles;
using static launcher.Core.UiReferences;
using static launcher.Utils.Logger;
using static launcher.Core.Application;
using launcher.Networking;
using launcher.Configuration;

namespace launcher.GameManagement
{
    public static class GameInstaller
    {
        public static async Task Start()
        {
            try
            {
                if (!await RunPreFlightChecksAsync()) return;

                GameTasks.SetInstallState(true, "INSTALLING");

                await ExecuteDownloadAndRepairAsync();
                await PerformPostInstallActionsAsync();
            }
            catch (Exception ex)
            {
                LogError(LogSource.Installer, $"A critical error occurred during installation: {ex.Message}");
            }
            finally
            {
                GameTasks.SetInstallState(false);
                AppState.SetRichPresence("", "Idle");
            }
        }

        public static async Task HDTextures()
        {
            if (AppState.IsInstalling || !AppState.IsOnline || GetBranch.IsLocalBranch()) return;

            GameFiles gameFiles = await ApiClient.GetGameFilesAsync(optional: true);
            if (!await CheckForSufficientSpaceAsync(gameFiles, "HD Textures")) return;

            GameTasks.SetInstallState(true);
            try
            {
                await RunDownloadProcessAsync(gameFiles, "Downloading optional files");

                SetBranch.DownloadHDTextures(true);
                appDispatcher.Invoke(() => Settings_Control.gameInstalls.UpdateGameItems());
                SendNotification($"R5Reloaded ({GetBranch.Name()}) optional files have been installed!", BalloonIcon.Info);
            }
            finally
            {
                GameTasks.SetInstallState(false);
                AppState.SetRichPresence("", "Idle");
            }
        }

        public static async Task LangFile(CheckBox checkBox, GameFiles gameFiles, string language, bool bypass_block = false)
        {
            if (!AppState.IsOnline || (AppState.BlockLanguageInstall && !bypass_block)) return;

            if (!await CheckForSufficientSpaceAsync(gameFiles, "Language File")) return;

            gameFiles.files = gameFiles.files.Where(file => file.path.Contains(language)).ToList();

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
            DownloadService.CreateDownloadMonitor();
            DownloadService.ConfigureConcurrency();
            DownloadService.ConfigureDownloadSpeed();

            string branchDirectory = GetBranch.Directory();
            var downloadTasks = GameTasks.InitializeDownloadTasks(gameFiles, branchDirectory);

            using var cts = new CancellationTokenSource();
            Task progressUpdateTask = DownloadService.UpdateGlobalDownloadProgressAsync(cts.Token);

            GameTasks.ShowSpeedLabels(showMainSpeed, true);
            GameTasks.UpdateStatusLabel(statusLabel, LogSource.Installer);

            await Task.WhenAll(downloadTasks);

            GameTasks.ShowSpeedLabels(false, false);
            await cts.CancelAsync();
        }

        private static async Task<bool> RunPreFlightChecksAsync()
        {
            if (AppState.IsInstalling || !AppState.IsOnline || GetBranch.IsLocalBranch()) return false;

            if (string.IsNullOrEmpty((string)IniSettings.Get(IniSettings.Vars.Library_Location)))
            {
                appDispatcher.Invoke(() => ShowInstallLocation());
                return false;
            }

            if (!GetBranch.EULAAccepted())
            {
                appDispatcher.Invoke(() => ShowEULA());
                return false;
            }

            if (GetBranch.ExeExists())
            {
                await Task.Run(() => GameRepairer.Start());
                return false; // Pivoted to repair, so stop the install flow.
            }

            GameFiles gameFiles = await ApiClient.GetGameFilesAsync(optional: false);
            const long extraSpaceBuffer = 30L * 1024 * 1024 * 1024; // 30 GB
            return await CheckForSufficientSpaceAsync(gameFiles, "R5Reloaded", extraSpaceBuffer);
        }

        private static async Task<bool> CheckForSufficientSpaceAsync(GameFiles gameFiles, string installName, long extraBuffer = 0)
        {
            await Task.Delay(1);

            long requiredSpace = gameFiles.files.Sum(f => f.size) + extraBuffer;
            string libraryLocation = (string)IniSettings.Get(IniSettings.Vars.Library_Location);

            if (string.IsNullOrEmpty(libraryLocation))
            {
                appDispatcher.Invoke(() => ShowInstallLocation());
                return false;
            }

            if (!HasEnoughFreeSpace(libraryLocation, requiredSpace))
            {
                MessageBox.Show($"Not enough free space to install {installName}.\n\nRequired: {FormatBytes(requiredSpace)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
        }

        private static async Task ExecuteDownloadAndRepairAsync()
        {
            GameFiles gameFiles = await ApiClient.GetGameFilesAsync(optional: false);
            await RunDownloadProcessAsync(gameFiles, "Downloading game files");

            if (AppState.BadFilesDetected)
            {
                GameTasks.UpdateStatusLabel("Repairing game files", LogSource.Installer);
                await AttemptGameRepair();
            }
        }

        private static async Task PerformPostInstallActionsAsync()
        {
            GameFiles gameFiles = await ApiClient.GetLanguageFilesAsync();
            bool languageAvailable = gameFiles.languages.Contains(Launcher.language_name, StringComparer.OrdinalIgnoreCase);
            if (languageAvailable && Launcher.language_name != "english")
            {
                await LangFile(null, gameFiles, Launcher.language_name, bypass_block: true);
            }

            SetBranch.Installed(true);
            SetBranch.Version(GetBranch.ServerVersion());
            appDispatcher.Invoke(() => SetupAdvancedMenu());
            SendNotification($"R5Reloaded ({GetBranch.Name()}) has been installed!", BalloonIcon.Info);

            GameFiles optFiles = await ApiClient.GetGameFilesAsync(optional: true);
            appDispatcher.Invoke(() =>
            {
                OptFiles_Control.SetDownloadSize(optFiles);
                ShowDownloadOptlFiles();
            });
        }

        private static async Task AttemptGameRepair()
        {
            bool isRepaired = false;
            for (int i = 0; i < Launcher.MAX_REPAIR_ATTEMPTS && !isRepaired; i++)
            {
                isRepaired = await GameRepairer.Start();
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