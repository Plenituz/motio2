using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Motio.ValueConverters
{
    public class BoolToColor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool cond = (bool)value;
            string param = (string)parameter;
            string[] split = param.Split(',');
            string color1 = split[0];
            string color2 = split[1];


            if (cond)
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color1));
            else
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color2));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
