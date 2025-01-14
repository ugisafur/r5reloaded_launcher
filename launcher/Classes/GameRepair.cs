using System.IO;
using static launcher.Global;
using static launcher.Logger;
using static launcher.ControlReferences;
using System.Windows;

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
            if (!IS_ONLINE)
                return false;

            if (SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].is_local_branch)
                return false;

            if (SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].update_available)
            {
                btnUpdate.Visibility = Visibility.Hidden;
                SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].update_available = false;
            }

            bool repairSuccess = true;

            //Install started
            Utilities.SetInstallState(true, "REPAIRING");

            //Set download limits
            DownloadManager.ConfigureConcurrency();
            DownloadManager.ConfigureDownloadSpeed();

            //Create branch library directory to store downloaded files
            string branchDirectory = FileManager.GetBranchDirectory();

            //Prepare checksum tasks
            Utilities.UpdateStatusLabel("Preparing checksum tasks", Source.Repair);
            var checksumTasks = FileManager.PrepareBaseGameChecksumTasks(branchDirectory);

            //Generate checksums for local files
            Utilities.UpdateStatusLabel("Generating local checksums", Source.Repair);
            await Task.WhenAll(checksumTasks);

            //Fetch non compressed base game file list
            Utilities.UpdateStatusLabel("Fetching base game files list", Source.Repair);
            BaseGameFiles baseGameFiles = await DataFetcher.FetchBaseGameFiles(false);

            //Identify bad files
            Utilities.UpdateStatusLabel("Identifying bad files", Source.Repair);
            int badFileCount = FileManager.IdentifyBadFiles(baseGameFiles, checksumTasks, branchDirectory);

            //if bad files exist, download and repair
            if (badFileCount > 0)
            {
                repairSuccess = false;

                Utilities.UpdateStatusLabel("Preparing download tasks", Source.Repair);
                var downloadTasks = DownloadManager.InitializeRepairTasks(branchDirectory);

                Utilities.UpdateStatusLabel("Downloading repaired files", Source.Repair);
                await Task.WhenAll(downloadTasks);

                Utilities.UpdateStatusLabel("Preparing decompression", Source.Repair);
                var decompressionTasks = DecompressionManager.PrepareTasks(downloadTasks);

                Utilities.UpdateStatusLabel("Decompressing repaired files", Source.Repair);
                await Task.WhenAll(decompressionTasks);
            }

            //Update launcher config
            Ini.Set(SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].branch, "Is_Installed", true);
            Ini.Set(SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].branch, "Version", SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].currentVersion);

            string[] find_opt_files = Directory.GetFiles(LAUNCHER_PATH, "*.opt.starpak", SearchOption.AllDirectories);
            if (find_opt_files.Length > 0)
                Ini.Set(SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].branch, "Download_HD_Textures", true);

            //Install finished
            Utilities.SetInstallState(false);

            if (Ini.Get(SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].branch, "Download_HD_Textures", false))
                Task.Run(() => RepairOptionalFiles());

            return repairSuccess;
        }

        private static async Task RepairOptionalFiles()
        {
            Utilities.SetOptionalInstallState(true);

            //Set download limits
            DownloadManager.ConfigureConcurrency();
            DownloadManager.ConfigureDownloadSpeed();

            //Create branch library directory to store downloaded files
            string branchDirectory = FileManager.GetBranchDirectory();

            //Prepare checksum tasks
            Utilities.UpdateStatusLabel("Preparing optional checksum tasks", Source.Repair);
            var checksumTasks = FileManager.PrepareOptionalGameChecksumTasks(branchDirectory);

            //Generate checksums for local files
            Utilities.UpdateStatusLabel("Generating optional checksums", Source.Repair);
            await Task.WhenAll(checksumTasks);

            //Fetch non compressed base game file list
            Utilities.UpdateStatusLabel("Fetching optional files list", Source.Repair);
            BaseGameFiles baseGameFiles = await DataFetcher.FetchOptionalGameFiles(false);

            //Identify bad files
            Utilities.UpdateStatusLabel("Identifying bad optional files", Source.Repair);
            int badFileCount = FileManager.IdentifyBadFiles(baseGameFiles, checksumTasks, branchDirectory);

            //if bad files exist, download and repair
            if (badFileCount > 0)
            {
                Utilities.UpdateStatusLabel("Preparing optional tasks", Source.Repair);
                var downloadTasks = DownloadManager.InitializeRepairTasks(branchDirectory);

                Utilities.UpdateStatusLabel("Downloading optional files", Source.Repair);
                await Task.WhenAll(downloadTasks);

                Utilities.UpdateStatusLabel("Preparing decompression", Source.Repair);
                var decompressionTasks = DecompressionManager.PrepareTasks(downloadTasks);

                Utilities.UpdateStatusLabel("Decompressing optional files", Source.Repair);
                await Task.WhenAll(decompressionTasks);
            }

            Utilities.SetOptionalInstallState(false);
        }
    }
}