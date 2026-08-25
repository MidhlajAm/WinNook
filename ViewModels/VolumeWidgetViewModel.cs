using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Project_Nook.Services.SystemEvents;
using Project_Nook.Widgets;
using System.Windows;
using Wpf.Ui.Controls;

namespace Project_Nook.ViewModels
{
    public partial class VolumeWidgetViewModel : ObservableObject, IWidgetLifecycle
    {
        /// <summary>One mouse wheel notch. Matches the granularity of the Windows volume keys.</summary>
        private const float WheelStep = 0.02f;

        private readonly AudioEventService _audio;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Icon))]
        private int _volumePercent;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Icon))]
        private bool _isMuted;

        public SymbolRegular Icon
        {
            get
            {
                if (IsMuted || VolumePercent == 0) return SymbolRegular.SpeakerMute24;
                if (VolumePercent < 34) return SymbolRegular.Speaker024;
                if (VolumePercent < 67) return SymbolRegular.Speaker124;
                return SymbolRegular.Speaker224;
            }
        }

        public VolumeWidgetViewModel(AudioEventService audio)
        {
            _audio = audio;

            // Event-driven, no polling: subscribing for the app's lifetime costs nothing and keeps the
            // tile correct even when it was changed while the dashboard was closed.
            _audio.AudioChanged += Audio_AudioChanged;
            Apply(_audio.GetState());
        }

        private void Audio_AudioChanged(object sender, AudioState state)
        {
            // WASAPI raises this on its own thread.
            Application.Current?.Dispatcher.Invoke(() => Apply(state));
        }

        private void Apply(AudioState state)
        {
            VolumePercent = (int)System.Math.Round(state.Volume * 100);
            IsMuted = state.IsMuted;
        }

        /// <summary>Re-read on activation in case the device changed while the dashboard was closed.</summary>
        public void OnActivated() => Apply(_audio.GetState());

        public void OnDeactivated() { }

        [RelayCommand]
        private void ToggleMute() => _audio.ToggleMute();

        /// <summary>Called from the tile's wheel handler. Positive scrolls up.</summary>
        public void Nudge(int direction) => _audio.AdjustVolume(direction * WheelStep);
    }
}
