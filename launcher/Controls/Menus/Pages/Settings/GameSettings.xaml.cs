using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static launcher.Global.References;
using System.Globalization;
using launcher.Game;
using launcher.Global;

namespace launcher
{
    public partial class GameSettings : UserControl
    {
        public List<GameItem> gameItems = [];
        private bool firstTime = true;

        public GameSettings()
        {
            InitializeComponent();
        }

        public void SetupGameSettings()
        {
            gameItems.Clear();
            BranchPanel.Children.Clear();

            LibraryPath.Text = (string)Ini.Get(Ini.Vars.Library_Location);

            List<Branch> branches = Configuration.ServerConfig.branches;

            int index = 0;

            for (int i = 0; i < branches.Count; i++)
            {
                // Skip local branches
                if (branches[i].is_local_branch)
                    continue;

                if (!branches[i].show_in_launcher)
                    continue;

                GameItem gameItem = new();
                gameItem.SetupGameItem(branches[i]);
                gameItem.isFirstItem = i == 0;
                gameItem.isLastItem = i == branches.Count - 1;
                gameItem.index = index;

                index++;

                BranchPanel.Children.Add(gameItem);

                if (gameItem.isLastItem)
                {
                    Separator separator = new()
                    {
                        Opacity = 0,
                        Height = 20
                    };

                    BranchPanel.Children.Add(separator);
                }

                gameItems.Add(gameItem);
            }
        }

        //I hate this, but it's the only way to get the first item to collapse on first load
        //otherwise i cant set the corner radius of the top bars button as it dosnt exist until the item is loaded
        public void FirstTime()
        {
            if (firstTime)
            {
                firstTime = false;
                foreach (GameItem gameItem in gameItems)
                {
                    gameItem.CollapseItem();
                }
            }
        }

        private void ChangePath_Click(object sender, RoutedEventArgs e)
        {
            var directoryDialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Select Folder"
            };

            if (directoryDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                LibraryPath.Text = directoryDialog.FileName;

                SetLibaryPath();
            }
        }

        private void InputTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            SetLibaryPath();
        }

        private void LibraryPath_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SetLibaryPath();
            }
        }

        private void SetLibaryPath()
        {
            if ((string)Ini.Get(Ini.Vars.Library_Location) == LibraryPath.Text)
                return;

            Ini.Set(Ini.Vars.Library_Location, LibraryPath.Text);

            foreach (var item in gameItems)
            {
                item.InstallPath.Text = $"{LibraryPath.Text}\\R5R Library\\{item.branchName.ToUpper(new CultureInfo("en-US"))}";
            }

            Main_Window.SetButtonState();
        }

        public void SetLibaryPath(string path)
        {
            LibraryPath.Text = path;

            foreach (var item in gameItems)
            {
                item.InstallPath.Text = $"{LibraryPath.Text}\\R5R Library\\{item.branchName.ToUpper(new CultureInfo("en-US"))}";
            }

            Main_Window.SetButtonState();
        }

        public void UpdateGameItems()
        {
            foreach (var item in gameItems)
            {
                item.UpdateGameItem();
            }
        }
    }
}