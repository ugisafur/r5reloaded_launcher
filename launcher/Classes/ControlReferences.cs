using Hardcodet.Wpf.TaskbarNotification;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace launcher
{
    /// <summary>
    /// The cs file contains a static class that holds references
    /// to various UI controls and components used throughout the application.
    /// These references are initialized as static fields, making them easily
    /// accessible from anywhere within the application.
    ///
    /// This approach centralizes the control references, which can simplify
    /// the management and manipulation of these controls, especially in a
    /// complex UI environment.
    ///
    /// Note: Ensure that the controls are properly initialized and used in
    /// the appropriate context to avoid potential issues with UI threading
    /// and control states.
    /// </summary>
    public static class ControlReferences
    {
        #region Windows

        public static MainWindow Main_Window = new();

        #endregion Windows

        #region Labels

        public static TextBlock Status_Label = new();
        public static TextBlock Files_Label = new();
        public static TextBlock Version_Label = new();

        #endregion Labels

        #region ComboBoxes

        public static ComboBox Branch_Combobox = new();

        #endregion ComboBoxes

        #region Progress Bars

        public static ProgressBar Progress_Bar = new();

        #endregion Progress Bars

        #region User Controls

        public static SettingsControl Settings_Control = new();
        public static AdvancedMenu Advanced_Control = new();

        #endregion User Controls

        #region Buttons

        public static Button Play_Button = new();
        public static Button Update_Button = new();
        public static Button Status_Button = new();
        public static Button Downloads_Button = new();

        #endregion Buttons

        #region Popups

        public static Popup Menu_Popup = new();
        public static Popup GameSettings_Popup = new();
        public static Popup Downloads_Popup = new();

        #endregion Popups

        #region Popup Controls

        public static SettingsPopup GameSettings_Control = new();
        public static DownloadsPopup Downloads_Control = new();
        public static StatusPopup Status_Control = new();
        public static MenuPopup Menu_Control = new();
        public static EULAPopup EULA_Control = new();
        public static InstallOptFilesPopup OptFiles_Control = new();
        public static CheckExisitngFilesPopup CheckFiles_Control = new();

        #endregion Popup Controls

        #region Other

        public static Dispatcher appDispatcher = Main_Window.Dispatcher;
        public static DependencyObject Transition_Rect = new();
        public static MediaElement Background_Video = new();
        public static Image Background_Image = new();
        public static TaskbarIcon System_Tray;
        public static Rectangle POPUP_BG = new();

        public static PlaylistRoot playlistRoot = new();
        public static List<string> gamemodes = new();
        public static List<string> maps = new();

        #endregion Other

        public static void SetupControlReferences(MainWindow mainWindow)
        {
            Main_Window = mainWindow;
            appDispatcher = mainWindow.Dispatcher;
            Progress_Bar = mainWindow.ProgressBar;
            Status_Label = mainWindow.Status_Label;
            Files_Label = mainWindow.Files_Label;
            Version_Label = mainWindow.Version_Label;
            Branch_Combobox = mainWindow.Branch_Combobox;
            Play_Button = mainWindow.Play_Button;
            Settings_Control = mainWindow.Settings_Control;
            Advanced_Control = mainWindow.Advanced_Control;
            Menu_Control = mainWindow.Menu_Control;
            Transition_Rect = mainWindow.Transition_Rect;
            Menu_Popup = mainWindow.Menu_Popup;
            GameSettings_Popup = mainWindow.GameSettings_Popup;
            Downloads_Control = mainWindow.Downloads_Control;
            Status_Control = mainWindow.Status_Control;
            Update_Button = mainWindow.Update_Button;
            System_Tray = mainWindow.System_Tray;
            Status_Button = mainWindow.Status_Button;
            Downloads_Button = mainWindow.Downloads_Button;
            GameSettings_Control = mainWindow.GameSettings_Control;
            Downloads_Popup = mainWindow.Downloads_Popup;
            Background_Video = mainWindow.Background_Video;
            Background_Image = mainWindow.Background_Image;
            EULA_Control = mainWindow.EULA_Control;
            POPUP_BG = mainWindow.POPUP_BG;
            OptFiles_Control = mainWindow.OptFiles_Control;
            CheckFiles_Control = mainWindow.CheckFiles_Control;

            Update_Button.Visibility = Visibility.Hidden;
            Progress_Bar.Visibility = Visibility.Hidden;
            Status_Label.Visibility = Visibility.Hidden;
            Files_Label.Visibility = Visibility.Hidden;
        }
    }
}