using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace launcher.Global
{
    public static class Networking
    {
        public static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(30) };

        //Only used for downloading files from the CDN as i need to set some custom headers
        public static readonly HttpClient DownloadHttpClient = new() { Timeout = TimeSpan.FromSeconds(30) };

        public static SemaphoreSlim DownloadSemaphore = new(500);
    }
}