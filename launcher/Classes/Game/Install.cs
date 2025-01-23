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

namespace launcher.Classes.Game
{
    public static class Install
    {
        public static async void Start()
        {
            if (AppState.IsInstalling)
                return;

            if (!AppState.IsOnline)
                return;

            if (GetBranch.IsLocalBranch())
                return;

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
    }
}