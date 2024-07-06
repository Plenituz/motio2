using PropertyChanged;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Motio.UI.Views.OverEverything
{
    /// <summary>
    /// this is the base class for controls that want to be disabled in the "overEverthingCanvas"
    /// to use this you have to put this as the base control in the xaml
    /// this handle the placement in the window, making sure the control is always within the 
    /// bounds of the MainWindow.Instance
    /// </summary>
    [AddINotifyPropertyChangedInterface]
    public class BaseOverEverything : UserControl
    {
        public double X { get; set; }
        public double Y { get; set; }

        Window _window;
        Window Window => _window ?? (_window = Window.GetWindow(this));

        public BaseOverEverything()
        {
            Binding bindingX = new Binding("X");
            this.SetBinding(Canvas.LeftProperty, bindingX);
            Binding bindingY = new Binding("Y");
            this.SetBinding(Canvas.TopProperty, bindingY);
            Loaded += BaseOverEverything_Loaded;
        }

        protected virtual void BaseOverEverything_Loaded(object sender, RoutedEventArgs e)
        {
            if (X + ActualWidth >= Window.ActualWidth - 20)
            {
                X = Window.ActualWidth - ActualWidth - 20;
            }
            if (Y + ActualHeight >= Window.ActualHeight)
            {
                //TODO ca marche pas ca
                Y = Window.ActualHeight - ActualHeight - 20;
            }
        }

        public virtual void Close()
        {
            if (VisualTreeHelper.GetParent(this) is Canvas parent)
                parent.Children.Remove(this);
        }
    }
}
