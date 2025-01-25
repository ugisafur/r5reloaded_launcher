using SoftCircuits.IniFileParser;
using System.IO;
using static launcher.Classes.Utilities.Logger;
using launcher.Classes.Global;

namespace launcher.Classes.Utilities
{
    public static class Ini
    {
        public enum Vars
        {
            Enable_Quit_On_Close,
            Enable_Notifications,
            Disable_Background_Video,
            Disable_Animations,
            Disable_Transitions,
            Concurrent_Downloads,
            Download_Speed_Limit,
            Library_Location,
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
            Map,
            Playlist,
            SelectedBranch,
            Offline_Mode,
            Keep_All_Logs,
            Stream_Video,
            Ask_For_Tour
        }

        public static void CreateConfig()
        {
            Directory.CreateDirectory(Path.Combine(Launcher.PATH, "launcher_data\\cfg\\"));

            string iniPath = Path.Combine(Launcher.PATH, "launcher_data\\cfg\\launcherConfig.ini");
            if (!File.Exists(iniPath))
            {
                IniFile file = new();

                file.SetSetting("Settings", "Keep_All_Logs", true);
                file.SetSetting("Settings", "Enable_Quit_On_Close", "");
                file.SetSetting("Settings", "Enable_Notifications", true);
                file.SetSetting("Settings", "Disable_Background_Video", false);
                file.SetSetting("Settings", "Disable_Animations", false);
                file.SetSetting("Settings", "Disable_Transitions", false);
                file.SetSetting("Settings", "Concurrent_Downloads", 100);
                file.SetSetting("Settings", "Download_Speed_Limit", 0);
                file.SetSetting("Settings", "Library_Location", "");
                file.SetSetting("Settings", "Stream_Video", true);

                file.SetSetting("Advanced_Options", "Enable_Cheats", false);
                file.SetSetting("Advanced_Options", "Enable_Developer", false);
                file.SetSetting("Advanced_Options", "Show_Console", false);
                file.SetSetting("Advanced_Options", "Color_Console", true);
                file.SetSetting("Advanced_Options", "Playlists_File", "playlists_r5_patch.txt");
                file.SetSetting("Advanced_Options", "Map", 0);
                file.SetSetting("Advanced_Options", "Playlist", 0);
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
                file.SetSetting("Advanced_Options", "Max_FPS", "0");
                file.SetSetting("Advanced_Options", "Offline_Mode", false);

                file.SetSetting("Launcher", "SelectedBranch", "");
                file.SetSetting("Launcher", "Ask_For_Tour", true);

                file.Save(iniPath);
            }
            else
            {
                // Check for new settings that may have been added by an update
                IniFile file = new();
                file.Load(Path.Combine(Launcher.PATH, "launcher_data\\cfg\\launcherConfig.ini"));

                IEnumerable<IniSetting> settings = file.GetSectionSettings("Settings");
                IEnumerable<IniSetting> advanced = file.GetSectionSettings("Advanced_Options");
                IEnumerable<IniSetting> launcher = file.GetSectionSettings("Launcher");

                foreach (Vars setting in Enum.GetValues(typeof(Vars)))
                {
                    if (settings.Any(x => x.Name == GetString(setting)))
                        continue;

                    if (advanced.Any(x => x.Name == GetString(setting)))
                        continue;

                    if (launcher.Any(x => x.Name == GetString(setting)))
                        continue;

                    if (GetDefaultValue(setting) is bool v)
                        file.SetSetting(GetSectionString(setting), GetString(setting), v);

                    if (GetDefaultValue(setting) is string v1)
                        file.SetSetting(GetSectionString(setting), GetString(setting), v1);

                    if (GetDefaultValue(setting) is int v2)
                        file.SetSetting(GetSectionString(setting), GetString(setting), v2);

                    file.Save(Path.Combine(Launcher.PATH, "launcher_data\\cfg\\launcherConfig.ini"));
                }
            }
        }

        public static IniFile GetConfig()
        {
            IniFile file = new();
            file.Load(Path.Combine(Launcher.PATH, "launcher_data\\cfg\\launcherConfig.ini"));
            return file;
        }

        public static bool Exists()
        {
            return File.Exists(Path.Combine(Launcher.PATH, "launcher_data\\cfg\\launcherConfig.ini"));
        }

