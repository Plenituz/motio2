using System;
using System.Globalization;
using System.Windows.Data;

namespace Motio.ValueConverters
{
    public class EnumList : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Type type = value.GetType();
            if (!type.IsEnum)
                return null;
            return Enum.GetValues(type);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
