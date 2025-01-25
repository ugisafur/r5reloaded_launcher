using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace launcher.Classes.CDN
{
    public class ThrottledStream : Stream
    {
        private readonly Stream _baseStream;
        private readonly GlobalBandwidthLimiter _baseGlobalBandwidthLimiter;

        public ThrottledStream(Stream baseStream, GlobalBandwidthLimiter baseGlobalBandwidthLimiter)
        {
            _baseStream = baseStream;
            _baseGlobalBandwidthLimiter = baseGlobalBandwidthLimiter;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            bool acquired = await GlobalBandwidthLimiter.Instance.AcquireAsync(count, cancellationToken);
            if (acquired)
            {
                return await _baseStream.ReadAsync(buffer, offset, count, cancellationToken);
            }
            else
            {
                throw new TimeoutException("Failed to acquire bandwidth.");
            }
        }

        public override bool CanRead => _baseStream.CanRead;

        public override bool CanSeek => _baseStream.CanSeek;
        public override bool CanWrite => _baseStream.CanWrite;
        public override long Length => _baseStream.Length;

        public override long Position
        {
            get => _baseStream.Position;
            set => _baseStream.Position = value;
        }

        public override void Flush() => _baseStream.Flush();

        public override long Seek(long offset, SeekOrigin origin) => _baseStream.Seek(offset, origin);

        public override void SetLength(long value) => _baseStream.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) => _baseStream.Write(buffer, offset, count);

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _baseStream.Read(buffer, offset, count);
        }
    }

    public class GlobalBandwidthLimiter
    {
        private long _maxBytesPerSecond;
        private long _availableBytes;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly Timer _timer;
        private readonly object _lock = new object();

        private static readonly Lazy<GlobalBandwidthLimiter> _instance =
            new Lazy<GlobalBandwidthLimiter>(() => new GlobalBandwidthLimiter(10 * 1024 * 1024)); // Default: 10 MB/s

        public static GlobalBandwidthLimiter Instance => _instance.Value;

        private GlobalBandwidthLimiter(long initialMaxBytesPerSecond)
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

    public static class DownloadSpeedTracker
    {
        private static long _totalDownloadedBytes = 0;
        private static readonly object _lock = new object();

        public static void AddDownloadedBytes(long bytes)
        {
            Interlocked.Add(ref _totalDownloadedBytes, bytes);
        }

        public static void Reset()
        {
            Interlocked.Exchange(ref _totalDownloadedBytes, 0);
        }

        public static long GetTotalDownloadedBytes()
        {
            return Interlocked.Read(ref _totalDownloadedBytes);
        }
    }

    public class DownloadSpeedMonitor
    {
        private readonly TimeSpan _monitorInterval = TimeSpan.FromSeconds(1);
        private long _previousTotalBytes = 0;
        private double _currentSpeedBytesPerSecond = 0;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public event Action<double> OnSpeedUpdated;

        public DownloadSpeedMonitor()
        {
            Task.Run(() => MonitorSpeedAsync(_cts.Token));
        }

        private async Task MonitorSpeedAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                long currentTotal = DownloadSpeedTracker.GetTotalDownloadedBytes();
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