using Newtonsoft.Json;
using patch_creator.Models;
using System.Net.Http;
using System.Threading.Tasks;

namespace patch_creator.Services
{
    public class RemoteContentService
    {
        private readonly HttpClient _httpClient;

        public RemoteContentService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<RemoteConfig> GetRemoteConfigAsync(string url)
        {
            var responseString = await _httpClient.GetStringAsync(url);
            return JsonConvert.DeserializeObject<RemoteConfig>(responseString);
        }

        public async Task<GameManifest> GetGameManifestAsync(string url)
        {
            var responseString = await _httpClient.GetStringAsync(url);
            return JsonConvert.DeserializeObject<GameManifest>(responseString);
        }
    }
} 