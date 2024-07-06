using Motio.FontTesselation;
using Motio.Geometry;
using Motio.Meshing;
using Motio.NodeCommon.Utils;
using Motio.NodeCore;
using Motio.NodeImpl.NodePropertyTypes;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Motio.NodeImpl.NodeTools;
using Motio.NodeCommon;
using Motio.Graphics;

namespace Motio.NodeImpl.GraphicsAffectingNodes
{
    public class TextGraphicsNode : GraphicsAffectingNode
    {
        public static string ClassNameStatic = "Text";
        public TextGraphicsNode(GraphicsNode nodeHost) : base(nodeHost) { }
        public TextGraphicsNode() { }

        protected override void SetupNode()
        {
            base.SetupNode();
            PassiveTools.Add(new MoveTool(this, "position"));
        }

        public override void SetupProperties()
        {
            List<string> fonts = FontFamily.Families.Select(f => f.Name).ToList();

            Properties.Add("text", new StringNodeProperty(this, "Text to display", "Text"), "azertyuiopmlkjhgfdsqwxcvbn,;:!");
            Properties.Add("font", new DropdownNodeProperty(this, "Font", "Font", fonts), fonts[0]);
            Properties.Add("position", new VectorNodeProperty(this, "Move the text", "Position"), Vector2.Zero);
            FloatNodeProperty sizeProp = new FloatNodeProperty(this, "Font size", "Font size");
            sizeProp.SetRangeFrom(0f, true);
            sizeProp.SetRangeTo(50f, false);
            Properties.Add("size", sizeProp, 12f);
            FloatNodeProperty qualityProp = new FloatNodeProperty(this, "Font detail", "Detail");
            qualityProp.SetRangeFrom(0.1f, true);
            qualityProp.SetRangeTo(2.5f, false);
            Properties.Add("quality", qualityProp, 1f);
            Properties.Add("style",
                new DropdownNodeProperty(this, "Font style", "Font style",
                new[]
                {
                    "Regular",
                    "Bold",
                    "Italic",
                    "Underline",
                    "Strikeout"
                }),
                "Regular");
            Properties.Add("color", new ColorNodeProperty(this, "Color of the text", "Color"), Graphics.Color.DodgerBlue);
        }

        public override void EvaluateFrame(int frame, DataFeed dataFeed)
        {
            string text = Properties.GetValue<string>("text", frame);
            float size = Properties.GetValue<float>("size", frame);
            Vector2 position = Properties.GetValue<Vector2>("position", frame);
            float quality = Properties.GetValue<float>("quality", frame);
            Vector4 color = Properties.GetValue<Graphics.Color>("color", frame).ToVector4();
            FontStyle style = ToFontStyle(Properties.GetValue<string>("style", frame));

            FontFamily[] fonts = FontFamily.Families;
            List<string> fontStr = FontFamily.Families.Select(f => f.Name).ToList();
            FontFamily selectedFont = fonts[fontStr.IndexOf(Properties.GetValue<string>("font", frame))];


            MotioShapeGroup textShape = FontTesselator.Text2Poly(selectedFont, style, size * quality, text);
            Matrix translation = Matrix.CreateTranslation(new Vector3(position.X, position.Y + (size / 2), 0f));
            Matrix scale = Matrix.CreateScale(1f / quality);

            Matrix transformMat = scale;
            transformMat.Append(translation);

            for (int i = 0; i < textShape.Count; i++)
            {
                MotioShape shape = textShape[i];
                ApplyColor(shape, color);
                shape.transform = transformMat;
            }
            dataFeed.SetChannelData(POLYGON_CHANNEL, textShape);
        }

        void Empty(MotioShapeGroup into, MotioShape shape)
        {
            shape = shape.Clone();
            into.Add(new MotioShape() { vertices = shape.vertices.Select(v => new Vertex(v.position + new Vector2(6, 0))).ToList() });
            
            for(int i = 0; i < shape.holes.Count; i++)
            {
                //var t = shape.holes[i];
                ////t.vertices = t.vertices.Select(v => new Vertex(v.position + new Vector2(6, 0))).ToList();
                //shape.holes[i] = t;
                Empty(into, shape.holes[i]);
            }
        }

        private void ApplyColor(MotioShape shape, Vector4 color)
        {
            for (int i = 0; i < shape.holes.Count; i++)
            {
                ApplyColor(shape.holes[i], color);
            }
            for (int j = 0; j < shape.vertices.Count; j++)
            {
                Vertex v = shape.vertices[j];
                v.SetColor(color);
                shape.vertices[j] = v;
            }
        }

        private FontStyle ToFontStyle(string fontStyleName)
        {
            switch (fontStyleName)
            {
                case "Regular":
                    return FontStyle.Regular;
                case "Bold":
                    return FontStyle.Bold;
                case "Italic":
                    return FontStyle.Italic;
                case "Underline":
                    return FontStyle.Underline;
                case "Strikeout":
                    return FontStyle.Strikeout;
                default:
                    throw new System.Exception("invalid font style name");
            }
        }
    }
}
