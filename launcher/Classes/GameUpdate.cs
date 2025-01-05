namespace launcher
{
    public class GameUpdate
    {
        public async void Start()
        {
            // Check if the game is already up to date
            if (Helper.launcherConfig.currentUpdateVersion == Helper.serverConfig.branches[Helper.GetCmbBranchIndex()].currentVersion)
                return;

            // Check if user is to outdated to update normally
            if (Helper.launcherConfig.currentUpdateVersion != Helper.serverConfig.branches[Helper.GetCmbBranchIndex()].lastVersion)
                await Helper.gameRepair.Start();

            // Install started
            Helper.SetInstallState(true, "UPDATING");

            // Create temp directory to store downloaded files
            string tempDirectory = Helper.CreateTempDirectory();

            // Fetch patch files
            GamePatch patchFiles = await Helper.FetchPatchFiles();

            // Prepare download tasks
            var downloadTasks = Helper.PreparePatchDownloadTasks(patchFiles, tempDirectory);

            // Download patch files
            await Task.WhenAll(downloadTasks);

            // Prepare file patch tasks
            var filePatchTasks = Helper.PrepareFilePatchTasks(patchFiles, tempDirectory);

            // Patch base game files
            await Task.WhenAll(filePatchTasks);

            // Update or create launcher config
            Helper.UpdateOrCreateLauncherConfig();

            // Install finished
            Helper.SetInstallState(false);

            // Set update required to false
            Helper.updateRequired = false;

            //Delete temp directory
            await Task.Run(() => Helper.CleanUpTempDirectory(tempDirectory));
        }
    }
}