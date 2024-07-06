using System.Windows;

namespace Motio.UICommon
{
    public class SimpleRect
    {
        //TODO do simple check to assure the rect's integrity
        public double Left { get; set; }
        public double Right { get; set; }
        public double Top { get; set; }
        public double Bottom { get; set; }

        public bool IsInside(Point point)
        {
            return point.X >= Left && point.X <= Right
                && point.Y >= Bottom && point.Y <= Top;
        }
    }
}
