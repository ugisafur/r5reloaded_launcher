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
        public static ServerConfig GetServerConfig()
        {
            LogInfo(LogSource.API, $"request: https://cdn.r5r.org/launcher/config.json");
            return NetworkHealthService.HttpClient.GetFromJsonAsync<ServerConfig>("https://cdn.r5r.org/launcher/config.json").Result;
        }

        public static string GetGameVersion(string branch_url)
        {
            var response = NetworkHealthService.HttpClient.GetAsync($"{branch_url}\\version.txt").Result;
            return response.Content.ReadAsStringAsync().Result;
        }

        public static async Task<GameFiles> GetGameFilesAsync(bool optional)
        {
            GameFiles gameFiles = await NetworkHealthService.HttpClient.GetFromJsonAsync<GameFiles>($"{GetBranch.GameURL()}\\checksums.json", new JsonSerializerOptions() { AllowTrailingCommas = true });

            gameFiles.files = gameFiles.files.Where(file => file.optional == optional && string.IsNullOrEmpty(file.language)).ToList();

            return gameFiles;
        }

        public static async Task<GameFiles> GetLanguageFilesAsync(Branch branch = null)
        {
            if (branch != null)
            {
                GameFiles branchgameFiles = await NetworkHealthService.HttpClient.GetFromJsonAsync<GameFiles>($"{branch.game_url}\\checksums.json", new JsonSerializerOptions() { AllowTrailingCommas = true });

                branchgameFiles.files = branchgameFiles.files.Where(file => !string.IsNullOrEmpty(file.language)).ToList();

                return branchgameFiles;
            }

            GameFiles gameFiles = await NetworkHealthService.HttpClient.GetFromJsonAsync<GameFiles>($"{GetBranch.GameURL()}\\checksums.json", new JsonSerializerOptions() { AllowTrailingCommas = true });

            gameFiles.files = gameFiles.files.Where(file => !string.IsNullOrEmpty(file.language)).ToList();

            return gameFiles;
        }
    }
} 