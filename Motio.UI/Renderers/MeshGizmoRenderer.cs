using System;
using System.Collections.Generic;
using System.Windows;
using Motio.ClickLogic;
using Motio.Meshing;
using Motio.Renderers;
using Motio.Selecting;
using Motio.UI.Gizmos;
using Motio.UI.Utils;
using Motio.UI.ViewModels;

namespace Motio.UI.Renderers
{
    public class MeshGizmoRenderer : BoundedItemRenderer<string, MeshPointEllipse>
    {
        public MeshGroup meshGroup;
        private readonly AnimationTimelineViewModel timeline;
        private static char[] splitChar = new char[] { ':' };
        private double pointSize;
        SelectionAwareClickHandler<MeshPointEllipse> clickHandler;

        public event Action<string, Geometry.Vector2> SetEdit;

        protected override IEnumerable<string> AllDataItems
        {
            get
            {
                for(int groupIndex = 0; groupIndex < meshGroup.Count; groupIndex++)
                {
                    Mesh mesh = meshGroup[groupIndex];
                    for(int i = 0; i < mesh.vertices.Count; i++)
                    {
                        yield return groupIndex.ToString() + splitChar[0] + i.ToString();
                    }
                }
            }
        }

        public MeshGizmoRenderer()
        {
            timeline = Gizmo.Canvas.FindMainViewModel().AnimationTimeline;
            clickHandler = new SelectionAwareClickHandler<MeshPointEllipse>(Gizmo.Canvas, Selection.MESH_POINTS);
            clickHandler.UpdatePosition += ClickHandler_UpdatePosition;
        }

        private void ClickHandler_UpdatePosition(MeshPointEllipse selectable, Point position)
        {
            Point worldPos = timeline.Canv2World(position);
            int groupIndex = meshGroup.IndexOf(selectable.mesh);
            string id = groupIndex.ToString() + splitChar[0] + selectable.pIndex.ToString();

            SetEdit?.Invoke(id, new Geometry.Vector2((float)worldPos.X, (float)worldPos.Y));
            UpdateItem(id, selectable, position);
        }

        private (Mesh mesh, int pIndex) FromStr(string dataItem)
        {
            string[] split = dataItem.Split(splitChar, 2);
            int groupIndex = int.Parse(split[0]);
            int pIndex = int.Parse(split[1]);
            return (meshGroup[groupIndex], pIndex);
        }

        protected override MeshPointEllipse CreateItem(string dataItem)
        {
            (Mesh mesh, int pIndex) = FromStr(dataItem);
            MeshPointEllipse ellipse = new MeshPointEllipse(mesh, pIndex)
            {
                Width = pointSize,
                Height = pointSize
            };
            clickHandler.Hook(ellipse);

            Gizmo.Canvas.Children.Add(ellipse);
            UpdateItem(dataItem, ellipse);

            return ellipse;
        }

        protected override Point DataItemToPoint(string dataItem)
        {
            (Mesh mesh, int pIndex) = FromStr(dataItem);
            Geometry.Vector2 pos = mesh.vertices[pIndex].position;
            pos = Geometry.Vector2.Transform(pos, mesh.transform);

            return new Point(pos.X, pos.Y);
        }

        protected override Point DataSpaceToVisualSpace(string dataItem)
        {
            (Mesh mesh, int pIndex) = FromStr(dataItem);
            Geometry.Vector2 pos = mesh.vertices[pIndex].position;
            pos = Geometry.Vector2.Transform(pos, mesh.transform);

            return timeline.World2Canv(new System.Windows.Media.Media3D.Point3D(pos.X, pos.Y, 0));
        }

        protected override void RemoveItem(string dataItem, MeshPointEllipse visualItem)
        {
            clickHandler.UnHook(visualItem);
            Gizmo.Canvas.Children.Remove(visualItem);
        }

        protected override void UpdateItem(string dataItem, MeshPointEllipse visualItem, Point visualSpacePos)
        {
            Gizmo.SetCanvasPos(visualItem, visualSpacePos.X, visualSpacePos.Y);
        }

        public void SetPointSize(double size)
        {
            pointSize = size;
            UpdatePointSizes();
        }

        public void UpdatePointSizes()
        {
            foreach (var visualItem in VisualItems)
            {
                visualItem.Value.Width = pointSize;
                visualItem.Value.Height = pointSize;
            }
        }


        public void Hide()
        {
            foreach (var visualItem in VisualItems)
            {
                RemoveItem(visualItem.Key, visualItem.Value);
            }
        }
    }
}
