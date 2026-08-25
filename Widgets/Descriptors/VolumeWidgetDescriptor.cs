using Project_Nook.UI.Widgets;
using System.Windows;
using Wpf.Ui.Controls;

namespace Project_Nook.Widgets.Descriptors
{
    public class VolumeWidgetDescriptor : IWidgetDescriptor
    {
        public string Id => WidgetIds.Volume;
        public string DisplayName => "Volume";
        public string Description => "Mute with a click, scroll the tile to change the level.";
        public SymbolRegular Icon => SymbolRegular.Speaker224;
        public WidgetSize Size => WidgetSize.Tile;
        public bool EnabledByDefault => true;

        public FrameworkElement CreateView() => new VolumeWidgetControl();
    }
}
