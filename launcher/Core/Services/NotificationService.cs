using Hardcodet.Wpf.TaskbarNotification;
using launcher.Services;
using static launcher.Core.AppContext;
using static launcher.Services.LoggerService;

namespace launcher.Core.Services
{
    public class NotificationService
    {
        public void SendNotification(string message, BalloonIcon icon)
        {
            if (!(bool)SettingsService.Get(SettingsService.Vars.Enable_Notifications))
                return;

            try
            {
                System_Tray.ShowBalloonTip("R5R Launcher", message, icon);
            }
            catch (Exception ex)
            {
                LogException($"Failed to send notification", LogSource.Launcher, ex);
            }
        }
    }
} 