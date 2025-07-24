namespace launcher.Services.Models
{
    public class Tag
    {
        public string id { get; set; }
        public string name { get; set; }
        public string slug { get; set; }
        public object description { get; set; }
        public object feature_image { get; set; }
        public string visibility { get; set; }
        public object og_image { get; set; }
        public object og_title { get; set; }
        public object og_description { get; set; }
        public object twitter_image { get; set; }
        public object twitter_title { get; set; }
        public object twitter_description { get; set; }
        public object meta_title { get; set; }
        public object meta_description { get; set; }
        public object codeinjection_head { get; set; }
        public object codeinjection_foot { get; set; }
        public object canonical_url { get; set; }
        public object accent_color { get; set; }
        public string url { get; set; }
    }
} 