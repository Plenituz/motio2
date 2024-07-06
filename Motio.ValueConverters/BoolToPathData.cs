using Motio.Graphics;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Motio.ValueConverters
{
    public class BoolToPathData : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DoConvert(value, parameter);
        }

        public static object DoConvert(object value, object parameter)
        {
            if (!(value is bool bVal))
                return "error value is not bool";
            if (!(parameter is string param))
                return "error param is not string";
            string[] split = param.Split('|');
            if (split.Length != 2)
                return "error split is not 2";

            string data1 = split[0];
            string data2 = split[1];

            if (data1.StartsWith("file:"))
                data1 = IconStorage.GetOrCreateCache(data1.Split(new char[] { ':' }, 2)[1]);
            if (data2.StartsWith("file:"))
                data2 = IconStorage.GetOrCreateCache(data2.Split(new char[] { ':' }, 2)[1]);

            if (bVal)
                return data1;
            else
                return data2;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
