using DiscordRPC;
using launcher.Game;
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
using static launcher.Global.Logger;
using static launcher.Global.References;
using static System.Windows.Forms.Design.AxImporter;

namespace launcher.Global
{
    public static class Launcher
    {
        public const string VERSION = "1.1.1";

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

            LogInfo(LogSource.Launcher, $"Launcher Version: {version}");

            Launcher.PATH = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
            LogInfo(LogSource.Launcher, $"Launcher path: {Launcher.PATH}");

            ServerConfig = AppState.IsOnline ? Fetch.Config() : null;

            LauncherConfig = Ini.GetConfig();
            LogInfo(LogSource.Launcher, $"Launcher config found");

            cultureInfo = CultureInfo.CurrentCulture;
            language_name = cultureInfo.Parent.EnglishName.ToLower(new CultureInfo("en-US"));

            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
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

        private static RichPresence richPresence;
        private static Timestamps timestamps;

        public static void SetRichPresence(string details, string state)
        {
            if (RPC_client == null || !RPC_client.IsInitialized)
                return;

            richPresence ??= new RichPresence();

            richPresence.Details = details;
            richPresence.State = state;

            if(timestamps == null) richPresence.Timestamps = timestamps;

            RPC_client.SetPresence(richPresence);
        }

        public static void SetRichPresence(string details, string state, string largeImageKey, string smallImageKey)
        {
            if (RPC_client == null || !RPC_client.IsInitialized)
                return;

            richPresence ??= new RichPresence();
            timestamps ??= new Timestamps() { Start = DateTime.UtcNow };

            richPresence.Timestamps = timestamps;
            richPresence.Details = details;
            richPresence.State = state;
            richPresence.Assets = new Assets()
            {
                LargeImageKey = largeImageKey,
                LargeImageText = "R5Reloaded Launcher",
                SmallImageKey = smallImageKey,
                SmallImageText = "R5Reloaded Launcher"
            };

            RPC_client.SetPresence(richPresence);
        }
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
        public static List<GameFile> BadFiles { get; } = [];
        public static List<Branch> FolderBranches { get; } = [];

        public static List<OnBoardingItem> OnBoardingItems { get; } = [
            new OnBoardingItem("Launcher Menu", "Quick access to settings and useful resources can be found in this menu.", new Rect(1,1,24,14), new Vector2(6,64)),
            new OnBoardingItem("Service Status", "Monitor the status of R5R services here. If there are any performance or service interruptions, you will see it here.", new Rect(210,1,31,14), new Vector2(600,64)),
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

    public static class Fetch
    {
        public static ServerConfig Config()
        {
            LogInfo(LogSource.API, $"request: https://cdn.r5r.org/launcher/config.json");
            return Networking.HttpClient.GetFromJsonAsync<ServerConfig>("https://cdn.r5r.org/launcher/config.json").Result;
        }

        public static string GameVersion(string branch_url)
        {
            var response = Networking.HttpClient.GetAsync($"{branch_url}\\version.txt").Result;
            return response.Content.ReadAsStringAsync().Result;
        }

        public static async Task<GameFiles> GameFiles(bool optional)
        {
            JsonSerializerOptions jsonSerializerOptions = new() { AllowTrailingCommas = true };

            GameFiles gameFiles = await Networking.HttpClient.GetFromJsonAsync<GameFiles>($"{GetBranch.GameURL()}\\checksums.json", jsonSerializerOptions);

            List<string> excludedLanguages = GetBranch.Branch().mstr_languages;
            excludedLanguages.Remove("english");

            string languagesPattern = string.Join("|", excludedLanguages.Select(Regex.Escape));
            Regex excludeLangRegex = new Regex($"general_({languagesPattern})(?:_|\\.)", RegexOptions.IgnoreCase);

            if (!optional)
            {
                gameFiles.files = gameFiles.files.Where(file => !file.optional && !excludeLangRegex.IsMatch(file.destinationPath)).ToList();
            }
            else
            {
                gameFiles.files = gameFiles.files.Where(file => file.optional && !excludeLangRegex.IsMatch(file.destinationPath)).ToList();
            }

            return gameFiles;
        }

        public static async Task<GameFiles> LanguageFiles(List<string> languages)
        {
            languages.Remove("english");

            string languagesPattern = string.Join("|", languages.Select(Regex.Escape));
            Regex excludeLangRegex = new Regex($"general_({languagesPattern})(?:_|\\.)", RegexOptions.IgnoreCase);

            JsonSerializerOptions jsonSerializerOptions = new()
            {
                AllowTrailingCommas = true
            };

            GameFiles gameFiles = await Networking.HttpClient.GetFromJsonAsync<GameFiles>($"{GetBranch.GameURL()}\\checksums.json", jsonSerializerOptions);

            gameFiles.files = gameFiles.files.Where(file => excludeLangRegex.IsMatch(file.destinationPath)).ToList();

            return gameFiles;
        }
    }
}