using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace launcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int lastSelectedIndex = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Helper.SetupApp(this);
            UpdateChecker updateChecker = new UpdateChecker(Dispatcher);

            if (Helper.isInstalled)
            {
                btnPlay.Content = "Play";
                cmbBranch.SelectedItem = Helper.serverConfig.branches.FirstOrDefault(b => b.branch == Helper.launcherConfig.currentUpdateBranch);
                Task.Run(() => updateChecker.Start());
            }
            else
            {
                btnPlay.Content = "Install";
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if (Helper.isInstalled)
            {
                if (Helper.updateRequired)
                {
                    Task.Run(() => Helper.gameUpdate.Start());
                }
                else
                {
                    Helper.LaunchGame();
                }
            }
            else
            {
                if (!Helper.isInstalling)
                {
                    Task.Run(() => Helper.gameInstall.Start());
                }
            }
        }

        private void mediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            mediaElement.Position = new TimeSpan(0, 0, 1);
            mediaElement.Play();
        }

        private void Rectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void cmbBranch_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox? comboBox = sender as ComboBox;

            if (comboBox == null)
                return;

            var selectedBranch = comboBox.SelectedIndex;

            if (Helper.serverConfig.branches[selectedBranch].enabled == false)
            {
                comboBox.SelectedIndex = lastSelectedIndex;
                return;
            }

            lastSelectedIndex = selectedBranch;

            if (Helper.isInstalled && Helper.serverConfig.branches[0].branch == Helper.launcherConfig.currentUpdateBranch)
            {
                Helper.updateRequired = false;
                btnPlay.Content = "Play";
            }
            else if (Helper.isInstalled && Helper.serverConfig.branches[0].branch != Helper.launcherConfig.currentUpdateBranch)
            {
                btnPlay.Content = "Update";
                Helper.updateRequired = true;
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsPopup.IsOpen = true;
        }

        private void StatusBtn_Click(object sender, RoutedEventArgs e)
        {
            StatusPopup.IsOpen = true;
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            if (StatusPopup.IsOpen)
            {
                var offset = StatusPopup.HorizontalOffset;
                StatusPopup.HorizontalOffset = offset + 1;
                StatusPopup.HorizontalOffset = offset;
            }
        }

        private void StatusPopup_Unloaded(object sender, EventArgs e)
        {
            StatusBtn.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        }

        private void StatusPopup_Loaded(object sender, EventArgs e)
        {
            StatusBtn.Background = new SolidColorBrush(Color.FromArgb(100, 0, 0, 0));
        }

        private void SubMenuBtn_Click(object sender, RoutedEventArgs e)
        {
            SubMenuPopup.IsOpen = true;
        }

        private void DownloadsBtn_Click(object sender, RoutedEventArgs e)
        {
            DownloadsPopup.IsOpen = true;
        }

        private void DownloadsPopup_Unloaded(object sender, EventArgs e)
        {
            DownloadsBtn.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        }

        private void DownloadsPopup_Loaded(object sender, EventArgs e)
        {
            DownloadsBtn.Background = new SolidColorBrush(Color.FromArgb(100, 0, 0, 0));
        }

        public void ShowSettingsControl()
        {
            var transistionInStoryboardHalf = new Storyboard();
            var doubleAnimation = new DoubleAnimation
            {
                From = -2400,
                To = 0,
                Duration = new Duration(TimeSpan.FromSeconds(0.25))
            };

            // Animate the Canvas.Left property
            Storyboard.SetTarget(doubleAnimation, TransitionRect);
            Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath("RenderTransform.Children[0].X"));

            // Add to storyboard and begin animation
            transistionInStoryboardHalf.Children.Add(doubleAnimation);

            transistionInStoryboardHalf.Completed += (s, e) =>
            {
                // Ensure the control is visible before starting the fade-in animation
                SettingsControl.Visibility = Visibility.Visible;

                // Retrieve the global fade-in storyboard or define it here
                var fadeInStoryboard = new Storyboard();
                fadeInStoryboard.Children.Add(new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = new Duration(TimeSpan.FromSeconds(0.2))
                });

                // Set the target of the animation to the SettingsControl
                Storyboard.SetTarget(fadeInStoryboard, SettingsControl);
                Storyboard.SetTargetProperty(fadeInStoryboard, new PropertyPath("Opacity"));

                fadeInStoryboard.Completed += (s, e) =>
                {
                    var transistionInStoryboardHalf2 = new Storyboard();
                    var doubleAnimation2 = new DoubleAnimation
                    {
                        From = 0,
                        To = 2400,
                        Duration = new Duration(TimeSpan.FromSeconds(0.25))
                    };

                    // Animate the Canvas.Left property
                    Storyboard.SetTarget(doubleAnimation2, TransitionRect);
                    Storyboard.SetTargetProperty(doubleAnimation2, new PropertyPath("RenderTransform.Children[0].X"));

                    // Add to storyboard and begin animation
                    transistionInStoryboardHalf2.Children.Add(doubleAnimation2);
                    transistionInStoryboardHalf2.Begin();
                };

                fadeInStoryboard.Begin();
            };

            transistionInStoryboardHalf.Begin();

            subMenuControl.Settings.IsEnabled = false;
        }

        public void HideSettingsControl()
        {
            var transistionInStoryboardHalf = new Storyboard();
            var doubleAnimation = new DoubleAnimation
            {
                From = 2400,
                To = 0,
                Duration = new Duration(TimeSpan.FromSeconds(0.5))
            };

            // Animate the Canvas.Left property
            Storyboard.SetTarget(doubleAnimation, TransitionRect);
            Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath("RenderTransform.Children[0].X"));

            // Add to storyboard and begin animation
            transistionInStoryboardHalf.Children.Add(doubleAnimation);

            transistionInStoryboardHalf.Completed += (s, e) =>
            {
                var fadeOutStoryboard = new Storyboard();
                fadeOutStoryboard.Children.Add(new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = new Duration(TimeSpan.FromSeconds(0.2))
                });

                // Clone the storyboard to make it modifiable
                fadeOutStoryboard.Completed += (s, e) =>
                {
                    // After the animation completes, set the control to Hidden
                    SettingsControl.Visibility = Visibility.Hidden;

                    var transistionInStoryboardHalf2 = new Storyboard();
                    var doubleAnimation2 = new DoubleAnimation
                    {
                        From = 0,
                        To = -2400,
                        Duration = new Duration(TimeSpan.FromSeconds(0.25))
                    };

                    // Animate the Canvas.Left property
                    Storyboard.SetTarget(doubleAnimation2, TransitionRect);
                    Storyboard.SetTargetProperty(doubleAnimation2, new PropertyPath("RenderTransform.Children[0].X"));

                    // Add to storyboard and begin animation
                    transistionInStoryboardHalf2.Children.Add(doubleAnimation2);
                    transistionInStoryboardHalf2.Begin();
                };

                // Set the target of the animation to the SettingsControl
                Storyboard.SetTarget(fadeOutStoryboard, SettingsControl);
                Storyboard.SetTargetProperty(fadeOutStoryboard, new PropertyPath("Opacity"));

                // Start the fade-out animation
                fadeOutStoryboard.Begin();
            };

            transistionInStoryboardHalf.Begin();

            subMenuControl.Settings.IsEnabled = true;
        }
    }
}