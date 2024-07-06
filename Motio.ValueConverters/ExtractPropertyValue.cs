using Motio.Geometry;
using Motio.NodeCore;
using Motio.UI.ViewModels;
using Motio.UICommon;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Motio.ValueConverters
{
    public class ExtractPropertyValue : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            NodePropertyBaseViewModel nodeProperty = (NodePropertyBaseViewModel)values[0];
            if (nodeProperty == null)
                return "error";
            int currentFrame = (int)values[1];
            string para = (string)parameter;

            //check and throw errors to facilitate debugging
            //if (values[1].GetType() != typeof(int))
            //    throw new Exception("you gave a bad frame number to this ExtractPropertyValue converter");
            //if (nodeProperty == null)
            //    throw new Exception("you gave a bad NodeProperty to this ExtractPropertyValue converter");
            //if(para == null)
            //    throw new Exception("you gave a bad parameter to this ExtractPropertyValue converter");

            object data = null;
            switch (para)
            {
                case "in":
                    //calculate the value in this thread, the in value is very light to calculate
                    data = nodeProperty.Original.GetCacheOrCalculateInValue(currentFrame).GetChannelData(Node.PROPERTY_OUT_CHANNEL);
                    break;
                case "out":
                    //never calculate the out value in this thread
                    data = nodeProperty.Original.GetCachedEndOfChain(currentFrame)?.GetChannelData(Node.PROPERTY_OUT_CHANNEL);
                    break;
            }
            if (data == null)
                return "--";

            Type dataType = data.GetType();
            if (dataType == typeof(float))
                return ToolBox.DisplayFloat((float)data);
            if (dataType == typeof(Vector2) || dataType == typeof(Vector3) || dataType == typeof(int))
                return data.ToString();
            
            return "invalid type";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            //TODO implement taht so you can modify a property from ui
            throw new NotImplementedException();
        }
    }
}
