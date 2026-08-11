using Microsoft.Extensions.DependencyInjection;
using Project_Nook.Models;
using Project_Nook.ViewModels;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Project_Nook.UI
{
    public partial class IslandWindow : Window
    {
        public IslandViewModel ViewModel { get; }

        public IslandWindow()
        {
            ViewModel = App.Current.Services.GetRequiredService<IslandViewModel>();
            DataContext = this;
            InitializeComponent();
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            
            this.Width = SystemParameters.PrimaryScreenWidth;
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IslandViewModel.CurrentState))
            {
                AnimateState(ViewModel.CurrentState);
            }
            else if (e.PropertyName == nameof(IslandViewModel.IdleWidth) || e.PropertyName == nameof(IslandViewModel.IdleHeight))
            {
                if (ViewModel.CurrentState == IslandState.Idle)
                {
                    // Clear the animation to let the real-time slider data binding take over!
                    NotchBorder.BeginAnimation(FrameworkElement.WidthProperty, null);
                    NotchBorder.BeginAnimation(FrameworkElement.HeightProperty, null);
                }
                else
                {
                    // If modified while hovered, update instantly
                    double targetWidth = ViewModel.CurrentState == IslandState.Hover ? ViewModel.IdleWidth + 20 : ViewModel.IdleWidth + 100;
                    double targetHeight = ViewModel.CurrentState == IslandState.Hover ? ViewModel.IdleHeight + 10 : ViewModel.IdleHeight + 110;
                    NotchBorder.BeginAnimation(FrameworkElement.WidthProperty, new DoubleAnimation { To = targetWidth, Duration = TimeSpan.Zero });
                    NotchBorder.BeginAnimation(FrameworkElement.HeightProperty, new DoubleAnimation { To = targetHeight, Duration = TimeSpan.Zero });
                }
            }
        }

        private void AnimateState(IslandState state)
        {
            double targetWidth = state switch
            {
                IslandState.Idle => ViewModel.IdleWidth,
                IslandState.Hover => ViewModel.IdleWidth + 20,
                IslandState.Expanded => ViewModel.IdleWidth + 100,
                _ => ViewModel.IdleWidth
            };
            
            double targetHeight = state switch
            {
                IslandState.Idle => ViewModel.IdleHeight,
                IslandState.Hover => ViewModel.IdleHeight + 10,
                IslandState.Expanded => ViewModel.IdleHeight + 110,
                _ => ViewModel.IdleHeight
            };

            StateText.Text = state == IslandState.Expanded ? "🎵 Song Name  ◀ ❚❚ ▶" : "11:50 🔋";

            var ease = new ElasticEase { EasingMode = EasingMode.EaseOut, Oscillations = 1, Springiness = 4 };

            var widthAnim = new DoubleAnimation { To = targetWidth, Duration = TimeSpan.FromMilliseconds(500), EasingFunction = ease };
            var heightAnim = new DoubleAnimation { To = targetHeight, Duration = TimeSpan.FromMilliseconds(500), EasingFunction = ease };

            NotchBorder.BeginAnimation(FrameworkElement.WidthProperty, widthAnim);
            NotchBorder.BeginAnimation(FrameworkElement.HeightProperty, heightAnim);
        }

        private void NotchBorder_MouseEnter(object sender, MouseEventArgs e) => ViewModel.HoverCommand.Execute(null);
        private void NotchBorder_MouseLeave(object sender, MouseEventArgs e) => ViewModel.UnhoverCommand.Execute(null);
        private void NotchBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.CurrentState == IslandState.Expanded) ViewModel.CollapseCommand.Execute(null);
            else ViewModel.ExpandCommand.Execute(null);
        }
    }
}
