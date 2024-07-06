using Motio.NodeCore;
using Motio.UI.ViewModels;
using Motio.UICommon;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Motio.ValueConverters
{
    public class ExtractVectorPropertyValue : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            /*
             * parameter should be [x/y/z],[in/out]
             */

            NodePropertyBaseViewModel nodeProperty = (NodePropertyBaseViewModel)values[0];
            if (nodeProperty == null)
                return "??";
            int currentFrame = (int)values[1];
            string para = (string)parameter;
            string[] split = para.Split(new char[] { ',' });
            string component = split[0];
            string side = split[1];

            dynamic point = null;
            switch (side)
            {
                case "in":
                    point = nodeProperty.Original.GetCacheOrCalculateInValue(currentFrame).GetChannelData(Node.PROPERTY_OUT_CHANNEL);
                    break;
                case "out":
                    point = nodeProperty.Original.GetCachedEndOfChain(currentFrame).GetChannelData(Node.PROPERTY_OUT_CHANNEL);
                    break;
            }
            if (point == null)    
                return "--";
            switch (component)
            {
                case "x":
                    return ToolBox.DisplayFloat(point.X);
                case "y":
                    return ToolBox.DisplayFloat(point.Y);
                case "z":
                    return ToolBox.DisplayFloat(point.Z);
            }
            return "??";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
