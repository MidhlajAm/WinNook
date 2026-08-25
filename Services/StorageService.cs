using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Project_Nook.Services
{
    public class StorageService : IStorageService
    {
        private readonly string _storageDir;
        private readonly object _sizeGate = new();
        private long? _cachedUsedBytes;

        public StorageService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _storageDir = Path.Combine(appData, "WinNook", "ShelfData");
            if (!Directory.Exists(_storageDir))
            {
                Directory.CreateDirectory(_storageDir);
            }
        }

        public async Task<string> CopyFileToStorageAsync(string sourcePath)
        {
            if (!File.Exists(sourcePath)) return null;
            
            var fileName = Path.GetFileName(sourcePath);
            var destPath = Path.Combine(_storageDir, fileName);
            
            int counter = 1;
            while (File.Exists(destPath))
            {
                string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                string ext = Path.GetExtension(fileName);
                destPath = Path.Combine(_storageDir, $"{nameWithoutExt}_{counter}{ext}");
                counter++;
            }
            
            await Task.Run(() => File.Copy(sourcePath, destPath));

            InvalidateSizeCache();
            return destPath;
        }

        public async Task<string> SaveTextToStorageAsync(string text, string hintName)
        {
            var destPath = Path.Combine(_storageDir, $"{Guid.NewGuid()}_{hintName}.txt");
            await File.WriteAllTextAsync(destPath, text);

            InvalidateSizeCache();
            return destPath;
        }

        public void DeleteFromStorage(string storedPath)
        {
            if (File.Exists(storedPath))
            {
                try { File.Delete(storedPath); InvalidateSizeCache(); } catch { }
            }
        }

        /// <summary>
        /// Measures the managed storage folder. Cached because this is checked on every drop, and
        /// invalidated whenever we add or remove a file ourselves.
        /// </summary>
        public long GetUsedBytes()
        {
            lock (_sizeGate)
            {
                if (_cachedUsedBytes.HasValue) return _cachedUsedBytes.Value;

                long total = 0;
                try
                {
                    foreach (var file in Directory.EnumerateFiles(_storageDir, "*", SearchOption.AllDirectories))
                    {
                        try { total += new FileInfo(file).Length; } catch { }
                    }
                }
                catch { }

                _cachedUsedBytes = total;
                return total;
            }
        }

        private void InvalidateSizeCache()
        {
            lock (_sizeGate) _cachedUsedBytes = null;
        }

        public void OpenFile(string storedPath)
        {
            // Folder shelf entries point at a real directory, which shell-executes into Explorer.
            if (!File.Exists(storedPath) && !Directory.Exists(storedPath)) return;

            try
            {
                Process.Start(new ProcessStartInfo { FileName = storedPath, UseShellExecute = true });
            }
            catch { }
        }

        public void RevealInExplorer(string storedPath)
        {
            try
            {
                if (File.Exists(storedPath)) Process.Start("explorer.exe", $"/select,\"{storedPath}\"");
                else if (Directory.Exists(storedPath)) Process.Start("explorer.exe", $"\"{storedPath}\"");
            }
            catch { }
        }
    }
}
