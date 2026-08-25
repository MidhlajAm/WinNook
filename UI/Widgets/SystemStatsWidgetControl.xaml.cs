using Microsoft.Extensions.DependencyInjection;
using Project_Nook.ViewModels;
using System.Windows.Controls;

namespace Project_Nook.UI.Widgets
{
    public partial class SystemStatsWidgetControl : UserControl
    {
        public SystemStatsWidgetViewModel ViewModel { get; }

        public SystemStatsWidgetControl()
        {
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject()))
            {
                ViewModel = ((App)System.Windows.Application.Current).Services.GetRequiredService<SystemStatsWidgetViewModel>();
            }
            DataContext = ViewModel;
            InitializeComponent();
        }
    }
}
