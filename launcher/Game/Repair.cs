using Hardcodet.Wpf.TaskbarNotification;
using System.IO;
using static launcher.Global.Logger;
using System.Windows;
using static launcher.Global.References;
using launcher.Global;

namespace launcher.Game
{
    public static class Repair
    {
        public static async Task<bool> Start()
        {
            if (AppState.IsInstalling || !AppState.IsOnline || GetBranch.IsLocalBranch())
                return false;

            if (Managers.App.IsR5ApexOpen())
            {
                if (MessageBox.Show("R5Reloaded is currently running. The game must be closed to repair.\n\nDo you want to close any open game proccesses now?", "R5Reloaded", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    Managers.App.CloseR5Apex();
                }
                else
                {
                    return false;
                }
            }

            if (GetBranch.UpdateAvailable())
            {
                Update_Button.Visibility = Visibility.Hidden;
                SetBranch.UpdateAvailable(false);
            }

            bool repairSuccess = true;

            Download.Tasks.CreateDownloadMontior();
            Download.Tasks.ConfigureConcurrency();
            Download.Tasks.ConfigureDownloadSpeed();

            Download.Tasks.SetInstallState(true, "REPAIRING");

            string branchDirectory = GetBranch.Directory();

            Download.Tasks.UpdateStatusLabel("Preparing to repair", Source.Repair);
            var checksumTasks = Checksums.PrepareBranchChecksumTasks(branchDirectory);

            Download.Tasks.UpdateStatusLabel("Checking files", Source.Repair);
            await Task.WhenAll(checksumTasks);

            Download.Tasks.UpdateStatusLabel("Fetching latest files", Source.Repair);
            GameFiles gameFiles = await Fetch.GameFiles(false);

            Download.Tasks.UpdateStatusLabel("Finding bad files", Source.Repair);
            int badFileCount = Checksums.IdentifyBadFiles(gameFiles, checksumTasks, branchDirectory);

            if (badFileCount > 0)
            {
                repairSuccess = false;

                Download.Tasks.UpdateStatusLabel("Preparing downloads", Source.Repair);
                var downloadTasks = Download.Tasks.InitializeRepairTasks(branchDirectory);

                CancellationTokenSource cts = new CancellationTokenSource();
                Task updateTask = Download.Tasks.UpdateGlobalDownloadProgressAsync(cts.Token);

                Download.Tasks.UpdateStatusLabel("Downloading files", Source.Repair);
                Download.Tasks.ShowSpeedLabels(true, true);
                await Task.WhenAll(downloadTasks);
                Download.Tasks.ShowSpeedLabels(false, false);

                cts.Cancel();
            }

            if (GetBranch.Branch().mstr_languages.Contains(Launcher.language_name, StringComparer.OrdinalIgnoreCase) && Launcher.language_name != "english")
                await Task.Run(() => LangFile([Launcher.language_name], true));

            SetBranch.Installed(true);
            SetBranch.Version(GetBranch.ServerVersion());

            string sigCacheFile = Path.Combine(branchDirectory, "cfg\\startup.bin");
            if (File.Exists(sigCacheFile))
                File.Delete(sigCacheFile);

            Managers.App.SetupAdvancedMenu();
            Managers.App.SendNotification($"R5Reloaded ({GetBranch.Name()}) has been repaired!", BalloonIcon.Info);

            var allOptFiles = Directory
                .GetFiles(branchDirectory, "*.opt.starpak", SearchOption.AllDirectories)
                .Where(path => !path.Split(Path.DirectorySeparatorChar).Any(segment => segment.Equals("mods", StringComparison.OrdinalIgnoreCase)))
                .ToArray();

            if (allOptFiles.Length > 0)
            {
                foreach (string file in allOptFiles)
                {
                    LogInfo(Source.Repair, $"Found HD Texture file: {file}");
                }

                SetBranch.DownloadHDTextures(true);
            }

            Download.Tasks.SetInstallState(false);
            AppState.SetRichPresence("", "Idle");

            if (GetBranch.DownloadHDTextures())
                Task.Run(() => RepairOptionalFiles());

            return repairSuccess;
        }

        private static async Task RepairOptionalFiles()
        {
            Download.Tasks.ConfigureConcurrency();
            Download.Tasks.ConfigureDownloadSpeed();

            Download.Tasks.SetOptionalInstallState(true);

            AppState.SetRichPresence($"Repairing {GetBranch.Name()}", $"Getting Ready");

            string branchDirectory = GetBranch.Directory();

            Download.Tasks.UpdateStatusLabel("Preparing to repair", Source.Repair);
            var checksumTasks = Checksums.PrepareOptChecksumTasks(branchDirectory);

            Download.Tasks.UpdateStatusLabel("Checking optional files", Source.Repair);
            await Task.WhenAll(checksumTasks);

            Download.Tasks.UpdateStatusLabel("Fetching optional files", Source.Repair);
            GameFiles gameFiles = await Fetch.GameFiles(true);

            Download.Tasks.UpdateStatusLabel("Finding bad optional files", Source.Repair);
            int badFileCount = Checksums.IdentifyBadFiles(gameFiles, checksumTasks, branchDirectory);

            if (badFileCount > 0)
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

            AppState.SetRichPresence("", "Idle");

            Managers.App.SendNotification($"R5Reloaded ({GetBranch.Name()}) optional files have been repaired!", BalloonIcon.Info);

            Download.Tasks.SetOptionalInstallState(false);
        }

        private static async Task LangFile(List<string> langs, bool bypass_block = false)
        {
            if (!AppState.IsOnline || (AppState.BlockLanguageInstall && !bypass_block))
                return;

            Download.Tasks.ConfigureConcurrency();
            Download.Tasks.ConfigureDownloadSpeed();

            string branchDirectory = GetBranch.Directory();

            Download.Tasks.UpdateStatusLabel("Preparing to repair", Source.Repair);
            var checksumTasks = Checksums.PrepareLangChecksumTasks(branchDirectory, langs);

            Download.Tasks.UpdateStatusLabel("Fetching language files", Source.Repair);
            GameFiles langFiles = await Fetch.LanguageFiles(langs);

            Download.Tasks.UpdateStatusLabel("Finding bad language files", Source.Repair);
            int badFileCount = Checksums.IdentifyBadFiles(langFiles, checksumTasks, branchDirectory);

            if (badFileCount > 0)
            {
                Download.Tasks.UpdateStatusLabel("Preparing language downloads", Source.Repair);
                var downloadTasks = Download.Tasks.InitializeRepairTasks(branchDirectory);

                CancellationTokenSource cts = new CancellationTokenSource();
                Task updateTask = Download.Tasks.UpdateGlobalDownloadProgressAsync(cts.Token);

                Download.Tasks.UpdateStatusLabel("Downloading language files", Source.Repair);
                Download.Tasks.ShowSpeedLabels(false, true);
                await Task.WhenAll(downloadTasks);
                Download.Tasks.ShowSpeedLabels(false, false);

                cts.Cancel();
            }
        }
    }
}