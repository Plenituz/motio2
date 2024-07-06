using PropertyChanged;
using System.Windows.Controls;

namespace Motio.UI.Views
{
    /// <summary>
    /// Interaction logic for ChangeLogView.xaml
    /// </summary>
    [AddINotifyPropertyChangedInterface]
    public partial class ChangeLogView : UserControl
    {
        public string[] ChangeLog { get; set; }

        public ChangeLogView(string[] changeLog)
        {
            this.ChangeLog = changeLog;
            DataContext = this;
            InitializeComponent();
        }
    }
}
