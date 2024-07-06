using System.Windows.Controls;

namespace Motio.UI.Views.ConfigViews
{
    public abstract class ConfigViewBase : UserControl
    {
        public abstract object GetUserInputedValue();
    }
}
