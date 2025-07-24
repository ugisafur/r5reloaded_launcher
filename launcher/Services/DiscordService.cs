using DiscordRPC;
using static launcher.Core.UiReferences;

namespace launcher.Services
{
    class DiscordService
    {
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
