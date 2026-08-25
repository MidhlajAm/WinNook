using Microsoft.Extensions.DependencyInjection;
using Project_Nook.ViewModels;
using System.Windows.Controls;

namespace Project_Nook.UI.Widgets
{
    public partial class ClipboardWidgetControl : UserControl
    {
        public ClipboardWidgetViewModel ViewModel { get; }

        public ClipboardWidgetControl()
        {
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject()))
            {
                ViewModel = ((App)System.Windows.Application.Current).Services.GetRequiredService<ClipboardWidgetViewModel>();
            }
            DataContext = ViewModel;
            InitializeComponent();
        }
    }
}
