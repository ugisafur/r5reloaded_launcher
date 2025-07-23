using System.Collections.Generic;

namespace launcher.Core.Models
{
    public class GameFile
    {
        public string path { get; set; } = string.Empty;
        public long size { get; set; } = 0;
        public string checksum { get; set; } = string.Empty;
        public bool optional { get; set; }
        public string language { get; set; } = string.Empty;
        public List<FilePart> parts { get; set; } = [];
        public DownloadMetadata downloadMetadata = new();
    }
} 