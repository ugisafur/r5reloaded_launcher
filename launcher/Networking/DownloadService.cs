using launcher.GameLifecycle.Models;
using launcher.Services;
using static launcher.Core.AppContext;
using static launcher.Networking.Models.DownloadProgress;

namespace launcher.Networking
{
    public static class DownloadService
    {
        public static int UpdateType = 0; // 0 = install, 1 = repair, 2 = uninstall

        private static long _totalDownloadedBytes = 0;

        public static long _downloadSpeedLimit = 0;
        public static SemaphoreSlim _downloadSemaphore;
        public static double currentDownloadSpeed = 0;

        public static DateTime speedCheckStart = DateTime.Now;

        private static readonly TimeSpan _monitorInterval = TimeSpan.FromSeconds(1);
        private static long _previousTotalBytes = 0;
        private static CancellationTokenSource _speedMonitorCts;

        public static void AddDownloadedBytes(long bytes, ManifestEntry file)
        {
            Interlocked.Add(ref _totalDownloadedBytes, bytes);
            Interlocked.Add(ref DownloadedBytes, bytes);
            file.downloadContext.downloadProgress.downloadedBytes += bytes;
        }

        public static void RemoveDownloadedBytes(long bytes)
        {
            Interlocked.Add(ref _totalDownloadedBytes, -bytes);
            Interlocked.Add(ref DownloadedBytes, -bytes);
        }

        public static void Reset()
        {
            Interlocked.Exchange(ref _totalDownloadedBytes, 0);
            Interlocked.Exchange(ref DownloadedBytes, 0);
        }

        public static long GetTotalDownloadedBytes()
        {
            return Interlocked.Read(ref _totalDownloadedBytes);
        }

        public static void ConfigureConcurrency()
        {
            int maxConcurrentDownloads = (int)SettingsService.Get(SettingsService.Vars.Concurrent_Downloads);
            _downloadSemaphore?.Dispose();
            _downloadSemaphore = new SemaphoreSlim(maxConcurrentDownloads);
        }

        public static async void ConfigureDownloadSpeed()
        {
            int speedLimitKb = (int)SettingsService.Get(SettingsService.Vars.Download_Speed_Limit);
            _downloadSpeedLimit = speedLimitKb > 0 ? speedLimitKb * 1024 : 0;
            await BandwidthThrottler.Instance.UpdateLimitAsync(_downloadSpeedLimit);
        }

        public static SemaphoreSlim GetSemaphoreSlim()
        {
            return _downloadSemaphore;
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
                Downloads_Control.Speed_Label.Text = $"{TimeLeftText}  |  {DownloadedText}/{TotalText}  |  {speedText}";
            });
        }

        public static void StartSpeedMonitor()
        {
            if (_speedMonitorCts != null && !_speedMonitorCts.IsCancellationRequested)
            {
                return; // Already running
            }
            _speedMonitorCts?.Dispose();
            _speedMonitorCts = new CancellationTokenSource();
            _previousTotalBytes = GetTotalDownloadedBytes();
            Task.Run(() => MonitorSpeedAsync(_speedMonitorCts.Token));
        }

        public static void StopSpeedMonitor()
        {
            _speedMonitorCts?.Cancel();
            _speedMonitorCts?.Dispose();
            _speedMonitorCts = null;

            currentDownloadSpeed = 0;

            appDispatcher.Invoke(() =>
            {
                if (Speed_Label != null && Downloads_Control != null)
                {
                    Speed_Label.Text = "0.00 KB/s";
                    Downloads_Control.Speed_Label.Text = "";
                }
            });
        }

        private static async Task MonitorSpeedAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(_monitorInterval, cancellationToken);

                if (cancellationToken.IsCancellationRequested) break;

                long currentTotal = GetTotalDownloadedBytes();
                long bytesThisInterval = currentTotal - _previousTotalBytes;
                _previousTotalBytes = currentTotal;

                double speed = bytesThisInterval / _monitorInterval.TotalSeconds;

                UpdateDownloadSpeedUI(speed);
            }
        }

        public static async Task UpdateGlobalDownloadProgressAsync(CancellationToken token)
        {
            DateTime lastPresenceUpdate = DateTime.MinValue;

            while (!token.IsCancellationRequested)
            {
                var elapsed = DateTime.Now - StartTime;
                double avgSpeed = elapsed.TotalSeconds > 0
                    ? DownloadedBytes / elapsed.TotalSeconds
                    : 0;

                // Use the current speed if available, otherwise fall back to average
                double currentSpeed = currentDownloadSpeed; // in bytes/sec
                double effectiveSpeed = currentSpeed > 0
                    ? (avgSpeed + currentSpeed) / 2   // blend average + current
                    : avgSpeed;

                long remainingBytes = TotalBytes - DownloadedBytes;
                TimeSpan estimatedRemaining = effectiveSpeed > 0
                    ? TimeSpan.FromSeconds(remainingBytes / effectiveSpeed)
                    : TimeSpan.Zero;

                await appDispatcher.InvokeAsync(() =>
                {
                    Progress_Bar.Value = DownloadedBytes;
                    Progress_Bar.Maximum = TotalBytes;
                    Percent_Label.Text = $"{(Math.Min(DownloadedBytes / (double)TotalBytes * 100, 99)):F2}%";

                    double totalSize = TotalBytes >= 1024L * 1024 * 1024 ? TotalBytes / (1024.0 * 1024 * 1024) : TotalBytes / (1024.0 * 1024.0);
                    string totalText = TotalBytes >= 1024L * 1024 * 1024 ? $"{totalSize:F2} GB" : $"{totalSize:F2} MB";

                    double downloadedSize = DownloadedBytes >= 1024L * 1024 * 1024 ? DownloadedBytes / (1024.0 * 1024 * 1024) : DownloadedBytes / (1024.0 * 1024.0);
                    string downloadedText = DownloadedBytes >= 1024L * 1024 * 1024 ? $"{downloadedSize:F2} GB" : $"{downloadedSize:F2} MB";

                    string timeLeft = estimatedRemaining.TotalHours >= 1 ? estimatedRemaining.ToString(@"h\:mm\:ss") : estimatedRemaining.ToString(@"m\:ss");

                    Main_Window.TimeLeft_Label.Text = $"{downloadedText}/{totalText} - Time Left: {timeLeft}";

                    TimeLeftText = $"Time Left: {timeLeft}";
                    TotalText = totalText;
                    DownloadedText = downloadedText;

                    if ((DateTime.UtcNow - lastPresenceUpdate).TotalSeconds >= 5)
                    {

                        string UpdateTypeString = UpdateType == 0 ? "Downloading" : "Repairing";

                        DiscordService.SetRichPresence($"{UpdateTypeString} {ReleaseChannelService.GetName()}", $"{downloadedText}/{totalText} - Time Left: {estimatedRemaining:hh\\:mm\\:ss}");
                        lastPresenceUpdate = DateTime.UtcNow;
                    }
                });

                await Task.Delay(100, token);
            }

            Main_Window.TimeLeft_Label.Text = "";
        }

        public static void SetGlobalDownloadStats(long totalBytes, long downloadedBytes, DateTime startTime)
        {
            TotalBytes = totalBytes;
            DownloadedBytes = downloadedBytes;
            StartTime = startTime;
        }
    }
}