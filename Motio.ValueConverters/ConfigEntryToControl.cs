using Motio.Configuration;
using Motio.UI.Views.ConfigViews;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Motio.ValueConverters
{
    public class ConfigEntryToControl : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is ConfigEntry entry))
                return null;
            Type type = entry.GetType().GetGenericArguments()[0];

            ConfigViewBase view;
            if (type.IsEnum)
            {
                //dropdown
                view = new EnumConfig();
            }
            else if(type == typeof(string))
            {
                //file
                view = new FileConfig();
            }
            else if(type == typeof(bool))
            {
                //checkbox
                view = new BoolConfig();
            }
            else if(type == typeof(double) || type == typeof(int) || type == typeof(long))
            {
                //number
                view = new NumberConfig();
            }
            else
            {
                throw new Exception("unknown config entry type:" + type);
            }
            view.DataContext = entry;

            return view;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
