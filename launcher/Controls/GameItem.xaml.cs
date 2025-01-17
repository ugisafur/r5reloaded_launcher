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
    /// Interaction logic for GameItem.xaml
    /// </summary>
    public partial class GameItem : UserControl
    {
        private bool isExpanded = true;

        public GameItem()
        {
            InitializeComponent();
        }

        public void SetupGameItem(Branch branch)
        {
            BranchName.Text = $"R5Reloaded - {branch.branch.ToUpper()}";
            InstallPath.Text = $"{(string)Ini.Get(Ini.Vars.Library_Location)}\\R5R Library\\{branch.branch.ToUpper()}";
            UninstallGame.Visibility = Ini.Get(branch.branch, "Is_Installed", false) ? Visibility.Visible : Visibility.Hidden;
            InstallGame.Visibility = Ini.Get(branch.branch, "Is_Installed", false) ? Visibility.Hidden : Visibility.Visible;
        }

        private void TopButton_Click(object sender, RoutedEventArgs e)
        {
            if (isExpanded)
            {
                CollapseItem();
            }
            else
            {
                ExpandItem();
            }
        }

        public void CollapseItem()
        {
            this.Height = 65;
            isExpanded = false;
            CollapseIcon.Text = "+";
            TopBar.CornerRadius = new CornerRadius(10, 10, 10, 10);
        }

        public void ExpandItem()
        {
            this.Height = 443;
            isExpanded = true;
            CollapseIcon.Text = "-";
            TopBar.CornerRadius = new CornerRadius(10, 10, 0, 0);
        }
    }
}