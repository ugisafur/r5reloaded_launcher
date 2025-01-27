using SoftCircuits.IniFileParser;
using static launcher.Utilities.Logger;
using static launcher.Global.References;
using System.IO;
using System.Globalization;
using launcher.Utilities;
using launcher.Managers;
using launcher.CDN;

namespace launcher.Global
{
    public static class Configuration
    {
        public static ServerConfig ServerConfig { get; set; }
        public static IniFile LauncherConfig { get; set; }
        public static CultureInfo cultureInfo { get; set; }
        public static string language_name { get; set; }

        public static void Init()
        {
            Version_Label.Text = Launcher.VERSION;
            LogInfo(Source.Launcher, $"Launcher Version: {Launcher.VERSION}");

            Launcher.PATH = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
            LogInfo(Source.Launcher, $"Launcher path: {Launcher.PATH}");

            ServerConfig = AppState.IsOnline ? Fetch.Config() : null;

            LauncherConfig = Ini.GetConfig();
            LogInfo(Source.Launcher, $"Launcher config found");

            cultureInfo = CultureInfo.CurrentCulture;
            language_name = cultureInfo.Parent.EnglishName.ToLower(new CultureInfo("en-US"));
        }
    }
}