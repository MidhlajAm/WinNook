using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace Project_Nook.ViewModels
{
    public partial class WidgetDashboardViewModel : ObservableObject
    {
        public Action OpenShelfAction { get; set; }
        public Action OpenControlCenterAction { get; set; }

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
