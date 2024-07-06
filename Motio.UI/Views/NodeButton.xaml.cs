using Motio.UI.Utils;
using Motio.UI.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Motio.UI.Views
{
    /// <summary>
    /// Interaction logic for NodeButton.xaml
    /// </summary>
    public partial class NodeButton : UserControl
    {
        //public event RoutedEventHandler OnClick;
        //public event MouseButtonEventHandler OnDoubleClick;

        public NodeButton()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            NodeViewModel clickContext = ((Button)sender).DataContext as NodeViewModel;
            this.FindMainViewModel().PropertyPanel.AddToPropertyPanel(clickContext);
            //OnClick?.Invoke(sender, e);
        }

        /// <summary>
        /// when double clinking on a button, the datacontext should be the Node object 
        /// the user clicked on so we can just get it and relay it to the MainWindowViewModel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonNodeName_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //OnDoubleClick?.Invoke(sender, e);
            //NodeViewModel clickContext = ((Button)sender).DataContext as NodeViewModel;
            //this.FindMainViewModel().PropertyPanel.AddToPropertyPanel(clickContext);
            //MainWindowViewModel.Instance.AddToPropertyPanel(clickContext);
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
