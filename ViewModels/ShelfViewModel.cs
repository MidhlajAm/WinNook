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
            var storedPath = await _storageService.CopyFileToStorageAsync(filePath);
            if (storedPath != null)
            {
                var item = new ShelfItem
                {
                    Type = "File",
                    OriginalName = System.IO.Path.GetFileName(filePath),
                    StoredPath = storedPath,
                    CreatedAt = System.DateTime.Now
                };
                await _shelfService.SaveItemAsync(item);
                System.Windows.Application.Current.Dispatcher.Invoke(() => Items.Insert(0, item));
            }
        }

        public async Task AddDroppedTextAsync(string text)
        {
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
    }
}
