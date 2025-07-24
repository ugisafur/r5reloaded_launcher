using Newtonsoft.Json;
using patch_creator.Models;
using System.IO;
using System.Windows.Forms;

namespace patch_creator.Services
{
    public class ConfigService
    {
        private readonly string _configPath;

        public ConfigService()
        {
            _configPath = Path.Combine(Application.StartupPath, "config.json");
        }

        public CFConfig LoadConfig()
        {
            if (!File.Exists(_configPath))
            {
                CreateDefaultConfig();
                return new CFConfig();
            }

            string configJson = File.ReadAllText(_configPath);
            return JsonConvert.DeserializeObject<CFConfig>(configJson);
        }

        private void CreateDefaultConfig()
        {
            Console.WriteLine("Config file not found, creating default config");
            Console.WriteLine("Please fill in the required fields in the config file");

            var defaultConfig = new CFConfig
            {
                zoneID = "",
                authKey = ""
            };

            string defaultConfigJson = JsonConvert.SerializeObject(defaultConfig, Formatting.Indented);
            File.WriteAllText(_configPath, defaultConfigJson);
        }
    }
} 