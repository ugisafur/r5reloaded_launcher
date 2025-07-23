using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace launcher.Networking
{
    public class BandwidthThrottler
    {
        private long _maxBytesPerSecond;
        private long _availableBytes;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly Timer _timer;
        private readonly object _lock = new object();

        private static readonly Lazy<BandwidthThrottler> _instance =
            new Lazy<BandwidthThrottler>(() => new BandwidthThrottler(10 * 1024 * 1024)); // Default: 10 MB/s

        public static BandwidthThrottler Instance => _instance.Value;

        private BandwidthThrottler(long initialMaxBytesPerSecond)
        {
            _maxBytesPerSecond = initialMaxBytesPerSecond;
            _availableBytes = initialMaxBytesPerSecond;

            _timer = new Timer(ReplenishTokens, null, 1000, 1000);
        }

        private void ReplenishTokens(object state)
        {
            Interlocked.Exchange(ref _availableBytes, _maxBytesPerSecond);
        }

        public void UpdateLimit(long newMaxBytesPerSecond)
        {
            if (newMaxBytesPerSecond < 0)
                throw new ArgumentOutOfRangeException(nameof(newMaxBytesPerSecond), "Limit cannot be negative.");

            lock (_lock)
            {
                _maxBytesPerSecond = newMaxBytesPerSecond;
                if (_maxBytesPerSecond > 0)
                    Interlocked.Exchange(ref _availableBytes, _maxBytesPerSecond);
                else
                    _availableBytes = long.MaxValue;
            }
        }

        public async Task<bool> AcquireAsync(long bytes, CancellationToken cancellationToken)
        {
            if (_maxBytesPerSecond == 0)
            {
                // Unlimited
                return true;
            }

            while (true)
            {
                await _semaphore.WaitAsync(cancellationToken);
                try
                {
                    if (_availableBytes >= bytes)
                    {
                        _availableBytes -= bytes;
                        return true;
                    }
                }
                finally
                {
                    _semaphore.Release();
                }

                await Task.Delay(100, cancellationToken);
            }
        }
    }
}