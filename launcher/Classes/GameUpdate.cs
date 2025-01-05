using Newtonsoft.Json;
using Octodiff.Core;
using Octodiff.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ZstdSharp;

namespace launcher
{
    public class GameUpdate
    {
        public async void Start()
        {
            // Check if the game is already up to date
            if (Helper.launcherConfig.currentUpdateVersion == Helper.serverConfig.branches[Helper.GetCmbBranchIndex()].currentVersion)
                return;

            // Check if user is to outdated to update normally
            if (Helper.launcherConfig.currentUpdateVersion != Helper.serverConfig.branches[Helper.GetCmbBranchIndex()].lastVersion)
                await Helper.gameRepair.Start();

            // Install started
            Helper.InstallStarted("Updating");

            // Create temp directory to store downloaded files
            string tempDirectory = Helper.CreateTempDirectory();

            // Fetch patch files
            GamePatch patchFiles = await Helper.FetchPatchFiles();

            // Prepare download tasks
            var downloadTasks = Helper.PreparePatchDownloadTasks(patchFiles, tempDirectory);

            // Download patch files
            await Task.WhenAll(downloadTasks);

            // Prepare file patch tasks
            var filePatchTasks = Helper.PrepareFilePatchTasks(patchFiles, tempDirectory);

            // Patch base game files
            await Task.WhenAll(filePatchTasks);

            // Update or create launcher config
            Helper.UpdateOrCreateLauncherConfig();

            // Install finished
            Helper.InstalledFinished();

            // Set update required to false
            Helper.updateRequired = false;

            //Delete temp directory
            await Task.Run(() => Helper.CleanUpTempDirectory(tempDirectory));
        }
    }
}