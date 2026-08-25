using Project_Nook.UI.Widgets;
using System.Windows;
using Wpf.Ui.Controls;

namespace Project_Nook.Widgets.Descriptors
{
    public class ScreenshotWidgetDescriptor : IWidgetDescriptor
    {
        public string Id => WidgetIds.Screenshot;
        public string DisplayName => "Screenshot";
        public string Description => "Capture the screen straight into the Shelf.";
        public SymbolRegular Icon => SymbolRegular.Camera24;
        public WidgetSize Size => WidgetSize.Tile;
        public bool EnabledByDefault => false;

        public FrameworkElement CreateView() => new ScreenshotWidgetControl();
    }
}
