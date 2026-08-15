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

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
