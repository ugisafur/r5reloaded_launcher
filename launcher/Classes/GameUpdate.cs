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
            if (!Global.isOnline)
                return;

            string currentVersion = Utilities.GetIniSetting(Utilities.IniSettings.Current_Version, "");
            // Check if the game is already up to date
            if (currentVersion == Global.serverConfig.branches[Utilities.GetCmbBranchIndex()].currentVersion)
                return;

            // Check if user is to outdated to update normally
            if (currentVersion != Global.serverConfig.branches[Utilities.GetCmbBranchIndex()].lastVersion)
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
            Utilities.SetIniSetting(Utilities.IniSettings.Current_Version, Global.serverConfig.branches[Utilities.GetCmbBranchIndex()].currentVersion);
            Utilities.SetIniSetting(Utilities.IniSettings.Current_Branch, Global.serverConfig.branches[Utilities.GetCmbBranchIndex()].branch);

            // Install finished
            Utilities.SetInstallState(false);

            // Set update required to false
            Global.updateRequired = false;

            //Delete temp directory
            await Task.Run(() => FileManager.CleanUpTempDirectory(tempDirectory));

            if (Utilities.GetIniSetting(Utilities.IniSettings.Download_HD_Textures, false) && Utilities.GetIniSetting(Utilities.IniSettings.HD_Textures_Installed, false))
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