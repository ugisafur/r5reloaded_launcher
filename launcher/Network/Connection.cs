using launcher.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace launcher.Network
{
    public static class Connection
    {
        public static bool CDNTest()
        {
            try
            {
                using var client = new System.Net.WebClient();
                using var stream = client.OpenRead("https://cdn.r5r.org/launcher/config.json");
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool NewsTest()
        {
            try
            {
                using var client = new System.Net.WebClient();
                using var stream = client.OpenRead($"{Launcher.NEWSURL}/posts/?key={Launcher.NEWSKEY}&include=tags,authors");
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool MasterServerTest()
        {
            try
            {
                using var client = new System.Net.WebClient();
                using var stream = client.OpenRead("https://r5r.org");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}