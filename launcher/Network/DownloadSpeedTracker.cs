using launcher.Download;
using launcher.Game;
using launcher.Global;
using static launcher.Global.References;

namespace launcher.Network
{
    public static class GlobalDownloadStats
    {
        public static long TotalBytes = 0;
        public static long DownloadedBytes = 0;
        public static DateTime StartTime;
        public static string totalText = "";
        public static string downloadedText = "";
        public static string timeLeftText = "";
    }

    public static class DownloadSpeedTracker
    {
        public static int UpdateType = 0; // 0 = install, 1 = repair, 2 = uninstall

        private static long _totalDownloadedBytes = 0;

        public static long _downloadSpeedLimit = 0;
        public static SemaphoreSlim _downloadSemaphore;
        public static DownloadSpeedMonitor _speedMonitor;
        public static double currentDownloadSpeed = 0;

        public static DateTime speedCheckStart = DateTime.Now;

        public static void AddDownloadedBytes(long bytes, GameFile file)
        {
            Interlocked.Add(ref _totalDownloadedBytes, bytes);
            Interlocked.Add(ref GlobalDownloadStats.DownloadedBytes, bytes);
            file.downloadMetadata.fileDownload.downloadedBytes += bytes;
        }

        public static void RemoveDownloadedBytes(long bytes)
        {
            Interlocked.Add(ref _totalDownloadedBytes, -bytes);
            Interlocked.Add(ref GlobalDownloadStats.DownloadedBytes, -bytes);
        }

        public static void Reset()
        {
            Interlocked.Exchange(ref _totalDownloadedBytes, 0);
            Interlocked.Exchange(ref GlobalDownloadStats.DownloadedBytes, 0);
        }

        public static long GetTotalDownloadedBytes()
        {
            return Interlocked.Read(ref _totalDownloadedBytes);
        }

        public static void ConfigureConcurrency()
        {
            int maxConcurrentDownloads = (int)Ini.Get(Ini.Vars.Concurrent_Downloads);
            _downloadSemaphore?.Dispose();
            _downloadSemaphore = new SemaphoreSlim(maxConcurrentDownloads);
        }

        public static void ConfigureDownloadSpeed()
        {
            int speedLimitKb = (int)Ini.Get(Ini.Vars.Download_Speed_Limit);
            _downloadSpeedLimit = speedLimitKb > 0 ? speedLimitKb * 1024 : 0;
            GlobalBandwidthLimiter.Instance.UpdateLimit(_downloadSpeedLimit);
        }

        public static SemaphoreSlim GetSemaphoreSlim()
        {
            return _downloadSemaphore;
        }

        public static DownloadSpeedMonitor GetDownloadSpeedMonitor()
        {
            return _speedMonitor;
        }

        public static long GetDownloadSpeedLimit()
        {
            return _downloadSpeedLimit;
        }

        public static double GetCurrentDownloadSpeed()
        {
            return currentDownloadSpeed;
        }

        public static void UpdateDownloadSpeedUI(double speedBytesPerSecond)
        {
            string speedText;
            double speed = speedBytesPerSecond;
            currentDownloadSpeed = speed;

            if (speed >= 1024 * 1024)
            {
                speed /= (1024 * 1024);
                speedText = $"{speed:F2} MB/s";
            }
            else if (speed >= 1024)
            {
                speed /= 1024;
                speedText = $"{speed:F2} KB/s";
            }
            else
            {
                speedText = $"{speed} B/s";
            }

            appDispatcher.Invoke(() =>
            {
                Speed_Label.Text = $"{speedText}";
                Downloads_Control.Speed_Label.Text = $"{GlobalDownloadStats.timeLeftText}  |  {GlobalDownloadStats.downloadedText}/{GlobalDownloadStats.totalText}  |  {speedText}";
            });
        }

        public static void CreateDownloadMonitor()
        {
            if (_speedMonitor != null)
            {
                _speedMonitor.OnSpeedUpdated -= UpdateDownloadSpeedUI;
                _speedMonitor.Dispose();
                _speedMonitor = null;
            }

            _speedMonitor = new DownloadSpeedMonitor();
            _speedMonitor.OnSpeedUpdated += UpdateDownloadSpeedUI;
        }

