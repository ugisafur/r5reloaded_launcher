using launcher.Core.Models;

namespace launcher.Core.Models
{
    public class DownloadContext
    {
        public DownloadProgress downloadProgress = new();
        public DownloadItem downloadItem { get; set; }
        public string finalPath { get; set; }
        public string fileUrl { get; set; }
    }
} 