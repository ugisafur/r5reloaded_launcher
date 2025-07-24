using DiscordRPC;
using launcher.Core.Models;
using launcher.Services;
using SoftCircuits.IniFileParser;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Numerics;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using static launcher.Core.UiReferences;
using static launcher.Services.LoggerService;

namespace launcher
{
    public static class Launcher
    {
        public const string VERSION = "1.2.0";
        public static bool IsOnline { get; set; } = false;
        public static bool isLocal { get; set; } = false;
        public static bool IsInstalling { get; set; } = false;
        public static bool UpdateCheckLoop { get; set; } = false;
        public static bool BadFilesDetected { get; set; } = false;
        public static bool InSettingsMenu { get; set; } = false;
        public static bool InAdvancedMenu { get; set; } = false;
        public static bool OnBoarding { get; set; } = false;
        public static bool BlockLanguageInstall { get; set; } = false;
        public static int FilesLeft { get; set; } = 0;
        public static bool DebugArg { get; set; } = false;

        #region Public Keys

        public const string NEWSKEY = "68767b4df970e8348b79ad74b1";
        public const string DISCORDRPC_CLIENT_ID = "1364049087434850444";

        #endregion Public Keys

        #region Public URLs

        public const string CONFIG_URL = "https://cdn.r5r.org/launcher/config.json";
        public const string GITHUB_API_URL = "https://api.github.com/repos/AyeZeeBB/r5reloaded_launcher/releases";
        public const string BACKGROUND_VIDEO_URL = "https://cdn.r5r.org/launcher/video_backgrounds/";
        public const string NEWSURL = "https://admin.r5reloaded.com/ghost/api/content";

        #endregion Public URLs

        #region Settings

        public const int MAX_REPAIR_ATTEMPTS = 5;
        public static string PATH { get; set; } = "";
        public static RemoteConfig RemoteConfig { get; set; }
        public static IniFile LauncherConfig { get; set; }
        public static CultureInfo cultureInfo { get; set; }
        public static string language_name { get; set; }
        public static bool wineEnv { get; set; }
        public static bool newsOnline { get; set; }

        #endregion Settings

        public static void Init()
        {
            string version = (bool)SettingsService.Get(SettingsService.Vars.Nightly_Builds) ? (string)SettingsService.Get(SettingsService.Vars.Launcher_Version) : VERSION;
            appDispatcher.Invoke(() => Version_Label.Text = version);

            LogInfo(LogSource.Launcher, $"Launcher Version: {version}");

            PATH = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
            LogInfo(LogSource.Launcher, $"Launcher path: {PATH}");

            RemoteConfig = IsOnline ? ApiService.GetRemoteConfig() : null;

            SettingsService.Load();

            LauncherConfig = SettingsService.IniFile;
            LogInfo(LogSource.Launcher, $"Launcher config found");

            cultureInfo = CultureInfo.CurrentCulture;
            language_name = cultureInfo.Parent.EnglishName.ToLower(new CultureInfo("en-US"));

            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }
    }
}