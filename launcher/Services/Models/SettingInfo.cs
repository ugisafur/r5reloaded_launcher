namespace launcher.Services.Models
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
