using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
        public static ProgressBar progressBar = new();
        public static TextBlock lblStatus = new();
        public static TextBlock lblFilesLeft = new();
        public static TextBlock launcherVersionlbl = new();
        public static ComboBox cmbBranch = new();
        public static Button btnPlay = new();
        public static Button btnUpdate = new();
        public static MainWindow mainApp = new();
        public static SettingsControl settingsControl = new();
        public static AdvancedMenu advancedControl = new();
        public static subMenu subMenuControl = new();
        public static DependencyObject TransitionRect = new();
        public static Popup SubMenuPopup = new();
        public static Popup gameSettingsPopup = new();
        public static StatusPopup statusPopup = new();
        public static Dispatcher appDispatcher = mainApp.Dispatcher;
        public static DownloadsPopup downloadsPopupControl = new();
    }
}