using System.IO;
using System.Numerics;
using System.Windows;
using System.Windows.Shapes;
using static launcher.Global;
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
            if (!IS_ONLINE)
                return;

            if (SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].is_local_branch)
                return;

            if (!SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].update_available)
                return;

            // Check if the game is already up to date
            if (Utilities.GetCurrentInstalledBranchVersion() == SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].currentVersion)
                return;

            // Check if user is to outdated to update normally
            if (Utilities.GetCurrentInstalledBranchVersion() != SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].lastVersion)
            {
                // Update the game without patching
                await UpdateWithoutPatching();

                // Game is now updated, no need to continue
                return;
            }

            // Install started
            Utilities.SetInstallState(true, "UPDATING");

            //Set download limits
            DownloadManager.SetSemaphoreLimit();
            DownloadManager.SetDownloadSpeedLimit();

            //Create branch library directory to store downloaded files
            string branchDirectory = FileManager.GetBranchDirectory();

            // Fetch patch files
            Utilities.UpdateStatusLabel("Fetching update files", Source.Update);
            GamePatch patchFiles = await DataFetcher.FetchPatchFiles();

            // Prepare download tasks
            Utilities.UpdateStatusLabel("Preparing update download", Source.Installer);
            var downloadTasks = DownloadManager.InitializeUpdateTasks(patchFiles, branchDirectory);

            // Download patch files
            Utilities.UpdateStatusLabel("Downloading update files", Source.Installer);
            await Task.WhenAll(downloadTasks);

            // Prepare file patch tasks
            Utilities.UpdateStatusLabel("Preparing file patching", Source.Installer);
            var filePatchTasks = DownloadManager.InitializeFileUpdateTasks(patchFiles, branchDirectory);

            // Patch base game files
            Utilities.UpdateStatusLabel("Patching game files", Source.Installer);
            await Task.WhenAll(filePatchTasks);

            // Update or create launcher config
            Ini.Set(SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].branch, "Is_Installed", true);
            Ini.Set(SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].branch, "Version", SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].currentVersion);

            // Install finished
            Utilities.SetInstallState(false);

            // Set update required to false
            SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].update_available = false;

            if (Ini.Get(SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].branch, "Download_HD_Textures", false))
                Task.Run(() => UpdateOptionalFiles());
        }

        private static async Task UpdateOptionalFiles()
        {
            Utilities.SetOptionalInstallState(true);

            //Set download limits
            DownloadManager.SetSemaphoreLimit();
            DownloadManager.SetDownloadSpeedLimit();

            //Create branch library directory to store downloaded files
            string branchDirectory = FileManager.GetBranchDirectory();

            // Fetch patch files
            Utilities.UpdateStatusLabel("Fetching optional list", Source.Update);
            GamePatch patchFiles = await DataFetcher.FetchOptionalPatchFiles();

            // Prepare download tasks
            Utilities.UpdateStatusLabel("Preparing optional download", Source.Update);
            var downloadTasks = DownloadManager.InitializeUpdateTasks(patchFiles, branchDirectory);

            // Download patch files
            Utilities.UpdateStatusLabel("Downloading optional files", Source.Update);
            await Task.WhenAll(downloadTasks);

            // Prepare file patch tasks
            Utilities.UpdateStatusLabel("Preparing optional patching", Source.Update);
            var filePatchTasks = DownloadManager.InitializeFileUpdateTasks(patchFiles, branchDirectory);

            // Patch base game files
            Utilities.UpdateStatusLabel("Patching optional files", Source.Update);
            await Task.WhenAll(filePatchTasks);

            Utilities.SetOptionalInstallState(false);
        }

        private static async Task UpdateWithoutPatching()
        {
            //Install started
            Utilities.SetInstallState(true, "UPDATING");

            //Set download limits
            DownloadManager.SetSemaphoreLimit();
            DownloadManager.SetDownloadSpeedLimit();

            //Create branch library directory to store downloaded files
            string branchDirectory = FileManager.GetBranchDirectory();

            //Prepare checksum tasks
            Utilities.UpdateStatusLabel("Preparing checksum tasks", Source.Update);
            var checksumTasks = FileManager.PrepareBaseGameChecksumTasks(branchDirectory);

            //Generate checksums for local files
            Utilities.UpdateStatusLabel("Generating local checksums", Source.Update);
            await Task.WhenAll(checksumTasks);

            //Fetch non compressed base game file list
            Utilities.UpdateStatusLabel("Fetching update files list", Source.Update);
            BaseGameFiles baseGameFiles = await DataFetcher.FetchBaseGameFiles(false);

            //Identify bad files
            Utilities.UpdateStatusLabel("Identifying changed files", Source.Update);
            int changedFileCount = FileManager.IdentifyBadFiles(baseGameFiles, checksumTasks, branchDirectory);

            //if bad files exist, download and repair
            if (changedFileCount > 0)
            {
                Utilities.UpdateStatusLabel("Preparing download tasks", Source.Update);
                var downloadTasks = DownloadManager.InitializeRepairTasks(branchDirectory);

                Utilities.UpdateStatusLabel("Downloading updated files", Source.Update);
                await Task.WhenAll(downloadTasks);

                Utilities.UpdateStatusLabel("Preparing decompression", Source.Update);
                var decompressionTasks = DecompressionManager.PrepareTasks(downloadTasks);

                Utilities.UpdateStatusLabel("Decompressing updated files", Source.Update);
                await Task.WhenAll(decompressionTasks);
            }

            //Update launcher config
            Ini.Set(SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].branch, "Is_Installed", true);
            Ini.Set(SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].branch, "Version", SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].currentVersion);

            //Install finished
            Utilities.SetInstallState(false);

            if (Ini.Get(SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].branch, "Download_HD_Textures", false))
                Task.Run(() => UpdateOptionalWithoutPatching());
        }

        private static async Task UpdateOptionalWithoutPatching()
        {
            Utilities.SetOptionalInstallState(true);

            //Set download limits
            DownloadManager.SetSemaphoreLimit();
            DownloadManager.SetDownloadSpeedLimit();

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
            Utilities.UpdateStatusLabel("Identifying changed files", Source.Repair);
            int changedFileCount = FileManager.IdentifyBadFiles(baseGameFiles, checksumTasks, branchDirectory);

            //if bad files exist, download and repair
            if (changedFileCount > 0)
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