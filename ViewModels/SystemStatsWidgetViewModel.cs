using CommunityToolkit.Mvvm.ComponentModel;
using Project_Nook.Services;
using Project_Nook.Widgets;

namespace Project_Nook.ViewModels
{
    public partial class SystemStatsWidgetViewModel : ObservableObject, IWidgetLifecycle
    {
        private readonly SystemStatsService _stats;
        private bool _isSubscribed;

        [ObservableProperty]
        private int _cpuPercent;

        [ObservableProperty]
        private int _memoryPercent;

        [ObservableProperty]
        private string _memoryDetail = "";

        public SystemStatsWidgetViewModel(SystemStatsService stats)
        {
            _stats = stats;
        }

        /// <summary>Polling only runs while the dashboard is open. A tray app should not sample CPU forever.</summary>
        public void OnActivated()
        {
            if (_isSubscribed) return;

            _stats.StatsChanged += Stats_StatsChanged;
            _stats.Start();
            _isSubscribed = true;
        }

        public void OnDeactivated()
        {
            if (!_isSubscribed) return;

            _stats.StatsChanged -= Stats_StatsChanged;
            _stats.Stop();
            _isSubscribed = false;
        }

        private void Stats_StatsChanged(object sender, SystemStats e)
        {
            // The service ticks on a DispatcherTimer, so this is already the UI thread.
            CpuPercent = e.CpuPercent;
            MemoryPercent = e.MemoryPercent;
            MemoryDetail = $"{e.UsedMemoryGb:0.0} / {e.TotalMemoryGb:0.0} GB";
        }
    }
}
