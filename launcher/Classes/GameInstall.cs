using System.IO;

namespace launcher
{
    public class GameInstall
    {
        public async void Start()
        {
            //Install started
            Utilities.SetInstallState(true, "INSTALLING");

            //Create temp directory to store downloaded files
            string tempDirectory = FileManager.CreateTempDirectory();

            //Fetch compressed base game file list
            Utilities.UpdateStatusLabel("Fetching base game files list");
            BaseGameFiles baseGameFiles = await DataFetcher.FetchBaseGameFiles(true);

            //Prepare download tasks
            Utilities.UpdateStatusLabel("Preparing game download");
            var downloadTasks = DownloadManager.PrepareDownloadTasks(baseGameFiles, tempDirectory);

            //Download base game files
            Utilities.UpdateStatusLabel("Downloading game files");
            await Task.WhenAll(downloadTasks);

            //Prepare decompression tasks
            Utilities.UpdateStatusLabel("Preparing game decompression");
            var decompressionTasks = DecompressionManager.PrepareTasks(downloadTasks);

            //Decompress base game files
            Utilities.UpdateStatusLabel("Decompressing game files");
            await Task.WhenAll(decompressionTasks);

            //if bad files detected, attempt game repair
            if (Global.badFilesDetected)
            {
                Utilities.UpdateStatusLabel("Reparing game files");
                await AttemptGameRepair();
            }

            //Update or create launcher config
            FileManager.UpdateOrCreateLauncherConfig();

            //Install finished
            Utilities.SetInstallState(false);

            //Set game as installed
            Global.isInstalled = true;

            //Delete temp directory
            if (Directory.Exists(tempDirectory))
                await Task.Run(() => FileManager.CleanUpTempDirectory(tempDirectory));
        }

        private async Task AttemptGameRepair()
        {
            bool isRepaired = false;

            for (int i = 0; i < Global.MAX_REPAIR_ATTEMPTS; i++)
            {
                isRepaired = await ControlReferences.gameRepair.Start();
                if (isRepaired) break;
            }

            Global.badFilesDetected = !isRepaired;
        }
    }
}