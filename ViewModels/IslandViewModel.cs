using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Project_Nook.Models;
using Project_Nook.Services;

namespace Project_Nook.ViewModels
{
    public partial class IslandViewModel : ObservableObject
    {
        private readonly IConfigurationService _config;
        private bool _isInitializing = false;

        [ObservableProperty]
        private IslandState _currentState = IslandState.Idle;

        [ObservableProperty]
        private string _triggerText = "WinNook";
        partial void OnTriggerTextChanged(string value) { if (!_isInitializing) _config.SetSetting("TriggerText", value); }

        [ObservableProperty]
        private Wpf.Ui.Controls.SymbolRegular _triggerIcon = Wpf.Ui.Controls.SymbolRegular.Empty;

        [ObservableProperty]
        private bool _isIslandEnabled;
        partial void OnIsIslandEnabledChanged(bool value) { if (!_isInitializing) _config.SetSetting("IsIslandEnabled", value.ToString()); }

        [ObservableProperty]
        private bool _isHidden;

        [ObservableProperty]
        private bool _isShelfVisible;

        public WidgetDashboardViewModel DashboardViewModel { get; } = new();

        [ObservableProperty]
        private bool _isMusicWidgetEnabled;

        [ObservableProperty]
        private bool _enableFillets;
        partial void OnEnableFilletsChanged(bool value) { if (!_isInitializing) _config.SetSetting("EnableFillets", value.ToString()); }

        [ObservableProperty]
        private string _notchColor;
        partial void OnNotchColorChanged(string value) { if (!_isInitializing) _config.SetSetting("NotchColor", value); }

        [ObservableProperty]
        private string _shadowColor;
        partial void OnShadowColorChanged(string value) { if (!_isInitializing) _config.SetSetting("ShadowColor", value); }

        [ObservableProperty]
        private double _shadowOpacity;
        partial void OnShadowOpacityChanged(double value) { if (!_isInitializing) _config.SetSetting("ShadowOpacity", value.ToString(System.Globalization.CultureInfo.InvariantCulture)); }

        [ObservableProperty]
        private double _shadowBlurRadius;
        partial void OnShadowBlurRadiusChanged(double value) { if (!_isInitializing) _config.SetSetting("ShadowBlurRadius", value.ToString(System.Globalization.CultureInfo.InvariantCulture)); }

        [ObservableProperty]
        private double _cornerRadius;
        partial void OnCornerRadiusChanged(double value) { if (!_isInitializing) _config.SetSetting("CornerRadius", value.ToString(System.Globalization.CultureInfo.InvariantCulture)); }

        [ObservableProperty]
        private double _hoverCornerRadius;
        partial void OnHoverCornerRadiusChanged(double value) { if (!_isInitializing) _config.SetSetting("HoverCornerRadius", value.ToString(System.Globalization.CultureInfo.InvariantCulture)); }

        [ObservableProperty]
        private double _expandedCornerRadius;
        partial void OnExpandedCornerRadiusChanged(double value) { if (!_isInitializing) _config.SetSetting("ExpandedCornerRadius", value.ToString(System.Globalization.CultureInfo.InvariantCulture)); }

        [ObservableProperty]
        private double _idleWidth;
        partial void OnIdleWidthChanged(double value) { if (!_isInitializing) _config.SetSetting("IdleWidth", value.ToString(System.Globalization.CultureInfo.InvariantCulture)); }

        [ObservableProperty]
        private double _idleHeight;
        partial void OnIdleHeightChanged(double value) { if (!_isInitializing) _config.SetSetting("IdleHeight", value.ToString(System.Globalization.CultureInfo.InvariantCulture)); }

        [ObservableProperty]
        private double _hoverWidth;
        partial void OnHoverWidthChanged(double value) { if (!_isInitializing) _config.SetSetting("HoverWidth", value.ToString(System.Globalization.CultureInfo.InvariantCulture)); }

        [ObservableProperty]
        private double _hoverHeight;
        partial void OnHoverHeightChanged(double value) { if (!_isInitializing) _config.SetSetting("HoverHeight", value.ToString(System.Globalization.CultureInfo.InvariantCulture)); }

        [ObservableProperty]
        private string _hoverNotchColor;
        partial void OnHoverNotchColorChanged(string value) { if (!_isInitializing) _config.SetSetting("HoverNotchColor", value); }

        [ObservableProperty]
        private string _hoverShadowColor;
        partial void OnHoverShadowColorChanged(string value) { if (!_isInitializing) _config.SetSetting("HoverShadowColor", value); }

        [ObservableProperty]
        private double _filletRadius;
        partial void OnFilletRadiusChanged(double value) { if (!_isInitializing) _config.SetSetting("FilletRadius", value.ToString(System.Globalization.CultureInfo.InvariantCulture)); }

        [ObservableProperty]
        private int _animationSpeedMs;
        partial void OnAnimationSpeedMsChanged(int value) { if (!_isInitializing) _config.SetSetting("AnimationSpeedMs", value.ToString(System.Globalization.CultureInfo.InvariantCulture)); }

