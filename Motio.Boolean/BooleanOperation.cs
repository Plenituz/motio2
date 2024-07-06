using ClipperLib;
using Motio.Geometry;
using Motio.Meshing;
using System.Collections.Generic;

namespace Motio.Boolean
{
    public enum PolyFillType
    {
        EvenOdd,
        NonZero,
        Positive,
        Negative
    }

    public enum ClipType
    {
        Intersection,
        Union,
        Difference,
        Xor
    }

    public static class BooleanOperation
    {
        public static (bool success, MotioShapeGroup result) Execute(MotioShapeGroup subject, MotioShapeGroup clip, 
            PolyFillType fillType, ClipType clipType)
        {
            PolyTree result = new PolyTree();
            Clipper clipper = new Clipper();

            for(int i = 0; i < subject.Count; i++)
            {
                List<List<Vector2>> pathSubj = ExtractPaths2(subject[i]);
                clipper.AddPaths(pathSubj, PolyType.ptSubject, true);
            }

            for(int i = 0; i < clip.Count; i++)
            {
                List<List<Vector2>> pathClip = ExtractPaths2(clip[i]);
                clipper.AddPaths(pathClip, PolyType.ptClip, true);
            }

            bool success = clipper.Execute(
                (ClipperLib.ClipType)clipType, 
                result,
                (ClipperLib.PolyFillType)fillType, 
                (ClipperLib.PolyFillType)fillType);

            if (!success)
            {
                return (false, null);
            }

            MotioShapeGroup resultShape = UnfoldTree(result);
            return (true, resultShape);
        }

        private static List<List<Vector2>> ExtractPaths2(MotioShape shape)
        {
            IEnumerable<Vector2> GetContour(MotioShape s)
            {
                for (int i = 0; i < s.vertices.Count; i++)
                {
                    yield return s.vertices[i].position;
                }
            }

            IEnumerable<PolyNode> ExtractChilds(MotioShape s)
            {
                for (int i = 0; i < s.holes.Count; i++)
                {
                    PolyNode n = new PolyNode();
                    n.Contour.AddRange(GetContour(s.holes[i]));
                    n.Childs.AddRange(ExtractChilds(s.holes[i]));
                    yield return n;
                }
            }


            PolyTree tree = new PolyTree();

            PolyNode node = new PolyNode();
            node.Contour.AddRange(GetContour(shape));
            node.Childs.AddRange(ExtractChilds(shape));
            tree.Childs.Add(node);

            return Clipper.PolyTreeToPaths(tree);
        }

        private static MotioShapeGroup UnfoldTree(PolyTree tree)
        {
            MotioShapeGroup shape = new MotioShapeGroup();
            for (int i = 0; i < tree.ChildCount; i++)
            {
                PolyNode node = tree.Childs[i];
                shape.Add(ToMotioShape(node));
            }
            return shape;
        }

        private static MotioShape ToMotioShape(PolyNode node)
        {
            MotioShape shape = new MotioShape()
            {
                vertices = new List<Vertex>(),
                holes = new List<MotioShape>()
            };
            for (int i = 0; i < node.Contour.Count; i++)
            {
                shape.vertices.Add(new Vertex(node.Contour[i]));
            }
            for (int i = 0; i < node.ChildCount; i++)
            {
                shape.holes.Add(ToMotioShape(node.Childs[i]));
            }
            return shape;
        }
    }
}
