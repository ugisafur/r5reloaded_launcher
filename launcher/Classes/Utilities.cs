using SoftCircuits.IniFileParser;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using static launcher.Logger;
using static launcher.Global;
using static launcher.ControlReferences;

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
        public enum IniSettings
        {
            Enable_Quit_On_Close,
            Disable_Background_Video,
            Disable_Animations,
            Disable_Transitions,
            Concurrent_Downloads,
            Download_Speed_Limit,
            Download_HD_Textures,
            HD_Textures_Installed,
            Enable_Cheats,
            Enable_Developer,
            Show_Console,
            Color_Console,
            Playlists_File,
            Mode,
            Visibility,
            HostName,
            Command_Line,
            Resolution_Width,
            Resolution_Height,
            Reserved_Cores,
            Worker_Threads,
            Processor_Affinity,
            No_Async,
            Encrypt_Packets,
            Queued_Packets,
            Random_Netkey,
            No_Timeout,
            Windowed,
            Borderless,
            Max_FPS,
            Current_Version,
            Current_Branch,
            Installed,
            Map,
            Playlist
        }

        private enum eMode
        {
            HOST,
            SERVER,
            CLIENT
        }

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

            IS_INSTALLED = GetIniSetting(IniSettings.Installed, false);
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

            eMode mode = (eMode)GetIniSetting(IniSettings.Mode, 0);

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
                int coreCount = int.Parse(GetIniSetting(IniSettings.Processor_Affinity, "-1"));
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

            if (GetIniSetting(IniSettings.Disable_Transitions, false))
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

            if (GetIniSetting(IniSettings.Disable_Transitions, false))
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

            if (GetIniSetting(IniSettings.Disable_Transitions, false))
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

            if (GetIniSetting(IniSettings.Disable_Transitions, false))
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

        private static IniFile GetIniFile()
        {
            IniFile file = new();
            file.Load(Path.Combine(LAUNCHER_PATH, "platform\\cfg\\user\\launcherConfig.ini"));
            return file;
        }

        private static bool IniExists()
        {
            return File.Exists(Path.Combine(LAUNCHER_PATH, "platform\\cfg\\user\\launcherConfig.ini"));
        }

        public static void SetIniSetting(IniSettings setting, bool value)
        {
            if (!IniExists())
                return;

            IniFile file = GetIniFile();
            file.SetSetting(GetSettingsSectionString(setting), GetSettingString(setting), value);
            file.Save(Path.Combine(LAUNCHER_PATH, "platform\\cfg\\user\\launcherConfig.ini"));
            Log(Logger.Type.Info, Source.Ini, $"Setting {setting} to: {value}");
        }

        public static void SetIniSetting(IniSettings setting, int value)
        {
            if (!IniExists())
                return;

            IniFile file = GetIniFile();
            file.SetSetting(GetSettingsSectionString(setting), GetSettingString(setting), value);
            file.Save(Path.Combine(LAUNCHER_PATH, "platform\\cfg\\user\\launcherConfig.ini"));
            Log(Logger.Type.Info, Source.Ini, $"Setting {setting} to: {value}");
        }

        public static void SetIniSetting(IniSettings setting, string value)
        {
            if (!IniExists())
                return;

            IniFile file = GetIniFile();
            file.SetSetting(GetSettingsSectionString(setting), GetSettingString(setting), value);
            file.Save(Path.Combine(LAUNCHER_PATH, "platform\\cfg\\user\\launcherConfig.ini"));
            Log(Logger.Type.Info, Source.Ini, $"Setting {setting} to: {value}");
        }

        public static bool GetIniSetting(IniSettings setting, bool defaultValue)
        {
            if (!IniExists())
                return defaultValue;

            IniFile file = GetIniFile();
            bool value = file.GetSetting(GetSettingsSectionString(setting), GetSettingString(setting), defaultValue);
            return value;
        }

        public static int GetIniSetting(IniSettings setting, int defaultValue)
        {
            if (!IniExists())
                return defaultValue;

            IniFile file = GetIniFile();
            int value = file.GetSetting(GetSettingsSectionString(setting), GetSettingString(setting), defaultValue);
            return value;
        }

        public static string GetIniSetting(IniSettings setting, string defaultValue)
        {
            if (!IniExists())
                return defaultValue;

            IniFile file = GetIniFile();
            string value = file.GetSetting(GetSettingsSectionString(setting), GetSettingString(setting), defaultValue);
            return value;
        }

        public static void CreateLauncherConfig()
        {
            Directory.CreateDirectory(Path.Combine(LAUNCHER_PATH, "platform\\cfg\\user"));

            string iniPath = Path.Combine(LAUNCHER_PATH, "platform\\cfg\\user\\launcherConfig.ini");
            if (!File.Exists(iniPath))
            {
                IniFile file = new();

                file.SetSetting("Settings", "Enable_Quit_On_Close", true);
                file.SetSetting("Settings", "Disable_Background_Video", false);
                file.SetSetting("Settings", "Disable_Animations", false);
                file.SetSetting("Settings", "Disable_Transitions", false);
                file.SetSetting("Settings", "Concurrent_Downloads", "Max");
                file.SetSetting("Settings", "Download_Speed_Limit", "");
                file.SetSetting("Settings", "Download_HD_Textures", false);

                file.SetSetting("Advanced_Options", "Enable_Cheats", false);
                file.SetSetting("Advanced_Options", "Enable_Developer", false);
                file.SetSetting("Advanced_Options", "Show_Console", false);
                file.SetSetting("Advanced_Options", "Color_Console", true);
                file.SetSetting("Advanced_Options", "Playlists_File", "playlists_r5_patch.txt");
                file.SetSetting("Advanced_Options", "Map", "");
                file.SetSetting("Advanced_Options", "Playlist", "");
                file.SetSetting("Advanced_Options", "Mode", 0);
                file.SetSetting("Advanced_Options", "Visibility", 0);
                file.SetSetting("Advanced_Options", "HostName", "");
                file.SetSetting("Advanced_Options", "Command_Line", "");
                file.SetSetting("Advanced_Options", "Resolution_Width", "");
                file.SetSetting("Advanced_Options", "Resolution_Height", "");
                file.SetSetting("Advanced_Options", "Reserved_Cores", "-1");
                file.SetSetting("Advanced_Options", "Worker_Threads", "-1");
                file.SetSetting("Advanced_Options", "Processor_Affinity", "0");
                file.SetSetting("Advanced_Options", "No_Async", false);
                file.SetSetting("Advanced_Options", "Encrypt_Packets", true);
                file.SetSetting("Advanced_Options", "Queued_Packets", true);
                file.SetSetting("Advanced_Options", "Random_Netkey", true);
                file.SetSetting("Advanced_Options", "No_Timeout", false);
                file.SetSetting("Advanced_Options", "Windowed", false);
                file.SetSetting("Advanced_Options", "Borderless", false);
                file.SetSetting("Advanced_Options", "Max_FPS", "-1");

                file.SetSetting("Launcher", "HD_Textures_Installed", false);
                file.SetSetting("Launcher", "Current_Version", "");
                file.SetSetting("Launcher", "Current_Branch", "");
                file.SetSetting("Launcher", "Installed", false);

                file.Save(iniPath);
            }
        }

        private static string GetSettingsSectionString(IniSettings setting)
        {
            return setting switch
            {
                IniSettings.Enable_Quit_On_Close => "Settings",
                IniSettings.Disable_Background_Video => "Settings",
                IniSettings.Disable_Animations => "Settings",
                IniSettings.Disable_Transitions => "Settings",
                IniSettings.Concurrent_Downloads => "Settings",
                IniSettings.Download_Speed_Limit => "Settings",
                IniSettings.Download_HD_Textures => "Settings",

                IniSettings.Enable_Cheats => "Advanced_Options",
                IniSettings.Enable_Developer => "Advanced_Options",
                IniSettings.Show_Console => "Advanced_Options",
                IniSettings.Color_Console => "Advanced_Options",
                IniSettings.Playlists_File => "Advanced_Options",
                IniSettings.Map => "Advanced_Options",
                IniSettings.Playlist => "Advanced_Options",
                IniSettings.Mode => "Advanced_Options",
                IniSettings.Visibility => "Advanced_Options",
                IniSettings.HostName => "Advanced_Options",
                IniSettings.Command_Line => "Advanced_Options",
                IniSettings.Resolution_Width => "Advanced_Options",
                IniSettings.Resolution_Height => "Advanced_Options",
                IniSettings.Reserved_Cores => "Advanced_Options",
                IniSettings.Worker_Threads => "Advanced_Options",
                IniSettings.Processor_Affinity => "Advanced_Options",
                IniSettings.No_Async => "Advanced_Options",
                IniSettings.Encrypt_Packets => "Advanced_Options",
                IniSettings.Queued_Packets => "Advanced_Options",
                IniSettings.Random_Netkey => "Advanced_Options",
                IniSettings.No_Timeout => "Advanced_Options",
                IniSettings.Windowed => "Advanced_Options",
                IniSettings.Borderless => "Advanced_Options",
                IniSettings.Max_FPS => "Advanced_Options",

                IniSettings.Current_Version => "Launcher",
                IniSettings.Current_Branch => "Launcher",
                IniSettings.Installed => "Launcher",
                IniSettings.HD_Textures_Installed => "Launcher",
                _ => throw new NotImplementedException()
            };
        }

        private static string GetSettingString(IniSettings setting)
        {
            return setting switch
            {
                IniSettings.Enable_Quit_On_Close => "Enable_Quit_On_Close",
                IniSettings.Disable_Background_Video => "Disable_Background_Video",
                IniSettings.Disable_Animations => "Disable_Animations",
                IniSettings.Disable_Transitions => "Disable_Transitions",
                IniSettings.Concurrent_Downloads => "Concurrent_Downloads",
                IniSettings.Download_Speed_Limit => "Download_Speed_Limit",
                IniSettings.Download_HD_Textures => "Download_HD_Textures",
                IniSettings.Enable_Cheats => "Enable_Cheats",
                IniSettings.Enable_Developer => "Enable_Developer",
                IniSettings.Show_Console => "Show_Console",
                IniSettings.Color_Console => "Color_Console",
                IniSettings.Playlists_File => "Playlists_File",
                IniSettings.Map => "Map",
                IniSettings.Playlist => "Playlist",
                IniSettings.Mode => "Mode",
                IniSettings.Visibility => "Visibility",
                IniSettings.HostName => "HostName",
                IniSettings.Command_Line => "Command_Line",
                IniSettings.Resolution_Width => "Resolution_Width",
                IniSettings.Resolution_Height => "Resolution_Height",
                IniSettings.Reserved_Cores => "Reserved_Cores",
                IniSettings.Worker_Threads => "Worker_Threads",
                IniSettings.Processor_Affinity => "Processor_Affinity",
                IniSettings.No_Async => "No_Async",
                IniSettings.Encrypt_Packets => "Encrypt_Packets",
                IniSettings.Queued_Packets => "Queued_Packets",
                IniSettings.Random_Netkey => "Random_Netkey",
                IniSettings.No_Timeout => "No_Timeout",
                IniSettings.Windowed => "Windowed",
                IniSettings.Borderless => "Borderless",
                IniSettings.Max_FPS => "Max_FPS",
                IniSettings.Current_Version => "Current_Version",
                IniSettings.Current_Branch => "Current_Branch",
                IniSettings.Installed => "Installed",
                IniSettings.HD_Textures_Installed => "HD_Textures_Installed",
                _ => throw new NotImplementedException()
            };
        }

        private static void AppendParameter(ref string svParameters, string parameter, string value = "")
        {
            svParameters += value == "" ? $"{parameter} " : $"{parameter} {value} ";
        }

        private static void AppendHostParameters(ref string svParameters)
        {
            if (!string.IsNullOrEmpty(GetIniSetting(IniSettings.HostName, "")))
            {
                AppendParameter(ref svParameters, "+hostname", GetIniSetting(IniSettings.HostName, ""));
                AppendParameter(ref svParameters, "+sv_pylonVisibility", GetIniSetting(IniSettings.Visibility, 0).ToString());
            }
        }

        private static void AppendVideoParameters(ref string svParameters)
        {
            if (GetIniSetting(IniSettings.Windowed, false))
                AppendParameter(ref svParameters, "-windowed");
            else
                AppendParameter(ref svParameters, "-fullscreen");

            if (GetIniSetting(IniSettings.Borderless, false))
                AppendParameter(ref svParameters, "-noborder");
            else
                AppendParameter(ref svParameters, "-forceborder");

            AppendParameter(ref svParameters, "+fps_max", GetIniSetting(IniSettings.Max_FPS, "-1"));

            if (!string.IsNullOrEmpty(GetIniSetting(IniSettings.Resolution_Width, "")))
                AppendParameter(ref svParameters, "-w", GetIniSetting(IniSettings.Resolution_Width, ""));

            if (!string.IsNullOrEmpty(GetIniSetting(IniSettings.Resolution_Height, "")))
                AppendParameter(ref svParameters, "-h", GetIniSetting(IniSettings.Resolution_Height, ""));
        }

        private static void AppendProcessorParameters(ref string svParameters)
        {
            int nReservedCores = int.Parse(GetIniSetting(IniSettings.Reserved_Cores, "-1"));
            if (nReservedCores > -1) // A reserved core count of 0 seems to crash the game on some systems.
                AppendParameter(ref svParameters, "-numreservedcores", GetIniSetting(IniSettings.Reserved_Cores, "-1"));

            int nWorkerThreads = int.Parse(GetIniSetting(IniSettings.Worker_Threads, "-1"));
            if (nWorkerThreads > -1)
                AppendParameter(ref svParameters, "-numworkerthreads", GetIniSetting(IniSettings.Worker_Threads, "-1"));
        }

        private static void AppendNetParameters(ref string svParameters)
        {
            AppendParameter(ref svParameters, "+net_encryptionEnable", GetIniSetting(IniSettings.Encrypt_Packets, false) == true ? "1" : "0");
            AppendParameter(ref svParameters, "+net_useRandomKey", GetIniSetting(IniSettings.Random_Netkey, false) == true ? "1" : "0");
            AppendParameter(ref svParameters, "+net_queued_packet_thread", GetIniSetting(IniSettings.Queued_Packets, false) == true ? "1" : "0");

            if (GetIniSetting(IniSettings.No_Timeout, false))
                AppendParameter(ref svParameters, "-notimeout");
        }

        private static void AppendConsoleParameters(ref string svParameters)
        {
            eMode mode = (eMode)GetIniSetting(IniSettings.Mode, 0);

            if (GetIniSetting(IniSettings.Show_Console, false) || mode == eMode.SERVER)
                AppendParameter(ref svParameters, "-wconsole");
            else
                AppendParameter(ref svParameters, "-noconsole");

            if (GetIniSetting(IniSettings.Color_Console, false))
                AppendParameter(ref svParameters, "-ansicolor");

            if (!string.IsNullOrEmpty(GetIniSetting(IniSettings.Playlists_File, "playlists_r5_patch.txt")))
                AppendParameter(ref svParameters, "-playlistfile", GetIniSetting(IniSettings.Playlists_File, "playlists_r5_patch.txt"));
        }

        public static string BuildParameter()
        {
            string svParameters = "";

            AppendProcessorParameters(ref svParameters);
            AppendConsoleParameters(ref svParameters);
            AppendNetParameters(ref svParameters);

            eMode mode = (eMode)GetIniSetting(IniSettings.Mode, 0);
            switch (mode)
            {
                case eMode.HOST:
                    {
                        // GAME ###############################################################
                        if (!string.IsNullOrEmpty(GetIniSetting(IniSettings.Map, "")))
                            AppendParameter(ref svParameters, "+map", GetIniSetting(IniSettings.Map, ""));

                        if (!string.IsNullOrEmpty(GetIniSetting(IniSettings.Playlist, "")))
                            AppendParameter(ref svParameters, "+launchplaylist", GetIniSetting(IniSettings.Playlist, ""));

                        if (GetIniSetting(IniSettings.Enable_Developer, false))
                        {
                            AppendParameter(ref svParameters, "-dev");
                            AppendParameter(ref svParameters, "-devsdk");
                        }

                        if (GetIniSetting(IniSettings.Enable_Cheats, false))
                        {
                            AppendParameter(ref svParameters, "-dev");
                            AppendParameter(ref svParameters, "-showdevmenu");
                        }

                        // ENGINE ###############################################################
                        if (GetIniSetting(IniSettings.No_Async, false))
                        {
                            AppendParameter(ref svParameters, "-noasync");
                            AppendParameter(ref svParameters, "+async_serialize", "0");
                            AppendParameter(ref svParameters, "+buildcubemaps_async", "0");
                            AppendParameter(ref svParameters, "+sv_asyncAIInit", "0");
                            AppendParameter(ref svParameters, "+sv_asyncSendSnapshot", "0");
                            AppendParameter(ref svParameters, "+sv_scriptCompileAsync", "0");
                            AppendParameter(ref svParameters, "+cl_scriptCompileAsync", "0");
                            AppendParameter(ref svParameters, "+cl_async_bone_setup", "0");
                            AppendParameter(ref svParameters, "+cl_updatedirty_async", "0");
                            AppendParameter(ref svParameters, "+mat_syncGPU", "1");
                            AppendParameter(ref svParameters, "+mat_sync_rt", "1");
                            AppendParameter(ref svParameters, "+mat_sync_rt_flushes_gpu", "1");
                            AppendParameter(ref svParameters, "+net_async_sendto", "0");
                            AppendParameter(ref svParameters, "+physics_async_sv", "0");
                            AppendParameter(ref svParameters, "+physics_async_cl", "0");
                        }

                        AppendHostParameters(ref svParameters);
                        AppendVideoParameters(ref svParameters);

                        if (!string.IsNullOrEmpty(GetIniSetting(IniSettings.Command_Line, "")))
                            AppendParameter(ref svParameters, GetIniSetting(IniSettings.Command_Line, ""));

                        return svParameters;
                    }
                case eMode.SERVER:
                    {
                        // GAME ###############################################################
                        if (!string.IsNullOrEmpty(GetIniSetting(IniSettings.Map, "")))
                            AppendParameter(ref svParameters, "+map", GetIniSetting(IniSettings.Map, ""));

                        if (!string.IsNullOrEmpty(GetIniSetting(IniSettings.Playlist, "")))
                            AppendParameter(ref svParameters, "+launchplaylist", GetIniSetting(IniSettings.Playlist, ""));

                        if (GetIniSetting(IniSettings.Enable_Developer, false))
                        {
                            AppendParameter(ref svParameters, "-dev");
                            AppendParameter(ref svParameters, "-devsdk");
                        }

                        if (GetIniSetting(IniSettings.Enable_Cheats, false))
                        {
                            AppendParameter(ref svParameters, "-dev");
                            AppendParameter(ref svParameters, "-showdevmenu");
                        }

                        // ENGINE ###############################################################
                        if (GetIniSetting(IniSettings.No_Async, false))
                        {
                            AppendParameter(ref svParameters, "-noasync");
                            AppendParameter(ref svParameters, "+async_serialize", "0");
                            AppendParameter(ref svParameters, "+sv_asyncAIInit", "0");
                            AppendParameter(ref svParameters, "+sv_asyncSendSnapshot", "0");
                            AppendParameter(ref svParameters, "+sv_scriptCompileAsync", "0");
                            AppendParameter(ref svParameters, "+physics_async_sv", "0");
                        }

                        AppendHostParameters(ref svParameters);

                        if (!string.IsNullOrEmpty(GetIniSetting(IniSettings.Command_Line, "")))
                            AppendParameter(ref svParameters, GetIniSetting(IniSettings.Command_Line, ""));

                        return svParameters;
                    }
                case eMode.CLIENT:
                    {
                        // Tells the loader module to only load the client dll.
                        AppendParameter(ref svParameters, "-noserverdll");

                        // GAME ###############################################################
                        if (GetIniSetting(IniSettings.Enable_Developer, false))
                        {
                            AppendParameter(ref svParameters, "-dev");
                            AppendParameter(ref svParameters, "-devsdk");
                        }

                        if (GetIniSetting(IniSettings.Enable_Cheats, false))
                        {
                            AppendParameter(ref svParameters, "-dev");
                            AppendParameter(ref svParameters, "-showdevmenu");
                        }

                        // ENGINE ###############################################################
                        if (GetIniSetting(IniSettings.No_Async, false))
                        {
                            AppendParameter(ref svParameters, "-noasync");
                            AppendParameter(ref svParameters, "+async_serialize", "0");
                            AppendParameter(ref svParameters, "+buildcubemaps_async", "0");
                            AppendParameter(ref svParameters, "+sv_asyncAIInit", "0");
                            AppendParameter(ref svParameters, "+sv_asyncSendSnapshot", "0");
                            AppendParameter(ref svParameters, "+sv_scriptCompileAsync", "0");
                            AppendParameter(ref svParameters, "+cl_scriptCompileAsync", "0");
                            AppendParameter(ref svParameters, "+cl_async_bone_setup", "0");
                            AppendParameter(ref svParameters, "+cl_updatedirty_async", "0");
                            AppendParameter(ref svParameters, "+mat_syncGPU", "1");
                            AppendParameter(ref svParameters, "+mat_sync_rt", "1");
                            AppendParameter(ref svParameters, "+mat_sync_rt_flushes_gpu", "1");
                            AppendParameter(ref svParameters, "+net_async_sendto", "0");
                            AppendParameter(ref svParameters, "+physics_async_sv", "0");
                            AppendParameter(ref svParameters, "+physics_async_cl", "0");
                        }

                        AppendVideoParameters(ref svParameters);

                        // MAIN ###############################################################
                        if (!string.IsNullOrEmpty(GetIniSetting(IniSettings.Command_Line, "")))
                            AppendParameter(ref svParameters, GetIniSetting(IniSettings.Command_Line, ""));

                        return svParameters;
                    }
                default:
                    return "";
            }
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