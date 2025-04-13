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

            Download.Tasks.UpdateStatusLabel("Preparing checksum tasks", Source.Repair);
            var checksumTasks = Checksums.PrepareBranchChecksumTasks(branchDirectory);

            Download.Tasks.UpdateStatusLabel("Generating local checksums", Source.Repair);
            await Task.WhenAll(checksumTasks);

            Download.Tasks.UpdateStatusLabel("Fetching base game files list", Source.Repair);
            GameFiles gameFiles = await Fetch.GameFiles(false, false);

            Download.Tasks.UpdateStatusLabel("Identifying bad files", Source.Repair);
            int badFileCount = Checksums.IdentifyBadFiles(gameFiles, checksumTasks, branchDirectory);

            if (badFileCount > 0)
            {
                repairSuccess = false;

                Download.Tasks.UpdateStatusLabel("Preparing download tasks", Source.Repair);
                var downloadTasks = Download.Tasks.InitializeRepairTasks(branchDirectory);

                CancellationTokenSource cts = new CancellationTokenSource();
                Task updateTask = Download.Tasks.UpdateGlobalDownloadProgressAsync(cts.Token);

                Download.Tasks.UpdateStatusLabel("Downloading repaired files", Source.Repair);
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

            if (Directory.GetFiles(branchDirectory, "*.opt.starpak", SearchOption.AllDirectories).Length > 0)
                SetBranch.DownloadHDTextures(true);

            Download.Tasks.SetInstallState(false);

            if (GetBranch.DownloadHDTextures())
                Task.Run(() => RepairOptionalFiles());

            return repairSuccess;
        }

        private static async Task RepairOptionalFiles()
        {
            Download.Tasks.ConfigureConcurrency();
            Download.Tasks.ConfigureDownloadSpeed();

            Download.Tasks.SetOptionalInstallState(true);

            string branchDirectory = GetBranch.Directory();

            Download.Tasks.UpdateStatusLabel("Preparing optional checksum tasks", Source.Repair);
            var checksumTasks = Checksums.PrepareOptChecksumTasks(branchDirectory);

            Download.Tasks.UpdateStatusLabel("Generating optional checksums", Source.Repair);
            await Task.WhenAll(checksumTasks);

            Download.Tasks.UpdateStatusLabel("Fetching optional files list", Source.Repair);
            GameFiles gameFiles = await Fetch.GameFiles(false, true);

            Download.Tasks.UpdateStatusLabel("Identifying bad optional files", Source.Repair);
            int badFileCount = Checksums.IdentifyBadFiles(gameFiles, checksumTasks, branchDirectory);

            if (badFileCount > 0)
            {
                Download.Tasks.UpdateStatusLabel("Preparing optional tasks", Source.Repair);
                var downloadTasks = Download.Tasks.InitializeRepairTasks(branchDirectory);

                CancellationTokenSource cts = new CancellationTokenSource();
                Task updateTask = Download.Tasks.UpdateGlobalDownloadProgressAsync(cts.Token);

                Download.Tasks.UpdateStatusLabel("Downloading optional files", Source.Repair);
                Download.Tasks.ShowSpeedLabels(true, true);
                await Task.WhenAll(downloadTasks);
                Download.Tasks.ShowSpeedLabels(false, false);

                cts.Cancel();
            }

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

            Download.Tasks.UpdateStatusLabel("Preparing language checksum tasks", Source.Repair);
            var checksumTasks = Checksums.PrepareLangChecksumTasks(branchDirectory, langs);

            Download.Tasks.UpdateStatusLabel("Fetching language files", Source.Repair);
            GameFiles langFiles = await Fetch.LanguageFiles(langs, false);

            Download.Tasks.UpdateStatusLabel("Identifying bad language files", Source.Repair);
            int badFileCount = Checksums.IdentifyBadFiles(langFiles, checksumTasks, branchDirectory);

            if (badFileCount > 0)
            {
                Download.Tasks.UpdateStatusLabel("Preparing language tasks", Source.Repair);
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