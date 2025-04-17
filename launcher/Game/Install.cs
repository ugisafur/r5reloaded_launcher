using Hardcodet.Wpf.TaskbarNotification;
using static launcher.Global.Logger;
using static launcher.Global.References;
using System.Windows.Controls;
using launcher.Global;
using System.Windows;

namespace launcher.Game
{
    public static class Install
    {
        public static async void Start()
        {
            if (AppState.IsInstalling || !AppState.IsOnline || GetBranch.IsLocalBranch())
                return;

            if (string.IsNullOrEmpty((string)Ini.Get(Ini.Vars.Library_Location)))
            {
                appDispatcher.Invoke(new Action(() => { Managers.App.ShowInstallLocation(); }));
                return;
            }

            if (!GetBranch.EULAAccepted())
            {
                appDispatcher.Invoke(new Action(() => { Managers.App.ShowEULA(); }));
                return;
            }

            if (GetBranch.ExeExists())
            {
                Task.Run(() => { Repair.Start(); });
                return;
            }

            GameFiles uncompressedgameFiles = await Fetch.GameFiles(false, false);
            long requiredSpace = uncompressedgameFiles.files.Sum(f => f.size);
            if (!Managers.App.HasEnoughFreeSpace((string)Ini.Get(Ini.Vars.Library_Location), requiredSpace))
            {
                MessageBox.Show($"Not enough free space to install R5Reloaded.\n\nRequired: {requiredSpace / 1024 / 1024} MB", "R5Reloaded", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Download.Tasks.CreateDownloadMontior();
            Download.Tasks.ConfigureConcurrency();
            Download.Tasks.ConfigureDownloadSpeed();

            Download.Tasks.SetInstallState(true, "INSTALLING");

            string branchDirectory = GetBranch.Directory();

            Download.Tasks.UpdateStatusLabel("Fetching latest files", Source.Installer);
            GameFiles gameFiles = await Fetch.GameFiles(true, false);

            Download.Tasks.UpdateStatusLabel("Preparing game download", Source.Installer);
            var downloadTasks = Download.Tasks.InitializeDownloadTasks(gameFiles, branchDirectory);

            CancellationTokenSource cts = new CancellationTokenSource();
            Task updateTask = Download.Tasks.UpdateGlobalDownloadProgressAsync(cts.Token);

            Download.Tasks.ShowSpeedLabels(true, true);
            Download.Tasks.UpdateStatusLabel("Downloading game files", Source.Installer);
            await Task.WhenAll(downloadTasks);
            Download.Tasks.ShowSpeedLabels(false, false);

            cts.Cancel();

            if (AppState.BadFilesDetected)
            {
                Download.Tasks.UpdateStatusLabel("Reparing game files", Source.Installer);
                await AttemptGameRepair();
            }

            LogInfo(Source.Installer, $"Checking system language against available game languages");
            if (GetBranch.Branch().mstr_languages.Contains(Launcher.language_name, StringComparer.OrdinalIgnoreCase) && Launcher.language_name != "english")
            {
                LogInfo(Source.Installer, $"game language found ({Launcher.language_name}), installing language files");
                await LangFile(null, [Launcher.language_name], true);
            }

            SetBranch.Installed(true);
            SetBranch.Version(GetBranch.ServerVersion());

            appDispatcher.Invoke(new Action(() => { Managers.App.SetupAdvancedMenu(); }));

            Managers.App.SendNotification($"R5Reloaded ({GetBranch.Name()}) has been installed!", BalloonIcon.Info);

            Download.Tasks.SetInstallState(false);

            appDispatcher.Invoke(new Action(() => { Managers.App.ShowDownloadOptlFiles(); }));
        }

        public static async Task HDTextures()
        {
            if (AppState.IsInstalling)
                return;

            if (!AppState.IsOnline)
                return;

            if (GetBranch.IsLocalBranch())
                return;

            GameFiles uncompressedgameFiles = await Fetch.GameFiles(false, true);
            long requiredSpace = uncompressedgameFiles.files.Sum(f => f.size);
            if (!Managers.App.HasEnoughFreeSpace((string)Ini.Get(Ini.Vars.Library_Location), requiredSpace))
            {
                MessageBox.Show($"Not enough free space to install HD Textures.\n\nRequired: {requiredSpace / 1024 / 1024} MB", "R5Reloaded", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Download.Tasks.ConfigureConcurrency();
            Download.Tasks.ConfigureDownloadSpeed();

            Download.Tasks.SetOptionalInstallState(true);

            string branchDirectory = GetBranch.Directory();

            Download.Tasks.UpdateStatusLabel("Fetching optional files", Source.Installer);
            GameFiles optionalGameFiles = await Fetch.GameFiles(true, true);

            Download.Tasks.UpdateStatusLabel("Preparing optional downloads", Source.Installer);
            var optionaldownloadTasks = Download.Tasks.InitializeDownloadTasks(optionalGameFiles, branchDirectory);

            CancellationTokenSource cts = new CancellationTokenSource();
            Task updateTask = Download.Tasks.UpdateGlobalDownloadProgressAsync(cts.Token);

            Download.Tasks.ShowSpeedLabels(true, true);
            Download.Tasks.UpdateStatusLabel("Downloading optional files", Source.Installer);
            await Task.WhenAll(optionaldownloadTasks);
            Download.Tasks.ShowSpeedLabels(false, false);

            cts.Cancel();

            Download.Tasks.SetOptionalInstallState(false);

            SetBranch.DownloadHDTextures(true);

            appDispatcher.Invoke(new Action(() => { Settings_Control.gameInstalls.UpdateGameItems(); }));

            Managers.App.SendNotification($"R5Reloaded ({GetBranch.Name()}) optional files have been installed!", BalloonIcon.Info);
        }

        private static async Task AttemptGameRepair()
        {
            bool isRepaired = false;

            for (int i = 0; i < Launcher.MAX_REPAIR_ATTEMPTS; i++)
            {
                isRepaired = await Repair.Start();
                if (isRepaired) break;
            }

            AppState.BadFilesDetected = !isRepaired;
        }

        public static async Task LangFile(CheckBox checkBox, List<string> langs, bool bypass_block = false)
        {
            if (!AppState.IsOnline || (AppState.BlockLanguageInstall && !bypass_block))
                return;

            if (string.IsNullOrEmpty((string)Ini.Get(Ini.Vars.Library_Location)))
            {
                appDispatcher.Invoke(new Action(() => { Managers.App.ShowInstallLocation(); }));
                return;
            }

            GameFiles uncompressedgameFiles = await Fetch.LanguageFiles(langs, false);
            long requiredSpace = uncompressedgameFiles.files.Sum(f => f.size);
            if (!Managers.App.HasEnoughFreeSpace((string)Ini.Get(Ini.Vars.Library_Location), requiredSpace))
            {
                MessageBox.Show($"Not enough free space to install Language File.\n\nRequired: {requiredSpace / 1024 / 1024} MB", "R5Reloaded", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            appDispatcher.Invoke(() =>
            {
                if (checkBox != null)
                    checkBox.IsEnabled = false;
            });

            Download.Tasks.ConfigureConcurrency();
            Download.Tasks.ConfigureDownloadSpeed();

            string branchDirectory = GetBranch.Directory();

            GameFiles langFiles = await Fetch.LanguageFiles(langs);

            var langdownloadTasks = Download.Tasks.InitializeDownloadTasks(langFiles, branchDirectory);

            CancellationTokenSource cts = new CancellationTokenSource();
            Task updateTask = Download.Tasks.UpdateGlobalDownloadProgressAsync(cts.Token);

            Download.Tasks.ShowSpeedLabels(false, true);
            await Task.WhenAll(langdownloadTasks);
            Download.Tasks.ShowSpeedLabels(false, false);

            cts.Cancel();

            appDispatcher.Invoke(new Action(() =>
            {
                if (checkBox != null)
                    checkBox.IsEnabled = true;
            }));
        }
    }
}