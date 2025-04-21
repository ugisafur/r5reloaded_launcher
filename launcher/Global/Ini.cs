using SoftCircuits.IniFileParser;
using System.IO;
using static launcher.Global.Logger;

namespace launcher.Global
{
    public static class Ini
    {
        //Add new settings to save to this enum
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
            Ask_For_Tour,
            Updater_Version,
            Nightly_Builds,
            Launcher_Version,
            Upload_Crashes,
            Server_Video_Name,
            Auto_Launch_EA_App,
            Enable_Discord_Rich_Presence,
        }

        // Maps the settings to their respective sections in the INI file
        public static Dictionary<Vars, string> VarSections = new()
        {
            { Vars.Enable_Quit_On_Close, "Settings" },
            { Vars.Enable_Notifications, "Settings" },
            { Vars.Disable_Background_Video, "Settings" },
            { Vars.Disable_Animations, "Settings" },
            { Vars.Disable_Transitions, "Settings" },
            { Vars.Concurrent_Downloads, "Settings" },
            { Vars.Download_Speed_Limit, "Settings" },
            { Vars.Library_Location, "Settings" },
            { Vars.Keep_All_Logs, "Settings" },
            { Vars.Stream_Video, "Settings" },
            { Vars.Upload_Crashes, "Settings" },
            { Vars.Auto_Launch_EA_App, "Settings" },

            { Vars.Enable_Cheats, "Advanced_Options" },
            { Vars.Enable_Developer, "Advanced_Options" },
            { Vars.Show_Console, "Advanced_Options" },
            { Vars.Color_Console, "Advanced_Options" },
            { Vars.Playlists_File, "Advanced_Options" },
            { Vars.Map, "Advanced_Options" },
            { Vars.Playlist, "Advanced_Options" },
            { Vars.Mode, "Advanced_Options" },
            { Vars.Visibility, "Advanced_Options" },
            { Vars.HostName, "Advanced_Options" },
            { Vars.Command_Line, "Advanced_Options" },
            { Vars.Resolution_Width, "Advanced_Options" },
            { Vars.Resolution_Height, "Advanced_Options" },
            { Vars.Reserved_Cores, "Advanced_Options" },
            { Vars.Worker_Threads, "Advanced_Options" },
            { Vars.Processor_Affinity, "Advanced_Options" },
            { Vars.No_Async, "Advanced_Options" },
            { Vars.Encrypt_Packets, "Advanced_Options" },
            { Vars.Queued_Packets, "Advanced_Options" },
            { Vars.Random_Netkey, "Advanced_Options" },
            { Vars.No_Timeout, "Advanced_Options" },
            { Vars.Windowed, "Advanced_Options" },
            { Vars.Borderless, "Advanced_Options" },
            { Vars.Max_FPS, "Advanced_Options" },
            { Vars.Offline_Mode, "Advanced_Options" },

            { Vars.SelectedBranch, "Launcher" },
            { Vars.Ask_For_Tour, "Launcher" },
            { Vars.Updater_Version, "Launcher" },
            { Vars.Nightly_Builds, "Launcher" },
            { Vars.Launcher_Version, "Launcher" },
            { Vars.Server_Video_Name, "Launcher" },
            { Vars.Enable_Discord_Rich_Presence, "Launcher" },
        };

        // Default values for each setting
        public static Dictionary<Vars, object> VarDefaults = new()
        {
            { Vars.Library_Location, "" },
            { Vars.Playlists_File, "playlists_r5_patch.txt" },
            { Vars.HostName, "" },
            { Vars.Command_Line, "" },
            { Vars.Resolution_Width, "" },
            { Vars.Resolution_Height, "" },
            { Vars.Reserved_Cores, "-1" },
            { Vars.Worker_Threads, "-1" },
            { Vars.Processor_Affinity, "0" },
            { Vars.Max_FPS, "0" },
            { Vars.SelectedBranch, "" },
            { Vars.Enable_Quit_On_Close, "" },
            { Vars.Updater_Version, "" },
            { Vars.Launcher_Version, "" },
            { Vars.Server_Video_Name, "" },

            { Vars.Keep_All_Logs, true },
            { Vars.Enable_Notifications, true },
            { Vars.Disable_Background_Video, false },
            { Vars.Disable_Animations, false },
            { Vars.Disable_Transitions, false },
            { Vars.Enable_Cheats, false },
            { Vars.Enable_Developer, false },
            { Vars.Show_Console, false },
            { Vars.Color_Console, true },
            { Vars.No_Async, false },
            { Vars.Encrypt_Packets, true },
            { Vars.Queued_Packets, true },
            { Vars.Random_Netkey, true },
            { Vars.No_Timeout, false },
            { Vars.Windowed, false },
            { Vars.Borderless, false },
            { Vars.Offline_Mode, false },
            { Vars.Stream_Video, true },
            { Vars.Ask_For_Tour, true },
            { Vars.Nightly_Builds, false },
            { Vars.Upload_Crashes, true },
            { Vars.Auto_Launch_EA_App, true },
            { Vars.Enable_Discord_Rich_Presence, true },

            { Vars.Mode, 0 },
            { Vars.Visibility, 0 },
            { Vars.Concurrent_Downloads, 50 },
            { Vars.Download_Speed_Limit, 0 },
            { Vars.Map, 0 },
            { Vars.Playlist, 0 },
        };

