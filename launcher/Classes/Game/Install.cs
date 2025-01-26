using Hardcodet.Wpf.TaskbarNotification;
using launcher.Classes.BranchUtils;
using System.IO;
using static launcher.Classes.Utilities.Logger;
using static launcher.Classes.Global.References;
using launcher.Classes.Global;
using launcher.Classes.CDN;
using launcher.Classes.Game;
using launcher.Classes.Utilities;
using launcher.Classes.Managers;
using System.Windows.Controls;

namespace launcher.Classes.Game
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

            //Install started
            DownloadManager.SetInstallState(true, "INSTALLING");

            //Set download limits
            DownloadManager.ConfigureConcurrency();
            DownloadManager.ConfigureDownloadSpeed();

            //Create branch library directory to store downloaded files
            string branchDirectory = GetBranch.Directory();

            //Fetch compressed base game file list
            DownloadManager.UpdateStatusLabel("Fetching game files list", Source.Installer);
            GameFiles gameFiles = await Fetch.BranchFiles(true, false);

            //Prepare download tasks
            DownloadManager.UpdateStatusLabel("Preparing game download", Source.Installer);
            var downloadTasks = DownloadManager.InitializeDownloadTasks(gameFiles, branchDirectory);

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
            Ini.Set(GetBranch.Name(false), "Is_Installed", true);
            Ini.Set(GetBranch.Name(false), "Version", GetBranch.ServerVersion());

            AppManager.SetupAdvancedMenu();
            AppManager.SendNotification($"R5Reloaded ({GetBranch.Name()}) has been installed!", BalloonIcon.Info);

            //Install finished
            DownloadManager.SetInstallState(false);

            appDispatcher.Invoke(new Action(() =>
            {
                AppManager.ShowDownloadOptlFiles();
            }));
        }

        public static async Task InstallOptionalFiles()
        {
            if (AppState.IsInstalling)
                return;

            if (!AppState.IsOnline)
                return;

            if (GetBranch.IsLocalBranch())
                return;

            DownloadManager.SetOptionalInstallState(true);

            //Set download limits
            DownloadManager.ConfigureConcurrency();
            DownloadManager.ConfigureDownloadSpeed();

            //Create branch library directory to store downloaded files
            string branchDirectory = GetBranch.Directory();

            //Fetch compressed base game file list
            DownloadManager.UpdateStatusLabel("Fetching optional files list", Source.Installer);
            GameFiles optionalGameFiles = await Fetch.BranchFiles(true, true);

            //Prepare download tasks
            DownloadManager.UpdateStatusLabel("Preparing optional download", Source.Installer);
            var optionaldownloadTasks = DownloadManager.InitializeDownloadTasks(optionalGameFiles, branchDirectory);

            //Download base game files
            DownloadManager.UpdateStatusLabel("Downloading optional files", Source.Installer);
            await Task.WhenAll(optionaldownloadTasks);

            DownloadManager.SetOptionalInstallState(false);

            Ini.Set(GetBranch.Name(false), "Download_HD_Textures", true);

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

            GameFiles langFiles = await Fetch.LangFile(langs);

            var langdownloadTasks = DownloadManager.InitializeDownloadTasks(langFiles, branchDirectory);

            await Task.WhenAll(langdownloadTasks);

            appDispatcher.Invoke(new Action(() =>
            {
                if (checkBox != null)
                    checkBox.IsEnabled = true;
            }));
        }
    }
}