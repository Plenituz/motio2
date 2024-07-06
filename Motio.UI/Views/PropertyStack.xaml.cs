using Motio.UI.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Motio.UI.Utils;

namespace Motio.UI.Views
{
    /// <summary>
    /// Interaction logic for PropertyStack.xaml
    /// </summary>
    public partial class PropertyStack : UserControl
    {
        bool init = false;

        public PropertyStack()
        {
            Loaded += PropertyStack_Loaded;
            InitializeComponent();
        }

        private void PropertyStack_Loaded(object sender, RoutedEventArgs e)
        {
            Refresh();
            if(!init)
            {
                init = true;
                this.FindMainViewModel().PropertyPanel.RefreshGraphics += Refresh;
            }
        }

        private void Refresh()
        {
            NodeViewModel node = DataContext as NodeViewModel;
            if (node == null)
                return;
            CollectionViewSource view = new CollectionViewSource();
            view.Source = node.Properties;
            view.Filter += View_Filter;
            Binding binding = new Binding()
            {
                Source = view
            };
            PropsList.SetBinding(ItemsControl.ItemsSourceProperty, binding);
        }

        private void View_Filter(object sender, FilterEventArgs e)
        {
            NodePropertyBaseViewModel prop = (NodePropertyBaseViewModel)e.Item;
            e.Accepted = prop.AttachedNodes.Count != 0;
            //e.Accepted = ((NodePropertyBaseViewModel)e.Item).Name.Contains("e");
        }
    }
}