        public static void Set(Vars setting, bool value)
        {
            if (!Exists())
                return;

            IniFile file = GetConfig();
            file.SetSetting(GetSectionString(setting), GetString(setting), value);
            file.Save(Path.Combine(Launcher.PATH, "launcher_data\\cfg\\launcherConfig.ini"));
            LogInfo(Source.Ini, $"Setting {setting} to: {value}");
        }

        public static void Set(Vars setting, int value)
        {
            if (!Exists())
                return;

            IniFile file = GetConfig();
            file.SetSetting(GetSectionString(setting), GetString(setting), value);
            file.Save(Path.Combine(Launcher.PATH, "launcher_data\\cfg\\launcherConfig.ini"));
            LogInfo(Source.Ini, $"Setting {setting} to: {value}");
        }

        public static void Set(Vars setting, string value)
        {
            if (!Exists())
                return;

            IniFile file = GetConfig();
            file.SetSetting(GetSectionString(setting), GetString(setting), value);
            file.Save(Path.Combine(Launcher.PATH, "launcher_data\\cfg\\launcherConfig.ini"));
            LogInfo(Source.Ini, $"Setting {setting} to: {value}");
        }

        public static void Set(string section, string setting, string value)
        {
            if (!Exists())
                return;

            IniFile file = GetConfig();
            file.SetSetting(section, setting, value);
            file.Save(Path.Combine(Launcher.PATH, "launcher_data\\cfg\\launcherConfig.ini"));
            LogInfo(Source.Ini, $"Setting {setting} to: {value}");
        }

        public static void Set(string section, string setting, bool value)
        {
            if (!Exists())
                return;

            IniFile file = GetConfig();
            file.SetSetting(section, setting, value);
            file.Save(Path.Combine(Launcher.PATH, "launcher_data\\cfg\\launcherConfig.ini"));
            LogInfo(Source.Ini, $"Setting {setting} to: {value}");
        }

        public static bool Get(string section, string setting, bool defaultValue)
        {
            if (!Exists())
                return defaultValue;

            IniFile file = GetConfig();
            bool value = file.GetSetting(section, setting, defaultValue);
            return value;
        }

        public static string Get(string section, string setting, string defaultValue)
        {
            if (!Exists())
                return defaultValue;

            IniFile file = GetConfig();
            string value = file.GetSetting(section, setting, defaultValue);
            return value;
        }

        public static object Get(Vars setting)
        {
            if (!Exists())
                return GetDefaultValue(setting);

            IniFile file = GetConfig();
            object value = GetDefaultValue(setting);

            if (value is string v)
                value = file.GetSetting(GetSectionString(setting), GetString(setting), v);
            else if (value is bool v1)
                value = file.GetSetting(GetSectionString(setting), GetString(setting), v1);
            else if (value is int v2)
                value = file.GetSetting(GetSectionString(setting), GetString(setting), v2);

            return value;
        }

        public static object GetDefaultValue(Vars setting)
        {
            return setting switch
            {
                Vars.Library_Location => "",
                Vars.Playlists_File => "playlists_r5_patch.txt",
                Vars.HostName => "",
                Vars.Command_Line => "",
                Vars.Resolution_Width => "",
                Vars.Resolution_Height => "",
                Vars.Reserved_Cores => "-1",
                Vars.Worker_Threads => "-1",
                Vars.Processor_Affinity => "0",
                Vars.Max_FPS => "0",
                Vars.SelectedBranch => "",
                Vars.Enable_Quit_On_Close => "",

                Vars.Keep_All_Logs => true,
                Vars.Enable_Notifications => true,
                Vars.Disable_Background_Video => false,
                Vars.Disable_Animations => false,
                Vars.Disable_Transitions => false,
                Vars.Enable_Cheats => false,
                Vars.Enable_Developer => false,
                Vars.Show_Console => false,
                Vars.Color_Console => true,
                Vars.No_Async => false,
                Vars.Encrypt_Packets => true,
                Vars.Queued_Packets => true,
                Vars.Random_Netkey => true,
                Vars.No_Timeout => false,
                Vars.Windowed => false,
                Vars.Borderless => false,
                Vars.Offline_Mode => false,
                Vars.Stream_Video => true,
                Vars.Ask_For_Tour => true,

                Vars.Mode => 0,
                Vars.Visibility => 0,
                Vars.Concurrent_Downloads => 100,
                Vars.Download_Speed_Limit => 0,
                Vars.Map => 0,
                Vars.Playlist => 0,

                _ => throw new NotImplementedException($"Default value for {setting} is not implemented.")
            };
        }

