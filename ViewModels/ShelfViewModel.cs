using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Project_Nook.Models;
using Project_Nook.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Project_Nook.ViewModels
{
    public partial class ShelfViewModel : ObservableObject
    {
        private readonly IShelfService _shelfService;
        private readonly IStorageService _storageService;

        public ObservableCollection<ShelfItem> Items { get; } = new();

        public System.Action CloseShelfAction { get; set; }

        [RelayCommand]
        private void CloseShelf()
        {
            CloseShelfAction?.Invoke();
        }

        public ShelfViewModel(IShelfService shelfService, IStorageService storageService)
        {
            _shelfService = shelfService;
            _storageService = storageService;
            
            LoadItemsAsync().ConfigureAwait(false);
        }

        public async Task LoadItemsAsync()
        {
            var items = await _shelfService.GetAllItemsAsync();
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                Items.Clear();
                foreach (var item in items)
                {
                    Items.Add(item);
                }
            });
        }

        [RelayCommand]
        private void OpenItem(ShelfItem item)
        {
            if (item != null && !string.IsNullOrEmpty(item.StoredPath))
            {
                _storageService.OpenFile(item.StoredPath);
            }
        }

        [RelayCommand]
        private void RevealItem(ShelfItem item)
        {
            if (item != null && !string.IsNullOrEmpty(item.StoredPath))
            {
                _storageService.RevealInExplorer(item.StoredPath);
            }
        }

        [RelayCommand]
        private async Task DeleteItem(ShelfItem item)
        {
            if (item != null)
            {
                if (!string.IsNullOrEmpty(item.StoredPath))
                {
                    _storageService.DeleteFromStorage(item.StoredPath);
                }
                await _shelfService.DeleteItemAsync(item);
                Items.Remove(item);
            }
        }

        public async Task AddDroppedFileAsync(string filePath)
        {
            if (!System.IO.File.Exists(filePath)) return;

            var fileInfo = new System.IO.FileInfo(filePath);
            
            // Prevent duplicates: Check if we already have a file with the same name and size
            foreach (var existingItem in Items)
            {
                if (existingItem.OriginalName == fileInfo.Name && !string.IsNullOrEmpty(existingItem.StoredPath) && System.IO.File.Exists(existingItem.StoredPath))
                {
                    var storedInfo = new System.IO.FileInfo(existingItem.StoredPath);
                    if (storedInfo.Length == fileInfo.Length)
                    {
                        return; // It's the same file, ignore the drop
                    }
                }
            }

            var storedPath = await _storageService.CopyFileToStorageAsync(filePath);
            if (storedPath != null)
            {
                var item = new ShelfItem
                {
                    Type = DetermineFileType(fileInfo.Name),
                    OriginalName = fileInfo.Name,
                    StoredPath = storedPath,
                    CreatedAt = System.DateTime.Now
                };
                await _shelfService.SaveItemAsync(item);
                System.Windows.Application.Current.Dispatcher.Invoke(() => Items.Insert(0, item));
            }
        }

        public async Task AddDroppedTextAsync(string text)
        {
            // Prevent duplicates for exact same text
            foreach (var existingItem in Items)
            {
                if (existingItem.Type == "Text" && existingItem.Content == text)
                {
                    return;
                }
            }

            var storedPath = await _storageService.SaveTextToStorageAsync(text, "TextSnippet");
            var item = new ShelfItem
            {
                Type = "Text",
                OriginalName = "Text Snippet",
                Content = text,
                StoredPath = storedPath,
                CreatedAt = System.DateTime.Now
            };
            await _shelfService.SaveItemAsync(item);
            System.Windows.Application.Current.Dispatcher.Invoke(() => Items.Insert(0, item));
        }

        private string DetermineFileType(string fileName)
        {
            string ext = System.IO.Path.GetExtension(fileName).ToLowerInvariant();
            return ext switch
            {
                ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" or ".webp" or ".ico" => "Image",
                ".mp4" or ".mkv" or ".avi" or ".mov" or ".webm" => "Video",
                ".mp3" or ".wav" or ".flac" or ".ogg" or ".m4a" => "Audio",
                ".txt" or ".md" or ".rtf" or ".json" or ".xml" or ".csv" or ".log" => "Text",
                ".zip" or ".rar" or ".7z" or ".tar" or ".gz" => "Archive",
                ".cs" or ".js" or ".html" or ".css" or ".py" or ".cpp" or ".c" => "Code",
                _ => "File"
            };
        }
    }
}
