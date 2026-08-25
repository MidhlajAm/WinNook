using Project_Nook.Widgets;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace Project_Nook.ViewModels
{
    public class CalendarWidgetViewModel : INotifyPropertyChanged, IWidgetLifecycle
    {
        private readonly DispatcherTimer _timer;

        public string DayOfWeek => DateTime.Now.ToString("dddd");
        public string DayNumber => DateTime.Now.ToString("dd");
        public string Month => DateTime.Now.ToString("MMM").ToUpper();

        public CalendarWidgetViewModel()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(1)
            };
            _timer.Tick += (s, e) => Refresh();
        }

        /// <summary>
        /// The clock only needs to run while the dashboard is on screen. Starting it in the constructor
        /// meant a minute-timer ticking forever behind a collapsed notch.
        /// </summary>
        public void OnActivated()
        {
            Refresh();
            _timer.Start();
        }

        public void OnDeactivated()
        {
            _timer.Stop();
        }

        private void Refresh()
        {
            OnPropertyChanged(nameof(DayOfWeek));
            OnPropertyChanged(nameof(DayNumber));
            OnPropertyChanged(nameof(Month));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null!)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
