using Backtrace.Model;
using Backtrace;
using static launcher.Global.Logger;
using System.Globalization;
using System.IO;

namespace launcher.Global
{
    public static class Backtrace
    {
        public static BacktraceCredentials Credentials = new(@"https://submit.backtrace.io/r5rlauncher/6193e7e11129f7cd24cba1c1388f4a4649c30b0d07940a25896171ff162902e5/json");
        public static BacktraceClient Client = new(Credentials);

        public static void Send(Exception exception, Source source)
        {
            if (AppState.IsOnline && (bool)Ini.Get(Ini.Vars.Upload_Crashes))
            {
                BacktraceReport report = new(exception);
                report.Attributes.Add("Launcher Version", Launcher.VERSION);
                report.Attributes.Add("Log Source", Enum.GetName(typeof(Source), source).ToUpper(new CultureInfo("en-US")));

                if (File.Exists(Path.Combine(Launcher.PATH, "launcher_data\\cfg\\launcherConfig.ini")))
                    report.AttachmentPaths.Add(Path.Combine(Launcher.PATH, "launcher_data\\cfg\\launcherConfig.ini"));

                if (File.Exists(LogFilePath))
                    report.AttachmentPaths.Add(LogFilePath);

                Client.Send(report);
            }
        }

        public static async Task SendAsync(Exception exception, Source source)
        {
            if (AppState.IsOnline && (bool)Ini.Get(Ini.Vars.Upload_Crashes))
            {
                BacktraceReport report = new(exception);
                report.Attributes.Add("Version", Launcher.VERSION);
                report.Attributes.Add("Source", Enum.GetName(typeof(Source), source).ToUpper(new CultureInfo("en-US")));
                await Client.SendAsync(report);
            }
        }
    }
}