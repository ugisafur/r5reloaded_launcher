using Polly.CircuitBreaker;
using System.Net.Http;
using static launcher.Services.LoggerService;

namespace launcher.Services
{
    public static class NetworkHealthService
    {
        public static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(30) };

        public static SemaphoreSlim DownloadSemaphore = new(100);

        public static async Task<bool> IsCdnAvailableAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5); // Set a timeout (e.g., 5 seconds)
                client.DefaultRequestHeaders.UserAgent.ParseAdd($"R5R-Launcher/{Launcher.VERSION} (+https://r5reloaded.com)");

                try
                {
                    var response = await client.GetAsync("https://cdn.r5r.org/launcher/config.json");
                    if (response.IsSuccessStatusCode)
                    {
                        Launcher.CONFIG_URL = "https://cdn.r5r.org/launcher/config.json";
                        Launcher.LAUNCHER_THEME_URL = "https://cdn.r5r.org/launcher/theme.xaml";
                        Launcher.BACKGROUND_VIDEO_URL = "https://cdn.r5r.org/launcher/video_backgrounds/";
                        Launcher.NEWSURL = "https://admin.r5reloaded.com/ghost/api/content";
                        Launcher.isAltCDN = false;
                        return true;
                    }
                    else
                    {
                        throw new Exception("CDN1 status code was unsuccessful");
                    }
                }
                catch (Exception ex) {
                    LogInfo(LogSource.Launcher, $"CDN1 check failed: {ex.Message}");
                    try
                    {
                        var response2 = await client.GetAsync("https://r5r.ugniushosting.com/launcher/config.json");
                        if (response2.IsSuccessStatusCode)
                        {
                            Launcher.CONFIG_URL = "https://r5r.ugniushosting.com/launcher/config.json";
                            Launcher.LAUNCHER_THEME_URL = "https://r5r.ugniushosting.com/launcher/theme.xaml";
                            Launcher.BACKGROUND_VIDEO_URL = "https://r5r.ugniushosting.com/launcher/video_backgrounds/";
                            Launcher.NEWSURL = "https://r5r.ugniushosting.com/ghost/api/content";
                            Launcher.isAltCDN = true;
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    catch (Exception a)
                    {
                        LogInfo(LogSource.Launcher, $"CDN2 check failed: {a.Message}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                LogInfo(LogSource.Launcher, $"IsCdnAvailableAsync check failed: {ex.Message}");
                return false; // Return false if there's an exception (e.g., timeout or network error)
            }
        }

        public static async Task<bool> IsNewsApiAvailableAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5); // Set a timeout (e.g., 5 seconds)

                var response = await client.GetAsync($"{Launcher.NEWSURL}/posts/?key={Launcher.NEWSKEY}&include=tags,authors");
                return response.IsSuccessStatusCode; // Return true if the request was successful
            }
            catch
            {
                return false; // Return false if there's an exception (e.g., timeout or network error)
            }
        }

        public static async Task<bool> IsMasterServerAvailableAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5); // Set a timeout (e.g., 5 seconds)

                var response = await client.GetAsync(Launcher.MS_URL);
                return response.IsSuccessStatusCode; // Return true if the request was successful
            }
            catch
            {
                return false; // Return false if there's an exception (e.g., timeout or network error)
            }
        }
    }
} 