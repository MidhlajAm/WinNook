using Wpf.Ui.Controls;
using Microsoft.Extensions.DependencyInjection;
using Project_Nook.ViewModels;
using System.Windows;

namespace Project_Nook
{
    public partial class MainWindow : FluentWindow
    {
        public ControlCenterViewModel ViewModel { get; }

        public MainWindow()
        {
            ViewModel = App.Current.Services.GetRequiredService<ControlCenterViewModel>();
            DataContext = this;
            InitializeComponent();
        }

        private void PickNotchColor_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new System.Windows.Forms.ColorDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ViewModel.Island.NotchColor = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
            }
        }

        private void PickShadowColor_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new System.Windows.Forms.ColorDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ViewModel.Island.ShadowColor = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
            }
        }
    }
}
