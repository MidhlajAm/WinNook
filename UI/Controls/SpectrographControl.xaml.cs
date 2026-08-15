using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Project_Nook.UI.Controls
{
    public partial class SpectrographControl : UserControl
    {
        public static readonly DependencyProperty IsPlayingProperty =
            DependencyProperty.Register(
                nameof(IsPlaying),
                typeof(bool),
                typeof(SpectrographControl),
                new PropertyMetadata(false, OnIsPlayingChanged));

        public bool IsPlaying
        {
            get => (bool)GetValue(IsPlayingProperty);
            set => SetValue(IsPlayingProperty, value);
        }

        public SpectrographControl()
        {
            InitializeComponent();
        }

        private static void OnIsPlayingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SpectrographControl control)
            {
                if ((bool)e.NewValue)
                {
                    control.StartAnimation();
                }
                else
                {
                    control.StopAnimation();
                }
            }
        }

        private void StartAnimation()
        {
            var storyboard = (Storyboard)Resources["SpectrographAnimation"];
            storyboard?.Begin();
        }

        private void StopAnimation()
        {
            var storyboard = (Storyboard)Resources["SpectrographAnimation"];
            storyboard?.Stop();
        }
    }
}
