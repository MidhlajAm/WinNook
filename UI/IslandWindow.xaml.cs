using Microsoft.Extensions.DependencyInjection;
using Project_Nook.Animation;
using Project_Nook.Models;
using Project_Nook.ViewModels;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

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
                window.ApplyCornerRadius(radius);
            }
        }

        /// <summary>
        /// A notch flush against the screen edge only rounds its bottom corners; the top edge is the
        /// screen edge. Once it floats clear of the edge it is a free-standing pill, so all four corners
        /// round - which is what makes it read as a Dynamic Island rather than a screen cutout.
        /// </summary>
        private void ApplyCornerRadius(double radius)
        {
            if (NotchBorder == null) return;

            NotchBorder.CornerRadius = ViewModel?.IsFloating == true
                ? new CornerRadius(radius)
                : new CornerRadius(0, 0, radius, radius);
        }

        public static readonly DependencyProperty AnimatableFilletRadiusProperty =
            DependencyProperty.Register("AnimatableFilletRadius", typeof(double), typeof(IslandWindow), new PropertyMetadata(16.0));

        public double AnimatableFilletRadius
        {
            get => (double)GetValue(AnimatableFilletRadiusProperty);
            set => SetValue(AnimatableFilletRadiusProperty, value);
        }

        public IslandViewModel ViewModel { get; }

        // Every animated quantity is a spring. Nothing here has a duration: a state change, a slider drag and
        // an interrupted transition are all the same operation, a retarget from the current value and velocity.
        private readonly SpringSystem _springs = new();
        private SpringDouble _widthSpring;
        private SpringDouble _heightSpring;
        private SpringDouble _cornerSpring;
        private SpringDouble _filletSpring;
        private SpringDouble _contentSpring;   // 0 = idle content, 1 = expanded content
        private SpringDouble _paletteSpring;   // 0 = idle palette, 1 = active palette

        private Color _idleNotchColor;
        private Color _idleShadowColor;
        private Color _activeNotchColor;
        private Color _activeShadowColor;

        private DispatcherTimer _successTimer;

        private static readonly string _ownProcessExe =
            System.IO.Path.GetFileName(System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "");

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

            // Windows reports clipboard changes as a window message, so the watcher needs a real HWND.
            // This window has one and outlives everything else, so it hosts the listener.
            SourceInitialized += IslandWindow_SourceInitialized;

            // Outside-click dismissal. Deactivated covers clicking another window or the desktop; the
            // foreground hook is the backstop for focus changes that never deactivate this window.
            Deactivated += (s, e) => CollapseIfClickedAway();

            if (ViewModel != null)
            {
                BuildSprings();
                RefreshPalette();
                ApplyTargets(animate: false);
            }
        }

        private void IslandWindow_SourceInitialized(object sender, EventArgs e)
        {
            var handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            App.Current.Services.GetRequiredService<Services.SystemEvents.ClipboardEventService>().Attach(handle);

            App.Current.Services.GetRequiredService<Services.SystemEvents.ForegroundWindowService>()
                .ForegroundAppChanged += ForegroundWindow_ForegroundAppChanged;
        }

        private void ForegroundWindow_ForegroundAppChanged(object sender, string exeName)
        {
            // Our own window coming to the front is not clicking away from it.
            if (string.IsNullOrEmpty(exeName)) return;
            if (exeName.Equals(_ownProcessExe, StringComparison.OrdinalIgnoreCase)) return;

            Dispatcher.Invoke(CollapseIfClickedAway);
        }

        /// <summary>
        /// Dismisses the expanded notch when focus moves elsewhere, but only if the user opted into
        /// outside-click dismissal. Otherwise the inside click is what closes it.
        /// </summary>
        private void CollapseIfClickedAway()
        {
            if (ViewModel == null) return;
            if (!ViewModel.CollapseOnOutsideClick) return;
            if (ViewModel.CurrentState != IslandState.Expanded) return;

            ViewModel.CollapseCommand.Execute(null);
        }

        private void BuildSprings()
        {
            _widthSpring = new SpringDouble(v => { NotchBorder.Width = Math.Max(0, v); UpdateContentClip(); });
            _heightSpring = new SpringDouble(v => { NotchBorder.Height = Math.Max(0, v); UpdateContentClip(); });
            _cornerSpring = new SpringDouble(v => { AnimatableCornerRadius = Math.Max(0, v); UpdateContentClip(); });
            _filletSpring = new SpringDouble(v => AnimatableFilletRadius = Math.Max(0, v));
            _contentSpring = new SpringDouble(ApplyContentProgress);
            _paletteSpring = new SpringDouble(ApplyPaletteMix);

            _springs.Add(_widthSpring);
            _springs.Add(_heightSpring);
            _springs.Add(_cornerSpring);
            _springs.Add(_filletSpring);
            _springs.Add(_contentSpring);
            _springs.Add(_paletteSpring);
        }

        /// <summary>
        /// Cross-fades the idle and expanded layers instead of switching Visibility on frame 0, and grows the
        /// expanded content slightly as it arrives so it reads as a surface materializing rather than a pop.
        /// </summary>
        private void ApplyContentProgress(double p)
        {
            double clamped = Math.Min(1.0, Math.Max(0.0, p));

            ExpandedContentGrid.Opacity = clamped;
            IdleContentGrid.Opacity = 1.0 - clamped;

            ExpandedContentScale.ScaleX = 0.94 + 0.06 * clamped;
            ExpandedContentScale.ScaleY = 0.94 + 0.06 * clamped;

            // Collapse fully faded layers so they stop costing layout and stop taking hits.
            ExpandedContentGrid.Visibility = clamped > 0.005 ? Visibility.Visible : Visibility.Collapsed;
            IdleContentGrid.Visibility = clamped < 0.995 ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Rebuilds the content clip to match the notch's current size and bottom corner radius.
        ///
        /// A Border with a CornerRadius does not clip its child to that radius, and ClipToBounds only
        /// clips to the square layout rect, so content sitting near a bottom corner draws over the curve.
        /// Only the bottom corners are rounded: the top edge meets the screen edge and stays square.
        /// </summary>
        private void UpdateContentClip()
        {
            if (NotchContentHost == null) return;

            double width = NotchBorder.Width;
            double height = NotchBorder.Height;

            if (double.IsNaN(width) || double.IsNaN(height) || width <= 0 || height <= 0)
            {
                NotchContentHost.Clip = null;
                return;
            }

            double radius = Math.Max(0, Math.Min(AnimatableCornerRadius, Math.Min(width / 2, height / 2)));

            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                bool floating = ViewModel?.IsFloating == true;
                double topRadius = floating ? radius : 0;

                ctx.BeginFigure(new Point(topRadius, 0), isFilled: true, isClosed: true);
                ctx.LineTo(new Point(width - topRadius, 0), false, false);

                if (topRadius > 0)
                {
                    ctx.ArcTo(new Point(width, topRadius), new Size(topRadius, topRadius),
                              0, false, SweepDirection.Clockwise, false, false);
                }

                ctx.LineTo(new Point(width, height - radius), false, false);

                if (radius > 0)
                {
                    ctx.ArcTo(new Point(width - radius, height), new Size(radius, radius),
                              0, false, SweepDirection.Clockwise, false, false);
                }

                ctx.LineTo(new Point(radius, height), false, false);

                if (radius > 0)
                {
                    ctx.ArcTo(new Point(0, height - radius), new Size(radius, radius),
                              0, false, SweepDirection.Clockwise, false, false);
                }

                ctx.LineTo(new Point(0, topRadius), false, false);

                if (topRadius > 0)
                {
                    ctx.ArcTo(new Point(topRadius, 0), new Size(topRadius, topRadius),
                              0, false, SweepDirection.Clockwise, false, false);
                }
            }

            geometry.Freeze();
            NotchContentHost.Clip = geometry;
        }

        private void ApplyPaletteMix(double m)
        {
            double clamped = Math.Min(1.0, Math.Max(0.0, m));
            NotchBackground.Color = Lerp(_idleNotchColor, _activeNotchColor, clamped);
            NotchShadow.Color = Lerp(_idleShadowColor, _activeShadowColor, clamped);
        }

        private static Color Lerp(Color a, Color b, double t)
        {
            return Color.FromArgb(
                (byte)Math.Round(a.A + (b.A - a.A) * t),
                (byte)Math.Round(a.R + (b.R - a.R) * t),
                (byte)Math.Round(a.G + (b.G - a.G) * t),
                (byte)Math.Round(a.B + (b.B - a.B) * t));
        }

        private static Color ParseColor(string value, Color fallback)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return (Color)ColorConverter.ConvertFromString(value);
            }
            catch { }
            return fallback;
        }

        private void RefreshPalette()
        {
            _idleNotchColor = ParseColor(ViewModel.NotchColor, Colors.Black);
            _idleShadowColor = ParseColor(ViewModel.ShadowColor, Colors.Black);
            _activeNotchColor = ParseColor(ViewModel.HoverNotchColor, _idleNotchColor);
            _activeShadowColor = ParseColor(ViewModel.HoverShadowColor, _idleShadowColor);
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_widthSpring == null) return;

            switch (e.PropertyName)
            {
                case nameof(IslandViewModel.CurrentState):
                    ApplyTargets();
                    break;

                case nameof(IslandViewModel.NotchColor):
                case nameof(IslandViewModel.ShadowColor):
                case nameof(IslandViewModel.HoverNotchColor):
                case nameof(IslandViewModel.HoverShadowColor):
                    RefreshPalette();
                    // Re-run the mix so an edited colour shows up even while the palette spring is at rest.
                    ApplyPaletteMix(ViewModel.CurrentState == IslandState.Idle ? 0.0 : 1.0);
                    ApplyTargets();
                    break;

                case nameof(IslandViewModel.IdleWidth):
                case nameof(IslandViewModel.IdleHeight):
                case nameof(IslandViewModel.HoverWidth):
                case nameof(IslandViewModel.HoverHeight):
                case nameof(IslandViewModel.ExpandedWidth):
                case nameof(IslandViewModel.ExpandedHeight):
                case nameof(IslandViewModel.CornerRadius):
                case nameof(IslandViewModel.HoverCornerRadius):
                case nameof(IslandViewModel.ExpandedCornerRadius):
                case nameof(IslandViewModel.FilletRadius):
                case nameof(IslandViewModel.ExpandedFilletRadius):
                case nameof(IslandViewModel.TopOffset):
                    // Detaching from the edge changes which corners are round, so redraw both the border
                    // radius and the content clip.
                    ApplyCornerRadius(AnimatableCornerRadius);
                    UpdateContentClip();
                    ApplyTargets();
                    break;

                case nameof(IslandViewModel.AnimationSpeedMs):
                    // A Control Center slider is just a new target. The springs keep the motion continuous,
                    // so there is no animation to clear and no jump to hide.
                    ApplyTargets();
                    break;
            }
        }

        /// <summary>
        /// Damping and response per state, following Apple's guidance: critically damped by default, and
        /// overshoot reserved for the states that carry physical momentum. Geometry (radii) never overshoots,
        /// because a bouncing corner reads as an artifact rather than as physics.
        /// </summary>
        private void ApplyTargets(bool animate = true)
        {
            if (ViewModel == null || _widthSpring == null) return;

            var state = ViewModel.CurrentState;

            double targetWidth = state switch
            {
                IslandState.Idle => ViewModel.IdleWidth,
                IslandState.Hover => ViewModel.HoverWidth,
                IslandState.Expanded => ViewModel.ExpandedWidth,
                IslandState.DropTarget => ViewModel.IdleWidth + 40,
                IslandState.Success => ViewModel.IdleWidth + 60,
                IslandState.Triggered => ViewModel.IdleWidth + 120,
                _ => ViewModel.IdleWidth
            };

            double targetHeight = state switch
            {
                IslandState.Idle => ViewModel.IdleHeight,
                IslandState.Hover => ViewModel.HoverHeight,
                IslandState.Expanded => ViewModel.ExpandedHeight,
                IslandState.DropTarget => ViewModel.IdleHeight + 20,
                IslandState.Success => ViewModel.IdleHeight + 30,
                IslandState.Triggered => ViewModel.IdleHeight + 10,
                _ => ViewModel.IdleHeight
            };

            double targetCorner = state switch
            {
                IslandState.Hover => ViewModel.HoverCornerRadius,
                IslandState.Expanded => ViewModel.ExpandedCornerRadius,
                _ => ViewModel.CornerRadius
            };

            double targetFillet = state == IslandState.Expanded
                ? ViewModel.ExpandedFilletRadius
                : ViewModel.FilletRadius;

            // AnimationSpeedMs stays the user-facing dial. 500ms, the shipped default, lands on a 0.35s
            // response, inside the range Apple ships for sheets and repositions.
            double baseResponse = Math.Max(0.05, ViewModel.AnimationSpeedMs / 1000.0 * 0.7);

            (double damping, double factor) = state switch
            {
                // Small, frequent, no momentum: snap in, never overshoot.
                IslandState.Hover => (1.0, 0.65),
                // Collapsing is a return to rest. Overshoot on the way home reads as a wobble.
                IslandState.Idle => (1.0, 0.85),
                // The drawer case. Apple ships damping 0.8 here.
                IslandState.Expanded => (0.8, 1.0),
                IslandState.DropTarget => (0.8, 0.7),
                IslandState.Success => (0.75, 0.85),
                IslandState.Triggered => (0.8, 0.85),
                _ => (1.0, 1.0)
            };

            double response = baseResponse * factor;

            _widthSpring.Retarget(targetWidth, response, damping);
            _heightSpring.Retarget(targetHeight, response, damping);
            _cornerSpring.Retarget(targetCorner, response, 1.0);
            _filletSpring.Retarget(targetFillet, response, 1.0);

            // Content leads slightly on the way in so the intermediate frames point at the outcome.
            _contentSpring.Retarget(state == IslandState.Expanded ? 1.0 : 0.0, response * 0.8, 1.0);
            _paletteSpring.Retarget(state == IslandState.Idle ? 0.0 : 1.0, response, 1.0);

            if (animate)
            {
                _springs.Start();
            }
            else
            {
                _widthSpring.SnapTo(targetWidth);
                _heightSpring.SnapTo(targetHeight);
                _cornerSpring.SnapTo(targetCorner);
                _filletSpring.SnapTo(targetFillet);
                _contentSpring.SnapTo(state == IslandState.Expanded ? 1.0 : 0.0);
                _paletteSpring.SnapTo(state == IslandState.Idle ? 0.0 : 1.0);
            }

            ScheduleSuccessReturn(state);
        }

        private void ScheduleSuccessReturn(IslandState state)
        {
            if (state != IslandState.Success)
            {
                _successTimer?.Stop();
                return;
            }

            _successTimer ??= new DispatcherTimer();
            _successTimer.Stop();
            _successTimer.Interval = TimeSpan.FromMilliseconds(ViewModel.AnimationSpeedMs);
            _successTimer.Tick -= SuccessTimer_Tick;
            _successTimer.Tick += SuccessTimer_Tick;
            _successTimer.Start();
        }

        private void SuccessTimer_Tick(object sender, EventArgs e)
        {
            _successTimer.Stop();
            if (ViewModel.CurrentState == IslandState.Success)
                ViewModel.CurrentState = IslandState.Idle;
        }

        private void NotchBorder_MouseEnter(object sender, MouseEventArgs e) => ViewModel.HoverCommand.Execute(null);
        private void NotchBorder_MouseLeave(object sender, MouseEventArgs e) => ViewModel.UnhoverCommand.Execute(null);
        private void NotchBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.CurrentState != IslandState.Expanded)
            {
                ViewModel.ExpandCommand.Execute(null);
                return;
            }

            // Already expanded. With outside-click dismissal on, a click in here belongs to whatever the
            // user is aiming at, not to closing the notch out from under them.
            if (!ViewModel.CollapseOnOutsideClick) ViewModel.CollapseCommand.Execute(null);
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
