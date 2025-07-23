using launcher.Core.Models;
using System.Collections.Generic;
using System.Numerics;
using System.Windows;

namespace launcher.Core
{
    public static class DataCollections
    {
        public static List<GameFile> BadFiles { get; } = [];
        public static List<Branch> FolderBranches { get; } = [];

        public static List<OnBoardingItem> OnBoardingItems { get; } = [
            new OnBoardingItem("Launcher Menu", "Quick access to settings and useful resources can be found in this menu.", new Rect(1,1,24,14), new Vector2(6,64)),
            new OnBoardingItem("Service Status", "Monitor the status of R5R services here. If there are any performance or service interruptions, you will see it here.", new Rect(210,1,31,14), new Vector2(600,64)),
            new OnBoardingItem("Downloads And Tasks", "Follow the progress of your game downloads / updates.", new Rect(246,1,31,14), new Vector2(760,64)),
            new OnBoardingItem("Branches And Installing", "Here you can select the game branch you want to install, update, or play", new Rect(20,75,71,63), new Vector2(86,538)),
            new OnBoardingItem("Game Settings", "Clicking this allows you to access advanced settings for the selected branch, as well as verify game files or uninstall.", new Rect(75,101,16,16), new Vector2(334,455)),
            new OnBoardingItem("News And Updates", "View latest updates, patch notes, guides, and anything else related to R5Reloaded straight from the R5R Team.", new Rect(102,77,190,116), new Vector2(455,128)),
            new OnBoardingItem("You're All Set", "You've successfully completed the Launcher Tour. If you have any questions or need further assistance, feel free to join our discord!", new Rect(135,95,0,0), new Vector2(430,305)),
            ];
    }
} 