using launcher.Classes.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Windows.Media.Animation;
using System.Windows;
using static launcher.Classes.Global.References;
using System.Windows.Shapes;
using System.Windows.Media;
using launcher.Classes.Utilities;

namespace launcher.Classes.News
{
    public static class Items
    {
        public static List<UIElement> Community = [];
        public static List<UIElement> NewLegends = [];
        public static List<UIElement> Comms = [];
        public static List<UIElement> PatchNotes = [];
        private static List<List<UIElement>> Pages = [];

        public static void Populate()
        {
            Root newsitems = GetNewsItems();
            foreach (Post post in newsitems.posts)
            {
                if (post.tags.Count < 1)
                    continue;

                if (post.tags[0].name == "Community")
                {
                    if (Community.Count < 8)
                        Community.Add(new NewsItem(post.title, post.excerpt, post.primary_author.name, post.published_at.ToShortDateString(), post.url, post.feature_image));
                }
                else if (post.tags[0].name == "Comms")
                {
                    if (Comms.Count < 8)
                        Comms.Add(new NewsItem(post.title, post.excerpt, post.primary_author.name, post.published_at.ToShortDateString(), post.url, post.feature_image));
                }
                else if (post.tags[0].name == "Patch Notes")
                {
                    if (PatchNotes.Count < 8)
                        PatchNotes.Add(new NewsItemSmall(post.title, post.excerpt, post.primary_author.name, post.published_at.ToShortDateString(), post.url));
                }
            }

            CreatePremadeNewLegends();

            Pages.Add(Community);
            Pages.Add(NewLegends);
            Pages.Add(Comms);
            Pages.Add(PatchNotes);
        }

        private static void CreatePremadeNewLegends()
        {
            NewLegends.Add(new NewsItem("Learn How to Play", "View a bunch of information ranging from tutorials, scripting, and more!", "", DateTime.Now.ToShortDateString(), "https://docs.r5reloaded.com/", "", "Welcome To R5R"));
            NewLegends.Add(new NewsItemSmall("View Our Blog", "View out blog containing a bunch of usefull information and updates!", "", DateTime.Now.ToShortDateString(), "https://blog.r5reloaded.com/", "View Blog"));
            NewLegends.Add(new NewsItemSmall("Join Our Discord", "Join our discord server to chat with other members of the community!", "", DateTime.Now.ToShortDateString(), "https://discord.com/invite/jqMkUdXrBr", "Join Discord"));
            NewLegends.Add(new NewsItemSmall("Follow Us On Twitter", "Follow us on twitter to stay up to date with the latest news and updates!", "", DateTime.Now.ToShortDateString(), "https://twitter.com/r5reloaded", "Follow Twitter"));
        }

        public static async void SetPage(int index)
        {
            List<UIElement> selected = Pages[index];
            NewsPanel.Children.Clear();

            for (int i = 0; i < selected.Count; i++)
            {
                double speed1 = (bool)Ini.Get(Ini.Vars.Disable_Transitions) ? 1 : 500;
                double speed2 = (bool)Ini.Get(Ini.Vars.Disable_Transitions) ? 1 : 200;
                double beginTime = (bool)Ini.Get(Ini.Vars.Disable_Transitions) ? 0 : i * 0.1;

                UIElement item = selected[i];
                NewsItem newsItem = item as NewsItem;
                NewsItemSmall newsItemSmall = item as NewsItemSmall;

                item.BeginAnimation(UIElement.OpacityProperty, null);
                item.BeginAnimation(FrameworkElement.MarginProperty, null);

                NewsPanel.Children.Add(item);

                if (item is NewsItem)
                {
                    newsItem.Opacity = 0;
                    newsItem.VerticalAlignment = VerticalAlignment.Top;
                    newsItem.Margin = new Thickness(0, 100, 0, 0);
                }
                else if (item is NewsItemSmall)
                {
                    newsItemSmall.Opacity = 0;
                    newsItemSmall.VerticalAlignment = VerticalAlignment.Top;
                    newsItemSmall.Margin = new Thickness(0, 100, 0, 0);
                }

                Rectangle separator = new Rectangle
                {
                    Opacity = 0,
                    Width = 20
                };

                NewsPanel.Children.Add(separator);

                // Create a storyboard for the animation
                Storyboard storyboard = new Storyboard();

                // Fade-in animation
                DoubleAnimation fadeInAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(speed1),
                    BeginTime = TimeSpan.FromSeconds(beginTime),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(fadeInAnimation, item);
                Storyboard.SetTargetProperty(fadeInAnimation, new PropertyPath(UIElement.OpacityProperty));
                storyboard.Children.Add(fadeInAnimation);

                // Slide-up animation
                ThicknessAnimation slideUpAnimation = new ThicknessAnimation
                {
                    From = new Thickness(0, 50, 0, 0),
                    To = new Thickness(0, 30, 0, 0),
                    Duration = TimeSpan.FromMilliseconds(speed2),
                    BeginTime = TimeSpan.FromSeconds(beginTime),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(slideUpAnimation, item);
                Storyboard.SetTargetProperty(slideUpAnimation, new PropertyPath(FrameworkElement.MarginProperty));
                storyboard.Children.Add(slideUpAnimation);

                // Start the storyboard
                storyboard.Begin();

                Task.Delay(500);
            }
        }

        public static Root GetNewsItems()
        {
            return HttpClientJsonExtensions.GetFromJsonAsync<Root>(Networking.HttpClient, $"{Launcher.NEWSURL}/posts/?key={Launcher.NEWSKEY}&include=tags,authors", new JsonSerializerOptions { AllowTrailingCommas = true }).Result;
        }
    }

    public static class Connection
    {
        public static bool Test()
        {
            try
            {
                using var client = new System.Net.WebClient();
                using var stream = client.OpenRead($"{Launcher.NEWSURL}/posts/?key={Launcher.NEWSKEY}&include=tags,authors");
                return true;
            }
            catch
            {
                return false;
            }
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