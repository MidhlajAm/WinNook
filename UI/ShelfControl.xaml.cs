using Microsoft.Extensions.DependencyInjection;
using Project_Nook.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using Project_Nook.Models;

namespace Project_Nook.UI
{
    public partial class ShelfControl : UserControl
    {
        public ShelfViewModel ViewModel { get; }

        public ShelfControl()
        {
            ViewModel = App.Current.Services.GetRequiredService<ShelfViewModel>();
            DataContext = ViewModel;
            InitializeComponent();
        }

        private Point _startPoint;

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(null);
        }

        private void Border_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point mousePos = e.GetPosition(null);
                Vector diff = _startPoint - mousePos;

                if (System.Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    System.Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    if (sender is FrameworkElement fe && fe.DataContext is ShelfItem item)
                    {
                        if (item.Type == "Text" && !string.IsNullOrEmpty(item.Content))
                        {
                            DataObject dragData = new DataObject(DataFormats.Text, item.Content);
                            DragDrop.DoDragDrop(fe, dragData, DragDropEffects.Copy);
                        }
                        else if (!string.IsNullOrEmpty(item.StoredPath))
                        {
                            DataObject dragData = new DataObject(DataFormats.FileDrop, new string[] { item.StoredPath });
                            DragDrop.DoDragDrop(fe, dragData, DragDropEffects.Copy);
                        }
                    }
                }
            }
        }
    }
}
