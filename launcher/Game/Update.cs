using Hardcodet.Wpf.TaskbarNotification;
using System.IO;
using static launcher.Global.Logger;
using launcher.Global;
using System.Text.RegularExpressions;
using System.Windows;

namespace launcher.Game
{
    public static class Update
    {
        public static async void Start()
        {
            if (AppState.IsInstalling || !AppState.IsOnline || GetBranch.IsLocalBranch() || !GetBranch.UpdateAvailable() || GetBranch.LocalVersion() == GetBranch.ServerVersion())
                return;

            if (Managers.App.IsR5ApexOpen())
            {
                if (MessageBox.Show("R5Reloaded is currently running. The game must be closed to update.\n\nDo you want to close any open game proccesses now?", "R5Reloaded", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    Managers.App.CloseR5Apex();
                }
                else
                {
                    return;
                }
            }

            Download.Tasks.CreateDownloadMontior();

            SetBranch.UpdateAvailable(false);

            Download.Tasks.ConfigureConcurrency();
            Download.Tasks.ConfigureDownloadSpeed();

            Download.Tasks.SetInstallState(true, "UPDATING");

            AppState.SetRichPresence($"Downloading {GetBranch.Name()}", $"Getting Ready...");

            string branchDirectory = GetBranch.Directory();

            await CheckForDeletedFiles(false);

            Download.Tasks.UpdateStatusLabel("Preparing update", Source.Update);
            var checksumTasks = Checksums.PrepareBranchChecksumTasks(branchDirectory);

            Download.Tasks.UpdateStatusLabel("Checking local files", Source.Update);
            await Task.WhenAll(checksumTasks);

            Download.Tasks.UpdateStatusLabel("Fetching latest files", Source.Update);
            GameFiles gameFiles = await Fetch.GameFiles(false, false);

            Download.Tasks.UpdateStatusLabel("Checking for updated files", Source.Update);
            int changedFileCount = Checksums.IdentifyBadFiles(gameFiles, checksumTasks, branchDirectory, true);

            if (changedFileCount > 0)
            {
                Download.Tasks.UpdateStatusLabel("Preparing downloads", Source.Update);
                var downloadTasks = Download.Tasks.InitializeRepairTasks(branchDirectory);

                CancellationTokenSource cts = new CancellationTokenSource();
                Task updateTask = Download.Tasks.UpdateGlobalDownloadProgressAsync(cts.Token);

                Download.Tasks.UpdateStatusLabel("Downloading updated files", Source.Update);
                Download.Tasks.ShowSpeedLabels(true, true);
                await Task.WhenAll(downloadTasks);
                Download.Tasks.ShowSpeedLabels(false, false);

                cts.Cancel();
            }

            SetBranch.Installed(true);
            SetBranch.Version(GetBranch.ServerVersion());

            string sigCacheFile = Path.Combine(branchDirectory, "cfg\\startup.bin");
            if (File.Exists(sigCacheFile))
                File.Delete(sigCacheFile);

            Managers.App.SendNotification($"R5Reloaded ({GetBranch.Name()}) has been updated!", BalloonIcon.Info);
            Managers.App.SetupAdvancedMenu();

            Download.Tasks.SetInstallState(false);

            AppState.SetRichPresence("", "Idle");

            if (GetBranch.DownloadHDTextures())
                Task.Run(() => UpdateOptionalWithoutPatching());
        }

        private static async Task UpdateOptionalWithoutPatching()
        {
            Download.Tasks.SetOptionalInstallState(true);

            Download.Tasks.ConfigureConcurrency();
            Download.Tasks.ConfigureDownloadSpeed();

            string branchDirectory = GetBranch.Directory();

            AppState.SetRichPresence($"Downloading {GetBranch.Name()}", $"Getting Ready...");

            await CheckForDeletedFiles(true);

            Download.Tasks.UpdateStatusLabel("Preparing update", Source.Repair);
            var checksumTasks = Checksums.PrepareOptChecksumTasks(branchDirectory);

            Download.Tasks.UpdateStatusLabel("Checking optional files", Source.Repair);
            await Task.WhenAll(checksumTasks);

            Download.Tasks.UpdateStatusLabel("Fetching optional files", Source.Repair);
            GameFiles gameFiles = await Fetch.GameFiles(false, true);

            Download.Tasks.UpdateStatusLabel("Checking for updated files", Source.Repair);
            int changedFileCount = Checksums.IdentifyBadFiles(gameFiles, checksumTasks, branchDirectory, true);

            if (changedFileCount > 0)
            {
                Download.Tasks.UpdateStatusLabel("Preparing optional downloads", Source.Repair);
                var downloadTasks = Download.Tasks.InitializeRepairTasks(branchDirectory);

                CancellationTokenSource cts = new CancellationTokenSource();
                Task updateTask = Download.Tasks.UpdateGlobalDownloadProgressAsync(cts.Token);

                Download.Tasks.UpdateStatusLabel("Downloading optional files", Source.Repair);
                Download.Tasks.ShowSpeedLabels(true, true);
                await Task.WhenAll(downloadTasks);
                Download.Tasks.ShowSpeedLabels(false, false);

                cts.Cancel();
            }

            Download.Tasks.SetOptionalInstallState(false);

            Managers.App.SendNotification($"R5Reloaded ({GetBranch.Name()}) optional files have been updated!", BalloonIcon.Info);

            AppState.SetRichPresence("", "Idle");
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
                            string languagesPattern = string.Join("|", GetBranch.Branch().mstr_languages.Select(Regex.Escape));
                            Regex excludeLangRegex = new Regex($"general_({languagesPattern})(?:_|\\.)", RegexOptions.IgnoreCase);

                            string fileName = Path.GetFileName(file);

                            if (!excludeLangRegex.IsMatch(fileName) && File.Exists(file))
                                File.Delete(file);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogException($"Failed to delete file: {relativePath}", Source.Update, ex);
                    }
                }
            }
        }
    }
}