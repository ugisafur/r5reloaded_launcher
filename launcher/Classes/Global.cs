using SoftCircuits.IniFileParser;
using System.Net.Http;
using System.Windows.Controls;

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
        public const string launcherVersion = "0.4.2";
        public const string serverConfigurl = "https://cdn.r5r.org/launcher/config.json";

        public static bool isOnline = false;

        public static ServerConfig serverConfig;
        public static IniFile launcherConfig;
        public static Lazy<HttpClient> client { get; } = new Lazy<HttpClient>(() => new HttpClient() { Timeout = TimeSpan.FromSeconds(30) });

        public static string launcherPath = "";
        public const int MAX_REPAIR_ATTEMPTS = 5;
        public static int filesLeft = 0;
        public static bool isInstalling = false;
        public static bool isInstalled = false;
        public static bool updateRequired = false;
        public static bool updateCheckLoop = false;
        public static bool badFilesDetected = false;

        public static SemaphoreSlim downloadSemaphore = new SemaphoreSlim(100);
        public static List<string> badFiles = new List<string>();

        public enum SettingsPage
        {
            Application = 0,
            Accessibility = 1,
            GameInstalls = 2,
            Download = 3,
            About = 4
        }
    }

    public static class SettingsGlobal
    {
        public static CheckBox DisableTransitionsBtn;
        public static CheckBox DisableAnimationsBtn;
        public static CheckBox DisableBackgroundVideoBtn;
    }
}