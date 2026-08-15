using Microsoft.Extensions.DependencyInjection;
using Project_Nook.Models;
using Project_Nook.ViewModels;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Threading.Tasks;

namespace Project_Nook.UI
{
    public partial class IslandWindow : Window
    {
        public static readonly DependencyProperty AnimatableCornerRadiusProperty =
            DependencyProperty.Register("AnimatableCornerRadius", typeof(double), typeof(IslandWindow), new PropertyMetadata(24.0, OnCornerRadiusChanged));

        public double AnimatableCornerRadius
        {
            get => (double)GetValue(AnimatableCornerRadiusProperty);
            set => SetValue(AnimatableCornerRadiusProperty, value);
        }

        private static void OnCornerRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is IslandWindow window && e.NewValue is double radius)
            {
                window.NotchBorder.CornerRadius = new CornerRadius(0, 0, radius, radius);
            }
        }

        public IslandViewModel ViewModel { get; }

        public IslandWindow()
        {
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                ViewModel = ((App)Application.Current).Services.GetRequiredService<IslandViewModel>();
                ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
            DataContext = this;
            InitializeComponent();
            
            this.Width = SystemParameters.PrimaryScreenWidth;
            if (ViewModel != null)
            {
                AnimatableCornerRadius = ViewModel.CornerRadius;
            }
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IslandViewModel.CurrentState))
            {
                AnimateState(ViewModel.CurrentState);
            }
            else if (e.PropertyName == nameof(IslandViewModel.IdleWidth) || e.PropertyName == nameof(IslandViewModel.IdleHeight)
                  || e.PropertyName == nameof(IslandViewModel.NotchColor) || e.PropertyName == nameof(IslandViewModel.ShadowColor)
                  || e.PropertyName == nameof(IslandViewModel.CornerRadius) || e.PropertyName == nameof(IslandViewModel.HoverCornerRadius))
            {
                if (ViewModel.CurrentState == IslandState.Idle)
                {
                    // Clear the animation to let the real-time slider data binding take over!
                    NotchBorder.BeginAnimation(FrameworkElement.WidthProperty, null);
                    NotchBorder.BeginAnimation(FrameworkElement.HeightProperty, null);
                    NotchBackground.BeginAnimation(System.Windows.Media.SolidColorBrush.ColorProperty, null);
                    NotchShadow.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.ColorProperty, null);
                    this.BeginAnimation(AnimatableCornerRadiusProperty, null);
                    
                    if (e.PropertyName == nameof(IslandViewModel.CornerRadius))
                    {
                        AnimatableCornerRadius = ViewModel.CornerRadius;
                    }
                }
                else
                {
                    // If modified while hovered, update instantly
                    double targetWidth = ViewModel.CurrentState == IslandState.Hover ? ViewModel.HoverWidth : ViewModel.IdleWidth + 100;
                    double targetHeight = ViewModel.CurrentState == IslandState.Hover ? ViewModel.HoverHeight : ViewModel.IdleHeight + 110;
                    NotchBorder.BeginAnimation(FrameworkElement.WidthProperty, new DoubleAnimation { To = targetWidth, Duration = TimeSpan.Zero });
                    NotchBorder.BeginAnimation(FrameworkElement.HeightProperty, new DoubleAnimation { To = targetHeight, Duration = TimeSpan.Zero });
                    
                    if (e.PropertyName == nameof(IslandViewModel.NotchColor) || e.PropertyName == nameof(IslandViewModel.ShadowColor))
                    {
                        System.Windows.Media.Color targetNotchColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(ViewModel.CurrentState == IslandState.Hover ? ViewModel.HoverNotchColor : ViewModel.NotchColor);
                        System.Windows.Media.Color targetShadowColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(ViewModel.CurrentState == IslandState.Hover ? ViewModel.HoverShadowColor : ViewModel.ShadowColor);
                        NotchBackground.BeginAnimation(System.Windows.Media.SolidColorBrush.ColorProperty, new ColorAnimation { To = targetNotchColor, Duration = TimeSpan.Zero });
                        NotchShadow.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.ColorProperty, new ColorAnimation { To = targetShadowColor, Duration = TimeSpan.Zero });
                    }

                    if (e.PropertyName == nameof(IslandViewModel.CornerRadius) || e.PropertyName == nameof(IslandViewModel.HoverCornerRadius))
                    {
                        double targetRadius = ViewModel.CurrentState == IslandState.Hover ? ViewModel.HoverCornerRadius : ViewModel.CornerRadius;
                        this.BeginAnimation(AnimatableCornerRadiusProperty, new DoubleAnimation { To = targetRadius, Duration = TimeSpan.Zero });
                    }
                }
            }
        }

        private void AnimateState(IslandState state)
        {
            ExpandedContentGrid.Visibility = state == IslandState.Expanded ? Visibility.Visible : Visibility.Collapsed;
            IdleContentGrid.Visibility = state == IslandState.Expanded ? Visibility.Collapsed : Visibility.Visible;

            double targetWidth = state switch
            {
                IslandState.Idle => ViewModel.IdleWidth,
                IslandState.Hover => ViewModel.HoverWidth,
                IslandState.Expanded => 400,
                IslandState.DropTarget => ViewModel.IdleWidth + 40,
                IslandState.Success => ViewModel.IdleWidth + 60,
                IslandState.Triggered => ViewModel.IdleWidth + 120,
                _ => ViewModel.IdleWidth
            };
            
            double targetHeight = state switch
            {
                IslandState.Idle => ViewModel.IdleHeight,
                IslandState.Hover => ViewModel.HoverHeight,
                IslandState.Expanded => 300,
                IslandState.DropTarget => ViewModel.IdleHeight + 20,
                IslandState.Success => ViewModel.IdleHeight + 30,
                IslandState.Triggered => ViewModel.IdleHeight + 10,
                _ => ViewModel.IdleHeight
            };

            var converter = new System.Windows.Media.BrushConverter();
            System.Windows.Media.Color targetNotchColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(ViewModel.NotchColor);
            System.Windows.Media.Color targetShadowColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(ViewModel.ShadowColor);

            if (state != IslandState.Idle)
            {
                targetNotchColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(ViewModel.HoverNotchColor);
                targetShadowColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(ViewModel.HoverShadowColor);
            }

            double targetCornerRadius = state == IslandState.Hover ? ViewModel.HoverCornerRadius : ViewModel.CornerRadius;

            IEasingFunction ease;
            if (state == IslandState.Idle)
            {
                // Smoothly collapse back to Idle without wobbling
                ease = new QuarticEase { EasingMode = EasingMode.EaseOut };
            }
            else
            {
                // Organic spring-like expansion overshoot
                ease = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.5 };
            }

            if (state == IslandState.Success)
            {
                Task.Delay(ViewModel.AnimationSpeedMs).ContinueWith(_ => Dispatcher.Invoke(() => 
                {
                    if (ViewModel.CurrentState == IslandState.Success) ViewModel.CurrentState = IslandState.Idle;
                }));
            }

            var widthAnim = new DoubleAnimation { To = targetWidth, Duration = TimeSpan.FromMilliseconds(ViewModel.AnimationSpeedMs), EasingFunction = ease };
            var heightAnim = new DoubleAnimation { To = targetHeight, Duration = TimeSpan.FromMilliseconds(ViewModel.AnimationSpeedMs), EasingFunction = ease };
            var notchColorAnim = new ColorAnimation { To = targetNotchColor, Duration = TimeSpan.FromMilliseconds(ViewModel.AnimationSpeedMs), EasingFunction = ease };
            var shadowColorAnim = new ColorAnimation { To = targetShadowColor, Duration = TimeSpan.FromMilliseconds(ViewModel.AnimationSpeedMs), EasingFunction = ease };
            var cornerRadiusAnim = new DoubleAnimation { To = targetCornerRadius, Duration = TimeSpan.FromMilliseconds(ViewModel.AnimationSpeedMs), EasingFunction = ease };

            NotchBorder.BeginAnimation(FrameworkElement.WidthProperty, widthAnim);
            NotchBorder.BeginAnimation(FrameworkElement.HeightProperty, heightAnim);
            NotchBackground.BeginAnimation(System.Windows.Media.SolidColorBrush.ColorProperty, notchColorAnim);
            NotchShadow.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.ColorProperty, shadowColorAnim);
            this.BeginAnimation(AnimatableCornerRadiusProperty, cornerRadiusAnim);
        }

        private void NotchBorder_MouseEnter(object sender, MouseEventArgs e) => ViewModel.HoverCommand.Execute(null);
        private void NotchBorder_MouseLeave(object sender, MouseEventArgs e) => ViewModel.UnhoverCommand.Execute(null);
        private void NotchBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.CurrentState == IslandState.Expanded) ViewModel.CollapseCommand.Execute(null);
            else ViewModel.ExpandCommand.Execute(null);
        }

        private void NotchBorder_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetDataPresent(DataFormats.Text))
            {
                e.Effects = DragDropEffects.Copy;
                ViewModel.CurrentState = IslandState.DropTarget;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void NotchBorder_DragLeave(object sender, DragEventArgs e)
        {
            if (ViewModel.CurrentState == IslandState.DropTarget)
            {
                ViewModel.CurrentState = IslandState.Idle;
            }
            e.Handled = true;
        }

        private async void NotchBorder_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                ViewModel.CurrentState = IslandState.Success;
                var shelfVm = App.Current.Services.GetRequiredService<ShelfViewModel>();
                foreach (var file in files)
                {
                    await shelfVm.AddDroppedFileAsync(file);
                }
            }
            else if (e.Data.GetDataPresent(DataFormats.Text))
            {
                var text = (string)e.Data.GetData(DataFormats.Text);
                ViewModel.CurrentState = IslandState.Success;
                var shelfVm = App.Current.Services.GetRequiredService<ShelfViewModel>();
                await shelfVm.AddDroppedTextAsync(text);
            }
            else
            {
                ViewModel.CurrentState = IslandState.Idle;
            }
            e.Handled = true;
        }
    }
}
