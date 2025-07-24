using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace launcher.Views.Popups.Models.Services
{
    public class GameServerList
    {
        public bool success { get; set; }
        public List<Server> servers { get; set; }
    }
}
