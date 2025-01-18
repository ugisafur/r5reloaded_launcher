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

        private void DragBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Setup global exception handlers
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            // Hide the window on startup
            this.Opacity = 0;

            // Create the configuration file if it doesn't exist
            Ini.CreateConfig();

            // Setup the system tray
            SetupSystemTray();

            // Setup the application
            Utilities.SetupApp(this);

            // Show window open animation
            await OnOpen();

            if (AppState.IsOnline)
            {
                Task.Run(() => UpdateChecker.Start());
                SetButtonState();
            }
            else
                Play_Button.Content = "PLAY";

            bool useStaticImage = (bool)Ini.Get(Ini.Vars.Disable_Background_Video);
            Background_Image.Visibility = useStaticImage ? Visibility.Visible : Visibility.Hidden;
            Background_Video.Visibility = useStaticImage ? Visibility.Hidden : Visibility.Visible;
        }

        private void Current_Exit(object sender, ExitEventArgs e)
        {
            System_Tray.Dispose();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Environment.Exit(0);
        }

        private void mediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            Background_Video.Position = TimeSpan.FromSeconds(0);
            Background_Video.Play();
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                Logger.LogCrashToFile(ex);
            }
        }

        private static void TaskScheduler_UnobservedTaskException(object sender, System.Threading.Tasks.UnobservedTaskExceptionEventArgs e)
        {
            Logger.LogCrashToFile(e.Exception);
            e.SetObserved();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)Ini.Get(Ini.Vars.Enable_Quit_On_Close))
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
            if (!AppState.IsOnline || Utilities.IsBranchInstalled() || Utilities.GetCurrentBranch().is_local_branch)
            {
                Utilities.LaunchGame();
                return;
            }

            if (!AppState.IsInstalling)
            {
                if (!Utilities.IsBranchInstalled() && File.Exists(Path.Combine(Utilities.GetBranchDirectory(), "r5apex.exe")))
                {
                    Task.Run(() => GameRepair.Start());
                }
                else
                {
                    if (Utilities.IsBranchEULAAccepted())
                    {
                        Task.Run(() => GameInstall.Start());
                    }
                    else
                    {
                        Utilities.ShowEULA();
                    }
                }
            }
        }

        private void cmbBranch_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox comboBox) return;

            var selectedBranch = comboBox.SelectedIndex;
            var comboBranch = (ComboBranch)Branch_Combobox.Items[selectedBranch];

            if (comboBranch.isLocalBranch || !AppState.IsOnline)
            {
                HandleLocalBranch(comboBranch.title);
                return;
            }

            AppState.IsLocalBranch = false;
            Ini.Set(Ini.Vars.SelectedBranch, Configuration.ServerConfig.branches[selectedBranch].branch);

            if (Utilities.IsBranchInstalled())
            {
                HandleInstalledBranch(selectedBranch);
            }
            else
            {
                HandleUninstalledBranch(selectedBranch);
            }
        }

        private void HandleLocalBranch(string branchTitle)
        {
            Ini.Set(Ini.Vars.SelectedBranch, branchTitle);
            Update_Button.Visibility = Visibility.Hidden;
            SetPlayState("PLAY", true, false, true);
            AppState.IsLocalBranch = true;
            Utilities.SetupAdvancedMenu();
        }

        private void HandleInstalledBranch(int selectedBranch)
        {
            Utilities.SetupAdvancedMenu();
            var branch = Configuration.ServerConfig.branches[selectedBranch];

            if (!branch.enabled)
            {
                SetPlayState("PLAY", false, false, true);
                return;
            }

            bool isUpToDate = Utilities.GetBranchVersion() == Configuration.ServerConfig.branches[0].version;
            Update_Button.Visibility = isUpToDate ? Visibility.Hidden : Visibility.Visible;
            SetPlayState("PLAY", true, true, true);
        }

        private void HandleUninstalledBranch(int selectedBranch)
        {
            var branch = Configuration.ServerConfig.branches[selectedBranch];

            if (!branch.enabled)
            {
                SetPlayState("DISABLED", false, false, false);
                return;
            }

            bool executableExists = File.Exists(Path.Combine(Utilities.GetBranchDirectory(), "r5apex.exe"));
            SetPlayState(executableExists ? "REPAIR" : "INSTALL", executableExists, executableExists, executableExists);
        }

        private void SetPlayState(string playContent, bool playEnabled, bool repairEnabled, bool uninstallEnabled)
        {
            Play_Button.Content = playContent;
            Play_Button.IsEnabled = playEnabled;
            GameSettings_Control.RepairGame_Button.IsEnabled = repairEnabled;
            GameSettings_Control.UninstallGame_Button.IsEnabled = uninstallEnabled;
        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (Configuration.ServerConfig.branches[Utilities.GetCmbBranchIndex()].update_available && Utilities.IsBranchInstalled())
            {
                Task.Run(() => GameUpdate.Start());
                Update_Button.Visibility = Visibility.Hidden;
            }
        }

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

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            GameSettings_Popup.IsOpen = true;
        }

        private void StatusBtn_Click(object sender, RoutedEventArgs e)
        {
            Status_Popup.IsOpen = true;
        }

        private void SubMenuBtn_Click(object sender, RoutedEventArgs e)
        {
            Menu_Popup.IsOpen = true;
        }

        private void DownloadsBtn_Click(object sender, RoutedEventArgs e)
        {
            Downloads_Popup.IsOpen = true;
        }

        private void StatusPopup_Unloaded(object sender, EventArgs e)
        {
            Status_Button.IsEnabled = true;
        }

        private void StatusPopup_Loaded(object sender, EventArgs e)
        {
            Status_Button.IsEnabled = false;
        }

        private void MenuPopup_Loaded(object sender, EventArgs e)
        {
            Menu_Button.IsEnabled = false;
        }

        private void MenuPopup_Unloaded(object sender, EventArgs e)
        {
            Menu_Button.IsEnabled = true;
        }

        private void GameSettings_Popup_Opened(object sender, EventArgs e)
        {
            GameSettings_Button.IsEnabled = false;
        }

        private void GameSettings_Popup_Closed(object sender, EventArgs e)
        {
            GameSettings_Button.IsEnabled = true;
        }

        private void DownloadsPopup_Unloaded(object sender, EventArgs e)
        {
            Downloads_Button.IsEnabled = true;
        }

        private void DownloadsPopup_Loaded(object sender, EventArgs e)
        {
            Downloads_Button.IsEnabled = false;
        }

        #region functions

        private void SetupSystemTray()
        {
            ContextMenu contextMenu = (ContextMenu)FindResource("tbiContextMenu");
            MenuItem versionMenuItem = contextMenu.Items.OfType<MenuItem>().FirstOrDefault(item => item.Name == "VersionContext");
            if (versionMenuItem != null)
                versionMenuItem.Header = "R5RLauncher " + Constants.Launcher.VERSION;

            System_Tray = new TaskbarIcon
            {
                ToolTipText = "R5Reloaded Launcher",
                Icon = this.Icon.ToIcon(),
                DoubleClickCommand = ShowWindowCommand,
                ContextMenu = (ContextMenu)FindResource("tbiContextMenu")
            };

            Application.Current.Exit += new ExitEventHandler(Current_Exit);
        }

        public void SetButtonState()
        {
            if (Utilities.GetCurrentBranch().is_local_branch)
            {
                Play_Button.Content = "PLAY";
                return;
            }

            Play_Button.Content = Utilities.IsBranchInstalled() ? "PLAY" : "INSTALL";
            if (!Utilities.IsBranchInstalled() && File.Exists(Path.Combine(Utilities.GetBranchDirectory(), "r5apex.exe")))
                Play_Button.Content = "REPAIR";
        }

        public async Task OnOpen()
        {
            if ((bool)Ini.Get(Ini.Vars.Disable_Animations))
            {
                this.Opacity = 1;
                return;
            }

            await Task.Delay(100);

            // Create a storyboard for simultaneous animations
            Storyboard storyboard = new();

            // Duration for the animations
            Duration animationDuration = new(TimeSpan.FromSeconds(0.5));

            // Easing function for smoothness
            CubicEase easing = new() { EasingMode = EasingMode.EaseInOut };

            // Animate ScaleX from 1 to 0
            DoubleAnimation scaleXAnimation = new()
            {
                From = 0.75,
                To = 1.0,
                Duration = animationDuration,
                EasingFunction = easing
            };
            Storyboard.SetTarget(scaleXAnimation, this);
            Storyboard.SetTargetProperty(scaleXAnimation, new PropertyPath("RenderTransform.ScaleX"));

            // Animate ScaleY from 1 to 0
            DoubleAnimation scaleYAnimation = new()
            {
                From = 0.75,
                To = 1.0,
                Duration = animationDuration,
                EasingFunction = easing
            };
            Storyboard.SetTarget(scaleYAnimation, this);
            Storyboard.SetTargetProperty(scaleYAnimation, new PropertyPath("RenderTransform.ScaleY"));

            // Animate Opacity from 1 to 0
            DoubleAnimation opacityAnimation = new()
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
            TaskCompletionSource<bool> tcs = new();
            storyboard.Completed += (s, e) => tcs.SetResult(true);

            // Begin the storyboard
            storyboard.Begin();

            // Await the completion of the animation
            await tcs.Task;
        }

        private async Task OnClose()
        {
            if ((bool)Ini.Get(Ini.Vars.Disable_Animations))
            {
                this.Hide();
                return;
            }

            // Create a storyboard for simultaneous animations
            Storyboard storyboard = new();

            // Duration for the animations
            Duration animationDuration = new(TimeSpan.FromSeconds(0.5));

            // Easing function for smoothness
            CubicEase easing = new() { EasingMode = EasingMode.EaseInOut };

            // Animate ScaleX from 1 to 0
            DoubleAnimation scaleXAnimation = new()
            {
                From = 1.0,
                To = 0.75,
                Duration = animationDuration,
                EasingFunction = easing
            };
            Storyboard.SetTarget(scaleXAnimation, this);
            Storyboard.SetTargetProperty(scaleXAnimation, new PropertyPath("RenderTransform.ScaleX"));

            // Animate ScaleY from 1 to 0
            DoubleAnimation scaleYAnimation = new()
            {
                From = 1.0,
                To = 0.75,
                Duration = animationDuration,
                EasingFunction = easing
            };
            Storyboard.SetTarget(scaleYAnimation, this);
            Storyboard.SetTargetProperty(scaleYAnimation, new PropertyPath("RenderTransform.ScaleY"));

            // Animate Opacity from 1 to 0
            DoubleAnimation opacityAnimation = new()
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
            TaskCompletionSource<bool> tcs = new();
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
            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion functions
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