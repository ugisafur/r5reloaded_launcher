using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace patch_creator.Models
{
    public class FileChunk
    {
        public string path { get; set; } = string.Empty;
        public string checksum { get; set; } = string.Empty;
        public long size { get; set; } = 0;
    }
}
