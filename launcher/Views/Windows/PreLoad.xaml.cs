using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows.Controls;

namespace launcher
{
    public partial class PreLoad : Window
    {
        List<TextBlock> textBlocks = new();
        public PreLoad()
        {
            InitializeComponent();

            textBlocks.Add(LoadingText1);
            textBlocks.Add(LoadingText2);
            textBlocks.Add(LoadingText3);
            textBlocks.Add(LoadingText4);
            textBlocks.Add(LoadingText5);
            textBlocks.Add(LoadingText6);

            foreach (TextBlock tb in textBlocks)
                tb.Text = "";
        }

        public void SetLoadingText(string text)
        {
            Dispatcher.InvokeAsync(() =>
            {
                if (textBlocks == null || textBlocks.Count == 0)
                    return;

                for (int i = textBlocks.Count - 1; i > 0; i--)
                {
                    textBlocks[i].Text = textBlocks[i - 1].Text;
                }

                textBlocks[0].Text = text;
            });
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }
    }
}