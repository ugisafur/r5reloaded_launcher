using Hardcodet.Wpf.TaskbarNotification;
using System.IO;
using System.Numerics;
using System.Windows;
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

            if (Utilities.GetCurrentBranch().is_local_branch)
                return;

            if (!Utilities.GetCurrentBranch().update_available)
                return;

            // Check if the game is already up to date
            if (Utilities.GetBranchVersion() == Utilities.GetCurrentBranch().version)
                return;

            // Check if user is to outdated to update normally
            //if (Utilities.GetBranchVersion() != Utilities.GetCurrentBranch().lastVersion)
            {
                // Update the game without patching
                await UpdateWithoutPatching();

                // Game is now updated, no need to continue
                return;
            }

            // File patching is disabled for now until I patching tool is finished.

            /*
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

            string branch = Utilities.GetCurrentBranch().branch;

            // Update or create launcher config
            Ini.Set(branch, "Is_Installed", true);
            Ini.Set(branch, "Version", Utilities.GetCurrentBranch().version);

            // Install finished
            DownloadManager.SetInstallState(false);

            Utilities.SendNotification($"R5Reloaded ({Utilities.GetCurrentBranch().branch}) has been updated!", BalloonIcon.Info);

            // Set update required to false
            Utilities.GetCurrentBranch().update_available = false;

            if (Ini.Get(branch, "Download_HD_Textures", false))
                Task.Run(() => UpdateOptionalFiles());*/
        }

        /*private static async Task UpdateOptionalFiles()
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

            Utilities.SendNotification($"R5Reloaded ({Utilities.GetCurrentBranch().branch}) optional files have been updated!", BalloonIcon.Info);
        }*/

        private static async Task UpdateWithoutPatching()
        {
            //Install started
            DownloadManager.SetInstallState(true, "UPDATING");

            //Set download limits
            DownloadManager.ConfigureConcurrency();
            DownloadManager.ConfigureDownloadSpeed();

            //Create branch library directory to store downloaded files
            string branchDirectory = FileManager.GetBranchDirectory();

            //Check for deleted files
            await CheckForDeletedFiles(false);

            //Prepare checksum tasks
            DownloadManager.UpdateStatusLabel("Preparing checksum tasks", Source.Update);
            var checksumTasks = FileManager.PrepareBaseGameChecksumTasks(branchDirectory);

            //Generate checksums for local files
            DownloadManager.UpdateStatusLabel("Generating local checksums", Source.Update);
            await Task.WhenAll(checksumTasks);

            //Fetch non compressed base game file list
            DownloadManager.UpdateStatusLabel("Fetching update files list", Source.Update);
            GameFiles gameFiles = await DataFetcher.FetchBaseGameFiles(false);

            //Identify changed files
            DownloadManager.UpdateStatusLabel("Identifying changed files", Source.Update);
            int changedFileCount = FileManager.IdentifyBadFiles(gameFiles, checksumTasks, branchDirectory);

            //if changed files exist, download and update
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

            string branch = Utilities.GetCurrentBranch().branch;

            //Update launcher config
            Ini.Set(branch, "Is_Installed", true);
            Ini.Set(branch, "Version", Utilities.GetCurrentBranch().version);

            Utilities.SendNotification($"R5Reloaded ({Utilities.GetCurrentBranch().branch}) has been updated!", BalloonIcon.Info);

            Utilities.SetupAdvancedMenu();

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

            //Check for deleted files
            await CheckForDeletedFiles(true);

            //Prepare checksum tasks
            DownloadManager.UpdateStatusLabel("Preparing optional checksum tasks", Source.Repair);
            var checksumTasks = FileManager.PrepareOptionalGameChecksumTasks(branchDirectory);

            //Generate checksums for local files
            DownloadManager.UpdateStatusLabel("Generating optional checksums", Source.Repair);
            await Task.WhenAll(checksumTasks);

            //Fetch non compressed base game file list
            DownloadManager.UpdateStatusLabel("Fetching optional files list", Source.Repair);
            GameFiles gameFiles = await DataFetcher.FetchOptionalGameFiles(false);

            //Identify bad files
            DownloadManager.UpdateStatusLabel("Identifying changed files", Source.Repair);
            int changedFileCount = FileManager.IdentifyBadFiles(gameFiles, checksumTasks, branchDirectory);

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

            Utilities.SendNotification($"R5Reloaded ({Utilities.GetCurrentBranch().branch}) optional files have been updated!", BalloonIcon.Info);
        }

        private static async Task CheckForDeletedFiles(bool optfiles)
        {
            string branchDirectory = FileManager.GetBranchDirectory();
            string[] files = Directory.GetFiles(branchDirectory, "*", SearchOption.AllDirectories);
            GameFiles gameFiles = optfiles ? await DataFetcher.FetchOptionalGameFiles(false) : await DataFetcher.FetchBaseGameFiles(false);

            foreach (var file in files)
            {
                string relativePath = Path.GetRelativePath(branchDirectory, file);

                try
                {
                    if (!gameFiles.files.Exists(f => f.name.Equals(relativePath, StringComparison.OrdinalIgnoreCase)))
                    {
                        if (File.Exists(file))
                            File.Delete(file);
                    }
                }
                catch (Exception ex)
                {
                    LogError(Source.Update, $"Error deleting file ({relativePath}): {ex.Message}");
                }
            }
        }
    }
}