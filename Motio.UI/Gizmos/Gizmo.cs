using Motio.Renderers;
using Motio.UI.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace Motio.UI.Gizmos
{
    public abstract class Gizmo : Shape
    {
        public static GizmoCanvas Canvas { get; protected set; }
        private static AnimationTimelineViewModel _timeline;

        public static void Init(GizmoCanvas canvas, AnimationTimelineViewModel timeline)
        {
            Canvas = canvas;
            _timeline = timeline;
        }

        public static void Add(UIElement gizmo)
        {
            Canvas.Children.Add(gizmo);
        }

        public static CenteredEllipse AddCircle(Point center, double radius, Brush color = null, bool transformPos = false)
        {
            if (color == null)
                color = Brushes.Red;
            CenteredEllipse ellipse = new CenteredEllipse()
            {
                Fill = color,
                Width = radius,
                Height = radius
            };
            Add(ellipse);
            if (transformPos)
                SetWorldPos(ellipse, center.X, center.Y);
            else
                SetCanvasPos(ellipse, center.X - radius / 2, center.Y - radius / 2);
            return ellipse;
        }

        /// <summary>
        /// set position of the gizmo, the given position must be in canvas space
        /// </summary>
        /// <param name="gizmo"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public static void SetCanvasPos(UIElement gizmo, double x, double y)
        {
            System.Windows.Controls.Canvas.SetLeft(gizmo, x);
            System.Windows.Controls.Canvas.SetTop(gizmo, y);
        }

        /// <summary>
        /// transform the given position to canvas space and set it
        /// </summary>
        /// <param name="gizmo"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public static void SetWorldPos(UIElement gizmo, double x, double y)
        {
            Point canvPos = _timeline.World2Canv(new Point3D(x, y, 0));
            SetCanvasPos(gizmo, canvPos.X, canvPos.Y);
        }

        public static void Remove(UIElement gizmo)
        {
            Canvas.Children.Remove(gizmo);
        }

        public static void Clear()
        {
            Canvas.Children.Clear();
        }
    }
}
