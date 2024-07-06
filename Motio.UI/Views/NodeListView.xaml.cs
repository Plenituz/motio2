using Motio.UI.Utils;
using Motio.UI.ViewModels;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace Motio.UI.Views
{
    /// <summary>
    /// Interaction logic for NodeListView.xaml
    /// </summary>
    public partial class NodeListView : UserControl
    {
        PropertyPanelViewModel propertyPanel;

        public NodeListView()
        {
            InitializeComponent();
        }

        private void NodeListView_Loaded(object sender, RoutedEventArgs e)
        {
            if(propertyPanel == null)
                propertyPanel = this.FindMainViewModel().PropertyPanel;

            //check if there are attached nodes
            PropertyInfo info = DataContext.GetType().GetProperty("AttachedNodes");
            if (info == null)
                Visibility = Visibility.Collapsed;
        }
    }
}
