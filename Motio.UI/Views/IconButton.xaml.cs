using Motio.ClickLogic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Motio.UI.Views
{
    /// <summary>
    /// Interaction logic for IconButton.xaml
    /// </summary>
    public partial class IconButton : UserControl
    {
        public event MouseEventHandler Click;
        ClickAndDragHandler handler;

        public string PathData
        {
            get => GetValue(PathDataProperty) as string;
            set => SetValue(PathDataProperty, value);
        }
        public static readonly DependencyProperty PathDataProperty =
            DependencyProperty.Register(nameof(PathData), typeof(string), typeof(IconButton), null);

        public Thickness PathMargin
        {
            get => (Thickness)GetValue(PathMarginProperty);
            set => SetValue(PathMarginProperty, value);
        }
        public static readonly DependencyProperty PathMarginProperty =
            DependencyProperty.Register(nameof(PathMargin), typeof(Thickness), typeof(IconButton), null);


        public Brush IconBackground
        {
            get => GetValue(IconBackgroundProperty) as Brush;
            set => SetValue(IconBackgroundProperty, value);
        }
        public static readonly DependencyProperty IconBackgroundProperty =
            DependencyProperty.Register(nameof(IconBackground), typeof(Brush), typeof(IconButton), null);

        public IconButton()
        {
            DataContext = this;
            PathMargin = new Thickness(3);
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if(handler == null)
            {
                handler = new ClickAndDragHandler(HighlightEllipse)
                {
                    OnClick = (args) => Click?.Invoke(this, args)
                };
            }
        }
    }
}
