using SoftCircuits.IniFileParser;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using static launcher.Logger;
using static launcher.Global;
using static launcher.ControlReferences;
using static launcher.LaunchParameters;

namespace launcher
{
    /// <summary>
    /// The Utilities class provides various utility methods for setting up the application,
    /// managing the game launch process, handling UI updates, and performing version checks.
    /// It includes methods for initializing the main window components, launching the game,
    /// checking for new versions, updating UI elements, and controlling the visibility of
    /// progress bars and settings controls.
    /// </summary>
    public static class Utilities
    {
        public static void SetupApp(MainWindow mainWindow)
        {
#if DEBUG
            EnableDebugConsole();
#endif

            Log(Logger.Type.Info, Source.Launcher, "Setting up launcher");

            IS_ONLINE = DataFetcher.HasInternetConnection();

            if (IS_ONLINE)
                Log(Logger.Type.Info, Source.Launcher, "Internet connection detected");
            else
                Log(Logger.Type.Warning, Source.Launcher, "No internet connection detected");

            mainApp = mainWindow;
            appDispatcher = mainWindow.Dispatcher;
            progressBar = mainWindow.progressBar;
            lblStatus = mainWindow.lblStatus;
            lblFilesLeft = mainWindow.lblFilesLeft;
            launcherVersionlbl = mainWindow.launcherVersionlbl;
            cmbBranch = mainWindow.cmbBranch;
            btnPlay = mainWindow.btnPlay;
            settingsControl = mainWindow.SettingsControl;
            advancedControl = mainWindow.AdvancedControl;
            subMenuControl = mainWindow.subMenuControl;
            TransitionRect = mainWindow.TransitionRect;
            SubMenuPopup = mainWindow.SubMenuPopup;
            gameSettingsPopup = mainWindow.SettingsPopup;
            downloadsPopupControl = mainWindow.DownloadsPopupControl;
            statusPopup = mainWindow.StatusPopupControl;

            ShowProgressBar(false);

            if (IS_ONLINE)
                Task.Run(() => statusPopup.StartStatusTimer());
            else
            {
                mainApp.StatusBtn.IsEnabled = false;
                mainApp.DownloadsBtn.IsEnabled = false;
            }

            launcherVersionlbl.Text = LAUNCHER_VERSION;
            Log(Logger.Type.Info, Source.Launcher, $"Launcher Version: {LAUNCHER_VERSION}");

            LAUNCHER_PATH = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
            Log(Logger.Type.Info, Source.Launcher, $"Launcher path: {LAUNCHER_PATH}");

            settingsControl.SetupSettingsMenu();
            Log(Logger.Type.Info, Source.Launcher, $"Settings menu initialized");

            advancedControl.SetupAdvancedSettings();
            Log(Logger.Type.Info, Source.Launcher, $"Advanced settings initialized");

            if (IS_ONLINE)
                SERVER_CONFIG = DataFetcher.FetchServerConfig();

            LAUNCHER_CONFIG = FileManager.GetLauncherConfig();
            Log(Logger.Type.Info, Source.Launcher, $"Launcher config found");

            IS_INSTALLED = Ini.Get(Ini.Vars.Installed, false);
            Log(Logger.Type.Info, Source.Launcher, $"Is game installed: {IS_INSTALLED}");

            cmbBranch.ItemsSource = SetupGameBranches();
            cmbBranch.SelectedIndex = 0;
            Log(Logger.Type.Info, Source.Launcher, "Game branches initialized");
        }

        public static void ToggleBackgroundVideo(bool disabled)
        {
            Log(Logger.Type.Info, Source.Launcher, $"Toggling background video: {disabled}");
            mainApp.mediaElement.Visibility = disabled ? Visibility.Hidden : Visibility.Visible;
            mainApp.mediaImage.Visibility = disabled ? Visibility.Visible : Visibility.Hidden;
        }

        public static List<ComboBranch> SetupGameBranches()
        {
            if (IS_ONLINE)
            {
                return SERVER_CONFIG.branches
                .Select(branch => new ComboBranch
                {
                    title = branch.branch,
                    subtext = branch.enabled ? branch.currentVersion : "branch disabled"
                })
                .ToList();
            }
            else
            {
                List<Branch> branches = [
                    new Branch() {
                        branch = "No Internet Conenction",
                        enabled = true,
                        currentVersion = "Branch selection disabled"
                    }
                ];

                return branches
                .Select(branch => new ComboBranch
                {
                    title = branch.branch,
                    subtext = branch.currentVersion
                })
                .ToList();
            }
        }

