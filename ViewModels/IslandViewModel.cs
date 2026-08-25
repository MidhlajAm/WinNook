using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Project_Nook.Models;
using Project_Nook.Services;
using Project_Nook.Widgets;

namespace Project_Nook.ViewModels
{
    public partial class IslandViewModel : ObservableObject
    {
        private readonly IConfigurationService _config;
        private readonly WidgetRegistry _widgets;
        private bool _isInitializing = false;

        [ObservableProperty]
        private IslandState _currentState = IslandState.Idle;

        /// <summary>
        /// The user's configured idle text, persisted. This must never hold a notification: it used to,
        /// and every volume change or screenshot wrote its own message into settings.json as the new
        /// permanent default.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplayText))]
        private string _triggerText = "WinNook";
        partial void OnTriggerTextChanged(string value) { if (!_isInitializing) _config.SetSetting("TriggerText", value); }

        /// <summary>Transient notification text. Never persisted, never survives the notification.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplayText))]
        private string _notificationText = string.Empty;

        /// <summary>What the idle notch actually shows: a live notification, otherwise the user's text.</summary>
        public string DisplayText =>
            CurrentState == IslandState.Triggered && !string.IsNullOrEmpty(NotificationText)
                ? NotificationText
                : TriggerText;

        [ObservableProperty]
        private Wpf.Ui.Controls.SymbolRegular _triggerIcon = Wpf.Ui.Controls.SymbolRegular.Empty;

        [ObservableProperty]
        private bool _isIslandEnabled;
        partial void OnIsIslandEnabledChanged(bool value) { if (!_isInitializing) _config.SetSetting("IsIslandEnabled", value.ToString()); }

        [ObservableProperty]
        private bool _isHidden;

        /// <summary>
        /// Which view the expanded notch is showing. Replaces the old IsShelfVisible bool now that widgets
        /// other than the Shelf need more room than a 64x64 tile.
        /// </summary>
        [ObservableProperty]
        private ExpandedPage _currentPage = ExpandedPage.Dashboard;
        partial void OnCurrentPageChanged(ExpandedPage value) => UpdateDashboardActivation();

        partial void OnCurrentStateChanged(IslandState value)
        {
            OnPropertyChanged(nameof(DisplayText));
            UpdateDashboardActivation();
        }

        public WidgetDashboardViewModel DashboardViewModel { get; }

        /// <summary>
        /// Backed by the widget registry rather than its own setting: the idle notch's media layout needs
        /// to know whether the music widget is on, and there should only ever be one answer to that.
        /// </summary>
        public bool IsMusicWidgetEnabled => _widgets.IsEnabled(WidgetIds.Music);

        /// <summary>Widgets only run while they are actually on screen.</summary>
        private void UpdateDashboardActivation()
        {
            DashboardViewModel.SetActive(CurrentState == IslandState.Expanded && CurrentPage == ExpandedPage.Dashboard);
        }

        /// <summary>
        /// How the expanded notch is dismissed. False (default): clicking anywhere inside it collapses it.
        /// True: clicks inside are left to the widgets, and the notch collapses when you click away from it.
        /// </summary>
        [ObservableProperty]
        private bool _collapseOnOutsideClick;
        partial void OnCollapseOnOutsideClickChanged(bool value) { if (!_isInitializing) _config.SetSetting("CollapseOnOutsideClick", value.ToString()); }

        /// <summary>
        /// Gap between the screen edge and the notch. 0 sits flush (the edge-attached look the fillets
        /// blend in); anything above 0 detaches it into a floating pill like Apple's Dynamic Island,
        /// and the corner radius then applies to all four corners rather than just the bottom two.
        /// </summary>
        [ObservableProperty]
        private double _topOffset;
        partial void OnTopOffsetChanged(double value) { if (!_isInitializing) _config.SetSetting("TopOffset", value.ToString(System.Globalization.CultureInfo.InvariantCulture)); }

        /// <summary>True when the notch floats clear of the screen edge.</summary>
        public bool IsFloating => TopOffset > 0.5;

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

