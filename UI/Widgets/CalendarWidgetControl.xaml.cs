using Microsoft.Extensions.DependencyInjection;
using Project_Nook.ViewModels;
using System.Windows.Controls;

namespace Project_Nook.UI.Widgets
{
    public partial class CalendarWidgetControl : UserControl
    {
        public CalendarWidgetViewModel ViewModel { get; }

        public CalendarWidgetControl()
        {
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject()))
            {
                ViewModel = ((App)System.Windows.Application.Current).Services.GetRequiredService<CalendarWidgetViewModel>();
            }
            DataContext = ViewModel;
            InitializeComponent();
        }
    }
}
