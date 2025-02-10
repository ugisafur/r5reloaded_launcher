using System.Globalization;
using System.IO;

namespace launcher.Global
{
    public static class Logger
    {
        private static readonly string LogFileName = "launcher_log.log";

        public static string LogFilePath = "";

        public enum Type
        {
            Info,
            Warning,
            Error
        }

        public enum Source
        {
            Launcher,
            Download,
            API,
            Installer,
            Uninstaller,
            Update,
            UpdateChecker,
            Repair,
            Patcher,
            Checksums,
            Decompression,
            Ini,
            VDF,
            Unknown,
            Pipe,
        }

        static Logger()
        {
            if (!(bool)Ini.Get(Ini.Vars.Keep_All_Logs))
            {
                string[] folders = Directory.GetDirectories(Path.Combine(Launcher.PATH, $"launcher_data\\logs\\"), "*");
                foreach (string folder in folders)
                {
                    Directory.Delete(folder, true);
                }
            }

            string folderUUID = GenerateFolderUUID();
            LogFilePath = Path.Combine(Launcher.PATH, $"launcher_data\\logs\\{folderUUID}", LogFileName);

            string logDirectory = Path.GetDirectoryName(LogFilePath);
            if (!Directory.Exists(logDirectory))
                Directory.CreateDirectory(logDirectory);
        }

        public static void LogCrashToFile(Exception ex)
        {
            // no point in making a crash log if we're uploading it to Backtrace anyways
            if ((bool)Ini.Get(Ini.Vars.Upload_Crashes))
            {
                Backtrace.Send(ex, Source.Unknown);
                return;
            }

            string filePath = Path.Combine(Path.GetDirectoryName(LogFilePath), "crash.log");

            string log = $@"
===========================================
=== CRASH LOG ============================
===========================================
Date: {DateTime.Now}
Message: {ex.Message}

--- Stack Trace ---
{ex.StackTrace}

--- Inner Exception ---
{(ex.InnerException != null ? ex.InnerException.Message : "None")}

===========================================
";

            try
            {
                File.AppendAllText(filePath, log + Environment.NewLine);
            }
            catch
            {
                // failed: file is probably locked but we don't want to crash the app
            }
        }

        public static string GenerateFolderUUID()
        {
            return Guid.NewGuid().ToString();
        }

        public static void Log(Type type, Source source, string message)
        {
            string typeString = Enum.GetName(typeof(Type), type).ToUpper(new CultureInfo("en-US"));
            string sourceString = Enum.GetName(typeof(Source), source).ToUpper(new CultureInfo("en-US"));
            string logMessage = $"{{ \"time\":\"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\", \"[{typeString}] \": \"[{sourceString}] - {message} }},";

#if DEBUG
            Console.WriteLine(logMessage);
#endif

            try
            {
                File.AppendAllText(LogFilePath, logMessage + Environment.NewLine);
            }
            catch
            {
                // failed: file is probably locked but we don't want to crash the app
            }
        }

        public static void LogException(string title, Source source, Exception ex)
        {
            LogError(source, $@"
==============================================================
{title}
==============================================================
Message: {ex.Message}

--- Stack Trace ---
{ex.StackTrace}

--- Inner Exception ---
{(ex.InnerException != null ? ex.InnerException.Message : "None")}");
        }

        #region Logging Helpers

        public static void LogInfo(Source source, string message) => Log(Type.Info, source, message);

        public static void LogWarning(Source source, string message) => Log(Type.Warning, source, message);

        public static void LogError(Source source, string message) => Log(Type.Error, source, message);

        #endregion Logging Helpers
    }
}