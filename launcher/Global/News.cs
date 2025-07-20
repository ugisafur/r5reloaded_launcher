using System.Net.Http.Json;
using System.Text.Json;
using System.Windows.Media.Animation;
using System.Windows;
using static launcher.Global.References;
using System.Windows.Shapes;
using System.IO;

namespace launcher.Global
{
    public static class News
    {
        private static List<List<NewsItem>> Pages = [[], [], [], []];
        private const int MaxItemsPerCategory = 8;
        private static bool firstTimePopulate = true;
        private static int currentPage = 0;
        private static Dictionary<string, bool> blogItemsCached = [];

        public static void Populate()
        {
            // Populate the community category
            if (Pages[0].Count == 0)
                PopulateNewsCatagory("community", 0, false);

            // Populate the new legends category
            if (Pages[1].Count == 0)
            {
                Pages[1].Add(new NewsItem("Learn How to Play", "View a bunch of information ranging from tutorials, scripting, and more!", "", DateTime.Now.ToShortDateString(), "https://docs.r5reloaded.com/", "", false, "Welcome To R5R"));
                Pages[1].Add(new NewsItem("View Our Blog", "View out blog containing a bunch of usefull information and updates!", "", DateTime.Now.ToShortDateString(), "https://blog.r5reloaded.com/", "", true, "View Blog"));
                Pages[1].Add(new NewsItem("Join Our Discord", "Join our discord server to chat with other members of the community!", "", DateTime.Now.ToShortDateString(), "https://discord.com/invite/jqMkUdXrBr", "", true, "Join Discord"));
                Pages[1].Add(new NewsItem("Follow Us On X", "Follow us on x to stay up to date with the latest news and updates!", "", DateTime.Now.ToShortDateString(), "https://x.com/r5reloaded", "", true, "Follow Us"));
            }

            // Populate the support-us category
            if (Pages[2].Count == 0)
                PopulateNewsCatagory("support-us", 2, false);

            // Populate the patch notes category
            if (!GetBranch.IsLocalBranch())
                PopulateNewsCatagory(GetBranch.BlogSlug(), 3, true);

            appDispatcher.BeginInvoke(() =>
            {
                if (currentPage == 3)
                    SetPage(3);

                if(firstTimePopulate)
                {
                    SetPage(0);
                    firstTimePopulate = false;
                }
            });
        }

        private static void PopulateNewsCatagory(string slug, int index, bool shouldCache)
        {
            Root root = new();

            if (blogItemsCached.TryGetValue(slug, out bool value) && value)
                root = GetCachedNewsItems(slug);
            else
                root = GetNewsItems(slug, shouldCache);

            appDispatcher.BeginInvoke(() =>
            {
                Pages[index].Clear();

                foreach (var post in root.posts)
                {
                    if (post.tags == null || post.tags.Count < 1)
                        continue;

                    var newsItem = new NewsItem(
                        post.title,
                        post.excerpt,
                        post.authors[0].name,
                        post.published_at.ToShortDateString(),
                        post.url,
                        post.feature_image,
                        string.IsNullOrEmpty(post.feature_image)
                    );

                    Pages[index].Add(newsItem);
                }
            });
        }

