using Hardcodet.Wpf.TaskbarNotification;
using System.IO;
using static launcher.Global.Logger;
using System.Windows;
using static launcher.Global.References;
using launcher.Global;
using launcher.Managers;
using launcher.BranchUtils;
using launcher.CDN;

namespace launcher.Game
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

            Download.Tasks.CreateDownloadMontior();
            Download.Tasks.ConfigureConcurrency();
            Download.Tasks.ConfigureDownloadSpeed();

            //Install started
            Download.Tasks.SetInstallState(true, "REPAIRING");

            //Create branch library directory to store downloaded files
            string branchDirectory = GetBranch.Directory();

            //Prepare checksum tasks
            Download.Tasks.UpdateStatusLabel("Preparing checksum tasks", Source.Repair);
            var checksumTasks = Checksums.PrepareBranchChecksumTasks(branchDirectory);

            //Generate checksums for local files
            Download.Tasks.UpdateStatusLabel("Generating local checksums", Source.Repair);
            await Task.WhenAll(checksumTasks);

            //Fetch non compressed base game file list
            Download.Tasks.UpdateStatusLabel("Fetching base game files list", Source.Repair);
            GameFiles gameFiles = await Fetch.GameFiles(false, false);

            //Identify bad files
            Download.Tasks.UpdateStatusLabel("Identifying bad files", Source.Repair);
            int badFileCount = Checksums.IdentifyBadFiles(gameFiles, checksumTasks, branchDirectory);

            //if bad files exist, download and repair
            if (badFileCount > 0)
            {
                repairSuccess = false;

                Download.Tasks.UpdateStatusLabel("Preparing download tasks", Source.Repair);
                var downloadTasks = Download.Tasks.InitializeRepairTasks(branchDirectory);

                Download.Tasks.UpdateStatusLabel("Downloading repaired files", Source.Repair);
                await Task.WhenAll(downloadTasks);
            }

            if (GetBranch.Branch().mstr_languages.Contains(Configuration.language_name, StringComparer.OrdinalIgnoreCase) && Configuration.language_name != "english")
            {
                await Task.Run(() => LangFile([Configuration.language_name], true));
            }

            //Update launcher config
            SetBranch.Installed(true);
            SetBranch.Version(GetBranch.ServerVersion());

            Managers.App.SetupAdvancedMenu();
            Managers.App.SendNotification($"R5Reloaded ({GetBranch.Name()}) has been repaired!", BalloonIcon.Info);

            string[] find_opt_files = Directory.GetFiles(branchDirectory, "*.opt.starpak", SearchOption.AllDirectories);
            if (find_opt_files.Length > 0)
                SetBranch.DownloadHDTextures(true);

            //Install finished
            Download.Tasks.SetInstallState(false);

            if (GetBranch.DownloadHDTextures())
                Task.Run(() => RepairOptionalFiles());

            return repairSuccess;
        }

        private static async Task RepairOptionalFiles()
        {
            Download.Tasks.ConfigureConcurrency();
            Download.Tasks.ConfigureDownloadSpeed();

            Download.Tasks.SetOptionalInstallState(true);

            //Create branch library directory to store downloaded files
            string branchDirectory = GetBranch.Directory();

            //Prepare checksum tasks
            Download.Tasks.UpdateStatusLabel("Preparing optional checksum tasks", Source.Repair);
            var checksumTasks = Checksums.PrepareOptChecksumTasks(branchDirectory);

            //Generate checksums for local files
            Download.Tasks.UpdateStatusLabel("Generating optional checksums", Source.Repair);
            await Task.WhenAll(checksumTasks);

            //Fetch non compressed base game file list
            Download.Tasks.UpdateStatusLabel("Fetching optional files list", Source.Repair);
            GameFiles gameFiles = await Fetch.GameFiles(false, true);

            //Identify bad files
            Download.Tasks.UpdateStatusLabel("Identifying bad optional files", Source.Repair);
            int badFileCount = Checksums.IdentifyBadFiles(gameFiles, checksumTasks, branchDirectory);

            //if bad files exist, download and repair
            if (badFileCount > 0)
            {
                Download.Tasks.UpdateStatusLabel("Preparing optional tasks", Source.Repair);
                var downloadTasks = Download.Tasks.InitializeRepairTasks(branchDirectory);

                Download.Tasks.UpdateStatusLabel("Downloading optional files", Source.Repair);
                await Task.WhenAll(downloadTasks);
            }

            Managers.App.SendNotification($"R5Reloaded ({GetBranch.Name()}) optional files have been repaired!", BalloonIcon.Info);

            Download.Tasks.SetOptionalInstallState(false);
        }

        private static async Task LangFile(List<string> langs, bool bypass_block = false)
        {
            if (AppState.BlockLanguageInstall && !bypass_block)
                return;

            if (!AppState.IsOnline)
                return;

            Download.Tasks.ConfigureConcurrency();
            Download.Tasks.ConfigureDownloadSpeed();

            string branchDirectory = GetBranch.Directory();

            Download.Tasks.UpdateStatusLabel("Preparing language checksum tasks", Source.Repair);
            var checksumTasks = Checksums.PrepareLangChecksumTasks(branchDirectory, langs);

            Download.Tasks.UpdateStatusLabel("Fetching language files", Source.Repair);
            GameFiles langFiles = await Fetch.LanguageFiles(langs, false);

            //Identify bad files
            Download.Tasks.UpdateStatusLabel("Identifying bad language files", Source.Repair);
            int badFileCount = Checksums.IdentifyBadFiles(langFiles, checksumTasks, branchDirectory);

            //if bad files exist, download and repair
            if (badFileCount > 0)
            {
                Download.Tasks.UpdateStatusLabel("Preparing language tasks", Source.Repair);
                var downloadTasks = Download.Tasks.InitializeRepairTasks(branchDirectory);

                Download.Tasks.UpdateStatusLabel("Downloading language files", Source.Repair);
                await Task.WhenAll(downloadTasks);
            }
        }
    }
}