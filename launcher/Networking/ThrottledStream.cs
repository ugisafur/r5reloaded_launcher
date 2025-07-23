using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace launcher.Networking
{
    public class ThrottledStream : Stream
    {
        private readonly Stream _baseStream;
        private readonly BandwidthThrottler _baseGlobalBandwidthLimiter;

        public ThrottledStream(Stream baseStream, BandwidthThrottler baseGlobalBandwidthLimiter)
        {
            _baseStream = baseStream;
            _baseGlobalBandwidthLimiter = baseGlobalBandwidthLimiter;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            bool acquired = await BandwidthThrottler.Instance.AcquireAsync(count, cancellationToken);
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
}