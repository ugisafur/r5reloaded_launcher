using System.IO;
using System.Windows;
using System.Windows.Media.Animation;
using static launcher.Global.Logger;
using static launcher.Global.References;
using Hardcodet.Wpf.TaskbarNotification;
using System.Globalization;
using launcher.Game;
using launcher.Global;
using Microsoft.Win32;
using System.Diagnostics;
using DiscordRPC;
using DiscordRPC.Logging;

namespace launcher.Managers
{
    public static class App
    {
        #region Setup Functions

        public static async Task SetupApp(MainWindow mainWindow)
        {
#if DEBUG
            EnableDebugConsole();
#endif
            PreLoad_Window.SetLoadingText("Checking for EA Desktop App");
            await Task.Delay(100);
            await Task.Run(() => FindAndStartEAApp());

            PreLoad_Window.SetLoadingText("Checking for internet connection");
            await Task.Delay(100);
            await Task.Run(() => CheckInternetConnection());

            PreLoad_Window.SetLoadingText("Setting up controls references");
            await Task.Delay(100);
            await Task.Run(() => SetupControlReferences(mainWindow));

            PreLoad_Window.SetLoadingText("Setting up app");
            await Task.Delay(100);
            await Task.Run(() => Launcher.Init());

            if ((bool)Ini.Get(Ini.Vars.Enable_Discord_Rich_Presence))
            {
                PreLoad_Window.SetLoadingText("Setting up Discord RPC");
                await Task.Delay(100);
                await Task.Run(() => InitDiscordRPC());
            }

            PreLoad_Window.SetLoadingText("Setting up menus");
            await Task.Delay(100);
            await Task.Run(() => SetupMenus());

            PreLoad_Window.SetLoadingText("Getting game branches");
            await Task.Delay(100);
            await Task.Run(() => SetupBranchComboBox());

            PreLoad_Window.SetLoadingText("Checking game installs");
            await Task.Delay(100);
            await Task.Run(() => CheckGameInstalls());

            PreLoad_Window.SetLoadingText("Starting update checker");
            await Task.Delay(100);
            await Task.Run(() => GetSelfUpdater());

            PreLoad_Window.SetLoadingText("Getting EULA contents");
            await Task.Delay(100);
            await Task.Run(() => EULA_Control.SetupEULA());

            PreLoad_Window.SetLoadingText("Starting service status");
            await Task.Delay(100);
            Task.Run(() => Status_Control.StartStatusTimer());

            Download.Tasks.ShowSpeedLabels(false, false);

            PreLoad_Window.SetLoadingText("Checking for news");
            await Task.Delay(100);

            Launcher.newsOnline = await Networking.NewsTestAsync();

            if (AppState.IsOnline && Launcher.newsOnline)
            {
                News.Populate();
                MoveNewsRect(0);
                mainWindow.HideNewsRect();
            }
            else
            {
                Main_Window.NewsContainer.Visibility = Visibility.Collapsed;
                foreach (var button in Main_Window.NewsButtons)
                    button.IsEnabled = false;
            }
        }

        public static void InitDiscordRPC()
        {
            if (!AppState.IsOnline)
                return;

            if(RPC_client != null && RPC_client.IsInitialized)
                return;

            RPC_client = new DiscordRpcClient(Launcher.DISCORDRPC_CLIENT_ID)
            {
                Logger = new ConsoleLogger() { Level = LogLevel.Warning }
            };

            RPC_client.OnReady += (sender, e) =>
            {
                LogInfo(Source.DiscordRPC, $"Discord RPC connected as {e.User.Username}");
            };

            //RPC_client.OnPresenceUpdate += (sender, e) =>
            //{
            //    //LogInfo(Source.DiscordRPC, $"Received Update! {e.Presence}");
            //};

            RPC_client.OnError += (sender, e) =>
            {
                LogError(Source.DiscordRPC, $"Discord RPC Error: {e.Message}");
            };

            RPC_client.OnConnectionFailed += (sender, e) =>
            {
                LogError(Source.DiscordRPC, $"Discord RPC Connection Failed");
            };

            RPC_client.OnConnectionEstablished += (sender, e) =>
            {
                LogInfo(Source.DiscordRPC, $"Discord RPC Connection Established");
            };

            RPC_client.Initialize();

            AppState.SetRichPresence("", "Idle", "embedded_cover", "");
        }
        public static bool IsR5ApexOpen()
        {
            Process[] processes = Process.GetProcessesByName("r5apex");
            return processes.Length > 0;
        }

