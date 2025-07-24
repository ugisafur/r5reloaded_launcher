using Microsoft.WindowsAPICodePack.Dialogs;
using System.Collections;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static launcher.Core.UiReferences;
using static launcher.Services.LoggerService;
using Application = System.Windows.Application;

namespace launcher
{
    /// <summary>
    /// Interaction logic for ThemeEditor.xaml
    /// </summary>
    public partial class ThemeEditor : Window
    {
        private List<ColorPicker.PortableColorPicker> colorsControls = [];

        public ThemeEditor()
        {
            InitializeComponent();
        }

        public void SetupThemeEditor()
        {
            var app = (App)Application.Current;

            colorsControls.Add(ThemePrimary);
            colorsControls.Add(ThemeSecondary);
            colorsControls.Add(ThemeSecondaryAlt);
            colorsControls.Add(ThemePrimaryText);
            colorsControls.Add(ThemePrimaryAltText);
            colorsControls.Add(ThemeSecondaryText);
            colorsControls.Add(ThemeSecondaryAltText);
            colorsControls.Add(ThemeDisabledText);
            colorsControls.Add(ThemeSeperator);
            colorsControls.Add(ThemeOtherButtonText);
            colorsControls.Add(ThemeOtherButtonHover);
            colorsControls.Add(ThemeOtherButtonAltText);
            colorsControls.Add(ThemeMainButtonsBackground);
            colorsControls.Add(ThemeMainButtonsBorder);
            colorsControls.Add(ThemeMainButtonsBorderHover);
            colorsControls.Add(ThemeUpdateButtonBackground);
            colorsControls.Add(ThemeUpdateButtonBackgroundHover);
            colorsControls.Add(ThemeMenuButtonColorHover);
            colorsControls.Add(ThemeMenuButtonColorDisabled);
            colorsControls.Add(ThemeComboBoxBorder);
            colorsControls.Add(ThemeComboBoxMouseOver);
            colorsControls.Add(ThemeComboBoxSelected);
            colorsControls.Add(ThemeComboBoxSelectedMouseOver);
            colorsControls.Add(ThemeUninstallButtonText);
            colorsControls.Add(ThemeUninstallButtonHover);
            colorsControls.Add(ThemeStatusPlayerServerCount);
            colorsControls.Add(ThemeStatusOperational);
            colorsControls.Add(ThemeStatusNonOperational);

            foreach (ColorPicker.PortableColorPicker color in colorsControls)
            {
                SolidColorBrush brush = app.ThemeDictionary[GetName(color)] as SolidColorBrush;
                color.SelectedColor = brush.Color;
                color.SecondaryColor = brush.Color;
                color.HintColor = brush.Color;
                color.ColorChanged += ColorChanged;

                app.ThemeDictionary[GetName(color)] = new SolidColorBrush(color.SelectedColor);
            }

            if(Launcher.wineEnv)
            {
                ChangeVideo.IsEnabled = false;
                ChangeVideo.Content = "Video Disabled Under Wine";
            }
        }

        private DateTime lastUpdate = DateTime.Now;

        private void ColorChanged(object sender, RoutedEventArgs e)
        {
            if (DateTime.Now.Subtract(lastUpdate).TotalMilliseconds < 100)
                return;

            lastUpdate = DateTime.Now;
            var app = (App)Application.Current;
            app.ThemeDictionary[GetName(sender)] = new SolidColorBrush(((ColorPicker.PortableColorPicker)sender).SelectedColor);
        }

