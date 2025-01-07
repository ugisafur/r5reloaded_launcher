using System.IO;

namespace launcher
{
    /// <summary>
    /// The GameInstall class handles the installation process of a game.
    /// It includes methods to start the installation, download necessary files,
    /// decompress them, and repair any corrupted files if detected.
    ///
    /// The Start method performs the following steps:
    /// 1. Sets the installation state to "INSTALLING".
    /// 2. Creates a temporary directory to store downloaded files.
    /// 3. Fetches the list of base game files.
    /// 4. Prepares download tasks for the base game files.
    /// 5. Downloads the base game files.
    /// 6. Prepares decompression tasks for the downloaded files.
    /// 7. Decompresses the downloaded files.
    /// 8. If any bad files are detected, attempts to repair the game files.
    /// 9. Updates or creates the launcher configuration.
    /// 10. Sets the installation state to false, indicating the installation is complete.
    /// 11. Marks the game as installed.
    /// 12. Cleans up the temporary directory used for downloading files.
    ///
    /// The AttemptGameRepair method tries to repair the game files if any bad files are detected.
    /// It makes multiple attempts (up to a maximum defined by Global.MAX_REPAIR_ATTEMPTS) to repair the files.
    /// </summary>
    public class GameInstall
    {
        public async void Start()
        {
            //Install started
            Utilities.SetInstallState(true, "INSTALLING");

            //Create temp directory to store downloaded files
            string tempDirectory = FileManager.CreateTempDirectory();

            //Fetch compressed base game file list
            Utilities.UpdateStatusLabel("Fetching base game files list", Logger.Source.Installer);
            BaseGameFiles baseGameFiles = await DataFetcher.FetchBaseGameFiles(true);

            //Prepare download tasks
            Utilities.UpdateStatusLabel("Preparing game download", Logger.Source.Installer);
            var downloadTasks = DownloadManager.PrepareDownloadTasks(baseGameFiles, tempDirectory);

            //Download base game files
            Utilities.UpdateStatusLabel("Downloading game files", Logger.Source.Installer);
            await Task.WhenAll(downloadTasks);

            //Prepare decompression tasks
            Utilities.UpdateStatusLabel("Preparing game decompression", Logger.Source.Installer);
            var decompressionTasks = DecompressionManager.PrepareTasks(downloadTasks);

            //Decompress base game files
            Utilities.UpdateStatusLabel("Decompressing game files", Logger.Source.Installer);
            await Task.WhenAll(decompressionTasks);

            //if bad files detected, attempt game repair
            if (Global.badFilesDetected)
            {
                Utilities.UpdateStatusLabel("Reparing game files", Logger.Source.Installer);
                await AttemptGameRepair();
            }

            //Update or create launcher config
            FileManager.UpdateOrCreateLauncherConfig();

            //Install finished
            Utilities.SetInstallState(false);

            //Set game as installed
            Global.isInstalled = true;

            //Delete temp directory
            if (Directory.Exists(tempDirectory))
                await Task.Run(() => FileManager.CleanUpTempDirectory(tempDirectory));
        }

        private async Task AttemptGameRepair()
        {
            bool isRepaired = false;

            for (int i = 0; i < Global.MAX_REPAIR_ATTEMPTS; i++)
            {
                isRepaired = await ControlReferences.gameRepair.Start();
                if (isRepaired) break;
            }

            Global.badFilesDetected = !isRepaired;
        }
    }
}