        public static void CloseR5Apex()
        {
            Process[] processes = Process.GetProcessesByName("r5apex");
            foreach (Process process in processes)
            {
                process.Kill();
                process.WaitForExit();
            }
        }

        public static bool HasEnoughFreeSpace(string installPath, long requiredBytes)
        {
            string root = Path.GetPathRoot(Path.GetFullPath(installPath));
            if (string.IsNullOrEmpty(root))
                throw new ArgumentException("Invalid path", nameof(installPath));

            var drive = new DriveInfo(root);
            if (!drive.IsReady)
                throw new IOException($"Drive {drive.Name} is not ready.");

            return drive.AvailableFreeSpace >= requiredBytes;
        }

        private static void FindAndStartEAApp()
        {
            if (!(bool)Ini.Get(Ini.Vars.Auto_Launch_EA_App))
                return;

            Process[] processes = Process.GetProcessesByName("EADesktop");
            if (processes.Length==0)
            {
                string subKeyPath = @"SOFTWARE\WOW6432Node\Electronic Arts\EA Desktop";
                string EADesktopPath = "";
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(subKeyPath))
                {
                    if (key != null)
                    {
                        object installLocationValue = key.GetValue("DesktopAppPath");

                        if (installLocationValue != null)
                        {
                            EADesktopPath = installLocationValue.ToString();
                            LogInfo(Source.Launcher, "Found EA Desktop App");
                        }
                    }
                }

                if (string.IsNullOrEmpty(EADesktopPath))
                {
                    LogError(Source.Launcher, "Failed to find EA Desktop App");
                    return;
                }

                LogInfo(Source.Launcher, "Starting EA Desktop App");
                var startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c start \"\" \"{EADesktopPath}\" /min",
                    WindowStyle = ProcessWindowStyle.Minimized
                };

