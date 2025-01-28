using Hardcodet.Wpf.TaskbarNotification;
using System.IO;
using static launcher.Global.Logger;
using static launcher.Global.References;
using System.Windows.Controls;
using launcher.Global;
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
                    Managers.App.ShowInstallLocation();
                }));
                return;
            }

            if (!GetBranch.EULAAccepted())
            {
                appDispatcher.Invoke(new Action(() =>
                {
                    Managers.App.ShowEULA();
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

            Download.Tasks.CreateDownloadMontior();
            Download.Tasks.ConfigureConcurrency();
            Download.Tasks.ConfigureDownloadSpeed();

            //Install started
            Download.Tasks.SetInstallState(true, "INSTALLING");

            //Create branch library directory to store downloaded files
            string branchDirectory = GetBranch.Directory();

            //Fetch compressed base game file list
            Download.Tasks.UpdateStatusLabel("Fetching game files list", Source.Installer);
            GameFiles gameFiles = await Fetch.GameFiles(true, false);

            //Prepare download tasks
            Download.Tasks.UpdateStatusLabel("Preparing game download", Source.Installer);
            var downloadTasks = Download.Tasks.CreateDownloadTasks(gameFiles, branchDirectory);

            //Download base game files
            Download.Tasks.UpdateStatusLabel("Downloading game files", Source.Installer);
            await Task.WhenAll(downloadTasks);

            //if bad files detected, attempt game repair
            if (AppState.BadFilesDetected)
            {
                Download.Tasks.UpdateStatusLabel("Reparing game files", Source.Installer);
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

            Managers.App.SetupAdvancedMenu();
            Managers.App.SendNotification($"R5Reloaded ({GetBranch.Name()}) has been installed!", BalloonIcon.Info);

            //Install finished
            Download.Tasks.SetInstallState(false);

            appDispatcher.Invoke(new Action(() =>
            {
                Managers.App.ShowDownloadOptlFiles();
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
            Download.Tasks.ConfigureConcurrency();
            Download.Tasks.ConfigureDownloadSpeed();

            Download.Tasks.SetOptionalInstallState(true);

            //Create branch library directory to store downloaded files
            string branchDirectory = GetBranch.Directory();

            //Fetch compressed base game file list
            Download.Tasks.UpdateStatusLabel("Fetching optional files list", Source.Installer);
            GameFiles optionalGameFiles = await Fetch.GameFiles(true, true);

            //Prepare download tasks
            Download.Tasks.UpdateStatusLabel("Preparing optional download", Source.Installer);
            var optionaldownloadTasks = Download.Tasks.CreateDownloadTasks(optionalGameFiles, branchDirectory);

            //Download base game files
            Download.Tasks.UpdateStatusLabel("Downloading optional files", Source.Installer);
            await Task.WhenAll(optionaldownloadTasks);

            Download.Tasks.SetOptionalInstallState(false);

            SetBranch.DownloadHDTextures(true);

            appDispatcher.Invoke(new Action(() =>
            {
                Settings_Control.gameInstalls.UpdateGameItems();
            }));

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
            if (AppState.BlockLanguageInstall && !bypass_block)
                return;

            if (string.IsNullOrEmpty((string)Ini.Get(Ini.Vars.Library_Location)))
            {
                appDispatcher.Invoke(new Action(() =>
                {
                    Managers.App.ShowInstallLocation();
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

            Download.Tasks.ConfigureConcurrency();
            Download.Tasks.ConfigureDownloadSpeed();

            string branchDirectory = GetBranch.Directory();

            GameFiles langFiles = await Fetch.LanguageFiles(langs);

            var langdownloadTasks = Download.Tasks.CreateDownloadTasks(langFiles, branchDirectory);

            await Task.WhenAll(langdownloadTasks);

            appDispatcher.Invoke(new Action(() =>
            {
                if (checkBox != null)
                    checkBox.IsEnabled = true;
            }));
        }
    }
}