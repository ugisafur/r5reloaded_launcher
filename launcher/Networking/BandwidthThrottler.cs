using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace launcher.Networking
{
    public class BandwidthThrottler : IDisposable
    {
        private long _maxBytesPerSecond;
        private long _availableBytes;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly Timer _timer;

        private static readonly Lazy<BandwidthThrottler> _instance =
            new Lazy<BandwidthThrottler>(() => new BandwidthThrottler(10 * 1024 * 1024)); // Default: 10 MB/s

        public static BandwidthThrottler Instance => _instance.Value;

        private BandwidthThrottler(long initialMaxBytesPerSecond)
        {
            _maxBytesPerSecond = initialMaxBytesPerSecond > 0 ? initialMaxBytesPerSecond : long.MaxValue;
            _availableBytes = _maxBytesPerSecond;

            _timer = new Timer(ReplenishTokens, null, 1000, 1000);
        }

        private void ReplenishTokens(object state)
        {
            _semaphore.Wait();
            try
            {
                if (_maxBytesPerSecond > 0)
                {
                    _availableBytes = _maxBytesPerSecond;
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task UpdateLimitAsync(long newMaxBytesPerSecond)
        {
            if (newMaxBytesPerSecond < 0)
                throw new ArgumentOutOfRangeException(nameof(newMaxBytesPerSecond), "Limit cannot be negative.");

            await _semaphore.WaitAsync();
            try
            {
                _maxBytesPerSecond = newMaxBytesPerSecond > 0 ? newMaxBytesPerSecond : long.MaxValue;
                _availableBytes = _maxBytesPerSecond;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task WaitToProceedAsync(int bytes, CancellationToken cancellationToken)
        {
            if (_maxBytesPerSecond == long.MaxValue)
            {
                return;
            }
            
            bool acquired = false;
            while (!acquired)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _semaphore.WaitAsync(cancellationToken);
                try
                {
                    if (_availableBytes >= bytes)
                    {
                        _availableBytes -= bytes;
                        acquired = true;
                    }
                }
                finally
                {
                    _semaphore.Release();
                }

                if (!acquired)
                {
                    await Task.Delay(50, cancellationToken);
                }
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _semaphore?.Dispose();
        }
    }
}