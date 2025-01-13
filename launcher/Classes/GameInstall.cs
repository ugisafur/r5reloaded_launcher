using System.IO;
using static launcher.Global;
using static launcher.Logger;
using static launcher.ControlReferences;
using System.Windows;

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
        public static async void Start()
        {
            if (!IS_ONLINE)
                return;

            if (SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].is_local_branch)
                return;

            //Install started
            Utilities.SetInstallState(true, "INSTALLING");

            //Set download limits
            DownloadManager.SetSemaphoreLimit();
            DownloadManager.SetDownloadSpeedLimit();

            //Create branch library directory to store downloaded files
            string branchDirectory = FileManager.GetBranchDirectory();

            //Fetch compressed base game file list
            Utilities.UpdateStatusLabel("Fetching game files list", Source.Installer);
            BaseGameFiles baseGameFiles = await DataFetcher.FetchBaseGameFiles(true);

            //Prepare download tasks
            Utilities.UpdateStatusLabel("Preparing game download", Source.Installer);
            var downloadTasks = DownloadManager.PrepareDownloadTasks(baseGameFiles, branchDirectory);

            //Download base game files
            Utilities.UpdateStatusLabel("Downloading game files", Source.Installer);
            await Task.WhenAll(downloadTasks);

            //Prepare decompression tasks
            Utilities.UpdateStatusLabel("Preparing game decompression", Source.Installer);
            var decompressionTasks = DecompressionManager.PrepareTasks(downloadTasks);

            //Decompress base game files
            Utilities.UpdateStatusLabel("Decompressing game files", Source.Installer);
            await Task.WhenAll(decompressionTasks);

            //if bad files detected, attempt game repair
            if (BAD_FILES_DETECTED)
            {
                Utilities.UpdateStatusLabel("Reparing game files", Source.Installer);
                await AttemptGameRepair();
            }

            //Install finished
            Utilities.SetInstallState(false);

            //Set branch as installed
            Ini.Set(SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].branch, "Is_Installed", true);
            Ini.Set(SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].branch, "Version", SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].currentVersion);

            MessageBoxResult result = MessageBox.Show("The game installation is complete.Would you like to install the HD Textures? you can always choose to install them at another time, they are not required to play.", "Install HD Textures", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (result == MessageBoxResult.Yes)
                Ini.Set(SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].branch, "Download_HD_Textures", true);
            else
                Ini.Set(SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].branch, "Download_HD_Textures", false);

            //Install optional files if HD textures are enabled
            if (Ini.Get(SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].branch, "Download_HD_Textures", false))
                Task.Run(() => InstallOptionalFiles());
        }

        private static async Task InstallOptionalFiles()
        {
            Utilities.SetOptionalInstallState(true);

            //Set download limits
            DownloadManager.SetSemaphoreLimit();
            DownloadManager.SetDownloadSpeedLimit();

            //Create branch library directory to store downloaded files
            string branchDirectory = FileManager.GetBranchDirectory();

            //Fetch compressed base game file list
            Utilities.UpdateStatusLabel("Fetching optional files list", Source.Installer);
            BaseGameFiles optionalGameFiles = await DataFetcher.FetchOptionalGameFiles(true);

            //Prepare download tasks
            Utilities.UpdateStatusLabel("Preparing optional download", Source.Installer);
            var optionaldownloadTasks = DownloadManager.PrepareDownloadTasks(optionalGameFiles, branchDirectory);

            //Download base game files
            Utilities.UpdateStatusLabel("Downloading optional files", Source.Installer);
            await Task.WhenAll(optionaldownloadTasks);

            //Prepare decompression tasks
            Utilities.UpdateStatusLabel("Preparing decompression", Source.Installer);
            var decompressionTasks = DecompressionManager.PrepareTasks(optionaldownloadTasks);

            //Decompress base game files
            Utilities.UpdateStatusLabel("Decompressing optional files", Source.Installer);
            await Task.WhenAll(decompressionTasks);

            Utilities.SetOptionalInstallState(false);
        }

        private static async Task AttemptGameRepair()
        {
            bool isRepaired = false;

            for (int i = 0; i < Global.MAX_REPAIR_ATTEMPTS; i++)
            {
                isRepaired = await GameRepair.Start();
                if (isRepaired) break;
            }

            BAD_FILES_DETECTED = !isRepaired;
        }
    }
}