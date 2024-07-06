using Motio.NodeCore;
using Motio.UI.Utils;
using Motio.UI.ViewModels;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Motio.UI.Views.PropertyDisplays
{
    /// <summary>
    /// Interaction logic for DeletablePropertyDisplay.xaml
    /// </summary>
    public partial class DeletablePropertyDisplay : UserControl
    {
        DeletablePropertyWrapperViewModel property;

        public DeletablePropertyDisplay()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if(property == null)
            {
                property = (DeletablePropertyWrapperViewModel)DataContext;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            NodeViewModel host = (NodeViewModel)property.Host;

            property.Properties.originalList.Clear();
            ControlExtensions.DeleteInProperties(host.Properties, property);
        }
    }
}
