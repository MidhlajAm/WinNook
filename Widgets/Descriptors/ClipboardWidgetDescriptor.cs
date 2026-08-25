using Project_Nook.UI.Widgets;
using System.Windows;
using Wpf.Ui.Controls;

namespace Project_Nook.Widgets.Descriptors
{
    public class ClipboardWidgetDescriptor : IWidgetDescriptor
    {
        public string Id => WidgetIds.Clipboard;
        public string DisplayName => "Clipboard";
        public string Description => "Keeps the last 25 things you copied, ready to paste back.";
        public SymbolRegular Icon => SymbolRegular.ClipboardPaste24;
        public WidgetSize Size => WidgetSize.Tile;
        public bool EnabledByDefault => false;

        public FrameworkElement CreateView() => new ClipboardWidgetControl();
    }
}
