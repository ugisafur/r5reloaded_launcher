namespace launcher
{
    /// <summary>
    /// The GameRepair class is responsible for repairing the game installation.
    /// It performs several tasks such as generating checksums, identifying corrupted files,
    /// downloading and decompressing repaired files, and updating the launcher configuration.
    /// </summary>
    public class GameRepair
    {
        public async Task<bool> Start()
        {
            bool repairSuccess = true;

            //Install started
            Utilities.SetInstallState(true, "REPAIRING");

            //Create temp directory to store downloaded files
            string tempDirectory = FileManager.CreateTempDirectory();

            //Prepare checksum tasks
            Utilities.UpdateStatusLabel("Preparing checksum tasks");
            var checksumTasks = FileManager.PrepareChecksumTasks();

            //Generate checksums for local files
            Utilities.UpdateStatusLabel("Generating local checksums");
            await Task.WhenAll(checksumTasks);

            //Fetch non compressed base game file list
            Utilities.UpdateStatusLabel("Fetching base game files list");
            BaseGameFiles baseGameFiles = await DataFetcher.FetchBaseGameFiles(false);

            //Identify bad files
            Utilities.UpdateStatusLabel("Identifying bad files");
            int badFileCount = FileManager.IdentifyBadFiles(baseGameFiles, checksumTasks);

            //if bad files exist, download and repair
            if (badFileCount > 0)
            {
                repairSuccess = false;

                Utilities.UpdateStatusLabel("Preparing download tasks");
                var downloadTasks = DownloadManager.PrepareRepairDownloadTasks(tempDirectory);

                Utilities.UpdateStatusLabel("Downloading repaired files");
                await Task.WhenAll(downloadTasks);

                Utilities.UpdateStatusLabel("Preparing decompression");
                var decompressionTasks = DecompressionManager.PrepareTasks(downloadTasks);

                Utilities.UpdateStatusLabel("Decompressing repaired files");
                await Task.WhenAll(decompressionTasks);
            }

            //Update or create launcher config
            Utilities.UpdateStatusLabel("Updating launcher config");
            FileManager.UpdateOrCreateLauncherConfig();

            //Install finished
            Utilities.SetInstallState(false);

            //Delete temp directory
            await Task.Run(() => FileManager.CleanUpTempDirectory(tempDirectory));

            return repairSuccess;
        }
    }
}