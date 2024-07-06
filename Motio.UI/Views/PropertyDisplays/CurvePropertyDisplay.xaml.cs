using Motio.NodeImpl.NodePropertyTypes;
using Motio.UI.ViewModels;
using Motio.UI.Views.Dialogs;
using System.Windows;
using System.Windows.Controls;

namespace Motio.UI.Views.PropertyDisplays
{
    /// <summary>
    /// Interaction logic for CurvePropertyDisplay.xaml
    /// </summary>
    public partial class CurvePropertyDisplay : UserControl
    {
        CurveNodePropertyViewModel property;

        public CurvePropertyDisplay()
        {
            InitializeComponent();
        }

        private void CurvePropertyDisplay_Loaded(object sender, RoutedEventArgs e)
        {
            if(property == null)
            {
                property = (CurveNodePropertyViewModel)DataContext;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            CurveEditDialog dialog = new CurveEditDialog(property);
            dialog.Show();
        }
    }
}
