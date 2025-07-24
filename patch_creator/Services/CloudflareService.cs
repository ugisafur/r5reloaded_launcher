using patch_creator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace patch_creator.Services
{
    public class CloudflareService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl = "https://api.cloudflare.com/client/v4";

        public CloudflareService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> PurgeEverythingAsync(string zoneId, string authToken)
        {
            if (string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(zoneId))
                return false;

            string url = $"{_apiBaseUrl}/zones/{zoneId}/purge_cache";
            var payload = new { purge_everything = true };
            string jsonPayload = JsonSerializer.Serialize(payload);

            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

                HttpResponseMessage response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Cache has been purged");
                    string responseBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Response from Cloudflare:");
                    Console.WriteLine(responseBody);
                    return true;
                }
                else
                {
                    Console.WriteLine("Failed to purge cache");
                    string errorBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error: {response.StatusCode}");
                    Console.WriteLine(errorBody);
                    return false;
                }
            }
        }

        public async Task<bool> PurgeFilesAsync(string zoneId, string authToken, IEnumerable<string> filesToPurge)
        {
            if (string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(zoneId) || !filesToPurge.Any())
                return false;

            List<List<string>> purgeLists = filesToPurge
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / 30)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();

            string url = $"{_apiBaseUrl}/zones/{zoneId}/purge_cache";
            bool allSucceeded = true;

            foreach (var list in purgeLists)
            {
                var payload = new { files = list };
                string jsonPayload = JsonSerializer.Serialize(payload);

                using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

                    HttpResponseMessage response = await _httpClient.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        Console.WriteLine("Response from Cloudflare:");
                        Console.WriteLine(responseBody);
                    }
                    else
                    {
                        string errorBody = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Error: {response.StatusCode}");
                        Console.WriteLine(errorBody);
                        allSucceeded = false;
                    }
                }
            }
            return allSucceeded;
        }
    }
} 