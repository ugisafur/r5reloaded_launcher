using launcher.Classes.BranchUtils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using launcher.Classes.Global;
using static launcher.Classes.Utilities.Logger;
using launcher.Classes.Utilities;
using System.Text.Json;
using System.Text.RegularExpressions;
using ZstdSharp;
using static System.Windows.Forms.Design.AxImporter;

namespace launcher.Classes.CDN
{
    public static class Fetch
    {
        public static ServerConfig Config()
        {
            LogInfo(Source.API, $"request: https://cdn.r5r.org/launcher/config.json");
            return HttpClientJsonExtensions.GetFromJsonAsync<ServerConfig>(Networking.HttpClient, "https://cdn.r5r.org/launcher/config.json").Result;
        }

        public static string GameVersion(string branch_url)
        {
            var response = Networking.HttpClient.GetAsync($"{branch_url}\\version.txt").Result;
            return response.Content.ReadAsStringAsync().Result;
        }

        public static async Task<GameFiles> BranchFiles(bool compressed, bool optional)
        {
            string fileName = compressed ? "checksums_zst.json" : "checksums.json";
            string endingString = compressed ? "opt.starpak.zst" : "opt.starpak";

            JsonSerializerOptions jsonSerializerOptions = new()
            {
                AllowTrailingCommas = true
            };

            GameFiles gameFiles = await HttpClientJsonExtensions.GetFromJsonAsync<GameFiles>(Networking.HttpClient, $"{GetBranch.GameURL()}\\{fileName}", jsonSerializerOptions);

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

        public static async Task<GameFiles> LangFile(List<string> languages, bool compressed = true)
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

            GameFiles gameFiles = await HttpClientJsonExtensions.GetFromJsonAsync<GameFiles>(Networking.HttpClient, $"{GetBranch.GameURL()}\\{fileName}", jsonSerializerOptions);

            gameFiles.files = gameFiles.files.Where(file => excludeLangRegex.IsMatch(file.name)).ToList();

            return gameFiles;
        }
    }

    public static class Connection
    {
        public static bool Test()
        {
            try
            {
                using var client = new System.Net.WebClient();
                using var stream = client.OpenRead("https://cdn.r5r.org/launcher/config.json");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}