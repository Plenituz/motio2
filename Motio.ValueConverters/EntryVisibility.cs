using Motio.Configuration;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Motio.ValueConverters
{
    public class EntryVisibility : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(values[0] is ConfigEntry entry) || !(values[1] is string category))
                return null;
            if (entry.Category.Equals(category))
                return Visibility.Visible;
            else
                return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
