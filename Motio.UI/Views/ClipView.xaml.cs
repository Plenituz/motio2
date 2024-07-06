using Motio.UI.Utils;
using Motio.UI.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Motio.UI.Views
{
    /// <summary>
    /// Logique d'interaction pour ClipView.xaml
    /// </summary>
    public partial class ClipView : UserControl
    {
        public ClipView()
        {
            InitializeComponent();
        }

        private void ListBox_Selected(object sender, RoutedEventArgs e)
        {
            NodeViewModel node = (NodeViewModel)((FrameworkElement)sender).DataContext;
            node.FindPropertyPanel().AddToPropertyPanel(node);
        }
    }
}
