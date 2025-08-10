using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Diagnostics;
using static launcher.Core.AppContext;
using static launcher.Core.AppController;
using launcher.Core.Models;
using launcher.Services;
using launcher.Game;
using launcher.GameLifecycle.Models;

namespace launcher
{
    public partial class GameItem : UserControl
    {
        #region Dependency Properties and Properties

        private const double CollapsedHeight = 65;
        private const double ExpandedHeight = 663;
        private const int AnimationDurationMs = 500;

        public static readonly DependencyProperty CornerRadiusValueProperty =
            DependencyProperty.Register("CornerRadiusValue", typeof(CornerRadius), typeof(GameItem), new PropertyMetadata(new CornerRadius(0)));

        public CornerRadius CornerRadiusValue
        {
            get => (CornerRadius)GetValue(CornerRadiusValueProperty);
            set => SetValue(CornerRadiusValueProperty, value);
        }

        public bool IsFirstItem { get; set; }
        public bool IsLastItem { get; set; }
        public int Index { get; set; } = 0;
        public ReleaseChannel ReleaseChannel { get; private set; }

        private bool _isExpanded = true;

        #endregion

        public GameItem()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes the GameItem with release channel data and populates its UI.
        /// Changed to `async Task` to follow best practices (avoid `async void`).
        /// </summary>
        public async Task InitializeAsync(ReleaseChannel channel)
        {
            ReleaseChannel = channel;

            UpdateUI();

            GameManifest langFiles = await ApiService.GetLanguageFilesAsync(channel);
            PopulateLanguageCheckboxes(langFiles);
        }

        /// <summary>
        /// Public method to trigger a UI refresh from outside.
        /// </summary>
        public void Refresh()
        {
            UpdateUI();
        }

        #region UI Update Logic

        /// <summary>
        /// Centralized method to update all UI elements based on the current state.
        /// This removes the code duplication between the old SetupGameItem and UpdateGameItem.
        /// </summary>
        private void UpdateUI()
        {
            if (ReleaseChannel == null) return;

            bool isInstalled = ReleaseChannelService.IsInstalled(ReleaseChannel);
            bool isEnabled = ReleaseChannel.enabled;
            string dediUrl = ReleaseChannelService.GetDediURL(ReleaseChannel);

            ReleaseChannelName.Text = ReleaseChannelService.GetName(true, ReleaseChannel);
            InstallPath.Text = ReleaseChannelService.GetDirectory(ReleaseChannel);

            // Set button enabled state
            bool canInteract = !appState.IsInstalling;
            UninstallGame.IsEnabled = canInteract;
            InstallGame.IsEnabled = canInteract;
            VerifyGame.IsEnabled = canInteract;
            InstallOpt.IsEnabled = canInteract;
            Dedi.IsEnabled = !string.IsNullOrEmpty(dediUrl);

            // Set visibility based on release channel enabled/installed status
            UninstallGame.Visibility = isEnabled && isInstalled ? Visibility.Visible : Visibility.Hidden;
            InstallGame.Visibility = isEnabled && !isInstalled ? Visibility.Visible : Visibility.Hidden;
            VerifyGame.Visibility = isEnabled ? Visibility.Visible : Visibility.Hidden;
            DisabledTxt.Visibility = isEnabled ? Visibility.Hidden : Visibility.Visible;

            bool canShowOpt = isEnabled && isInstalled;
            InstallOpt.Visibility = canShowOpt ? Visibility.Visible : Visibility.Hidden;
            if (canShowOpt)
            {
                InstallOpt.Content = ReleaseChannelService.ShouldDownloadHDTextures(ReleaseChannel) ? "UNINSTALL HD TEXTURES" : "INSTALL HD TEXTURES";
            }

            // Set Dedi Name
            dediName.Text = !string.IsNullOrEmpty(dediUrl) ? Path.GetFileNameWithoutExtension(dediUrl) : "";
        }

        /// <summary>
        /// Clears and populates the language selection grid.
        /// </summary>
        private void PopulateLanguageCheckboxes(GameManifest langFiles)
        {
            LangBox.Children.Clear();

            // English is always present and checked by default
            LangBox.Children.Add(CreateLanguageCheckbox("english", isEnabled: false, isChecked: true));

            int row = 0;
            int column = 1;
            foreach (string lang in langFiles.languages)
            {
                if (column > 4)
                {
                    column = 0;
                    row++;
                }

                var langCheckBox = CreateLanguageCheckbox(lang, isChecked: DoesLangFileExist(lang));

                langCheckBox.Checked += (sender, e) => HandleLanguageAction(async () => await GameInstaller.LangFile((CheckBox)sender, langFiles, lang));
                langCheckBox.Unchecked += (sender, e) => HandleLanguageAction(async () => await GameUninstaller.LangFile((CheckBox)sender, lang));

                langCheckBox.SetValue(Grid.RowProperty, row);
                langCheckBox.SetValue(Grid.ColumnProperty, column);
                LangBox.Children.Add(langCheckBox);
                column++;
            }
        }

        /// <summary>
        /// Factory method to create a styled CheckBox for a language.
        /// </summary>
        private CheckBox CreateLanguageCheckbox(string lang, bool isEnabled = true, bool isChecked = false)
        {
            bool canInteract = !appState.IsInstalling && isEnabled && ReleaseChannelService.IsInstalled(ReleaseChannel);

            return new CheckBox
            {
                Content = new CultureInfo("en-US").TextInfo.ToTitleCase(lang),
                IsChecked = isChecked,
                IsEnabled = canInteract,
                FontFamily = new System.Windows.Media.FontFamily("{StaticResource SansationBold}"),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                FontSize = 14,
                Foreground = System.Windows.Media.Brushes.White
            };
        }

