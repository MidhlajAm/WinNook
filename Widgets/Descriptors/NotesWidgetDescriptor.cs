using Project_Nook.UI.Widgets;
using System.Windows;
using Wpf.Ui.Controls;

namespace Project_Nook.Widgets.Descriptors
{
    public class NotesWidgetDescriptor : IWidgetDescriptor
    {
        public string Id => WidgetIds.Notes;
        public string DisplayName => "Notes";
        public string Description => "A scratchpad in the Notch that saves itself as you type.";
        public SymbolRegular Icon => SymbolRegular.Note24;
        public WidgetSize Size => WidgetSize.Tile;
        public bool EnabledByDefault => false;

        public FrameworkElement CreateView() => new NotesWidgetControl();
    }
}
