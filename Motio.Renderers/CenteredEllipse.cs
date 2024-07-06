using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Motio.Renderers
{
    /// <summary>
    /// an ellipse centered on it's center instead of the default top right corner. also implements <see cref="ISelectable"/>
    /// through events
    /// </summary>
    public class CenteredEllipse : Shape
    {
        protected override Geometry DefiningGeometry => new EllipseGeometry(new Point(0, 0), Width / 2, Height / 2);
    }
}
