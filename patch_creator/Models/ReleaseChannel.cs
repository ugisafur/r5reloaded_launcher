using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace patch_creator.Models
{
    public class ReleaseChannel
    {
        public string? name { get; set; }
        public string? version { get; set; }
        public string? game_url { get; set; }
        public bool? enabled { get; set; }
        public bool? show_in_launcher { get; set; }
    }
}
