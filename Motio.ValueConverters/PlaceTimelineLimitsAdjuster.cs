using Motio.UI.Renderers.KeyframeRendering;
using Motio.UI.Views;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Motio.ValueConverters
{
    public class PlaceTimelineLimitsAdjuster : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            //values : 
            //[0] the timelimits adjuster 
            //[1] left keyframe space
            //[2] right keyframe space
            //[3] max frame 
            //[4] actualwidth of container
            if (values.Length != 5)
                throw new Exception("expected 5 values, you don't even know how to use your own shit");
            //the first call will be with unsetvalue because everything is not initialised
            if (values[1] == DependencyProperty.UnsetValue)
                return true;
            TimelineLimitsAdjuster adjuster = (TimelineLimitsAdjuster)values[0];
            double left = (double)values[1];
            double right = (double)values[2];
            int maxFrame = (int)values[3];
            double actualWidth = (double)values[4];


            double end = KeyframeTimelineRenderer.KeyframeToCanvasSpace(right, 0, maxFrame, actualWidth);
            double start = KeyframeTimelineRenderer.KeyframeToCanvasSpace(left, 0, maxFrame, actualWidth);
            double width = end - start;
            adjuster.Size = width - adjuster.SizeSideFlap * 2;//substracting the size of the side flaps
            Canvas.SetLeft(adjuster, start);
            return true;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
