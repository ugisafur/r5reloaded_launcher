using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using static launcher.Classes.Global.References;
using launcher.Classes.Global;
using launcher.Classes.Game;
using launcher.Classes.Utilities;
using launcher.Classes.Managers;
using System.IO;

namespace launcher
{
    /// <summary>
    /// Interaction logic for GameItem.xaml
    /// </summary>
    public partial class GameItem : UserControl
    {
        public static readonly DependencyProperty CornerRadiusValueProperty =
        DependencyProperty.Register("CornerRadiusValue", typeof(CornerRadius), typeof(GameItem), new PropertyMetadata(new CornerRadius(0)));

        public CornerRadius CornerRadiusValue
        {
            get { return (CornerRadius)GetValue(CornerRadiusValueProperty); }
            set { SetValue(CornerRadiusValueProperty, value); }
        }

        private bool isExpanded = true;
        public bool isFirstItem = false;
        public bool isLastItem = false;
        public string branchName = "";
        public int index = 0;

        public GameItem()
        {
            InitializeComponent();
        }

        public void SetupGameItem(Branch branch)
        {
            BranchName.Text = $"R5Reloaded - {branch.branch.ToUpper(new CultureInfo("en-US"))}";
            InstallPath.Text = $"{(string)Ini.Get(Ini.Vars.Library_Location)}\\R5R Library\\{branch.branch.ToUpper(new CultureInfo("en-US"))}";
            UninstallGame.Visibility = Ini.Get(branch.branch, "Is_Installed", false) ? Visibility.Visible : Visibility.Hidden;
            InstallGame.Visibility = Ini.Get(branch.branch, "Is_Installed", false) ? Visibility.Hidden : Visibility.Visible;
            branchName = branch.branch;

            UninstallGame.IsEnabled = !AppState.IsInstalling;
            InstallGame.IsEnabled = !AppState.IsInstalling;
            VerifyGame.IsEnabled = !AppState.IsInstalling;
            InstallOpt.IsEnabled = !AppState.IsInstalling;

            UninstallGame.Visibility = branch.enabled ? Visibility.Visible : Visibility.Hidden;
            InstallGame.Visibility = branch.enabled ? Visibility.Visible : Visibility.Hidden;
            VerifyGame.Visibility = branch.enabled ? Visibility.Visible : Visibility.Hidden;
            BranchDisabledTxt.Visibility = branch.enabled ? Visibility.Hidden : Visibility.Visible;

            if (branch.enabled && Ini.Get(branch.branch, "Is_Installed", false))
            {
                InstallOpt.Visibility = Ini.Get(branch.branch, "Download_HD_Textures", false) ? Visibility.Hidden : Visibility.Visible;
                HDTexturesInstalledTxt.Visibility = Ini.Get(branch.branch, "Download_HD_Textures", false) ? Visibility.Visible : Visibility.Hidden;
            }
            else
            {
                InstallOpt.Visibility = Visibility.Hidden;
                HDTexturesInstalledTxt.Visibility = Visibility.Hidden;
            }

            int row = 0;
            int column = 0;

            LangBox.Children.Clear();

            foreach (string lang in branch.mstr_languages)
            {
                if (column > 4)
                {
                    column = 0;
                    row++;
                }

                CheckBox langCheckBox = new CheckBox
                {
                    Content = new CultureInfo("en-US").TextInfo.ToTitleCase(lang),
                    IsChecked = lang.ToLower(new CultureInfo("en-US")) == "english" ? true : DoesLangFileExist(branch, lang),
                    FontFamily = new System.Windows.Media.FontFamily("Bahnschrift SemiBold"),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    FontSize = 14,
                    Foreground = System.Windows.Media.Brushes.White,
                };

                if (AppState.IsInstalling)
                {
                    langCheckBox.IsEnabled = false;
                }
                else
                {
                    langCheckBox.IsEnabled = lang.ToLower(new CultureInfo("en-US")) == "english" ? false : Classes.BranchUtils.GetBranch.Installed(branch);
                }

                langCheckBox.Checked += (sender, e) =>
                {
                    Branch_Combobox.SelectedIndex = index;
                    Task.Run(() => Install.LangFile(langCheckBox, [lang]));
                    Downloads_Popup.IsOpen = true;
                };

                langCheckBox.Unchecked += (sender, e) =>
                {
                    Branch_Combobox.SelectedIndex = index;
                    Task.Run(() => Uninstall.LangFile(langCheckBox, [lang]));
                };

                LangBox.Children.Add(langCheckBox);
                langCheckBox.SetValue(Grid.RowProperty, row);
                langCheckBox.SetValue(Grid.ColumnProperty, column);

                column++;
            }
        }

        private bool DoesLangFileExist(Branch branch, string lang)
        {
            if (!File.Exists($"{(string)Ini.Get(Ini.Vars.Library_Location)}\\R5R Library\\{branch.branch.ToUpper(new CultureInfo("en-US"))}\\audio\\ship\\general_{lang.ToLower(new CultureInfo("en-US"))}.mstr"))
                return false;

            if (!File.Exists($"{(string)Ini.Get(Ini.Vars.Library_Location)}\\R5R Library\\{branch.branch.ToUpper(new CultureInfo("en-US"))}\\audio\\ship\\general_{lang.ToLower(new CultureInfo("en-US"))}_patch_1.mstr"))
                return false;

            return true;
        }