        public static async Task UpdateGlobalDownloadProgressAsync(CancellationToken token)
        {
            DateTime lastPresenceUpdate = DateTime.MinValue;

            while (!token.IsCancellationRequested)
            {
                var elapsed = DateTime.Now - GlobalDownloadStats.StartTime;
                double avgSpeed = elapsed.TotalSeconds > 0
                    ? GlobalDownloadStats.DownloadedBytes / elapsed.TotalSeconds
                    : 0;

                // Use the current speed if available, otherwise fall back to average
                double currentSpeed = currentDownloadSpeed; // in bytes/sec
                double effectiveSpeed = currentSpeed > 0
                    ? (avgSpeed + currentSpeed) / 2   // blend average + current
                    : avgSpeed;

                long remainingBytes = GlobalDownloadStats.TotalBytes - GlobalDownloadStats.DownloadedBytes;
                TimeSpan estimatedRemaining = effectiveSpeed > 0
                    ? TimeSpan.FromSeconds(remainingBytes / effectiveSpeed)
                    : TimeSpan.Zero;

                await appDispatcher.InvokeAsync(() =>
                {
                    Progress_Bar.Value = GlobalDownloadStats.DownloadedBytes;
                    Progress_Bar.Maximum = GlobalDownloadStats.TotalBytes;
                    Percent_Label.Text = $"{(Math.Min(GlobalDownloadStats.DownloadedBytes / (double)GlobalDownloadStats.TotalBytes * 100, 99)):F2}%";

                    double totalSize = GlobalDownloadStats.TotalBytes >= 1024L * 1024 * 1024 ? GlobalDownloadStats.TotalBytes / (1024.0 * 1024 * 1024) : GlobalDownloadStats.TotalBytes / (1024.0 * 1024.0);
                    string totalText = GlobalDownloadStats.TotalBytes >= 1024L * 1024 * 1024 ? $"{totalSize:F2} GB" : $"{totalSize:F2} MB";

                    double downloadedSize = GlobalDownloadStats.DownloadedBytes >= 1024L * 1024 * 1024 ? GlobalDownloadStats.DownloadedBytes / (1024.0 * 1024 * 1024) : GlobalDownloadStats.DownloadedBytes / (1024.0 * 1024.0);
                    string downloadedText = GlobalDownloadStats.DownloadedBytes >= 1024L * 1024 * 1024 ? $"{downloadedSize:F2} GB" : $"{downloadedSize:F2} MB";

                    string timeLeft = estimatedRemaining.TotalHours >= 1 ? estimatedRemaining.ToString(@"h\:mm\:ss") : estimatedRemaining.ToString(@"m\:ss");

                    Main_Window.TimeLeft_Label.Text = $"{downloadedText}/{totalText} - Time Left: {timeLeft}";

                    GlobalDownloadStats.timeLeftText = $"Time Left: {timeLeft}";
                    GlobalDownloadStats.totalText = totalText;
                    GlobalDownloadStats.downloadedText = downloadedText;

                    if ((DateTime.UtcNow - lastPresenceUpdate).TotalSeconds >= 5)
                    {

                        string UpdateTypeString = UpdateType == 0 ? "Downloading" : "Repairing";

                        AppState.SetRichPresence($"{UpdateTypeString} {GetBranch.Name()}", $"{downloadedText}/{totalText} - Time Left: {estimatedRemaining:hh\\:mm\\:ss}");
                        lastPresenceUpdate = DateTime.UtcNow;
                    }
                });

                await Task.Delay(100, token);
            }

            Main_Window.TimeLeft_Label.Text = "";
        }

        public static void SetGlobalDownloadStats(long totalBytes, long downloadedBytes, DateTime startTime)
        {
            GlobalDownloadStats.TotalBytes = totalBytes;
            GlobalDownloadStats.DownloadedBytes = downloadedBytes;
            GlobalDownloadStats.StartTime = startTime;
        }
    }
}