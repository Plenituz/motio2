using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Motio.UI.Views
{
    /// <summary>
    /// Interaction logic for SelectableItem.xaml
    /// </summary>
    public partial class SelectableItem : UserControl, INotifyPropertyChanged
    {
        public UIElement InsideContent { get; set; }
        public bool Selected { get; set; }
        public object Data { get; set; }
        public FilterableList parent;

        public event PropertyChangedEventHandler PropertyChanged;

        public SelectableItem(FilterableList parent,  object data)
        {
            this.parent = parent;
            this.Data = data;
            DataContext = this;
            InitializeComponent();
        }

        private void stack_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            parent.SelectedItem = this;
            parent.ValidateSelected();
        }
    }
}
