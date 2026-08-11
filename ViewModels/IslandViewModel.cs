using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Project_Nook.Models;
using Project_Nook.Services;

namespace Project_Nook.ViewModels
{
    public partial class IslandViewModel : ObservableObject
    {
        private readonly IConfigurationService _config;

        [ObservableProperty]
        private IslandState _currentState = IslandState.Idle;

        [ObservableProperty]
        private bool _isIslandEnabled;
        partial void OnIsIslandEnabledChanged(bool value) => _config.SetSetting("IsIslandEnabled", value.ToString());

        [ObservableProperty]
        private string _notchColor;
        partial void OnNotchColorChanged(string value) => _config.SetSetting("NotchColor", value);

        [ObservableProperty]
        private string _shadowColor;
        partial void OnShadowColorChanged(string value) => _config.SetSetting("ShadowColor", value);

        [ObservableProperty]
        private double _shadowOpacity;
        partial void OnShadowOpacityChanged(double value) => _config.SetSetting("ShadowOpacity", value.ToString());

        [ObservableProperty]
        private double _shadowBlurRadius;
        partial void OnShadowBlurRadiusChanged(double value) => _config.SetSetting("ShadowBlurRadius", value.ToString());

        [ObservableProperty]
        private double _cornerRadius;
        partial void OnCornerRadiusChanged(double value) => _config.SetSetting("CornerRadius", value.ToString());

        [ObservableProperty]
        private double _idleWidth;
        partial void OnIdleWidthChanged(double value) => _config.SetSetting("IdleWidth", value.ToString());

        [ObservableProperty]
        private double _idleHeight;
        partial void OnIdleHeightChanged(double value) => _config.SetSetting("IdleHeight", value.ToString());

        [ObservableProperty]
        private int _animationSpeedMs;
        partial void OnAnimationSpeedMsChanged(int value) => _config.SetSetting("AnimationSpeedMs", value.ToString());

        public IslandViewModel(IConfigurationService config)
        {
            _config = config;
            
            NotchColor = _config.GetSetting("NotchColor") ?? "#000000";
            ShadowColor = _config.GetSetting("ShadowColor") ?? "#000000";
            
            if (bool.TryParse(_config.GetSetting("IsIslandEnabled"), out var isEnabled)) IsIslandEnabled = isEnabled; else IsIslandEnabled = true;
            if (double.TryParse(_config.GetSetting("ShadowOpacity"), out var sOpacity)) ShadowOpacity = sOpacity; else ShadowOpacity = 0.5;
            if (double.TryParse(_config.GetSetting("ShadowBlurRadius"), out var sBlur)) ShadowBlurRadius = sBlur; else ShadowBlurRadius = 20.0;
            if (double.TryParse(_config.GetSetting("CornerRadius"), out var cRadius)) CornerRadius = cRadius; else CornerRadius = 24.0;
            if (double.TryParse(_config.GetSetting("IdleWidth"), out var iWidth)) IdleWidth = iWidth; else IdleWidth = 200.0;
            if (double.TryParse(_config.GetSetting("IdleHeight"), out var iHeight)) IdleHeight = iHeight; else IdleHeight = 40.0;
            if (int.TryParse(_config.GetSetting("AnimationSpeedMs"), out var asm)) AnimationSpeedMs = asm; else AnimationSpeedMs = 500;
        }

        [RelayCommand]
        private void Hover()
        {
            if (CurrentState == IslandState.Idle)
                CurrentState = IslandState.Hover;
        }

        [RelayCommand]
        private void Unhover()
        {
            if (CurrentState == IslandState.Hover)
                CurrentState = IslandState.Idle;
        }

        [RelayCommand]
        private void Expand()
        {
            CurrentState = IslandState.Expanded;
        }

        [RelayCommand]
        private void Collapse()
        {
            CurrentState = IslandState.Idle;
        }

        [RelayCommand]
        private void ResetSettings()
        {
            IsIslandEnabled = true;
            NotchColor = "#000000";
            ShadowColor = "#000000";
            ShadowOpacity = 0.5;
            ShadowBlurRadius = 20.0;
            CornerRadius = 24.0;
            IdleWidth = 200.0;
            IdleHeight = 40.0;
            AnimationSpeedMs = 500;
        }
    }
}
