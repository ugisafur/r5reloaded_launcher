using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace launcher.Configuration.Models
{
    public class SettingInfo
    {
        public string Section { get; }
        public object DefaultValue { get; }

        public SettingInfo(string section, object defaultValue)
        {
            Section = section;
            DefaultValue = defaultValue;
        }
    }
}
