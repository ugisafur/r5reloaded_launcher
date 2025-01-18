using System.IO;

namespace launcher
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
            DownloadManager,
            API,
            Installer,
            Update,
            UpdateChecker,
            Repair,
            Patcher,
            FileManager,
            Decompression,
            Ini,
            VDF
        }

        static Logger()
        {
            // Clear logs if the setting is disabled
            if (!(bool)Ini.Get(Ini.Vars.Keep_All_Logs))
            {
                string[] folders = Directory.GetDirectories(Path.Combine(Constants.Paths.LauncherPath, $"launcher_data\\logs\\"), "*");
                foreach (string folder in folders)
                {
                    Directory.Delete(folder, true);
                }
            }

            // Ensure the directory exists
            string folderUUID = GenerateFolderUUID();
            LogFilePath = Path.Combine(Constants.Paths.LauncherPath, $"launcher_data\\logs\\{folderUUID}", LogFileName);

            string logDirectory = Path.GetDirectoryName(LogFilePath);
            if (!Directory.Exists(logDirectory))
                Directory.CreateDirectory(logDirectory);
        }

        public static void LogCrashToFile(Exception ex)
        {
            string filePath = Path.Combine(Path.GetDirectoryName(Logger.LogFilePath), "crash.log");

            string log = $@"--- Crash Log ---
Date: {DateTime.Now}
Message: {ex.Message}
StackTrace: {ex.StackTrace}
InnerException: {ex.InnerException?.Message}
-------------------";

            File.AppendAllText(filePath, log + Environment.NewLine);
            Logger.LogError(Logger.Source.Launcher, "An error occurred. Check crash.log for details.");
        }

        public static string GenerateFolderUUID()
        {
            return Guid.NewGuid().ToString();
        }

        public static void Log(Type type, Source source, string message)
        {
            string typeString = GetTypeString(type);
            string sourceString = GetSourceString(source);
            string logMessage = $"{{ \"time\":\"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\", \"[{typeString}] \": \"[{sourceString}] - {message} }},";

#if DEBUG
            Console.WriteLine(logMessage);
#endif

            try
            {
                File.AppendAllText(LogFilePath, logMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to write to log file: {ex.Message}");
            }
        }

        private static string GetTypeString(Type type)
        {
            return type switch
            {
                Type.Info => "INFO",
                Type.Warning => "WARNING",
                Type.Error => "ERROR",
                _ => "UNKNOWN"
            };
        }

        private static string GetSourceString(Source source)
        {
            return source switch
            {
                Source.Launcher => "Launcher",
                Source.DownloadManager => "DownloadManager",
                Source.API => "API",
                Source.Installer => "Installer",
                Source.Update => "Update",
                Source.UpdateChecker => "UpdateChecker",
                Source.Repair => "Repair",
                Source.Patcher => "Patcher",
                Source.FileManager => "FileManager",
                Source.Decompression => "Decompression",
                Source.Ini => "INI",
                Source.VDF => "VDF",
                _ => "Unknown"
            };
        }

        #region Logging Helpers

        public static void LogInfo(Source source, string message) => Log(Type.Info, source, message);

        public static void LogWarning(Source source, string message) => Log(Type.Warning, source, message);

        public static void LogError(Source source, string message) => Log(Type.Error, source, message);

        #endregion Logging Helpers
    }
}