        public MusicWidgetViewModel MusicWidget { get; }

        public IslandViewModel(IConfigurationService config, ShelfViewModel shelfVm, MusicWidgetViewModel musicWidget)
        {
            _isInitializing = true;
            _config = config;
            MusicWidget = musicWidget;
            
            var nColor = _config.GetSetting("NotchColor");
            NotchColor = string.IsNullOrEmpty(nColor) ? "#000000" : nColor;
            
            var sColor = _config.GetSetting("ShadowColor");
            ShadowColor = string.IsNullOrEmpty(sColor) ? "#000000" : sColor;
            
            var hnColor = _config.GetSetting("HoverNotchColor");
            HoverNotchColor = string.IsNullOrEmpty(hnColor) ? "#000000" : hnColor;

            var hsColor = _config.GetSetting("HoverShadowColor");
            HoverShadowColor = string.IsNullOrEmpty(hsColor) ? "#000000" : hsColor;

            if (_config.HasSetting("TriggerText"))
                TriggerText = _config.GetSetting("TriggerText");
            else
                TriggerText = "WinNook";
            
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            if (bool.TryParse(_config.GetSetting("IsIslandEnabled"), out var isEnabled)) IsIslandEnabled = isEnabled; else IsIslandEnabled = true;
            IsMusicWidgetEnabled = _config.IsMusicWidgetEnabled;
            if (bool.TryParse(_config.GetSetting("EnableFillets"), out var eFillets)) EnableFillets = eFillets; else EnableFillets = true;
            if (double.TryParse(_config.GetSetting("ShadowOpacity"), System.Globalization.NumberStyles.Any, culture, out var sOpacity)) ShadowOpacity = sOpacity; else ShadowOpacity = 0.5;
            if (double.TryParse(_config.GetSetting("ShadowBlurRadius"), System.Globalization.NumberStyles.Any, culture, out var sBlur)) ShadowBlurRadius = sBlur; else ShadowBlurRadius = 20.0;
            if (double.TryParse(_config.GetSetting("CornerRadius"), System.Globalization.NumberStyles.Any, culture, out var cRadius)) CornerRadius = cRadius; else CornerRadius = 24.0;
            if (double.TryParse(_config.GetSetting("HoverCornerRadius"), System.Globalization.NumberStyles.Any, culture, out var hcRadius)) HoverCornerRadius = hcRadius; else HoverCornerRadius = 24.0;
            if (double.TryParse(_config.GetSetting("ExpandedCornerRadius"), System.Globalization.NumberStyles.Any, culture, out var ecRadius)) ExpandedCornerRadius = ecRadius; else ExpandedCornerRadius = 24.0;
            if (double.TryParse(_config.GetSetting("IdleWidth"), System.Globalization.NumberStyles.Any, culture, out var iWidth)) IdleWidth = iWidth; else IdleWidth = 200.0;
            if (double.TryParse(_config.GetSetting("IdleHeight"), System.Globalization.NumberStyles.Any, culture, out var iHeight)) IdleHeight = iHeight; else IdleHeight = 40.0;
            if (double.TryParse(_config.GetSetting("HoverWidth"), System.Globalization.NumberStyles.Any, culture, out var hWidth)) HoverWidth = hWidth; else HoverWidth = 220.0;
            if (double.TryParse(_config.GetSetting("HoverHeight"), System.Globalization.NumberStyles.Any, culture, out var hHeight)) HoverHeight = hHeight; else HoverHeight = 50.0;
            if (double.TryParse(_config.GetSetting("FilletRadius"), System.Globalization.NumberStyles.Any, culture, out var fRadius)) FilletRadius = fRadius; else FilletRadius = 16.0;
            if (int.TryParse(_config.GetSetting("AnimationSpeedMs"), System.Globalization.NumberStyles.Any, culture, out var asm)) AnimationSpeedMs = asm; else AnimationSpeedMs = 500;
            
            DashboardViewModel.OpenShelfAction = () => IsShelfVisible = true;
            DashboardViewModel.OpenControlCenterAction = () =>
            {
                var app = (App)System.Windows.Application.Current;
                app.ShowControlCenter();
                Collapse();
            };
            
            shelfVm.CloseShelfAction = () => IsShelfVisible = false;

            _isInitializing = false;
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
        private void RestoreDefaults()
        {
            IsIslandEnabled = true;
            EnableFillets = true;
            TriggerText = "WinNook";
            NotchColor = "#000000";
            ShadowColor = "#000000";
            HoverNotchColor = "#000000";
            HoverShadowColor = "#000000";
            ShadowOpacity = 0.5;
            ShadowBlurRadius = 20.0;
            CornerRadius = 24.0;
            HoverCornerRadius = 24.0;
            ExpandedCornerRadius = 24.0;
            FilletRadius = 16.0;
            IdleWidth = 200.0;
            IdleHeight = 40.0;
            HoverWidth = 220.0;
            HoverHeight = 50.0;
            AnimationSpeedMs = 500;
        }
    }
}
