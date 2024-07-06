using Motio.Animation;
using Motio.Geometry;
using Motio.Renderers;
using Motio.Renderers.BezierRendering;
using Motio.Selecting;
using Motio.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Motio.UI.Renderers.KeyframeCurveRendering
{
    public class KeyframeCurveRenderer
    {
        private BezierCurveRenderer<KeyframeCurvePathPoint, KeyframeCurvePathTangent> renderer;
        private GridRenderer gridRenderer;
        private CurvePanelViewModel curvePanel;
        HashSet<KeyframeHolder> holders = new HashSet<KeyframeHolder>();
        public IEnumerable<KeyframeFloat> RenderedKeyframes
        {
            get
            {
                foreach (KeyframeFloat keyframe in renderer.RenderedPoints)
                    yield return keyframe;
            }
        }

        public KeyframeCurveRenderer(CurvePanelViewModel curvePanel)
        {
            this.curvePanel = curvePanel;
            gridRenderer = new GridRenderer(curvePanel.CurveCanvas, KeyframeToCanvas);
            renderer = new BezierCurveRenderer<KeyframeCurvePathPoint, KeyframeCurvePathTangent>(
                curvePanel.CurveCanvas, KeyframeToCanvas, 
                CanvasToKeyframe, PointDeleter, PointAdder,
                Selection.KEYFRAME_CURVES, Selection.KEYFRAME_CURVES_TANGENTS);
            UpdateBounds();
            renderer.propertiesToListenToOnPoints = new HashSet<string>
            {
                nameof(KeyframeFloat.Value),
                nameof(KeyframeFloat.Time),
                nameof(KeyframeFloat.LeftHandle),
                nameof(KeyframeFloat.RightHandle)
            };
            renderer.SetPointSize(8);
            renderer.SetCurveThickness(3);
        }

        private void Holder_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    KeyframeFloat keyframe = (KeyframeFloat)e.NewItems[0];
                    if (!renderer.RenderedPoints.Contains(keyframe))
                        renderer.RenderedPoints.Add(keyframe);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    renderer.RenderedPoints.Remove((KeyframeFloat)e.OldItems[0]);
                    break;
            }
        }

        public bool Contains(KeyframeHolder holder)
        {
            return holders.Contains(holder);
        }

        public void Add(KeyframeHolder holder)
        {
            if (holders.Contains(holder))
                return;
            holders.Add(holder);

            for(int i = 0; i < holder.Count; i++)
            {
                KeyframeFloat keyframe = holder.KeyframeAt(i);
                if (!renderer.RenderedPoints.Contains(keyframe))
                    renderer.RenderedPoints.Add(keyframe);
            }
            holder.CollectionChanged += Holder_CollectionChanged;
        }

        public void Remove(KeyframeHolder holder)
        {
            holders.Remove(holder);
            holder.CollectionChanged -= Holder_CollectionChanged;
            for (int i = 0; i < holder.Count; i++)
            {
                renderer.RenderedPoints.Remove(holder.KeyframeAt(i));
            }
        }

        public void UpdateBounds()
        {
            gridRenderer.SetBounds(curvePanel.Left, curvePanel.Right, curvePanel.Top, curvePanel.Bottom);
            renderer.SetBounds(curvePanel.Left, curvePanel.Right, curvePanel.Top, curvePanel.Bottom);
            renderer.UpdateRender();
            gridRenderer.UpdateRender();
        }

        private (float xCanvas, float yCanvas) KeyframeToCanvas(float xWorld, float yWorld)
        {
            System.Windows.Point p = curvePanel.Keyframe2Canvas(new System.Windows.Point(xWorld, yWorld));
            return ((float)p.X, (float)p.Y);
        }

        private (float xWorld, float yWorld) CanvasToKeyframe(float xCanvas, float yCanvas)
        {
            System.Windows.Point p = curvePanel.Canvas2Keyframe(new System.Windows.Point(xCanvas, yCanvas));
            return ((float)p.X, (float)p.Y);
        }

        private bool PointDeleter(IBezierPoint pointToDelete)
        {
            KeyframeFloat keyframe = (KeyframeFloat)pointToDelete;
            if (keyframe.Holder == null)
                return false;
            keyframe.Holder.RemoveKeyframe(keyframe);
            return true;
        }

        private void PointAdder(IBezierPoint previousBezier, IBezierPoint nextBezier, float xWorld, float yWorld)
        {
            if (previousBezier == null || nextBezier == null)
                return;
            KeyframeFloat prev = (KeyframeFloat)previousBezier;
            KeyframeFloat next = (KeyframeFloat)nextBezier;
            KeyframeHolder holder = prev.Holder;
            if (holder == null || next.Holder == null)
                throw new Exception("previous or next keyframe has no holder in KeyframeCurveRenderer PointAdder");
            if (holder != next.Holder)
                throw new Exception("previous keyframe and next keyframe don't have the same holder in KeyframeCurveRenderer PointAdder");

            Vector2 prevPos = new Vector2(prev.Time, prev.Value);
            Vector2 nextPos = new Vector2(next.Time, next.Value);

            Vector2 firstHandle1;
            Vector2 secondHandle1;
            Vector2 midPoint;
            Vector2 firstHandle2;
            Vector2 secondHandle2;

            //if this is a straight line, no need to create handles
            if (prev.RightHandle == Vector2.Zero && next.LeftHandle == Vector2.Zero)
            {
                midPoint = new Vector2(xWorld, yWorld);
                firstHandle1 = prevPos;
                secondHandle1 = midPoint;
                firstHandle2 = midPoint;
                secondHandle2 = nextPos;
            }
            else
            {
                (firstHandle1, secondHandle1,
                midPoint,
                firstHandle2, secondHandle2) = DoubleInterpolator.SplitBezier(prevPos,
                    prevPos + prev.RightHandle,
                    nextPos + next.LeftHandle,
                    nextPos, new Vector2(xWorld, yWorld));
            }

            prev.RightHandle = firstHandle1 - prevPos;
            KeyframeFloat newKey = new KeyframeFloat((int)midPoint.X, midPoint.Y)
            {
                LeftHandle = secondHandle1 - midPoint,
                RightHandle = firstHandle2 - midPoint
            };
            next.LeftHandle = secondHandle2 - nextPos;
            holder.AddKeyframe(newKey);
            renderer.RenderedPoints.Add(newKey);
        }
    }
}
