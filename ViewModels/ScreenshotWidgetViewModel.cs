using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Project_Nook.Models;
using Project_Nook.Services;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Project_Nook.ViewModels
{
    public partial class ScreenshotWidgetViewModel : ObservableObject
    {
        /// <summary>
        /// Time to let the notch actually disappear before the shutter fires. Hiding is a WPF layout and
        /// render pass, not an instant, and a screenshot with the notch in it defeats the point.
        /// </summary>
        private static readonly TimeSpan HideSettleDelay = TimeSpan.FromMilliseconds(180);

        private readonly ScreenCaptureService _capture;
        private readonly ShelfViewModel _shelf;
        private readonly Func<IslandViewModel> _island;
        private readonly Func<TriggerEngine> _triggerEngine;
        private readonly IConfigurationService _config;

        [ObservableProperty]
        private bool _isCapturing;

        private const string SaveToShelfKey = "ScreenshotSaveToShelf";

        /// <summary>
        /// False by default: captures go to Pictures\Screenshots, which is where Windows puts them and
        /// where people look. True routes them into the Shelf instead.
        /// </summary>
        public bool SaveToShelf
        {
            get => bool.TryParse(_config.GetSetting(SaveToShelfKey), out var value) && value;
            set
            {
                if (SaveToShelf == value) return;
                _config.SetSetting(SaveToShelfKey, value.ToString());
                OnPropertyChanged();
            }
        }

        public ScreenshotWidgetViewModel(
            ScreenCaptureService capture,
            ShelfViewModel shelf,
            Func<IslandViewModel> island,
            Func<TriggerEngine> triggerEngine,
            IConfigurationService config)
        {
            _capture = capture;
            _shelf = shelf;
            _island = island;
            _triggerEngine = triggerEngine;
            _config = config;
        }

        [RelayCommand]
        private async Task Capture()
        {
            if (IsCapturing) return;

            var island = _island();
            if (island == null) return;

            IsCapturing = true;
            string tempPath = null;

            try
            {
                // Get out of the way: collapse, then hide entirely.
                island.CurrentPage = ExpandedPage.Dashboard;
                island.CurrentState = IslandState.Idle;
                island.IsHidden = true;

                // Let the hide reach the screen before grabbing pixels.
                await Dispatcher.Yield(DispatcherPriority.Render);
                await Task.Delay(HideSettleDelay);

                tempPath = await Task.Run(() => _capture.CaptureAllScreens());
            }
            catch
            {
                tempPath = null;
            }
            finally
            {
                island.IsHidden = false;
            }

            if (tempPath == null)
            {
                IsCapturing = false;
                return;
            }

            string message;

            if (SaveToShelf)
            {
                // Same landing place as a dropped file: the Shelf copies it into managed storage.
                await _shelf.AddDroppedFileAsync(tempPath);
                try { File.Delete(tempPath); } catch { }

                message = "Screenshot saved to Shelf";
            }
            else
            {
                // Default: where people actually expect to find a screenshot.
                try
                {
                    ScreenCaptureService.MoveInto(tempPath, ScreenCaptureService.GetWindowsScreenshotsFolder());
                    message = "Screenshot saved to Pictures";
                }
                catch
                {
                    message = "Screenshot failed to save";
                }
            }

            // Announce through the trigger engine rather than flashing Success: this way the notch springs
            // open with a camera icon and a label, then auto-collapses on the engine's own timer.
            _triggerEngine()?.Trigger(4, message, Wpf.Ui.Controls.SymbolRegular.Camera24);
            IsCapturing = false;
        }
    }
}
