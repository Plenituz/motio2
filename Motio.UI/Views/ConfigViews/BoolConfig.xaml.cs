namespace Motio.UI.Views.ConfigViews
{
    /// <summary>
    /// Interaction logic for BoolConfig.xaml
    /// </summary>
    public partial class BoolConfig : ConfigViewBase
    {
        public BoolConfig()
        {
            InitializeComponent();
        }

        public override object GetUserInputedValue()
        {
            return CheckMan.IsChecked;
        }
    }
}
