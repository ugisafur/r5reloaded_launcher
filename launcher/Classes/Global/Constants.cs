using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace launcher.Classes.Global
{
    public static class Launcher
    {
        public const string VERSION = "0.9.2";
        public const string CONFIG_URL = "https://cdn.r5r.org/launcher/config.json";
        public const string BACKGROUND_VIDEO_URL = "https://cdn.r5r.org/launcher/video_backgrounds/";
        public const int MAX_REPAIR_ATTEMPTS = 5;
        public static string PATH { get; set; } = "";
    }
}