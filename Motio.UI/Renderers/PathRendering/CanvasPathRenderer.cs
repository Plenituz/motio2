using Motio.Pathing;
using Motio.Renderers.BezierRendering;
using Motio.UI.ViewModels;
using Motio.UICommon;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Motio.UI.Renderers.PathRendering
{
    /// <summary>
    /// render a <see cref="Pathing.Path"/> on a regular canvas
    /// </summary>
    public class CanvasPathRenderer : BasePathRenderer
    {
        private readonly CanvasBezierContainer canvas;
        protected override IBezierContainer Canvas => canvas;
        protected override float WorldWidth => (float)Math.Abs(Bounds.Right - Bounds.Left);
        protected override float WorldHeight => (float)Math.Abs(Bounds.Top - Bounds.Bottom);
        public SimpleRect Bounds { get; set; }

        public CanvasPathRenderer(Path path, Canvas canvas) : base(path)
        {
            this.canvas = new CanvasBezierContainer(canvas);
        }

        protected override (float xWorld, float yWorld) CanvasToWorld(float xCanvas, float yCanvas)
        {
            if (canvas == null)
                return (0, 0);
            Point pos = CurvePanelViewModel.CanvasToKeyframeSpace(
                new Point(xCanvas, yCanvas),
                Bounds,
                canvas.canvas.ActualWidth,
                canvas.canvas.ActualHeight);
            return ((float)pos.X, (float)pos.Y);
        }

        protected override (float xCanvas, float yCanvas) WorldToCanvas(float xWorld, float yWorld)
        {
            if (canvas == null)
                return (0, 0);
            Point pos = CurvePanelViewModel.KeyframeToCanvasSpace(
                new Point(xWorld, yWorld),
                Bounds,
                canvas.canvas.ActualWidth,
                canvas.canvas.ActualHeight);
            return ((float)pos.X, (float)pos.Y);
        }
    }
}
