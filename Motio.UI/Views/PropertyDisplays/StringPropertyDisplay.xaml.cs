using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Motio.UI.ViewModels;
using Motio.UICommon;
using Motio.Debuging;

namespace Motio.UI.Views.PropertyDisplays
{
    /// <summary>
    /// Interaction logic for StringPropertyDisplay.xaml
    /// </summary>
    public partial class StringPropertyDisplay : UserControl
    {
        StringNodePropertyViewModel property;

        public StringPropertyDisplay()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if(property == null)
            {
                property = (StringNodePropertyViewModel)DataContext;
                property.PropertyChanged += Property_PropertyChanged;
            }
            exprText.Text = property.StringValue;
        }

        private void Property_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(e.PropertyName.Equals(nameof(StringNodePropertyViewModel.StaticValue)))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    exprText.Text = property.StringValue;
                });
            }
        }

        private void exprText_LostFocus(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            property.ValidateValue(exprText.Text);
            property.StaticValue = exprText.Text;
        }

        private void exprText_TextChanged(object sender, EventArgs e)
        {
            //only update when the user click out/ctrl+enter
            //property.StaticValue = exprText.Text;
        }

        private void exprText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && ToolBox.IsCtrlHeld())
            {
                try
                {
                    DependencyObject scope = FocusManager.GetFocusScope((DependencyObject)Keyboard.FocusedElement);
                    FocusManager.SetFocusedElement(scope, Window.GetWindow(this));
                }
                catch (Exception)
                {
                    Logger.WriteLine("error clearing focus in StringPropertyDisplay, " + property.Name);
                }
            }
        }
    }
}
