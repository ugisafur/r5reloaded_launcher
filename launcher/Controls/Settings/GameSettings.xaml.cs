using System;
using System.Collections.Generic;
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

namespace launcher
{
    /// <summary>
    /// Interaction logic for GameSettings.xaml
    /// </summary>
    public partial class GameSettings : UserControl
    {
        public GameSettings()
        {
            InitializeComponent();
        }

        public void SetupGameSettings()
        {
            LibraryPath.Text = (string)Ini.Get(Ini.Vars.Library_Location);

            List<Branch> branches = Configuration.ServerConfig.branches;
            foreach (Branch branch in branches)
            {
                Separator separator = new Separator();
                separator.Opacity = 0;
                separator.Height = 20;

                GameItem gameItem = new GameItem();
                gameItem.SetupGameItem(branch);
                gameItem.Width = 860;

                BranchPanel.Children.Add(gameItem);
                BranchPanel.Children.Add(separator);

                gameItem.CollapseItem();
            }
        }
    }
}