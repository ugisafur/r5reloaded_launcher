using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace patch_creator.Models
{
    public class ManifestEntry
    {
        public string path { get; set; } = string.Empty;
        public long size { get; set; } = 0;
        public string checksum { get; set; } = string.Empty;
        public bool? optional { get; set; }
        public string language { get; set; } = string.Empty;
        public List<FileChunk> parts { get; set; } = [];
    }
}
