using Motio.UI.Renderers.KeyframeRendering;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Motio.ValueConverters
{
    public class PositionPlayHeadOnCanvas : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0].GetType() != typeof(int))
                return 0;
            int currentFrame = (int)values[0];
            double canvasWidth = (double)values[1];
            double left = (double)values[2];
            double right = (double)values[3];
            //value[4] : AnimationTimeline.maxFrame

            if (currentFrame > right)
                return KeyframeTimelineRenderer.KeyframeToCanvasSpace(right, left, right, canvasWidth);
            else if(currentFrame < left)
                return KeyframeTimelineRenderer.KeyframeToCanvasSpace(left, left, right, canvasWidth);
            else
                return KeyframeTimelineRenderer.KeyframeToCanvasSpace(currentFrame, left, right, canvasWidth);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
