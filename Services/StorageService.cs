using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Project_Nook.Services
{
    public class StorageService : IStorageService
    {
        private readonly string _storageDir;

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
            
            return destPath;
        }

        public async Task<string> SaveTextToStorageAsync(string text, string hintName)
        {
            var destPath = Path.Combine(_storageDir, $"{Guid.NewGuid()}_{hintName}.txt");
            await File.WriteAllTextAsync(destPath, text);
            return destPath;
        }

        public void DeleteFromStorage(string storedPath)
        {
            if (File.Exists(storedPath))
            {
                try { File.Delete(storedPath); } catch { }
            }
        }

        public void OpenFile(string storedPath)
        {
            if (File.Exists(storedPath))
            {
                Process.Start(new ProcessStartInfo { FileName = storedPath, UseShellExecute = true });
            }
        }

        public void RevealInExplorer(string storedPath)
        {
            if (File.Exists(storedPath))
            {
                Process.Start("explorer.exe", $"/select,\"{storedPath}\"");
            }
        }
    }
}
