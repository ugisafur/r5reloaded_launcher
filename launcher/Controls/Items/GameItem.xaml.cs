using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using static launcher.Global.References;
using System.IO;
using launcher.Game;
using launcher.Global;
using System.Diagnostics;

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
        public Branch gameBranch { get; set; }

        public GameItem()
        {
            InitializeComponent();
        }

        public void UpdateGameItem()
        {
            BranchName.Text = $"{GetBranch.Name(true, gameBranch)}";
            InstallPath.Text = $"{GetBranch.Directory(gameBranch)}";

            Dedi.IsEnabled = !string.IsNullOrEmpty(GetBranch.DediURL(gameBranch));
            UninstallGame.IsEnabled = !AppState.IsInstalling;
            InstallGame.IsEnabled = !AppState.IsInstalling;
            VerifyGame.IsEnabled = !AppState.IsInstalling;
            InstallOpt.IsEnabled = !AppState.IsInstalling;

            if (gameBranch.enabled)
            {
                UninstallGame.Visibility = GetBranch.Installed(gameBranch) ? Visibility.Visible : Visibility.Hidden;
                InstallGame.Visibility = GetBranch.Installed(gameBranch) ? Visibility.Hidden : Visibility.Visible;
                VerifyGame.Visibility = Visibility.Visible;
                BranchDisabledTxt.Visibility = Visibility.Hidden;
            }
            else
            {
                UninstallGame.Visibility = Visibility.Hidden;
                InstallGame.Visibility = Visibility.Hidden;
                VerifyGame.Visibility = Visibility.Hidden;
                BranchDisabledTxt.Visibility = Visibility.Visible;
            }

            InstallOpt.Visibility = Visibility.Hidden;

            if (GetBranch.Enabled(gameBranch) && GetBranch.Installed(gameBranch))
            {
                InstallOpt.Visibility = Visibility.Visible;
                InstallOpt.Content = GetBranch.DownloadHDTextures(gameBranch) ? "UNINSTALL HD TEXTURES" : "INSTALL HD TEXTURES";
            }

            if (!string.IsNullOrEmpty(GetBranch.DediURL(gameBranch)))
            {
                string[] dedisplit = GetBranch.DediURL(gameBranch).Replace("https://", "").Split('/');
                dediName.Text = dedisplit[dedisplit.Length - 1].Replace(".zip", "").Replace(".rar", "").Replace(".7z", "");
            }
            else
            {
                dediName.Text = "";
            }
        }

        public void SetupGameItem(Branch branch)
        {
            gameBranch = branch;
            branchName = branch.branch;

            BranchName.Text = $"R5Reloaded - {GetBranch.Name(true, branch)}";
            InstallPath.Text = $"{GetBranch.Directory(branch)}";
            
            Dedi.IsEnabled = !string.IsNullOrEmpty(GetBranch.DediURL(gameBranch));
            UninstallGame.IsEnabled = !AppState.IsInstalling;
            InstallGame.IsEnabled = !AppState.IsInstalling;
            VerifyGame.IsEnabled = !AppState.IsInstalling;
            InstallOpt.IsEnabled = !AppState.IsInstalling;

            if(gameBranch.enabled)
            {
                UninstallGame.Visibility = GetBranch.Installed(branch) ? Visibility.Visible : Visibility.Hidden;
                InstallGame.Visibility = GetBranch.Installed(branch) ? Visibility.Hidden : Visibility.Visible;
                VerifyGame.Visibility = Visibility.Visible;
                BranchDisabledTxt.Visibility = Visibility.Hidden;
            }
            else
            {
                UninstallGame.Visibility = Visibility.Hidden;
                InstallGame.Visibility = Visibility.Hidden;
                VerifyGame.Visibility = Visibility.Hidden;
                BranchDisabledTxt.Visibility = Visibility.Visible;
            }

            InstallOpt.Visibility = Visibility.Hidden;
            if (GetBranch.Enabled(branch) && GetBranch.Installed(branch))
            {
                InstallOpt.Visibility = Visibility.Visible;
                InstallOpt.Content = GetBranch.DownloadHDTextures(branch) ? "UINSTALL HD TEXTURES" : "INSTALL HD TEXTURES";
            }

            if (!string.IsNullOrEmpty(GetBranch.DediURL(gameBranch)))
            {
                string[] dedisplit = GetBranch.DediURL(branch).Replace("https://", "").Split('/');
                dediName.Text = dedisplit[dedisplit.Length - 1].Replace(".zip", "").Replace(".rar", "").Replace(".7z", "");
            }
            else
            {
                dediName.Text = "";
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
                    FontFamily = new System.Windows.Media.FontFamily("{StaticResource SansationBold}"),
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
                    langCheckBox.IsEnabled = lang.ToLower(new CultureInfo("en-US")) == "english" ? false : GetBranch.Installed(branch);
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
            if (!File.Exists($"{GetBranch.Directory(gameBranch)}\\audio\\ship\\general_{lang.ToLower(new CultureInfo("en-US"))}.mstr"))
                return false;

            if (!File.Exists($"{GetBranch.Directory(gameBranch)}\\audio\\ship\\general_{lang.ToLower(new CultureInfo("en-US"))}_patch_1.mstr"))
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
                To = 663,
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

            if (GetBranch.Installed(gameBranch))
                Task.Run(() => Repair.Start());

            Managers.App.HideSettingsControl();
        }

        private void UninstallGame_Click(object sender, RoutedEventArgs e)
        {
            if (AppState.IsInstalling)
                return;

            Branch_Combobox.SelectedIndex = index;

            if (GetBranch.Installed(gameBranch))
                Task.Run(() => Uninstall.Start());

            Managers.App.HideSettingsControl();
        }

        private void InstallGame_Click(object sender, RoutedEventArgs e)
        {
            if (AppState.IsInstalling)
                return;

            Branch_Combobox.SelectedIndex = index;

            if (!GetBranch.Installed(gameBranch))
                Task.Run(() => Install.Start());

            Managers.App.HideSettingsControl();
        }

        private void InstallOpt_Click(object sender, RoutedEventArgs e)
        {
            if (AppState.IsInstalling)
                return;

            Branch_Combobox.SelectedIndex = index;
            Managers.App.HideSettingsControl();

            if (GetBranch.DownloadHDTextures(gameBranch))
                Task.Run(() => Uninstall.HDTextures(gameBranch));
            else
                Managers.App.ShowDownloadOptlFiles();
        }

        private void Dedi_Click(object sender, RoutedEventArgs e)
        {
            if(!string.IsNullOrEmpty(GetBranch.DediURL(gameBranch)))
            {
                Process.Start(new ProcessStartInfo("cmd", $"/c start {GetBranch.DediURL(gameBranch)}") { CreateNoWindow = true });
            }
        }
    }
}