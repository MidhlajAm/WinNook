using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Project_Nook.Services;

namespace Project_Nook.ViewModels
{
    public partial class ControlCenterViewModel : ObservableObject
    {
        private readonly IConfigurationService _configurationService;

        [ObservableProperty]
        private string _greeting = "Welcome to WinNook Control Center!";

        public IslandViewModel Island { get; }

        public ControlCenterViewModel(IConfigurationService configurationService, IslandViewModel islandViewModel)
        {
            _configurationService = configurationService;
            Island = islandViewModel;
        }

        [RelayCommand]
        private void UpdateGreeting()
        {
            Greeting = "WinNook is ready!";
        }

        [RelayCommand]
        private void HoverIsland() => Island.HoverCommand.Execute(null);

        [RelayCommand]
        private void UnhoverIsland() => Island.UnhoverCommand.Execute(null);

        [RelayCommand]
        private void ExpandIsland() => Island.ExpandCommand.Execute(null);

        [RelayCommand]
        private void CollapseIsland() => Island.CollapseCommand.Execute(null);
    }
}