        public static void LaunchGame()
        {
            string gameArguments = BuildParameter();

            eMode mode = (eMode)Ini.Get(Ini.Vars.Mode, 0);

            string exeName = mode switch
            {
                eMode.HOST => "r5apex.exe",
                eMode.SERVER => "r5apex_ds.exe",
                eMode.CLIENT => "r5apex.exe",
                _ => "r5apex.exe"
            };

            var startInfo = new ProcessStartInfo
            {
                FileName = $"{LAUNCHER_PATH}\\{exeName}",
                Arguments = gameArguments,
                UseShellExecute = true,
                CreateNoWindow = true
            };

            Process gameProcess = Process.Start(startInfo);

            if (gameProcess != null)
                SetProcessorAffinity(gameProcess);

            Log(Logger.Type.Info, Source.Launcher, $"Launched game with arguments: {gameArguments}");
        }

        private static void SetProcessorAffinity(Process gameProcess)
        {
            try
            {
                int coreCount = int.Parse(Ini.Get(Ini.Vars.Processor_Affinity, "-1"));
                int processorCount = Environment.ProcessorCount;

                if (coreCount == -1 || coreCount == 0)
                    return;

                if (coreCount > processorCount)
                    coreCount = processorCount;

                if (coreCount >= 1 && coreCount <= processorCount)
                {
                    // Set processor affinity to the first 'coreCount' cores
                    int affinityMask = 0;

                    // Set bits for the first 'coreCount' cores
                    for (int i = 0; i < coreCount; i++)
                        affinityMask |= (1 << i);  // Set the bit corresponding to core 'i'

                    gameProcess.ProcessorAffinity = (IntPtr)affinityMask;

                    Log(Logger.Type.Info, Source.Launcher, $"Processor affinity set to the first {coreCount} cores.");
                }
                else
                    Log(Logger.Type.Error, Source.Launcher, $"Invalid core index: {coreCount}. Must be between -1 and {processorCount}.");
            }
            catch (Exception ex)
            {
                Log(Logger.Type.Error, Source.Launcher, $"Failed to set processor affinity: {ex.Message}");
            }
        }

        public static void SetInstallState(bool installing, string buttonText = "PLAY")
        {
            Log(Logger.Type.Info, Source.Launcher, $"Setting install state to: {installing}");

            appDispatcher.Invoke(() =>
            {
                IS_INSTALLING = installing;

                btnPlay.Content = buttonText;
                cmbBranch.IsEnabled = !installing;
                btnPlay.IsEnabled = !installing;
                lblStatus.Text = "";
                lblFilesLeft.Text = "";
            });

            ShowProgressBar(installing);
        }

        public static void UpdateStatusLabel(string statusText, Source source)
        {
            Log(Logger.Type.Info, source, $"Updating status label: {statusText}");
            appDispatcher.Invoke(() =>
            {
                lblStatus.Text = statusText;
            });
        }

        private static void ShowProgressBar(bool isVisible)
        {
            appDispatcher.Invoke(() =>
            {
                progressBar.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
                lblStatus.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
                lblFilesLeft.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
            });
        }

        public static int GetCmbBranchIndex()
        {
            int cmbSelectedIndex = 0;

            //TODO: FOR BRANCHES LATER, ONLY ALLOWING MAIN BRANCH FOR NOW
            //mainWindow.Dispatcher.Invoke(() =>
            //{
            //    cmbSelectedIndex = mainWindow.cmbBranch.SelectedIndex;
            //});

            return cmbSelectedIndex;
        }

        public static void ShowSettingsControl()
        {
            IN_SETTINGS_MENU = true;

            if (Ini.Get(Ini.Vars.Disable_Transitions, false))
            {
                settingsControl.Visibility = Visibility.Visible;
                subMenuControl.Settings.IsEnabled = false;
                mainApp.DownloadsPopupControl.gotoDownloads.IsEnabled = false;
                return;
            }

            var transitionInStoryboard = CreateTransitionStoryboard(-2400, 0, 0.25);
            transitionInStoryboard.Completed += (s, e) =>
            {
                settingsControl.Visibility = Visibility.Visible;
                var fadeInStoryboard = CreateFadeStoryboard(0, 1, 0.2);
                fadeInStoryboard.Completed += (s, e) =>
                {
                    var transitionOutStoryboard = CreateTransitionStoryboard(0, 2400, 0.25);
                    transitionOutStoryboard.Begin();
                };
                fadeInStoryboard.Begin();
            };
            transitionInStoryboard.Begin();
            subMenuControl.Settings.IsEnabled = false;
            mainApp.DownloadsPopupControl.gotoDownloads.IsEnabled = false;
        }

