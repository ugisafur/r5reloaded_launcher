using Hardcodet.Wpf.TaskbarNotification;
using System.IO;
using static launcher.Utilities.Logger;
using static launcher.Global.References;
using System.Windows.Controls;
using launcher.Global;
using launcher.Utilities;
using launcher.Managers;
using launcher.BranchUtils;
using launcher.CDN;

namespace launcher.Game
{
    public static class Install
    {
        public static async void Start()
        {
            if (string.IsNullOrEmpty((string)Ini.Get(Ini.Vars.Library_Location)))
            {
                appDispatcher.Invoke(new Action(() =>
                {
                    AppManager.ShowInstallLocation();
                }));
                return;
            }

            if (!GetBranch.EULAAccepted())
            {
                appDispatcher.Invoke(new Action(() =>
                {
                    AppManager.ShowEULA();
                }));
                return;
            }

            if (AppState.IsInstalling)
                return;

            if (!AppState.IsOnline)
                return;

            if (GetBranch.IsLocalBranch())
                return;

            //If this branch exists were just going to repair it
            if (Directory.Exists(GetBranch.Directory()))
            {
                Task.Run(() => { Repair.Start(); });
                return;
            }

            DownloadManager.CreateDownloadMontior();
            DownloadManager.ConfigureConcurrency();
            DownloadManager.ConfigureDownloadSpeed();

            //Install started
            DownloadManager.SetInstallState(true, "INSTALLING");

            //Create branch library directory to store downloaded files
            string branchDirectory = GetBranch.Directory();

            //Fetch compressed base game file list
            DownloadManager.UpdateStatusLabel("Fetching game files list", Source.Installer);
            GameFiles gameFiles = await Fetch.GameFiles(true, false);

            //Prepare download tasks
            DownloadManager.UpdateStatusLabel("Preparing game download", Source.Installer);
            var downloadTasks = DownloadManager.CreateDownloadTasks(gameFiles, branchDirectory);

            //Download base game files
            DownloadManager.UpdateStatusLabel("Downloading game files", Source.Installer);
            await Task.WhenAll(downloadTasks);

            //if bad files detected, attempt game repair
            if (AppState.BadFilesDetected)
            {
                DownloadManager.UpdateStatusLabel("Reparing game files", Source.Installer);
                await AttemptGameRepair();
            }

            //Check if language files can to be installed
            LogInfo(Source.Installer, $"Checking system language against available game languages");
            if (GetBranch.Branch().mstr_languages.Contains(Configuration.language_name, StringComparer.OrdinalIgnoreCase) && Configuration.language_name != "english")
            {
                LogInfo(Source.Installer, $"game language found ({Configuration.language_name}), installing language files");
                await LangFile(null, [Configuration.language_name], true);
            }

            //Set branch as installed
            SetBranch.Installed(true);
            SetBranch.Version(GetBranch.ServerVersion());

            AppManager.SetupAdvancedMenu();
            AppManager.SendNotification($"R5Reloaded ({GetBranch.Name()}) has been installed!", BalloonIcon.Info);

            //Install finished
            DownloadManager.SetInstallState(false);

            appDispatcher.Invoke(new Action(() =>
            {
                AppManager.ShowDownloadOptlFiles();
            }));
        }

        public static async Task HDTextures()
        {
            if (AppState.IsInstalling)
                return;

            if (!AppState.IsOnline)
                return;

            if (GetBranch.IsLocalBranch())
                return;

            //Set download limits
            DownloadManager.ConfigureConcurrency();
            DownloadManager.ConfigureDownloadSpeed();

            DownloadManager.SetOptionalInstallState(true);

            //Create branch library directory to store downloaded files
            string branchDirectory = GetBranch.Directory();

            //Fetch compressed base game file list
            DownloadManager.UpdateStatusLabel("Fetching optional files list", Source.Installer);
            GameFiles optionalGameFiles = await Fetch.GameFiles(true, true);

            //Prepare download tasks
            DownloadManager.UpdateStatusLabel("Preparing optional download", Source.Installer);
            var optionaldownloadTasks = DownloadManager.CreateDownloadTasks(optionalGameFiles, branchDirectory);

            //Download base game files
            DownloadManager.UpdateStatusLabel("Downloading optional files", Source.Installer);
            await Task.WhenAll(optionaldownloadTasks);

            DownloadManager.SetOptionalInstallState(false);

            SetBranch.DownloadHDTextures(true);

            appDispatcher.Invoke(new Action(() =>
            {
                Settings_Control.gameInstalls.UpdateGameItems();
            }));

            AppManager.SendNotification($"R5Reloaded ({GetBranch.Name()}) optional files have been installed!", BalloonIcon.Info);
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
            if (AppState.BlockLanguageInstall && !bypass_block)
                return;

            if (string.IsNullOrEmpty((string)Ini.Get(Ini.Vars.Library_Location)))
            {
                appDispatcher.Invoke(new Action(() =>
                {
                    AppManager.ShowInstallLocation();
                }));
                return;
            }

            if (!AppState.IsOnline)
                return;

            appDispatcher.Invoke(() =>
            {
                if (checkBox != null)
                    checkBox.IsEnabled = false;
            });

            DownloadManager.ConfigureConcurrency();
            DownloadManager.ConfigureDownloadSpeed();

            string branchDirectory = GetBranch.Directory();

            GameFiles langFiles = await Fetch.LanguageFiles(langs);

            var langdownloadTasks = DownloadManager.CreateDownloadTasks(langFiles, branchDirectory);

            await Task.WhenAll(langdownloadTasks);

            appDispatcher.Invoke(new Action(() =>
            {
                if (checkBox != null)
                    checkBox.IsEnabled = true;
            }));
        }
    }
}