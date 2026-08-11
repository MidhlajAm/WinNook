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
            ShelfView.Visibility = state == IslandState.Expanded ? Visibility.Visible : Visibility.Collapsed;
            StateText.Visibility = state == IslandState.Expanded ? Visibility.Collapsed : Visibility.Visible;

            double targetWidth = state switch
            {
                IslandState.Idle => ViewModel.IdleWidth,
                IslandState.Hover => ViewModel.IdleWidth + 20,
                IslandState.Expanded => 400,
                IslandState.DropTarget => ViewModel.IdleWidth + 40,
                IslandState.Success => ViewModel.IdleWidth + 60,
                _ => ViewModel.IdleWidth
            };
            
            double targetHeight = state switch
            {
                IslandState.Idle => ViewModel.IdleHeight,
                IslandState.Hover => ViewModel.IdleHeight + 10,
                IslandState.Expanded => 300,
                IslandState.DropTarget => ViewModel.IdleHeight + 20,
                IslandState.Success => ViewModel.IdleHeight + 30,
                _ => ViewModel.IdleHeight
            };

            var ease = new ElasticEase { EasingMode = EasingMode.EaseOut, Oscillations = 1, Springiness = 4 };
            if (state == IslandState.Success)
            {
                ease = new ElasticEase { EasingMode = EasingMode.EaseOut, Oscillations = 2, Springiness = 3 };
                
                Task.Delay(ViewModel.AnimationSpeedMs).ContinueWith(_ => Dispatcher.Invoke(() => 
                {
                    if (ViewModel.CurrentState == IslandState.Success) ViewModel.CurrentState = IslandState.Idle;
                }));
            }

            var widthAnim = new DoubleAnimation { To = targetWidth, Duration = TimeSpan.FromMilliseconds(ViewModel.AnimationSpeedMs), EasingFunction = ease };
            var heightAnim = new DoubleAnimation { To = targetHeight, Duration = TimeSpan.FromMilliseconds(ViewModel.AnimationSpeedMs), EasingFunction = ease };

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
