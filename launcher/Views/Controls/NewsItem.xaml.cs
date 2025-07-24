using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace launcher
{
    public partial class NewsItem : UserControl
    {
        private string link = "";

        public NewsItem()
        {
            InitializeComponent();
        }

        public NewsItem(string title, string excerpt, string author, string published, string url, string image, bool small = false, string overrideReadme = "")
        {
            InitializeComponent();

            Title.Text = title;
            Excerpt.Text = excerpt;
            link = url;
            Date.Text = published;
            Author.Text = "Author: " + author;

            if (!string.IsNullOrEmpty(overrideReadme))
                ReadMore.Text = overrideReadme;

            if (!string.IsNullOrEmpty(image))
                FeatImage.Source = new BitmapImage(new Uri(image));

            if (small)
            {
                NewsItemBorder.Width = 207;
                FeatImage.Visibility = Visibility.Hidden;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("cmd", $"/c start {link}") { CreateNoWindow = true });
        }
    }
}