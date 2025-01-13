using System.Numerics;
using System.Windows;
using System.Windows.Shapes;
using static launcher.Global;

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

            // Check if the game is already up to date
            if (Utilities.GetCurrentInstalledBranchVersion() == SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].currentVersion)
                return;

            // Check if user is to outdated to update normally
            if (Utilities.GetCurrentInstalledBranchVersion() != SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].lastVersion)
                await GameRepair.Start();

            // Install started
            Utilities.SetInstallState(true, "UPDATING");

            //Set download limits
            DownloadManager.SetSemaphoreLimit();
            DownloadManager.SetDownloadSpeedLimit();

            //Create branch library directory to store downloaded files
            string branchDirectory = FileManager.GetBranchDirectory();

            // Fetch patch files
            GamePatch patchFiles = await DataFetcher.FetchPatchFiles();

            // Prepare download tasks
            var downloadTasks = DownloadManager.PreparePatchDownloadTasks(patchFiles, branchDirectory);

            // Download patch files
            await Task.WhenAll(downloadTasks);

            // Prepare file patch tasks
            var filePatchTasks = DownloadManager.PrepareFilePatchTasks(patchFiles, branchDirectory);

            // Patch base game files
            await Task.WhenAll(filePatchTasks);

            // Update or create launcher config
            Ini.Set(SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].branch, "Is_Installed", true);
            Ini.Set(SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].branch, "Version", SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].currentVersion);

            // Install finished
            Utilities.SetInstallState(false);

            // Set update required to false
            UPDATE_REQUIRED = false;

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
            GamePatch patchFiles = await DataFetcher.FetchOptionalPatchFiles();

            // Prepare download tasks
            var downloadTasks = DownloadManager.PreparePatchDownloadTasks(patchFiles, branchDirectory);

            // Download patch files
            await Task.WhenAll(downloadTasks);

            // Prepare file patch tasks
            var filePatchTasks = DownloadManager.PrepareFilePatchTasks(patchFiles, branchDirectory);

            // Patch base game files
            await Task.WhenAll(filePatchTasks);

            Utilities.SetOptionalInstallState(false);
        }
    }
}