using System.IO;

namespace launcher
{
    public static class Logger
    {
        private static readonly string AppName = "r5r_launcher"; // Change to your app's name
        private static readonly string LogFileName = "log.txt";

        private static readonly string LogFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppName,
            LogFileName);

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
        }

        static Logger()
        {
            // Ensure the directory exists
            var logDirectory = Path.GetDirectoryName(LogFilePath);
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
        }

        public static void Log(Type type, Source source, string message)
        {
            string typeString = GetTypeString(type);
            string sourceString = GetSourceString(source);
            string logMessage = $"{{ \"time\":\"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\", \"[{typeString}] \": \"[{sourceString}] - {message} }},";

#if DEBUG
            Console.WriteLine(logMessage);
#endif

            File.AppendAllText(LogFilePath, logMessage + Environment.NewLine);
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
                _ => "Unknown"
            };
        }
    }
}