        /// <summary>
        /// How far the shadow falls below the notch. Paired with a fixed 270 degree direction: the notch
        /// hangs off the top edge of the screen, so its shadow belongs straight underneath it, not offset
        /// diagonally like a floating card.
        /// </summary>
        [ObservableProperty]
        private double _shadowDepth;
        partial void OnShadowDepthChanged(double value) { if (!_isInitializing) _config.SetSetting("ShadowDepth", value.ToString(System.Globalization.CultureInfo.InvariantCulture)); }

        [ObservableProperty]
        private double _cornerRadius;
        partial void OnCornerRadiusChanged(double value) { if (!_isInitializing) _config.SetSetting("CornerRadius", value.ToString(System.Globalization.CultureInfo.InvariantCulture)); }

        [ObservableProperty]
        private double _hoverCornerRadius;
        partial void OnHoverCornerRadiusChanged(double value) { if (!_isInitializing) _config.SetSetting("HoverCornerRadius", value.ToString(System.Globalization.CultureInfo.InvariantCulture)); }

        [ObservableProperty]
        private double _expandedCornerRadius;
        partial void OnExpandedCornerRadiusChanged(double value) { if (!_isInitializing) { _config.SetSetting("ExpandedCornerRadius", value.ToString(System.Globalization.CultureInfo.InvariantCulture)); CurrentState = IslandState.Expanded; } }

        [ObservableProperty]
        private double _expandedWidth;
        partial void OnExpandedWidthChanged(double value) { if (!_isInitializing) { _config.SetSetting("ExpandedWidth", value.ToString(System.Globalization.CultureInfo.InvariantCulture)); CurrentState = IslandState.Expanded; } }

        [ObservableProperty]
        private double _expandedHeight;
        partial void OnExpandedHeightChanged(double value) { if (!_isInitializing) { _config.SetSetting("ExpandedHeight", value.ToString(System.Globalization.CultureInfo.InvariantCulture)); CurrentState = IslandState.Expanded; } }

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
        partial void OnFilletRadiusChanged(double value) { if (!_isInitializing) { _config.SetSetting("FilletRadius", value.ToString(System.Globalization.CultureInfo.InvariantCulture)); } }

        [ObservableProperty]
        private double _expandedFilletRadius;
        partial void OnExpandedFilletRadiusChanged(double value) { if (!_isInitializing) { _config.SetSetting("ExpandedFilletRadius", value.ToString(System.Globalization.CultureInfo.InvariantCulture)); CurrentState = IslandState.Expanded; } }

        [ObservableProperty]
        private int _animationSpeedMs;
        partial void OnAnimationSpeedMsChanged(int value) { if (!_isInitializing) _config.SetSetting("AnimationSpeedMs", value.ToString(System.Globalization.CultureInfo.InvariantCulture)); }

        public MusicWidgetViewModel MusicWidget { get; }

