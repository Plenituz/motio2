using System.Windows.Media;
using System.Windows.Shapes;

namespace Motio.UICommon
{
    public class SelectionRect : Shape
    {
        protected override System.Windows.Media.Geometry DefiningGeometry => geometry;

        public System.Windows.Media.Geometry geometry;

        public SelectionRect()
        {
            Fill = new SolidColorBrush(new Color() { R = 150, G = 150, B = 150, A = 150 });
            IsHitTestVisible = false;
        }
    }
}
