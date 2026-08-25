using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Project_Nook.Models
{
    public class AppItem : INotifyPropertyChanged
    {
        private bool _isDiveInEnabled;

        public string DisplayName { get; set; } = string.Empty;
        public string ExecutableName { get; set; } = string.Empty;

        /// <summary>
        /// Full path to the executable. ExecutableName is only a filename, which is enough to match a
        /// foreground window for Dive In but not enough to launch anything.
        /// </summary>
        public string TargetPath { get; set; } = string.Empty;
        public System.Windows.Media.ImageSource? IconImage { get; set; }

        public bool IsDiveInEnabled
        {
            get => _isDiveInEnabled;
            set
            {
                if (_isDiveInEnabled != value)
                {
                    _isDiveInEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isPinned;

        /// <summary>Whether this app sits in the Quick Launch rail.</summary>
        public bool IsPinned
        {
            get => _isPinned;
            set
            {
                if (_isPinned != value)
                {
                    _isPinned = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
