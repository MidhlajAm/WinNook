using Project_Nook.UI.Widgets;
using System.Windows;
using Wpf.Ui.Controls;

namespace Project_Nook.Widgets.Descriptors
{
    public class CalendarWidgetDescriptor : IWidgetDescriptor
    {
        public string Id => WidgetIds.Calendar;
        public string DisplayName => "Calendar";
        public string Description => "Show today's date in the expanded Notch dashboard.";
        public SymbolRegular Icon => SymbolRegular.Calendar24;
        public WidgetSize Size => WidgetSize.Tile;
        public bool EnabledByDefault => true;

        public FrameworkElement CreateView() => new CalendarWidgetControl();
    }
}
