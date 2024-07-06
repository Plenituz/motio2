using Motio.Geometry;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Media3D;

namespace Motio.ValueConverters
{
    public class Vector3ToPoint3D : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Vector3 point = (Vector3)value;
            return new Point3D(point.X, point.Y, point.Z);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
