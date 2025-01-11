using Newtonsoft.Json;

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
            var response = Global.client.Value.GetAsync("https://cdn.r5r.org/launcher/config.json").Result;
            var responseString = response.Content.ReadAsStringAsync().Result;
            Logger.Log(Logger.Type.Info, Logger.Source.API, $"request: https://cdn.r5r.org/launcher/config.json");
            Logger.Log(Logger.Type.Info, Logger.Source.API, $"response: {responseString}");
            return JsonConvert.DeserializeObject<ServerConfig>(responseString);
        }

        public static async Task<GamePatch> FetchPatchFiles()
        {
            int selectedBranchIndex = Utilities.GetCmbBranchIndex();
            string patchURL = Global.serverConfig.branches[selectedBranchIndex].patch_url + "\\patch.json";
            string patchFile = await FetchJson(patchURL);
            var patchFiles = JsonConvert.DeserializeObject<GamePatch>(patchFile);

            patchFiles.files = patchFiles.files
                .Where(file => !file.Name.Contains("opt.starpak", StringComparison.OrdinalIgnoreCase))
                .ToList();

            return patchFiles;
        }

        public static async Task<GamePatch> FetchOptionalPatchFiles()
        {
            int selectedBranchIndex = Utilities.GetCmbBranchIndex();
            string patchURL = Global.serverConfig.branches[selectedBranchIndex].patch_url + "\\patch.json";
            string patchFile = await FetchJson(patchURL);
            var patchFiles = JsonConvert.DeserializeObject<GamePatch>(patchFile);

            patchFiles.files = patchFiles.files
                .Where(file => file.Name.Contains("opt.starpak", StringComparison.OrdinalIgnoreCase))
                .ToList();

            return patchFiles;
        }

        public static async Task<BaseGameFiles> FetchBaseGameFiles(bool compressed)
        {
            string fileName = compressed ? "checksums_zst.json" : "checksums.json";
            string baseGameChecksumUrl = $"{Global.serverConfig.base_game_url}\\{fileName}";
            string baseGameZstChecksums = await FetchJson(baseGameChecksumUrl);

            var baseGameFiles = JsonConvert.DeserializeObject<BaseGameFiles>(baseGameZstChecksums);
            baseGameFiles.files = baseGameFiles.files
                .Where(file => !file.name.Contains("opt.starpak", StringComparison.OrdinalIgnoreCase))
                .ToList();

            return baseGameFiles;
        }

        public static async Task<BaseGameFiles> FetchOptionalGameFiles(bool compressed)
        {
            string fileName = compressed ? "checksums_zst.json" : "checksums.json";
            string baseGameChecksumUrl = $"{Global.serverConfig.base_game_url}\\{fileName}";
            string baseGameZstChecksums = await FetchJson(baseGameChecksumUrl);

            var baseGameFiles = JsonConvert.DeserializeObject<BaseGameFiles>(baseGameZstChecksums);
            baseGameFiles.files = baseGameFiles.files
                .Where(file => file.name.Contains("opt.starpak", StringComparison.OrdinalIgnoreCase))
                .ToList();

            return baseGameFiles;
        }

        public static async Task<string> FetchJson(string url)
        {
            Logger.Log(Logger.Type.Info, Logger.Source.API, $"request: {url}");
            var response = await Global.client.Value.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public static bool HasInternetConnection()
        {
            try
            {
                using var client = new System.Net.WebClient();
                using var stream = client.OpenRead("http://www.google.com");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}