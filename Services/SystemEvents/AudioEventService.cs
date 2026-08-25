using NAudio.CoreAudioApi;
using System;

namespace Project_Nook.Services.SystemEvents
{
    /// <summary>Master volume and mute state of the default render device, as one snapshot.</summary>
    public class AudioState
    {
        public float Volume { get; set; }
        public bool IsMuted { get; set; }
    }

    public class AudioEventService : IDisposable
    {
        private readonly MMDevice _device;
        private readonly MMDeviceEnumerator _enumerator;

        /// <summary>Raised on every volume change. Consumed by the trigger engine.</summary>
        public event EventHandler<float> VolumeChanged;

        /// <summary>
        /// Raised with volume and mute together. Widgets use this one so a mute from a hardware key or the
        /// Windows mixer shows up in the tile without any polling.
        /// </summary>
        public event EventHandler<AudioState> AudioChanged;

        public AudioEventService()
        {
            try
            {
                _enumerator = new MMDeviceEnumerator();
                _device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                _device.AudioEndpointVolume.OnVolumeNotification += AudioEndpointVolume_OnVolumeNotification;
            }
            catch
            {
                // Audio device not available
            }
        }

        private void AudioEndpointVolume_OnVolumeNotification(AudioVolumeNotificationData data)
        {
            VolumeChanged?.Invoke(this, data.MasterVolume);
            AudioChanged?.Invoke(this, new AudioState { Volume = data.MasterVolume, IsMuted = data.Muted });
        }

        public float GetCurrentVolume()
        {
            return _device?.AudioEndpointVolume?.MasterVolumeLevelScalar ?? 0f;
        }

        public bool IsMuted
        {
            get
            {
                try { return _device?.AudioEndpointVolume?.Mute ?? false; }
                catch { return false; }
            }
        }

        /// <summary>Reads the device directly. Used to seed a widget that just came on screen.</summary>
        public AudioState GetState()
        {
            return new AudioState { Volume = GetCurrentVolume(), IsMuted = IsMuted };
        }

        /// <summary>Sets master volume. Scalar is clamped to 0..1.</summary>
        public void SetVolume(float scalar)
        {
            if (_device?.AudioEndpointVolume == null) return;

            if (scalar < 0f) scalar = 0f;
            if (scalar > 1f) scalar = 1f;

            try { _device.AudioEndpointVolume.MasterVolumeLevelScalar = scalar; }
            catch { }
        }

        /// <summary>Nudges master volume by a delta, e.g. a mouse wheel notch.</summary>
        public void AdjustVolume(float delta) => SetVolume(GetCurrentVolume() + delta);

        public void ToggleMute()
        {
            if (_device?.AudioEndpointVolume == null) return;

            try { _device.AudioEndpointVolume.Mute = !_device.AudioEndpointVolume.Mute; }
            catch { }
        }

        public void Dispose()
        {
            if (_device != null)
            {
                _device.AudioEndpointVolume.OnVolumeNotification -= AudioEndpointVolume_OnVolumeNotification;
                _device.Dispose();
            }
            _enumerator?.Dispose();
        }
    }
}
