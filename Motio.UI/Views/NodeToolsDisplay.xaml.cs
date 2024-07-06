using Motio.UI.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Motio.UI.Utils;
using Motio.ClickLogic;

namespace Motio.UI.Views
{
    /// <summary>
    /// Interaction logic for NodeToolsDisplay.xaml
    /// </summary>
    public partial class NodeToolsDisplay : UserControl
    {
        PropertyPanelViewModel propertyPanel;

        public NodeToolsDisplay()
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
            
            if(tool.Selected)
            {
                propertyPanel.DeactivateActiveTool();
            }
            else
            {
                propertyPanel.SetActiveTool(tool);
            }
        }

        private void ContentPresenter_Unloaded(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)sender;
            if (element.Tag != null)
            {
                ClickAndDragHandler handler = (ClickAndDragHandler)element.Tag;
                handler.UnHook(element);
            }
        }

        private void ContentPresenter_Loaded(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)sender;
            if(element.Tag == null)
            {
                ClickAndDragHandler handler = new ClickAndDragHandler(element)
                {
                    OnClickWithSender = ToolButton_Click
                };
                element.Tag = handler;
            }
        }
    }
}
