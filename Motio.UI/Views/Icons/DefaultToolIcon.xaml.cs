using Motio.UI.Utils;
using Motio.UI.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Motio.UI.Views.Icons
{
    /// <summary>
    /// Interaction logic for DefaultToolIcon.xaml
    /// </summary>
    public partial class DefaultToolIcon : UserControl
    {
        PropertyPanelViewModel propertyPanel;

        public DefaultToolIcon()
        {
            InitializeComponent();
        }

        private void NodeToolsDisplay_Loaded(object sender, RoutedEventArgs e)
        {
            if (propertyPanel == null)
            {
                propertyPanel = this.FindMainViewModel().PropertyPanel;
            }
        }

        private void ToolButton_Click(object sender, RoutedEventArgs e)
        {
            FrameworkElement button = (FrameworkElement)sender;
            NodeToolViewModel tool = (NodeToolViewModel)button.DataContext;

            if (tool.Selected)
            {
                propertyPanel.DeactivateActiveTool();
            }
            else
            {
                propertyPanel.SetActiveTool(tool);
            }
        }
    }
}
