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
        public static SemaphoreSlim DownloadSemaphore = new(500);
    }
}