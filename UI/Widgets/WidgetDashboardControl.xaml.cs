using Project_Nook.Widgets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Project_Nook.UI.Widgets
{
    public partial class WidgetDashboardControl : UserControl
    {
        public WidgetDashboardControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// The rail is horizontal but a mouse wheel is vertical, so translate one to the other.
        /// Same handling the Shelf uses for its own horizontal strip.
        ///
        /// This is a preview handler, which tunnels from the root down, so it runs before any widget's own
        /// wheel handler. A widget that wants the wheel for itself (the volume tile) is skipped here, or it
        /// would never see the event at all.
        /// </summary>
        private void TileRailScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is not ScrollViewer scrollViewer) return;
            if (WantsWheel(e.OriginalSource as DependencyObject, scrollViewer)) return;

            if (e.Delta > 0) scrollViewer.LineLeft();
            else scrollViewer.LineRight();

            e.Handled = true;
        }

        /// <summary>
        /// Walks up from whatever was under the pointer to the rail, looking for a widget that consumes
        /// the wheel itself.
        /// </summary>
        private static bool WantsWheel(DependencyObject source, DependencyObject stopAt)
        {
            while (source != null && source != stopAt)
            {
                if (source is IWheelInput) return true;

                source = source is Visual or System.Windows.Media.Media3D.Visual3D
                    ? VisualTreeHelper.GetParent(source)
                    : LogicalTreeHelper.GetParent(source);
            }

            return false;
        }
    }
}
