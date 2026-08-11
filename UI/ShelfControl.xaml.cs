using Microsoft.Extensions.DependencyInjection;
using Project_Nook.ViewModels;
using System.Windows.Controls;

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
    }
}
