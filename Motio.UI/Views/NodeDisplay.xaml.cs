using Motio.UI.ViewModels;
using System.Windows;
using System.Windows.Controls;
using Motio.UI.Utils;
using Motio.ClickLogic;
using System.Windows.Input;
using Motio.UI.Views.OverEverything;
using System;
using System.Collections.Specialized;
using System.Windows.Data;

namespace Motio.UI.Views
{
    /// <summary>
    /// Interaction logic for NodeDisplay.xaml
    /// </summary>
    public partial class NodeDisplay : UserControl
    {
        NodeViewModel Node => DataContext as NodeViewModel;
        bool init = false;

        public NodeDisplay()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if(!init)
            {
                init = true;
                //node = (NodeViewModel)DataContext;
                ClickAndDragHandler handler = new ClickAndDragHandler(NameDisplay);
                handler.OnClick = NameDisplay_Click;

                //weak listener so the control gets garbage collected without the need to unsub
                CollectionChangedEventManager.AddHandler(
                    this.FindMainViewModel().PropertyPanel.DisplayedLockedNodes, DisplayedLockedNodes_CollectionChanged);
            }
        }

        private void DisplayedLockedNodes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            //only for simple bindings:
            LockButton.GetBindingExpression(IconButton.PathDataProperty).UpdateTarget();
            //multibinding friendly:
            BindingOperations.GetBindingExpressionBase(LockButton, IconButton.IconBackgroundProperty).UpdateTarget();
        }

        private void NameDisplay_Click(MouseButtonEventArgs args)
        {
            Point position = NameDisplay.TransformToAncestor(Window.GetWindow(this)).Transform(new Point(0, 0));

            ModifyValueView mod = new ModifyValueView()
            {
                DefaultValue = Node.UserGivenName,
                TrySetPropertyValue = name =>
                {
                    if (name.Length <= 2)
                        return new Exception("name is too short");
                    Node.UserGivenName = name;
                    return null;
                },
                X = position.X,
                Y = position.Y,
            };
            Node.FindMainViewModel().CanvasOverEverything.Children.Add(mod);
        }

        private void AddToTimeline_Click(object sender, RoutedEventArgs e)
        {
            Node.FindKeyframePanel().AddToTimeline(Node);
        }

        private void RemoveFromPropertyPanel_Click(object sender, RoutedEventArgs e)
        {
            Node.FindPropertyPanel().RemoveFromPropertyPanel(Node);
        }

        private void LockButton_Click(object sender, MouseEventArgs e)
        {
            int indexLock = Node.PropertyPanel.DisplayedLockedNodes.IndexOf(Node);
            if (indexLock == -1)
                Node.PropertyPanel.DisplayedLockedNodes.Add(Node);
            else
                Node.PropertyPanel.DisplayedLockedNodes.RemoveAt(indexLock);
        }
    }
}