        public static void HideSettingsControl()
        {
            IN_SETTINGS_MENU = false;

            if (Ini.Get(Ini.Vars.Disable_Transitions, false))
            {
                settingsControl.Visibility = Visibility.Hidden;
                subMenuControl.Settings.IsEnabled = true;
                mainApp.DownloadsPopupControl.gotoDownloads.IsEnabled = true;
                return;
            }

            var transitionInStoryboard = CreateTransitionStoryboard(2400, 0, 0.25);
            transitionInStoryboard.Completed += (s, e) =>
            {
                var fadeOutStoryboard = CreateFadeStoryboard(1, 0, 0.2);
                fadeOutStoryboard.Completed += (s, e) =>
                {
                    settingsControl.Visibility = Visibility.Hidden;
                    var transitionOutStoryboard = CreateTransitionStoryboard(0, -2400, 0.25);
                    transitionOutStoryboard.Begin();
                };
                fadeOutStoryboard.Begin();
            };
            transitionInStoryboard.Begin();
            subMenuControl.Settings.IsEnabled = true;
            mainApp.DownloadsPopupControl.gotoDownloads.IsEnabled = true;
        }

        public static void ShowAdvancedControl()
        {
            IN_ADVANCED_MENU = true;

            if (Ini.Get(Ini.Vars.Disable_Transitions, false))
            {
                advancedControl.Visibility = Visibility.Visible;
                subMenuControl.Settings.IsEnabled = false;
                mainApp.DownloadsPopupControl.gotoDownloads.IsEnabled = false;
                return;
            }

            var transitionInStoryboard = CreateTransitionStoryboard(-2400, 0, 0.25);
            transitionInStoryboard.Completed += (s, e) =>
            {
                advancedControl.Visibility = Visibility.Visible;
                var fadeInStoryboard = CreateFadeStoryboard(0, 1, 0.2);
                fadeInStoryboard.Completed += (s, e) =>
                {
                    var transitionOutStoryboard = CreateTransitionStoryboard(0, 2400, 0.25);
                    transitionOutStoryboard.Begin();
                };
                fadeInStoryboard.Begin();
            };
            transitionInStoryboard.Begin();
            subMenuControl.Settings.IsEnabled = false;
            mainApp.DownloadsPopupControl.gotoDownloads.IsEnabled = false;
        }

        public static void HideAdvancedControl()
        {
            IN_ADVANCED_MENU = false;

            if (Ini.Get(Ini.Vars.Disable_Transitions, false))
            {
                advancedControl.Visibility = Visibility.Hidden;
                subMenuControl.Settings.IsEnabled = true;
                mainApp.DownloadsPopupControl.gotoDownloads.IsEnabled = true;
                return;
            }

            var transitionInStoryboard = CreateTransitionStoryboard(2400, 0, 0.25);
            transitionInStoryboard.Completed += (s, e) =>
            {
                var fadeOutStoryboard = CreateFadeStoryboard(1, 0, 0.2);
                fadeOutStoryboard.Completed += (s, e) =>
                {
                    advancedControl.Visibility = Visibility.Hidden;
                    var transitionOutStoryboard = CreateTransitionStoryboard(0, -2400, 0.25);
                    transitionOutStoryboard.Begin();
                };
                fadeOutStoryboard.Begin();
            };
            transitionInStoryboard.Begin();
            subMenuControl.Settings.IsEnabled = true;
            mainApp.DownloadsPopupControl.gotoDownloads.IsEnabled = true;
        }

        private static Storyboard CreateTransitionStoryboard(double from, double to, double duration)
        {
            var storyboard = new Storyboard();
            var doubleAnimation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = new Duration(TimeSpan.FromSeconds(duration))
            };
            Storyboard.SetTarget(doubleAnimation, TransitionRect);
            Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath("RenderTransform.Children[0].X"));
            storyboard.Children.Add(doubleAnimation);
            return storyboard;
        }

        private static Storyboard CreateFadeStoryboard(double from, double to, double duration)
        {
            var storyboard = new Storyboard();
            var doubleAnimation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = new Duration(TimeSpan.FromSeconds(duration))
            };
            Storyboard.SetTarget(doubleAnimation, settingsControl);
            Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath("Opacity"));
            storyboard.Children.Add(doubleAnimation);
            return storyboard;
        }

#if DEBUG

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        public static void EnableDebugConsole()
        {
            // Only in Debug build, this will open a console window
            AllocConsole();  // Opens a new console window
        }

#endif
    }
}