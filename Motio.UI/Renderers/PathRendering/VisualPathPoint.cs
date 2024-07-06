using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Motio.Geometry;
using Motio.Pathing;
using Motio.Renderers.BezierRendering;
using Motio.Selecting;
using PointWPF = System.Windows.Point;

namespace Motio.UI.Renderers.PathRendering
{
    public class VisualPathPoint : BezierVisualItem
    {
        protected override Brush Color => (Brush)Application.Current.Resources["CurvePointColor"];
        protected override Brush SelectedColor => (Brush)Application.Current.Resources["CurvePointSelectedColor"];
        public override string DefaultSelectionGroup => Selection.PATH_POINTS;

        protected override System.Windows.Media.Geometry DefiningGeometry
        {
            get
            {
                PathPoint p = (PathPoint)point;
                if(p == p.Host.Points[0])
                {
                    EllipseGeometry mainEllipse = new EllipseGeometry(new PointWPF(0, 0), Width * .5, Height * .5);
                    EllipseGeometry antiMask = new EllipseGeometry(new PointWPF(0, 0), Width * .65, Height * .65);
                    EllipseGeometry masked = new EllipseGeometry(new PointWPF(0, 0), Width * .8, Height * .8);

                    CombinedGeometry ring = new CombinedGeometry(GeometryCombineMode.Xor, masked, antiMask);
                    CombinedGeometry wholeThing = new CombinedGeometry(GeometryCombineMode.Union, ring, mainEllipse);
                    wholeThing.Freeze();
                    return wholeThing;
                }
                else
                {
                    return base.DefiningGeometry;
                }
            }
        }



        /// <summary>
        /// empty constructor to satify constaints
        /// </summary>
        public VisualPathPoint() : base(null)
        {
        }

        public VisualPathPoint(IBezierPoint point) : base(point)
        {
        }

        public override void KeyPressed(KeyEventArgs e)
        {
            PathPoint p = (PathPoint)point;
            switch (e.Key)
            {
                case Key.Z:
                    if(p.RightHandle != Vector2.Zero)
                        p.RightHandle += Vector2.Normalize(p.RightHandle);
                    if(p.LeftHandle != Vector2.Zero)
                        p.LeftHandle += Vector2.Normalize(p.LeftHandle);
                    break;
                case Key.E:
                    {
                        if (p.RightHandle != Vector2.Zero && p.RightHandle.LengthSquared() <= 1)
                            p.RightHandle = Vector2.Zero;
                        else if (p.RightHandle != Vector2.Zero)
                            p.RightHandle -= Vector2.Normalize(p.RightHandle);

                        if (p.LeftHandle != Vector2.Zero && p.LeftHandle.LengthSquared() <= 1)
                            p.LeftHandle = Vector2.Zero;
                        else if (p.LeftHandle != Vector2.Zero)
                            p.LeftHandle -= Vector2.Normalize(p.LeftHandle);
                    }
                    break;
            }
        }
    }
}
