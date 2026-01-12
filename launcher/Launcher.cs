using launcher.Services;
using System.Globalization;
using System.IO;
using System.Net;
using static launcher.Core.AppContext;
using static launcher.Services.LoggerService;

namespace launcher
{
    public static class Launcher
    {
        public const string VERSION = "1.6.2";

        #region Settings

        public const int MAX_REPAIR_ATTEMPTS = 5;
        public static string PATH { get; set; } = "";

        #endregion Settings

        #region Public Keys

        public const string NEWSKEY = "75d19830bc19c339c69eff0c51";
        public const string DISCORDRPC_CLIENT_ID = "1364049087434850444";

        #endregion Public Keys

        #region Public URLs

        public const string CONFIG_URL = "https://cdn.r5r.org/launcher/config.json";
        public const string MS_URL = "https://r5r.org";
        public const string WEBSITE_URL = "https://r5reloaded.com";
        public const string LAUNCHER_THEME_URL = "https://cdn.r5r.org/launcher/theme.xaml";
        public const string CDN_URL = "https://cdn.r5r.org/launcher/config.json"; // only used for displaying the CDN UP or DOWN status in the status window, can be sorta useful if in the future I get somesort of url switching for when the CDN fails for whatever reason
        // public const string GITHUB_API_URL = "https://api.github.com/repos/AyeZeeBB/r5reloaded_launcher/releases";
        public const string BACKGROUND_VIDEO_URL = "https://cdn.r5r.org/launcher/video_backgrounds/"; // some of default videos available here https://github.com/AyeZeeBB/r5reloaded_launcher/tree/main/launcher/assets
        public const string NEWSURL = "https://admin.r5reloaded.com/ghost/api/content";

        #endregion Public URLs

        public static void Init()
        {
            //string version = (bool)SettingsService.Get(SettingsService.Vars.Nightly_Builds) ? (string)SettingsService.Get(SettingsService.Vars.Launcher_Version) : VERSION;
            appDispatcher.Invoke(() => Version_Label.Text = VERSION);

            LogInfo(LogSource.Launcher, $"Launcher Version: {VERSION}");

            PATH = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
            LogInfo(LogSource.Launcher, $"Launcher path: {PATH}");

            appState.RemoteConfig = appState.IsOnline ? ApiService.GetRemoteConfig() : null;

            SettingsService.Load();

            appState.LauncherConfig = SettingsService.IniFile;
            LogInfo(LogSource.Launcher, $"Launcher config found");

            appState.cultureInfo = CultureInfo.CurrentCulture;
            appState.language_name = appState.cultureInfo.Parent.EnglishName.ToLower(new CultureInfo("en-US"));

            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }
    }
}