        public IslandViewModel(
            IConfigurationService config,
            ShelfViewModel shelfVm,
            MusicWidgetViewModel musicWidget,
            WidgetRegistry widgets,
            WidgetDashboardViewModel dashboardViewModel,
            NotesWidgetViewModel notesVm,
            ClipboardWidgetViewModel clipboardVm,
            QuickLaunchWidgetViewModel quickLaunchVm)
        {
            _isInitializing = true;
            _config = config;
            _widgets = widgets;
            MusicWidget = musicWidget;
            DashboardViewModel = dashboardViewModel;

            // The idle media layout keys off the music widget being enabled, so it has to hear about toggles.
            _widgets.LayoutChanged += (s, e) => OnPropertyChanged(nameof(IsMusicWidgetEnabled));
            
            var nColor = _config.GetSetting("NotchColor");
            NotchColor = string.IsNullOrEmpty(nColor) ? "#000000" : nColor;
            
            var sColor = _config.GetSetting("ShadowColor");
            ShadowColor = string.IsNullOrEmpty(sColor) ? "#000000" : sColor;
            
            var hnColor = _config.GetSetting("HoverNotchColor");
            HoverNotchColor = string.IsNullOrEmpty(hnColor) ? "#000000" : hnColor;

            var hsColor = _config.GetSetting("HoverShadowColor");
            HoverShadowColor = string.IsNullOrEmpty(hsColor) ? "#000000" : hsColor;

            // HasSetting, not GetSetting: a missing key and a deliberately emptied one both read back as
            // "", and treating them the same meant clearing the field silently reverted to "WinNook".
            var storedTriggerText = _config.HasSetting("TriggerText") ? _config.GetSetting("TriggerText") : null;
            TriggerText = RepairTriggerText(storedTriggerText);

            // The repair runs while initializing, so the write-through is suppressed. Persist it here or
            // the leaked text stays in settings.json and has to be repaired again on every launch.
            if (TriggerText != storedTriggerText) _config.SetSetting("TriggerText", TriggerText);
            
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            if (bool.TryParse(_config.GetSetting("IsIslandEnabled"), out var isEnabled)) IsIslandEnabled = isEnabled; else IsIslandEnabled = true;
            if (bool.TryParse(_config.GetSetting("EnableFillets"), out var eFillets)) EnableFillets = eFillets; else EnableFillets = true;
            if (double.TryParse(_config.GetSetting("TopOffset"), System.Globalization.NumberStyles.Any, culture, out var tOffset)) TopOffset = tOffset; else TopOffset = 0.0;
            if (bool.TryParse(_config.GetSetting("CollapseOnOutsideClick"), out var cooc)) CollapseOnOutsideClick = cooc; else CollapseOnOutsideClick = false;
            if (double.TryParse(_config.GetSetting("ShadowOpacity"), System.Globalization.NumberStyles.Any, culture, out var sOpacity)) ShadowOpacity = sOpacity; else ShadowOpacity = 0.5;
            if (double.TryParse(_config.GetSetting("ShadowBlurRadius"), System.Globalization.NumberStyles.Any, culture, out var sBlur)) ShadowBlurRadius = sBlur; else ShadowBlurRadius = 20.0;
            if (double.TryParse(_config.GetSetting("ShadowDepth"), System.Globalization.NumberStyles.Any, culture, out var sDepth)) ShadowDepth = sDepth; else ShadowDepth = 6.0;
            if (double.TryParse(_config.GetSetting("CornerRadius"), System.Globalization.NumberStyles.Any, culture, out var cRadius)) CornerRadius = cRadius; else CornerRadius = 17.0;
            if (double.TryParse(_config.GetSetting("HoverCornerRadius"), System.Globalization.NumberStyles.Any, culture, out var hcRadius)) HoverCornerRadius = hcRadius; else HoverCornerRadius = 17.0;
            if (double.TryParse(_config.GetSetting("ExpandedCornerRadius"), System.Globalization.NumberStyles.Any, culture, out var ecRadius)) ExpandedCornerRadius = ecRadius; else ExpandedCornerRadius = 30.0;
            if (double.TryParse(_config.GetSetting("ExpandedWidth"), System.Globalization.NumberStyles.Any, culture, out var ewRadius)) ExpandedWidth = ewRadius; else ExpandedWidth = 750.0;
            if (double.TryParse(_config.GetSetting("ExpandedHeight"), System.Globalization.NumberStyles.Any, culture, out var ehRadius)) ExpandedHeight = ehRadius; else ExpandedHeight = 117.0;
            if (double.TryParse(_config.GetSetting("IdleWidth"), System.Globalization.NumberStyles.Any, culture, out var iWidth)) IdleWidth = iWidth; else IdleWidth = 202.0;
            if (double.TryParse(_config.GetSetting("IdleHeight"), System.Globalization.NumberStyles.Any, culture, out var iHeight)) IdleHeight = iHeight; else IdleHeight = 40.0;
            if (double.TryParse(_config.GetSetting("HoverWidth"), System.Globalization.NumberStyles.Any, culture, out var hWidth)) HoverWidth = hWidth; else HoverWidth = 220.0;
            if (double.TryParse(_config.GetSetting("HoverHeight"), System.Globalization.NumberStyles.Any, culture, out var hHeight)) HoverHeight = hHeight; else HoverHeight = 42.0;
            if (double.TryParse(_config.GetSetting("FilletRadius"), System.Globalization.NumberStyles.Any, culture, out var fRadius)) FilletRadius = fRadius; else FilletRadius = 13.0;
            if (double.TryParse(_config.GetSetting("ExpandedFilletRadius"), System.Globalization.NumberStyles.Any, culture, out var efRadius)) ExpandedFilletRadius = efRadius; else ExpandedFilletRadius = 20.0;
            if (int.TryParse(_config.GetSetting("AnimationSpeedMs"), System.Globalization.NumberStyles.Any, culture, out var asm)) AnimationSpeedMs = asm; else AnimationSpeedMs = 500;
            
            DashboardViewModel.OpenShelfAction = () => CurrentPage = ExpandedPage.Shelf;
            DashboardViewModel.OpenControlCenterAction = () =>
            {
                var app = (App)System.Windows.Application.Current;
                app.ShowControlCenter();
                Collapse();
            };
            
            shelfVm.CloseShelfAction = () => CurrentPage = ExpandedPage.Dashboard;

            notesVm.OpenPageAction = () => CurrentPage = ExpandedPage.Notes;
            notesVm.CloseAction = () => CurrentPage = ExpandedPage.Dashboard;

            clipboardVm.OpenPageAction = () => CurrentPage = ExpandedPage.Clipboard;
            clipboardVm.CloseAction = () => CurrentPage = ExpandedPage.Dashboard;

            quickLaunchVm.OpenSettingsAction = () =>
            {
                ((App)System.Windows.Application.Current).ShowControlCenter(App.TabApps);
                Collapse();
            };

            _isInitializing = false;
        }

