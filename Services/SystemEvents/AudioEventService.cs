using NAudio.CoreAudioApi;
using System;

namespace Project_Nook.Services.SystemEvents
{
    public class AudioEventService : IDisposable
    {
        private readonly MMDevice _device;
        private readonly MMDeviceEnumerator _enumerator;
        
        public event EventHandler<float> VolumeChanged;

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
        }

        public float GetCurrentVolume()
        {
            return _device?.AudioEndpointVolume?.MasterVolumeLevelScalar ?? 0f;
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