        public static void CreateConfig()
        {
            Directory.CreateDirectory(Path.Combine(Launcher.PATH, "launcher_data\\cfg\\"));

            string iniPath = Path.Combine(Launcher.PATH, "launcher_data\\cfg\\launcherConfig.ini");

            if (!File.Exists(iniPath))
            {
                IniFile file = new();

                foreach (Vars setting in Enum.GetValues(typeof(Vars)))
                {
                    string settings_name = Enum.GetName(typeof(Vars), setting);

                    switch (VarDefaults[setting])
                    {
                        case string s:
                            file.SetSetting(VarSections[setting], settings_name, s);
                            break;

                        case bool b:
                            file.SetSetting(VarSections[setting], settings_name, b);
                            break;

                        case int i:
                            file.SetSetting(VarSections[setting], settings_name, i);
                            break;

                        default:
                            file.SetSetting(VarSections[setting], settings_name, (string)VarDefaults[setting]);
                            break;
                    }
                }

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
                    string settings_name = Enum.GetName(typeof(Vars), setting);

                    if (settings.Concat(advanced).Concat(launcher).Any(x => x.Name == settings_name))
                        continue;

                    switch (VarDefaults[setting])
                    {
                        case string s:
                            file.SetSetting(VarSections[setting], settings_name, s);
                            break;

                        case bool b:
                            file.SetSetting(VarSections[setting], settings_name, b);
                            break;

                        case int i:
                            file.SetSetting(VarSections[setting], settings_name, i);
                            break;

                        default:
                            file.SetSetting(VarSections[setting], settings_name, (string)VarDefaults[setting]);
                            break;
                    }

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

        public static void Set(Vars setting, object value)
        {
            if (!Exists())
                return;

            IniFile file = GetConfig();
            string settingsName = Enum.GetName(typeof(Vars), setting);
            string section = VarSections[setting];

            switch (value)
            {
                case string s:
                    file.SetSetting(section, settingsName, s);
                    break;

                case bool b:
                    file.SetSetting(section, settingsName, b);
                    break;

                case int i:
                    file.SetSetting(section, settingsName, i);
                    break;

                default:
                    file.SetSetting(section, settingsName, (string)value);
                    break;
            }

            file.Save(Path.Combine(Launcher.PATH, "launcher_data\\cfg\\launcherConfig.ini"));
            LogInfo(Source.Ini, $"Setting {setting} to: {value}");
        }

        public static void Set(string section, string setting, object value)
        {
            if (!Exists())
                return;

            IniFile file = GetConfig();

            switch (value)
            {
                case string s:
                    file.SetSetting(section, setting, s);
                    break;

                case bool b:
                    file.SetSetting(section, setting, b);
                    break;

                case int i:
                    file.SetSetting(section, setting, i);
                    break;

                default:
                    file.SetSetting(section, setting, (string)value);
                    break;
            }

            file.Save(Path.Combine(Launcher.PATH, "launcher_data\\cfg\\launcherConfig.ini"));
            LogInfo(Source.Ini, $"Setting {setting} to: {value}");
        }

        public static object Get(string section, string setting, object defaultValue)
        {
            if (!Exists())
                return defaultValue;

            IniFile file = GetConfig();

            return defaultValue switch
            {
                string s => file.GetSetting(section, setting, s),
                bool b => file.GetSetting(section, setting, b),
                int i => file.GetSetting(section, setting, i),
                _ => defaultValue
            };
        }

        public static object Get(Vars setting)
        {
            if (!Exists())
                return VarDefaults[setting];

            IniFile file = GetConfig();
            object defaultValue = VarDefaults[setting];
            string settingsName = Enum.GetName(typeof(Vars), setting);

            return defaultValue switch
            {
                string s => file.GetSetting(VarSections[setting], settingsName, s),
                bool b => file.GetSetting(VarSections[setting], settingsName, b),
                int i => file.GetSetting(VarSections[setting], settingsName, i),
                _ => defaultValue
            };
        }
    }
}