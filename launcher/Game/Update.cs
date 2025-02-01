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
            if (AppState.IsInstalling || !AppState.IsOnline || GetBranch.IsLocalBranch() || !GetBranch.UpdateAvailable() || GetBranch.LocalVersion() == GetBranch.ServerVersion())
                return;

            Download.Tasks.CreateDownloadMontior();

            SetBranch.UpdateAvailable(false);

            Download.Tasks.ConfigureConcurrency();
            Download.Tasks.ConfigureDownloadSpeed();

            Download.Tasks.SetInstallState(true, "UPDATING");

            string branchDirectory = GetBranch.Directory();

            await CheckForDeletedFiles(false);

            Download.Tasks.UpdateStatusLabel("Preparing checksum tasks", Source.Update);
            var checksumTasks = Checksums.PrepareBranchChecksumTasks(branchDirectory);

            Download.Tasks.UpdateStatusLabel("Generating local checksums", Source.Update);
            await Task.WhenAll(checksumTasks);

            Download.Tasks.UpdateStatusLabel("Fetching update files list", Source.Update);
            GameFiles gameFiles = await Fetch.GameFiles(false, false);

            Download.Tasks.UpdateStatusLabel("Identifying changed files", Source.Update);
            int changedFileCount = Checksums.IdentifyBadFiles(gameFiles, checksumTasks, branchDirectory);

            if (changedFileCount > 0)
            {
                Download.Tasks.UpdateStatusLabel("Preparing download tasks", Source.Update);
                var downloadTasks = Download.Tasks.InitializeRepairTasks(branchDirectory);

                Download.Tasks.UpdateStatusLabel("Downloading updated files", Source.Update);
                Download.Tasks.ShowSpeedLabels(true, true);
                await Task.WhenAll(downloadTasks);
                Download.Tasks.ShowSpeedLabels(false, false);
            }

            SetBranch.Installed(true);
            SetBranch.Version(GetBranch.ServerVersion());

            Managers.App.SendNotification($"R5Reloaded ({GetBranch.Name()}) has been updated!", BalloonIcon.Info);
            Managers.App.SetupAdvancedMenu();

            Download.Tasks.SetInstallState(false);

            if (GetBranch.DownloadHDTextures())
                Task.Run(() => UpdateOptionalWithoutPatching());
        }

        private static async Task UpdateOptionalWithoutPatching()
        {
            Download.Tasks.SetOptionalInstallState(true);

            Download.Tasks.ConfigureConcurrency();
            Download.Tasks.ConfigureDownloadSpeed();

            string branchDirectory = GetBranch.Directory();

            await CheckForDeletedFiles(true);

            Download.Tasks.UpdateStatusLabel("Preparing optional checksum tasks", Source.Repair);
            var checksumTasks = Checksums.PrepareOptChecksumTasks(branchDirectory);

            Download.Tasks.UpdateStatusLabel("Generating optional checksums", Source.Repair);
            await Task.WhenAll(checksumTasks);

            Download.Tasks.UpdateStatusLabel("Fetching optional files list", Source.Repair);
            GameFiles gameFiles = await Fetch.GameFiles(false, true);

            Download.Tasks.UpdateStatusLabel("Identifying changed files", Source.Repair);
            int changedFileCount = Checksums.IdentifyBadFiles(gameFiles, checksumTasks, branchDirectory);

            if (changedFileCount > 0)
            {
                Download.Tasks.UpdateStatusLabel("Preparing optional tasks", Source.Repair);
                var downloadTasks = Download.Tasks.InitializeRepairTasks(branchDirectory);

                Download.Tasks.UpdateStatusLabel("Downloading optional files", Source.Repair);
                Download.Tasks.ShowSpeedLabels(true, true);
                await Task.WhenAll(downloadTasks);
                Download.Tasks.ShowSpeedLabels(false, false);
            }

            Download.Tasks.SetOptionalInstallState(false);

            Managers.App.SendNotification($"R5Reloaded ({GetBranch.Name()}) optional files have been updated!", BalloonIcon.Info);
        }

        private static async Task CheckForDeletedFiles(bool optfiles)
        {
            string branchDirectory = GetBranch.Directory();

            string[] files = Directory.GetFiles(branchDirectory, "*", SearchOption.AllDirectories);

            GameFiles gameFiles = await Fetch.GameFiles(false, optfiles);

            foreach (var file in files)
            {
                string relativePath = Path.GetRelativePath(branchDirectory, file);

                bool isOptFile = relativePath.EndsWith("opt.starpak", StringComparison.OrdinalIgnoreCase);

                if (optfiles && isOptFile || !optfiles && !isOptFile)
                {
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
                        LogError(Source.Update, $@"
==============================================================
Failed to delete file: {relativePath}
==============================================================
Message: {ex.Message}

--- Stack Trace ---
{ex.StackTrace}

--- Inner Exception ---
{(ex.InnerException != null ? ex.InnerException.Message : "None")}");
                    }
                }
            }
        }
    }
}