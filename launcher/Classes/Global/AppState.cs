using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace launcher.Classes.Global
{
    public static class AppState
    {
        public static bool IsOnline { get; set; } = false;
        public static bool IsLocalBranch { get; set; } = false;
        public static bool IsInstalling { get; set; } = false;
        public static bool UpdateCheckLoop { get; set; } = false;
        public static bool BadFilesDetected { get; set; } = false;

        public static bool InSettingsMenu { get; set; } = false;
        public static bool InAdvancedMenu { get; set; } = false;

        public static int FilesLeft { get; set; } = 0;
    }
}