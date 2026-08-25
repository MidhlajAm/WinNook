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

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();

            // Settings writes are debounced, and force-killing the app from Task Manager never reaches
            // OnExit. Dismissing this window is the moment a setting was most likely just edited, so
            // commit it now rather than trusting the process to shut down cleanly.
            App.Current.Services.GetRequiredService<Project_Nook.Services.IConfigurationService>().Flush();

            base.OnClosing(e);
        }

        private void PickNotchColor_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new System.Windows.Forms.ColorDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var color = dialog.Color;
                ViewModel.Island.NotchColor = $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
            }
        }

        private void PickHoverNotchColor_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new System.Windows.Forms.ColorDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var color = dialog.Color;
                ViewModel.Island.HoverNotchColor = $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
            }
        }

        private void PickShadowColor_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new System.Windows.Forms.ColorDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var color = dialog.Color;
                ViewModel.Island.ShadowColor = $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
            }
        }

        private void PickHoverShadowColor_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new System.Windows.Forms.ColorDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var color = dialog.Color;
                ViewModel.Island.HoverShadowColor = $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
            }
        }

        private void LinkedIn_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://www.linkedin.com/in/midhlaj-am/");
        }

        private void GitHub_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://github.com/MidhlajAm");
        }

        private void OpenKofi_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://ko-fi.com/T8Q124XOE1");
        }

        private void OpenUrl(string url)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch { }
        }

        private void TabControl_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

        }
    }
}
