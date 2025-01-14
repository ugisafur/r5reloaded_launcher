using SoftCircuits.IniFileParser;
using System.Net.Http;
using System.Windows.Controls;
using static launcher.Logger;
using static launcher.ControlReferences;
using System.IO;

namespace launcher
{
    /// <summary>
    /// The Global class contains static fields and constants that are used throughout the launcher application.
    /// It includes configuration settings, HTTP client setup, and various flags and counters to manage the state of the application.
    ///
    /// Fields and Constants:
    /// - launcherVersion: The current version of the launcher.
    /// - serverConfig: Configuration settings for the server (nullable).
    /// - launcherConfig: Configuration settings for the launcher (nullable).
    /// - client: An instance of HttpClient with a timeout of 30 seconds, used for making HTTP requests.
    /// - launcherPath: The file path where the launcher is located.
    /// - MAX_REPAIR_ATTEMPTS: The maximum number of attempts to repair the launcher.
    /// - filesLeft: The number of files left to process.
    /// - isInstalling: A flag indicating if the installation process is ongoing.
    /// - isInstalled: A flag indicating if the launcher is installed.
    /// - updateRequired: A flag indicating if an update is required.
    /// - updateCheckLoop: A flag indicating if the update check loop is active.
    /// - badFilesDetected: A flag indicating if any bad files have been detected.
    /// - downloadSemaphore: A semaphore to limit the number of concurrent downloads to 100.
    /// - badFiles: A list of bad files detected during the process.
    /// </summary>
    public static class Global
    {
        public const string LAUNCHER_VERSION = "0.6.0";
        public const string SERVER_CONFIG_URL = "https://cdn.r5r.org/launcher/config.json";

        public static bool IS_ONLINE = false;

        public static ServerConfig SERVER_CONFIG;
        public static IniFile LAUNCHER_CONFIG;
        public static readonly HttpClient HTTP_CLIENT = new() { Timeout = TimeSpan.FromSeconds(30) };

        public static bool IS_LOCAL_BRANCH = false;
        public static string LAUNCHER_PATH = "";
        public const int MAX_REPAIR_ATTEMPTS = 5;
        public static int FILES_LEFT = 0;
        public static bool IS_INSTALLING = false;
        public static bool UPDATE_CHECK_LOOP = false;
        public static bool BAD_FILES_DETECTED = false;

        public static bool IN_SETTINGS_MENU = false;
        public static bool IN_ADVANCED_MENU = false;

        public static SemaphoreSlim DOWNLOAD_SEMAPHORE = new(500);
        public static List<string> BAD_FILES = [];
        public static List<Branch> folderBranches = [];

        public enum SettingsPage
        {
            APPLICATION = 0,
            ACCESSIBILITY = 1,
            GAME_INSTALLS = 2,
            DOWNLOAD = 3,
            ABOUT = 4
        }

        public static void SetupGlobals()
        {
            launcherVersionlbl.Text = LAUNCHER_VERSION;
            LogInfo(Source.Launcher, $"Launcher Version: {LAUNCHER_VERSION}");

            LAUNCHER_PATH = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
            LogInfo(Source.Launcher, $"Launcher path: {LAUNCHER_PATH}");

            SERVER_CONFIG = IS_ONLINE ? DataFetcher.FetchServerConfig() : null;

            LAUNCHER_CONFIG = Ini.GetConfig();
            LogInfo(Source.Launcher, $"Launcher config found");
        }
    }
}