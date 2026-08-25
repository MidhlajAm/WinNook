using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Project_Nook.Services;
using System;
using System.Globalization;
using System.Windows.Threading;
using Wpf.Ui.Controls;

namespace Project_Nook.ViewModels
{
    public enum TimerRunState
    {
        Idle,
        Running,
        Paused,
        Finished
    }

    /// <summary>
    /// A countdown / Pomodoro tile.
    ///
    /// Deliberately does NOT implement IWidgetLifecycle: a timer that paused whenever the notch collapsed
    /// would be useless. Instead the tick only exists while a countdown is actually running, so an idle
    /// timer costs nothing either way.
    /// </summary>
    public partial class TimerWidgetViewModel : ObservableObject
    {
        private const string DurationSettingKey = "TimerDurationMinutes";
        private const int DefaultDurationMinutes = 25;

        private readonly IConfigurationService _config;
        private readonly Func<TriggerEngine> _triggerEngine;
        private readonly DispatcherTimer _tick;

        private DateTime _endsAt;
        private TimeSpan _remainingWhenPaused;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Display))]
        [NotifyPropertyChangedFor(nameof(Icon))]
        [NotifyPropertyChangedFor(nameof(Caption))]
        private TimerRunState _state = TimerRunState.Idle;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Display))]
        private TimeSpan _remaining;

        [ObservableProperty]
        private int _durationMinutes;

        public string Display => Remaining.TotalHours >= 1
            ? Remaining.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture)
            : Remaining.ToString(@"mm\:ss", CultureInfo.InvariantCulture);

        public string Caption => State switch
        {
            TimerRunState.Running => "RUNNING",
            TimerRunState.Paused => "PAUSED",
            TimerRunState.Finished => "DONE",
            _ => "TIMER"
        };

        public SymbolRegular Icon => State == TimerRunState.Running
            ? SymbolRegular.Pause24
            : SymbolRegular.Play24;

        public TimerWidgetViewModel(IConfigurationService config, Func<TriggerEngine> triggerEngine)
        {
            _config = config;
            _triggerEngine = triggerEngine;

            DurationMinutes = ReadDuration();
            Remaining = TimeSpan.FromMinutes(DurationMinutes);

            _tick = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            _tick.Tick += Tick_Elapsed;
        }

        private int ReadDuration()
        {
            if (int.TryParse(_config.GetSetting(DurationSettingKey), NumberStyles.Integer,
                             CultureInfo.InvariantCulture, out var minutes) && minutes > 0)
            {
                return minutes;
            }
            return DefaultDurationMinutes;
        }

        partial void OnDurationMinutesChanged(int value)
        {
            _config.SetSetting(DurationSettingKey, value.ToString(CultureInfo.InvariantCulture));

            // Changing the length while nothing is counting rearms the tile at the new duration.
            if (State is TimerRunState.Idle or TimerRunState.Finished)
            {
                State = TimerRunState.Idle;
                Remaining = TimeSpan.FromMinutes(value);
            }
        }

        private void Tick_Elapsed(object sender, EventArgs e)
        {
            var left = _endsAt - DateTime.Now;

            if (left <= TimeSpan.Zero)
            {
                Remaining = TimeSpan.Zero;
                Finish();
                return;
            }

            Remaining = left;
        }

        private void Finish()
        {
            _tick.Stop();
            State = TimerRunState.Finished;

            // Route through the trigger engine so this queues behind volume and battery events properly
            // and inherits the 3-second auto-collapse instead of pinning the notch open.
            _triggerEngine()?.Trigger(4, "Timer finished", SymbolRegular.Timer24);
        }

        /// <summary>The tile's primary action: start, pause, or resume depending on where we are.</summary>
        [RelayCommand]
        private void Toggle()
        {
            switch (State)
            {
                case TimerRunState.Running:
                    _remainingWhenPaused = _endsAt - DateTime.Now;
                    if (_remainingWhenPaused < TimeSpan.Zero) _remainingWhenPaused = TimeSpan.Zero;
                    _tick.Stop();
                    Remaining = _remainingWhenPaused;
                    State = TimerRunState.Paused;
                    break;

                case TimerRunState.Paused:
                    StartCountdown(_remainingWhenPaused);
                    break;

                default:
                    StartCountdown(TimeSpan.FromMinutes(DurationMinutes));
                    break;
            }
        }

        private void StartCountdown(TimeSpan length)
        {
            if (length <= TimeSpan.Zero) length = TimeSpan.FromMinutes(DurationMinutes);

            // Anchored to a wall-clock end time, not accumulated ticks, so a stalled or throttled
            // dispatcher cannot make the countdown drift.
            _endsAt = DateTime.Now + length;
            Remaining = length;
            State = TimerRunState.Running;
            _tick.Start();
        }

        [RelayCommand]
        private void Reset()
        {
            _tick.Stop();
            State = TimerRunState.Idle;
            Remaining = TimeSpan.FromMinutes(DurationMinutes);
        }

        /// <summary>Preset lengths offered on the tile's context menu.</summary>
        [RelayCommand]
        private void SetDuration(string minutes)
        {
            if (!int.TryParse(minutes, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)) return;
            if (value <= 0) return;

            Reset();
            DurationMinutes = value;
        }
    }
}
