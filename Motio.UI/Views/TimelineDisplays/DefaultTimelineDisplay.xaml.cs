using Motio.UI.Utils;
using Motio.UI.ViewModels;
using System;
using System.Windows.Controls;
using System.Windows.Input;

namespace Motio.UI.Views.TimelineDisplays
{
    /// <summary>
    /// Interaction logic for DefaultTimelineDisplay.xaml
    /// </summary>
    public partial class DefaultTimelineDisplay : UserControl
    {
        long lastClickTime;
        int clickCount = 0;

        public DefaultTimelineDisplay()
        {
            InitializeComponent();
            LayoutUpdated += DefaultTimelineDisplay_LayoutUpdated;
            Loaded += DefaultTimelineDisplay_Loaded;
        }

        private void DefaultTimelineDisplay_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            var t = DataContext;

        }

        private void DefaultTimelineDisplay_LayoutUpdated(object sender, EventArgs e)
        {
            var t = DataContext;
        }

        private void KeyframeCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Click();
        }

        private void Click()
        {
            if (DateTimeOffset.Now.UtcTicks - lastClickTime < 1000)
            {
                clickCount++;
            }
            else
            {
                clickCount = 1;
            }
            lastClickTime = DateTimeOffset.Now.UtcTicks;
            if (clickCount > 1)
            {
                DoubleClick();
                clickCount = 0;
            }
        }

        private void DoubleClick()
        {
            NodePropertyBaseViewModel property = (NodePropertyBaseViewModel)DataContext;
            NodeViewModel node = ProxyStatic.GetProxyOf<NodeViewModel>(property.Original.nodeHost);
            node.FindPropertyPanel().AddToPropertyPanel(node);
        }
    }
}
