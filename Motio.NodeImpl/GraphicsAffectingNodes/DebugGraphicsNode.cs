using Motio.Geometry;
using Motio.Meshing;
using Motio.NodeCore;
using Motio.NodeImpl.NodePropertyTypes;
using System;
using System.Collections.Generic;
using System.Threading;
using Motio.NodeCommon.Utils;
using Motio.Pathing;

namespace Motio.NodeImpl.GraphicsAffectingNodes
{
    public class DebugGraphicsNode : GraphicsAffectingNode
    {
        public DebugGraphicsNode(GraphicsNode nodeHost) : base(nodeHost) { }
        public DebugGraphicsNode() { }

        public static string ClassNameStatic = "Debug";
        //public override string ClassName => ClassNameStatic;
        private const int DivCircle = 6;
        bool isMesh;

        public override void SetupProperties()
        {
            Properties.Add("thickness", new FloatNodeProperty(this, "Thickness of the wireframe", "Thickness"), 0.1);
            Properties.Add("radius", new FloatNodeProperty(this, "Radius of the points", "Radius"), 0.3);
            Properties.Add("from",
                new DropdownNodeProperty(this,
                "Channel to get information from", "From",
                new List<string>()
                {
                    "Mesh", "Path"
                }), "Mesh");
            Properties.Add("tmp", new BoolNodeProperty(this, "", "na"), true);
        }
        //TODO create a "prepare node" method that gets called before every batch call so you can put only the processing in
        //the evaluate frame

        //tester avec stackalloc
        //tester avec fixed
        //tester sans utiliser de struct pour les vect mais des tableaux 
        //tester si faite mesh.points[] plusieurs fois ca prend plus de temps que de le faire une fois dans une variable locale

        public override void Prepare()
        {
            base.Prepare();
            Properties.WaitForProperty("tmp");
            string selectedChannel = Properties.GetValue<string>("from", 0);
            isMesh = selectedChannel.Equals("Mesh");
        }

        private void EstimatePath(Path path, int circleCuts, out int pCount, out int tCount)
        {
            FastMeshBuilder.EstimateCircle(circleCuts, out pCount, out tCount);
            pCount *= path.Points.Count;
            tCount *= path.Points.Count;
        }

        private void EstimateMesh(Mesh mesh, int circleCuts, out int pCount, out int tCount)
        {
            FastMeshBuilder.EstimateCircle(circleCuts, out int circlePCount, out int circleTCount);
            FastMeshBuilder.EstimateLine(out int linePCount, out int lineTCount);

            pCount = circlePCount * mesh.vertices.Count + mesh.triangles.Count * linePCount;
            tCount = circleTCount * mesh.vertices.Count + mesh.triangles.Count * lineTCount;
        }

        private void EstimateMeshGroup(MeshGroup group, int circleCuts, out int pCount, out int tCount)
        {
            pCount = 0;
            tCount = 0;
            for(int i = 0; i < group.Count; i++)
            {
                EstimateMesh(group[i], circleCuts, out int thisPCount, out int thisTCount);
                pCount += thisPCount;
                tCount += thisTCount;
            }
        }

        private void EstimatePathGroup(PathGroup paths, int circleCuts, out int pCount, out int tCount)
        {
            pCount = 0;
            tCount = 0;
            for (int i = 0; i < paths.Count; i++)
            {
                EstimatePath(paths[i], circleCuts, out int thisPCount, out int thisTCount);
                pCount += thisPCount;
                tCount += thisTCount;
            }
        }

        public override void EvaluateFrame(int frame, DataFeed dataFeed)
        {
            Properties.WaitForProperty("tmp");
            Properties.WaitForProperty("radius");
            Properties.WaitForProperty("thickness");
            float radius = Properties.GetValue<float>("radius", frame);

            if (isMesh)
            {
                if (!dataFeed.ChannelExists(MESH_CHANNEL))
                    return;
                float thickness = Properties.GetValue<float>("thickness", frame);

                MeshGroup containedMesh = dataFeed.GetChannelData<MeshGroup>(MESH_CHANNEL);                

                EstimateMeshGroup(containedMesh, DivCircle, out int pCount, out int tCount);

                FastMeshBuilder builder = new FastMeshBuilder(pCount, tCount);

                for(int i = 0; i < containedMesh.Count; i++)
                {
                    DrawMesh(containedMesh[i], builder, radius, thickness);
                }
                containedMesh.Clear();
                containedMesh.Add(builder.Mesh);
            }
            else
            {
                if (!dataFeed.ChannelExists(PATH_CHANNEL))
                    return;

                PathGroup paths = dataFeed.GetChannelData<PathGroup>(PATH_CHANNEL);
                EstimatePathGroup(paths, DivCircle, out int pCount, out int tCount);
                FastMeshBuilder builder = new FastMeshBuilder(pCount, tCount);
                for(int i = 0; i < paths.Count; i++)
                {
                    DrawComplexPath(paths[i], builder, radius);
                }
                dataFeed.SetChannelData(MESH_CHANNEL, new MeshGroup(builder.Mesh));
            }
        }

        //Line TmpLine(MeshPoint from, MeshPoint to)
        //{
        //    Point fr = Global.World2Canv((Point3D)from);
        //    Point t = Global.World2Canv((Point3D)to);

