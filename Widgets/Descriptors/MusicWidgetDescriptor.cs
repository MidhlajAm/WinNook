using Project_Nook.UI.Widgets;
using System.Windows;
using Wpf.Ui.Controls;

namespace Project_Nook.Widgets.Descriptors
{
    public class MusicWidgetDescriptor : IWidgetDescriptor
    {
        public string Id => WidgetIds.Music;
        public string DisplayName => "Music";
        public string Description => "Show media playback and controls in the Notch.";
        public SymbolRegular Icon => SymbolRegular.MusicNote224;
        public WidgetSize Size => WidgetSize.Wide;
        public bool EnabledByDefault => true;

        public FrameworkElement CreateView() => new MusicWidgetControl();
    }
}
