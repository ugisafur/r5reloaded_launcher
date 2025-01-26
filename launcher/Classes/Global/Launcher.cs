using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace launcher.Classes.Global
{
    public static class Launcher
    {
        public const string VERSION = "0.9.6.8";
        public const string CONFIG_URL = "https://cdn.r5r.org/launcher/config.json";
        public const string BACKGROUND_VIDEO_URL = "https://cdn.r5r.org/launcher/video_backgrounds/";
        public const int MAX_REPAIR_ATTEMPTS = 5;
        public static string PATH { get; set; } = "";
        public const string NEWSKEY = "68767b4df970e8348b79ad74b1";
        public const string NEWSURL = "https://admin.r5reloaded.com/ghost/api/content";
    }
}