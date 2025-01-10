using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace launcher
{
    /// <summary>
    /// The ControlReferences.cs file contains a static class that holds references
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
        public static ProgressBar progressBar = new ProgressBar();
        public static TextBlock lblStatus = new TextBlock();
        public static TextBlock lblFilesLeft = new TextBlock();
        public static TextBlock launcherVersionlbl = new TextBlock();
        public static ComboBox cmbBranch = new ComboBox();
        public static Button btnPlay = new Button();
        public static MainWindow App = new MainWindow();
        public static SettingsControl settingsControl = new SettingsControl();
        public static AdvancedMenu advancedControl = new AdvancedMenu();
        public static subMenu subMenuControl = new subMenu();
        public static DependencyObject TransitionRect = new DependencyObject();
        public static Popup SubMenuPopup = new Popup();
        public static Popup gameSettingsPopup = new Popup();
        public static StatusPopup statusPopup = new StatusPopup();
        public static Dispatcher dispatcher = App.Dispatcher;
        public static DownloadsPopup downloadsPopupControl = new DownloadsPopup();
        public static GameRepair gameRepair = new GameRepair();
        public static GameInstall gameInstall = new GameInstall();
        public static GameUpdate gameUpdate = new GameUpdate();
    }
}