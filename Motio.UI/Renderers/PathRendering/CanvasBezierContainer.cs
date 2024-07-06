using Motio.Renderers.BezierRendering;
using System.Windows;
using System.Windows.Controls;

namespace Motio.UI.Renderers.PathRendering
{
    public class CanvasBezierContainer : IBezierContainer
    {
        public readonly Canvas canvas;

        public CanvasBezierContainer(Canvas canvas)
        {
            this.canvas = canvas;
        }

        public void AddToRender(FrameworkElement element)
        {
            canvas.Children.Add(element);
        }

        public void RemoveFromRender(FrameworkElement element)
        {
            canvas.Children.Remove(element);
        }
    }
}
