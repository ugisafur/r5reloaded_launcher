using Hardcodet.Wpf.TaskbarNotification;
using launcher.Classes.BranchUtils;
using launcher.Classes.Global;
using System.IO;
using static launcher.Classes.Utilities.Logger;
using static launcher.Classes.Global.References;
using launcher.Classes.Utilities;
using launcher.Classes.Managers;

namespace launcher.Classes.Game
{
    public static class Uninstall
    {
        public static async void Start()
        {
            if (!GetBranch.Installed())
                return;

            if (!Directory.Exists(GetBranch.Directory()))
            {
                Ini.Set(GetBranch.Name(false), "Is_Installed", false);
                Ini.Set(GetBranch.Name(false), "Download_HD_Textures", false);
                Ini.Set(GetBranch.Name(false), "Version", "");
                return;
            }

            DownloadManager.SetInstallState(true, "UNINSTALLING");

            string[] files = Directory.GetFiles(GetBranch.Directory(), "*", SearchOption.AllDirectories);

            DownloadManager.UpdateStatusLabel("Removing Game Files", Source.Installer);
            AppState.FilesLeft = files.Length;

            appDispatcher.Invoke(() =>
            {
                Progress_Bar.Maximum = files.Length;
                Files_Label.Text = $"{AppState.FilesLeft} files left";
            });

            await Task.Run(() =>
            {
                Parallel.ForEach(files, file =>
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        LogError(Source.Installer, $"Failed to delete file: {file}");
                    }
                    finally
                    {
                        appDispatcher.Invoke(() =>
                        {
                            Progress_Bar.Value++;
                            Files_Label.Text = $"{--AppState.FilesLeft} files left";
                        });
                    }
                });
            });

            Directory.Delete(GetBranch.Directory(), true);

            Ini.Set(GetBranch.Name(false), "Is_Installed", false);
            Ini.Set(GetBranch.Name(false), "Download_HD_Textures", false);
            Ini.Set(GetBranch.Name(false), "Version", "");

            DownloadManager.SetInstallState(false);

            AppManager.SendNotification($"R5Reloaded ({GetBranch.Name()}) has been uninstalled!", BalloonIcon.Info);
        }
    }
}