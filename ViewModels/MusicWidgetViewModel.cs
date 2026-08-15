using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Project_Nook.Services.SystemEvents;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Project_Nook.ViewModels
{
    public partial class MusicWidgetViewModel : ObservableObject
    {
        private readonly MediaEventService _mediaEventService;

        [ObservableProperty]
        private string _title = "Not Playing";

        [ObservableProperty]
        private string _artist = "Unknown Artist";

        [ObservableProperty]
        private bool _isPlaying;

        [ObservableProperty]
        private BitmapImage _albumArt;

        public MusicWidgetViewModel(MediaEventService mediaEventService)
        {
            _mediaEventService = mediaEventService;
            _mediaEventService.MediaChanged += MediaEventService_MediaChanged;
        }

        private void MediaEventService_MediaChanged(object sender, MediaPlaybackInfo e)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                Title = e.Title;
                Artist = e.Artist;
                IsPlaying = e.IsPlaying;

                if (e.AlbumArt != null && e.AlbumArt.Length > 0)
                {
                    try
                    {
                        var image = new BitmapImage();
                        using (var mem = new MemoryStream(e.AlbumArt))
                        {
                            mem.Position = 0;
                            image.BeginInit();
                            image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                            image.CacheOption = BitmapCacheOption.OnLoad;
                            image.UriSource = null;
                            image.StreamSource = mem;
                            image.EndInit();
                        }
                        image.Freeze();
                        AlbumArt = image;
                    }
                    catch
                    {
                        AlbumArt = null;
                    }
                }
                else
                {
                    AlbumArt = null;
                }
            });
        }

        [RelayCommand]
        private async Task PlayPauseAsync()
        {
            await _mediaEventService.PlayPauseAsync();
        }

        [RelayCommand]
        private async Task NextAsync()
        {
            await _mediaEventService.NextAsync();
        }

        [RelayCommand]
        private async Task PreviousAsync()
        {
            await _mediaEventService.PreviousAsync();
        }
    }
}
