using System;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace Project_Nook.Services
{
    public class SystemStats
    {
        /// <summary>Whole-system CPU load, 0-100.</summary>
        public int CpuPercent { get; set; }

        /// <summary>Physical memory in use, 0-100.</summary>
        public int MemoryPercent { get; set; }

        public double UsedMemoryGb { get; set; }
        public double TotalMemoryGb { get; set; }
    }

    /// <summary>
    /// Whole-machine CPU and RAM, sampled on a timer.
    ///
    /// Uses GetSystemTimes and GlobalMemoryStatusEx rather than System.Diagnostics.PerformanceCounter:
    /// same P/Invoke approach PowerEventService already takes, and it avoids pulling in another package
    /// plus the multi-second first-sample warmup that PerformanceCounter needs.
    ///
    /// Start/Stop are reference counted so several widgets could share one poll.
    /// </summary>
    public class SystemStatsService : IDisposable
    {
        private readonly DispatcherTimer _timer;
        private int _subscribers;

        private ulong _lastIdle;
        private ulong _lastKernel;
        private ulong _lastUser;
        private bool _hasBaseline;

        public event EventHandler<SystemStats> StatsChanged;

        public SystemStatsService()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _timer.Tick += (s, e) => Sample();
        }

        public void Start()
        {
            _subscribers++;
            if (_subscribers != 1) return;

            // The first sample after a gap has no delta to compare against, so discard the baseline
            // and let the next tick produce the first real reading.
            _hasBaseline = false;
            Sample();
            _timer.Start();
        }

        public void Stop()
        {
            if (_subscribers > 0) _subscribers--;
            if (_subscribers == 0) _timer.Stop();
        }

        private void Sample()
        {
            var stats = new SystemStats
            {
                CpuPercent = ReadCpuPercent(),
                MemoryPercent = 0
            };

            var memory = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
            if (GlobalMemoryStatusEx(ref memory))
            {
                const double bytesPerGb = 1024d * 1024d * 1024d;
                stats.MemoryPercent = (int)memory.dwMemoryLoad;
                stats.TotalMemoryGb = memory.ullTotalPhys / bytesPerGb;
                stats.UsedMemoryGb = (memory.ullTotalPhys - memory.ullAvailPhys) / bytesPerGb;
            }

            StatsChanged?.Invoke(this, stats);
        }

        private int ReadCpuPercent()
        {
            if (!GetSystemTimes(out var idleTime, out var kernelTime, out var userTime))
                return 0;

            ulong idle = ToUInt64(idleTime);
            ulong kernel = ToUInt64(kernelTime);
            ulong user = ToUInt64(userTime);

            if (!_hasBaseline)
            {
                _lastIdle = idle;
                _lastKernel = kernel;
                _lastUser = user;
                _hasBaseline = true;
                return 0;
            }

            ulong idleDelta = idle - _lastIdle;
            ulong kernelDelta = kernel - _lastKernel;
            ulong userDelta = user - _lastUser;

            _lastIdle = idle;
            _lastKernel = kernel;
            _lastUser = user;

            // Kernel time already includes idle time, so total is kernel + user and busy is total - idle.
            ulong total = kernelDelta + userDelta;
            if (total == 0) return 0;

            double busy = (total - idleDelta) * 100d / total;
            if (busy < 0) busy = 0;
            if (busy > 100) busy = 100;

            return (int)Math.Round(busy);
        }

        private static ulong ToUInt64(FILETIME time) =>
            ((ulong)(uint)time.dwHighDateTime << 32) | (uint)time.dwLowDateTime;

        public void Dispose()
        {
            _timer?.Stop();
            _subscribers = 0;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetSystemTimes(out FILETIME lpIdleTime, out FILETIME lpKernelTime, out FILETIME lpUserTime);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        [StructLayout(LayoutKind.Sequential)]
        private struct FILETIME
        {
            public int dwLowDateTime;
            public int dwHighDateTime;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }
    }
}
