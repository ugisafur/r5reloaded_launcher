using System.IO;

namespace launcher.Networking
{
    public class ThrottledStream : Stream
    {
        private readonly Stream _baseStream;

        public ThrottledStream(Stream baseStream)
        {
            _baseStream = baseStream;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var bytesRead = await _baseStream.ReadAsync(buffer, offset, count, cancellationToken);
            if (bytesRead > 0)
            {
                await BandwidthThrottler.Instance.WaitToProceedAsync(bytesRead, cancellationToken);
            }
            return bytesRead;
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
            var bytesRead = _baseStream.Read(buffer, offset, count);
            if (bytesRead > 0)
            {
                BandwidthThrottler.Instance.WaitToProceedAsync(bytesRead, CancellationToken.None).Wait();
            }
            return bytesRead;
        }
    }
}