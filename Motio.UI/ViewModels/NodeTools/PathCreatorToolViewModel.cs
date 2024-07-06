using System.Windows;
using System.Windows.Input;
using Motio.NodeCore;
using Motio.NodeImpl.GraphicsAffectingNodes;
using Motio.Pathing;
using Motio.UI.Gizmos;
using Motio.UI.Renderers.PathRendering;
using Motio.UI.Utils;
using Motio.UI.Views;

namespace Motio.UI.ViewModels
{
    public class PathCreatorToolViewModel : NodeToolViewModel
    {
        private BasePathRenderer renderer;
        private PathGraphicsNode node;
        private RenderView renderView;
        private MainControlViewModel mainViewModel;

        private PathPoint dragingPoint;
        private Geometry.Vector2 worldPosStart;

        public PathCreatorToolViewModel(NodeTool tool) : base(tool)
        {
            node = (PathGraphicsNode)tool.nodeHost;
        }

        public override void OnShow()
        {
            base.OnShow();
            if(renderer == null)
            {
                mainViewModel = _host.FindMainViewModel();
                renderView = mainViewModel.RenderView;
                renderer = new GizmoPathRenderer(node.path, mainViewModel.AnimationTimeline);
                renderer.Click += Renderer_Click;
                _host.FindPropertyPanel().SetActiveTool(this);
            }

            renderer.Show();
            UpdateSizes();
            renderView.ContentScaleChanged += RenderView_ContentScaleChanged;
        }
        
        private void InvalidateGraphics()
        {
            ((GraphicsNode)_host.FindGraphicsNode(out var gAff).Original).InvalidateAllCachedFrames((GraphicsAffectingNode)gAff.Original);
        }

        private void Renderer_Click(PathPoint obj)
        {
            if (Selected && obj == node.path.Points[0])
            {
                node.path.Closed = true;
                InvalidateGraphics();
                renderer.UpdateItem(node.path.Points[node.path.Points.Count-1]);
            }
        }

        private void RenderView_ContentScaleChanged(double obj)
        {
            UpdateSizes();
        }

        private void UpdateSizes()
        {
            renderer.SetPointSize(8 * renderView.InverseContentScale);
            renderer.SetCurveThickness(1 * renderView.InverseContentScale);
        }

        public override void OnHide()
        {
            base.OnHide();
            renderer.Hide();
            renderView.ContentScaleChanged -= RenderView_ContentScaleChanged;
        }

        public override void Delete()
        {
            base.Delete();
            OnHide();
            renderer.Delete();
        }

        public override void OnClickInViewport(MouseEventArgs ev, Point worldPos, Point canvasPos)
        {
            base.OnClickInViewport(ev, worldPos, canvasPos);
            if(!node.path.Closed)
            {
                node.path.Points.Add(new PathPoint()
                {
                    Position = new Geometry.Vector2((float)worldPos.X, (float)worldPos.Y)
                });
            }
        }

        public override void OnDragEnterInViewport(ClickLogic.DragEventArgs dragEvent)
        {
            base.OnDragEnterInViewport(dragEvent);
            if (!node.path.Closed)
            {
                worldPosStart = GetWorldPos(dragEvent.startEvent);
                dragingPoint = new PathPoint()
                {
                    Position = worldPosStart
                };
                node.path.Points.Add(dragingPoint);
            }
        }

        public override void OnDragInViewport(ClickLogic.DragEventArgs dragEvent)
        {
            base.OnDragInViewport(dragEvent);
            if(dragingPoint != null)
            {
                Geometry.Vector2 currentWorld = GetWorldPos(dragEvent.currentEvent);
                dragingPoint.RightHandle = currentWorld - worldPosStart;
                dragingPoint.LeftHandle = -dragingPoint.RightHandle;
            }
        }

        public override void OnDragEndInViewport(ClickLogic.DragEventArgs dragEvent)
        {
            base.OnDragEndInViewport(dragEvent);
            dragingPoint = null;
        }

        public override void OnSelect()
        {
            base.OnSelect();
            renderer.canAddPoints = true;
        }

        public override void OnDeselect()
        {
            base.OnDeselect();
            renderer.canAddPoints = false;
        }

        private Geometry.Vector2 GetWorldPos(MouseEventArgs args)
        {
            Point p = mainViewModel.AnimationTimeline.Canv2World(args.GetPosition(Gizmo.Canvas));
            return new Geometry.Vector2((float)p.X, (float)p.Y);
        }
    }
}
