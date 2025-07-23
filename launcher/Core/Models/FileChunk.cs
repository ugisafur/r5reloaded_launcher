namespace launcher.Core.Models
{
    public class FileChunk
    {
        public string path { get; set; }
        public string checksum { get; set; }
        public long size { get; set; }
    }
} 