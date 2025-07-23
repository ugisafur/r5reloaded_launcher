using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace launcher.Networking
{
    public class DownloadSpeedMonitor
    {
        private readonly TimeSpan _monitorInterval = TimeSpan.FromSeconds(1);
        private long _previousTotalBytes = 0;
        private double _currentSpeedBytesPerSecond = 0;
        private readonly CancellationTokenSource _cts = new();

        public event Action<double> OnSpeedUpdated;

        public DownloadSpeedMonitor()
        {
            Task.Run(() => MonitorSpeedAsync(_cts.Token));
        }

        private async Task MonitorSpeedAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                long currentTotal = DownloadService.GetTotalDownloadedBytes();
                long bytesThisInterval = currentTotal - _previousTotalBytes;
                _previousTotalBytes = currentTotal;

                _currentSpeedBytesPerSecond = bytesThisInterval / _monitorInterval.TotalSeconds;

                OnSpeedUpdated?.Invoke(_currentSpeedBytesPerSecond);

                await Task.Delay(_monitorInterval, cancellationToken);
            }
        }

        public double GetCurrentSpeedBytesPerSecond()
        {
            return _currentSpeedBytesPerSecond;
        }

        public void Stop()
        {
            _cts.Cancel();
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}