        private void ExportFullTheme(string filePath)
        {
            // Get the theme dictionary (assumed to be the first merged dictionary)
            var app = (App)Application.Current;
            ResourceDictionary themeDictionary = app.ThemeDictionary;

            // Use a StringBuilder to construct the XAML
            var sb = new StringBuilder();

            // Write the header for the ResourceDictionary
            sb.AppendLine(@"<ResourceDictionary xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""");
            sb.AppendLine(@"                    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">");

            // Iterate through each key/value pair in the dictionary
            foreach (DictionaryEntry entry in themeDictionary)
            {
                string key = entry.Key.ToString();

                // Handle SolidColorBrush specially
                if (entry.Value is SolidColorBrush brush)
                {
                    // Convert the Color to a hex string (ARGB)
                    string hex = $"#{brush.Color.A:X2}{brush.Color.R:X2}{brush.Color.G:X2}{brush.Color.B:X2}";
                    sb.AppendLine($"    <SolidColorBrush x:Key=\"{key}\" Color=\"{hex}\" />");
                }
                else
                {
                    // For other types, fall back to XamlWriter (or handle them manually as needed)
                    try
                    {
                        string serialized = System.Windows.Markup.XamlWriter.Save(entry.Value);
                        // Indent the serialized XAML properly (optional)
                        sb.AppendLine("    " + serialized.Replace(Environment.NewLine, Environment.NewLine + "    "));
                    }
                    catch (Exception ex)
                    {
                        // If serialization fails, you might log or handle the error
                        sb.AppendLine($"    <!-- Unable to serialize resource with key '{key}': {ex.Message} -->");
                    }
                }
            }

            // Close the ResourceDictionary tag
            sb.AppendLine("</ResourceDictionary>");

            // Write the constructed XAML to file
            File.WriteAllText(filePath, sb.ToString());
        }

        private static string GetName(object obj)
        {
            // First see if it is a FrameworkElement
            var element = obj as FrameworkElement;
            if (element != null)
                return element.Name;
            // If not, try reflection to get the value of a Name property.
            try { return (string)obj.GetType().GetProperty("Name").GetValue(obj, null); }
            catch
            {
                // Last of all, try reflection to get the value of a Name field.
                try { return (string)obj.GetType().GetField("Name").GetValue(obj); }
                catch { return null; }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string exportPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]), "launcher_data\\cfg\\theme.xaml");
            ExportFullTheme(exportPath);
            LogInfo(LogSource.Launcher, $"Theme changes exported to {exportPath}");
        }

        private void StartupImage_Click(object sender, RoutedEventArgs e)
        {
            var directoryDialog = new CommonOpenFileDialog
            {
                IsFolderPicker = false,
                Title = "Select PNG File",
                Filters = { new CommonFileDialogFilter("PNG Files", "*.png") }
            };

            if (directoryDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                Directory.CreateDirectory(System.IO.Path.Combine(Launcher.PATH, "launcher_data\\assets"));

                File.Copy(directoryDialog.FileName, System.IO.Path.Combine(Launcher.PATH, "launcher_data\\assets", "startup.png"), true);

                LogInfo(LogSource.Launcher, "Loading local startup image");
            }
        }

        private async void BackgroundVideo_Click(object sender, RoutedEventArgs e)
        {
            if (Launcher.wineEnv)
                return;

            var directoryDialog = new CommonOpenFileDialog
            {
                IsFolderPicker = false,
                Title = "Select Mp4 File",
                Filters = { new CommonFileDialogFilter("Mp4 Files", "*.mp4") }
            };

            if (directoryDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                Background_Video.Stop();
                Background_Video.Close();
                Background_Video.ClearValue(MediaElement.SourceProperty);

                Directory.CreateDirectory(System.IO.Path.Combine(Launcher.PATH, "launcher_data\\assets"));

                await Task.Delay(500);

                File.Copy(directoryDialog.FileName, System.IO.Path.Combine(Launcher.PATH, "launcher_data\\assets", "background.mp4"), true);

                Background_Video.Source = new Uri(System.IO.Path.Combine(Launcher.PATH, "launcher_data\\assets", "background.mp4"), UriKind.Absolute);
                LogInfo(LogSource.Launcher, "Loading local video background");
            }
        }

        private void BackgroundImageClick(object sender, RoutedEventArgs e)
        {
            var directoryDialog = new CommonOpenFileDialog
            {
                IsFolderPicker = false,
                Title = "Select PNG File",
                Filters = { new CommonFileDialogFilter("PNG Files", "*.png") }
            };

            if (directoryDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                Directory.CreateDirectory(System.IO.Path.Combine(Launcher.PATH, "launcher_data\\assets"));

                File.Copy(directoryDialog.FileName, System.IO.Path.Combine(Launcher.PATH, "launcher_data\\assets", "background.png"), true);

                string imagePath = System.IO.Path.Combine(Launcher.PATH, "launcher_data\\assets", "background.png");
                if (File.Exists(imagePath))
                {
                    var bitmap = new BitmapImage();
                    using (var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = stream;
                        bitmap.EndInit();
                    }
                    bitmap.Freeze();
                    Background_Image.Source = bitmap;
                }

                LogInfo(LogSource.Launcher, "Loading local image background");
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            themeEditor = null;
        }
    }
}