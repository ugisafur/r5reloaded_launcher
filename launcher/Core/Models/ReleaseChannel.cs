namespace launcher.Core.Models
{
    public class ReleaseChannel
    {
        public string branch { get; set; }
        public string game_url { get; set; }
        public string dedi_url { get; set; }
        public bool enabled { get; set; }
        public bool allow_updates { get; set; }
        public bool is_local_branch = false;
        public bool update_available = false;
    }
} 