using Microsoft.Extensions.DependencyInjection;
using Project_Nook.ViewModels;
using System.Windows.Controls;

namespace Project_Nook.UI.Widgets
{
    public partial class TimerWidgetControl : UserControl
    {
        public TimerWidgetViewModel ViewModel { get; }

        public TimerWidgetControl()
        {
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject()))
            {
                ViewModel = ((App)System.Windows.Application.Current).Services.GetRequiredService<TimerWidgetViewModel>();
            }
            DataContext = ViewModel;
            InitializeComponent();
        }
    }
}
