using Hardcodet.Wpf.TaskbarNotification;
using System.IO;
using static launcher.Global.References;
using static launcher.Global.Logger;
using launcher.Global;
using launcher.Managers;
using launcher.BranchUtils;
using launcher.CDN;

namespace launcher.Game
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

            Download.Tasks.CreateDownloadMontior();

            SetBranch.UpdateAvailable(false);

            Download.Tasks.ConfigureConcurrency();
            Download.Tasks.ConfigureDownloadSpeed();

            //Install started
            Download.Tasks.SetInstallState(true, "UPDATING");

            //Create branch library directory to store downloaded files
            string branchDirectory = GetBranch.Directory();

            //Check for deleted files
            await CheckForDeletedFiles(false);

            //Prepare checksum tasks
            Download.Tasks.UpdateStatusLabel("Preparing checksum tasks", Source.Update);
            var checksumTasks = Checksums.PrepareBranchChecksumTasks(branchDirectory);

            //Generate checksums for local files
            Download.Tasks.UpdateStatusLabel("Generating local checksums", Source.Update);
            await Task.WhenAll(checksumTasks);

            //Fetch non compressed base game file list
            Download.Tasks.UpdateStatusLabel("Fetching update files list", Source.Update);
            GameFiles gameFiles = await Fetch.GameFiles(false, false);

            //Identify changed files
            Download.Tasks.UpdateStatusLabel("Identifying changed files", Source.Update);
            int changedFileCount = Checksums.IdentifyBadFiles(gameFiles, checksumTasks, branchDirectory);

            //if changed files exist, download and update
            if (changedFileCount > 0)
            {
                Download.Tasks.UpdateStatusLabel("Preparing download tasks", Source.Update);
                var downloadTasks = Download.Tasks.CreateRepairTasks(branchDirectory);

                Download.Tasks.UpdateStatusLabel("Downloading updated files", Source.Update);
                await Task.WhenAll(downloadTasks);
            }

            //Update launcher config
            SetBranch.Installed(true);
            SetBranch.Version(GetBranch.ServerVersion());

            Managers.App.SendNotification($"R5Reloaded ({GetBranch.Name()}) has been updated!", BalloonIcon.Info);

            Managers.App.SetupAdvancedMenu();

            //Install finished
            Download.Tasks.SetInstallState(false);

            if (GetBranch.DownloadHDTextures())
                Task.Run(() => UpdateOptionalWithoutPatching());
        }

        private static async Task UpdateOptionalWithoutPatching()
        {
            Download.Tasks.SetOptionalInstallState(true);

            //Set download limits
            Download.Tasks.ConfigureConcurrency();
            Download.Tasks.ConfigureDownloadSpeed();

            //Create branch library directory to store downloaded files
            string branchDirectory = GetBranch.Directory();

            //Check for deleted files
            await CheckForDeletedFiles(true);

            //Prepare checksum tasks
            Download.Tasks.UpdateStatusLabel("Preparing optional checksum tasks", Source.Repair);
            var checksumTasks = Checksums.PrepareOptChecksumTasks(branchDirectory);

            //Generate checksums for local files
            Download.Tasks.UpdateStatusLabel("Generating optional checksums", Source.Repair);
            await Task.WhenAll(checksumTasks);

            //Fetch non compressed base game file list
            Download.Tasks.UpdateStatusLabel("Fetching optional files list", Source.Repair);
            GameFiles gameFiles = await Fetch.GameFiles(false, true);

            //Identify bad files
            Download.Tasks.UpdateStatusLabel("Identifying changed files", Source.Repair);
            int changedFileCount = Checksums.IdentifyBadFiles(gameFiles, checksumTasks, branchDirectory);

            //if bad files exist, download and repair
            if (changedFileCount > 0)
            {
                Download.Tasks.UpdateStatusLabel("Preparing optional tasks", Source.Repair);
                var downloadTasks = Download.Tasks.CreateRepairTasks(branchDirectory);

                Download.Tasks.UpdateStatusLabel("Downloading optional files", Source.Repair);
                await Task.WhenAll(downloadTasks);
            }

            Download.Tasks.SetOptionalInstallState(false);

            Managers.App.SendNotification($"R5Reloaded ({GetBranch.Name()}) optional files have been updated!", BalloonIcon.Info);
        }

        private static async Task CheckForDeletedFiles(bool optfiles)
        {
            string branchDirectory = GetBranch.Directory();

            // Get all files in the branch directory
            string[] files = Directory.GetFiles(branchDirectory, "*", SearchOption.AllDirectories);

            // Fetch the appropriate game files based on optfiles

            GameFiles gameFiles = await Fetch.GameFiles(false, optfiles);

            foreach (var file in files)
            {
                string relativePath = Path.GetRelativePath(branchDirectory, file);

                // Handle the optfiles logic
                bool isOptFile = relativePath.EndsWith("opt.starpak", StringComparison.OrdinalIgnoreCase);

                if (optfiles && isOptFile || !optfiles && !isOptFile)
                {
                    try
                    {
                        // Check if the file exists in the fetched game files
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
}