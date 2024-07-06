using Motio.NodeCommon;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Motio.ValueConverters
{
    public class ToFloat : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ToolBox.ConvertToFloat(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
