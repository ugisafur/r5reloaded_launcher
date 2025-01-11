using System.IO;

namespace launcher
{
    /// <summary>
    /// The GameRepair class is responsible for repairing the game installation.
    /// It performs several tasks such as generating checksums, identifying corrupted files,
    /// downloading and decompressing repaired files, and updating the launcher configuration.
    /// </summary>
    public class GameRepair
    {
        public static async Task<bool> Start()
        {
            if (!Global.isOnline)
                return false;

            bool repairSuccess = true;

            //Install started
            Utilities.SetInstallState(true, "REPAIRING");

            //Set download limits
            DownloadManager.SetSemaphoreLimit();
            DownloadManager.SetDownloadSpeedLimit();

            //Create temp directory to store downloaded files
            string tempDirectory = FileManager.CreateTempDirectory();

            //Prepare checksum tasks
            Utilities.UpdateStatusLabel("Preparing checksum tasks", Logger.Source.Repair);
            var checksumTasks = FileManager.PrepareBaseGameChecksumTasks();

            //Generate checksums for local files
            Utilities.UpdateStatusLabel("Generating local checksums", Logger.Source.Repair);
            await Task.WhenAll(checksumTasks);

            //Fetch non compressed base game file list
            Utilities.UpdateStatusLabel("Fetching base game files list", Logger.Source.Repair);
            BaseGameFiles baseGameFiles = await DataFetcher.FetchBaseGameFiles(false);

            //Identify bad files
            Utilities.UpdateStatusLabel("Identifying bad files", Logger.Source.Repair);
            int badFileCount = FileManager.IdentifyBadFiles(baseGameFiles, checksumTasks);

            //if bad files exist, download and repair
            if (badFileCount > 0)
            {
                repairSuccess = false;

                Utilities.UpdateStatusLabel("Preparing download tasks", Logger.Source.Repair);
                var downloadTasks = DownloadManager.PrepareRepairDownloadTasks(tempDirectory);

                Utilities.UpdateStatusLabel("Downloading repaired files", Logger.Source.Repair);
                await Task.WhenAll(downloadTasks);

                Utilities.UpdateStatusLabel("Preparing decompression", Logger.Source.Repair);
                var decompressionTasks = DecompressionManager.PrepareTasks(downloadTasks);

                Utilities.UpdateStatusLabel("Decompressing repaired files", Logger.Source.Repair);
                await Task.WhenAll(decompressionTasks);
            }

            //Update launcher config
            Utilities.SetIniSetting(Utilities.IniSettings.Installed, true);
            Utilities.SetIniSetting(Utilities.IniSettings.Current_Version, Global.serverConfig.branches[Utilities.GetCmbBranchIndex()].currentVersion);
            Utilities.SetIniSetting(Utilities.IniSettings.Current_Branch, Global.serverConfig.branches[Utilities.GetCmbBranchIndex()].branch);

            //Install finished
            Utilities.SetInstallState(false);

            //Delete temp directory
            await Task.Run(() => FileManager.CleanUpTempDirectory(tempDirectory));

            if (Utilities.GetIniSetting(Utilities.IniSettings.Download_HD_Textures, false) && Utilities.GetIniSetting(Utilities.IniSettings.HD_Textures_Installed, false))
                Task.Run(() => RepairOptionalFiles());

            return repairSuccess;
        }

        private static async Task RepairOptionalFiles()
        {
            //Set download limits
            DownloadManager.SetSemaphoreLimit();
            DownloadManager.SetDownloadSpeedLimit();

            //Create temp directory to store downloaded files
            string tempDirectory = FileManager.CreateTempDirectory();

            //Prepare checksum tasks
            Utilities.UpdateStatusLabel("Preparing checksum tasks", Logger.Source.Repair);
            var checksumTasks = FileManager.PrepareOptionalGameChecksumTasks();

            //Generate checksums for local files
            Utilities.UpdateStatusLabel("Generating local checksums", Logger.Source.Repair);
            await Task.WhenAll(checksumTasks);

            //Fetch non compressed base game file list
            Utilities.UpdateStatusLabel("Fetching base game files list", Logger.Source.Repair);
            BaseGameFiles baseGameFiles = await DataFetcher.FetchOptionalGameFiles(false);

            //Identify bad files
            Utilities.UpdateStatusLabel("Identifying bad files", Logger.Source.Repair);
            int badFileCount = FileManager.IdentifyBadFiles(baseGameFiles, checksumTasks);

            //if bad files exist, download and repair
            if (badFileCount > 0)
            {
                Utilities.UpdateStatusLabel("Preparing download tasks", Logger.Source.Repair);
                var downloadTasks = DownloadManager.PrepareRepairDownloadTasks(tempDirectory);

                Utilities.UpdateStatusLabel("Downloading repaired files", Logger.Source.Repair);
                await Task.WhenAll(downloadTasks);

                Utilities.UpdateStatusLabel("Preparing decompression", Logger.Source.Repair);
                var decompressionTasks = DecompressionManager.PrepareTasks(downloadTasks);

                Utilities.UpdateStatusLabel("Decompressing repaired files", Logger.Source.Repair);
                await Task.WhenAll(decompressionTasks);
            }

            //Set HD textures as installed
            Utilities.SetIniSetting(Utilities.IniSettings.HD_Textures_Installed, true);

            //Delete temp directory
            await Task.Run(() => FileManager.CleanUpTempDirectory(tempDirectory));
        }
    }
}