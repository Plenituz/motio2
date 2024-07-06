using Motio.UI.Renderers;
using Motio.UICommon;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Motio.ValueConverters
{
    public class ShowCacheData : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            /*
             * values[0] : Canvas canvas
             * values[1] : double left
             * values[2] : double right
             * values[3] : int maxFrame
             * values[4] : FrameworkElement container
             */
            if (values[1].GetType() != typeof(double))
                return true;
            Canvas canvas = (Canvas)values[0];
            double left = (double)values[1];
            double right = (double)values[2];
            FrameworkElement container = (FrameworkElement)values[4];

            CacheRenderer renderer;
            if(canvas.Tag == null)
            {
                renderer = new CacheRenderer(canvas, container);
                canvas.Tag = renderer;
            }
            else
            {
                renderer = (CacheRenderer)canvas.Tag;
            }
            //changing the bounds updates the render
            renderer.Bounds = new SimpleRect()
            {
                Left = left,
                Right = right
            };
            return true;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
