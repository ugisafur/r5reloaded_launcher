using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ZstdSharp;

namespace launcher
{
    public class GameInstall
    {
        public async void Start()
        {
            //Install started
            Helper.InstallStarted("Installing");

            //Create temp directory to store downloaded files
            string tempDirectory = Helper.CreateTempDirectory();

            //Fetch compressed base game file list
            Helper.UpdateStatusLabel("Fetching base game files list");
            BaseGameFiles baseGameFiles = await Helper.FetchBaseGameFiles(true);

            //Prepare download tasks
            Helper.UpdateStatusLabel("Preparing game download");
            var downloadTasks = Helper.PrepareDownloadTasks(baseGameFiles, tempDirectory);

            //Download base game files
            Helper.UpdateStatusLabel("Downloading game files");
            await Task.WhenAll(downloadTasks);

            //Prepare decompression tasks
            Helper.UpdateStatusLabel("Preparing game decompression");
            var decompressionTasks = Helper.PrepareDecompressionTasks(downloadTasks);

            //Decompress base game files
            Helper.UpdateStatusLabel("Decompressing game files");
            await Task.WhenAll(decompressionTasks);

            //if bad files detected, attempt game repair
            if (Helper.badFilesDetected)
            {
                Helper.UpdateStatusLabel("Reparing game files");
                await AttemptGameRepair();
            }

            //Update or create launcher config
            Helper.UpdateOrCreateLauncherConfig();

            //Install finished
            Helper.InstalledFinished();

            //Delete temp directory
            if (Directory.Exists(tempDirectory))
                await Task.Run(() => Helper.CleanUpTempDirectory(tempDirectory));
        }

        private async Task AttemptGameRepair()
        {
            bool isRepaired = false;

            for (int i = 0; i < Helper.MAX_REPAIR_ATTEMPTS; i++)
            {
                isRepaired = await Helper.gameRepair.Start();
                if (isRepaired) break;
            }

            Helper.badFilesDetected = !isRepaired;
        }
    }
}