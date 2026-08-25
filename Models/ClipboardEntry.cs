using SQLite;
using System;

namespace Project_Nook.Models
{
    public class ClipboardEntry
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>Full captured text. Only text is stored; images would balloon the database.</summary>
        public string Content { get; set; }

        public DateTime CapturedAt { get; set; }
    }
}
