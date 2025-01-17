using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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

        public GameItem()
        {
            InitializeComponent();
        }

        public void SetupGameItem(Branch branch)
        {
            BranchName.Text = $"R5Reloaded - {branch.branch.ToUpper()}";
            InstallPath.Text = $"{(string)Ini.Get(Ini.Vars.Library_Location)}\\R5R Library\\{branch.branch.ToUpper()}";
            UninstallGame.Visibility = Ini.Get(branch.branch, "Is_Installed", false) ? Visibility.Visible : Visibility.Hidden;
            InstallGame.Visibility = Ini.Get(branch.branch, "Is_Installed", false) ? Visibility.Hidden : Visibility.Visible;
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
            Duration animationDuration = new Duration(TimeSpan.FromMilliseconds(duration));
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
            Duration animationDuration = new Duration(TimeSpan.FromMilliseconds(duration));
            var easing = new CubicEase { EasingMode = EasingMode.EaseInOut };

            var heightAnimation = new DoubleAnimation
            {
                From = (int)this.Height,
                To = 443,
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
    }
}