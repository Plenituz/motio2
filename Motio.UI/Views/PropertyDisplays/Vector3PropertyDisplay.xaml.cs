using Motio.ClickLogic;
using Motio.Geometry;
using Motio.UI.Utils;
using Motio.UI.ViewModels;
using Motio.UI.Views.OverEverything;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Motio.UI.Views.PropertyDisplays
{
    /// <summary>
    /// Interaction logic for Vector3PropertyDisplay.xaml
    /// </summary>
    public partial class Vector3PropertyDisplay : UserControl
    {
        Vector3NodePropertyViewModel property;
        ClickAndDragPropertyValue clickX;
        ClickAndDragPropertyValue clickY;
        ClickAndDragPropertyValue clickZ;

        public Vector3PropertyDisplay()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (clickX == null || clickY == null || clickZ == null)
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
                    ((Vector3)property.GetCurrentInValue()).X.ToString(),
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
                        ((Vector3)property.GetCurrentInValue()).Y.ToString(),
                        ev);

                clickZ = new ClickAndDragPropertyValue(inValueZ)
                {
                    sensitivity = property.Sensitivity,
                    GetValue = () => property.Z,
                    SetValue = v =>
                    {
                        property.Z = v;
                    }
                };
                clickZ.handler.OnClick =
                    ev => InValueText_Click(
                        inValueZ,
                        property.SetZFromUserInput,
                        ((Vector3)property.GetCurrentInValue()).Z.ToString(),
                        ev);
            }
        }

        private void InValueText_Click(TextBlock inValue, Func<string, Exception> trySetValue, string defaultValue, MouseButtonEventArgs e)
        {
            //get the absolute position of the element relative to the MainWindow
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
                property = (Vector3NodePropertyViewModel)DataContext;
            }

            //manually set the visibility because if you bind to AttachedNodes.Count the binding
            //doesnt update if you add element to the collection
            Visibility visibility = property.AttachedNodes.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            outValue.Visibility = visibility;
        }
    }
}
