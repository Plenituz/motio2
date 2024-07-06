using System.Windows.Media;
using System.Windows.Shapes;

namespace Motio.UI.Views
{
    public class TimelineItem : Shape
    {
        GeometryGroup _geometry;
        protected override System.Windows.Media.Geometry DefiningGeometry => _geometry;

        public TimelineItem()
        {
            _geometry = new GeometryGroup();
        }
    }
}
