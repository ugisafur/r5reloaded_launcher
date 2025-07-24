using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace patch_creator.Models
{
    public class FileChunk
    {
        public string? path { get; set; }
        public string? checksum { get; set; }
        public long? size { get; set; }
    }
}