        public static void SetPage(int index)
        {
            currentPage = index;

            List<NewsItem> selected = Pages[index];
            NewsPanel.Children.Clear();

            int i = 0;
            foreach (NewsItem newsItem in selected)
            {
                double speed1 = (bool)Ini.Get(Ini.Vars.Disable_Animations) ? 1 : 500;
                double speed2 = (bool)Ini.Get(Ini.Vars.Disable_Animations) ? 1 : 200;
                double beginTime = (bool)Ini.Get(Ini.Vars.Disable_Animations) ? 0 : i * 0.1;
                i++;

                newsItem.BeginAnimation(UIElement.OpacityProperty, null);
                newsItem.BeginAnimation(FrameworkElement.MarginProperty, null);

                NewsPanel.Children.Add(newsItem);

                newsItem.Opacity = 0;
                newsItem.VerticalAlignment = VerticalAlignment.Top;
                newsItem.Margin = new Thickness(0, 100, 0, 0);

                Rectangle separator = new Rectangle
                {
                    Opacity = 0,
                    Width = 20
                };

                NewsPanel.Children.Add(separator);

                Storyboard storyboard = new Storyboard();

                DoubleAnimation fadeInAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(speed1),
                    BeginTime = TimeSpan.FromSeconds(beginTime),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(fadeInAnimation, newsItem);
                Storyboard.SetTargetProperty(fadeInAnimation, new PropertyPath(UIElement.OpacityProperty));
                storyboard.Children.Add(fadeInAnimation);

                ThicknessAnimation slideUpAnimation = new ThicknessAnimation
                {
                    From = new Thickness(0, 50, 0, 0),
                    To = new Thickness(0, 30, 0, 0),
                    Duration = TimeSpan.FromMilliseconds(speed2),
                    BeginTime = TimeSpan.FromSeconds(beginTime),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(slideUpAnimation, newsItem);
                Storyboard.SetTargetProperty(slideUpAnimation, new PropertyPath(FrameworkElement.MarginProperty));
                storyboard.Children.Add(slideUpAnimation);

                storyboard.Begin();

                Task.Delay(500);
            }
        }

        private static Root GetNewsItems(string slug, bool shouldCache)
        {
            Root root = new();

            try
            {
                string filter = string.IsNullOrEmpty(slug) ? "" : $"&filter=tag:{slug}";
                //string order = sortByOldest ? "&order=published_at%20desc" : "&order=published_at%20asc";
                root = Networking.HttpClient.GetFromJsonAsync<Root>($"{Launcher.NEWSURL}/posts/?key={Launcher.NEWSKEY}&include=tags,authors{filter}&limit={MaxItemsPerCategory}&fields=title,excerpt,published_at,url,feature_image").Result;
            }
            catch
            {
                Logger.LogWarning(Logger.Source.Launcher, "Failed to fetch news items.");
            }

            if (shouldCache)
            {
                try
                {
                    string filePath = System.IO.Path.Combine(Launcher.PATH, "launcher_data\\cache", $"{slug}.json");
                    if (File.Exists(filePath))
                        File.Delete(filePath);

                    File.WriteAllText(filePath, JsonSerializer.Serialize(root));
                    blogItemsCached[slug] = true;

                    Logger.LogInfo(Logger.Source.Launcher, $"Cached news items for {slug}.");
                }
                catch (Exception ex)
                {
                    Logger.LogError(Logger.Source.Launcher, $"Failed to cache news items: {ex.Message}");
                }
            }

            return root;
        }

        private static Root GetCachedNewsItems(string slug)
        {
            Root root = new();

            string filePath = System.IO.Path.Combine(Launcher.PATH, "launcher_data\\cache", $"{slug}.json");
            if (!File.Exists(filePath))
                return root;

            string json = File.ReadAllText(filePath);

            if (string.IsNullOrEmpty(json))
                return root;

            try
            {
                root = JsonSerializer.Deserialize<Root>(json);
            }
            catch (JsonException ex)
            {
                Logger.LogError(Logger.Source.Launcher, $"Failed to deserialize JSON: {ex.Message}");
            }

            return root;
        }

        public static void CachedCleared()
        {
            blogItemsCached.Clear();
        }
    }

    public class Author
    {
        public string id { get; set; }
        public string name { get; set; }
        public string slug { get; set; }
        public string profile_image { get; set; }
        public object cover_image { get; set; }
        public object bio { get; set; }
        public string website { get; set; }
        public object location { get; set; }
        public object facebook { get; set; }
        public string twitter { get; set; }
        public object meta_title { get; set; }
        public object meta_description { get; set; }
        public string url { get; set; }
    }

    public class Meta
    {
        public Pagination pagination { get; set; }
    }

    public class Pagination
    {
        public int page { get; set; }
        public int limit { get; set; }
        public int pages { get; set; }
        public int total { get; set; }
        public object next { get; set; }
        public object prev { get; set; }
    }

    public class Post
    {
        public string id { get; set; }
        public string uuid { get; set; }
        public string title { get; set; }
        public string slug { get; set; }
        public string html { get; set; }
        public string comment_id { get; set; }
        public string feature_image { get; set; }
        public bool featured { get; set; }
        public string visibility { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public DateTime published_at { get; set; }
        public object custom_excerpt { get; set; }
        public object codeinjection_head { get; set; }
        public object codeinjection_foot { get; set; }
        public object custom_template { get; set; }
        public object canonical_url { get; set; }
        public List<Tag> tags { get; set; }
        public List<Author> authors { get; set; }
        public PrimaryAuthor primary_author { get; set; }
        public PrimaryTag primary_tag { get; set; }
        public string url { get; set; }
        public string excerpt { get; set; }
        public int reading_time { get; set; }
        public bool access { get; set; }
        public bool comments { get; set; }
        public object og_image { get; set; }
        public object og_title { get; set; }
        public object og_description { get; set; }
        public object twitter_image { get; set; }
        public object twitter_title { get; set; }
        public object twitter_description { get; set; }
        public object meta_title { get; set; }
        public object meta_description { get; set; }
        public object email_subject { get; set; }
        public object frontmatter { get; set; }
        public object feature_image_alt { get; set; }
        public object feature_image_caption { get; set; }
    }

    public class PrimaryAuthor
    {
        public string id { get; set; }
        public string name { get; set; }
        public string slug { get; set; }
        public string profile_image { get; set; }
        public object cover_image { get; set; }
        public object bio { get; set; }
        public string website { get; set; }
        public object location { get; set; }
        public object facebook { get; set; }
        public string twitter { get; set; }
        public object meta_title { get; set; }
        public object meta_description { get; set; }
        public string url { get; set; }
    }

    public class PrimaryTag
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

    public class Root
    {
        public List<Post> posts { get; set; }
        public Meta meta { get; set; }
    }

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