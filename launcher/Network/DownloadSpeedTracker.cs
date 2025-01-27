using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace launcher.Network
{
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
}