        public static string GetSectionString(Vars setting)
        {
            return setting switch
            {
                Vars.Enable_Quit_On_Close => "Settings",
                Vars.Enable_Notifications => "Settings",
                Vars.Disable_Background_Video => "Settings",
                Vars.Disable_Animations => "Settings",
                Vars.Disable_Transitions => "Settings",
                Vars.Concurrent_Downloads => "Settings",
                Vars.Download_Speed_Limit => "Settings",
                Vars.Library_Location => "Settings",
                Vars.Keep_All_Logs => "Settings",
                Vars.Stream_Video => "Settings",

                Vars.Enable_Cheats => "Advanced_Options",
                Vars.Enable_Developer => "Advanced_Options",
                Vars.Show_Console => "Advanced_Options",
                Vars.Color_Console => "Advanced_Options",
                Vars.Playlists_File => "Advanced_Options",
                Vars.Map => "Advanced_Options",
                Vars.Playlist => "Advanced_Options",
                Vars.Mode => "Advanced_Options",
                Vars.Visibility => "Advanced_Options",
                Vars.HostName => "Advanced_Options",
                Vars.Command_Line => "Advanced_Options",
                Vars.Resolution_Width => "Advanced_Options",
                Vars.Resolution_Height => "Advanced_Options",
                Vars.Reserved_Cores => "Advanced_Options",
                Vars.Worker_Threads => "Advanced_Options",
                Vars.Processor_Affinity => "Advanced_Options",
                Vars.No_Async => "Advanced_Options",
                Vars.Encrypt_Packets => "Advanced_Options",
                Vars.Queued_Packets => "Advanced_Options",
                Vars.Random_Netkey => "Advanced_Options",
                Vars.No_Timeout => "Advanced_Options",
                Vars.Windowed => "Advanced_Options",
                Vars.Borderless => "Advanced_Options",
                Vars.Max_FPS => "Advanced_Options",
                Vars.Offline_Mode => "Advanced_Options",

                Vars.SelectedBranch => "Launcher",
                Vars.Ask_For_Tour => "Launcher",
                _ => throw new NotImplementedException()
            };
        }

        public static string GetString(Vars setting)
        {
            return setting switch
            {
                Vars.Enable_Quit_On_Close => "Enable_Quit_On_Close",
                Vars.Enable_Notifications => "Enable_Notifications",
                Vars.Disable_Background_Video => "Disable_Background_Video",
                Vars.Disable_Animations => "Disable_Animations",
                Vars.Disable_Transitions => "Disable_Transitions",
                Vars.Concurrent_Downloads => "Concurrent_Downloads",
                Vars.Download_Speed_Limit => "Download_Speed_Limit",
                Vars.Enable_Cheats => "Enable_Cheats",
                Vars.Enable_Developer => "Enable_Developer",
                Vars.Show_Console => "Show_Console",
                Vars.Color_Console => "Color_Console",
                Vars.Playlists_File => "Playlists_File",
                Vars.Map => "Map",
                Vars.Playlist => "Playlist",
                Vars.Mode => "Mode",
                Vars.Visibility => "Visibility",
                Vars.HostName => "HostName",
                Vars.Command_Line => "Command_Line",
                Vars.Resolution_Width => "Resolution_Width",
                Vars.Resolution_Height => "Resolution_Height",
                Vars.Reserved_Cores => "Reserved_Cores",
                Vars.Worker_Threads => "Worker_Threads",
                Vars.Processor_Affinity => "Processor_Affinity",
                Vars.No_Async => "No_Async",
                Vars.Encrypt_Packets => "Encrypt_Packets",
                Vars.Queued_Packets => "Queued_Packets",
                Vars.Random_Netkey => "Random_Netkey",
                Vars.No_Timeout => "No_Timeout",
                Vars.Windowed => "Windowed",
                Vars.Borderless => "Borderless",
                Vars.Max_FPS => "Max_FPS",
                Vars.SelectedBranch => "SelectedBranch",
                Vars.Library_Location => "Library_Location",
                Vars.Offline_Mode => "Offline_Mode",
                Vars.Keep_All_Logs => "Keep_All_Logs",
                Vars.Stream_Video => "Stream_Video",
                Vars.Ask_For_Tour => "Ask_For_Tour",
                _ => throw new NotImplementedException()
            };
        }
    }
}