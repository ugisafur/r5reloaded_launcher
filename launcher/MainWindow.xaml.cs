using Hardcodet.Wpf.TaskbarNotification;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

using Color = System.Windows.Media.Color;

namespace launcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public TaskbarIcon System_Tray { get; set; }
        public ICommand ShowWindowCommand { get; }

        public MainWindow()
        {
            ShowWindowCommand = new RelayCommand(ExecuteShowWindow, CanExecuteShowWindow);
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Hide the window on startup
            this.Opacity = 0;

            Ini.CreateConfig();

            Utilities.SetupApp(this);

            await OnOpen();

            if (AppState.IsOnline)
            {
                Task.Run(() => UpdateChecker.Start());

                Play_Button.Content = Utilities.IsBranchInstalled() ? "PLAY" : "INSTALL";
                if (!Utilities.IsBranchInstalled() && File.Exists(Path.Combine(FileManager.GetBranchDirectory(), "r5apex.exe")))
                    Play_Button.Content = "REPAIR";
            }
            else
                Play_Button.Content = "PLAY";

            bool useStaticImage = Ini.Get(Ini.Vars.Disable_Background_Video, false);
            Background_Image.Visibility = useStaticImage ? Visibility.Visible : Visibility.Hidden;
            Background_Video.Visibility = useStaticImage ? Visibility.Hidden : Visibility.Visible;

            SetupSystemTray();
        }

        private void SetupSystemTray()
        {
            // Set the version number in the system tray context menu
            ContextMenu contextMenu = (ContextMenu)FindResource("tbiContextMenu");
            MenuItem versionMenuItem = contextMenu.Items.OfType<MenuItem>().FirstOrDefault(item => item.Name == "VersionContext");
            if (versionMenuItem != null)
                versionMenuItem.Header = "R5RLauncher " + Constants.Launcher.VERSION;

            System_Tray = new TaskbarIcon();
            System_Tray.ToolTipText = "R5Reloaded Launcher";
            System_Tray.Icon = this.Icon.ToIcon();
            System_Tray.DoubleClickCommand = ShowWindowCommand;
            System_Tray.ContextMenu = (ContextMenu)FindResource("tbiContextMenu");

            Application.Current.Exit += new ExitEventHandler(Current_Exit);
        }

        private void Current_Exit(object sender, ExitEventArgs e)
        {
            System_Tray.Dispose();
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
                Utilities.SendNotification("Launcher minimized to tray.", BalloonIcon.Info);
                OnClose();
            }
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if (!AppState.IsOnline)
            {
                Utilities.LaunchGame();
                return;
            }

            if (Utilities.IsBranchInstalled())
            {
                Utilities.LaunchGame();
            }
            else if (!AppState.IsInstalling)
            {
                if (!Utilities.IsBranchInstalled() && File.Exists(Path.Combine(FileManager.GetBranchDirectory(), "r5apex.exe")))
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
            Background_Video.Position = TimeSpan.FromSeconds(0);
            Background_Video.Play();
        }

        private void DragBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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

            ComboBranch comboBranch = (ComboBranch)Branch_Combobox.Items[selectedBranch];

            if (comboBranch.isLocalBranch || !AppState.IsOnline)
            {
                Ini.Set(Ini.Vars.SelectedBranch, comboBranch.title);
                Update_Button.Visibility = Visibility.Hidden;
                Play_Button.Content = "PLAY";
                Play_Button.IsEnabled = true;
                AppState.IsLocalBranch = true;
                GameSettings_Control.RepairGame_Button.IsEnabled = false;
                return;
            }

            AppState.IsLocalBranch = false;

            Ini.Set(Ini.Vars.SelectedBranch, Configuration.ServerConfig.branches[selectedBranch].branch);

            if (Utilities.IsBranchInstalled())
            {
                Utilities.SetupAdvancedMenu();

                if (!Configuration.ServerConfig.branches[selectedBranch].enabled)
                {
                    Update_Button.Visibility = Visibility.Hidden;
                    Play_Button.Content = "PLAY";
                    Play_Button.IsEnabled = true;
                    GameSettings_Control.RepairGame_Button.IsEnabled = false;
                    return;
                }

                if (Utilities.GetBranchVersion() == Configuration.ServerConfig.branches[0].currentVersion)
                {
                    Update_Button.Visibility = Visibility.Hidden;
                    Play_Button.Content = "PLAY";
                    Play_Button.IsEnabled = true;
                    GameSettings_Control.RepairGame_Button.IsEnabled = true;
                }
                else
                {
                    Update_Button.Visibility = Visibility.Visible;
                    Play_Button.Content = "PLAY";
                    Play_Button.IsEnabled = true;
                    GameSettings_Control.RepairGame_Button.IsEnabled = true;
                }
            }
            else
            {
                if (!Configuration.ServerConfig.branches[selectedBranch].enabled)
                {
                    Update_Button.Visibility = Visibility.Hidden;
                    Play_Button.Content = "DISABLED";
                    Play_Button.IsEnabled = false;
                    GameSettings_Control.RepairGame_Button.IsEnabled = false;
                    return;
                }

                if (File.Exists(Path.Combine(FileManager.GetBranchDirectory(), "r5apex.exe")))
                {
                    Update_Button.Visibility = Visibility.Hidden;
                    Play_Button.Content = "REPAIR";
                    Play_Button.IsEnabled = true;
                    GameSettings_Control.RepairGame_Button.IsEnabled = true;
                }
                else
                {
                    Update_Button.Visibility = Visibility.Hidden;
                    Play_Button.Content = "INSTALL";
                    Play_Button.IsEnabled = true;
                    GameSettings_Control.RepairGame_Button.IsEnabled = false;
                }
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            GameSettings_Popup.IsOpen = true;
        }

        private void StatusBtn_Click(object sender, RoutedEventArgs e)
        {
            Status_Popup.IsOpen = true;
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            if (Status_Popup.IsOpen)
            {
                var offset = Status_Popup.HorizontalOffset;
                Status_Popup.HorizontalOffset = offset + 1;
                Status_Popup.HorizontalOffset = offset;
            }
        }

        private void StatusPopup_Unloaded(object sender, EventArgs e)
        {
            Status_Button.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        }

        private void StatusPopup_Loaded(object sender, EventArgs e)
        {
            Status_Button.Background = new SolidColorBrush(Color.FromArgb(100, 0, 0, 0));
        }

        private void SubMenuBtn_Click(object sender, RoutedEventArgs e)
        {
            Menu_Popup.IsOpen = true;
        }

        private void DownloadsBtn_Click(object sender, RoutedEventArgs e)
        {
            Downloads_Popup.IsOpen = true;
        }

        private void DownloadsPopup_Unloaded(object sender, EventArgs e)
        {
            Downloads_Button.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        }

        private void DownloadsPopup_Loaded(object sender, EventArgs e)
        {
            Downloads_Button.Background = new SolidColorBrush(Color.FromArgb(100, 0, 0, 0));
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Environment.Exit(0);
        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (Configuration.ServerConfig.branches[Utilities.GetCmbBranchIndex()].update_available && Utilities.IsBranchInstalled())
            {
                Task.Run(() => GameUpdate.Start());
                Update_Button.Visibility = Visibility.Hidden;
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