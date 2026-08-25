using Project_Nook.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Project_Nook.Services
{
    public class ShelfService : IShelfService
    {
        private readonly SemaphoreSlim _initGate = new(1, 1);
        private SQLiteAsyncConnection _db;
        private bool _isInitialized;

        /// <summary>
        /// Opens the database and creates its table, once, even when several callers race here.
        /// The connection is published only after the schema exists, or a concurrent caller can skip
        /// the wait and query a table that has not been created yet.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            await _initGate.WaitAsync();
            try
            {
                if (_isInitialized) return;

                var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var dbDir = Path.Combine(appData, "WinNook");
                if (!Directory.Exists(dbDir)) Directory.CreateDirectory(dbDir);

                var dbPath = Path.Combine(dbDir, "shelf.db3");
                var connection = new SQLiteAsyncConnection(dbPath);
                await connection.CreateTableAsync<ShelfItem>();

                _db = connection;
                _isInitialized = true;
            }
            finally
            {
                _initGate.Release();
            }
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
