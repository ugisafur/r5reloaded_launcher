using SoftCircuits.IniFileParser;
using System.Net.Http;
using System.Windows.Controls;
using static launcher.Logger;
using static launcher.ControlReferences;
using System.IO;
using Microsoft.VisualBasic;

namespace launcher
{
    public static class Constants
    {
        public static class Launcher
        {
            public const string VERSION = "0.7.8";
            public const string CONFIG_URL = "https://cdn.r5r.org/launcher/config.json";
            public const int MAX_REPAIR_ATTEMPTS = 5;
        }

        public static class Paths
        {
            public static string LauncherPath { get; set; } = "";
        }

        public static class Settings
        {
            public enum SettingsPage
            {
                Application = 0,
                Accessibility = 1,
                GameInstalls = 2,
                Download = 3,
                About = 4
            }
        }
    }

    public static class Configuration
    {
        public static ServerConfig ServerConfig { get; set; }
        public static IniFile LauncherConfig { get; set; }
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

        public static int FilesLeft { get; set; } = 0;
    }

    public static class Networking
    {
        public static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(30) };
        public static SemaphoreSlim DownloadSemaphore = new(500);
    }

    public static class DataCollections
    {
        public static List<string> BadFiles { get; } = [];
        public static List<Branch> FolderBranches { get; } = [];
    }

    public static class GlobalInitializer
    {
        public static void Setup()
        {
            Version_Label.Text = Constants.Launcher.VERSION;
            Logger.LogInfo(Source.Launcher, $"Launcher Version: {Constants.Launcher.VERSION}");

            Constants.Paths.LauncherPath = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
            Logger.LogInfo(Source.Launcher, $"Launcher path: {Constants.Paths.LauncherPath}");

            Configuration.ServerConfig = AppState.IsOnline ? DataFetcher.FetchServerConfig() : null;

            Configuration.LauncherConfig = Ini.GetConfig();
            Logger.LogInfo(Source.Launcher, $"Launcher config found");
        }
    }
}