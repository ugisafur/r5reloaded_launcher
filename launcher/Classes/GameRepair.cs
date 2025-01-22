using System.IO;

using static launcher.Logger;
using static launcher.ControlReferences;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;

namespace launcher
{
    /// <summary>
    /// The GameRepair class is responsible for repairing the game installation.
    /// It performs several tasks such as generating checksums, identifying corrupted files,
    /// downloading and decompressing repaired files, and updating the launcher configuration.
    /// </summary>
    public class GameRepair
    {
        public static async Task<bool> Start()
        {
            if (AppState.IsInstalling)
                return false;

            if (!AppState.IsOnline)
                return false;

            if (Utilities.GetCurrentBranch().is_local_branch)
                return false;

            if (Utilities.GetCurrentBranch().update_available)
            {
                Update_Button.Visibility = Visibility.Hidden;
                Utilities.GetCurrentBranch().update_available = false;
            }

            bool repairSuccess = true;

            //Install started
            DownloadManager.SetInstallState(true, "REPAIRING");

            //Set download limits
            DownloadManager.ConfigureConcurrency();
            DownloadManager.ConfigureDownloadSpeed();

            //Create branch library directory to store downloaded files
            string branchDirectory = Utilities.GetBranchDirectory();

            //Prepare checksum tasks
            DownloadManager.UpdateStatusLabel("Preparing checksum tasks", Source.Repair);
            var checksumTasks = FileManager.PrepareBaseGameChecksumTasks(branchDirectory);

            //Generate checksums for local files
            DownloadManager.UpdateStatusLabel("Generating local checksums", Source.Repair);
            await Task.WhenAll(checksumTasks);

            //Fetch non compressed base game file list
            DownloadManager.UpdateStatusLabel("Fetching base game files list", Source.Repair);
            GameFiles gameFiles = await DataFetcher.FetchBaseGameFiles(false);

            //Identify bad files
            DownloadManager.UpdateStatusLabel("Identifying bad files", Source.Repair);
            int badFileCount = FileManager.IdentifyBadFiles(gameFiles, checksumTasks, branchDirectory);

            //if bad files exist, download and repair
            if (badFileCount > 0)
            {
                repairSuccess = false;

                DownloadManager.UpdateStatusLabel("Preparing download tasks", Source.Repair);
                var downloadTasks = DownloadManager.InitializeRepairTasks(branchDirectory);

                DownloadManager.UpdateStatusLabel("Downloading repaired files", Source.Repair);
                await Task.WhenAll(downloadTasks);

                DownloadManager.UpdateStatusLabel("Preparing decompression", Source.Repair);
                var decompressionTasks = DecompressionManager.PrepareTasks(downloadTasks);

                DownloadManager.UpdateStatusLabel("Decompressing repaired files", Source.Repair);
                await Task.WhenAll(decompressionTasks);
            }

            string branch = Utilities.GetCurrentBranch().branch;

            //Update launcher config
            Ini.Set(branch, "Is_Installed", true);
            Ini.Set(branch, "Version", Utilities.GetServerBranchVersion(Utilities.GetCurrentBranch()));

            Utilities.SetupAdvancedMenu();
            Utilities.SendNotification($"R5Reloaded ({Utilities.GetCurrentBranch().branch}) has been repaired!", BalloonIcon.Info);

            string[] find_opt_files = Directory.GetFiles(branchDirectory, "*.opt.starpak", SearchOption.AllDirectories);
            if (find_opt_files.Length > 0)
                Ini.Set(branch, "Download_HD_Textures", true);

            //Install finished
            DownloadManager.SetInstallState(false);

            if (Ini.Get(branch, "Download_HD_Textures", false))
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
            string branchDirectory = Utilities.GetBranchDirectory();

            //Prepare checksum tasks
            DownloadManager.UpdateStatusLabel("Preparing optional checksum tasks", Source.Repair);
            var checksumTasks = FileManager.PrepareOptionalGameChecksumTasks(branchDirectory);

            //Generate checksums for local files
            DownloadManager.UpdateStatusLabel("Generating optional checksums", Source.Repair);
            await Task.WhenAll(checksumTasks);

            //Fetch non compressed base game file list
            DownloadManager.UpdateStatusLabel("Fetching optional files list", Source.Repair);
            GameFiles gameFiles = await DataFetcher.FetchOptionalGameFiles(false);

            //Identify bad files
            DownloadManager.UpdateStatusLabel("Identifying bad optional files", Source.Repair);
            int badFileCount = FileManager.IdentifyBadFiles(gameFiles, checksumTasks, branchDirectory);

            //if bad files exist, download and repair
            if (badFileCount > 0)
            {
                DownloadManager.UpdateStatusLabel("Preparing optional tasks", Source.Repair);
                var downloadTasks = DownloadManager.InitializeRepairTasks(branchDirectory);

                DownloadManager.UpdateStatusLabel("Downloading optional files", Source.Repair);
                await Task.WhenAll(downloadTasks);

                DownloadManager.UpdateStatusLabel("Preparing decompression", Source.Repair);
                var decompressionTasks = DecompressionManager.PrepareTasks(downloadTasks);

                DownloadManager.UpdateStatusLabel("Decompressing optional files", Source.Repair);
                await Task.WhenAll(decompressionTasks);
            }

            Utilities.SendNotification($"R5Reloaded ({Utilities.GetCurrentBranch().branch}) optional files have been repaired!", BalloonIcon.Info);

            DownloadManager.SetOptionalInstallState(false);
        }
    }
}