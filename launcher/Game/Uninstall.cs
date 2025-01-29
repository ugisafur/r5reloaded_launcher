using Hardcodet.Wpf.TaskbarNotification;
using System.IO;
using static launcher.Global.Logger;
using static launcher.Global.References;
using System.Windows.Controls;
using launcher.Global;
using launcher.Managers;
using launcher.BranchUtils;

namespace launcher.Game
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

            Download.Tasks.SetInstallState(true, "UNINSTALLING");

            string[] files = Directory.GetFiles(GetBranch.Directory(), "*", SearchOption.AllDirectories);

            Download.Tasks.UpdateStatusLabel("Removing Game Files", Source.Uninstaller);
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
                    catch (Exception ex)
                    {
                        LogError(Source.Uninstaller, $@"
==============================================================
Failed to delete file: {file}
==============================================================
Message: {ex.Message}

--- Stack Trace ---
{ex.StackTrace}

--- Inner Exception ---
{(ex.InnerException != null ? ex.InnerException.Message : "None")}");
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

            Download.Tasks.SetInstallState(false, "INSTALL");

            Managers.App.SendNotification($"R5Reloaded ({GetBranch.Name()}) has been uninstalled!", BalloonIcon.Info);
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

            Download.Tasks.SetInstallState(true, "UNINSTALLING");

            GameFiles langFiles = await CDN.Fetch.LanguageFiles(lang, false);

            Download.Tasks.UpdateStatusLabel("Removing Game Files", Source.Uninstaller);
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
                    LogInfo(Source.Uninstaller, $"Removing file: {GetBranch.Directory()}\\{langFile.name}");
                    File.Delete($"{GetBranch.Directory()}\\{langFile.name}");
                }
                else
                {
                    LogInfo(Source.Uninstaller, $"File not found: {GetBranch.Directory()}\\{langFile.name}");
                }

                appDispatcher.Invoke(() =>
            {
                Progress_Bar.Value++;
                Files_Label.Text = $"{--AppState.FilesLeft} files left";
            });
            }

            Download.Tasks.SetInstallState(false);

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

            Download.Tasks.SetInstallState(true, "UNINSTALLING");

            string[] opt_files = Directory.GetFiles(GetBranch.Directory(branch), "*.opt.starpak", SearchOption.AllDirectories);

            Download.Tasks.UpdateStatusLabel("Removing HD Textures", Source.Uninstaller);
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
                    catch (Exception ex)
                    {
                        LogError(Source.Uninstaller, $@"
==============================================================
Failed to delete file: {file}
==============================================================
Message: {ex.Message}

--- Stack Trace ---
{ex.StackTrace}

--- Inner Exception ---
{(ex.InnerException != null ? ex.InnerException.Message : "None")}");
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

            Download.Tasks.SetInstallState(false, "PLAY");

            Managers.App.SendNotification($"HD Textures ({GetBranch.Name(true, branch)}) has been uninstalled!", BalloonIcon.Info);
        }
    }
}