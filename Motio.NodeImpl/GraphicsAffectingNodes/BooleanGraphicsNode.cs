using ClipperLib;
using Motio.Boolean;
using Motio.Debuging;
using Motio.Geometry;
using Motio.Graphics;
using Motio.Meshing;
using Motio.NodeCommon.Utils;
using Motio.NodeCore;
using Motio.NodeImpl.NodePropertyTypes;
using Poly2Tri;
using System.Collections.Generic;
using System.Linq;

namespace Motio.NodeImpl.GraphicsAffectingNodes
{
    public class BooleanGraphicsNode : GraphicsAffectingNode
    {
        public static string ClassNameStatic = "Boolean";
        public BooleanGraphicsNode(GraphicsNode nodeHost) : base(nodeHost) { }
        public BooleanGraphicsNode() { }

        public override void SetupProperties()
        {
            Properties.Add("operation",
                new DropdownNodeProperty(this, "The boolean operation to execute", "Operation",
                new[]
                {
                    "Union", "Difference", "Intersection", "XOR"
                }), "Intersection");
            Properties.Add("fill", new DropdownNodeProperty(this, "Fill type", "Fill type",
                new[]
                {
                    "Even Odd", "Non Zero", "Negative", "Positive"
                }), "Even Odd");
        }

        public override void EvaluateFrame(int frame, DataFeed dataFeed)
        {
            if (!dataFeed.TryGetChannelData(POLYGON_CHANNEL, out MotioShapeGroup group))
                return;
            if (group.Count < 2)
                return;

            Properties.WaitForProperty("fill");
            NodePropertyBase operationProp = Properties["operation"];
            string operation = Properties.GetValue<string>("operation", frame);
            string fill = Properties.GetValue<string>("fill", frame);
            Boolean.PolyFillType fillType = ToFillType(fill);
            Boolean.ClipType clipType = ToClipType(operation);

            MotioShape shape1 = group[0];
            MotioShape shape2 = group[1];

            Vector2 pos = new Vector2();
            MotioShape motioPoly = new MotioShape
            {
                vertices = new Vertex[]
                {
                    new Vertex(new Vector2(0, 0) + pos, Color.Red.ToVector4()),
                    new Vertex(new Vector2(10, 0) + pos, Color.Red.ToVector4()),
                    new Vertex(new Vector2(10, 10) + pos, Color.Red.ToVector4()),
                    new Vertex(new Vector2(0, 10) + pos, Color.Red.ToVector4())
                },
                holes = new MotioShape[]{
                    new MotioShape()
                    {
                        vertices = new Vertex[]
                        {
                            new Vertex(new Vector2(0 + 2, 0 + 2) + pos),
                            new Vertex(new Vector2(10 - 2, 0 + 2) + pos),
                            new Vertex(new Vector2(10 - 2, 10 - 2) + pos),
                            new Vertex(new Vector2(0 + 2, 10 - 2) + pos)
                        },
                        holes = new MotioShape[]
                        {
                            new MotioShape()
                            {
                                vertices = new Vertex[]
                                {
                                    new Vertex(new Vector2(0 + 4, 0 + 4) + pos),
                                    new Vertex(new Vector2(10 - 4, 0 + 4) + pos),
                                    new Vertex(new Vector2(10 - 4, 10 - 4) + pos),
                                    new Vertex(new Vector2(0 + 4, 10 - 4) + pos)
                                },
                                holes = new MotioShape[]
                                {
                                    new MotioShape()
                                    {
                                        vertices = new Vertex[]
                                        {
                                            new Vertex(new Vector2(0 + 5.2f, 0 + 5.2f) + pos),
                                            new Vertex(new Vector2(10 - 5.2f, 0 + 5.2f) + pos),
                                            new Vertex(new Vector2(10 - 5.2f, 10 - 5.2f) + pos),
                                            new Vertex(new Vector2(0 + 5.2f, 10 - 5.2f) + pos)
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            pos = new Vector2(0, -10);
            MotioShape motioPoly2 = new MotioShape
            {
                vertices = new Vertex[]
                {
                    new Vertex(new Vector2(0, 0) + pos, Color.Red.ToVector4()),
                    new Vertex(new Vector2(10, 0) + pos, Color.Red.ToVector4()),
                    new Vertex(new Vector2(10, 10) + pos, Color.Red.ToVector4()),
                    new Vertex(new Vector2(0, 10) + pos, Color.Red.ToVector4())
                },
                holes = new MotioShape[]{
                    new MotioShape()
                    {
                        vertices = new Vertex[]
                        {
                            new Vertex(new Vector2(0 + 2, 0 + 2) + pos),
                            new Vertex(new Vector2(10 - 2, 0 + 2) + pos),
                            new Vertex(new Vector2(10 - 2, 10 - 2) + pos),
                            new Vertex(new Vector2(0 + 2, 10 - 2) + pos)
                        },
                        holes = new MotioShape[]
                        {
                            new MotioShape()
                            {
                                vertices = new Vertex[]
                                {
                                    new Vertex(new Vector2(0 + 4, 0 + 4) + pos),
                                    new Vertex(new Vector2(10 - 4, 0 + 4) + pos),
                                    new Vertex(new Vector2(10 - 4, 10 - 4) + pos),
                                    new Vertex(new Vector2(0 + 4, 10 - 4) + pos)
                                },
                                holes = new MotioShape[]
                                {
                                    new MotioShape()
                                    {
                                        vertices = new Vertex[]
                                        {
                                            new Vertex(new Vector2(0 + 5.2f, 0 + 5.2f) + pos),
                                            new Vertex(new Vector2(10 - 5.2f, 0 + 5.2f) + pos),
                                            new Vertex(new Vector2(10 - 5.2f, 10 - 5.2f) + pos),
                                            new Vertex(new Vector2(0 + 5.2f, 10 - 5.2f) + pos)
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            MotioShapeGroup group2 = new MotioShapeGroup();
            group2.Add(motioPoly);
            group2.Add(motioPoly2);

            (bool success, MotioShapeGroup result) = BooleanOperation.Execute(group, group2, fillType, clipType);

            var t = typeof(BooleanOperation).Assembly.FullName;

            //List<List<Vector2>> subject = ExtractPaths2(shape1);
            //List<List<Vector2>> clip = ExtractPaths2(shape2);

            //PolyTree result = new PolyTree();
            //Clipper clipper = new Clipper();
            //clipper.AddPaths(subject, PolyType.ptSubject, true);
            //clipper.AddPaths(clip, PolyType.ptClip, true);


            operationProp.ClearError(1);
            operationProp.ClearError(2);
            //bool success = clipper.Execute(clipType, result, fillType, fillType);
            if (!success)
            {
                Logger.WriteLine("boolean operation wasn't successful");
                operationProp.SetError(1, "The boolean operation wasn't successful");
                return;
            }


            //MotioShapeGroup resultShape = UnfoldTree(result);
            dataFeed.SetChannelData(POLYGON_CHANNEL, result);

            //TriangleNet.Geometry.Polygon poly = new TriangleNet.Geometry.Polygon();
            //for (int i = 0; i < result.ChildCount; i++)
            //{
            //    PolyNode child = result.Childs[i];
            //    var contour = ToContour(child);
                
            //    DrillHoles2(poly, child);
            //    poly.Add(contour);
            //}

            //if (poly.Count == 0)
            //{
            //    group.Clear();
            //    return;
            //}

            //TriangleNet.Meshing.GenericMesher mesher = new TriangleNet.Meshing.GenericMesher();
            //TriangleNet.Meshing.IMesh imesh = mesher.Triangulate(poly);

            //PolygonSet polygons = new PolygonSet();

            //for(int i = 0; i < result.ChildCount; i++)
            //{
            //    PolyNode child = result.Childs[i];
            //    Polygon polygon = ToPolygon(child);
            //    DrillHoles(polygon, child);
            //    polygons.Add(polygon);
            //}

            //try
            //{
            //    P2T.Triangulate(polygons);
            //}
            //catch (Exception ex)
            //{
            //    Logger.WriteLine("Error triangulating:\n" + ex);
            //    operationProp.SetError(2, "Error triangulating:\n" + ex);
            //    return;
            //}

            //MeshGroup newGroup = new MeshGroup();
            //Mesh mesh = newGroup.New;
            //mesh.vertices = new List<Vertex>();
            //mesh.triangles = new List<int>();
            

            //foreach (var t in imesh.Triangles)
            //{
            //    var v1 = t.GetVertex(0);
            //    var v2 = t.GetVertex(1);
            //    var v3 = t.GetVertex(2);

            //    int c = mesh.vertices.Count;
            //    mesh.vertices.Add(new Vertex(new Vector2((float)v1.X, (float)v1.Y)));
            //    mesh.vertices.Add(new Vertex(new Vector2((float)v2.X, (float)v2.Y)));
            //    mesh.vertices.Add(new Vertex(new Vector2((float)v3.X, (float)v3.Y)));

            //    mesh.triangles.Add(c);
            //    mesh.triangles.Add(c + 1);
            //    mesh.triangles.Add(c + 2);
            //}
            //mesh.RemoveDuplicates();
            //foreach(Polygon polygon in polygons.Polygons)
            //{
            //    Mesh mesh = ToMesh(polygon);
            //    mesh.RemoveDuplicates();
            //    newGroup.Add(mesh);
            //}
            //dataFeed.SetChannelData(MESH_CHANNEL, newGroup);
        }

        private MotioShapeGroup UnfoldTree(PolyTree tree)
        {
            MotioShapeGroup shape = new MotioShapeGroup();
            for(int i = 0; i < tree.ChildCount; i++)
            {
                PolyNode node = tree.Childs[i];
                shape.Add(ToMotioShape(node));    
            }
            return shape;
        }

        private MotioShape ToMotioShape(PolyNode node)
        {
            MotioShape shape = new MotioShape()
            {
                vertices = new List<Vertex>(),
                holes = new List<MotioShape>()
            };
            for(int i = 0; i < node.Contour.Count; i++)
            {
                shape.vertices.Add(new Vertex(node.Contour[i]));
            }
            for(int i = 0; i < node.ChildCount; i++)
            {
                shape.holes.Add(ToMotioShape(node.Childs[i]));
            }
            return shape;
        }

        private void DrillHoles(Polygon poly, PolyNode node)
        {
            for(int i = 0; i < node.ChildCount; i++)
            {
                PolyNode child = node.Childs[i];
                Polygon hole = ToPolygon(child);
                DrillHoles(hole, child);
                poly.AddHole(hole);
            }
        }

        private void DrillHoles2(TriangleNet.Geometry.Polygon poly, PolyNode node)
        {
            for (int i = 0; i < node.ChildCount; i++)
            {
                PolyNode child = node.Childs[i];
                TriangleNet.Geometry.Contour hole = ToContour(child);
                DrillHoles2(poly, child);
                poly.Add(hole, true);
            }
        }

        private Polygon ToPolygon(PolyNode node)
        {
            return new Polygon(node.Contour.Select(v => new PolygonPoint(v.X, v.Y)));
        }

        private TriangleNet.Geometry.Contour ToContour(PolyNode node)
        {
            return new TriangleNet.Geometry.Contour(node.Contour.Select(v => new TriangleNet.Geometry.Vertex(v.X, v.Y)));
        }

        private Mesh ToMesh(Polygon polygon)
        {
            Mesh mesh = new Mesh
            {
                vertices = new List<Vertex>(),
                triangles = new List<int>()
            };
            foreach (DelaunayTriangle triangle in polygon.Triangles)
            {
                int c = mesh.vertices.Count;
                mesh.vertices.Add(new Vertex(new Vector2(triangle.Points[0].Xf, triangle.Points[0].Yf)));
                mesh.vertices.Add(new Vertex(new Vector2(triangle.Points[1].Xf, triangle.Points[1].Yf)));
                mesh.vertices.Add(new Vertex(new Vector2(triangle.Points[2].Xf, triangle.Points[2].Yf)));
                mesh.triangles.Add(c);
                mesh.triangles.Add(c + 1);
                mesh.triangles.Add(c + 2);
            }
            return mesh;
        }

        //private List<List<Vector2>> ExtractPaths2(MotioShape shape)
        //{
        //    IEnumerable<Vector2> GetContour(MotioShape s)
        //    {
        //        for(int i = 0; i < s.vertices.Count; i++)
        //        {
        //            yield return s.vertices[i].position;
        //        }
        //    }

        //    IEnumerable<PolyNode> ExtractChilds(MotioShape s)
        //    {
        //        for(int i = 0; i < s.holes.Count; i++)
        //        {
        //            PolyNode n = new PolyNode();
        //            n.Contour.AddRange(GetContour(s.holes[i]));
        //            n.Childs.AddRange(ExtractChilds(s.holes[i]));
        //            yield return n;
        //        }
        //    }


        //    PolyTree tree = new PolyTree();
           
        //    PolyNode node = new PolyNode();
        //    node.Contour.AddRange(GetContour(shape));
        //    node.Childs.AddRange(ExtractChilds(shape));
        //    tree.Childs.Add(node);

        //    return Clipper.PolyTreeToPaths(tree);
        //}

        private List<List<Vector2>> ExtractPaths(Mesh mesh)
        {
            IList<IList<int>> trains = EdgeSet.UnfoldAllEdges(mesh.ExternalEdges());

            //doing it by hand to avoid any overhead linq may bring
            //var ts = trains.Select(train => train.Select(i => mesh.vertices[i].position).ToList()).ToList();

            List<List<Vector2>> vectorTrains = new List<List<Vector2>>();
            for(int i = 0; i < trains.Count; i++)
            {
                IList<int> train = trains[i];
                List<Vector2> vectorTrain = new List<Vector2>();
                vectorTrains.Add(vectorTrain);
                for(int k = 0; k < train.Count; k++)
                {
                    vectorTrain.Add(mesh.vertices[train[k]].position);
                }
            }
            return vectorTrains;
        }

        private Boolean.PolyFillType ToFillType(string fill)
        {
            switch (fill[0])
            {
                case 'E':
                    return Boolean.PolyFillType.EvenOdd;
                case 'N':
                    if (fill[1] == 'o')
                        return Boolean.PolyFillType.NonZero;
                    else
                        return Boolean.PolyFillType.Negative;
                case 'P':
                    return Boolean.PolyFillType.Positive;
            }
            throw new System.Exception("invalid boolean polyfilltype");
        }

        private Boolean.ClipType ToClipType(string operation)
        {
            switch (operation[0])
            {
                case 'U':
                    return Boolean.ClipType.Union;
                case 'D':
                    return Boolean.ClipType.Difference;
                case 'I':
                    return Boolean.ClipType.Intersection;
                case 'X':
                    return Boolean.ClipType.Xor;
            }
            throw new System.Exception("invalid boolean operation");
        }
    }
}
