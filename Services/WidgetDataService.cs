using Project_Nook.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Project_Nook.Services
{
    public interface IWidgetDataService
    {
        Task InitializeAsync();

        Task<NoteItem> GetScratchpadAsync();
        Task SaveScratchpadAsync(NoteItem note);

        Task<List<ClipboardEntry>> GetClipboardHistoryAsync();
        Task<ClipboardEntry> AddClipboardEntryAsync(string content, int maxEntries);
        Task DeleteClipboardEntryAsync(ClipboardEntry entry);
        Task ClearClipboardHistoryAsync();
    }

    /// <summary>
    /// SQLite storage for the widgets that outlive a session. Mirrors ShelfService: same lazy
    /// InitializeAsync guard, same %LocalAppData%\WinNook folder, its own database file so the Shelf's
    /// shelf.db3 stays exactly what its name says it is.
    /// </summary>
    public class WidgetDataService : IWidgetDataService
    {
        private SQLiteAsyncConnection _db;

        public async Task InitializeAsync()
        {
            if (_db != null) return;

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dbDir = Path.Combine(appData, "WinNook");
            if (!Directory.Exists(dbDir)) Directory.CreateDirectory(dbDir);

            var dbPath = Path.Combine(dbDir, "widgets.db3");
            _db = new SQLiteAsyncConnection(dbPath);

            await _db.CreateTableAsync<NoteItem>();
            await _db.CreateTableAsync<ClipboardEntry>();
        }

        /// <summary>
        /// The notes widget is a single always-there scratchpad rather than a note list: the expanded notch
        /// is a ~77px strip, which is honest room for one editor and not much else. The table is id-keyed,
        /// so growing this into multiple notes later is additive.
        /// </summary>
        public async Task<NoteItem> GetScratchpadAsync()
        {
            await InitializeAsync();

            var existing = await _db.Table<NoteItem>().OrderByDescending(n => n.UpdatedAt).FirstOrDefaultAsync();
            if (existing != null) return existing;

            return new NoteItem
            {
                Content = string.Empty,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
        }

        public async Task SaveScratchpadAsync(NoteItem note)
        {
            await InitializeAsync();

            note.UpdatedAt = DateTime.Now;

            if (note.Id != 0) await _db.UpdateAsync(note);
            else await _db.InsertAsync(note);
        }

        public async Task<List<ClipboardEntry>> GetClipboardHistoryAsync()
        {
            await InitializeAsync();
            return await _db.Table<ClipboardEntry>().OrderByDescending(c => c.CapturedAt).ToListAsync();
        }

        /// <summary>
        /// Records a clip and trims the history to the newest <paramref name="maxEntries"/>.
        /// Re-copying something already at the top is ignored rather than duplicated.
        /// </summary>
        public async Task<ClipboardEntry> AddClipboardEntryAsync(string content, int maxEntries)
        {
            await InitializeAsync();

            if (string.IsNullOrWhiteSpace(content)) return null;

            var newest = await _db.Table<ClipboardEntry>().OrderByDescending(c => c.CapturedAt).FirstOrDefaultAsync();
            if (newest != null && newest.Content == content) return null;

            // A repeat of something further down the list moves back to the top instead of duplicating.
            var duplicates = await _db.Table<ClipboardEntry>().Where(c => c.Content == content).ToListAsync();
            foreach (var duplicate in duplicates) await _db.DeleteAsync(duplicate);

            var entry = new ClipboardEntry { Content = content, CapturedAt = DateTime.Now };
            await _db.InsertAsync(entry);

            var all = await _db.Table<ClipboardEntry>().OrderByDescending(c => c.CapturedAt).ToListAsync();
            foreach (var stale in all.Skip(maxEntries)) await _db.DeleteAsync(stale);

            return entry;
        }

        public async Task DeleteClipboardEntryAsync(ClipboardEntry entry)
        {
            await InitializeAsync();
            if (entry != null) await _db.DeleteAsync(entry);
        }

        public async Task ClearClipboardHistoryAsync()
        {
            await InitializeAsync();
            await _db.DeleteAllAsync<ClipboardEntry>();
        }
    }
}
