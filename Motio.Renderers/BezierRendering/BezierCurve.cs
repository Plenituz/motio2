using Motio.ClickLogic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Motio.Renderers.BezierRendering
{
    /// <summary>
    /// transform from IBezier data space to canvas space
    /// </summary>
    /// <param name="xWorld"></param>
    /// <param name="yWorld"></param>
    /// <returns></returns>
    public delegate (float xCanvas, float yCanvas) CoordinateTransformer(float xWorld, float yWorld);
    /// <summary>
    /// try to delete the given point from the actual bezier curve data and return wether the removal was sucessful
    /// </summary>
    /// <param name="pointToDelete"></param>
    /// <returns></returns>
    public delegate bool PointDeleter(IBezierPoint pointToDelete);
    /// <summary>
    /// Add a point that sits in between the 2 given points at the given coordinates. previous or next might be null.
    /// </summary>
    /// <param name="previous"></param>
    /// <param name="next"></param>
    /// <param name="xWorld"></param>
    /// <param name="yWorld"></param>
    public delegate void PointAdder(IBezierPoint previous, IBezierPoint next, float xWorld, float yWorld);

    public class BezierCurve
    {
        private Point _p1, _p2, _p3, _p4;
        private Path path;
        private BezierSegment segment;
        private PathFigure fig;

        private CoordinateTransformer ToCanvasSpace;
        public readonly IBezierPoint startPoint;
        public readonly IBezierPoint endPoint;
        public event MouseEventHandler Click;

        public BezierCurve(IBezierPoint startPoint,
            IBezierPoint endPoint,
            CoordinateTransformer toCanvasSpace)
        {
            this.ToCanvasSpace = toCanvasSpace;
            this.startPoint = startPoint;
            this.endPoint = endPoint;

            path = new Path();
            PathGeometry pGeo = new PathGeometry();
            fig = new PathFigure();
            pGeo.Figures = new PathFigureCollection();
            fig.Segments = new PathSegmentCollection();
            path.Data = pGeo;
            pGeo.Figures.Add(fig);
            segment = new BezierSegment();
            fig.Segments.Add(segment);

            path.Stroke = Brushes.Black;
            path.StrokeThickness = 3;
            Panel.SetZIndex(path, -1);

            new ClickAndDragHandler(path)
            {
                OnClickWithSender = (s, a) => Click?.Invoke(s, a)
            };
        }

        public void SetCurveThickness(double thiccneck)
        {
            path.StrokeThickness = thiccneck;
        }

        public void UpdateCurvePoints(
            float? p1X = null, float? p1Y = null,
            float? p2X = null, float? p2Y = null,
            float? p3X = null, float? p3Y = null,
            float? p4X = null, float? p4Y = null)
        {
            float startXWorld = startPoint.PointX;
            float startYWorld = startPoint.PointY;
            float endXWorld = endPoint.PointX;
            float endYWorld = endPoint.PointY;

            if(!p1X.HasValue || !p1Y.HasValue)
                (p1X, p1Y) = ToCanvasSpace(startXWorld, startYWorld);
            _p1.X = p1X.Value;
            _p1.Y = p1Y.Value;

            if(!p2X.HasValue || !p2Y.HasValue)
                (p2X, p2Y) = ToCanvasSpace(startXWorld + startPoint.RightHandleX, startYWorld + startPoint.RightHandleY);
            _p2.X = p2X.Value;
            _p2.Y = p2Y.Value;

            if(!p3X.HasValue || !p3Y.HasValue)
                (p3X, p3Y) = ToCanvasSpace(endXWorld + endPoint.LeftHandleX, endYWorld + endPoint.LeftHandleY);
            _p3.X = p3X.Value;
            _p3.Y = p3Y.Value;

            if (!p4X.HasValue || !p4Y.HasValue)
                (p4X, p4Y) = ToCanvasSpace(endXWorld, endYWorld);
            _p4.X = p4X.Value;
            _p4.Y = p4Y.Value;

            UpdateCurve();
        }

        private void UpdateCurve()
        {
            fig.StartPoint = _p1;
            segment.Point1 = _p2;
            segment.Point2 = _p3;
            segment.Point3 = _p4;
        }

        /// <summary>
        /// display the curve on the given canvas
        /// </summary>
        /// <param name="canvas"></param>
        public void AddToCanvas(IBezierContainer canvas)
        {
            canvas.AddToRender(path);
        }

        /// <summary>
        /// remove the curve from the given canvas
        /// </summary>
        /// <param name="canvas"></param>
        public void RemoveFromCanvas(IBezierContainer canvas)
        {
            canvas.RemoveFromRender(path);
        }
    }
}
