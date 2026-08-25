using Microsoft.Extensions.DependencyInjection;
using Project_Nook.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Project_Nook.UI.Widgets
{
    public partial class QuickLaunchWidgetControl : UserControl
    {
        public QuickLaunchWidgetViewModel ViewModel { get; }

        public QuickLaunchWidgetControl()
        {
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                ViewModel = ((App)Application.Current).Services.GetRequiredService<QuickLaunchWidgetViewModel>();
            }
            DataContext = ViewModel;
            InitializeComponent();
        }
    }
}
