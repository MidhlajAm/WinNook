using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Project_Nook.UI.Controls
{
    public partial class MarqueeTextBlock : UserControl
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(MarqueeTextBlock), new PropertyMetadata(string.Empty, OnTextChanged));

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public MarqueeTextBlock()
        {
            InitializeComponent();
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (MarqueeTextBlock)d;
            control.Dispatcher.InvokeAsync(() => control.UpdateMarquee());
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Dispatcher.InvokeAsync(() => UpdateMarquee());
        }

        private void MainText_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Dispatcher.InvokeAsync(() => UpdateMarquee());
        }

        private DispatcherTimer _timer;
        private double _currentOffset = 0;
        private double _direction = -1;

        private void UpdateMarquee()
        {
            if (Translate == null || RootGrid == null || MainText == null)
                return;

            if (_timer != null)
            {
                _timer.Stop();
                _timer = null;
            }
            
            Translate.BeginAnimation(TranslateTransform.XProperty, null);
            Translate.X = 0;
            _currentOffset = 0;
            _direction = -1;

            if (RootGrid.ActualWidth == 0 || MainText.ActualWidth == 0)
                return;

            if (MainText.ActualWidth > RootGrid.ActualWidth)
            {
                double overflow = MainText.ActualWidth - RootGrid.ActualWidth;
                
                _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
                _timer.Tick += (s, e) =>
                {
                    _currentOffset += 0.5 * _direction;
                    
                    if (_currentOffset <= -overflow)
                    {
                        _currentOffset = -overflow;
                        _direction = 1; // reverse
                    }
                    else if (_currentOffset >= 0)
                    {
                        _currentOffset = 0;
                        _direction = -1; // forward
                    }
                    
                    Translate.X = _currentOffset;
                };
                _timer.Start();
            }
        }
    }
}
