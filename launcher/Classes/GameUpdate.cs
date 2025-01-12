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

            string currentVersion = Ini.Get(Ini.Vars.Current_Version, "");
            // Check if the game is already up to date
            if (currentVersion == SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].currentVersion)
                return;

            // Check if user is to outdated to update normally
            if (currentVersion != SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].lastVersion)
                await GameRepair.Start();

            // Install started
            Utilities.SetInstallState(true, "UPDATING");

            //Set download limits
            DownloadManager.SetSemaphoreLimit();
            DownloadManager.SetDownloadSpeedLimit();

            // Create temp directory to store downloaded files
            string tempDirectory = FileManager.CreateTempDirectory();

            // Fetch patch files
            GamePatch patchFiles = await DataFetcher.FetchPatchFiles();

            // Prepare download tasks
            var downloadTasks = DownloadManager.PreparePatchDownloadTasks(patchFiles, tempDirectory);

            // Download patch files
            await Task.WhenAll(downloadTasks);

            // Prepare file patch tasks
            var filePatchTasks = DownloadManager.PrepareFilePatchTasks(patchFiles, tempDirectory);

            // Patch base game files
            await Task.WhenAll(filePatchTasks);

            // Update or create launcher config
            Ini.Set(Ini.Vars.Current_Version, SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].currentVersion);
            Ini.Set(Ini.Vars.Current_Branch, SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].branch);

            // Install finished
            Utilities.SetInstallState(false);

            // Set update required to false
            UPDATE_REQUIRED = false;

            //Delete temp directory
            await Task.Run(() => FileManager.CleanUpTempDirectory(tempDirectory));

            if (Ini.Get(Ini.Vars.Download_HD_Textures, false) && Ini.Get(Ini.Vars.HD_Textures_Installed, false))
                Task.Run(() => UpdateOptionalFiles());
        }

        private static async Task UpdateOptionalFiles()
        {
            //Set download limits
            DownloadManager.SetSemaphoreLimit();
            DownloadManager.SetDownloadSpeedLimit();

            // Create temp directory to store downloaded files
            string tempDirectory = FileManager.CreateTempDirectory();

            // Fetch patch files
            GamePatch patchFiles = await DataFetcher.FetchOptionalPatchFiles();

            // Prepare download tasks
            var downloadTasks = DownloadManager.PreparePatchDownloadTasks(patchFiles, tempDirectory);

            // Download patch files
            await Task.WhenAll(downloadTasks);

            // Prepare file patch tasks
            var filePatchTasks = DownloadManager.PrepareFilePatchTasks(patchFiles, tempDirectory);

            // Patch base game files
            await Task.WhenAll(filePatchTasks);

            //Delete temp directory
            await Task.Run(() => FileManager.CleanUpTempDirectory(tempDirectory));
        }
    }
}