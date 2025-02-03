using Backtrace.Model;
using Backtrace;

namespace launcher.Global
{
    public static class Backtrace
    {
        public static BacktraceCredentials Credentials = new(@"https://submit.backtrace.io/r5rlauncher/6193e7e11129f7cd24cba1c1388f4a4649c30b0d07940a25896171ff162902e5/json");
        public static BacktraceClient Client = new(Credentials);

        public static void Send(Exception exception)
        {
            if (AppState.IsOnline && (bool)Ini.Get(Ini.Vars.Upload_Crashes))
                Client.Send(new BacktraceReport(exception));
        }

        public static async Task SendAsync(Exception exception)
        {
            if (AppState.IsOnline && (bool)Ini.Get(Ini.Vars.Upload_Crashes))
                await Client.SendAsync(new BacktraceReport(exception));
        }
    }
}