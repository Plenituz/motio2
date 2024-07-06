using Motio.Geometry;
using Motio.Meshing;
using Motio.NodeCommon.Utils;
using Motio.NodeCore;
using Motio.NodeImpl.NodePropertyTypes;
using Motio.NodeImpl.NodeTools;
using Poly2Tri;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Motio.NodeImpl.GraphicsAffectingNodes
{
    public class PolygonGraphicsNode : GraphicsAffectingNode
    {
        public static string ClassNameStatic = "Polygon";
        public PolygonGraphicsNode(GraphicsNode nodeHost) : base(nodeHost) { }
        public PolygonGraphicsNode() { }

        protected override void SetupNode()
        {
            base.SetupNode();
            Tools.Add(new PolygonCreatorTool(this));
        }

        public override void SetupProperties()
        {
            Properties.Add("generate",
                new DropdownNodeProperty(this, "What should this node generate ?", "Generate",
                new string[] { "Mesh", "Only points" }), "Mesh");
            Properties.AddManually("point_group", 
                new GroupNodeProperty(this, "All the points of the polygon", "Points"));
        }

        public override void EvaluateFrame(int frame, DataFeed dataFeed)
        {
            GroupNodeProperty pointGroup = Properties.Get<GroupNodeProperty>("point_group");
            if (pointGroup.Properties.Count < 3)
                return;
            List<PolygonPoint> points = new List<PolygonPoint>();
            List<TriangulationPoint> triPoints = new List<TriangulationPoint>();

            for(int i = 0; i < pointGroup.Properties.Count; i++)
            {
                var deletable = pointGroup.Properties.Get<DeletablePropertyWrapper>(i);
                string name = pointGroup.Properties.GetUniqueName(deletable);
                NodePropertyBase prop;
                while(!deletable.Properties.TryGetProperty(name + "_point", out prop))
                {
                    Thread.Sleep(1);
                }
                Vector2 p = prop.GetCachedEndOfChain(frame).GetChannelData<Vector2>(PROPERTY_OUT_CHANNEL);

                points.Add(new PolygonPoint(p.X, p.Y));
                var t = new TriangulationPoint(p.X, p.Y);
                triPoints.Add(t);
            }
            int[] indexes = new int[triPoints.Count - 1];
            for(int i = 0; i < indexes.Length; i++)
            {
                indexes[i] = i;
            }

            
            PointSet set = new PointSet(triPoints);
            //ConstrainedPointSet set = new ConstrainedPointSet(triPoints, indexes);
            Polygon polygon = new Polygon(points);
            PolygonSet polygonSet = new PolygonSet(polygon);
            
            try
            {

                P2T.Triangulate(polygon);
            }
            catch (Exception)
            {
                return;
            }

            Mesh mesh = new Mesh();
            mesh.vertices = new List<Vertex>();
            mesh.triangles = new List<int>();

            

            foreach(DelaunayTriangle triangle in polygon.Triangles)
            {
                int c = mesh.vertices.Count;
                mesh.vertices.Add(new Vertex(new Vector2(triangle.Points[0].Xf, triangle.Points[0].Yf)));
                mesh.vertices.Add(new Vertex(new Vector2(triangle.Points[1].Xf, triangle.Points[1].Yf)));
                mesh.vertices.Add(new Vertex(new Vector2(triangle.Points[2].Xf, triangle.Points[2].Yf)));
                mesh.triangles.Add(c);
                mesh.triangles.Add(c + 1);
                mesh.triangles.Add(c + 2);
            }

            //foreach (Polygon poly in polygonSet.Polygons)
            //{
            //    foreach(DelaunayTriangle triangle in poly.Triangles)
            //    {
            //        int c = mesh.points.Count;
            //        mesh.points.Add(new Vector2(triangle.Points[0].Xf, triangle.Points[0].Yf));
            //        mesh.points.Add(new Vector2(triangle.Points[1].Xf, triangle.Points[1].Yf));
            //        mesh.points.Add(new Vector2(triangle.Points[2].Xf, triangle.Points[2].Yf));
            //        mesh.triangles.Add(c);
            //        mesh.triangles.Add(c + 1);
            //        mesh.triangles.Add(c + 2);
            //    }
            //}

            dataFeed.SetChannelData(MESH_CHANNEL, new MeshGroup(mesh));
        }
    }
}
