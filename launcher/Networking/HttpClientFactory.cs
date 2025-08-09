
using System.Net.Http;
using System.Net.Http.Handlers;

namespace launcher.Networking
{
    public static class HttpClientFactory
    {
        public static HttpClient CreateClient()
        {
            var handler = new HttpClientHandler
            {
                UseProxy = false,
                UseCookies = false
            };

            var progressHandler = new ProgressMessageHandler(handler);

            return new HttpClient(progressHandler)
            {
                Timeout = TimeSpan.FromMinutes(5)
            };
        }
    }
}
