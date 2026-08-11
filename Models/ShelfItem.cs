using SQLite;
using System;

namespace Project_Nook.Models
{
    public class ShelfItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Type { get; set; } // "File", "Folder", "Text", "URL"
        public string Content { get; set; } // Text content or File Path
        public string StoredPath { get; set; } // Path in our isolated storage
        public string ThumbnailPath { get; set; } // Path to generated thumbnail

        public string OriginalName { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public bool IsPinned { get; set; }
    }
}
