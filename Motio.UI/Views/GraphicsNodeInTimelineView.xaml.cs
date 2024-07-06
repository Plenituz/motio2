using System.Windows.Controls;
using System.Windows.Input;
using Motio.UI.ViewModels;
using Motio.UI.Utils;

namespace Motio.UI.Views
{
    /// <summary>
    /// Interaction logic for GraphicsNodeInTimelineView.xaml
    /// </summary>
    public partial class GraphicsNodeInTimelineView : UserControl
    {
        MainControlViewModel mainViewModel;

        public GraphicsNodeInTimelineView()
        {
            InitializeComponent();
        }

        private void GraphicsNodeInTimelineView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if(mainViewModel == null)
            {
                mainViewModel = this.FindMainViewModel();
            }
        }

        private void OpenNode_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            NodeViewModel node = (NodeViewModel)DataContext;
            mainViewModel.PropertyPanel.AddToPropertyPanel(node);
        }

        private void RemoveFromTimeline_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            mainViewModel.KeyframePanel.RemoveFromTimeline((NodeViewModel)DataContext);
        }

    }
}
