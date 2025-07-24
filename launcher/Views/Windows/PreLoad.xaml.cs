using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media;

namespace launcher
{
    public partial class PreLoad : Window
    {
        public PreLoad()
        {
            InitializeComponent();
        }

        public void SetLoadingText(string text)
        {
            Task.Run(() =>
            {
                Dispatcher.InvokeAsync(() =>
                {
                    LoadingText.Text = text;
                });
            });
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }
    }
}