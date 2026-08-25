using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Project_Nook.Models;
using Project_Nook.Services;
using Project_Nook.Widgets;
using System;
using System.Linq;
using System.Windows.Threading;

namespace Project_Nook.ViewModels
{
    /// <summary>
    /// A single always-there scratchpad. Backs both the tile (which shows a preview) and the notes page
    /// (which is the editor), so what you type is what the tile shows without any syncing between two
    /// view models.
    /// </summary>
    public partial class NotesWidgetViewModel : ObservableObject, IWidgetLifecycle
    {
        /// <summary>Typing pause before a write. Long enough not to hit SQLite on every keystroke.</summary>
        private static readonly TimeSpan SaveDelay = TimeSpan.FromMilliseconds(600);

        private readonly IWidgetDataService _data;
        private readonly DispatcherTimer _saveTimer;

        private NoteItem _note;
        private bool _isLoading;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Preview))]
        [NotifyPropertyChangedFor(nameof(IsEmpty))]
        private string _text = string.Empty;

        /// <summary>Opens the notes page. Wired by IslandViewModel.</summary>
        public Action OpenPageAction { get; set; }

        /// <summary>Returns to the dashboard. Wired by IslandViewModel.</summary>
        public Action CloseAction { get; set; }

        public bool IsEmpty => string.IsNullOrWhiteSpace(Text);

        /// <summary>First non-blank line, for the tile.</summary>
        public string Preview
        {
            get
            {
                if (IsEmpty) return "Empty";

                var line = Text.Split('\n').FirstOrDefault(l => !string.IsNullOrWhiteSpace(l))?.Trim();
                if (string.IsNullOrEmpty(line)) return "Empty";

                return line.Length <= 40 ? line : line.Substring(0, 40) + "…";
            }
        }

        public NotesWidgetViewModel(IWidgetDataService data)
        {
            _data = data;

            _saveTimer = new DispatcherTimer { Interval = SaveDelay };
            _saveTimer.Tick += async (s, e) =>
            {
                _saveTimer.Stop();
                await SaveAsync();
            };

            _ = LoadAsync();
        }

        private async System.Threading.Tasks.Task LoadAsync()
        {
            var loaded = await _data.GetScratchpadAsync();

            // Typing can beat the database. If anything was entered while this was in flight, that is the
            // newer text and it wins; the stored copy would otherwise overwrite it and the keystrokes
            // would vanish with no error.
            bool typedFirst = !string.IsNullOrEmpty(Text);

            _note = loaded;

            if (typedFirst)
            {
                await SaveAsync();
                return;
            }

            _isLoading = true;
            Text = loaded.Content ?? string.Empty;
            _isLoading = false;
        }

        partial void OnTextChanged(string value)
        {
            if (_isLoading) return;

            // Debounced: restart the countdown on every keystroke, write once the typing stops.
            _saveTimer.Stop();
            _saveTimer.Start();
        }

        private async System.Threading.Tasks.Task SaveAsync()
        {
            if (_note == null) return;

            _note.Content = Text;
            await _data.SaveScratchpadAsync(_note);
        }

        /// <summary>Flush on close so nothing is lost between the last keystroke and the debounce firing.</summary>
        public void OnActivated() { }

        public void OnDeactivated()
        {
            if (!_saveTimer.IsEnabled) return;

            _saveTimer.Stop();
            _ = SaveAsync();
        }

        [RelayCommand]
        private void OpenPage() => OpenPageAction?.Invoke();

        [RelayCommand]
        private void Close()
        {
            _saveTimer.Stop();
            _ = SaveAsync();
            CloseAction?.Invoke();
        }

        [RelayCommand]
        private void Clear() => Text = string.Empty;
    }
}
