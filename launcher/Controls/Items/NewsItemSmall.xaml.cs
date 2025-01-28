using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace launcher
{
    public partial class NewsItemSmall : UserControl
    {
        private string link = "";

        public NewsItemSmall()
        {
            InitializeComponent();
        }

        public NewsItemSmall(string title, string excerpt, string author, string published, string url, string overrideReadme = "")
        {
            InitializeComponent();

            Title.Text = title;
            Excerpt.Text = excerpt;
            link = url;

            if (!string.IsNullOrEmpty(overrideReadme))
                ReadMore.Text = overrideReadme;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("cmd", $"/c start {link}") { CreateNoWindow = true });
        }
    }
}