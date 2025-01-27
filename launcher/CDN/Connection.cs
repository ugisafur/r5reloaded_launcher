using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace launcher.CDN
{
    public static class Connection
    {
        public static bool Test()
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
    }
}