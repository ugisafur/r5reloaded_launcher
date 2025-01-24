using launcher.Classes.News;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace launcher
{
    /// <summary>
    /// Interaction logic for NewsItem.xaml
    /// </summary>
    public partial class NewsItemSmall : UserControl
    {
        private string link = "";

        public NewsItemSmall()
        {
            InitializeComponent();
        }

        public NewsItemSmall(string title, string excerpt, string author, string published, string url)
        {
            InitializeComponent();

            Title.Text = title;
            Excerpt.Text = excerpt;
            link = url;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("cmd", $"/c start {link}") { CreateNoWindow = true });
        }
    }
}