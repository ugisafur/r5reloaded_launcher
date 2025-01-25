using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using launcher.Classes.Utilities;

namespace launcher.Classes.Global
{
    public static class DataCollections
    {
        public static List<string> BadFiles { get; } = [];
        public static List<Branch> FolderBranches { get; } = [];

        public static List<Rect> OnboardGeoRects { get; } = [
            new Rect(1,1,24,14),
            new Rect(210,1,31,14),
            new Rect(246,1,31,14),
            new Rect(20,75,71,63),
            new Rect(102,77,190,116),
            ];

        public static List<Vector2> OnBoardControlPos { get; } = [
            new Vector2(6,64),
            new Vector2(600,64),
            new Vector2(760,64),
            new Vector2(86,538),
            new Vector2(455,128),
            ];

        public static List<string> OnBoardTitles { get; } = [
            "Launcher Menu",
            "Status",
            "Downloads And Tasks",
            "Branches And Installing",
            "News And Updates"
            ];

        public static List<string> OnBoardDescs { get; } = [
            "Quick access to settings and useful resources can be found in this menu.",
            "Monitor the status of R5R services here. If there are any preformance or service interruptions, you will see it here.",
            "Follow the progress of your game downloads / updates.",
            "Here you can select the game branch you want to install update, or play, you can also access advanced settings for the game using the button to the right of branches.",
            "View latest updates, patch notes, guides, and anything else related to R5Reloaded straight from the R5R Team."
            ];
    }
}