        /// <summary>
        /// Earlier builds leaked notification text into the saved default, so an upgrading user can have
        /// "Volume 40%" or "Screenshot saved" as their permanent notch label. Recognise those and restore
        /// the default; anything the user actually chose is left alone.
        /// </summary>
        private static string RepairTriggerText(string stored)
        {
            // Never configured: fall back to the product name.
            if (stored == null) return "WinNook";

            // Configured as blank: that is a real choice - a bare notch with no label - so keep it.
            if (stored.Trim().Length == 0) return string.Empty;

            string[] leakedPrefixes =
            {
                "Volume ", "Charging:", "Battery ", "Screenshot ", "Timer "
            };

            foreach (var prefix in leakedPrefixes)
            {
                if (stored.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase)) return "WinNook";
            }

            return stored;
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
            CurrentPage = ExpandedPage.Dashboard;
        }

        /// <summary>
        /// The default look: a notch fused to the top edge of the screen, with inverted corner fillets
        /// blending it into the bezel. Only the bottom corners are rounded, because the top edge is the
        /// screen edge.
        /// </summary>
        [RelayCommand]
        private void ApplyNookStyle()
        {
            TopOffset = 0.0;
            EnableFillets = true;

            CornerRadius = 17.0;
            HoverCornerRadius = 17.0;
            ExpandedCornerRadius = 30.0;
            FilletRadius = 13.0;
            ExpandedFilletRadius = 20.0;

            IdleWidth = 202.0;
            IdleHeight = 40.0;
            HoverWidth = 220.0;
            HoverHeight = 42.0;

            ShadowOpacity = 0.5;
            ShadowBlurRadius = 20.0;
            ShadowDepth = 6.0;
        }

        /// <summary>
        /// Apple's Dynamic Island: a pill floating clear of the screen edge, fully rounded (radius is
        /// exactly half the height), with no fillets since it has no edge to blend into.
        /// </summary>
        [RelayCommand]
        private void ApplyDynamicIslandStyle()
        {
            TopOffset = 6.0;
            EnableFillets = false;

            CornerRadius = 19.0;
            HoverCornerRadius = 20.0;
            ExpandedCornerRadius = 32.0;

            IdleWidth = 190.0;
            IdleHeight = 38.0;
            HoverWidth = 200.0;
            HoverHeight = 40.0;

            ShadowOpacity = 0.35;
            ShadowBlurRadius = 24.0;
            ShadowDepth = 4.0;
        }

        [RelayCommand]
        private void RestoreDefaults()
        {
            IsIslandEnabled = true;
            CollapseOnOutsideClick = false;
            TriggerText = "WinNook";

            NotchColor = "#000000";
            ShadowColor = "#000000";
            HoverNotchColor = "#000000";
            HoverShadowColor = "#000000";

            ExpandedWidth = 750.0;
            ExpandedHeight = 117.0;
            AnimationSpeedMs = 500;

            ApplyNookStyle();
        }
    }
}
