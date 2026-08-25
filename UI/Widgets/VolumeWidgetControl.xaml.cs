using Microsoft.Extensions.DependencyInjection;
using Project_Nook.ViewModels;
using Project_Nook.Widgets;
using System.Windows.Controls;
using System.Windows.Input;

namespace Project_Nook.UI.Widgets
{
    public partial class VolumeWidgetControl : UserControl, IWheelInput
    {
        public VolumeWidgetViewModel ViewModel { get; }

        public VolumeWidgetControl()
        {
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject()))
            {
                ViewModel = ((App)System.Windows.Application.Current).Services.GetRequiredService<VolumeWidgetViewModel>();
            }
            DataContext = ViewModel;
            InitializeComponent();
        }

        private void Tile_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ViewModel?.Nudge(e.Delta > 0 ? 1 : -1);

            // Swallow it so the rail does not also scroll sideways once the event bubbles back up.
            e.Handled = true;
        }
    }
}
