using Motio.Debuging;
using Motio.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Motio.UI.Views.PropertyDisplays
{
    /// <summary>
    /// Interaction logic for ButtonPropertyDisplay.xaml
    /// </summary>
    public partial class ButtonPropertyDisplay : UserControl
    {
        public ButtonPropertyDisplay()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ((ButtonNodePropertyViewModel)DataContext).ClickFunc?.Invoke();
            }
            catch (Exception ex)
            {
                Logger.WriteLine("Error while running click method of button:\n" + ex);
            }
        }
    }
}
