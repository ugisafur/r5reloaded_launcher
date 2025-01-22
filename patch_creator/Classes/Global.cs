using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace patch_creator
{
    public static class Global
    {
        public static HttpClient HTTP_CLIENT = new();
        public static ServerConfig SERVER_CONFIG = new();

        public static readonly string[] BLACKLIST = {
            "platform\\logs",
            "platform\\screenshots",
            "platform\\user",
            "launcher.exe",
            "bin\\updater.exe",
            "cfg\\startup.bin",
            "launcher_data"
        };

        public static List<string> ignoredFiles = new()
        {
            "checksums.json",
            "checksums_zst.json",
            "clearcache.txt"
        };
    }
}