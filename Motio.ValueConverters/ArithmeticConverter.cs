using System;
using System.Data;
using System.Globalization;
using System.Windows.Data;

namespace Motio.ValueConverters
{
    public class ArithmeticConverter : IValueConverter
    {
        private static DataTable evaluator = new DataTable();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double v = 0;
            try
            {
                v = System.Convert.ToDouble(value);
            }
            catch (Exception) { }

            try
            {
                return evaluator.Compute(v.ToString() + parameter, "");
            }
            catch (Exception) { }
            return v;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
