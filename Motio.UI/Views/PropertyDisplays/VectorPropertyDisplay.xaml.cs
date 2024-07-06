using Motio.ClickLogic;
using Motio.Geometry;
using Motio.UI.ViewModels;
using Motio.UI.Views.OverEverything;
using Motio.UI.Utils;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Motio.UI.Views.PropertyDisplays
{
    /// <summary>
    /// Interaction logic for VectorPropertyDisplay.xaml
    /// </summary>
    public partial class VectorPropertyDisplay : UserControl
    {
        VectorNodePropertyViewModel property;
        ClickAndDragPropertyValue clickX;
        ClickAndDragPropertyValue clickY;

        public VectorPropertyDisplay()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if(clickX == null || clickY == null)
            {
                clickX = new ClickAndDragPropertyValue(inValueX)
                {
                    sensitivity = property.Sensitivity,
                    GetValue = () => property.X,
                    SetValue = v =>
                    {
                        //using .X and not .StaticValue allow the keyframeholder to differenciate 
                        //between modifying X and modifying Y
                        property.X = v;
                    }
                };
                clickX.handler.OnClick = 
                    ev => InValueText_Click(
                        inValueX, 
                    property.SetXFromUserInput, 
                    ((Vector2)property.GetCurrentInValue()).X.ToString(), 
                    ev);
                clickY = new ClickAndDragPropertyValue(inValueY)
                {
                    sensitivity = property.Sensitivity,
                    GetValue = () => property.Y,
                    SetValue = v =>
                    {
                        property.Y = v;
                    }
                };
                clickY.handler.OnClick =
                    ev => InValueText_Click(
                        inValueY,
                        property.SetYFromUserInput,
                        ((Vector2)property.GetCurrentInValue()).Y.ToString(),
                        ev);
            }
        }

        private void InValueText_Click(TextBlock inValue, Func<string, Exception> trySetValue, string defaultValue, MouseButtonEventArgs e)
        {
            //get the absolute position of the element relative to the MainWindow
            //TODO check if Window.GetWindow works here
            System.Windows.Point position = inValue.TransformToAncestor(Window.GetWindow(this)).Transform(new System.Windows.Point(0, 0));
            ModifyValueView mod = new ModifyValueView()
            {
                DefaultValue = defaultValue,
                DefaultWidth = inValue.ActualWidth,
                TrySetPropertyValue = trySetValue,
                X = position.X,
                Y = position.Y,
            };
            this.FindMainViewModel().CanvasOverEverything.Children.Add(mod);
        }

        private void UserControl_LayoutUpdated(object sender, System.EventArgs e)
        {
            if (property == null)
            {
                property = (VectorNodePropertyViewModel)DataContext;
            }

            //manually set the visibility because if you bind to AttachedNodes.Count the binding
            //doesnt update if you add element to the collection
            Visibility visibility = property.AttachedNodes.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            outValue.Visibility = visibility;
        }
    }
}
