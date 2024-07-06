using Motio.Pathing;
using Motio.Renderers.BezierRendering;
using Motio.UI.Gizmos;
using Motio.UI.ViewModels;
using System.Windows.Media.Media3D;

namespace Motio.UI.Renderers.PathRendering
{
    /// <summary>
    /// renders a <see cref="Path"/> on the gizmo canvas
    /// </summary>
    public class GizmoPathRenderer : BasePathRenderer
    {
        private readonly AnimationTimelineViewModel timeline;

        protected override IBezierContainer Canvas => Gizmo.Canvas;
        protected override float WorldWidth => timeline.CameraWidth;
        protected override float WorldHeight => timeline.CameraHeight;

        public GizmoPathRenderer(Path path, AnimationTimelineViewModel timeline) : base(path)
        {
            this.timeline = timeline;
        }

        protected override (float xCanvas, float yCanvas) WorldToCanvas(float xWorld, float yWorld)
        {
            System.Windows.Point p = timeline.World2Canv(new Point3D(xWorld, yWorld, 0));
            return ((float)p.X, (float)p.Y);
        }

        protected override (float xWorld, float yWorld) CanvasToWorld(float xCanvas, float yCanvas)
        {
            System.Windows.Point p = timeline.Canv2World(new System.Windows.Point(xCanvas, yCanvas));
            return ((float) p.X, (float) p.Y);
        }
}
}
