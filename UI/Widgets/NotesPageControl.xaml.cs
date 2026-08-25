using Microsoft.Extensions.DependencyInjection;
using Project_Nook.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace Project_Nook.UI.Widgets
{
    public partial class NotesPageControl : UserControl
    {
        public NotesWidgetViewModel ViewModel { get; }

        public NotesPageControl()
        {
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject()))
            {
                ViewModel = ((App)System.Windows.Application.Current).Services.GetRequiredService<NotesWidgetViewModel>();
            }
            DataContext = ViewModel;
            InitializeComponent();
        }

        private void BackButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;   // stop the notch seeing this and collapsing
            ViewModel?.CloseCommand.Execute(null);
        }

        /// <summary>
        /// A plain TextBox does not mark its own mouse-down handled, so a click meant to place the caret
        /// would bubble to the notch and collapse the whole thing mid-sentence. Take focus explicitly
        /// and stop it here.
        /// </summary>
        private void NoteEditor_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not TextBox box) return;

            if (!box.IsKeyboardFocusWithin)
            {
                box.Focus();
                e.Handled = true;
            }
        }
    }
}
