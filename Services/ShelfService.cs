using Project_Nook.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Project_Nook.Services
{
    public class ShelfService : IShelfService
    {
        private SQLiteAsyncConnection _db;

        public async Task InitializeAsync()
        {
            if (_db != null) return;

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dbDir = Path.Combine(appData, "WinNook");
            if (!Directory.Exists(dbDir)) Directory.CreateDirectory(dbDir);
            
            var dbPath = Path.Combine(dbDir, "shelf.db3");
            _db = new SQLiteAsyncConnection(dbPath);
            await _db.CreateTableAsync<ShelfItem>();
        }

        public async Task<List<ShelfItem>> GetAllItemsAsync()
        {
            await InitializeAsync();
            return await _db.Table<ShelfItem>().OrderByDescending(x => x.CreatedAt).ToListAsync();
        }

        public async Task SaveItemAsync(ShelfItem item)
        {
            await InitializeAsync();
            if (item.Id != 0)
            {
                await _db.UpdateAsync(item);
            }
            else
            {
                await _db.InsertAsync(item);
            }
        }

        public async Task DeleteItemAsync(ShelfItem item)
        {
            await InitializeAsync();
            await _db.DeleteAsync(item);
        }
    }
}
