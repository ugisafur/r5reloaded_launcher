using Newtonsoft.Json;
using static launcher.Logger;

namespace launcher
{
    /// <summary>
    /// The DataFetcher class is responsible for fetching various types of data from remote servers.
    /// It includes methods to fetch server configuration, game patch files, JSON data from a given URL,
    /// and base game files. The class uses asynchronous programming to ensure non-blocking operations
    /// when fetching data over the network.
    /// </summary>
    public class DataFetcher
    {
        public static ServerConfig FetchServerConfig()
        {
            var response = Networking.HttpClient.GetAsync("https://cdn.r5r.org/launcher/config.json").Result;
            var responseString = response.Content.ReadAsStringAsync().Result;
            LogInfo(Source.API, $"request: https://cdn.r5r.org/launcher/config.json");
            //LogInfo(Source.API, $"response: \n{responseString}");
            return JsonConvert.DeserializeObject<ServerConfig>(responseString);
        }

        public static string FetchBranchVersion(string branch_url)
        {
            var response = Networking.HttpClient.GetAsync($"{branch_url}\\version.txt").Result;
            var responseString = response.Content.ReadAsStringAsync().Result;
            return responseString;
        }

        public static async Task<GameFiles> FetchBaseGameFiles(bool compressed)
        {
            string fileName = compressed ? "checksums_zst.json" : "checksums.json";
            string endingString = compressed ? "opt.starpak.zst" : "opt.starpak";

            string baseGameChecksumUrl = $"{Configuration.ServerConfig.branches[Utilities.GetCmbBranchIndex()].game_url}\\{fileName}";
            string baseGameZstChecksums = await FetchJson(baseGameChecksumUrl);

            var baseGameFiles = JsonConvert.DeserializeObject<GameFiles>(baseGameZstChecksums);
            baseGameFiles.files = baseGameFiles.files
                .Where(file => !file.name.EndsWith(endingString, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return baseGameFiles;
        }

        public static async Task<GameFiles> FetchOptionalGameFiles(bool compressed)
        {
            string fileName = compressed ? "checksums_zst.json" : "checksums.json";
            string endingString = compressed ? "opt.starpak.zst" : "opt.starpak";

            string optionalGameChecksumUrl = $"{Configuration.ServerConfig.branches[Utilities.GetCmbBranchIndex()].game_url}\\{fileName}";
            string optionalGameZstChecksums = await FetchJson(optionalGameChecksumUrl);

            var optionalGameFiles = JsonConvert.DeserializeObject<GameFiles>(optionalGameZstChecksums);
            optionalGameFiles.files = optionalGameFiles.files
                .Where(file => file.name.EndsWith(endingString, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return optionalGameFiles;
        }

        public static async Task<string> FetchJson(string url)
        {
            LogInfo(Source.API, $"request: {url}");
            var response = await Networking.HttpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public static bool TestConnection()
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