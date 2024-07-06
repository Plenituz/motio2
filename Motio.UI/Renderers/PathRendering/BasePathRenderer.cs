using Motio.Animation;
using Motio.Geometry;
using Motio.Pathing;
using Motio.Renderers.BezierRendering;
using Motio.Selecting;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Motio.UI.Renderers.PathRendering
{
    /// <summary>
    /// base class for rendering a <see cref="Path"/>
    /// </summary>
    public abstract class BasePathRenderer
    {
        protected abstract IBezierContainer Canvas { get; }
        protected abstract float WorldWidth { get; }
        protected abstract float WorldHeight { get; }

        private BezierCurveRenderer<VisualPathPoint, VisualPathTangent> renderer;
        private Path path;
        public event Action<PathPoint> Click;
        public bool canAddPoints = true;

        public BasePathRenderer(Path path)
        {
            renderer = new BezierCurveRenderer<VisualPathPoint, VisualPathTangent>(
                Canvas, WorldToCanvas, 
                CanvasToWorld, PointDeleter, PointAdder,
                Selection.PATH_POINTS, Selection.PATH_TANGENTS);
            renderer.Click += o => Click?.Invoke((PathPoint)o);
            renderer.propertiesToListenToOnPoints = new HashSet<string>()
            {
                nameof(PathPoint.Position),
                nameof(PathPoint.RightHandle),
                nameof(PathPoint.LeftHandle)
            };
            this.path = path;

            for(int i = 0; i < path.Points.Count; i++)
            {
                renderer.RenderedPoints.Add(path.Points[i]);
            }
            path.Points.CollectionChanged += Points_CollectionChanged;
            path.PropertyChanged += Path_PropertyChanged;
        }

        protected abstract (float xCanvas, float yCanvas) WorldToCanvas(float xWorld, float yWorld);
        protected abstract (float xWorld, float yWorld) CanvasToWorld(float xCanvas, float yCanvas);

        public void SetPointSize(double size)
        {
            renderer.SetPointSize(size);
        }

        public void SetCurveThickness(double thickness)
        {
            renderer.SetCurveThickness(thickness);
        }

        public void Hide()
        {
            renderer.SetBounds(0, 0, 0, 0);
            renderer.UpdateRender();
        }

        public void Show()
        {
            float width = WorldWidth / 2;
            float height = WorldHeight / 2;
            renderer.SetBounds(-width, width, height, -height);
            renderer.UpdateRender();
        }

        public void UpdateItem(PathPoint point)
        {
            renderer.UpdateItem(point);
        }

        public void Delete()
        {
            path.Points.CollectionChanged -= Points_CollectionChanged;
            path.PropertyChanged -= Path_PropertyChanged;
            renderer.Delete();
        }

        private void Path_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(Path.Closed)))
                renderer.UpdateVisibleItems();
        }

        private void Points_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch(e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (PathPoint point in e.NewItems)
                        renderer.RenderedPoints.Add(point);
                    if(e.NewStartingIndex == 0 && path.Points.Count > 1)
                        renderer.ForceRedraw(path.Points[1]);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (PathPoint point in e.OldItems)
                        renderer.RenderedPoints.Remove(point);
                    //if the first point has been removed, update the new first point
                    //so the special drawing is displayed
                    if(e.OldStartingIndex == 0 && path.Points.Count != 0)
                        renderer.ForceRedraw(path.Points[0]);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    foreach (PathPoint point in e.OldItems)
                        renderer.RenderedPoints.Remove(point);
                    foreach (PathPoint point in e.NewItems)
                        renderer.RenderedPoints.Add(point);
                    break;
                case NotifyCollectionChangedAction.Move:
                    //update render of the points so the first point is the right color
                    if(e.OldStartingIndex == 0 || e.NewStartingIndex == 0)
                    {
                        renderer.ForceRedraw(path.Points[e.NewStartingIndex]);
                        renderer.ForceRedraw(path.Points[e.OldStartingIndex]);
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    renderer.RenderedPoints.Clear();
                    break;
            }
        }        

        private bool PointDeleter(IBezierPoint pointToDelete)
        {
            PathPoint point = (PathPoint)pointToDelete;
            //if this is the start point or the point before, reopen path
            if (path.Closed && point == path.Points[0] || point == path.Points[path.Points.Count - 1])
                path.Closed = false;

            path.Points.Remove(point);
            return true;
        }

        private void PointAdder(IBezierPoint previousBezier, IBezierPoint nextBezier, float xWorld, float yWorld)
        {
            if (!canAddPoints || previousBezier == null || nextBezier == null)
                return;
            PathPoint prev = (PathPoint)previousBezier;
            PathPoint next = (PathPoint)nextBezier;

            int index = path.Points.IndexOf(next);
            if (index == -1)
                throw new Exception("got points that are not in this path in PointAdder of PathRenderer");
            //if we were supposed to insert the point before the start point,
            //insert it after the last point instead
            //that way the start point is not replaced by this new point
            //changing the start point can mess up a setup/animation
            if (index == 0)
                index = path.Points.Count;

            Vector2 firstHandle1;
            Vector2 secondHandle1;
            Vector2 midPoint;
            Vector2 firstHandle2;
            Vector2 secondHandle2;
            //if this is a straight line, no need to create handles
            if(prev.RightHandle == Vector2.Zero && next.LeftHandle == Vector2.Zero)
            {
                midPoint = new Vector2(xWorld, yWorld);
                firstHandle1 = prev.Position;
                secondHandle1 = midPoint;
                firstHandle2 = midPoint;
                secondHandle2 = next.Position;
            }
            else
            {
                (firstHandle1, secondHandle1,
                midPoint,
                firstHandle2, secondHandle2) = DoubleInterpolator.SplitBezier(prev.Position,
                    prev.Position + prev.RightHandle,
                    next.Position + next.LeftHandle,
                    next.Position, new Vector2(xWorld, yWorld));
            }

            prev.RightHandle = firstHandle1 - prev.Position;
            PathPoint newPoint = new PathPoint()
            {
                LeftHandle = secondHandle1 - midPoint,
                Position = midPoint,
                RightHandle = firstHandle2 - midPoint
            };
            next.LeftHandle = secondHandle2 - next.Position;
            //this inserts the point BEFORE the point at given index
            path.Points.Insert(index, newPoint);
        }
    }
}
