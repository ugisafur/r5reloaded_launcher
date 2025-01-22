using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Animation;
using static launcher.Logger;
using static launcher.ControlReferences;
using static launcher.LaunchParameters;
using Hardcodet.Wpf.TaskbarNotification;
using System.Globalization;

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
            EnableDebugConsole();
#endif
            CheckInternetConnection();
            SetupControlReferences(mainWindow);
            StartStatusChecker();
            GlobalInitializer.Setup();
            SetupLibaryPath();
            SetupMenus();
            SetupBranchComboBox();
            GetSelfUpdater();
            EULA_Control.SetupEULA();
        }

        public static void SetupAdvancedMenu()
        {
            if ((!IsBranchInstalled() && !GetCurrentBranch().is_local_branch) || !File.Exists(Path.Combine(GetBranchDirectory(), "platform\\playlists_r5_patch.txt")))
            {
                maps = new List<string> { "No Selection" };
                gamemodes = new List<string> { "No Selection" };
                Advanced_Control.serverPage.SetMapList(maps);
                Advanced_Control.serverPage.SetPlaylistList(gamemodes);
                LogInfo(Source.Launcher, "Branch not installed, skipping playlist file");
                return;
            }

            try
            {
                appDispatcher.Invoke(new Action(() =>
                {
                    playlistRoot = PlaylistFile.Parse(Path.Combine(GetBranchDirectory(), "platform\\playlists_r5_patch.txt"));
                    gamemodes = PlaylistFile.GetPlaylists(playlistRoot);
                    maps = PlaylistFile.GetMaps(playlistRoot);
                    Advanced_Control.serverPage.SetMapList(maps);
                    Advanced_Control.serverPage.SetPlaylistList(gamemodes);
                    LogInfo(Source.Launcher, $"Loaded playlist file for branch {GetCurrentBranch().branch}");
                }));
            }
            catch (Exception ex)
            {
                LogError(Source.Launcher, $"Failed to load playlist file: {ex.Message}");
            }
        }

        private static void CheckInternetConnection()
        {
            LogInfo(Source.Launcher, DataFetcher.TestConnection() ? "Connected to CDN" : "Cant connect to CDN");
            AppState.IsOnline = DataFetcher.TestConnection();
        }

        private static void StartStatusChecker()
        {
            if (AppState.IsOnline)
            {
                Task.Run(() => Status_Control.StartStatusTimer());
                return;
            }

            Status_Button.IsEnabled = false;
            Downloads_Button.IsEnabled = false;
        }

        private static void SetupMenus()
        {
            Settings_Control.SetupSettingsMenu();
            LogInfo(Source.Launcher, $"Settings menu initialized");

            Advanced_Control.SetupAdvancedSettings();
            LogInfo(Source.Launcher, $"Advanced settings initialized");
        }

        private static void SetupLibaryPath()
        {
            if (string.IsNullOrEmpty((string)Ini.Get(Ini.Vars.Library_Location)))
            {
                DirectoryInfo parentDir = Directory.GetParent(Constants.Paths.LauncherPath.TrimEnd(Path.DirectorySeparatorChar));
                Ini.Set(Ini.Vars.Library_Location, parentDir.FullName);
            }
        }

        private static void SetupBranchComboBox()
        {
            Branch_Combobox.ItemsSource = GetGameBranches();

            string savedBranch = (string)Ini.Get(Ini.Vars.SelectedBranch);
            string selectedBranch = string.IsNullOrEmpty(savedBranch) ? Configuration.ServerConfig.branches[0].branch : (string)Ini.Get(Ini.Vars.SelectedBranch);

            int selectedIndex = Configuration.ServerConfig.branches.FindIndex(branch => branch.branch == selectedBranch && branch.show_in_launcher == true);

            if (selectedIndex == -1)
                selectedIndex = 0;

            Branch_Combobox.SelectedIndex = selectedIndex;

            LogInfo(Source.Launcher, "Game branches initialized");
        }

        public static List<ComboBranch> GetGameBranches()
        {
            string libraryPath = GetLibraryPathDirectory();
            string[] directories = Directory.GetDirectories(libraryPath);
            string[] folderNames = directories.Select(Path.GetFileName).ToArray();

            DataCollections.FolderBranches.Clear();

            foreach (string folder in folderNames)
            {
                bool shouldAdd = true;

                if (AppState.IsOnline)
                    shouldAdd = !Configuration.ServerConfig.branches.Any(b => string.Equals(b.branch, folder, StringComparison.OrdinalIgnoreCase));

                if (shouldAdd)
                {
                    Branch branch = new()
                    {
                        branch = folder,
                        game_url = "",
                        enabled = true,
                        show_in_launcher = true,
                        is_local_branch = true
                    };
                    DataCollections.FolderBranches.Add(branch);
                    LogInfo(Source.Launcher, $"Local branch found: {folder}");
                }
            }

            if (AppState.IsOnline)
                Configuration.ServerConfig.branches.AddRange(DataCollections.FolderBranches);
            else
                Configuration.ServerConfig = new ServerConfig { branches = new List<Branch>(DataCollections.FolderBranches) };

            return Configuration.ServerConfig.branches
                .Where(branch => branch.show_in_launcher || !AppState.IsOnline)
                .Select(branch => new ComboBranch
                {
                    title = branch.branch,
                    subtext = GetServerBranchVersion(branch),
                    isLocalBranch = branch.is_local_branch
                })
                .ToList();
        }

        private static void GetSelfUpdater()
        {
            if (!File.Exists(Path.Combine(Constants.Paths.LauncherPath, "launcher_data\\updater.exe")))
            {
                LogInfo(Source.Launcher, "Downloading launcher updater");
                Networking.HttpClient.GetAsync(Configuration.ServerConfig.launcherSelfUpdater)
                    .ContinueWith(response =>
                    {
                        if (response.Result.IsSuccessStatusCode)
                        {
                            byte[] data = response.Result.Content.ReadAsByteArrayAsync().Result;
                            File.WriteAllBytes(Path.Combine(Constants.Paths.LauncherPath, "launcher_data\\updater.exe"), data);
                        }
                    });
            }
        }

        #endregion Setup Functions

        #region Launch Game Functions

        public static void LaunchGame()
        {
            eMode mode = (eMode)(int)Ini.Get(Ini.Vars.Mode);

            string exeName = mode switch
            {
                eMode.HOST => "r5apex.exe",
                eMode.SERVER => "r5apex_ds.exe",
                eMode.CLIENT => "r5apex.exe",
                _ => "r5apex.exe"
            };

            if (!File.Exists($"{GetBranchDirectory()}\\{exeName}"))
                return;

            string gameArguments = BuildParameter();

            var startInfo = new ProcessStartInfo
            {
                FileName = $"{GetBranchDirectory()}\\{exeName}",
                WorkingDirectory = GetBranchDirectory(),
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
                int coreCount = int.Parse((string)Ini.Get(Ini.Vars.Processor_Affinity));
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

        public static bool IsBranchEULAAccepted()
        {
            return Ini.Get(Configuration.ServerConfig.branches[GetCmbBranchIndex()].branch, "EULA_Accepted", false);
        }

        public static bool IsBranchInstalled()
        {
            return Ini.Get(Configuration.ServerConfig.branches[GetCmbBranchIndex()].branch, "Is_Installed", false);
        }

        public static string GetBranchVersion()
        {
            return Ini.Get(Configuration.ServerConfig.branches[GetCmbBranchIndex()].branch, "Version", "");
        }

        public static string GetServerBranchVersion(Branch branch)
        {
            if (branch.is_local_branch)
                return "Local Install";

            string version = DataFetcher.FetchBranchVersion(branch.game_url);
            return version;
        }

        public static Branch GetCurrentBranch()
        {
            return Configuration.ServerConfig.branches[GetCmbBranchIndex()];
        }

        public static int GetCmbBranchIndex()
        {
            int cmbSelectedIndex = 0;

            appDispatcher.Invoke(() =>
            {
                cmbSelectedIndex = Branch_Combobox.SelectedIndex;
            });

            return cmbSelectedIndex;
        }

        public static string GetBranchDirectory()
        {
            string branchName = Configuration.ServerConfig.branches[GetCmbBranchIndex()].branch.ToUpper(new CultureInfo("en-US"));
            string libraryPath = (string)Ini.Get(Ini.Vars.Library_Location);
            string finalDirectory = Path.Combine(libraryPath, "R5R Library", branchName);

            return finalDirectory;
        }

        public static string GetLibraryPathDirectory()
        {
            string libraryPath = (string)Ini.Get(Ini.Vars.Library_Location);
            string finalDirectory = Path.Combine(libraryPath, "R5R Library");

            Directory.CreateDirectory(finalDirectory);

            return finalDirectory;
        }

        #endregion Branch Functions

        #region Settings Functions

        //TODO: Refactor these functions to use a single function with parameters
        //      cuz this is just a mess

        public static void ShowSettingsControl()
        {
            AppState.InSettingsMenu = true;

            if ((bool)Ini.Get(Ini.Vars.Disable_Transitions))
            {
                Settings_Control.Visibility = Visibility.Visible;
                Menu_Control.Settings.IsEnabled = false;
                Downloads_Control.gotoDownloads.IsEnabled = false;
                return;
            }

            double windowWidth = Main_Window.Width;
            if (Main_Window.WindowState == WindowState.Maximized)
                windowWidth = SystemParameters.PrimaryScreenWidth;

            double start = -(windowWidth * 2) - 60;
            double end = (windowWidth * 2) + 60;

            var transitionInStoryboard = CreateTransitionStoryboard(start, 0, 0.25);
            transitionInStoryboard.Completed += (s, e) =>
            {
                Settings_Control.Visibility = Visibility.Visible;
                var transitionOutStoryboard = CreateTransitionStoryboard(0, end, 0.25);
                transitionOutStoryboard.Begin();
            };
            transitionInStoryboard.Begin();
            Menu_Control.Settings.IsEnabled = false;
            Downloads_Control.gotoDownloads.IsEnabled = false;
        }

        public static void HideSettingsControl()
        {
            AppState.InSettingsMenu = false;

            if ((bool)Ini.Get(Ini.Vars.Disable_Transitions))
            {
                Settings_Control.Visibility = Visibility.Hidden;
                Menu_Control.Settings.IsEnabled = true;
                Downloads_Control.gotoDownloads.IsEnabled = true;
                return;
            }

            double windowWidth = Main_Window.Width;
            if (Main_Window.WindowState == WindowState.Maximized)
                windowWidth = SystemParameters.PrimaryScreenWidth;

            double end = -(windowWidth * 2) - 60;
            double start = (windowWidth * 2) + 60;

            var transitionInStoryboard = CreateTransitionStoryboard(start, 0, 0.25);
            transitionInStoryboard.Completed += (s, e) =>
            {
                Settings_Control.Visibility = Visibility.Hidden;
                var transitionOutStoryboard = CreateTransitionStoryboard(0, end, 0.25);
                transitionOutStoryboard.Begin();
            };
            transitionInStoryboard.Begin();
            Menu_Control.Settings.IsEnabled = true;
            Downloads_Control.gotoDownloads.IsEnabled = true;
        }

        public static void ShowAdvancedControl()
        {
            AppState.InAdvancedMenu = true;

            if ((bool)Ini.Get(Ini.Vars.Disable_Transitions))
            {
                Advanced_Control.Visibility = Visibility.Visible;
                Menu_Control.Settings.IsEnabled = false;
                Downloads_Control.gotoDownloads.IsEnabled = false;
                return;
            }

            double windowWidth = Main_Window.Width;
            if (Main_Window.WindowState == WindowState.Maximized)
                windowWidth = SystemParameters.PrimaryScreenWidth;

            double start = -(windowWidth * 2) - 60;
            double end = (windowWidth * 2) + 60;

            var transitionInStoryboard = CreateTransitionStoryboard(start, 0, 0.25);
            transitionInStoryboard.Completed += (s, e) =>
            {
                Advanced_Control.Visibility = Visibility.Visible;
                var transitionOutStoryboard = CreateTransitionStoryboard(0, end, 0.25);
                transitionOutStoryboard.Begin();
            };
            transitionInStoryboard.Begin();
            Menu_Control.Settings.IsEnabled = false;
            Downloads_Control.gotoDownloads.IsEnabled = false;
        }

        public static void HideAdvancedControl()
        {
            AppState.InAdvancedMenu = false;

            if ((bool)Ini.Get(Ini.Vars.Disable_Transitions))
            {
                Advanced_Control.Visibility = Visibility.Hidden;
                Menu_Control.Settings.IsEnabled = true;
                Downloads_Control.gotoDownloads.IsEnabled = true;
                return;
            }

            double windowWidth = Main_Window.Width;
            if (Main_Window.WindowState == WindowState.Maximized)
                windowWidth = SystemParameters.PrimaryScreenWidth;

            double end = -(windowWidth * 2) - 60;
            double start = (windowWidth * 2) + 60;

            var transitionInStoryboard = CreateTransitionStoryboard(start, 0, 0.25);
            transitionInStoryboard.Completed += (s, e) =>
            {
                Advanced_Control.Visibility = Visibility.Hidden;
                var transitionOutStoryboard = CreateTransitionStoryboard(0, end, 0.25);
                transitionOutStoryboard.Begin();
            };
            transitionInStoryboard.Begin();
            Menu_Control.Settings.IsEnabled = true;
            Downloads_Control.gotoDownloads.IsEnabled = true;
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
            Storyboard.SetTarget(doubleAnimation, Transition_Rect);
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
            Storyboard.SetTarget(doubleAnimation, Settings_Control);
            Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath("Opacity"));
            storyboard.Children.Add(doubleAnimation);
            return storyboard;
        }

        #endregion Settings Functions

        public static void SendNotification(string message, BalloonIcon icon)
        {
            if (!(bool)Ini.Get(Ini.Vars.Enable_Notifications))
                return;

            try
            {
                System_Tray.ShowBalloonTip("R5R Launcher", message, icon);
            }
            catch (Exception ex)
            {
                LogError(Source.Launcher, $"Failed to send notification: {ex.Message}");
            }
        }

        public static async Task AnimateElement(FrameworkElement element, FrameworkElement background, bool isShowing, bool disableAnimations)
        {
            if (isShowing)
            {
                element.Visibility = Visibility.Visible;
                background.Visibility = Visibility.Visible;
            }

            int duration = disableAnimations ? 1 : 500;
            var storyboard = new Storyboard();
            Duration animationDuration = new(TimeSpan.FromMilliseconds(duration));
            var easing = new CubicEase { EasingMode = EasingMode.EaseInOut };

            // Animation for the background
            var backgroundOpacity = new DoubleAnimation
            {
                From = isShowing ? 0 : 1,
                To = isShowing ? 1 : 0,
                Duration = animationDuration,
                EasingFunction = easing
            };
            Storyboard.SetTarget(backgroundOpacity, background);
            Storyboard.SetTargetProperty(backgroundOpacity, new PropertyPath("Opacity"));

            // Animation for the element
            var elementOpacity = new DoubleAnimation
            {
                From = isShowing ? 0 : 1,
                To = isShowing ? 1 : 0,
                Duration = animationDuration,
                EasingFunction = easing
            };
            Storyboard.SetTarget(elementOpacity, element);
            Storyboard.SetTargetProperty(elementOpacity, new PropertyPath("Opacity"));

            storyboard.Children.Add(backgroundOpacity);
            storyboard.Children.Add(elementOpacity);

            TaskCompletionSource<bool> tcs = new();
            storyboard.Completed += (s, e) => tcs.SetResult(true);

            storyboard.Begin();

            await tcs.Task;

            if (!isShowing)
            {
                element.Visibility = Visibility.Hidden;
                background.Visibility = Visibility.Hidden;
            }
        }

        // Methods to show and hide EULA
        public static Task ShowEULA() =>
            AnimateElement(EULA_Control, POPUP_BG, true, (bool)Ini.Get(Ini.Vars.Disable_Animations));

        public static Task HideEULA() =>
            AnimateElement(EULA_Control, POPUP_BG, false, (bool)Ini.Get(Ini.Vars.Disable_Animations));

        // Methods to show and hide DownloadOptFiles
        public static Task ShowDownloadOptlFiles() =>
            AnimateElement(OptFiles_Control, POPUP_BG, true, (bool)Ini.Get(Ini.Vars.Disable_Animations));

        public static Task HideDownloadOptlFiles() =>
            AnimateElement(OptFiles_Control, POPUP_BG, false, (bool)Ini.Get(Ini.Vars.Disable_Animations));

        // Methods to show and hide CheckExistingFiles
        public static Task ShowCheckExistingFiles() =>
            AnimateElement(CheckFiles_Control, POPUP_BG, true, (bool)Ini.Get(Ini.Vars.Disable_Animations));

        public static Task HideCheckExistingFiles() =>
            AnimateElement(CheckFiles_Control, POPUP_BG, false, (bool)Ini.Get(Ini.Vars.Disable_Animations));

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