namespace Motio.UI.Views.ConfigViews
{
    /// <summary>
    /// Interaction logic for EnumConfig.xaml
    /// </summary>
    public partial class EnumConfig : ConfigViewBase
    {
        public EnumConfig()
        {
            InitializeComponent();
        }

        public override object GetUserInputedValue()
        {
            return ComboMan.SelectedItem;
        }
    }
}