        //    Line l = new Line()
        //    {
        //        X1 = fr.X,
        //        Y1 = fr.Y,
        //        X2 = t.X,
        //        Y2 = t.Y,
        //        Stroke = Brushes.Black,
        //        StrokeThickness = 1
        //    };
        //    return l;
        //}

        //tmp method that draw the debug mesh as a gizmo,
        //kept it in case the mesh creation bugs out, this one is going to work
        //void TmpDisp(Mesh mesh)
        //{
        //    Application.Current.Dispatcher.Invoke(() =>
        //    {
        //        IEnumerable<Point3D> ps = mesh.PointsWpf;
        //        Console.WriteLine(ps.Count());
        //        Console.WriteLine(mesh.points.Count);
        //        foreach (Point3D p in ps)
        //        {
        //            Ellipse ellipse = new Ellipse()
        //            {
        //                Fill = Brushes.Red,
        //                Width = 3,
        //                Height = 3
        //            };
        //            Point pCanv = Global.World2Canv(p);
        //            Gizmo.SetCanvasPos(ellipse, pCanv.X, pCanv.Y);
        //            myGizmos.Add(ellipse);
        //            Gizmo.Add(ellipse);
        //        }

        //        for (int i = 0; i < mesh.triangles.Count; i += 3)
        //        {
        //            if (mesh.points.Count < mesh.triangles[i + 2] || mesh.triangles.Count < i + 2)
        //            {
        //                throw new Exception("invalid mesh");
        //            }
        //            MeshPoint p1 = mesh.points[mesh.triangles[i]];
        //            MeshPoint p2 = mesh.points[mesh.triangles[i + 1]];
        //            MeshPoint p3 = mesh.points[mesh.triangles[i + 2]];
        //            if (float.IsNaN((float)p1.x))
        //            {

        //            }
        //            Line l1 = TmpLine(p1, p2);
        //            Line l2 = TmpLine(p2, p3);
        //            Line l3 = TmpLine(p1, p3);
        //            myGizmos.AddRange(new UIElement[] { l1, l2, l3 });
        //            Gizmo.Add(l1);
        //            Gizmo.Add(l2);
        //            Gizmo.Add(l3);
        //        }
        //    });
        //}

        void DrawMesh(Mesh mesh, FastMeshBuilder builder, float radius, float thickness)
        {
            //draw the mesh points
            for(int i = 0; i < mesh.vertices.Count; i++)
            {
                builder.AddCircle(mesh.vertices[i].position, radius, DivCircle);
            }

            if (!Properties.GetValue<bool>("tmp", 0))
                return;

            HashSet<DrawnLine> lines = new HashSet<DrawnLine>();
            //draw lines linking points
            for (int i = 0; i < mesh.triangles.Count; i += 3)
            {
                if (mesh.vertices.Count < mesh.triangles[i + 2] || mesh.triangles.Count < i + 2)
                    throw new Exception("invalid mesh");

                Vector2 p1 = mesh.vertices[mesh.triangles[i]].position;
                Vector2 p2 = mesh.vertices[mesh.triangles[i + 1]].position;
                Vector2 p3 = mesh.vertices[mesh.triangles[i + 2]].position;
                DrawnLine p1_2 = new DrawnLine(p1, p2);
                DrawnLine p2_3 = new DrawnLine(p2, p3);
                DrawnLine p3_1 = new DrawnLine(p3, p1);

                if (Properties.GetValue<bool>("tmp", 0))
                {
                    if (!lines.Contains(p1_2))
                    {
                        builder.AddLine(p1, p2, thickness);
                        lines.Add(p1_2);
                    }
                    else
                    {

                    }
                    if (!lines.Contains(p2_3))
                    {
                        builder.AddLine(p2, p3, thickness);
                        lines.Add(p2_3);
                    }
                    else
                    {

                    }
                    if (!lines.Contains(p3_1))
                    {
                        builder.AddLine(p3, p1, thickness);
                        lines.Add(p3_1);
                    }
                    else
                    {

                    }
                }
                else
                {
                    builder.AddLine(p1, p2, thickness);
                    builder.AddLine(p2, p3, thickness);
                    builder.AddLine(p3, p1, thickness);
                }



            }
        }

        private struct DrawnLine 
        {
            Vector2 from, to;

            public DrawnLine(Vector2 from, Vector2 to)
            {
                this.from = from;
                this.to = to;
            }

            public override bool Equals(object obj)
            {
                if(obj is DrawnLine other)
                {
                    return (from == other.from && to == other.to) || (from == other.to && to == other.from);
                }
                return false;
            }

            public override int GetHashCode()
            {
                var hashCode = -1951484959;
                hashCode = hashCode * -1521134295 + base.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<Vector2>.Default.GetHashCode(from);
                hashCode = hashCode * -1521134295 + EqualityComparer<Vector2>.Default.GetHashCode(to);
                return hashCode;
            }
        }

        void DrawComplexPath(Path path, FastMeshBuilder builder, float radius)
        {
            for(int i = 0; i < path.Points.Count; i++)
            {
                builder.AddCircle(path.Points[i].Position, radius, DivCircle);
            }
        }
    }
}
