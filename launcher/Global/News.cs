using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Windows.Media.Animation;
using System.Windows;
using static launcher.Global.References;
using System.Windows.Shapes;
using System.Windows.Media;
using launcher.BranchUtils;

namespace launcher.Global
{
    public static class News
    {
        private static List<List<NewsItem>> Pages = [[], [], [], []];

        const int MaxItemsPerCategory = 8;

        static int currentPage = 0;

        public static void Populate()
        {
            if (Pages[0].Count == 0)
            {
                Root CommunityItems = GetNewsItems("community");

                appDispatcher.BeginInvoke(() =>
                {
                    foreach (var post in CommunityItems.posts)
                    {
                        if (post.tags == null || post.tags.Count < 1)
                            continue;

                        var newsItem = CreateNewsItem(post);
                        Pages[0].Add(newsItem);
                    }
                });
            }

            if (Pages[1].Count == 0)
            {
                CreatePremadeNewLegends();
            }

            if (Pages[2].Count == 0)
            {
                Root CommsItems = GetNewsItems("comms");

                appDispatcher.BeginInvoke(() =>
                {
                    foreach (var post in CommsItems.posts)
                    {
                        if (post.tags == null || post.tags.Count < 1)
                            continue;

                        var newsItem = CreateNewsItem(post);
                        Pages[2].Add(newsItem);
                    }
                });
            }

            Root PatchNotesItems = GetNewsItems(GetBranch.Branch().patch_notes_blog_slug);

            appDispatcher.BeginInvoke(() =>
            {
                Pages[3].Clear();

                foreach (var post in PatchNotesItems.posts)
                {
                    if (post.tags == null || post.tags.Count < 1)
                        continue;

                    var newsItem = CreateNewsItem(post);
                    Pages[3].Add(newsItem);
                }
            });

            appDispatcher.BeginInvoke(() =>
            {
                if (currentPage == 3)
                    SetPage(3);
            });
        }

        private static NewsItem CreateNewsItem(Post post)
        {
            return new NewsItem(
                post.title,
                post.excerpt,
                post.primary_author.name,
                post.published_at.ToShortDateString(),
                post.url,
                post.feature_image,
                string.IsNullOrEmpty(post.feature_image)
            );
        }

        private static NewsItem CreateNewsItem(Post post, bool smallItem)
        {
            return new NewsItem(
                post.title,
                post.excerpt,
                post.primary_author.name,
                post.published_at.ToShortDateString(),
                post.url,
                post.feature_image,
                smallItem
            );
        }

        private static void CreatePremadeNewLegends()
        {
            Pages[1].Add(new NewsItem("Learn How to Play", "View a bunch of information ranging from tutorials, scripting, and more!", "", DateTime.Now.ToShortDateString(), "https://docs.r5reloaded.com/", "", false, "Welcome To R5R"));
            Pages[1].Add(new NewsItem("View Our Blog", "View out blog containing a bunch of usefull information and updates!", "", DateTime.Now.ToShortDateString(), "https://blog.r5reloaded.com/", "", true, "View Blog"));
            Pages[1].Add(new NewsItem("Join Our Discord", "Join our discord server to chat with other members of the community!", "", DateTime.Now.ToShortDateString(), "https://discord.com/invite/jqMkUdXrBr", "", true, "Join Discord"));
            Pages[1].Add(new NewsItem("Follow Us On X", "Follow us on x to stay up to date with the latest news and updates!", "", DateTime.Now.ToShortDateString(), "https://x.com/r5reloaded", "", true, "Follow Us"));
        }

        public static void SetPage(int index)
        {
            currentPage = index;

            List<NewsItem> selected = Pages[index];
            NewsPanel.Children.Clear();

            int i = 0;
            foreach (NewsItem newsItem in selected)
            {
                double speed1 = (bool)Ini.Get(Ini.Vars.Disable_Transitions) ? 1 : 500;
                double speed2 = (bool)Ini.Get(Ini.Vars.Disable_Transitions) ? 1 : 200;
                double beginTime = (bool)Ini.Get(Ini.Vars.Disable_Transitions) ? 0 : i * 0.1;
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

        public static Root GetNewsItems(string slug)
        {
            Root root = new();

            try
            {
                string filter = string.IsNullOrEmpty(slug) ? "" : $"&filter=tag:{slug}";
                //string order = sortByOldest ? "&order=published_at%20desc" : "&order=published_at%20asc";

                root = Networking.HttpClient.GetFromJsonAsync<Root>($"{Launcher.NEWSURL}/posts/?key={Launcher.NEWSKEY}&include=tags,authors{filter}&limit={MaxItemsPerCategory}").Result;
            }
            catch
            {
                Logger.LogWarning(Logger.Source.Launcher, "Failed to fetch news items.");
            }

            return root;
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