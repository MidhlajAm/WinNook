using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Project_Nook.Widgets;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Project_Nook.ViewModels
{
    public partial class WidgetDashboardViewModel : ObservableObject
    {
        private readonly WidgetRegistry _registry;
        private bool _isActive;

        public Action OpenShelfAction { get; set; }
        public Action OpenControlCenterAction { get; set; }

        /// <summary>
        /// Every enabled widget in one strip, in user order.
        ///
        /// Wide widgets and tiles deliberately share a single list rather than sitting in separate zones:
        /// with a fixed wide-then-tiles layout, reordering the only wide widget (Music) changed its Order
        /// but could never move it on screen, so the Activity tab's arrows appeared to do nothing.
        /// </summary>
        public ObservableCollection<WidgetSlot> Widgets { get; } = new();

        public WidgetDashboardViewModel(WidgetRegistry registry)
        {
            _registry = registry;
            _registry.LayoutChanged += (s, e) => RebuildLayout();
            RebuildLayout();
        }

        private void RebuildLayout()
        {
            // Widgets that just left the dashboard should stop doing work immediately.
            foreach (var slot in Widgets)
            {
                if (!slot.IsEnabled) slot.Lifecycle?.OnDeactivated();
            }

            Widgets.Clear();
            foreach (var slot in _registry.Visible()) Widgets.Add(slot);

            // A widget switched on while the dashboard is already open needs starting now, not on next open.
            if (_isActive) SetActive(true);
        }

        /// <summary>
        /// Called as the dashboard comes into and goes out of view. Widgets that hold a timer or a poll
        /// only run between these two calls; anything that must stay live while the notch is collapsed
        /// simply does not implement IWidgetLifecycle.
        /// </summary>
        public void SetActive(bool isActive)
        {
            _isActive = isActive;

            foreach (var slot in Widgets)
            {
                // Activating has to force the view into existence: until the dashboard has actually been
                // measured, nothing has touched slot.View and there is no view model to call yet.
                // Deactivating never forces it — a widget with no view has nothing running.
                if (isActive) _ = slot.View;
                else if (!slot.HasView) continue;

                var lifecycle = slot.Lifecycle;
                if (lifecycle == null) continue;

                if (isActive) lifecycle.OnActivated();
                else lifecycle.OnDeactivated();
            }
        }

        [RelayCommand]
        private void OpenShelf()
        {
            OpenShelfAction?.Invoke();
        }

        [RelayCommand]
        private void OpenControlCenter()
        {
            OpenControlCenterAction?.Invoke();
        }
    }
}
