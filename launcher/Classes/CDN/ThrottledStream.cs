using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            // Acquire permission from the global limiter
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

        // Implement other required members...
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

            // Replenish tokens every second
            _timer = new Timer(ReplenishTokens, null, 1000, 1000);
        }

        private void ReplenishTokens(object state)
        {
            Interlocked.Exchange(ref _availableBytes, _maxBytesPerSecond);
        }

        /// <summary>
        /// Updates the global download speed limit.
        /// </summary>
        /// <param name="newMaxBytesPerSecond">New speed limit in bytes per second.</param>
        public void UpdateLimit(long newMaxBytesPerSecond)
        {
            if (newMaxBytesPerSecond < 0)
                throw new ArgumentOutOfRangeException(nameof(newMaxBytesPerSecond), "Limit cannot be negative.");

            lock (_lock)
            {
                _maxBytesPerSecond = newMaxBytesPerSecond;
                if (_maxBytesPerSecond > 0)
                {
                    Interlocked.Exchange(ref _availableBytes, _maxBytesPerSecond);
                }
                else
                {
                    // When limit is 0, set a flag to indicate unlimited
                    // This requires modifying the AcquireAsync method accordingly
                    _availableBytes = long.MaxValue;
                }
            }
        }

        /// <summary>
        /// Attempts to acquire permission to read a specified number of bytes.
        /// </summary>
        /// <param name="bytes">Number of bytes to acquire.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task<bool> AcquireAsync(long bytes, CancellationToken cancellationToken)
        {
            if (_maxBytesPerSecond == 0)
            {
                // Unlimited: Allow all bytes without throttling
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

                // Wait a short time before retrying
                await Task.Delay(100, cancellationToken);
            }
        }
    }
}