                Process.Start(startInfo);
            }
            else
            {
                LogInfo(Source.Launcher, "EA Desktop App is already running");
            }
        }

        public enum EAAppCodes
        {
            Installed_And_Running,
            Installed_And_Not_Running,
            Not_Installed,
        }

        public static EAAppCodes IsEAAppRunning()
        {
            if((bool)Ini.Get(Ini.Vars.Offline_Mode))
                return EAAppCodes.Installed_And_Running;

            //TODO: Find a better way to check if EA App is installed
            //string subKeyPath = @"SOFTWARE\WOW6432Node\Electronic Arts\EA Desktop";
            //if (Registry.GetValue($"HKEY_LOCAL_MACHINE\\{subKeyPath}", "DesktopAppPath", null) == null)
            //return EAAppCodes.Not_Installed;

            Process[] processes = Process.GetProcessesByName("EADesktop");
            if (processes.Length == 0)
                return EAAppCodes.Installed_And_Not_Running;

            return EAAppCodes.Installed_And_Running;
        }

        public static void SetupAdvancedMenu()
        {
            if (!GetBranch.Installed() && !GetBranch.IsLocalBranch() || !File.Exists(Path.Combine(GetBranch.Directory(), "platform\\playlists_r5_patch.txt")))
            {
                maps = ["No Selection"];
                gamemodes = ["No Selection"];
                Advanced_Control.serverPage.SetMapList(maps);
                Advanced_Control.serverPage.SetPlaylistList(gamemodes);
                LogInfo(Source.Launcher, "Branch not installed, skipping playlist file");
                return;
            }

            try
            {
                appDispatcher.Invoke(new Action(() =>
                {
                    playlistRoot = PlaylistFile.Parse(Path.Combine(GetBranch.Directory(), "platform\\playlists_r5_patch.txt"));
                    gamemodes = PlaylistFile.GetPlaylists(playlistRoot);
                    maps = PlaylistFile.GetMaps(playlistRoot);
                    Advanced_Control.serverPage.SetMapList(maps);
                    Advanced_Control.serverPage.SetPlaylistList(gamemodes);
                    LogInfo(Source.Launcher, $"Loaded playlist file for branch {GetBranch.Name()}");
                }));
            }
            catch (Exception ex)
            {
                LogException($"Failed to load playlist file", Source.Launcher, ex);
            }
        }

        private static void CheckInternetConnection()
        {
            bool isOnline = Networking.CDNTest().Result;
            LogInfo(Source.Launcher, isOnline ? "Connected to CDN" : "Cant connect to CDN");
            AppState.IsOnline = isOnline;
        }

        private static void SetupMenus()
        {
            appDispatcher.BeginInvoke(new Action(() =>
            {
                Settings_Control.SetupSettingsMenu();
                LogInfo(Source.Launcher, $"Settings menu initialized");

                Advanced_Control.SetupAdvancedSettings();
                LogInfo(Source.Launcher, $"Advanced settings initialized");
            }));
        }

        private static void CheckGameInstalls()
        {
            appDispatcher.BeginInvoke(new Action(() =>
            {
                foreach (var branch in Launcher.ServerConfig.branches)
                {
                    if (GetBranch.Installed(branch) && !Directory.Exists(GetBranch.Directory(branch)))
                    {
                        LogWarning(Source.Launcher, $"Branch {branch.branch} is set as installed but directory is missing");
                        SetBranch.Installed(false, branch);
                        SetBranch.DownloadHDTextures(false, branch);
                        SetBranch.Version("", branch);
                    }
                }
            }));
        }

        private static void SetupBranchComboBox()
        {
            appDispatcher.BeginInvoke(new Action(() =>
            {
                Branch_Combobox.ItemsSource = GetGameBranches();

                string savedBranch = (string)Ini.Get(Ini.Vars.SelectedBranch);
                string selectedBranch = string.IsNullOrEmpty(savedBranch) ? Launcher.ServerConfig.branches[0].branch.ToUpper(new CultureInfo("en-US")) : (string)Ini.Get(Ini.Vars.SelectedBranch);

                int selectedIndex = Launcher.ServerConfig.branches.FindIndex(branch => branch.branch == selectedBranch && branch.show_in_launcher == true);

                if (selectedIndex == -1 || selectedIndex >= Branch_Combobox.Items.Count)
                    selectedIndex = 0;

                Branch_Combobox.SelectedIndex = selectedIndex;

                LogInfo(Source.Launcher, "Game branches initialized");
            }));
        }

        public static List<ComboBranch> GetGameBranches()
        {
            DataCollections.FolderBranches.Clear();

            if (Directory.Exists(GetBaseLibraryPath()))
            {
                string libraryPath = GetBaseLibraryPath();
                string[] directories = Directory.GetDirectories(libraryPath);
                string[] folderNames = directories.Select(Path.GetFileName).ToArray();

                foreach (string folder in folderNames)
                {
                    bool shouldAdd = true;

                    if (AppState.IsOnline)
                        shouldAdd = !Launcher.ServerConfig.branches.Any(b => string.Equals(b.branch, folder, StringComparison.OrdinalIgnoreCase));

                    if (shouldAdd)
                    {
                        Branch branch = new()
                        {
                            branch = folder.ToUpper(new CultureInfo("en-US")),
                            game_url = "",
                            enabled = true,
                            show_in_launcher = true,
                            is_local_branch = true,
                            patch_notes_blog_slug = "null",
                        };
                        DataCollections.FolderBranches.Add(branch);
                        LogInfo(Source.Launcher, $"Local branch found: {folder}");
                    }
                }
            }

            if (AppState.IsOnline)
                Launcher.ServerConfig.branches.AddRange(DataCollections.FolderBranches);
            else
                Launcher.ServerConfig = new ServerConfig { branches = new List<Branch>(DataCollections.FolderBranches) };

            List<Branch> branches_to_remove = [];
            for (int i = 0; i < Launcher.ServerConfig.branches.Count; i++)
            {
                if (!Launcher.ServerConfig.branches[i].enabled)
                    branches_to_remove.Add(Launcher.ServerConfig.branches[i]);
            }

            if (branches_to_remove.Count > 0)
            {
                foreach (Branch branch in branches_to_remove)
                    Launcher.ServerConfig.branches.Remove(branch);
            }

            return Launcher.ServerConfig.branches
                .Where(branch => branch.show_in_launcher || !AppState.IsOnline)
                .Select(branch => new ComboBranch
                {
                    title = branch.branch.ToUpper(new CultureInfo("en-US")),
                    subtext = GetBranch.ServerComboVersion(branch),
                    isLocalBranch = branch.is_local_branch
                })
                .ToList();
        }

        private static void GetSelfUpdater()
        {
            if (!File.Exists(Path.Combine(Launcher.PATH, "launcher_data\\updater.exe")) || (string)Ini.Get(Ini.Vars.Updater_Version) != Launcher.ServerConfig.updaterVersion)
            {
                if (File.Exists(Path.Combine(Launcher.PATH, "launcher_data\\updater.exe")))
                    File.Delete(Path.Combine(Launcher.PATH, "launcher_data\\updater.exe"));

                LogInfo(Source.Launcher, "Downloading launcher updater");
                Networking.HttpClient.GetAsync(Launcher.ServerConfig.launcherSelfUpdater)
                    .ContinueWith(response =>
                    {
                        if (response.Result.IsSuccessStatusCode)
                        {
                            byte[] data = response.Result.Content.ReadAsByteArrayAsync().Result;
                            File.WriteAllBytes(Path.Combine(Launcher.PATH, "launcher_data\\updater.exe"), data);
                            Ini.Set(Ini.Vars.Updater_Version, Launcher.ServerConfig.updaterVersion);
                        }
                    });
            }
        }

        #endregion Setup Functions

        public static string GetBaseLibraryPath()
        {
            string libraryPath = (string)Ini.Get(Ini.Vars.Library_Location);
            string finalDirectory = Path.Combine(libraryPath, "R5R Library");
            return finalDirectory;
        }

        public static bool IsWineEnvironment()
        {
            return Process.GetProcessesByName("winlogon").Length == 0;
        }

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
            double end = windowWidth * 2 + 60;

            var transitionInStoryboard = CreateTransitionStoryboard(start, 0, 0.25);
            transitionInStoryboard.Completed += (s, e) =>
            {
                DragBarDropShadow.Visibility = Visibility.Visible;
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
            double start = windowWidth * 2 + 60;

            var transitionInStoryboard = CreateTransitionStoryboard(start, 0, 0.25);
            transitionInStoryboard.Completed += (s, e) =>
            {
                DragBarDropShadow.Visibility = Visibility.Hidden;
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
            double end = windowWidth * 2 + 60;

            var transitionInStoryboard = CreateTransitionStoryboard(start, 0, 0.25);
            transitionInStoryboard.Completed += (s, e) =>
            {
                DragBarDropShadow.Visibility = Visibility.Visible;
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
            double start = windowWidth * 2 + 60;

            var transitionInStoryboard = CreateTransitionStoryboard(start, 0, 0.25);
            transitionInStoryboard.Completed += (s, e) =>
            {
                DragBarDropShadow.Visibility = Visibility.Hidden;
                Advanced_Control.Visibility = Visibility.Hidden;
                var transitionOutStoryboard = CreateTransitionStoryboard(0, end, 0.25);
                transitionOutStoryboard.Begin();
            };
            transitionInStoryboard.Begin();
            Menu_Control.Settings.IsEnabled = true;
            Downloads_Control.gotoDownloads.IsEnabled = true;
        }

        public static void MoveNewsRect(int index)
        {
            double speed = (bool)Ini.Get(Ini.Vars.Disable_Transitions) ? 1 : 400;

            double startx = Main_Window.News_Rect_Translate.X;
            double endx = Main_Window.NewsButtonsX[index];

            var storyboard = new Storyboard();

            var moveAnimation = new DoubleAnimation
            {
                From = startx,
                To = endx,
                Duration = new Duration(TimeSpan.FromMilliseconds(speed)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            Storyboard.SetTarget(moveAnimation, Main_Window.NewsRect);
            Storyboard.SetTargetProperty(moveAnimation, new PropertyPath("RenderTransform.Children[0].X"));

            double startw = Main_Window.NewsRect.Width;
            double endw = Main_Window.NewsButtonsWidth[index];

            var widthAnimation = new DoubleAnimation
            {
                From = startw,
                To = endw,
                Duration = new Duration(TimeSpan.FromMilliseconds(speed)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            Storyboard.SetTarget(widthAnimation, Main_Window.NewsRect);
            Storyboard.SetTargetProperty(widthAnimation, new PropertyPath("Width"));

            storyboard.Children.Add(moveAnimation);
            storyboard.Children.Add(widthAnimation);

            storyboard.Begin();

            News.SetPage(index);
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
                LogException($"Failed to send notification", Source.Launcher, ex);
            }
        }

        public static async Task AnimateElement(FrameworkElement element, FrameworkElement background, bool isShowing, bool disableAnimations)
        {
            if (isShowing)
            {
                UpdateChecker.otherPopupsOpened = true;
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
                UpdateChecker.otherPopupsOpened = false;
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

        public static Task ShowAskToQuit() =>
            AnimateElement(AskToQuit_Control, POPUP_BG, true, (bool)Ini.Get(Ini.Vars.Disable_Animations));

        public static Task HideAskToQuit() =>
            AnimateElement(AskToQuit_Control, POPUP_BG, false, (bool)Ini.Get(Ini.Vars.Disable_Animations));

        public static Task ShowOnBoardAskPopup() =>
            AnimateElement(OnBoardAsk_Control, POPUP_BG, true, (bool)Ini.Get(Ini.Vars.Disable_Animations));

        public static Task HideOnBoardAskPopup() =>
            AnimateElement(OnBoardAsk_Control, POPUP_BG, false, (bool)Ini.Get(Ini.Vars.Disable_Animations));

        public static Task ShowLauncherUpdatePopup() =>
            AnimateElement(LauncherUpdate_Control, POPUP_BG, true, (bool)Ini.Get(Ini.Vars.Disable_Animations));

        public static Task HideLauncherUpdatePopup() =>
            AnimateElement(LauncherUpdate_Control, POPUP_BG, false, (bool)Ini.Get(Ini.Vars.Disable_Animations));

        public static Task ShowInstallLocation()
        {
            InstallLocation_Control.SetupInstallLocation();
            return AnimateElement(InstallLocation_Control, POPUP_BG, true, (bool)Ini.Get(Ini.Vars.Disable_Animations));
        }

        public static Task HideInstallLocation() =>
            AnimateElement(InstallLocation_Control, POPUP_BG, false, (bool)Ini.Get(Ini.Vars.Disable_Animations));

        public static void StartTour()
        {
            if (AppState.InSettingsMenu)
                HideSettingsControl();

            if (AppState.InAdvancedMenu)
                HideAdvancedControl();

            AppState.OnBoarding = true;

            Main_Window.ResizeMode = ResizeMode.NoResize;
            Main_Window.Width = Main_Window.MinWidth;
            Main_Window.Height = Main_Window.MinHeight;

            OnBoard_Control.SetItem(0);

            Main_Window.OnBoard_Control.Visibility = Visibility.Visible;
            Main_Window.OnBoardingRect.Visibility = Visibility.Visible;
        }

        public static void EndTour()
        {
            AppState.OnBoarding = false;

            OnBoard_Control.Visibility = Visibility.Hidden;
            OnBoardingRect.Visibility = Visibility.Hidden;

            Main_Window.ResizeMode = ResizeMode.CanResize;

            OnBoard_Control.SetItem(0);
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