using Hardcodet.Wpf.TaskbarNotification;
using launcher.Classes.BranchUtils;
using launcher.Classes.Global;
using System.IO;
using static launcher.Classes.Utilities.Logger;
using static launcher.Classes.Global.References;
using launcher.Classes.Utilities;
using launcher.Classes.Managers;
using System.Windows.Controls;

namespace launcher.Classes.Game
{
    public static class Uninstall
    {
        public static async void Start()
        {
            if (!GetBranch.Installed() && !Directory.Exists(GetBranch.Directory()))
                return;

            if (!Directory.Exists(GetBranch.Directory()))
            {
                SetBranch.Installed(false);
                SetBranch.DownloadHDTextures(false);
                SetBranch.Version("");
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

            SetBranch.Installed(false);
            SetBranch.DownloadHDTextures(false);
            SetBranch.Version("");

            DownloadManager.SetInstallState(false, "INSTALL");

            AppManager.SendNotification($"R5Reloaded ({GetBranch.Name()}) has been uninstalled!", BalloonIcon.Info);
        }

        public static async void LangFile(CheckBox checkbox, List<string> lang)
        {
            if (!GetBranch.Installed())
                return;

            if (!Directory.Exists(GetBranch.Directory()))
                return;

            appDispatcher.Invoke(() =>
            {
                checkbox.IsEnabled = false;
            });

            DownloadManager.SetInstallState(true, "UNINSTALLING");

            GameFiles langFiles = await CDN.Fetch.LanguageFiles(lang, false);

            DownloadManager.UpdateStatusLabel("Removing Game Files", Source.Installer);
            AppState.FilesLeft = langFiles.files.Count;

            appDispatcher.Invoke(() =>
            {
                Progress_Bar.Maximum = langFiles.files.Count;
                Files_Label.Text = $"{AppState.FilesLeft} files left";
            });

            foreach (var langFile in langFiles.files)
            {
                if (File.Exists($"{GetBranch.Directory()}\\{langFile.name}"))
                {
                    LogInfo(Source.Installer, $"Removing file: {GetBranch.Directory()}\\{langFile.name}");
                    File.Delete($"{GetBranch.Directory()}\\{langFile.name}");
                }
                else
                {
                    LogInfo(Source.Installer, $"File not found: {GetBranch.Directory()}\\{langFile.name}");
                }

                appDispatcher.Invoke(() =>
            {
                Progress_Bar.Value++;
                Files_Label.Text = $"{--AppState.FilesLeft} files left";
            });
            }

            DownloadManager.SetInstallState(false);

            appDispatcher.Invoke(() =>
            {
                checkbox.IsEnabled = true;
            });
        }

        public static async void HDTextures(Branch branch)
        {
            if (!GetBranch.Installed(branch) && !Directory.Exists(GetBranch.Directory(branch)))
                return;

            if (!Directory.Exists(GetBranch.Directory(branch)))
                return;

            DownloadManager.SetInstallState(true, "UNINSTALLING");

            string[] opt_files = Directory.GetFiles(GetBranch.Directory(branch), "*.opt.starpak", SearchOption.AllDirectories);

            DownloadManager.UpdateStatusLabel("Removing HD Textures", Source.Installer);
            AppState.FilesLeft = opt_files.Length;

            appDispatcher.Invoke(() =>
            {
                Progress_Bar.Maximum = opt_files.Length;
                Files_Label.Text = $"{AppState.FilesLeft} files left";
            });

            await Task.Run(() =>
            {
                Parallel.ForEach(opt_files, file =>
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

            SetBranch.DownloadHDTextures(false, branch);

            DownloadManager.SetInstallState(false, "PLAY");

            AppManager.SendNotification($"HD Textures ({GetBranch.Name(true, branch)}) has been uninstalled!", BalloonIcon.Info);
        }
    }
}