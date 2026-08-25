using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Project_Nook.Models;
using Project_Nook.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Project_Nook.ViewModels
{
    /// <summary>A selectable Shelf storage budget.</summary>
    public class ShelfLimitOption
    {
        public string Label { get; init; }
        public int Megabytes { get; init; }
        public override string ToString() => Label;
    }

    public partial class ShelfViewModel : ObservableObject
    {
        private const string LimitSettingKey = "ShelfSizeLimitMb";

        /// <summary>
        /// 2 GB. Big enough to be invisible for normal use — documents, images, the odd video — and small
        /// enough to stay polite on a modest SSD. The Shelf is a scratch area, not an archive.
        /// </summary>
        private const int DefaultLimitMb = 2048;

        private readonly IShelfService _shelfService;
        private readonly IStorageService _storageService;
        private readonly IConfigurationService _config;
        private readonly Func<TriggerEngine> _triggerEngine;

        public ObservableCollection<ShelfItem> Items { get; } = new();

        public System.Action CloseShelfAction { get; set; }

        [RelayCommand]
        private void CloseShelf()
        {
            CloseShelfAction?.Invoke();
        }

        public ShelfViewModel(
            IShelfService shelfService,
            IStorageService storageService,
            IConfigurationService config,
            Func<TriggerEngine> triggerEngine)
        {
            _shelfService = shelfService;
            _storageService = storageService;
            _config = config;
            _triggerEngine = triggerEngine;

            LoadItemsAsync().ConfigureAwait(false);
        }

        public IReadOnlyList<ShelfLimitOption> LimitOptions { get; } = new List<ShelfLimitOption>
        {
            new() { Label = "512 MB", Megabytes = 512 },
            new() { Label = "1 GB", Megabytes = 1024 },
            new() { Label = "2 GB (recommended)", Megabytes = DefaultLimitMb },
            new() { Label = "5 GB", Megabytes = 5120 },
            new() { Label = "10 GB", Megabytes = 10240 },
            new() { Label = "Unlimited", Megabytes = 0 }
        };

        public int LimitMb
        {
            get
            {
                var raw = _config.GetSetting(LimitSettingKey);
                return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var mb) && mb >= 0
                    ? mb
                    : DefaultLimitMb;
            }
        }

        private long LimitBytes => LimitMb <= 0 ? long.MaxValue : (long)LimitMb * 1024 * 1024;

        public ShelfLimitOption SelectedLimit
        {
            get
            {
                int current = LimitMb;
                return LimitOptions.FirstOrDefault(o => o.Megabytes == current) ?? LimitOptions[2];
            }
            set
            {
                if (value == null || value.Megabytes == LimitMb) return;

                _config.SetSetting(LimitSettingKey, value.Megabytes.ToString(CultureInfo.InvariantCulture));
                OnPropertyChanged();
                OnPropertyChanged(nameof(UsageLabel));
            }
        }

        /// <summary>"1.2 GB of 2 GB used", for the Control Center.</summary>
        public string UsageLabel
        {
            get
            {
                long used = _storageService.GetUsedBytes();
                return LimitMb <= 0
                    ? $"{FormatSize(used)} used"
                    : $"{FormatSize(used)} of {FormatSize(LimitBytes)} used";
            }
        }

        public void RefreshUsage() => OnPropertyChanged(nameof(UsageLabel));

        private static string FormatSize(long bytes)
        {
            if (bytes >= 1024L * 1024 * 1024) return $"{bytes / 1024d / 1024d / 1024d:0.#} GB";
            if (bytes >= 1024L * 1024) return $"{bytes / 1024d / 1024d:0} MB";
            if (bytes >= 1024) return $"{bytes / 1024d:0} KB";
            return $"{bytes} B";
        }

        private void Warn(string message) =>
            _triggerEngine()?.Trigger(4, message, Wpf.Ui.Controls.SymbolRegular.Warning24);

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
                // A folder entry points at the user's real directory, so removing it from the Shelf must
                // only drop the reference. Deleting managed storage is for copies we made ourselves.
                if (!string.IsNullOrEmpty(item.StoredPath) && item.Type != "Folder")
                {
                    _storageService.DeleteFromStorage(item.StoredPath);
                }
                await _shelfService.DeleteItemAsync(item);
                Items.Remove(item);
                RefreshUsage();
            }
        }

        public async Task AddDroppedFileAsync(string filePath)
        {
            if (System.IO.Directory.Exists(filePath))
            {
                await AddDroppedFolderAsync(filePath);
                return;
            }

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

            if (!HasRoomFor(fileInfo.Length, fileInfo.Name)) return;

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
                RefreshUsage();
            }
        }

        /// <summary>
        /// Refuses a copy that would blow the storage budget, and says why in the notch rather than
        /// failing silently. Folders and text snippets skip this: folders are stored by reference and
        /// cost nothing, and a snippet cannot meaningfully move the needle.
        /// </summary>
        private bool HasRoomFor(long incomingBytes, string name)
        {
            long limit = LimitBytes;
            if (limit == long.MaxValue) return true;

            if (incomingBytes > limit)
            {
                Warn($"{name} is larger than the Shelf limit");
                return false;
            }

            if (_storageService.GetUsedBytes() + incomingBytes > limit)
            {
                Warn("Shelf is full - remove some items");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Folders are recorded by reference rather than copied. The Shelf advertises folder support, but
        /// deep-copying a dropped folder could duplicate gigabytes without warning, and a shortcut is what
        /// people actually want from a shelf anyway.
        /// </summary>
        private async Task AddDroppedFolderAsync(string folderPath)
        {
            var info = new System.IO.DirectoryInfo(folderPath);

            foreach (var existing in Items)
            {
                if (existing.Type == "Folder" &&
                    string.Equals(existing.StoredPath, folderPath, System.StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            var item = new ShelfItem
            {
                Type = "Folder",
                OriginalName = info.Name,
                Content = folderPath,
                StoredPath = folderPath,
                CreatedAt = System.DateTime.Now
            };

            await _shelfService.SaveItemAsync(item);
            System.Windows.Application.Current.Dispatcher.Invoke(() => Items.Insert(0, item));
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
