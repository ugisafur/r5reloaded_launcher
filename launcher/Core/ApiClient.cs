using launcher.Core.Models;
using launcher.Services;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using static launcher.Utils.Logger;

namespace launcher.Core
{
    public static class ApiClient
    {
        public static RemoteConfig GetRemoteConfig()
        {
            LogInfo(LogSource.API, $"request: https://cdn.r5r.org/launcher/config.json");
            return NetworkHealthService.HttpClient.GetFromJsonAsync<RemoteConfig>("https://cdn.r5r.org/launcher/config.json").Result;
        }

        public static string GetGameVersion(string branch_url)
        {
            var response = NetworkHealthService.HttpClient.GetAsync($"{branch_url}\\version.txt").Result;
            return response.Content.ReadAsStringAsync().Result;
        }

        public static async Task<GameManifest> GetGameManifestAsync(bool optional)
        {
            GameManifest gameManifest = await NetworkHealthService.HttpClient.GetFromJsonAsync<GameManifest>($"{ReleaseChannelService.GetGameURL()}\\checksums.json", new JsonSerializerOptions() { AllowTrailingCommas = true });

            gameManifest.files = gameManifest.files.Where(file => file.optional == optional && string.IsNullOrEmpty(file.language)).ToList();

            return gameManifest;
        }

        public static async Task<GameManifest> GetLanguageFilesAsync(ReleaseChannel channel = null)
        {
            if (channel != null)
            {
                GameManifest gameManifest = await NetworkHealthService.HttpClient.GetFromJsonAsync<GameManifest>($"{channel.game_url}\\checksums.json", new JsonSerializerOptions() { AllowTrailingCommas = true });

                gameManifest.files = gameManifest.files.Where(file => !string.IsNullOrEmpty(file.language)).ToList();

                return gameManifest;
            }

            GameManifest GameManifest = await NetworkHealthService.HttpClient.GetFromJsonAsync<GameManifest>($"{ReleaseChannelService.GetGameURL()}\\checksums.json", new JsonSerializerOptions() { AllowTrailingCommas = true });

            GameManifest.files = GameManifest.files.Where(file => !string.IsNullOrEmpty(file.language)).ToList();

            return GameManifest;
        }
    }
} 