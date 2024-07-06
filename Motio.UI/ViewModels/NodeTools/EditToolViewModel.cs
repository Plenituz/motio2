using Motio.Meshing;
using Motio.NodeCommon.Utils;
using Motio.NodeCore;
using Motio.NodeCore.Utils;
using Motio.NodeImpl.GraphicsAffectingNodes;
using Motio.UI.Renderers;
using Motio.UI.Utils;
using Motio.UI.Views;
using System.Threading;
using System.Windows;

namespace Motio.UI.ViewModels
{
    public class EditToolViewModel : NodeToolViewModel
    {
        EditGraphicsNode mNode;
        MeshGizmoRenderer renderer;
        AnimationTimeline timeline;
        RenderView renderView;
        private const double POINT_SIZE = 10;
        private const int MAX_TRY = 10;
        int tries = 0;

        public EditToolViewModel(NodeTool tool) : base(tool)
        {
            mNode = (EditGraphicsNode)tool.nodeHost;
            timeline = mNode.GetTimeline();
        }

        private void RenderView_ContentScaleChanged(double size)
        {
            renderer.SetPointSize(POINT_SIZE / size);
        }


        public override void OnShow()
        {
            base.OnShow();
            GraphicsNode parent = mNode.FindGraphicsNode(out _);
            DataFeed cache = parent.GetCache(timeline.CurrentFrame, mNode);

            if (renderer == null)
            {
                renderer = new MeshGizmoRenderer();
                renderView = _host.FindMainViewModel().RenderView;
                renderer.SetPointSize(renderView.InverseContentScale * POINT_SIZE);
                renderer.SetEdit += Renderer_SetEdit;
            }

            if (cache == null || !cache.TryGetChannelData(Node.MESH_CHANNEL, out MeshGroup group))
            {
                if(tries < MAX_TRY)
                {
                    ThreadPool.QueueUserWorkItem(e =>
                    {
                        tries++;
                        Thread.Sleep(100);
                        Application.Current.Dispatcher.Invoke(OnShow);
                    });
                }
                return;
            }
            tries = 0;
            renderView.ContentScaleChanged += RenderView_ContentScaleChanged;

            double width2 = timeline.CameraWidth / 2;
            double height2 = timeline.CameraHeight / 2;
            renderer.SetBounds(-width2, width2, height2, -height2);

            renderer.meshGroup = group;
            renderer.UpdateRender();
        }

        private void Renderer_SetEdit(string id, Geometry.Vector2 position)
        {
            mNode.edits[id] = position;
            mNode.FindGraphicsNode(out var gAff).InvalidateAllCachedFrames(gAff);
        }

        public override void OnHide()
        {
            base.OnHide();
            renderView.ContentScaleChanged -= RenderView_ContentScaleChanged;
            renderer.SetEdit -= Renderer_SetEdit;//not needed but just for completness
            renderer.SetBounds(0, 0, 0, 0);//hide
            //renderer.Hide();
            renderer.UpdateRender();
        }

        public override void Delete()
        {
            base.Delete();
            OnHide();
        }
    }
}
