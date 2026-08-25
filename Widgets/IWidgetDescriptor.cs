using System.Windows;
using Wpf.Ui.Controls;

namespace Project_Nook.Widgets
{
    /// <summary>
    /// Where a widget lives in the expanded dashboard. Wide widgets sit on the left at their natural size;
    /// tiles are 64x64 squircles in the horizontal rail, matching the Shelf's visual language.
    /// </summary>
    public enum WidgetSize
    {
        Tile,
        Wide
    }

    /// <summary>
    /// Everything the dashboard and the Control Center need to know about a widget. Adding a widget means
    /// writing one of these plus a UserControl, then registering both in App.ConfigureServices.
    /// </summary>
    public interface IWidgetDescriptor
    {
        /// <summary>Stable key persisted in settings.json. Never change it once shipped.</summary>
        string Id { get; }

        string DisplayName { get; }

        /// <summary>One line shown under the name in the Control Center's Activity tab.</summary>
        string Description { get; }

        SymbolRegular Icon { get; }

        WidgetSize Size { get; }

        bool EnabledByDefault { get; }

        /// <summary>
        /// Builds the widget's view. Called once, lazily, the first time the dashboard needs it.
        /// The returned element's DataContext is what receives <see cref="IWidgetLifecycle"/> callbacks.
        /// </summary>
        FrameworkElement CreateView();
    }

    /// <summary>
    /// Implemented by a widget's view model when it holds a timer, a poll, or any other recurring cost.
    /// The dashboard calls these as the widget comes into and goes out of view, so a collapsed notch does
    /// not sit there burning battery. A widget that must stay live even while collapsed — the music widget,
    /// which drives the idle notch content — simply does not implement this.
    /// </summary>
    public interface IWidgetLifecycle
    {
        void OnActivated();
        void OnDeactivated();
    }

    /// <summary>
    /// Marks a widget view that uses the mouse wheel itself, like the volume tile.
    ///
    /// The dashboard rail converts wheel input into horizontal scrolling from a PreviewMouseWheel handler,
    /// and preview events tunnel from the root down. The rail is an ancestor of every widget, so without
    /// this marker it always sees the wheel first and marks it handled, and the tunnel never reaches the
    /// widget underneath the pointer.
    /// </summary>
    public interface IWheelInput
    {
    }

    /// <summary>Stable widget ids, referenced from code that needs to ask about a specific widget.</summary>
    public static class WidgetIds
    {
        public const string Music = "music";
        public const string Calendar = "calendar";
        public const string Volume = "volume";
        public const string Timer = "timer";
        public const string SystemStats = "systemstats";
        public const string Notes = "notes";
        public const string Clipboard = "clipboard";
        public const string Screenshot = "screenshot";
        public const string QuickLaunch = "quicklaunch";
    }
}
