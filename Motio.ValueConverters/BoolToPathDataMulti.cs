using System;
using System.Globalization;
using System.Windows.Data;

namespace Motio.ValueConverters
{
    public class BoolToPathDataMulti : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return BoolToPathData.DoConvert(values[0], parameter);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
