using Motio.ClickLogic;
using Motio.NodeCommon;
using Motio.UI.Utils;
using Motio.UI.ViewModels;
using Motio.UI.Views.OverEverything;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Motio.UI.Views.PropertyDisplays
{
    /// <summary>
    /// Interaction logic for IntPropertyDisplay.xaml
    /// </summary>
    public partial class IntPropertyDisplay : UserControl
    {
        IntNodePropertyViewModel property;
        ClickAndDragPropertyValue clickHandler;

        public IntPropertyDisplay()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (clickHandler == null)
            {
                clickHandler = new ClickAndDragPropertyValue(inValueText)
                {
                    sensitivity = property.Sensitivity,
                    GetValue = () => (int)property.GetCurrentInValue(),
                    SetValue = (v) => {
                        property.StaticValue = v;
                    }
                };
                clickHandler.handler.OnClick = InValueText_Click;
            }
        }

        private void UserControl_LayoutUpdated(object sender, EventArgs e)
        {
            if (property == null)
            {
                property = (IntNodePropertyViewModel)DataContext;
            }

            Visibility visibility = property.AttachedNodes.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            outValue.Visibility = visibility;
        }

        private void InValueText_Click(MouseButtonEventArgs e)
        {
            //get the absolute position of the element relative to the MainWindow
            //TODO check if Window.GetWindow works here
            Point position = inValueText.TransformToAncestor(Window.GetWindow(this)).Transform(new Point(0, 0));

            string defaultValue = ToolBox.ToStringInvariantCulture(property.GetCurrentInValue());
            ModifyValueView mod = new ModifyValueView()
            {
                DefaultValue = defaultValue,
                DefaultWidth = inValueText.ActualWidth,
                TrySetPropertyValue = property.SetPropertyValueFromUserInput,
                X = position.X,
                Y = position.Y,
            };
            this.FindMainViewModel().CanvasOverEverything.Children.Add(mod);
        }
    }
}
