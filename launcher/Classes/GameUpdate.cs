using System.IO;
using System.Numerics;
using System.Windows;
using System.Windows.Shapes;
using static launcher.Logger;

namespace launcher
{
    /// <summary>
    /// The GameUpdate class is responsible for managing the update process of the game.
    /// It performs several tasks asynchronously to ensure the game is up to date:
    /// 1. Checks if the game is already up to date.
    /// 2. If the game version is too outdated, it initiates a repair process.
    /// 3. Sets the installation state to "UPDATING".
    /// 4. Creates a temporary directory to store downloaded patch files.
    /// 5. Fetches the necessary patch files from the server.
    /// 6. Prepares and executes download tasks for the patch files.
    /// 7. Prepares and executes tasks to patch the base game files.
    /// 8. Updates or creates the launcher configuration file.
    /// 9. Resets the installation state and marks the update as complete.
    /// 10. Cleans up the temporary directory used for the update process.
    /// </summary>
    public class GameUpdate
    {
        public static async void Start()
        {
            if (!AppState.IsOnline)
                return;

            if (Configuration.ServerConfig.branches[Utilities.GetCmbBranchIndex()].is_local_branch)
                return;

            if (!Configuration.ServerConfig.branches[Utilities.GetCmbBranchIndex()].update_available)
                return;

            // Check if the game is already up to date
            if (Utilities.GetBranchVersion() == Configuration.ServerConfig.branches[Utilities.GetCmbBranchIndex()].currentVersion)
                return;

            // Check if user is to outdated to update normally
            if (Utilities.GetBranchVersion() != Configuration.ServerConfig.branches[Utilities.GetCmbBranchIndex()].lastVersion)
            {
                // Update the game without patching
                await UpdateWithoutPatching();

                // Game is now updated, no need to continue
                return;
            }

            // Install started
            DownloadManager.SetInstallState(true, "UPDATING");

            //Set download limits
            DownloadManager.ConfigureConcurrency();
            DownloadManager.ConfigureDownloadSpeed();

            //Create branch library directory to store downloaded files
            string branchDirectory = FileManager.GetBranchDirectory();

            // Fetch patch files
            DownloadManager.UpdateStatusLabel("Fetching update files", Source.Update);
            GamePatch patchFiles = await DataFetcher.FetchPatchFiles();

            // Prepare download tasks
            DownloadManager.UpdateStatusLabel("Preparing update download", Source.Installer);
            var downloadTasks = DownloadManager.InitializeUpdateTasks(patchFiles, branchDirectory);

            // Download patch files
            DownloadManager.UpdateStatusLabel("Downloading update files", Source.Installer);
            await Task.WhenAll(downloadTasks);

            // Prepare file patch tasks
            DownloadManager.UpdateStatusLabel("Preparing file patching", Source.Installer);
            var filePatchTasks = DownloadManager.InitializeFileUpdateTasks(patchFiles, branchDirectory);

            // Patch base game files
            DownloadManager.UpdateStatusLabel("Patching game files", Source.Installer);
            await Task.WhenAll(filePatchTasks);

            string branch = Configuration.ServerConfig.branches[Utilities.GetCmbBranchIndex()].branch;

            // Update or create launcher config
            Ini.Set(branch, "Is_Installed", true);
            Ini.Set(branch, "Version", Configuration.ServerConfig.branches[Utilities.GetCmbBranchIndex()].currentVersion);

            // Install finished
            DownloadManager.SetInstallState(false);

            // Set update required to false
            Configuration.ServerConfig.branches[Utilities.GetCmbBranchIndex()].update_available = false;

            if (Ini.Get(branch, "Download_HD_Textures", false))
                Task.Run(() => UpdateOptionalFiles());
        }

        private static async Task UpdateOptionalFiles()
        {
            DownloadManager.SetOptionalInstallState(true);

            //Set download limits
            DownloadManager.ConfigureConcurrency();
            DownloadManager.ConfigureDownloadSpeed();

            //Create branch library directory to store downloaded files
            string branchDirectory = FileManager.GetBranchDirectory();

            // Fetch patch files
            DownloadManager.UpdateStatusLabel("Fetching optional list", Source.Update);
            GamePatch patchFiles = await DataFetcher.FetchOptionalPatchFiles();

            // Prepare download tasks
            DownloadManager.UpdateStatusLabel("Preparing optional download", Source.Update);
            var downloadTasks = DownloadManager.InitializeUpdateTasks(patchFiles, branchDirectory);

            // Download patch files
            DownloadManager.UpdateStatusLabel("Downloading optional files", Source.Update);
            await Task.WhenAll(downloadTasks);

            // Prepare file patch tasks
            DownloadManager.UpdateStatusLabel("Preparing optional patching", Source.Update);
            var filePatchTasks = DownloadManager.InitializeFileUpdateTasks(patchFiles, branchDirectory);

            // Patch base game files
            DownloadManager.UpdateStatusLabel("Patching optional files", Source.Update);
            await Task.WhenAll(filePatchTasks);

            DownloadManager.SetOptionalInstallState(false);
        }

