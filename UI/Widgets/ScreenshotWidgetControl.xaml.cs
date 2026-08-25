using Microsoft.Extensions.DependencyInjection;
using Project_Nook.ViewModels;
using System.Windows.Controls;

namespace Project_Nook.UI.Widgets
{
    public partial class ScreenshotWidgetControl : UserControl
    {
        public ScreenshotWidgetViewModel ViewModel { get; }

        public ScreenshotWidgetControl()
        {
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject()))
            {
                ViewModel = ((App)System.Windows.Application.Current).Services.GetRequiredService<ScreenshotWidgetViewModel>();
            }
            DataContext = ViewModel;
            InitializeComponent();
        }
    }
}
