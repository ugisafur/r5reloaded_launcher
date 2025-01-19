using System.IO;

using static launcher.Logger;
using static launcher.ControlReferences;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;

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
            if (AppState.IsInstalling)
                return;

            if (!AppState.IsOnline)
                return;

            if (Utilities.GetCurrentBranch().is_local_branch)
                return;

            //Install started
            DownloadManager.SetInstallState(true, "INSTALLING");

            //Set download limits
            DownloadManager.ConfigureConcurrency();
            DownloadManager.ConfigureDownloadSpeed();

            //Create branch library directory to store downloaded files
            string branchDirectory = Utilities.GetBranchDirectory();

            //Fetch compressed base game file list
            DownloadManager.UpdateStatusLabel("Fetching game files list", Source.Installer);
            GameFiles gameFiles = await DataFetcher.FetchBaseGameFiles(true);

            //Prepare download tasks
            DownloadManager.UpdateStatusLabel("Preparing game download", Source.Installer);
            var downloadTasks = DownloadManager.InitializeDownloadTasks(gameFiles, branchDirectory);

            //Download base game files
            DownloadManager.UpdateStatusLabel("Downloading game files", Source.Installer);
            await Task.WhenAll(downloadTasks);

            //Prepare decompression tasks
            DownloadManager.UpdateStatusLabel("Preparing game decompression", Source.Installer);
            var decompressionTasks = DecompressionManager.PrepareTasks(downloadTasks);

            //Decompress base game files
            DownloadManager.UpdateStatusLabel("Decompressing game files", Source.Installer);
            await Task.WhenAll(decompressionTasks);

            //if bad files detected, attempt game repair
            if (AppState.BadFilesDetected)
            {
                DownloadManager.UpdateStatusLabel("Reparing game files", Source.Installer);
                await AttemptGameRepair();
            }

            //Install finished
            DownloadManager.SetInstallState(false);

            string branch = Utilities.GetCurrentBranch().branch;

            //Set branch as installed
            Ini.Set(branch, "Is_Installed", true);
            Ini.Set(branch, "Version", Utilities.GetCurrentBranch().version);

            Utilities.SetupAdvancedMenu();
            Utilities.SendNotification($"R5Reloaded ({Utilities.GetCurrentBranch().branch}) has been installed!", BalloonIcon.Info);

            appDispatcher.Invoke(new Action(() =>
            {
                Utilities.ShowDownloadOptlFiles();
            }));
        }

        public static async Task InstallOptionalFiles()
        {
            if (AppState.IsInstalling)
                return;

            if (!AppState.IsOnline)
                return;

            if (Utilities.GetCurrentBranch().is_local_branch)
                return;

            DownloadManager.SetOptionalInstallState(true);

            //Set download limits
            DownloadManager.ConfigureConcurrency();
            DownloadManager.ConfigureDownloadSpeed();

            //Create branch library directory to store downloaded files
            string branchDirectory = Utilities.GetBranchDirectory();

            //Fetch compressed base game file list
            DownloadManager.UpdateStatusLabel("Fetching optional files list", Source.Installer);
            GameFiles optionalGameFiles = await DataFetcher.FetchOptionalGameFiles(true);

            //Prepare download tasks
            DownloadManager.UpdateStatusLabel("Preparing optional download", Source.Installer);
            var optionaldownloadTasks = DownloadManager.InitializeDownloadTasks(optionalGameFiles, branchDirectory);

            //Download base game files
            DownloadManager.UpdateStatusLabel("Downloading optional files", Source.Installer);
            await Task.WhenAll(optionaldownloadTasks);

            //Prepare decompression tasks
            DownloadManager.UpdateStatusLabel("Preparing decompression", Source.Installer);
            var decompressionTasks = DecompressionManager.PrepareTasks(optionaldownloadTasks);

            //Decompress base game files
            DownloadManager.UpdateStatusLabel("Decompressing optional files", Source.Installer);
            await Task.WhenAll(decompressionTasks);

            DownloadManager.SetOptionalInstallState(false);

            Ini.Set(Utilities.GetCurrentBranch().branch, "Download_HD_Textures", true);

            appDispatcher.Invoke(new Action(() =>
            {
                Settings_Control.gameInstalls.UpdateGameItems();
            }));

            Utilities.SendNotification($"R5Reloaded ({Utilities.GetCurrentBranch().branch}) optional files have been installed!", BalloonIcon.Info);
        }

        private static async Task AttemptGameRepair()
        {
            bool isRepaired = false;

            for (int i = 0; i < Constants.Launcher.MAX_REPAIR_ATTEMPTS; i++)
            {
                isRepaired = await GameRepair.Start();
                if (isRepaired) break;
            }

            AppState.BadFilesDetected = !isRepaired;
        }

        public static async void Uninstall()
        {
            if (!Utilities.IsBranchInstalled())
                return;

            if (!Directory.Exists(Utilities.GetBranchDirectory()))
            {
                Ini.Set(Utilities.GetCurrentBranch().branch, "Is_Installed", false);
                Ini.Set(Utilities.GetCurrentBranch().branch, "Download_HD_Textures", false);
                Ini.Set(Utilities.GetCurrentBranch().branch, "Version", "");
                return;
            }

            DownloadManager.SetInstallState(true, "UNINSTALLING");

            string[] files = Directory.GetFiles(Utilities.GetBranchDirectory(), "*", SearchOption.AllDirectories);

            DownloadManager.UpdateStatusLabel("Removing Game Files", Source.Installer);
            AppState.FilesLeft = files.Length;

            appDispatcher.Invoke(() =>
            {
                Progress_Bar.Maximum = files.Length;
                Files_Label.Text = $"{AppState.FilesLeft} files left";
            });

            await Task.Run(() =>
            {
                Parallel.ForEach(files, file =>
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        LogError(Source.Installer, $"Failed to delete file: {file}");
                    }
                    finally
                    {
                        appDispatcher.Invoke(() =>
                        {
                            Progress_Bar.Value++;
                            Files_Label.Text = $"{--AppState.FilesLeft} files left";
                        });
                    }
                });
            });

            Directory.Delete(Utilities.GetBranchDirectory(), true);

            DownloadManager.SetInstallState(false);

            string branch = Utilities.GetCurrentBranch().branch;
            Ini.Set(branch, "Is_Installed", false);
            Ini.Set(branch, "Download_HD_Textures", false);
            Ini.Set(branch, "Version", "");

            Utilities.SendNotification($"R5Reloaded ({Utilities.GetCurrentBranch().branch}) has been uninstalled!", BalloonIcon.Info);
        }
    }
}