using Hardcodet.Wpf.TaskbarNotification;
using System.IO;
using static launcher.Global.Logger;
using static launcher.Global.References;
using launcher.Global;
using System.Windows;

namespace launcher.Game
{
    public static class Uninstall
    {
        public static async void Start()
        {
            if (!Directory.Exists(GetBranch.Directory()))
            {
                SetBranch.Installed(false);
                SetBranch.DownloadHDTextures(false);
                SetBranch.Version("");
                return;
            }

            if (Managers.App.IsR5ApexOpen())
            {
                if (MessageBox.Show("R5Reloaded is currently running. The game must be closed to uninstall.\n\nDo you want to close any open game proccesses now?", "R5Reloaded", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    Managers.App.CloseR5Apex();
                }
                else
                {
                    return;
                }
            }

            if (AnyFilesOpen(GetBranch.Directory()))
                return;

            Download.Tasks.SetInstallState(true, "UNINSTALLING");

            string[] files = Directory.GetFiles(GetBranch.Directory(), "*", SearchOption.AllDirectories);

            Download.Tasks.UpdateStatusLabel("Removing game files", Source.Uninstaller);
            AppState.FilesLeft = files.Length;

            appDispatcher.Invoke(() =>
            {
                Progress_Bar.Maximum = files.Length;
                //Files_Label.Text = $"{AppState.FilesLeft} files left";
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
                        LogException($"Failed to delete file: {file}", Source.Uninstaller, ex);
                    }
                    finally
                    {
                        appDispatcher.Invoke(() =>
                        {
                            Progress_Bar.Value++;
                            //Files_Label.Text = $"{--AppState.FilesLeft} files left";
                        });
                    }
                });
            });

            Directory.Delete(GetBranch.Directory(), true);

            SetBranch.Installed(false);
            SetBranch.DownloadHDTextures(false);
            SetBranch.Version("");

            AppState.SetRichPresence("", "Idle");

            Download.Tasks.SetInstallState(false, "INSTALL");

            Managers.App.SendNotification($"R5Reloaded ({GetBranch.Name()}) has been uninstalled!", BalloonIcon.Info);
        }

        public static async void LangFile(System.Windows.Controls.CheckBox checkbox, List<string> lang)
        {
            if (!GetBranch.Installed() || !Directory.Exists(GetBranch.Directory()))
                return;

            appDispatcher.Invoke(() =>
            {
                if (checkbox != null)
                    checkbox.IsEnabled = false;
            });

            Download.Tasks.SetInstallState(true, "UNINSTALLING");

            GameFiles langFiles = await Fetch.LanguageFiles(lang, false);

            Download.Tasks.UpdateStatusLabel("Removing game files", Source.Uninstaller);
            AppState.FilesLeft = langFiles.files.Count;

            appDispatcher.Invoke(() =>
            {
                Progress_Bar.Maximum = langFiles.files.Count;
                //Files_Label.Text = $"{AppState.FilesLeft} files left";
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
                    //Files_Label.Text = $"{--AppState.FilesLeft} files left";
                });
            }

            Download.Tasks.SetInstallState(false);

            AppState.SetRichPresence("", "Idle");

            appDispatcher.Invoke(() =>
            {
                if (checkbox != null)
                    checkbox.IsEnabled = true;
            });
        }

        public static async void HDTextures(Branch branch)
        {
            if (!GetBranch.Installed(branch) || !Directory.Exists(GetBranch.Directory(branch)))
                return;

            Download.Tasks.SetInstallState(true, "UNINSTALLING");

            string[] opt_files = Directory.GetFiles(GetBranch.Directory(branch), "*.opt.starpak", SearchOption.AllDirectories);

            Download.Tasks.UpdateStatusLabel("Removing hd textures", Source.Uninstaller);
            AppState.FilesLeft = opt_files.Length;

            appDispatcher.Invoke(() =>
            {
                Progress_Bar.Maximum = opt_files.Length;
                //Files_Label.Text = $"{AppState.FilesLeft} files left";
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
                        LogException($"Failed to delete file: {file}", Source.Uninstaller, ex);
                    }
                    finally
                    {
                        appDispatcher.Invoke(() =>
                        {
                            Progress_Bar.Value++;
                            //Files_Label.Text = $"{--AppState.FilesLeft} files left";
                        });
                    }
                });
            });

            SetBranch.DownloadHDTextures(false, branch);

            Download.Tasks.SetInstallState(false, "PLAY");

            Managers.App.SendNotification($"HD Textures ({GetBranch.Name(true, branch)}) has been uninstalled!", BalloonIcon.Info);
        }

        private static bool AnyFilesOpen(string path)
        {
            bool anyFileInUse = false;

            foreach (string file in Directory.GetFiles(path))
            {
                if (IsFileLocked(file))
                {
                    MessageBox.Show($"The file '{Path.GetFileName(file)}' is currently in use. Please close it before uninstalling.",
                                    "File In Use",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                    anyFileInUse = true;
                    break;
                }
            }

            return anyFileInUse;
        }

        private static bool IsFileLocked(string filePath)
        {
            try
            {
                using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    // If we get here, the file is not locked.
                }
            }
            catch (IOException)
            {
                return true;
            }
            return false;
        }
    }
}