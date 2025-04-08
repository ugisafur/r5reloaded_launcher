using launcher.CDN;
using SoftCircuits.IniFileParser;
using System.Globalization;
using static launcher.Global.Logger;
using static launcher.Global.References;
using launcher.Game;
using System.IO;
using System.Numerics;
using System.Windows;
using System.Net.Http;

namespace launcher.Global
{
    public static class Launcher
    {
        public const string VERSION = "0.9.9.6";

        #region Public Keys

        public const string NEWSKEY = "68767b4df970e8348b79ad74b1";

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
        public static ServerConfig ServerConfig { get; set; }
        public static IniFile LauncherConfig { get; set; }
        public static CultureInfo cultureInfo { get; set; }
        public static string language_name { get; set; }
        public static bool wineEnv { get; set; }
        public static bool newsOnline { get; set; }

        #endregion Settings

        public static void Init()
        {
            string version = (bool)Ini.Get(Ini.Vars.Nightly_Builds) ? (string)Ini.Get(Ini.Vars.Launcher_Version) : Launcher.VERSION;
            appDispatcher.Invoke(() => Version_Label.Text = version);

            LogInfo(Source.Launcher, $"Launcher Version: {version}");

            Launcher.PATH = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
            LogInfo(Source.Launcher, $"Launcher path: {Launcher.PATH}");

            ServerConfig = AppState.IsOnline ? Fetch.Config() : null;

            LauncherConfig = Ini.GetConfig();
            LogInfo(Source.Launcher, $"Launcher config found");

            cultureInfo = CultureInfo.CurrentCulture;
            language_name = cultureInfo.Parent.EnglishName.ToLower(new CultureInfo("en-US"));
        }
    }

    public static class AppState
    {
        public static bool IsOnline { get; set; } = false;
        public static bool IsLocalBranch { get; set; } = false;
        public static bool IsInstalling { get; set; } = false;
        public static bool UpdateCheckLoop { get; set; } = false;
        public static bool BadFilesDetected { get; set; } = false;
        public static bool InSettingsMenu { get; set; } = false;
        public static bool InAdvancedMenu { get; set; } = false;
        public static bool OnBoarding { get; set; } = false;
        public static bool BlockLanguageInstall { get; set; } = false;
        public static int FilesLeft { get; set; } = 0;
    }

    public static class Networking
    {
        public static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(30) };

        public static SemaphoreSlim DownloadSemaphore = new(100);

        public static async Task<bool> CDNTest()
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5); // Set a timeout (e.g., 5 seconds)

                var response = await client.GetAsync($"https://cdn.r5r.org/launcher/config.json");
                return response.IsSuccessStatusCode; // Return true if the request was successful
            }
            catch
            {
                return false; // Return false if there's an exception (e.g., timeout or network error)
            }
        }

        public static async Task<bool> NewsTestAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5); // Set a timeout (e.g., 5 seconds)

                var response = await client.GetAsync($"{Launcher.NEWSURL}/posts/?key={Launcher.NEWSKEY}&include=tags,authors");
                return response.IsSuccessStatusCode; // Return true if the request was successful
            }
            catch
            {
                return false; // Return false if there's an exception (e.g., timeout or network error)
            }
        }

        public static async Task<bool> MasterServerTest()
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5); // Set a timeout (e.g., 5 seconds)

                var response = await client.GetAsync($"https://r5r.org");
                return response.IsSuccessStatusCode; // Return true if the request was successful
            }
            catch
            {
                return false; // Return false if there's an exception (e.g., timeout or network error)
            }
        }
    }

    public static class DataCollections
    {
        public static List<string> BadFiles { get; } = [];
        public static List<Branch> FolderBranches { get; } = [];

        public static List<OnBoardingItem> OnBoardingItems { get; } = [
            new OnBoardingItem("Launcher Menu", "Quick access to settings and useful resources can be found in this menu.", new Rect(1,1,24,14), new Vector2(6,64)),
            new OnBoardingItem("Service Status", "Monitor the status of R5R services here. If there are any preformance or service interruptions, you will see it here.", new Rect(210,1,31,14), new Vector2(600,64)),
            new OnBoardingItem("Downloads And Tasks", "Follow the progress of your game downloads / updates.", new Rect(246,1,31,14), new Vector2(760,64)),
            new OnBoardingItem("Branches And Installing", "Here you can select the game branch you want to install, update, or play", new Rect(20,75,71,63), new Vector2(86,538)),
            new OnBoardingItem("Game Settings", "Clicking this allows you to access advanced settings for the selected branch, as well as verify game files or uninstall.", new Rect(75,101,16,16), new Vector2(334,455)),
            new OnBoardingItem("News And Updates", "View latest updates, patch notes, guides, and anything else related to R5Reloaded straight from the R5R Team.", new Rect(102,77,190,116), new Vector2(455,128)),
            new OnBoardingItem("You're All Set", "You've successfully completed the Launcher Tour. If you have any questions or need further assistance, feel free to join our discord!", new Rect(135,95,0,0), new Vector2(430,305)),
            ];
    }

    public class OnBoardingItem(string title, string description, Rect geoRect, Vector2 translatePos)
    {
        public string Title { get; set; } = title;
        public string Description { get; set; } = description;
        public Rect geoRect { get; set; } = geoRect;
        public Vector2 translatePos { get; set; } = translatePos;
    }
}