        private void TopButton_Click(object sender, RoutedEventArgs e)
        {
            if (isExpanded)
            {
                CollapseItem();
            }
            else
            {
                ExpandItem();
            }
        }

        public void CollapseItem()
        {
            isExpanded = false;
            CollapseIcon.Text = "+";

            int duration = (bool)Ini.Get(Ini.Vars.Disable_Animations) ? 1 : 500;

            var storyboard = new Storyboard();
            Duration animationDuration = new(TimeSpan.FromMilliseconds(duration));
            var easing = new CubicEase { EasingMode = EasingMode.EaseInOut };

            var heightAnimation = new DoubleAnimation
            {
                From = (int)this.Height,
                To = 65,
                Duration = animationDuration,
                EasingFunction = easing
            };
            Storyboard.SetTarget(heightAnimation, this);
            Storyboard.SetTargetProperty(heightAnimation, new PropertyPath("Height"));

            storyboard.Children.Add(heightAnimation);

            storyboard.Begin();

            var border = TopButton.Template.FindName("btnborder", TopButton) as Border;

            if (isFirstItem)
            {
                TopBar.CornerRadius = new CornerRadius(10, 10, 0, 0);
                MainBG.CornerRadius = new CornerRadius(10, 10, 0, 0);

                if (border != null)
                    border.CornerRadius = new CornerRadius(10, 10, 0, 0);
            }
            else if (isLastItem)
            {
                TopBar.CornerRadius = new CornerRadius(0, 0, 10, 10);
                MainBG.CornerRadius = new CornerRadius(0, 0, 10, 10);

                if (border != null)
                    border.CornerRadius = new CornerRadius(0, 0, 10, 10);
            }
            else
            {
                TopBar.CornerRadius = new CornerRadius(0, 0, 0, 0);
                MainBG.CornerRadius = new CornerRadius(0, 0, 0, 0);

                if (border != null)
                    border.CornerRadius = new CornerRadius(0, 0, 0, 0);
            }
        }

        public void ExpandItem()
        {
            isExpanded = true;
            CollapseIcon.Text = "-";

            int duration = (bool)Ini.Get(Ini.Vars.Disable_Animations) ? 1 : 500;

            var storyboard = new Storyboard();
            Duration animationDuration = new(TimeSpan.FromMilliseconds(duration));
            var easing = new CubicEase { EasingMode = EasingMode.EaseInOut };

            var heightAnimation = new DoubleAnimation
            {
                From = (int)this.Height,
                To = 603,
                Duration = animationDuration,
                EasingFunction = easing
            };
            Storyboard.SetTarget(heightAnimation, this);
            Storyboard.SetTargetProperty(heightAnimation, new PropertyPath("Height"));

            storyboard.Children.Add(heightAnimation);

            storyboard.Begin();

            var border = TopButton.Template.FindName("btnborder", TopButton) as Border;

            if (isFirstItem)
            {
                TopBar.CornerRadius = new CornerRadius(10, 10, 0, 0);
                MainBG.CornerRadius = new CornerRadius(10, 10, 0, 0);

                if (border != null)
                    border.CornerRadius = new CornerRadius(10, 10, 0, 0);
            }
            else if (isLastItem)
            {
                TopBar.CornerRadius = new CornerRadius(0, 0, 0, 0);
                MainBG.CornerRadius = new CornerRadius(0, 0, 10, 10);

                if (border != null)
                    border.CornerRadius = new CornerRadius(0, 0, 0, 0);
            }
            else
            {
                TopBar.CornerRadius = new CornerRadius(0, 0, 0, 0);
                MainBG.CornerRadius = new CornerRadius(0, 0, 0, 0);

                if (border != null)
                    border.CornerRadius = new CornerRadius(0, 0, 0, 0);
            }
        }

        private void VerifyGame_Click(object sender, RoutedEventArgs e)
        {
            if (AppState.IsInstalling)
                return;

            Branch_Combobox.SelectedIndex = index;
            Task.Run(() => Repair.Start());
            AppManager.HideSettingsControl();
        }

        private void UninstallGame_Click(object sender, RoutedEventArgs e)
        {
            if (AppState.IsInstalling)
                return;

            Branch_Combobox.SelectedIndex = index;
            Task.Run(() => Uninstall.Start());
            AppManager.HideSettingsControl();
        }

        private void InstallGame_Click(object sender, RoutedEventArgs e)
        {
            if (AppState.IsInstalling)
                return;

            Branch_Combobox.SelectedIndex = index;
            Task.Run(() => Install.Start());
            AppManager.HideSettingsControl();
        }

        private void InstallOpt_Click(object sender, RoutedEventArgs e)
        {
            if (AppState.IsInstalling)
                return;

            Branch_Combobox.SelectedIndex = index;
            AppManager.HideSettingsControl();
            AppManager.ShowDownloadOptlFiles();
        }
    }
}