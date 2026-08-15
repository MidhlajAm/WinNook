using System;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace Project_Nook.Services.SystemEvents
{
    public class PowerEventService : IDisposable
    {
        private readonly DispatcherTimer _timer;
        private byte _lastBatteryPercent = 255;
        private byte _lastAcStatus = 255;

        public event EventHandler<PowerInfo> PowerChanged;

        public PowerEventService()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (GetSystemPowerStatus(out var status))
            {
                // Only trigger if charging status changed or battery dropped by 5% increments
                bool acChanged = _lastAcStatus != 255 && _lastAcStatus != status.ACLineStatus;
                bool batteryDroppedSignificantly = _lastBatteryPercent != 255 && 
                                                   status.BatteryLifePercent <= 20 && 
                                                   _lastBatteryPercent > 20;

                if (acChanged || batteryDroppedSignificantly)
                {
                    PowerChanged?.Invoke(this, new PowerInfo
                    {
                        IsCharging = status.ACLineStatus == 1,
                        BatteryPercent = status.BatteryLifePercent
                    });
                }

                _lastAcStatus = status.ACLineStatus;
                _lastBatteryPercent = status.BatteryLifePercent;
            }
        }

        public void Dispose()
        {
            _timer?.Stop();
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS sps);

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEM_POWER_STATUS
        {
            public byte ACLineStatus;
            public byte BatteryFlag;
            public byte BatteryLifePercent;
            public byte SystemStatusFlag;
            public int BatteryLifeTime;
            public int BatteryFullLifeTime;
        }
    }

    public class PowerInfo
    {
        public bool IsCharging { get; set; }
        public byte BatteryPercent { get; set; }
    }
}
