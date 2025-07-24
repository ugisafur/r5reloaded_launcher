using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace launcher.Views.Popups.Models.Services
{
    public class Server
    {
        public string maxPlayers { get; set; }
        public string port { get; set; }
        public string checksum { get; set; }
        public string name { get; set; }
        public string ip { get; set; }
        public string description { get; set; }
        public string hidden { get; set; }
        public string playerCount { get; set; }
        public string playlist { get; set; }
        public string key { get; set; }
        public string region { get; set; }
        public string map { get; set; }
    }
}
