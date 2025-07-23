using System;

namespace launcher.Core.Models
{
    public class FileDownload
    {
        public long totalBytes = 0;
        public long downloadedBytes = 0;
        public DateTime lastUpdate = DateTime.Now;
    }
} 