using PropertyChanged;
using System.Windows.Controls;

namespace Motio.UI.Views
{
    /// <summary>
    /// Interaction logic for ExceptionStackView.xaml
    /// </summary>
    [AddINotifyPropertyChangedInterface]
    public partial class ExceptionStackView : UserControl
    {
        public string Text { get; set; }

        public ExceptionStackView()
        {
            DataContext = this;
            InitializeComponent();
        }
    }
}
