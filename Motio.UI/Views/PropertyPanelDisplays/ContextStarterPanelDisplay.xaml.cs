using Motio.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Motio.UI.Views.PropertyPanelDisplays
{
    /// <summary>
    /// Interaction logic for ContextStarterPanelDisplay.xaml
    /// </summary>
    public partial class ContextStarterPanelDisplay : UserControl
    {
        public ContextStarterPanelDisplay()
        {
            InitializeComponent();
        }

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
