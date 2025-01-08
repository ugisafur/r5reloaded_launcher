using SoftCircuits.IniFileParser;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Animation;
using static launcher.FileManager;
using static launcher.Logger;

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
            Installed
        }

        public static void SetupApp(MainWindow mainWindow)
        {
#if DEBUG
            EnableDebugConsole();
#endif
            CreateLauncherConfig();

            Logger.Log(Logger.Type.Info, Logger.Source.Launcher, "Setting up launcher");

            ControlReferences.App = mainWindow;
            ControlReferences.dispatcher = mainWindow.Dispatcher;
            ControlReferences.progressBar = mainWindow.progressBar;
            ControlReferences.lblStatus = mainWindow.lblStatus;
            ControlReferences.lblFilesLeft = mainWindow.lblFilesLeft;
            ControlReferences.launcherVersionlbl = mainWindow.launcherVersionlbl;
            ControlReferences.cmbBranch = mainWindow.cmbBranch;
            ControlReferences.btnPlay = mainWindow.btnPlay;
            ControlReferences.settingsControl = mainWindow.SettingsControl;
            ControlReferences.subMenuControl = mainWindow.subMenuControl;
            ControlReferences.TransitionRect = mainWindow.TransitionRect;
            ControlReferences.SubMenuPopup = mainWindow.SubMenuPopup;
            ControlReferences.downloadsPopupControl = mainWindow.DownloadsPopupControl;

            ShowProgressBar(false);

            ControlReferences.launcherVersionlbl.Text = Global.launcherVersion;
            Logger.Log(Logger.Type.Info, Logger.Source.Launcher, $"Launcher Version: {Global.launcherVersion}");

            Global.launcherPath = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
            Logger.Log(Logger.Type.Info, Logger.Source.Launcher, $"Launcher path: {Global.launcherPath}");

            ControlReferences.settingsControl.SetupSettingsMenu();
            Logger.Log(Logger.Type.Info, Logger.Source.Launcher, $"Settings menu initialized");

            Global.serverConfig = DataFetcher.FetchServerConfig();

            Global.launcherConfig = FileManager.GetLauncherConfig();
            Logger.Log(Logger.Type.Info, Logger.Source.Launcher, $"Launcher config found");

            Global.isInstalled = GetIniSetting(IniSettings.Installed, false);
            Logger.Log(Logger.Type.Info, Logger.Source.Launcher, $"Is game installed: {Global.isInstalled}");

            ControlReferences.cmbBranch.ItemsSource = SetupGameBranches();
            ControlReferences.cmbBranch.SelectedIndex = 0;
            Logger.Log(Logger.Type.Info, Logger.Source.Launcher, "Game branches initialized");
        }

        public static void ToggleBackgroundVideo(bool disabled)
        {
            Logger.Log(Logger.Type.Info, Logger.Source.Launcher, $"Toggling background video: {disabled}");
            ControlReferences.App.mediaElement.Visibility = disabled ? Visibility.Hidden : Visibility.Visible;
            ControlReferences.App.mediaImage.Visibility = disabled ? Visibility.Visible : Visibility.Hidden;
        }

        public static List<ComboBranch> SetupGameBranches()
        {
            return Global.serverConfig.branches
                .Select(branch => new ComboBranch
                {
                    title = branch.branch,
                    subtext = branch.enabled ? branch.currentVersion : "branch disabled"
                })
                .ToList();
        }

        public static void LaunchGame()
        {
            Logger.Log(Logger.Type.Info, Logger.Source.Launcher, "Launching game");
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c start \"\" \"{Global.launcherPath}\\r5apex.exe\""
            };

            // Start the new process via cmd
            Process.Start(startInfo);
        }

        public static void SetInstallState(bool installing, string buttonText = "PLAY")
        {
            Logger.Log(Logger.Type.Info, Logger.Source.Launcher, $"Setting install state to: {installing}");

            ControlReferences.dispatcher.Invoke(() =>
            {
                Global.isInstalling = installing;

                ControlReferences.btnPlay.Content = buttonText;
                ControlReferences.cmbBranch.IsEnabled = !installing;
                ControlReferences.btnPlay.IsEnabled = !installing;
                ControlReferences.lblStatus.Text = "";
                ControlReferences.lblFilesLeft.Text = "";
            });

            ShowProgressBar(installing);
        }

        public static void UpdateStatusLabel(string statusText, Logger.Source source)
        {
            Logger.Log(Logger.Type.Info, source, $"Updating status label: {statusText}");
            ControlReferences.dispatcher.Invoke(() =>
            {
                ControlReferences.lblStatus.Text = statusText;
            });
        }

        private static void ShowProgressBar(bool isVisible)
        {
            ControlReferences.dispatcher.Invoke(() =>
            {
                ControlReferences.progressBar.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
                ControlReferences.lblStatus.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
                ControlReferences.lblFilesLeft.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
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
            if (SettingsGlobal.DisableTransitions)
            {
                ControlReferences.settingsControl.Visibility = Visibility.Visible;
                ControlReferences.subMenuControl.Settings.IsEnabled = false;
                return;
            }

            var transitionInStoryboard = CreateTransitionStoryboard(-2400, 0, 0.25);
            transitionInStoryboard.Completed += (s, e) =>
            {
                ControlReferences.settingsControl.Visibility = Visibility.Visible;
                var fadeInStoryboard = CreateFadeStoryboard(0, 1, 0.2);
                fadeInStoryboard.Completed += (s, e) =>
                {
                    var transitionOutStoryboard = CreateTransitionStoryboard(0, 2400, 0.25);
                    transitionOutStoryboard.Begin();
                };
                fadeInStoryboard.Begin();
            };
            transitionInStoryboard.Begin();
            ControlReferences.subMenuControl.Settings.IsEnabled = false;
        }

        public static void HideSettingsControl()
        {
            if (SettingsGlobal.DisableTransitions)
            {
                ControlReferences.settingsControl.Visibility = Visibility.Hidden;
                ControlReferences.subMenuControl.Settings.IsEnabled = true;
                return;
            }

            var transitionInStoryboard = CreateTransitionStoryboard(2400, 0, 0.25);
            transitionInStoryboard.Completed += (s, e) =>
            {
                var fadeOutStoryboard = CreateFadeStoryboard(1, 0, 0.2);
                fadeOutStoryboard.Completed += (s, e) =>
                {
                    ControlReferences.settingsControl.Visibility = Visibility.Hidden;
                    var transitionOutStoryboard = CreateTransitionStoryboard(0, -2400, 0.25);
                    transitionOutStoryboard.Begin();
                };
                fadeOutStoryboard.Begin();
            };
            transitionInStoryboard.Begin();
            ControlReferences.subMenuControl.Settings.IsEnabled = true;
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
            Storyboard.SetTarget(doubleAnimation, ControlReferences.TransitionRect);
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
            Storyboard.SetTarget(doubleAnimation, ControlReferences.settingsControl);
            Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath("Opacity"));
            storyboard.Children.Add(doubleAnimation);
            return storyboard;
        }

        private static IniFile GetIniFile()
        {
            IniFile file = new IniFile();
            file.Load(Path.Combine(Global.launcherPath, "platform\\cfg\\user\\launcherConfig.ini"));
            return file;
        }

        public static void SetIniSetting(IniSettings setting, bool value)
        {
            IniFile file = GetIniFile();
            file.SetSetting(GetSettingsSectionString(setting), GetSettingString(setting), value);
            file.Save(Path.Combine(Global.launcherPath, "platform\\cfg\\user\\launcherConfig.ini"));
            Log(Logger.Type.Info, Source.Ini, $"Setting {setting} to: {value}");
        }

        public static void SetIniSetting(IniSettings setting, int value)
        {
            IniFile file = GetIniFile();
            file.SetSetting(GetSettingsSectionString(setting), GetSettingString(setting), value);
            file.Save(Path.Combine(Global.launcherPath, "platform\\cfg\\user\\launcherConfig.ini"));
            Log(Logger.Type.Info, Source.Ini, $"Setting {setting} to: {value}");
        }

        public static void SetIniSetting(IniSettings setting, string value)
        {
            IniFile file = GetIniFile();
            file.SetSetting(GetSettingsSectionString(setting), GetSettingString(setting), value);
            file.Save(Path.Combine(Global.launcherPath, "platform\\cfg\\user\\launcherConfig.ini"));
            Log(Logger.Type.Info, Source.Ini, $"Setting {setting} to: {value}");
        }

        public static bool GetIniSetting(IniSettings setting, bool defaultValue)
        {
            IniFile file = GetIniFile();
            bool value = file.GetSetting(GetSettingsSectionString(setting), GetSettingString(setting), defaultValue);
            return value;
        }

        public static int GetIniSetting(IniSettings setting, int defaultValue)
        {
            IniFile file = GetIniFile();
            int value = file.GetSetting(GetSettingsSectionString(setting), GetSettingString(setting), defaultValue);
            return value;
        }

        public static string GetIniSetting(IniSettings setting, string defaultValue)
        {
            IniFile file = GetIniFile();
            string value = file.GetSetting(GetSettingsSectionString(setting), GetSettingString(setting), defaultValue);
            return value;
        }

        public static async Task CreateLauncherConfig()
        {
            Directory.CreateDirectory(Path.Combine(Global.launcherPath, "platform\\cfg\\user"));

            string iniPath = Path.Combine(Global.launcherPath, "platform\\cfg\\user\\launcherConfig.ini");
            if (!File.Exists(iniPath))
            {
                IniFile file = new IniFile();

                file.SetSetting("Settings_Appilication", "Enable_Quit_On_Close", true);

                file.SetSetting("Settings_Accessibility", "Disable_Background_Video", false);
                file.SetSetting("Settings_Accessibility", "Disable_Animations", false);
                file.SetSetting("Settings_Accessibility", "Disable_Transitions", false);

                file.SetSetting("Settings_Downloads", "Concurrent_Downloads", "Max");
                file.SetSetting("Settings_Downloads", "Download_Speed_Limit", -1);

                file.SetSetting("Settings_Launch_Options_Game", "Enable_Cheats", false);
                file.SetSetting("Settings_Launch_Options_Game", "Enable_Developer", false);
                file.SetSetting("Settings_Launch_Options_Game", "Show_Console", false);
                file.SetSetting("Settings_Launch_Options_Game", "Color_Console", true);
                file.SetSetting("Settings_Launch_Options_Game", "Playlists_File", "playlists_r5_patch.txt");

                file.SetSetting("Settings_Launch_Options_Main", "Mode", 0);
                file.SetSetting("Settings_Launch_Options_Main", "Visibility", 0);
                file.SetSetting("Settings_Launch_Options_Main", "HostName", "");
                file.SetSetting("Settings_Launch_Options_Main", "Command_Line", "");

                file.SetSetting("Settings_Launch_Options_Engine", "Resolution_Width", -1);
                file.SetSetting("Settings_Launch_Options_Engine", "Resolution_Height", -1);
                file.SetSetting("Settings_Launch_Options_Engine", "Reserved_Cores", -1);
                file.SetSetting("Settings_Launch_Options_Engine", "Worker_Threads", -1);
                file.SetSetting("Settings_Launch_Options_Engine", "Processor_Affinity", 0);
                file.SetSetting("Settings_Launch_Options_Engine", "No_Async", false);
                file.SetSetting("Settings_Launch_Options_Engine", "Encrypt_Packets", true);
                file.SetSetting("Settings_Launch_Options_Engine", "Queued_Packets", true);
                file.SetSetting("Settings_Launch_Options_Engine", "Random_Netkey", true);
                file.SetSetting("Settings_Launch_Options_Engine", "No_Timeout", false);
                file.SetSetting("Settings_Launch_Options_Engine", "Windowed", false);
                file.SetSetting("Settings_Launch_Options_Engine", "Borderless", false);
                file.SetSetting("Settings_Launch_Options_Engine", "Max_FPS", -1);

                file.SetSetting("Launcher", "Current_Version", "");
                file.SetSetting("Launcher", "Current_Branch", "");
                file.SetSetting("Launcher", "Installed", false);

                await file.SaveAsync(iniPath);
            }

            Log(Logger.Type.Info, Source.Ini, "Launcher config created");
        }

        private static string GetSettingsSectionString(IniSettings setting)
        {
            return setting switch
            {
                IniSettings.Enable_Quit_On_Close => "Settings_Appilication",

                IniSettings.Disable_Background_Video => "Settings_Accessibility",
                IniSettings.Disable_Animations => "Settings_Accessibility",
                IniSettings.Disable_Transitions => "Settings_Accessibility",

                IniSettings.Concurrent_Downloads => "Settings_Downloads",
                IniSettings.Download_Speed_Limit => "Settings_Downloads",

                IniSettings.Enable_Cheats => "Settings_Launch_Options_Game",
                IniSettings.Enable_Developer => "Settings_Launch_Options_Game",
                IniSettings.Show_Console => "Settings_Launch_Options_Game",
                IniSettings.Color_Console => "Settings_Launch_Options_Game",
                IniSettings.Playlists_File => "Settings_Launch_Options_Game",

                IniSettings.Mode => "Settings_Launch_Options_Main",
                IniSettings.Visibility => "Settings_Launch_Options_Main",
                IniSettings.HostName => "Settings_Launch_Options_Main",
                IniSettings.Command_Line => "Settings_Launch_Options_Main",

                IniSettings.Resolution_Width => "Settings_Launch_Options_Engine",
                IniSettings.Resolution_Height => "Settings_Launch_Options_Engine",
                IniSettings.Reserved_Cores => "Settings_Launch_Options_Engine",
                IniSettings.Worker_Threads => "Settings_Launch_Options_Engine",
                IniSettings.Processor_Affinity => "Settings_Launch_Options_Engine",
                IniSettings.No_Async => "Settings_Launch_Options_Engine",
                IniSettings.Encrypt_Packets => "Settings_Launch_Options_Engine",
                IniSettings.Queued_Packets => "Settings_Launch_Options_Engine",
                IniSettings.Random_Netkey => "Settings_Launch_Options_Engine",
                IniSettings.No_Timeout => "Settings_Launch_Options_Engine",
                IniSettings.Windowed => "Settings_Launch_Options_Engine",
                IniSettings.Borderless => "Settings_Launch_Options_Engine",
                IniSettings.Max_FPS => "Settings_Launch_Options_Engine",

                IniSettings.Current_Version => "Launcher",
                IniSettings.Current_Branch => "Launcher",
                IniSettings.Installed => "Launcher",
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
                IniSettings.Enable_Cheats => "Enable_Cheats",
                IniSettings.Enable_Developer => "Enable_Developer",
                IniSettings.Show_Console => "Show_Console",
                IniSettings.Color_Console => "Color_Console",
                IniSettings.Playlists_File => "Playlists_File",
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
                _ => throw new NotImplementedException()
            };
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