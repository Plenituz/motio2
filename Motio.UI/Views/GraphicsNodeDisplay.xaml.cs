using Motio.NodeCommon.ObjectStoringImpl;
using Motio.UI.Utils;
using Motio.UI.ViewModels;
using Motio.UI.Views.OverEverything;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Motio.UI.Views
{
    /// <summary>
    /// Interaction logic for GraphicsNodeDisplay.xaml
    /// </summary>
    public partial class GraphicsNodeDisplay : UserControl
    {
        public GraphicsNodeDisplay()
        {
            InitializeComponent();
        }

        public void PlusBtn_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            this.FindMainViewModel().PropertyPanel.TargetedAddButton = this;
            //Point position = e.GetPosition(Window.GetWindow(this));

            Point position = AddButton.TransformToAncestor(Window.GetWindow(this))
                          .Transform(new Point(0, 0));

            AddNodeView lis = new AddNodeView()
            {
                Parameter = NodeType.Graphics,
                X = position.X,
                Y = position.Y,
                onSelectedType = AddNodeOfType
            };
            this.FindMainViewModel().CanvasOverEverything.Children.Add(lis);
        }

        private void AddNodeOfType(ICreatableNode type)
        {
            GraphicsNodeViewModel graphics = DataContext as GraphicsNodeViewModel;
            if (graphics == null)
                throw new Exception("Trying to add a GraphicsAffectingNode on a non GraphicsNode DataContext");

            GraphicsAffectingNodeViewModel created = graphics.CreateNode(type);
            if (created != null)
            {
                this.FindMainViewModel().PropertyPanel.SetDisplayedNode(created);
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.FindMainViewModel().PropertyPanel.TargetedAddButton = this;
        }

        private void NameDisplay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point position = NameDisplay.TransformToAncestor(Window.GetWindow(this)).Transform(new Point(0, 0));
            NodeViewModel node = (NodeViewModel)DataContext;

            ModifyValueView mod = new ModifyValueView()
            {
                DefaultValue = node.UserGivenName,
                TrySetPropertyValue = name =>
                {
                    if (name.Length <= 2)
                        return new Exception("name is too short");
                    node.UserGivenName = name;
                    return null;
                },
                X = position.X,
                Y = position.Y,
            };
            node.FindMainViewModel().CanvasOverEverything.Children.Add(mod);
        }

        private void IconButton_Click(object sender, MouseEventArgs e)
        {
            GraphicsNodeViewModel node = (GraphicsNodeViewModel)DataContext;
            node.Visible = !node.Visible;
            node.UpdateModel();
        }
    }
}
