using launcher.Core.Models;

namespace launcher.Core.Models
{
    public class DownloadMetadata
    {
        public FileDownload fileDownload = new();
        public DownloadItem downloadItem { get; set; }
        public string finalPath { get; set; }
        public string fileUrl { get; set; }
    }
} 