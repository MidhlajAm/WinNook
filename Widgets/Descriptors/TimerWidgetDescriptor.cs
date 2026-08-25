using Project_Nook.UI.Widgets;
using System.Windows;
using Wpf.Ui.Controls;

namespace Project_Nook.Widgets.Descriptors
{
    public class TimerWidgetDescriptor : IWidgetDescriptor
    {
        public string Id => WidgetIds.Timer;
        public string DisplayName => "Timer";
        public string Description => "A countdown and Pomodoro clock that keeps running while the Notch is closed.";
        public SymbolRegular Icon => SymbolRegular.Timer24;
        public WidgetSize Size => WidgetSize.Tile;
        public bool EnabledByDefault => true;

        public FrameworkElement CreateView() => new TimerWidgetControl();
    }
}
