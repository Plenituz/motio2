using System.Windows;

namespace Motio.Renderers.BezierRendering
{
    public interface IBezierContainer
    {
        void AddToRender(FrameworkElement element);
        void RemoveFromRender(FrameworkElement element);
    }
}
