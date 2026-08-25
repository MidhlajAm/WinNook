using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Project_Nook.Models;
using Project_Nook.Services;
using Project_Nook.Services.SystemEvents;
using Project_Nook.Widgets;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace Project_Nook.ViewModels
{
    /// <summary>
    /// Clipboard history. Backs both the tile (count) and the clipboard page (the list).
    ///
    /// Capture runs for the whole session, not just while the dashboard is open: a history that only
    /// recorded what you copied with the notch expanded would never have anything in it.
    /// </summary>
    public partial class ClipboardWidgetViewModel : ObservableObject
    {
        private const int MaxEntries = 25;

        private readonly IWidgetDataService _data;
        private readonly ClipboardEventService _clipboard;

        public ObservableCollection<ClipboardEntry> Entries { get; } = new();

        [ObservableProperty]
        private int _count;

        /// <summary>Opens the clipboard page. Wired by IslandViewModel.</summary>
        public Action OpenPageAction { get; set; }

        /// <summary>Returns to the dashboard. Wired by IslandViewModel.</summary>
        public Action CloseAction { get; set; }

        public ClipboardWidgetViewModel(IWidgetDataService data, ClipboardEventService clipboard)
        {
            _data = data;
            _clipboard = clipboard;

            _clipboard.TextCaptured += Clipboard_TextCaptured;
            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            var stored = await _data.GetClipboardHistoryAsync();

            Application.Current?.Dispatcher.Invoke(() =>
            {
                Entries.Clear();
                foreach (var entry in stored) Entries.Add(entry);
                Count = Entries.Count;
            });
        }

        private async void Clipboard_TextCaptured(object sender, string text)
        {
            var entry = await _data.AddClipboardEntryAsync(text, MaxEntries);
            if (entry == null) return;   // duplicate of what is already on top

            Application.Current?.Dispatcher.Invoke(() =>
            {
                // Re-copying something from further down moves it to the top rather than duplicating it,
                // so drop any older copy from the list too.
                for (int i = Entries.Count - 1; i >= 0; i--)
                {
                    if (Entries[i].Content == text) Entries.RemoveAt(i);
                }

                Entries.Insert(0, entry);

                while (Entries.Count > MaxEntries) Entries.RemoveAt(Entries.Count - 1);
                Count = Entries.Count;
            });
        }

        [RelayCommand]
        private void CopyBack(ClipboardEntry entry)
        {
            if (entry?.Content == null) return;

            try
            {
                // Tell the watcher first, or this write comes straight back as a new capture.
                _clipboard.SuppressNext(entry.Content);
                Clipboard.SetText(entry.Content);
            }
            catch { }
        }

        [RelayCommand]
        private async Task DeleteEntry(ClipboardEntry entry)
        {
            if (entry == null) return;

            await _data.DeleteClipboardEntryAsync(entry);
            Entries.Remove(entry);
            Count = Entries.Count;
        }

        [RelayCommand]
        private async Task ClearAll()
        {
            await _data.ClearClipboardHistoryAsync();
            Entries.Clear();
            Count = 0;
        }

        [RelayCommand]
        private void OpenPage() => OpenPageAction?.Invoke();

        [RelayCommand]
        private void Close() => CloseAction?.Invoke();
    }
}
