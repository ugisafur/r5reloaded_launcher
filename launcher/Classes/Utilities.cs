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
using System.Windows.Shapes;
using Path = System.IO.Path;

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
        #region Setup Functions

        public static void SetupApp(MainWindow mainWindow)
        {
#if DEBUG
            LogInfo(Source.Launcher, "Debug console enabled");
            EnableDebugConsole();
#endif
            CheckInternetConnection();
            SetupControlReferences(mainWindow);
            StartStatusChecker();
            SetupGlobals();
            SetupSettingsMenus();
            SetupLibaryPath();
            SetupBranchComboBox();
            GetSelfUpdater();
        }

        private static void CheckInternetConnection()
        {
            if (DataFetcher.HasInternetConnection())
            {
                LogInfo(Source.Launcher, "Internet connection detected");
                IS_ONLINE = true;
            }
            else
            {
                LogWarning(Source.Launcher, "No internet connection detected");
                IS_ONLINE = false;
            }
        }

        private static void StartStatusChecker()
        {
            if (IS_ONLINE)
                Task.Run(() => statusPopup.StartStatusTimer());
            else
            {
                mainApp.StatusBtn.IsEnabled = false;
                mainApp.DownloadsBtn.IsEnabled = false;
            }
        }

        private static void SetupSettingsMenus()
        {
            settingsControl.SetupSettingsMenu();
            LogInfo(Source.Launcher, $"Settings menu initialized");

            advancedControl.SetupAdvancedSettings();
            LogInfo(Source.Launcher, $"Advanced settings initialized");
        }

        private static void SetupLibaryPath()
        {
            if (string.IsNullOrEmpty(Ini.Get(Ini.Vars.Library_Location, "")))
            {
                DirectoryInfo parentDir = Directory.GetParent(LAUNCHER_PATH.TrimEnd(Path.DirectorySeparatorChar));

                Ini.Set(Ini.Vars.Library_Location, parentDir.FullName);
            }
        }

        private static void SetupBranchComboBox()
        {
            cmbBranch.ItemsSource = GetGameBranches();

            string selectedBranch = Ini.Get(Ini.Vars.SelectedBranch, "");
            if (string.IsNullOrEmpty(selectedBranch))
            {
                Ini.Set(Ini.Vars.SelectedBranch, SERVER_CONFIG.branches[0].branch);
                selectedBranch = SERVER_CONFIG.branches[0].branch;
            }

            int selectedIndex = SERVER_CONFIG.branches.FindIndex(branch => branch.branch == selectedBranch && branch.show_in_launcher == true);

            if (selectedIndex == -1)
            {
                selectedIndex = 0;
                Ini.Set(Ini.Vars.SelectedBranch, SERVER_CONFIG.branches[0].branch);
            }

            cmbBranch.SelectedIndex = selectedIndex;

            LogInfo(Source.Launcher, "Game branches initialized");
        }

        public static List<ComboBranch> GetGameBranches()
        {
            string libraryPath = FileManager.GetLibraryPathDirectory();
            string[] directories = Directory.GetDirectories(libraryPath);
            string[] folderNames = directories.Select(Path.GetFileName).ToArray();

            folderBranches.Clear();

            foreach (string folder in folderNames)
            {
                bool shouldAdd = true;

                if (IS_ONLINE)
                {
                    shouldAdd = !SERVER_CONFIG.branches
                        .Any(b => string.Equals(b.branch, folder, StringComparison.OrdinalIgnoreCase));
                }

                if (shouldAdd)
                {
                    Branch branch = new()
                    {
                        branch = folder,
                        currentVersion = "Local Install",
                        lastVersion = "",
                        game_url = "",
                        patch_url = "",
                        enabled = true,
                        show_in_launcher = true,
                        is_local_branch = true
                    };
                    folderBranches.Add(branch);
                    LogInfo(Source.Launcher, $"Local branch found: {folder}");
                }
            }

            if (IS_ONLINE)
            {
                SERVER_CONFIG.branches.AddRange(folderBranches);
            }
            else
            {
                SERVER_CONFIG = new ServerConfig
                {
                    branches = new List<Branch>(folderBranches)
                };
            }

            return SERVER_CONFIG.branches
                .Where(branch => branch.show_in_launcher || !IS_ONLINE)
                .Select(branch => new ComboBranch
                {
                    title = branch.branch,
                    subtext = branch.currentVersion,
                    isLocalBranch = branch.is_local_branch
                })
                .ToList();
        }

        private static void GetSelfUpdater()
        {
            if (!File.Exists(Path.Combine(LAUNCHER_PATH, "launcher_data\\selfupdater.exe")))
            {
                LogInfo(Source.Launcher, "Downloading self updater");
                CLIENT.GetAsync(SERVER_CONFIG.launcherSelfUpdater)
                    .ContinueWith(response =>
                    {
                        if (response.Result.IsSuccessStatusCode)
                        {
                            byte[] data = response.Result.Content.ReadAsByteArrayAsync().Result;
                            File.WriteAllBytes(Path.Combine(LAUNCHER_PATH, "launcher_data\\selfupdater.exe"), data);
                        }
                    });
            }
        }

        #endregion Setup Functions

        #region Launch Game Functions

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
                FileName = $"{FileManager.GetBranchDirectory()}\\{exeName}",
                WorkingDirectory = FileManager.GetBranchDirectory(),
                Arguments = gameArguments,
                UseShellExecute = true,
                CreateNoWindow = true
            };

            Process gameProcess = Process.Start(startInfo);

            if (gameProcess != null)
                SetProcessorAffinity(gameProcess);

            LogInfo(Source.Launcher, $"Launched game with arguments: {gameArguments}");
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

                    LogInfo(Source.Launcher, $"Processor affinity set to the first {coreCount} cores.");
                }
                else
                    LogError(Source.Launcher, $"Invalid core index: {coreCount}. Must be between -1 and {processorCount}.");
            }
            catch (Exception ex)
            {
                LogError(Source.Launcher, $"Failed to set processor affinity: {ex.Message}");
            }
        }

        #endregion Launch Game Functions

        #region Branch Functions

        public static bool IsBranchInstalled()
        {
            return Ini.Get(SERVER_CONFIG.branches[GetCmbBranchIndex()].branch, "Is_Installed", false);
        }

        public static string GetBranchVersion()
        {
            return Ini.Get(SERVER_CONFIG.branches[GetCmbBranchIndex()].branch, "Version", "");
        }

        public static Branch GetCurrentBranch()
        {
            return SERVER_CONFIG.branches[GetCmbBranchIndex()];
        }

        public static int GetCmbBranchIndex()
        {
            int cmbSelectedIndex = 0;

            appDispatcher.Invoke(() =>
            {
                cmbSelectedIndex = cmbBranch.SelectedIndex;
            });

            return cmbSelectedIndex;
        }

        #endregion Branch Functions

        #region Settings Functions

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

        #endregion Settings Functions

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