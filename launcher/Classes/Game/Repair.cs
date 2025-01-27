using Hardcodet.Wpf.TaskbarNotification;
using launcher.Classes.BranchUtils;
using System.IO;
using static launcher.Classes.Utilities.Logger;
using System.Windows;
using static launcher.Classes.Global.References;
using launcher.Classes.Global;
using launcher.Classes.CDN;
using launcher.Classes.Utilities;
using launcher.Classes.Managers;

namespace launcher.Classes.Game
{
    public static class Repair
    {
        public static async Task<bool> Start()
        {
            if (AppState.IsInstalling)
                return false;

            if (!AppState.IsOnline)
                return false;

            if (GetBranch.IsLocalBranch())
                return false;

            if (GetBranch.UpdateAvailable())
            {
                Update_Button.Visibility = Visibility.Hidden;
                SetBranch.UpdateAvailable(false);
            }

            bool repairSuccess = true;

            DownloadManager.CreateDownloadMontior();

            //Install started
            DownloadManager.SetInstallState(true, "REPAIRING");

            //Set download limits
            DownloadManager.ConfigureConcurrency();
            DownloadManager.ConfigureDownloadSpeed();

            //Create branch library directory to store downloaded files
            string branchDirectory = GetBranch.Directory();

            //Prepare checksum tasks
            DownloadManager.UpdateStatusLabel("Preparing checksum tasks", Source.Repair);
            var checksumTasks = FileManager.PrepareBaseGameChecksumTasks(branchDirectory);

            //Generate checksums for local files
            DownloadManager.UpdateStatusLabel("Generating local checksums", Source.Repair);
            await Task.WhenAll(checksumTasks);

            //Fetch non compressed base game file list
            DownloadManager.UpdateStatusLabel("Fetching base game files list", Source.Repair);
            GameFiles gameFiles = await Fetch.GameFiles(false, false);

            //Identify bad files
            DownloadManager.UpdateStatusLabel("Identifying bad files", Source.Repair);
            int badFileCount = FileManager.IdentifyBadFiles(gameFiles, checksumTasks, branchDirectory);

            //if bad files exist, download and repair
            if (badFileCount > 0)
            {
                repairSuccess = false;

                DownloadManager.UpdateStatusLabel("Preparing download tasks", Source.Repair);
                var downloadTasks = DownloadManager.CreateRepairTasks(branchDirectory);

                DownloadManager.UpdateStatusLabel("Downloading repaired files", Source.Repair);
                await Task.WhenAll(downloadTasks);
            }

            if (GetBranch.Branch().mstr_languages.Contains(Configuration.language_name, StringComparer.OrdinalIgnoreCase) && Configuration.language_name != "english")
            {
                await Task.Run(() => LangFile([Configuration.language_name], true));
            }

            //Update launcher config
            SetBranch.Installed(true);
            SetBranch.Version(GetBranch.ServerVersion());

            AppManager.SetupAdvancedMenu();
            AppManager.SendNotification($"R5Reloaded ({GetBranch.Name()}) has been repaired!", BalloonIcon.Info);

            string[] find_opt_files = Directory.GetFiles(branchDirectory, "*.opt.starpak", SearchOption.AllDirectories);
            if (find_opt_files.Length > 0)
                SetBranch.DownloadHDTextures(true);

            //Install finished
            DownloadManager.SetInstallState(false);

            if (GetBranch.DownloadHDTextures())
                Task.Run(() => RepairOptionalFiles());

            return repairSuccess;
        }

        private static async Task RepairOptionalFiles()
        {
            DownloadManager.SetOptionalInstallState(true);

            //Set download limits
            DownloadManager.ConfigureConcurrency();
            DownloadManager.ConfigureDownloadSpeed();

            //Create branch library directory to store downloaded files
            string branchDirectory = GetBranch.Directory();

            //Prepare checksum tasks
            DownloadManager.UpdateStatusLabel("Preparing optional checksum tasks", Source.Repair);
            var checksumTasks = FileManager.PrepareOptionalGameChecksumTasks(branchDirectory);

            //Generate checksums for local files
            DownloadManager.UpdateStatusLabel("Generating optional checksums", Source.Repair);
            await Task.WhenAll(checksumTasks);

            //Fetch non compressed base game file list
            DownloadManager.UpdateStatusLabel("Fetching optional files list", Source.Repair);
            GameFiles gameFiles = await Fetch.GameFiles(false, true);

            //Identify bad files
            DownloadManager.UpdateStatusLabel("Identifying bad optional files", Source.Repair);
            int badFileCount = FileManager.IdentifyBadFiles(gameFiles, checksumTasks, branchDirectory);

            //if bad files exist, download and repair
            if (badFileCount > 0)
            {
                DownloadManager.UpdateStatusLabel("Preparing optional tasks", Source.Repair);
                var downloadTasks = DownloadManager.CreateRepairTasks(branchDirectory);

                DownloadManager.UpdateStatusLabel("Downloading optional files", Source.Repair);
                await Task.WhenAll(downloadTasks);
            }

            AppManager.SendNotification($"R5Reloaded ({GetBranch.Name()}) optional files have been repaired!", BalloonIcon.Info);

            DownloadManager.SetOptionalInstallState(false);
        }

        private static async Task LangFile(List<string> langs, bool bypass_block = false)
        {
            if (AppState.BlockLanguageInstall && !bypass_block)
                return;

            if (!AppState.IsOnline)
                return;

            DownloadManager.ConfigureConcurrency();
            DownloadManager.ConfigureDownloadSpeed();

            string branchDirectory = GetBranch.Directory();

            DownloadManager.UpdateStatusLabel("Preparing language checksum tasks", Source.Repair);
            var checksumTasks = FileManager.PrepareLangChecksumTasks(branchDirectory, langs);

            DownloadManager.UpdateStatusLabel("Fetching language files", Source.Repair);
            GameFiles langFiles = await Fetch.LanguageFiles(langs, false);

            //Identify bad files
            DownloadManager.UpdateStatusLabel("Identifying bad language files", Source.Repair);
            int badFileCount = FileManager.IdentifyBadFiles(langFiles, checksumTasks, branchDirectory);

            //if bad files exist, download and repair
            if (badFileCount > 0)
            {
                DownloadManager.UpdateStatusLabel("Preparing language tasks", Source.Repair);
                var downloadTasks = DownloadManager.CreateRepairTasks(branchDirectory);

                DownloadManager.UpdateStatusLabel("Downloading language files", Source.Repair);
                await Task.WhenAll(downloadTasks);
            }
        }
    }
}