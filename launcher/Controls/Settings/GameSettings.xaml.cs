using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TreeView;

namespace launcher
{
    /// <summary>
    /// Interaction logic for GameSettings.xaml
    /// </summary>
    public partial class GameSettings : UserControl
    {
        private List<GameItem> gameItems = [];
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

            for (int i = 0; i < branches.Count; i++)
            {
                // Skip local branches
                if (branches[i].is_local_branch)
                    continue;

                if (!branches[i].show_in_launcher)
                    continue;

                GameItem gameItem = new();
                gameItem.SetupGameItem(branches[i]);
                gameItem.Width = 860;
                gameItem.isFirstItem = i == 0;
                gameItem.isLastItem = i == branches.Count - 1;

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
                Ini.Set(Ini.Vars.Library_Location, directoryDialog.FileName);

                foreach (var item in gameItems)
                {
                    item.InstallPath.Text = $"{directoryDialog.FileName}\\R5R Library\\{item.branchName.ToUpper()}";
                }
            }
        }
    }
}