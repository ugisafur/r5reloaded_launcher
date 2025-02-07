using Newtonsoft.Json;

namespace r2_upload
{
    public partial class Form1 : Form
    {
        public static HttpClient HTTP_CLIENT = new();
        public static ServerConfig SERVER_CONFIG = new();
        public static CFConfig CF_CONFIG = new();

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(CF_CONFIG.bucket_name) || string.IsNullOrEmpty(CF_CONFIG.account_id) || string.IsNullOrEmpty(CF_CONFIG.access_key) || string.IsNullOrEmpty(CF_CONFIG.secret_key) || SERVER_CONFIG.branches.Count < 1)
                return;

            var messageboxResult = MessageBox.Show("Are you sure you want to upload all files in this directory?", "Upload Files", MessageBoxButtons.YesNo);
            if (messageboxResult == DialogResult.Yes)
                StartUpload();
        }

        private async void StartUpload()
        {
            CloudflareClient.accountId = CF_CONFIG.account_id;
            CloudflareClient.accessKey = CF_CONFIG.access_key;
            CloudflareClient.accessSecret = CF_CONFIG.secret_key;

            var all_Files = Directory.GetFiles(textBox1.Text, "*.*", SearchOption.AllDirectories);

            CloudflareClient.filesLeftCount = all_Files.Length;

            var gameFiles = new GameFiles();
            gameFiles.files = new List<GameFile>();
            foreach (var file in all_Files)
            {
                gameFiles.files.Add(new GameFile() { name = file, checksum = "" });
            }

            var UploadTasks = CloudflareClient.InitializeUploadTasks(gameFiles, SERVER_CONFIG.branches[comboBox1.SelectedIndex], textBox1.Text, CF_CONFIG.bucket_name);

            await Task.WhenAll(UploadTasks);

            MessageBox.Show("Uploads complete!");
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var response = HTTP_CLIENT.GetAsync("https://cdn.r5r.org/launcher/config.json").Result;
            var responseString = response.Content.ReadAsStringAsync().Result;
            SERVER_CONFIG = JsonConvert.DeserializeObject<ServerConfig>(responseString);

            foreach (Branch branch in SERVER_CONFIG.branches)
            {
                comboBox1.Items.Add(branch.branch);
            }

            comboBox1.SelectedIndex = 0;

            LoadConfig();
        }

        private void LoadConfig()
        {
            string configPath = Application.StartupPath + "config.json";
            if (!File.Exists(configPath))
            {
                CFConfig cFConfig = new CFConfig
                {
                    bucket_name = "some_bucket_name",
                    account_id = "",
                    access_key = "",
                    secret_key = ""
                };

                string defaultConfig = JsonConvert.SerializeObject(cFConfig, Formatting.Indented);
                File.WriteAllText(configPath, defaultConfig);
            }

            string config = File.ReadAllText(configPath);
            CFConfig cfConfig = JsonConvert.DeserializeObject<CFConfig>(config);

            if (!string.IsNullOrEmpty(cfConfig.bucket_name) || !string.IsNullOrEmpty(cfConfig.account_id) || !string.IsNullOrEmpty(cfConfig.access_key) || !string.IsNullOrEmpty(cfConfig.secret_key))
            {
                CF_CONFIG = cfConfig;
            }
            else
            {
                button1.Enabled = false;
            }
        }
    }

    public class CFConfig
    {
        public string bucket_name { get; set; }
        public string account_id { get; set; }
        public string access_key { get; set; }
        public string secret_key { get; set; }
    }

    public class Branch
    {
        public string? branch { get; set; }
        public string? version { get; set; }
        public string? game_url { get; set; }
        public bool? enabled { get; set; }
        public bool? show_in_launcher { get; set; }
    }

    public class ServerConfig
    {
        public string? launcherVersion { get; set; }
        public string? launcherSelfUpdater { get; set; }
        public bool? allowUpdates { get; set; }
        public List<Branch>? branches { get; set; }
    }
}