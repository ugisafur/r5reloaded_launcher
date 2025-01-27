using Hardcodet.Wpf.TaskbarNotification;
using launcher.Classes.BranchUtils;
using launcher.Classes.CDN;
using launcher.Classes.Game;
using launcher.Classes.Global;
using launcher.Classes.Managers;
using launcher.Classes.Utilities;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using static launcher.Classes.Utilities.Logger;
using Color = System.Windows.Media.Color;

namespace launcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private double _previousWidth;
        private double _previousHeight;
        private double _previousTop;
        private double _previousLeft;
        private bool _isMaximized = false;

        public TaskbarIcon System_Tray { get; set; }
        public ICommand ShowWindowCommand { get; }

        public List<Button> NewsButtons = [];
        public List<double> NewsButtonsX = [-167.4, -45.5, 61.8, 163];
        public List<double> NewsButtonsWidth = [95, 113, 65, 101];

        public MainWindow()
        {
            ShowWindowCommand = new RelayCommand(ExecuteShowWindow, CanExecuteShowWindow);
            InitializeComponent();
        }

        private void DragBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (_isMaximized)
                {
                    // Calculate the mouse position relative to the window
                    var mousePosition = PointToScreen(e.GetPosition(this));

                    // Restore the window to normal state
                    WindowState = WindowState.Normal;
                    Top = _previousTop;
                    Left = _previousLeft;
                    Width = _previousWidth;
                    Height = _previousHeight;

                    // Adjust the window position to ensure the cursor stays aligned
                    Left = mousePosition.X - (RestoreBounds.Width / 2);
                    Top = mousePosition.Y - 20; // Offset for the title bar height

                    _isMaximized = false;
                }

                if (!AppState.OnBoarding)
                    DragMove();
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RenderOptions.ProcessRenderMode = RenderMode.Default;

            // Hide the window on startup
            this.Opacity = 0;

            PreLoad preLoad = new();
            preLoad.Show();

            // Setup global exception handlers
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            // Create the configuration file if it doesn't exist
            Ini.CreateConfig();

            // Setup the system tray
            SetupSystemTray();

            // Setup the application
            AppManager.SetupApp(this);

            // Setup the news buttons
            NewsButtons.Add(Community_Button);
            NewsButtons.Add(NewLegends_Button);
            NewsButtons.Add(Comms_Button);
            NewsButtons.Add(PatchNotes_Button);

            if (AppState.IsOnline && Classes.News.Connection.Test())
            {
                AppManager.MoveNewsRect(0);
                HideNewsRect();
            }

            // Setup Background
            bool useStaticImage = (bool)Ini.Get(Ini.Vars.Disable_Background_Video);
            if (!useStaticImage)
            {
                await LoadVideoBackground();
            }
            else
            {
                if (File.Exists(Path.Combine(Launcher.PATH, "launcher_data\\assets", "background.png")))
                    Background_Image.Source = new BitmapImage(new Uri(Path.Combine(Launcher.PATH, "launcher_data\\assets", "background.png")));
            }

            Background_Image.Visibility = useStaticImage ? Visibility.Visible : Visibility.Hidden;
            Background_Video.Visibility = useStaticImage ? Visibility.Hidden : Visibility.Visible;

            preLoad.Close();

            // Show window open animation
            await OnOpen();

            if (AppState.IsOnline)
            {
                Task.Run(() => UpdateChecker.Start());
                SetButtonState();
            }
            else
                Play_Button.Content = "PLAY";

            if ((bool)Ini.Get(Ini.Vars.Ask_For_Tour))
            {
                AppManager.ShowOnBoardAskPopup();
            }
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
            if ((string)Ini.Get(Ini.Vars.Enable_Quit_On_Close) != "quit" && (string)Ini.Get(Ini.Vars.Enable_Quit_On_Close) != "tray")
                Ini.Set(Ini.Vars.Enable_Quit_On_Close, "");

            if (string.IsNullOrEmpty((string)Ini.Get(Ini.Vars.Enable_Quit_On_Close)))
            {
                AppManager.ShowAskToQuit();
                return;
            }

            if ((string)Ini.Get(Ini.Vars.Enable_Quit_On_Close) == "quit")
                Application.Current.Shutdown();
            else if ((string)Ini.Get(Ini.Vars.Enable_Quit_On_Close) == "tray")
            {
                AppManager.SendNotification("Launcher minimized to tray.", BalloonIcon.Info);
                OnClose();
            }
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if (!AppState.IsOnline || GetBranch.Installed() || GetBranch.IsLocalBranch())
            {
                Task.Run(() => Game.Launch());
                return;
            }

            if (!AppState.IsInstalling)
            {
                if (!GetBranch.Installed() && !string.IsNullOrEmpty((string)Ini.Get(Ini.Vars.Library_Location)) && File.Exists(Path.Combine(GetBranch.Directory(), "r5apex.exe")))
                {
                    AppManager.ShowCheckExistingFiles();
                }
                else
                {
                    Task.Run(() => Install.Start());
                }
            }
        }

        private void cmbBranch_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox comboBox) return;

            var selectedBranch = comboBox.SelectedIndex;
            var comboBranch = (ComboBranch)Branch_Combobox.Items[selectedBranch];

            AppManager.SetupAdvancedMenu();
            GameSettings_Control.OpenDir_Button.IsEnabled = GetBranch.Installed() || comboBranch.isLocalBranch;
            GameSettings_Control.AdvancedMenu_Button.IsEnabled = GetBranch.Installed() || comboBranch.isLocalBranch;

            if (comboBranch.isLocalBranch || !AppState.IsOnline)
            {
                ReadMore_Label.Inlines.Clear();
                HandleLocalBranch(comboBranch.title);
                return;
            }

            AppState.IsLocalBranch = false;
            Ini.Set(Ini.Vars.SelectedBranch, GetBranch.Name(false));

            SetTextBlockContent(comboBranch.subtext);

            if (GetBranch.Installed())
            {
                HandleInstalledBranch(selectedBranch);
            }
            else
            {
                HandleUninstalledBranch(selectedBranch);
            }
        }

        private void SetTextBlockContent(string version)
        {
            // Clear any existing inlines
            ReadMore_Label.Inlines.Clear();

            // Add plain text
            ReadMore_Label.Inlines.Add(new Run($"Read about {version} features, "));

            string url = string.IsNullOrEmpty(GetBranch.Branch().latest_patch_notes) ? "https://blog.r5reloaded.com" : GetBranch.Branch().latest_patch_notes;

            // Create a hyperlink
            Hyperlink link = new Hyperlink(new Run("see patch notes"))
            {
                NavigateUri = new Uri(url)
            };
            link.RequestNavigate += Hyperlink_RequestNavigate;

            // Add hyperlink to inlines
            ReadMore_Label.Inlines.Add(link);
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Unable to open link: {ex.Message}");
            }
            e.Handled = true;
        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (GetBranch.UpdateAvailable() && GetBranch.Installed())
            {
                Task.Run(() => Update.Start());
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

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Transition_Rect_Translate.BeginAnimation(TranslateTransform.XProperty, null);

            WindowClip.Rect = new Rect(0, 0, ActualWidth, ActualHeight);
            Transition_Rect.Width = ActualWidth * 4;
            Transition_Rect.Height = ActualHeight - 64;
            Transition_Rect_Translate.X = -(ActualWidth * 4) - 60;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                // Store current size and position before maximizing
                _previousWidth = Width;
                _previousHeight = Height;
                _previousTop = Top;
                _previousLeft = Left;

                // Adjust the window to fit the screen's working area
                var screen = System.Windows.SystemParameters.WorkArea;
                WindowState = WindowState.Normal;
                Top = screen.Top;
                Left = screen.Left;
                Width = screen.Width;
                Height = screen.Height;

                _isMaximized = true;
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

        private void Window_LocationChanged(object sender, EventArgs e)
        {
        }

        private void NewsButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            int index = NewsButtons.IndexOf(button);
            AppManager.MoveNewsRect(index);
        }

        private bool _isNewsRectShown = false;

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.HorizontalOffset > 1)
            {
                if (_isNewsRectShown)
                    return;

                ShowNewsRect();
            }
            else
            {
                if (!_isNewsRectShown)
                    return;

                HideNewsRect();
            }
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (NewsScrollViewer.ScrollableWidth > 0)
            {
                e.Handled = true;
                double newOffset = NewsScrollViewer.HorizontalOffset - (e.Delta > 0 ? 30 : -30);
                newOffset = Math.Max(0, Math.Min(newOffset, NewsScrollViewer.ScrollableWidth));
                NewsScrollViewer.ScrollToHorizontalOffset(newOffset);
            }
        }

        #region functions

        private void ShowNewsRect()
        {
            _isNewsRectShown = true;

            Storyboard storyboard = new Storyboard();

            // Fade-in animation
            DoubleAnimation fadeInAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(fadeInAnimation, LeftNewsRect);
            Storyboard.SetTargetProperty(fadeInAnimation, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(fadeInAnimation);

            storyboard.Begin();
        }

        private void HideNewsRect()
        {
            _isNewsRectShown = false;

            Storyboard storyboard = new Storyboard();

            // Fade-in animation
            DoubleAnimation fadeInAnimation = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(fadeInAnimation, LeftNewsRect);
            Storyboard.SetTargetProperty(fadeInAnimation, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(fadeInAnimation);

            storyboard.Begin();
        }

        private async Task LoadVideoBackground()
        {
            if ((bool)Ini.Get(Ini.Vars.Stream_Video) && !File.Exists(Path.Combine(Launcher.PATH, "launcher_data\\assets", "background.mp4")) && AppState.IsOnline)
            {
                string videoUrl = Launcher.BACKGROUND_VIDEO_URL + Configuration.ServerConfig.launcherBackgroundVideo;
                Background_Video.Source = new Uri(videoUrl, UriKind.Absolute);

                double bufferingProgress = Background_Video.BufferingProgress;
                while (bufferingProgress < 0.3)
                {
                    bufferingProgress = Background_Video.BufferingProgress;
                    await Task.Delay(100);
                }

                LogInfo(Source.Launcher, $"Streaming video background from: {videoUrl}");
            }
            else if (File.Exists(Path.Combine(Launcher.PATH, "launcher_data\\assets", "background.mp4")))
            {
                Background_Video.Source = new Uri(Path.Combine(Launcher.PATH, "launcher_data\\assets", "background.mp4"), UriKind.Absolute);
                LogInfo(Source.Launcher, "Loading local video background");
            }

            Background_Video.MediaOpened += (sender, e) =>
            {
                Background_Video.Play();
            };

            await Task.Delay(1000);

            Background_Video.MediaFailed += (sender, e) =>
            {
                LogInfo(Source.Launcher, $"Failed to load video: {e.ErrorException?.Message}");
                Background_Image.Visibility = Visibility.Visible;
                Background_Video.Visibility = Visibility.Hidden;
            };
        }

        private void HandleLocalBranch(string branchTitle)
        {
            Ini.Set(Ini.Vars.SelectedBranch, branchTitle);
            Update_Button.Visibility = Visibility.Hidden;
            SetPlayState("PLAY", true, false, true, true, true);
            AppState.IsLocalBranch = true;
        }

        private void HandleInstalledBranch(int selectedBranch)
        {
            var branch = Configuration.ServerConfig.branches[selectedBranch];

            if (!branch.enabled)
            {
                SetPlayState("PLAY", false, false, true, true, true);
                return;
            }

            bool isUpToDate = GetBranch.LocalVersion() == GetBranch.ServerVersion();
            Update_Button.Visibility = isUpToDate ? Visibility.Hidden : Visibility.Visible;
            SetBranch.UpdateAvailable(!isUpToDate);
            SetPlayState("PLAY", true, true, true, true, true);
        }

        private void HandleUninstalledBranch(int selectedBranch)
        {
            var branch = Configuration.ServerConfig.branches[selectedBranch];

            if (!branch.enabled)
            {
                SetPlayState("DISABLED", false, false, false, false, false);
                return;
            }

            Update_Button.Visibility = Visibility.Hidden;
            SetBranch.UpdateAvailable(false);

            bool executableExists = File.Exists(Path.Combine(GetBranch.Directory(), "r5apex.exe"));
            SetPlayState(executableExists ? "REPAIR" : "INSTALL", true, executableExists, executableExists, executableExists, executableExists);
        }

        private void SetPlayState(string playContent, bool playEnabled, bool repairEnabled, bool uninstallEnabled, bool openBranchFolder, bool advancedsettings)
        {
            Play_Button.Content = playContent;
            Play_Button.IsEnabled = playEnabled;
            GameSettings_Control.RepairGame_Button.IsEnabled = repairEnabled;
            GameSettings_Control.UninstallGame_Button.IsEnabled = uninstallEnabled;
            GameSettings_Control.OpenDir_Button.IsEnabled = openBranchFolder;
            GameSettings_Control.AdvancedMenu_Button.IsEnabled = advancedsettings;
        }

        private void SetupSystemTray()
        {
            ContextMenu contextMenu = (ContextMenu)FindResource("tbiContextMenu");
            MenuItem versionMenuItem = contextMenu.Items.OfType<MenuItem>().FirstOrDefault(item => item.Name == "VersionContext");
            if (versionMenuItem != null)
                versionMenuItem.Header = "R5RLauncher " + Launcher.VERSION;

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
            if (GetBranch.IsLocalBranch())
            {
                Play_Button.Content = "PLAY";
                return;
            }

            if (!GetBranch.Enabled())
            {
                Play_Button.Content = "DISABLED";
                return;
            }

            if (GetBranch.Installed())
            {
                Play_Button.Content = "PLAY";
                return;
            }

            if (!GetBranch.Installed() && File.Exists(Path.Combine(GetBranch.Directory(), "r5apex.exe")))
            {
                Play_Button.Content = "CHECK FILES";
                return;
            }

            Play_Button.Content = "INSTALL";
        }

        private async Task AnimateWindow(bool isOpening)
        {
            if (isOpening && this.Opacity == 1)
                return;

            if ((bool)Ini.Get(Ini.Vars.Disable_Animations))
            {
                if (isOpening)
                {
                    this.Opacity = 1;
                }
                else
                {
                    this.Hide();
                    this.Opacity = 1;
                    WindowScale.ScaleX = 1;
                    WindowScale.ScaleY = 1;
                }
                return;
            }

            // Delay before opening animation
            if (isOpening)
                await Task.Delay(100);

            // Create a storyboard for simultaneous animations
            Storyboard storyboard = new();

            // Duration for the animations
            Duration animationDuration = new(TimeSpan.FromSeconds(0.5));

            // Easing function for smoothness
            CubicEase easing = new() { EasingMode = EasingMode.EaseInOut };

            // Define animation values based on opening or closing
            double scaleStart = isOpening ? 0.75 : 1.0;
            double scaleEnd = isOpening ? 1.0 : 0.75;
            double opacityStart = isOpening ? 0.0 : 1.0;
            double opacityEnd = isOpening ? 1.0 : 0.0;

            // Animate ScaleX
            DoubleAnimation scaleXAnimation = new()
            {
                From = scaleStart,
                To = scaleEnd,
                Duration = animationDuration,
                EasingFunction = easing
            };
            Storyboard.SetTarget(scaleXAnimation, this);
            Storyboard.SetTargetProperty(scaleXAnimation, new PropertyPath("RenderTransform.ScaleX"));

            // Animate ScaleY
            DoubleAnimation scaleYAnimation = new()
            {
                From = scaleStart,
                To = scaleEnd,
                Duration = animationDuration,
                EasingFunction = easing
            };
            Storyboard.SetTarget(scaleYAnimation, this);
            Storyboard.SetTargetProperty(scaleYAnimation, new PropertyPath("RenderTransform.ScaleY"));

            // Animate Opacity
            DoubleAnimation opacityAnimation = new()
            {
                From = opacityStart,
                To = opacityEnd,
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

            // Finalize actions after animation
            if (!isOpening)
            {
                this.Hide();
                this.Opacity = 1;
                WindowScale.ScaleX = 1;
                WindowScale.ScaleY = 1;
            }
        }

        public Task OnOpen() => AnimateWindow(isOpening: true);

        public Task OnClose() => AnimateWindow(isOpening: false);

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

    public class ComboBranch
    {
        public string title { get; set; }
        public string subtext { get; set; }
        public bool isLocalBranch { get; set; }
    }
}