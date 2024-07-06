using Motio.Animation;
using Motio.UI.Renderers.KeyframeRendering;
using Motio.UI.Utils;
using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace Motio.ValueConverters
{
    public class ExtractAndDisplayKeyframes : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            //values should be:
            //[0] : NodePropertyBaseViewModel
            //[1] : canvas Width (just used for binding updates)
            //[2] : canvas space left
            //[3] : canvas space right
            //[4] : canvas 
            //[5] : KeyframeHolder
            //[6] : AnimationTimelin.maxFrame
            //even tho we have the canvas and we could get the width from it, we pass it
            //as a binding for the auto update
            //same for maxFrame

            //sometimes the datacontext is not set properly when this is called, 
            //but the next call it will be so just ignore this one
            if (values[2].GetType() != typeof(double))
                return true;

            double width = (double)values[1];
            double left = (double)values[2];
            double right = (double)values[3];
            Canvas canvas = (Canvas)values[4];
            KeyframeHolder holder = (KeyframeHolder)values[5];

            //TODO store the renderer in the viewmodel ?
            KeyframeTimelineRenderer renderer;
            if (canvas.Tag == null)
            {
                renderer = new KeyframeTimelineRenderer(canvas, holder, canvas.FindMainViewModel().AnimationTimeline);
                canvas.Tag = renderer;
            }
            else
            {
                renderer = (KeyframeTimelineRenderer)canvas.Tag;
            }

            renderer.SetBounds(left, right, 0, 0);
            renderer.UpdateRender();
            return true;
        }



        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
