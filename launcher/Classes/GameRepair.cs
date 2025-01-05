namespace launcher
{
    public class GameRepair
    {
        public async Task<bool> Start()
        {
            bool repairSuccess = true;

            //Install started
            Helper.InstallStarted("Repairing");

            //Create temp directory to store downloaded files
            string tempDirectory = Helper.CreateTempDirectory();

            //Prepare checksum tasks
            Helper.UpdateStatusLabel("Preparing checksum tasks");
            var checksumTasks = Helper.PrepareChecksumTasks();

            //Generate checksums for local files
            Helper.UpdateStatusLabel("Generating local checksums");
            await Task.WhenAll(checksumTasks);

            //Fetch non compressed base game file list
            Helper.UpdateStatusLabel("Fetching base game files list");
            BaseGameFiles baseGameFiles = await Helper.FetchBaseGameFiles(false);

            //Identify bad files
            Helper.UpdateStatusLabel("Identifying bad files");
            int badFileCount = Helper.IdentifyBadFiles(baseGameFiles, checksumTasks);

            //if bad files exist, download and repair
            if (badFileCount > 0)
            {
                repairSuccess = false;

                Helper.UpdateStatusLabel("Preparing download tasks");
                var downloadTasks = Helper.PrepareRepairDownloadTasks(tempDirectory);

                Helper.UpdateStatusLabel("Downloading repaired files");
                await Task.WhenAll(downloadTasks);

                Helper.UpdateStatusLabel("Preparing decompression");
                var decompressionTasks = Helper.PrepareDecompressionTasks(downloadTasks);

                Helper.UpdateStatusLabel("Decompressing repaired files");
                await Task.WhenAll(decompressionTasks);
            }

            //Update or create launcher config
            Helper.UpdateStatusLabel("Updating launcher config");
            Helper.UpdateOrCreateLauncherConfig();

            //Install finished
            Helper.InstalledFinished();

            //Delete temp directory
            await Task.Run(() => Helper.CleanUpTempDirectory(tempDirectory));

            return repairSuccess;
        }
    }
}