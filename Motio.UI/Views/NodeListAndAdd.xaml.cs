using IronPython.Runtime.Types;
using Motio.NodeCommon.ObjectStoringImpl;
using Motio.NodeCore;
using Motio.NodeCore.Interfaces;
using Motio.PythonRunning;
using Motio.UI.Utils;
using Motio.UI.ViewModels;
using Motio.UI.Views.OverEverything;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Motio.UI.Views
{

    /// <summary>
    /// Interaction logic for NodeListAndAdd.xaml
    /// </summary>
    public partial class NodeListAndAdd : UserControl
    {
        //caching the property to avoid casting it everytime
        IHasAttached property;

        //public NodeType Type
        //{
        //    get => (NodeType)GetValue(NodeTypeProperty);
        //    set => SetValue(NodeTypeProperty, value);
        //}
        //public static readonly DependencyProperty NodeTypeProperty =
        //    DependencyProperty.Register("Type", typeof(NodeType), typeof(NodeListAndAdd), null);

        public NodeListAndAdd()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (property == null)
                plusBtn.Visibility = Visibility.Collapsed;
        }

        private void NodeListAndAdd_LayoutUpdated(object sender, System.EventArgs e)
        {
            //manually set the visibility because if you bind to AttachedNodes.Count the binding
            //doesnt update if you add element to the collection
            if (property == null)
                property = DataContext as IHasAttached;
            
            Visibility visibility = property != null && AnyIEnumerable(property.AttachedMembers) ? Visibility.Visible : Visibility.Collapsed;
            nodeScroll.Visibility = visibility;
        }

        private void PlusBtn_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            //TODO optimise "window.getwindow" into a field
            Point position = e.GetPosition(Window.GetWindow(this));
            AddNodeView lis = new AddNodeView()
            {
                Parameter = NodeType.Property,
                X = position.X,
                Y = position.Y,
                onSelectedType = AddNodeOfType
            };
            //if(Type == NodeType.Property)
            //{
            Predicate<object> defaultFilter = lis.filteredList.DefaultFilter;
            lis.filteredList.Filter = o =>
            {
                //TODO this is shit code
                bool defVal = defaultFilter(o);
                IList<Type> allowedTypes = null;
                bool pyVal = true;
                if(o is ICreatableNode creatable)
                {
                    if (o is PythonCreatableNodeWrapper pycreatable)
                        allowedTypes = NodePropertyBase.GetAcceptedPropertyTypes(pycreatable.pythonNode.PythonType);
                    else
                        allowedTypes = NodePropertyBase.GetAcceptedPropertyTypes(creatable.TypeCreated);
                }
                if (allowedTypes != null)
                    pyVal = NodePropertyBase.ContainsTypeOrParentType(allowedTypes, ((NodePropertyBaseViewModel)property).StaticValue.GetType());

                return defVal && pyVal;
            };
            //}

            this.FindMainViewModel().CanvasOverEverything.Children.Add(lis);
        }

        private void AddNodeOfType(ICreatableNode type)
        {
            //switch (Type)
            //{
            //    case NodeType.Graphics:
            //        {
            //            //GraphicsNodeViewModel graphics = DataContext as GraphicsNodeViewModel;
            //            //if (graphics == null)
            //            //    throw new Exception("Trying to add a GraphicsAffectingNode on a non GraphicsNode DataContext");

            //            //graphics.CreateNode(type);
            //        }
            //        break;
            //    case NodeType.Property:
            //        {
            NodePropertyBaseViewModel property = DataContext as NodePropertyBaseViewModel;
            var created = property.CreateNode(type);
            if (created != null)
                this.FindMainViewModel().PropertyPanel.AddToPropertyPanel(created);
            //        }
            //        break;
            //    default:
            //        throw new Exception("invalid NodeType");
            //}
        }

        private bool AnyIEnumerable(IEnumerable enu)
        {
            IEnumerator e = enu.GetEnumerator();
            return e.MoveNext();
        }
    }
}
