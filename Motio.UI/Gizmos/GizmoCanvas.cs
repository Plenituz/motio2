using System.Windows;
using System.Windows.Controls;
using Motio.Renderers.BezierRendering;
using Motio.Selecting;

namespace Motio.UI.Gizmos
{
    public class GizmoCanvas : Canvas, ISelectionClearer, IBezierContainer
    {
        public string[] HyperGroupsToClear => new[]
        {
            Selection.GIZMOS
        };

        public void AddToRender(FrameworkElement element)
        {
            Children.Add(element);
        }

        public void RemoveFromRender(FrameworkElement element)
        {
            Children.Remove(element);
        }
    }
}
