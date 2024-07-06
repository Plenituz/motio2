using Motio.NodeCommon;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Motio.ValueConverters
{
    public class PointConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double mult1 = 1, mult2 = 1, add1 = 0, add2 = 0;
            if(parameter != null)
            {
                string[] split = parameter.ToString().Split(',');
                mult1 = ToolBox.ConvertToDouble(split[0]);
                if(split.Length > 1)
                    mult2 = ToolBox.ConvertToDouble(split[1]);
                if (split.Length > 2)
                    add1 = ToolBox.ConvertToDouble(split[2]);
                if (split.Length > 3)
                    add2 = ToolBox.ConvertToDouble(split[3]);
            }
            double dynAdd1 = 0, dynAdd2 = 0;
            if(values.Length > 2)
                dynAdd1 = ToolBox.ConvertToDouble(values[2]);
            if (values.Length > 3)
                dynAdd2 = ToolBox.ConvertToDouble(values[3]);

            return new Point(
                ToolBox.ConvertToDouble(values[0]) * mult1 + (dynAdd1 != 0 ? dynAdd1*add1 : add1),
                ToolBox.ConvertToDouble(values[1]) * mult2 + (dynAdd2 != 0 ? dynAdd2*add2 : add2));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
