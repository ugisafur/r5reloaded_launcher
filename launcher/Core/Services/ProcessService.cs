using System.Diagnostics;
using launcher.Services;
using Microsoft.Win32;
using static launcher.Services.LoggerService;

namespace launcher.Core.Services
{
    public enum EAAppCodes
    {
        Installed_And_Running,
        Installed_And_Not_Running,
        Not_Installed,
    }

    public class ProcessService
    {
        public bool IsR5ApexOpen()
        {
            Process[] processes = Process.GetProcessesByName("r5apex");
            return processes.Length > 0;
        }

        public void CloseR5Apex()
        {
            Process[] processes = Process.GetProcessesByName("r5apex");
            foreach (Process process in processes)
            {
                process.Kill();
                process.WaitForExit();
            }
        }

        public void FindAndStartEAApp()
        {
            if (!(bool)SettingsService.Get(SettingsService.Vars.Auto_Launch_EA_App))
                return;

            Process[] processes = Process.GetProcessesByName("EADesktop");
            if (processes.Length == 0)
            {
                string subKeyPath = @"SOFTWARE\WOW6432Node\Electronic Arts\EA Desktop";
                string EADesktopPath = "";
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(subKeyPath))
                {
                    if (key != null)
                    {
                        object installLocationValue = key.GetValue("DesktopAppPath");

                        if (installLocationValue != null)
                        {
                            EADesktopPath = installLocationValue.ToString();
                            LogInfo(LogSource.Launcher, "Found EA Desktop App");
                        }
                    }
                }

                if (string.IsNullOrEmpty(EADesktopPath))
                {
                    LogError(LogSource.Launcher, "Failed to find EA Desktop App");
                    return;
                }

                LogInfo(LogSource.Launcher, "Starting EA Desktop App");
                var startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c start \"\" \"{EADesktopPath}\" /min",
                    WindowStyle = ProcessWindowStyle.Minimized
                };

                Process.Start(startInfo);
            }
            else
            {
                LogInfo(LogSource.Launcher, "EA Desktop App is already running");
            }
        }

        public EAAppCodes IsEAAppRunning()
        {
            if ((bool)SettingsService.Get(SettingsService.Vars.Offline_Mode))
                return EAAppCodes.Installed_And_Running;

            //TODO: Find a better way to check if EA App is installed
            //string subKeyPath = @"SOFTWARE\WOW6432Node\Electronic Arts\EA Desktop";
            //if (Registry.GetValue($"HKEY_LOCAL_MACHINE\\{subKeyPath}", "DesktopAppPath", null) == null)
            //return EAAppCodes.Not_Installed;

            Process[] processes = Process.GetProcessesByName("EADesktop");
            if (processes.Length == 0)
                return EAAppCodes.Installed_And_Not_Running;

            return EAAppCodes.Installed_And_Running;
        }

        public bool IsWineEnvironment()
        {
            return Process.GetProcessesByName("winlogon").Length == 0;
        }
    }
} 