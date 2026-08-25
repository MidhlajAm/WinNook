using Microsoft.Extensions.DependencyInjection;
using Project_Nook.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace Project_Nook.UI.Widgets
{
    public partial class ClipboardPageControl : UserControl
    {
        public ClipboardWidgetViewModel ViewModel { get; }

        public ClipboardPageControl()
        {
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject()))
            {
                ViewModel = ((App)System.Windows.Application.Current).Services.GetRequiredService<ClipboardWidgetViewModel>();
            }
            DataContext = ViewModel;
            InitializeComponent();
        }

        private void BackButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;   // stop the notch seeing this and collapsing
            ViewModel?.CloseCommand.Execute(null);
        }

        private void HistoryScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                if (e.Delta > 0) scrollViewer.LineLeft();
                else scrollViewer.LineRight();

                e.Handled = true;
            }
        }
    }
}
