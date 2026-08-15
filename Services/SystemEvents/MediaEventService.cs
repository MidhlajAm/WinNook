using System;
using System.Threading.Tasks;
using Windows.Media.Control;

namespace Project_Nook.Services.SystemEvents
{
    public class MediaEventService : IDisposable
    {
        private GlobalSystemMediaTransportControlsSessionManager _sessionManager;
        private string _lastTitle = string.Empty;
        private bool? _lastIsPlaying = null;
        
        public event EventHandler<MediaPlaybackInfo> MediaChanged;

        public MediaEventService()
        {
            InitializeAsync().ConfigureAwait(false);
        }

        private async Task InitializeAsync()
        {
            try
            {
                _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
                if (_sessionManager != null)
                {
                    _sessionManager.CurrentSessionChanged += SessionManager_CurrentSessionChanged;
                    AttachToCurrentSession();
                }
            }
            catch { }
        }

        private void SessionManager_CurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
        {
            AttachToCurrentSession();
        }

        private void AttachToCurrentSession()
        {
            var session = _sessionManager?.GetCurrentSession();
            if (session != null)
            {
                session.MediaPropertiesChanged -= Session_MediaPropertiesChanged;
                session.PlaybackInfoChanged -= Session_PlaybackInfoChanged;
                
                session.MediaPropertiesChanged += Session_MediaPropertiesChanged;
                session.PlaybackInfoChanged += Session_PlaybackInfoChanged;
                
                UpdateMediaInfo(session);
            }
        }

        private void Session_PlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
        {
            UpdateMediaInfo(sender);
        }

        private void Session_MediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
        {
            UpdateMediaInfo(sender);
        }

        private async void UpdateMediaInfo(GlobalSystemMediaTransportControlsSession session)
        {
            try
            {
                var appId = session.SourceAppUserModelId?.ToLower() ?? "";
                
                // Ignore code editors and IDEs that mistakenly broadcast media sessions via Electron/Chromium
                if (appId.Contains("antigravity") || appId.Contains("code.exe") || appId.Contains("visual studio") || appId.Contains("devenv"))
                {
                    return;
                }

                var properties = await session.TryGetMediaPropertiesAsync();
                var playback = session.GetPlaybackInfo();

                string title = properties?.Title ?? "Unknown";
                string artist = properties?.Artist ?? "Unknown";
                bool isPlaying = playback?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
                
                byte[] albumArt = null;
                if (properties?.Thumbnail != null)
                {
                    try
                    {
                        using var stream = await properties.Thumbnail.OpenReadAsync();
                        using var dataReader = new Windows.Storage.Streams.DataReader(stream);
                        await dataReader.LoadAsync((uint)stream.Size);
                        albumArt = new byte[stream.Size];
                        dataReader.ReadBytes(albumArt);
                    }
                    catch { }
                }

                _lastTitle = title;
                _lastIsPlaying = isPlaying;

                var info = new MediaPlaybackInfo
                {
                    Title = title,
                    Artist = artist,
                    IsPlaying = isPlaying,
                    AlbumArt = albumArt
                };

                MediaChanged?.Invoke(this, info);
            }
            catch { }
        }

        public async Task PlayPauseAsync()
        {
            var session = _sessionManager?.GetCurrentSession();
            if (session != null)
            {
                var playback = session.GetPlaybackInfo();
                if (playback?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
                {
                    await session.TryPauseAsync();
                }
                else
                {
                    await session.TryPlayAsync();
                }
            }
        }

        public async Task NextAsync()
        {
            var session = _sessionManager?.GetCurrentSession();
            if (session != null)
            {
                await session.TrySkipNextAsync();
            }
        }

        public async Task PreviousAsync()
        {
            var session = _sessionManager?.GetCurrentSession();
            if (session != null)
            {
                await session.TrySkipPreviousAsync();
            }
        }

        public void Dispose()
        {
            if (_sessionManager != null)
            {
                _sessionManager.CurrentSessionChanged -= SessionManager_CurrentSessionChanged;
            }
        }
    }

    public class MediaPlaybackInfo
    {
        public string Title { get; set; }
        public string Artist { get; set; }
        public bool IsPlaying { get; set; }
        public byte[] AlbumArt { get; set; }
    }
}
