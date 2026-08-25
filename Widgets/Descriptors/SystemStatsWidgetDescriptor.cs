using Project_Nook.UI.Widgets;
using System.Windows;
using Wpf.Ui.Controls;

namespace Project_Nook.Widgets.Descriptors
{
    public class SystemStatsWidgetDescriptor : IWidgetDescriptor
    {
        public string Id => WidgetIds.SystemStats;
        public string DisplayName => "System Stats";
        public string Description => "Live CPU and memory load at a glance.";
        public SymbolRegular Icon => SymbolRegular.TopSpeed24;
        public WidgetSize Size => WidgetSize.Tile;
        public bool EnabledByDefault => false;

        public FrameworkElement CreateView() => new SystemStatsWidgetControl();
    }
}
