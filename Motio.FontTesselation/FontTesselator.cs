using Motio.Geometry;
using Motio.Meshing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using PreShape = System.Collections.Generic.List<Motio.Geometry.Vector2>;

namespace Motio.FontTesselation
{
    /// <summary>
    /// transform a font into its polygon representation.
    /// For faster performance, call <see cref="Text2Poly(FontFamily, FontStyle, float, string)"/> with one letter at a time
    /// </summary>
    public static class FontTesselator
    {
        /// <summary>
        /// Transform a text into the MotioShape reprensting it. 
        /// Since each polygon inside the shape has to be tested against all the others, it's better to 
        /// call this with one letter at a time instead of a whole text at once
        /// </summary>
        /// <param name="font"></param>
        /// <param name="style"></param>
        /// <param name="fontSize"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static MotioShapeGroup Text2Poly(FontFamily font, FontStyle style, float fontSize, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new MotioShapeGroup();

            List<PreShape> shapes = ExtractUnorderedShapes(font, style, fontSize, text);
            ShapeTree rough = GroupShapes(shapes);
            MotioShapeGroup shapeGroup = ToShapeGroup(rough);
            return shapeGroup;
        }

        private static MotioShapeGroup ToShapeGroup(ShapeTree tree)
        {
            MotioShapeGroup group = new MotioShapeGroup();

            for(int i = 0; i < tree.children.Count; i++)
            {
                group.Add(ToShape(tree.children[i]));
            }
            return group;
        }

        private static MotioShape ToShape(ShapeNode node)
        {
            MotioShape shape = new MotioShape
            {
                holes = new List<MotioShape>(),
                vertices = new List<Vertex>()
            };

            for (int i = 0; i < node.Path.Count; i++)
            {
                shape.vertices.Add(new Vertex(node.Path[i]));
            }

            for(int i = 0; i < node.children.Count; i++)
            {
                shape.holes.Add(ToShape(node.children[i]));
            }
            return shape;
        }

        /// <summary>
        /// transform the list of unordered path into a <see cref="ShapeTree"/> by testing which shape is inside which
        /// other shape
        /// </summary>
        /// <param name="shapes"></param>
        /// <returns></returns>
        private static ShapeTree GroupShapes(List<PreShape> shapes)
        {
            ShapeTree root = new ShapeTree();
            for(int i = 0; i < shapes.Count; i++)
            {
                root.Insert(shapes[i]);
            }
            return root;
        }

        /// <summary>
        /// this comes from www.dupuis.me/node/17
        /// transforms the font into a list of unordered path
        /// </summary>
        /// <param name="fontFamily"></param>
        /// <param name="style"></param>
        /// <param name="fontSize"></param>
        /// <param name="glyph"></param>
        /// <returns></returns>
        private static List<PreShape> ExtractUnorderedShapes(FontFamily fontFamily, FontStyle style, float fontSize, string glyph)
        {
            PointF[] pts = null;
            byte[] ptsType = null;

            using (var path = new GraphicsPath())
            {
                path.AddString(glyph, fontFamily, (int)style, fontSize,
                    new PointF(0f, 0f), StringFormat.GenericDefault);

                path.Flatten();

                if (path.PointCount == 0)
                    return new List<PreShape>();

                pts = path.PathPoints;
                ptsType = path.PathTypes;
            }

            List<PreShape> polygons = new List<PreShape>();
            PreShape points = null;
            var start = -1;

            for (var i = 0; i < pts.Length; i++)
            {
                var pointType = ptsType[i] & 0x07;
                if (pointType == 0)
                {
                    points = new PreShape { new Vector2(pts[i].X, -pts[i].Y) };
                    start = i;
                    continue;
                }
                if (pointType != 1)
                    throw new Exception("Unsupported point type");

                if ((ptsType[i] & 0x80) != 0)
                {
                    //- Last point in the polygon
                    if (pts[i] != pts[start])
                    {
                        points.Add(new Vector2(pts[i].X, -pts[i].Y));
                    }
                    polygons.Add(points);
                    points = null;
                }
                else
                {
                    points.Add(new Vector2(pts[i].X, -pts[i].Y));
                }
            }

            return polygons;
        }
    }
}
