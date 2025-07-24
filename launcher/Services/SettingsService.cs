using launcher.Services.Models;
using SoftCircuits.IniFileParser;
using System.IO;
using static launcher.Services.LoggerService;

namespace launcher.Services
{
    /// <summary>
    /// Manages application settings stored in an INI file.
    /// </summary>
    public static class SettingsService
    {
        private static readonly string IniPath = Path.Combine(Launcher.PATH, "launcher_data", "cfg", "launcherConfig.ini");
        public static readonly IniFile IniFile = new();

        /// <summary>
        /// Represents the settings variables.
        /// </summary>
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
            Server_Video_Name,
            Auto_Launch_EA_App,
            Enable_Discord_Rich_Presence,
        }

        private static readonly Dictionary<Vars, SettingInfo> SettingsMap = new()
        {
            { Vars.Enable_Quit_On_Close, new("Settings", "") },
            { Vars.Enable_Notifications, new("Settings", true) },
            { Vars.Disable_Background_Video, new("Settings", false) },
            { Vars.Disable_Animations, new("Settings", false) },
            { Vars.Disable_Transitions, new("Settings", false) },
            { Vars.Concurrent_Downloads, new("Settings", 50) },
            { Vars.Download_Speed_Limit, new("Settings", 0) },
            { Vars.Library_Location, new("Settings", "") },
            { Vars.Keep_All_Logs, new("Settings", true) },
            { Vars.Stream_Video, new("Settings", true) },
            { Vars.Auto_Launch_EA_App, new("Settings", true) },
            { Vars.Enable_Cheats, new("Advanced_Options", false) },
            { Vars.Enable_Developer, new("Advanced_Options", false) },
            { Vars.Show_Console, new("Advanced_Options", false) },
            { Vars.Color_Console, new("Advanced_Options", true) },
            { Vars.Playlists_File, new("Advanced_Options", "playlists_r5_patch.txt") },
            { Vars.Map, new("Advanced_Options", 0) },
            { Vars.Playlist, new("Advanced_Options", 0) },
            { Vars.Mode, new("Advanced_Options", 0) },
            { Vars.Visibility, new("Advanced_Options", 0) },
            { Vars.HostName, new("Advanced_Options", "") },
            { Vars.Command_Line, new("Advanced_Options", "") },
            { Vars.Resolution_Width, new("Advanced_Options", "") },
            { Vars.Resolution_Height, new("Advanced_Options", "") },
            { Vars.Reserved_Cores, new("Advanced_Options", "-1") },
            { Vars.Worker_Threads, new("Advanced_Options", "-1") },
            { Vars.Processor_Affinity, new("Advanced_Options", "0") },
            { Vars.No_Async, new("Advanced_Options", false) },
            { Vars.Encrypt_Packets, new("Advanced_Options", true) },
            { Vars.Queued_Packets, new("Advanced_Options", true) },
            { Vars.Random_Netkey, new("Advanced_Options", true) },
            { Vars.No_Timeout, new("Advanced_Options", false) },
            { Vars.Windowed, new("Advanced_Options", false) },
            { Vars.Borderless, new("Advanced_Options", false) },
            { Vars.Max_FPS, new("Advanced_Options", "0") },
            { Vars.Offline_Mode, new("Advanced_Options", false) },
            { Vars.SelectedBranch, new("Launcher", "") },
            { Vars.Ask_For_Tour, new("Launcher", true) },
            { Vars.Updater_Version, new("Launcher", "") },
            { Vars.Nightly_Builds, new("Launcher", false) },
            { Vars.Launcher_Version, new("Launcher", "") },
            { Vars.Server_Video_Name, new("Launcher", "") },
            { Vars.Enable_Discord_Rich_Presence, new("Launcher", true) },
        };

        public static void Load()
        {
            if (File.Exists(IniPath))
            {
                IniFile.Load(IniPath);
                UpdateConfigWithNewSettings();
            }
            else
            {
                CreateDefaultConfig();
            }
        }

        public static void CreateDefaultConfig()
        {
            if (File.Exists(IniPath))
                return;

            Directory.CreateDirectory(Path.GetDirectoryName(IniPath));
            foreach (var (key, info) in SettingsMap)
            {
                Set(key, info.DefaultValue, save: false);
            }
            IniFile.Save(IniPath);
        }

        private static void UpdateConfigWithNewSettings()
        {
            var existingSettings = IniFile.GetSectionSettings("Settings")
                .Concat(IniFile.GetSectionSettings("Advanced_Options"))
                .Concat(IniFile.GetSectionSettings("Launcher"))
                .Select(s => s.Name)
                .ToHashSet();

            bool wasModified = false;
            foreach (var (key, info) in SettingsMap)
            {
                var settingName = Enum.GetName(typeof(Vars), key);
                if (!existingSettings.Contains(settingName))
                {
                    Set(key, info.DefaultValue, save: false);
                    wasModified = true;
                }
            }

            if (wasModified)
            {
                IniFile.Save(IniPath);
            }
        }

        public static bool Exists() => File.Exists(IniPath);

        /// <summary>
        /// Sets a setting value.
        /// </summary>
        public static void Set(Vars setting, object value, bool save = true)
        {
            var info = SettingsMap[setting];
            var settingName = Enum.GetName(typeof(Vars), setting);
            Set(info.Section, settingName, value, save);
        }

        /// <summary>
        /// Sets a setting value in a specific section.
        /// </summary>
        public static void Set(string section, string setting, object value, bool save = true)
        {
            switch (value)
            {
                case string s:
                    IniFile.SetSetting(section, setting, s);
                    break;
                case bool b:
                    IniFile.SetSetting(section, setting, b);
                    break;
                case int i:
                    IniFile.SetSetting(section, setting, i);
                    break;
                default:
                    IniFile.SetSetting(section, setting, value.ToString());
                    break;
            }

            if (save)
            {
                IniFile.Save(IniPath);
            }
            LogInfo(LogSource.Ini, $"Setting {setting} to: {value}");
        }

        /// <summary>
        /// Gets a setting value.
        /// </summary>
        public static object Get(Vars setting)
        {
            var info = SettingsMap[setting];
            var settingName = Enum.GetName(typeof(Vars), setting);
            return Get(info.Section, settingName, info.DefaultValue);
        }

        /// <summary>
        /// Gets a setting value from a specific section.
        /// </summary>
        public static object Get(string section, string setting, object defaultValue)
        {
            if (!Exists())
                return defaultValue;

            return defaultValue switch
            {
                string s => IniFile.GetSetting(section, setting, s),
                bool b => IniFile.GetSetting(section, setting, b),
                int i => IniFile.GetSetting(section, setting, i),
                _ => defaultValue,
            };
        }
    }
}