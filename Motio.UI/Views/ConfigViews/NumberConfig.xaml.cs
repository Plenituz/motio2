using Motio.Configuration;
using Motio.NodeCommon;
using System;

namespace Motio.UI.Views.ConfigViews
{
    /// <summary>
    /// Interaction logic for NumberConfig.xaml
    /// </summary>
    public partial class NumberConfig : ConfigViewBase
    {
        public NumberConfig()
        {
            InitializeComponent();
        }

        public override object GetUserInputedValue()
        {
            ConfigEntry entry = (ConfigEntry)DataContext;
            Type type = entry.GetType().GetGenericArguments()[0];

            if (type == typeof(double))
            {
                return ToolBox.ConvertToDouble(ValueField.Text);
            }
            else if(type == typeof(int))
            {
                return ToolBox.ConvertToInt(ValueField.Text);
            }
            else if(type == typeof(long))
            {
                return ToolBox.ConvertToLong(ValueField.Text);
            }
            else
            {
                throw new InvalidCastException("config entry type is not int double or long");
            }
        }
    }
}
