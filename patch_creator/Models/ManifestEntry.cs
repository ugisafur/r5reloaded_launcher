using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace patch_creator.Models
{
    public class ManifestEntry
    {
        public string? path { get; set; }
        public long? size { get; set; }
        public string? checksum { get; set; }
        public bool? optional { get; set; }
        public string? language { get; set; }
        public List<FileChunk>? parts { get; set; }
    }
}
