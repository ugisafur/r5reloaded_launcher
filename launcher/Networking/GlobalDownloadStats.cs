using System;

namespace launcher.Networking
{
    public static class GlobalDownloadStats
    {
        public static long TotalBytes = 0;
        public static long DownloadedBytes = 0;
        public static DateTime StartTime;
        public static string totalText = "";
        public static string downloadedText = "";
        public static string timeLeftText = "";
    }
} 