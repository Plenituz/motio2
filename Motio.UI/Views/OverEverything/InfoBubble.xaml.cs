using System.Windows;

namespace Motio.UI.Views.OverEverything
{
    /// <summary>
    /// Interaction logic for InfoBubble.xaml
    /// </summary>
    public partial class InfoBubble : BaseOverEverything
    {
        public string Text { get; set; }
        public bool conpensateHeight = false;

        public InfoBubble()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void InfoBubble_Loaded(object sender, RoutedEventArgs e)
        {
            if (conpensateHeight)
                Y -= ActualHeight;
        }
    }
}
