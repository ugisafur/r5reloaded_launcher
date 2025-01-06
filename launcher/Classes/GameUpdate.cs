namespace launcher
{
    public class GameUpdate
    {
        public async void Start()
        {
            // Check if the game is already up to date
            if (Global.launcherConfig.currentUpdateVersion == Global.serverConfig.branches[Utilities.GetCmbBranchIndex()].currentVersion)
                return;

            // Check if user is to outdated to update normally
            if (Global.launcherConfig.currentUpdateVersion != Global.serverConfig.branches[Utilities.GetCmbBranchIndex()].lastVersion)
                await ControlReferences.gameRepair.Start();

            // Install started
            Utilities.SetInstallState(true, "UPDATING");

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
            FileManager.UpdateOrCreateLauncherConfig();

            // Install finished
            Utilities.SetInstallState(false);

            // Set update required to false
            Global.updateRequired = false;

            //Delete temp directory
            await Task.Run(() => FileManager.CleanUpTempDirectory(tempDirectory));
        }
    }
}