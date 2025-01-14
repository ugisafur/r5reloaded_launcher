using Hardcodet.Wpf.TaskbarNotification;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using static launcher.Global;
using Color = System.Windows.Media.Color;

namespace launcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public TaskbarIcon taskbar;
        public ICommand ShowWindowCommand { get; }

        public MainWindow()
        {
            ShowWindowCommand = new RelayCommand(ExecuteShowWindow, CanExecuteShowWindow);
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Opacity = 0;

            TaskbarIcon tbi = new()
            {
                ToolTipText = "R5Reloaded Launcher",
                Icon = this.Icon.ToIcon(),
                DoubleClickCommand = ShowWindowCommand,
                ContextMenu = (ContextMenu)FindResource("tbiContextMenu")
            };

            ContextMenu contextMenu = (ContextMenu)FindResource("tbiContextMenu");
            MenuItem versionMenuItem = contextMenu.Items.OfType<MenuItem>().FirstOrDefault(item => item.Name == "VersionContext");
            if (versionMenuItem != null)
                versionMenuItem.Header = "R5RLauncher " + LAUNCHER_VERSION;

            taskbar = tbi;

            Ini.CreateConfig();

            Utilities.SetupApp(this);

            await OnOpen();

            if (IS_ONLINE)
            {
                Task.Run(() => UpdateChecker.Start());

                btnPlay.Content = Utilities.isSelectedBranchInstalled() ? "PLAY" : "INSTALL";
                if (!Utilities.isSelectedBranchInstalled() && File.Exists(Path.Combine(FileManager.GetBranchDirectory(), "r5apex.exe")))
                    btnPlay.Content = "REPAIR";
            }
            else
            {
                btnPlay.Content = "PLAY";
            }

            bool useStaticImage = Ini.Get(Ini.Vars.Disable_Background_Video, false);
            mediaImage.Visibility = useStaticImage ? Visibility.Visible : Visibility.Hidden;
            mediaElement.Visibility = useStaticImage ? Visibility.Hidden : Visibility.Visible;
        }

        public async Task OnOpen()
        {
            if (Ini.Get(Ini.Vars.Disable_Animations, false))
            {
                this.Opacity = 1;
                return;
            }

            await Task.Delay(100);

            // Create a storyboard for simultaneous animations
            var storyboard = new Storyboard();

            // Duration for the animations
            Duration animationDuration = new Duration(TimeSpan.FromSeconds(0.5));

            // Easing function for smoothness
            var easing = new CubicEase { EasingMode = EasingMode.EaseInOut };

            // Animate ScaleX from 1 to 0
            var scaleXAnimation = new DoubleAnimation
            {
                From = 0.75,
                To = 1.0,
                Duration = animationDuration,
                EasingFunction = easing
            };
            Storyboard.SetTarget(scaleXAnimation, this);
            Storyboard.SetTargetProperty(scaleXAnimation, new PropertyPath("RenderTransform.ScaleX"));

            // Animate ScaleY from 1 to 0
            var scaleYAnimation = new DoubleAnimation
            {
                From = 0.75,
                To = 1.0,
                Duration = animationDuration,
                EasingFunction = easing
            };
            Storyboard.SetTarget(scaleYAnimation, this);
            Storyboard.SetTargetProperty(scaleYAnimation, new PropertyPath("RenderTransform.ScaleY"));

            // Animate Opacity from 1 to 0
            var opacityAnimation = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = animationDuration,
                EasingFunction = easing
            };
            Storyboard.SetTarget(opacityAnimation, this);
            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath("Opacity"));

            // Add animations to the storyboard
            storyboard.Children.Add(scaleXAnimation);
            storyboard.Children.Add(scaleYAnimation);
            storyboard.Children.Add(opacityAnimation);

            // Create a TaskCompletionSource to await the animation
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            storyboard.Completed += (s, e) => tcs.SetResult(true);

            // Begin the storyboard
            storyboard.Begin();

            // Await the completion of the animation
            await tcs.Task;
        }

        private async Task OnClose()
        {
            if (Ini.Get(Ini.Vars.Disable_Animations, false))
            {
                this.Hide();
                return;
            }

            // Create a storyboard for simultaneous animations
            var storyboard = new Storyboard();

            // Duration for the animations
            Duration animationDuration = new Duration(TimeSpan.FromSeconds(0.5));

            // Easing function for smoothness
            var easing = new CubicEase { EasingMode = EasingMode.EaseInOut };

            // Animate ScaleX from 1 to 0
            var scaleXAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.75,
                Duration = animationDuration,
                EasingFunction = easing
            };
            Storyboard.SetTarget(scaleXAnimation, this);
            Storyboard.SetTargetProperty(scaleXAnimation, new PropertyPath("RenderTransform.ScaleX"));

            // Animate ScaleY from 1 to 0
            var scaleYAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.75,
                Duration = animationDuration,
                EasingFunction = easing
            };
            Storyboard.SetTarget(scaleYAnimation, this);
            Storyboard.SetTargetProperty(scaleYAnimation, new PropertyPath("RenderTransform.ScaleY"));

            // Animate Opacity from 1 to 0
            var opacityAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = animationDuration,
                EasingFunction = easing
            };
            Storyboard.SetTarget(opacityAnimation, this);
            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath("Opacity"));

            // Add animations to the storyboard
            storyboard.Children.Add(scaleXAnimation);
            storyboard.Children.Add(scaleYAnimation);
            storyboard.Children.Add(opacityAnimation);

            // Create a TaskCompletionSource to await the animation
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            storyboard.Completed += (s, e) => tcs.SetResult(true);

            // Begin the storyboard
            storyboard.Begin();

            // Await the completion of the animation
            await tcs.Task;

            // Hide the window after animation
            this.Hide();

            this.Opacity = 1;
            WindowScale.ScaleX = 1;
            WindowScale.ScaleY = 1;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            if (Ini.Get(Ini.Vars.Enable_Quit_On_Close, false))
                Application.Current.Shutdown();
            else
            {
                taskbar.ShowBalloonTip("R5R Launcher", "Launcher minimized to tray.", BalloonIcon.Info);
                OnClose();
            }
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if (!IS_ONLINE)
            {
                Utilities.LaunchGame();
                return;
            }

            if (Utilities.isSelectedBranchInstalled())
            {
                Utilities.LaunchGame();
            }
            else if (!IS_INSTALLING)
            {
                if (!Utilities.isSelectedBranchInstalled() && File.Exists(Path.Combine(FileManager.GetBranchDirectory(), "r5apex.exe")))
                {
                    Task.Run(() => GameRepair.Start());
                }
                else
                {
                    Task.Run(() => GameInstall.Start());
                }
            }
        }

        private void mediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            mediaElement.Position = TimeSpan.FromSeconds(0);
            mediaElement.Play();
        }

        private void Rectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void cmbBranch_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox comboBox) return;

            var selectedBranch = comboBox.SelectedIndex;

            ComboBranch comboBranch = (ComboBranch)cmbBranch.Items[selectedBranch];

            if (comboBranch.isLocalBranch || !IS_ONLINE)
            {
                Ini.Set(Ini.Vars.SelectedBranch, comboBranch.title);
                btnUpdate.Visibility = Visibility.Hidden;
                btnPlay.Content = "PLAY";
                btnPlay.IsEnabled = true;
                IS_LOCAL_BRANCH = true;
                SettingsPopupControl.btnRepair.IsEnabled = false;
                return;
            }

            IS_LOCAL_BRANCH = false;

            Ini.Set(Ini.Vars.SelectedBranch, SERVER_CONFIG.branches[selectedBranch].branch);

            if (Utilities.isSelectedBranchInstalled())
            {
                if (!SERVER_CONFIG.branches[selectedBranch].enabled)
                {
                    btnUpdate.Visibility = Visibility.Hidden;
                    btnPlay.Content = "PLAY";
                    btnPlay.IsEnabled = true;
                    SettingsPopupControl.btnRepair.IsEnabled = false;
                    return;
                }

                if (Utilities.GetCurrentInstalledBranchVersion() == SERVER_CONFIG.branches[0].currentVersion)
                {
                    btnUpdate.Visibility = Visibility.Hidden;
                    btnPlay.Content = "PLAY";
                    btnPlay.IsEnabled = true;
                    SettingsPopupControl.btnRepair.IsEnabled = true;
                }
                else
                {
                    btnUpdate.Visibility = Visibility.Visible;
                    btnPlay.Content = "PLAY";
                    btnPlay.IsEnabled = true;
                    SettingsPopupControl.btnRepair.IsEnabled = true;
                }
            }
            else
            {
                if (!SERVER_CONFIG.branches[selectedBranch].enabled)
                {
                    btnUpdate.Visibility = Visibility.Hidden;
                    btnPlay.Content = "DISABLED";
                    btnPlay.IsEnabled = false;
                    SettingsPopupControl.btnRepair.IsEnabled = false;
                    return;
                }

                if (File.Exists(Path.Combine(LAUNCHER_PATH, "r5apex.exe")))
                {
                    btnUpdate.Visibility = Visibility.Hidden;
                    btnPlay.Content = "REPAIR";
                    btnPlay.IsEnabled = true;
                    SettingsPopupControl.btnRepair.IsEnabled = true;
                }
                else
                {
                    btnUpdate.Visibility = Visibility.Hidden;
                    btnPlay.Content = "INSTALL";
                    btnPlay.IsEnabled = true;
                    SettingsPopupControl.btnRepair.IsEnabled = false;
                }
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Environment.Exit(0);
        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].update_available && Utilities.isSelectedBranchInstalled())
            {
                Task.Run(() => GameUpdate.Start());
                btnUpdate.Visibility = Visibility.Hidden;
            }
        }

        private void ExecuteShowWindow()
        {
            // Show the main window
            Application.Current.Dispatcher.Invoke(() =>
            {
                var mainWindow = Application.Current.MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.Show();
                    mainWindow.WindowState = WindowState.Normal;
                    mainWindow.Activate();
                    OnOpen();
                }
            });
        }

        private bool CanExecuteShowWindow()
        {
            // Logic to determine if the command can execute
            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void VisitWebsite_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("cmd", $"/c start https://r5reloaded.com") { CreateNoWindow = true });
        }

        private void JoinDiscord_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("cmd", $"/c start https://discord.com/invite/jqMkUdXrBr") { CreateNoWindow = true });
        }
    }

    public class RelayCommand(Action execute, Func<bool> canExecute = null) : ICommand
    {
        private readonly Action _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        private readonly Func<bool> _canExecute = canExecute;

        public bool CanExecute(object parameter) =>
            _canExecute == null || _canExecute();

        public void Execute(object parameter) =>
            _execute();

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}