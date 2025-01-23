using SoftCircuits.IniFileParser;
using static launcher.Classes.Utilities.Logger;
using static launcher.Classes.Global.References;
using launcher.Classes.CDN;
using System.IO;
using launcher.Classes.Utilities;

namespace launcher.Classes.Global
{
    public static class Configuration
    {
        public static ServerConfig ServerConfig { get; set; }
        public static IniFile LauncherConfig { get; set; }

        public static void Init()
        {
            Version_Label.Text = Launcher.VERSION;
            Logger.LogInfo(Source.Launcher, $"Launcher Version: {Launcher.VERSION}");

            Launcher.PATH = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
            Logger.LogInfo(Source.Launcher, $"Launcher path: {Launcher.PATH}");

            Configuration.ServerConfig = AppState.IsOnline ? Fetch.Config() : null;

            Configuration.LauncherConfig = Ini.GetConfig();
            Logger.LogInfo(Source.Launcher, $"Launcher config found");
        }
    }
}