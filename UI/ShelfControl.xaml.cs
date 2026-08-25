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
            
            e.Handled = true;
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

        private void HorizontalScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                // Translate vertical scrolling to horizontal
                if (e.Delta > 0)
                {
                    scrollViewer.LineLeft();
                }
                else
                {
                    scrollViewer.LineRight();
                }
                e.Handled = true;
            }
        }

        private void Item_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Check if the click originated from the delete badge
            var source = e.OriginalSource as DependencyObject;
            while (source != null && source != sender as DependencyObject)
            {
                if (source is FrameworkElement element && element.Name == "DeleteBadge")
                {
                    return; // Ignore clicks on the delete badge so it doesn't open the item
                }
                source = System.Windows.Media.VisualTreeHelper.GetParent(source);
            }

            if (sender is FrameworkElement fe && fe.DataContext is ShelfItem item)
            {
                ViewModel.OpenItemCommand.Execute(item);
            }
        }

        private void Item_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Just let the mouse down through for drag/drop, but we could mark handled to stop island collapse.
            // Actually, we can just handle it here and start drag manually if needed, or let Border_MouseLeftButtonDown do it.
        }

        private void BackButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true; // Prevent NotchBorder from seeing this and collapsing the island
            ViewModel.CloseShelfCommand.Execute(null);
        }

        private void BackButton_Click(object sender, MouseButtonEventArgs e)
        {
            // Now handled in PreviewMouseLeftButtonDown for immediate response without bubbling
        }
    }
}
