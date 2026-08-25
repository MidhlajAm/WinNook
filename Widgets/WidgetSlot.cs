using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;
using Wpf.Ui.Controls;

namespace Project_Nook.Widgets
{
    /// <summary>
    /// One widget as the registry sees it: its descriptor, whether the user has it turned on, where it sits
    /// in the rail, and its view once something has asked for it.
    /// </summary>
    public partial class WidgetSlot : ObservableObject
    {
        public IWidgetDescriptor Descriptor { get; }

        [ObservableProperty]
        private bool _isEnabled;

        [ObservableProperty]
        private int _order;

        private FrameworkElement _view;

        public WidgetSlot(IWidgetDescriptor descriptor, bool isEnabled, int order)
        {
            Descriptor = descriptor;
            _isEnabled = isEnabled;
            _order = order;
        }

        /// <summary>Built on first access so a widget the user has never enabled costs nothing.</summary>
        public FrameworkElement View => _view ??= Descriptor.CreateView();

        /// <summary>True once <see cref="View"/> has actually been built.</summary>
        public bool HasView => _view != null;

        /// <summary>The view model behind the view, when it wants activation callbacks.</summary>
        public IWidgetLifecycle Lifecycle => _view?.DataContext as IWidgetLifecycle;

        // Passthroughs so the Control Center can bind straight to the slot.
        public string Id => Descriptor.Id;
        public string DisplayName => Descriptor.DisplayName;
        public string Description => Descriptor.Description;
        public SymbolRegular Icon => Descriptor.Icon;
        public WidgetSize Size => Descriptor.Size;
    }
}
