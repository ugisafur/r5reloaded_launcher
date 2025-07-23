using DiscordRPC;
using System;
using static launcher.Core.UiReferences;

namespace launcher.Core
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
        public static bool OnBoarding { get; set; } = false;
        public static bool BlockLanguageInstall { get; set; } = false;
        public static int FilesLeft { get; set; } = 0;

        private static RichPresence richPresence;
        private static Timestamps timestamps;

        public static void SetRichPresence(string details, string state)
        {
            if (RPC_client == null || !RPC_client.IsInitialized)
                return;

            richPresence ??= new RichPresence();

            richPresence.Details = details;
            richPresence.State = state;

            if (timestamps == null) richPresence.Timestamps = timestamps;

            RPC_client.SetPresence(richPresence);
        }

        public static void SetRichPresence(string details, string state, string largeImageKey, string smallImageKey)
        {
            if (RPC_client == null || !RPC_client.IsInitialized)
                return;

            richPresence ??= new RichPresence();
            timestamps ??= new Timestamps() { Start = DateTime.UtcNow };

            richPresence.Timestamps = timestamps;
            richPresence.Details = details;
            richPresence.State = state;
            richPresence.Assets = new Assets()
            {
                LargeImageKey = largeImageKey,
                LargeImageText = "R5Reloaded Launcher",
                SmallImageKey = smallImageKey,
                SmallImageText = "R5Reloaded Launcher"
            };

            RPC_client.SetPresence(richPresence);
        }
    }
} 