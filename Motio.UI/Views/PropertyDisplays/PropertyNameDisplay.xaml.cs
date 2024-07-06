using Motio.UI.ViewModels;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Motio.UI.Views.PropertyDisplays
{
    /// <summary>
    /// Interaction logic for PropertyNameDisplay.xaml
    /// </summary>
    public partial class PropertyNameDisplay : UserControl
    {
        NodePropertyBaseViewModel property;

        public PropertyNameDisplay()
        {
            InitializeComponent();
        }

        private void PropertyNameDisplay_Loaded(object sender, RoutedEventArgs e)
        {
            if(property == null)
            {
                property = (NodePropertyBaseViewModel)DataContext;
            }
        }

        private void Error_Click(object sender, RoutedEventArgs e)
        {
            string errors = property.Errors.Aggregate((p, n) => p + "\n" + n);
            MessageBox.Show("error on this property:\n" + errors);
        }

        private void CopyPath_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(property.GetPath());
        }

        private void IsInTimeline_Click(object sender, RoutedEventArgs e)
        {
            //this is inverted, if the checkbox is unchecked it means it was checked before 
            if (isInTimeline.IsChecked.HasValue  && isInTimeline.IsChecked.Value)
            {
                //if it was checked before, then remove it from the properties in tl
                property.CreateAnimationNode();
                property.DisplayInTimeline();
            }
            else
            {
                //if it was unchecked before then add it to properties in tl
                property.HideInTimeline();
            }
        }
    }
}
