namespace launcher.GameLifecycle.Models
{
    public class DownloadContext
    {
        public DownloadProgress downloadProgress = new();
        public DownloadItem downloadItem { get; set; }
        public string finalPath { get; set; }
        public string fileUrl { get; set; }
    }
} 