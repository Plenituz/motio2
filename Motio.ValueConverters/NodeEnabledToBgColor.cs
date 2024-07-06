using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Motio.ValueConverters
{
    public class NodeEnabledToBgColor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool enabled = (bool)value;
            //TODO this doesn't seem to change the bg color 
            return enabled ? Brushes.LightGray : Brushes.Plum;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
