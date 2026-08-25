using Project_Nook.Services.SystemEvents;
using Project_Nook.ViewModels;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Project_Nook.Services
{
    public class TriggerEngine
    {
        private readonly IslandViewModel _islandVm;
        private readonly AudioEventService _audio;
        private readonly MediaEventService _media;
        private readonly PowerEventService _power;
        private readonly ForegroundWindowService _foregroundWindow;
        private readonly IConfigurationService _configurationService;
        private readonly Dispatcher _dispatcher;

        private int _currentPriority = 0;
        private DispatcherTimer _collapseTimer;

        public TriggerEngine(
            IslandViewModel islandVm, 
            AudioEventService audio, 
            MediaEventService media, 
            PowerEventService power,
            ForegroundWindowService foregroundWindow,
            IConfigurationService configurationService)
        {
            _islandVm = islandVm;
            _audio = audio;
            _media = media;
            _power = power;
            _foregroundWindow = foregroundWindow;
            _configurationService = configurationService;
            _dispatcher = Dispatcher.CurrentDispatcher;

            _collapseTimer = new DispatcherTimer { Interval = System.TimeSpan.FromSeconds(3) };
            _collapseTimer.Tick += CollapseTimer_Tick;

            _audio.VolumeChanged += Audio_VolumeChanged;
            _power.PowerChanged += Power_PowerChanged;
            _foregroundWindow.ForegroundAppChanged += ForegroundWindow_ForegroundAppChanged;
        }

        private void ForegroundWindow_ForegroundAppChanged(object? sender, string exeName)
        {
            _dispatcher.Invoke(() =>
            {
                if (string.IsNullOrEmpty(exeName))
                {
                    _islandVm.IsHidden = false;
                    return;
                }

                var diveInApps = _configurationService.GetDiveInApps();
                bool isHidden = diveInApps.Contains(exeName, System.StringComparer.OrdinalIgnoreCase);
                _islandVm.IsHidden = isHidden;
            });
        }

        private void CollapseTimer_Tick(object sender, System.EventArgs e)
        {
            _collapseTimer.Stop();
            _currentPriority = 0;
            if (_islandVm.CurrentState == Models.IslandState.Triggered)
            {
                _islandVm.CurrentState = Models.IslandState.Idle;
                _islandVm.NotificationText = string.Empty;
                _islandVm.TriggerIcon = Wpf.Ui.Controls.SymbolRegular.Empty;
            }
        }

        /// <summary>
        /// Raises a transient notch notification. Public so widgets (a finished timer, for instance) go
        /// through the same priority gate, auto-collapse timer, and trigger-text restore as system events,
        /// instead of writing CurrentState directly and fighting all three.
        /// </summary>
        public void Trigger(int priority, string text, Wpf.Ui.Controls.SymbolRegular icon = Wpf.Ui.Controls.SymbolRegular.Empty)
        {
            _dispatcher.Invoke(() =>
            {
                if (priority >= _currentPriority && _islandVm.CurrentState != Models.IslandState.Expanded)
                {
                    _currentPriority = priority;
                    _islandVm.NotificationText = text;
                    _islandVm.TriggerIcon = icon;
                    _islandVm.CurrentState = Models.IslandState.Triggered;
                    _collapseTimer.Stop();
                    _collapseTimer.Start();
                }
            });
        }

        private void Power_PowerChanged(object sender, PowerInfo e)
        {
            int level = (e.BatteryPercent + 5) / 10;
            if (level > 10) level = 10;
            if (level < 0) level = 0;

            Wpf.Ui.Controls.SymbolRegular icon = Wpf.Ui.Controls.SymbolRegular.Empty;
            string iconName = e.IsCharging ? $"BatteryCharge{level}24" : $"Battery{level}24";
            
            if (System.Enum.TryParse<Wpf.Ui.Controls.SymbolRegular>(iconName, out var parsed))
            {
                icon = parsed;
            }
            else if (e.IsCharging)
            {
                icon = Wpf.Ui.Controls.SymbolRegular.BatteryCharge24;
            }

            if (e.IsCharging && e.BatteryPercent == 100)
            {
                Trigger(3, "Battery Full", icon);
            }
            else if (e.IsCharging)
            {
                Trigger(3, $"Charging: {e.BatteryPercent}%", icon);
            }
            else if (e.BatteryPercent <= 20)
            {
                Trigger(3, $"Battery Low: {e.BatteryPercent}%", icon);
            }
        }



        private void Audio_VolumeChanged(object sender, float volume)
        {
            int vol = (int)(volume * 100);
            Wpf.Ui.Controls.SymbolRegular icon = vol == 0 ? Wpf.Ui.Controls.SymbolRegular.SpeakerOff24 : Wpf.Ui.Controls.SymbolRegular.Speaker224;
            Trigger(2, $"Volume {vol}%", icon);
        }
    }
}
