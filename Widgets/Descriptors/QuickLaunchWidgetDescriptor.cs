using Project_Nook.UI.Widgets;
using System.Windows;
using Wpf.Ui.Controls;

namespace Project_Nook.Widgets.Descriptors
{
    public class QuickLaunchWidgetDescriptor : IWidgetDescriptor
    {
        public string Id => WidgetIds.QuickLaunch;
        public string DisplayName => "Quick Launch";
        public string Description => "Pin up to six apps and start them from the Notch.";
        public SymbolRegular Icon => SymbolRegular.Rocket24;
        public WidgetSize Size => WidgetSize.Tile;
        public bool EnabledByDefault => false;

        public FrameworkElement CreateView() => new QuickLaunchWidgetControl();
    }
}
