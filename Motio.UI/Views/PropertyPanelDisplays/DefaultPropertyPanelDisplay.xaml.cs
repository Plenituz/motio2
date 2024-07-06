using Motio.UI.Utils;
using Motio.UI.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Motio.UI.Views.PropertyPanelDisplays
{
    /// <summary>
    /// Interaction logic for DefaultPropertyPanelDisplay.xaml
    /// </summary>
    public partial class DefaultPropertyPanelDisplay : UserControl
    {
        public DefaultPropertyPanelDisplay()
        {
            InitializeComponent();
        }

        //private void Button_Click(object sender, RoutedEventArgs e)
        //{
        //    NodeViewModel clickContext = ((Button)sender).DataContext as NodeViewModel;
        //    this.FindMainViewModel().PropertyPanel.AddToPropertyPanel(clickContext);
        //}

        private void ActDeact_Click(object sender, RoutedEventArgs e)
        {
            MenuItem clickContext = ((MenuItem)sender);
            //freshly made casting soup
            NodeViewModel node = ((FrameworkElement)((ContextMenu)clickContext.Parent).PlacementTarget).DataContext as NodeViewModel;

            node.Enabled = !node.Enabled;
        }

        private void CopyUUID_Click(object sender, RoutedEventArgs e)
        {
            MenuItem clickContext = ((MenuItem)sender);
            NodeViewModel node = ((FrameworkElement)((ContextMenu)clickContext.Parent).PlacementTarget).DataContext as NodeViewModel;

            Clipboard.SetText(node.UUID.ToString());
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            MenuItem clickContext = ((MenuItem)sender);
            NodeViewModel node = ((FrameworkElement)((ContextMenu)clickContext.Parent).PlacementTarget).DataContext as NodeViewModel;

            node.Original.Delete();
        }
    }
}
