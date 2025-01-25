using Hardcodet.Wpf.TaskbarNotification;
using launcher.Classes.BranchUtils;
using System.IO;
using static launcher.Classes.Global.References;
using launcher.Classes.Global;
using launcher.Classes.CDN;
using static launcher.Classes.Utilities.Logger;
using launcher.Classes.Utilities;
using launcher.Classes.Managers;

namespace launcher.Classes.Game
{
    public static class Update
    {
        public static async void Start()
        {
            if (AppState.IsInstalling)
                return;

            if (!AppState.IsOnline)
                return;

            if (GetBranch.IsLocalBranch())
                return;

            if (!GetBranch.UpdateAvailable())
                return;

            if (GetBranch.LocalVersion() == GetBranch.ServerVersion())
                return;

            DownloadManager.CreateDownloadMontior();

            SetBranch.UpdateAvailable(false);

            //Install started
            DownloadManager.SetInstallState(true, "UPDATING");

            //Set download limits
            DownloadManager.ConfigureConcurrency();
            DownloadManager.ConfigureDownloadSpeed();

            //Create branch library directory to store downloaded files
            string branchDirectory = GetBranch.Directory();

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
            GameFiles gameFiles = await Fetch.BranchFiles(false, false);

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
            }

            //Update launcher config
            Ini.Set(GetBranch.Name(false), "Is_Installed", true);
            Ini.Set(GetBranch.Name(false), "Version", GetBranch.ServerVersion());

            AppManager.SendNotification($"R5Reloaded ({GetBranch.Name()}) has been updated!", BalloonIcon.Info);

            AppManager.SetupAdvancedMenu();

            //Install finished
            DownloadManager.SetInstallState(false);

            if (Ini.Get(GetBranch.Name(false), "Download_HD_Textures", false))
                Task.Run(() => UpdateOptionalWithoutPatching());
        }

        private static async Task UpdateOptionalWithoutPatching()
        {
            DownloadManager.SetOptionalInstallState(true);

            //Set download limits
            DownloadManager.ConfigureConcurrency();
            DownloadManager.ConfigureDownloadSpeed();

            //Create branch library directory to store downloaded files
            string branchDirectory = GetBranch.Directory();

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
            GameFiles gameFiles = await Fetch.BranchFiles(false, true);

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
            }

            DownloadManager.SetOptionalInstallState(false);

            AppManager.SendNotification($"R5Reloaded ({GetBranch.Name()}) optional files have been updated!", BalloonIcon.Info);
        }

        private static async Task CheckForDeletedFiles(bool optfiles)
        {
            string branchDirectory = GetBranch.Directory();

            // Get all files in the branch directory
            string[] files = Directory.GetFiles(branchDirectory, "*", SearchOption.AllDirectories);

            // Fetch the appropriate game files based on optfiles

            GameFiles gameFiles = await Fetch.BranchFiles(false, optfiles);

            foreach (var file in files)
            {
                string relativePath = Path.GetRelativePath(branchDirectory, file);

                // Handle the optfiles logic
                bool isOptFile = relativePath.EndsWith("opt.starpak", StringComparison.OrdinalIgnoreCase);

                if ((optfiles && isOptFile) || (!optfiles && !isOptFile))
                {
                    try
                    {
                        // Check if the file exists in the fetched game files
                        if (!gameFiles.files.Exists(f => f.name.Equals(relativePath, StringComparison.OrdinalIgnoreCase)))
                        {
                            if (System.IO.File.Exists(file))
                                System.IO.File.Delete(file);
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
}