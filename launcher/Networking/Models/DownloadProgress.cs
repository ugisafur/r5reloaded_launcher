using System;

namespace launcher.Networking.Models
{
    public static class DownloadProgress
    {
        public static long TotalBytes = 0;
        public static long DownloadedBytes = 0;
        public static DateTime StartTime;
        public static string TotalText = "";
        public static string DownloadedText = "";
        public static string TimeLeftText = "";
    }
} 