        /// <summary>
        /// Checks if both required files for a given language exist.
        /// </summary>
        private bool DoesLangFileExist(string lang)
        {
            string dir = ReleaseChannelService.GetDirectory(ReleaseChannel);
            string langLower = lang.ToLower(CultureInfo.InvariantCulture);

            // Use Path.Combine for safe and correct path building.
            string path1 = Path.Combine(dir, "audio", "ship", $"general_{langLower}.mstr");
            string path2 = Path.Combine(dir, "audio", "ship", $"general_{langLower}_patch_1.mstr");

            return File.Exists(path1) && File.Exists(path2);
        }

        #endregion

        #region Expansion Animation

        private void TopButton_Click(object sender, RoutedEventArgs e)
        {
            AnimateExpansion(!_isExpanded);
        }

        /// <summary>
        /// Handles both expanding and collapsing animations.
        /// </summary>
        public void AnimateExpansion(bool expand)
        {
            _isExpanded = expand;
            CollapseIcon.Text = expand ? "-" : "+";
            double targetHeight = expand ? ExpandedHeight : CollapsedHeight;

            int duration = (bool)SettingsService.Get(SettingsService.Vars.Disable_Animations) ? 1 : AnimationDurationMs;
            var storyboard = new Storyboard();
            var animation = new DoubleAnimation
            {
                // FIX: Explicitly set the starting point of the animation.
                From = this.ActualHeight,
                To = targetHeight,
                Duration = new Duration(TimeSpan.FromMilliseconds(duration)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            Storyboard.SetTarget(animation, this);
            Storyboard.SetTargetProperty(animation, new PropertyPath("Height"));
            storyboard.Children.Add(animation);
            storyboard.Begin();

            UpdateCornerRadius(expand);
        }

        /// <summary>
        /// Updates the corner radius of elements based on position and expansion state.
        /// </summary>
        private void UpdateCornerRadius(bool isExpanded)
        {
            // Define the standard radius values for clarity
            var noRadius = new CornerRadius(0);
            var topRadius = new CornerRadius(10, 10, 0, 0);
            var bottomRadius = new CornerRadius(0, 0, 10, 10);
            var allRadius = new CornerRadius(10);

            CornerRadius itemShapeRadius;

            if (IsFirstItem && IsLastItem)
            {
                // Case: This is the ONLY item in the list.
                itemShapeRadius = allRadius;
            }
            else if (IsFirstItem)
            {
                // Case: This is the FIRST of multiple items.
                itemShapeRadius = topRadius;
            }
            else if (IsLastItem)
            {
                // Case: This is the LAST of multiple items.
                itemShapeRadius = bottomRadius;
            }
            else
            {
                // Case: This is a MIDDLE item.
                itemShapeRadius = noRadius;
            }

            MainBG.CornerRadius = itemShapeRadius;

            if (isExpanded)
            {
                // When EXPANDED, the TopBar only needs rounded top corners if it's the first item.
                TopBar.CornerRadius = IsFirstItem ? topRadius : noRadius;
            }
            else
            {
                // When COLLAPSED, the TopBar IS the item, so it must take the overall item shape.
                TopBar.CornerRadius = itemShapeRadius;
            }

            if (TopButton.Template.FindName("btnborder", TopButton) is Border border)
            {
                border.CornerRadius = TopBar.CornerRadius;
            }
        }

        #endregion

        #region Click Event Handlers

        /// <summary>
        /// Helper to run before any action to prevent execution if busy and to select the channel.
        /// </summary>
        private bool CanExecuteAction()
        {
            if (appState.IsInstalling) return false;
            ReleaseChannel_Combobox.SelectedIndex = Index;
            HideSettingsControl();
            return true;
        }

        /// <summary>
        /// Wrapper for language checkbox events to reduce code duplication.
        /// </summary>
        private void HandleLanguageAction(Func<Task> action)
        {
            if (appState.IsInstalling) return;
            ReleaseChannel_Combobox.SelectedIndex = Index;
            Task.Run(action);
        }

        private void VerifyGame_Click(object sender, RoutedEventArgs e)
        {
            if (!CanExecuteAction()) return;
            if (ReleaseChannelService.IsInstalled(ReleaseChannel))
                Task.Run(() => GameRepairer.Start());
        }

        private void UninstallGame_Click(object sender, RoutedEventArgs e)
        {
            if (!CanExecuteAction()) return;
            if (ReleaseChannelService.IsInstalled(ReleaseChannel))
                Task.Run(() => GameUninstaller.Start());
        }

        private void InstallGame_Click(object sender, RoutedEventArgs e)
        {
            if (!CanExecuteAction()) return;
            if (!ReleaseChannelService.IsInstalled(ReleaseChannel))
                Task.Run(() => GameInstaller.Start());
        }

        private void InstallOpt_Click(object sender, RoutedEventArgs e)
        {
            if (!CanExecuteAction()) return;

            if (ReleaseChannelService.ShouldDownloadHDTextures(ReleaseChannel))
                Task.Run(() => GameUninstaller.HDTextures(ReleaseChannel));
            else
                ShowDownloadOptlFiles();
        }

        private void Dedi_Click(object sender, RoutedEventArgs e)
        {
            string url = ReleaseChannelService.GetDediURL(ReleaseChannel);
            if (!string.IsNullOrEmpty(url))
            {
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            }
        }

        #endregion
    }
}