        private static async Task UpdateWithoutPatching()
        {
            //Install started
            DownloadManager.SetInstallState(true, "UPDATING");

            //Set download limits
            DownloadManager.ConfigureConcurrency();
            DownloadManager.ConfigureDownloadSpeed();

            //Create branch library directory to store downloaded files
            string branchDirectory = FileManager.GetBranchDirectory();

            //Prepare checksum tasks
            DownloadManager.UpdateStatusLabel("Preparing checksum tasks", Source.Update);
            var checksumTasks = FileManager.PrepareBaseGameChecksumTasks(branchDirectory);

            //Generate checksums for local files
            DownloadManager.UpdateStatusLabel("Generating local checksums", Source.Update);
            await Task.WhenAll(checksumTasks);

            //Fetch non compressed base game file list
            DownloadManager.UpdateStatusLabel("Fetching update files list", Source.Update);
            BaseGameFiles baseGameFiles = await DataFetcher.FetchBaseGameFiles(false);

            //Identify bad files
            DownloadManager.UpdateStatusLabel("Identifying changed files", Source.Update);
            int changedFileCount = FileManager.IdentifyBadFiles(baseGameFiles, checksumTasks, branchDirectory);

            //if bad files exist, download and repair
            if (changedFileCount > 0)
            {
                DownloadManager.UpdateStatusLabel("Preparing download tasks", Source.Update);
                var downloadTasks = DownloadManager.InitializeRepairTasks(branchDirectory);

                DownloadManager.UpdateStatusLabel("Downloading updated files", Source.Update);
                await Task.WhenAll(downloadTasks);

                DownloadManager.UpdateStatusLabel("Preparing decompression", Source.Update);
                var decompressionTasks = DecompressionManager.PrepareTasks(downloadTasks);

                DownloadManager.UpdateStatusLabel("Decompressing updated files", Source.Update);
                await Task.WhenAll(decompressionTasks);
            }

            string branch = Configuration.ServerConfig.branches[Utilities.GetCmbBranchIndex()].branch;

            //Update launcher config
            Ini.Set(branch, "Is_Installed", true);
            Ini.Set(branch, "Version", Configuration.ServerConfig.branches[Utilities.GetCmbBranchIndex()].currentVersion);

            //Install finished
            DownloadManager.SetInstallState(false);

            if (Ini.Get(branch, "Download_HD_Textures", false))
                Task.Run(() => UpdateOptionalWithoutPatching());
        }

        private static async Task UpdateOptionalWithoutPatching()
        {
            DownloadManager.SetOptionalInstallState(true);

            //Set download limits
            DownloadManager.ConfigureConcurrency();
            DownloadManager.ConfigureDownloadSpeed();

            //Create branch library directory to store downloaded files
            string branchDirectory = FileManager.GetBranchDirectory();

            //Prepare checksum tasks
            DownloadManager.UpdateStatusLabel("Preparing optional checksum tasks", Source.Repair);
            var checksumTasks = FileManager.PrepareOptionalGameChecksumTasks(branchDirectory);

            //Generate checksums for local files
            DownloadManager.UpdateStatusLabel("Generating optional checksums", Source.Repair);
            await Task.WhenAll(checksumTasks);

            //Fetch non compressed base game file list
            DownloadManager.UpdateStatusLabel("Fetching optional files list", Source.Repair);
            BaseGameFiles baseGameFiles = await DataFetcher.FetchOptionalGameFiles(false);

            //Identify bad files
            DownloadManager.UpdateStatusLabel("Identifying changed files", Source.Repair);
            int changedFileCount = FileManager.IdentifyBadFiles(baseGameFiles, checksumTasks, branchDirectory);

            //if bad files exist, download and repair
            if (changedFileCount > 0)
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

            DownloadManager.SetOptionalInstallState(false);
        }
    }
}