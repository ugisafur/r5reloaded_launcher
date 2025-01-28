using System.Net.Http.Json;
using static launcher.Global.Logger;
using System.Text.Json;
using System.Text.RegularExpressions;
using launcher.Game;
using launcher.Global;
using launcher.Managers;
using launcher.BranchUtils;

namespace launcher.CDN
{
    public static class Fetch
    {
        public static ServerConfig Config()
        {
            LogInfo(Source.API, $"request: https://cdn.r5r.org/launcher/config.json");
            return Networking.HttpClient.GetFromJsonAsync<ServerConfig>("https://cdn.r5r.org/launcher/config.json").Result;
        }

        public static string GameVersion(string branch_url)
        {
            var response = Networking.HttpClient.GetAsync($"{branch_url}\\version.txt").Result;
            return response.Content.ReadAsStringAsync().Result;
        }

        public static async Task<GameFiles> GameFiles(bool compressed, bool optional)
        {
            string fileName = compressed ? "checksums_zst.json" : "checksums.json";
            string endingString = compressed ? "opt.starpak.zst" : "opt.starpak";

            JsonSerializerOptions jsonSerializerOptions = new()
            {
                AllowTrailingCommas = true
            };

            GameFiles gameFiles = await Networking.HttpClient.GetFromJsonAsync<GameFiles>($"{GetBranch.GameURL()}\\{fileName}", jsonSerializerOptions);

            List<string> excludedLanguages = GetBranch.Branch().mstr_languages;
            excludedLanguages.Remove("english");

            string languagesPattern = string.Join("|", excludedLanguages.Select(Regex.Escape));
            Regex excludeLangRegex = new Regex($"general_({languagesPattern})(?:_|\\.)", RegexOptions.IgnoreCase);

            if (!optional)
            {
                gameFiles.files = gameFiles.files
                .Where(file =>
                    !file.name.EndsWith(endingString, StringComparison.OrdinalIgnoreCase) &&
                    !excludeLangRegex.IsMatch(file.name)
                )
                .ToList();
            }
            else
            {
                gameFiles.files = gameFiles.files
                .Where(file =>
                    file.name.EndsWith(endingString, StringComparison.OrdinalIgnoreCase) &&
                    !excludeLangRegex.IsMatch(file.name)
                )
                .ToList(); ;
            }

            return gameFiles;
        }

        public static async Task<GameFiles> LanguageFiles(List<string> languages, bool compressed = true)
        {
            string fileName = compressed ? "checksums_zst.json" : "checksums.json";
            string endingstring = compressed ? "mstr.zst" : "mstr";

            // Remove english from the list of languages as english is always included
            languages.Remove("english");

            string languagesPattern = string.Join("|", languages.Select(Regex.Escape));
            Regex excludeLangRegex = new Regex($"general_({languagesPattern})(?:_|\\.)", RegexOptions.IgnoreCase);

            JsonSerializerOptions jsonSerializerOptions = new()
            {
                AllowTrailingCommas = true
            };

            GameFiles gameFiles = await Networking.HttpClient.GetFromJsonAsync<GameFiles>($"{GetBranch.GameURL()}\\{fileName}", jsonSerializerOptions);

            gameFiles.files = gameFiles.files.Where(file => excludeLangRegex.IsMatch(file.name)).ToList();

            return gameFiles;
        }
    }
}