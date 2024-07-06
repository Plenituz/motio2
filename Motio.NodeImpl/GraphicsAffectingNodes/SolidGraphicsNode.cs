using Motio.Geometry;
using Motio.Graphics;
using Motio.Meshing;
using Motio.NodeCommon.Utils;
using Motio.NodeCore;
using Motio.NodeCore.Utils;
using Motio.NodeImpl.NodePropertyTypes;

namespace Motio.NodeImpl.GraphicsAffectingNodes
{
    public class SolidGraphicsNode : GraphicsAffectingNode
    {
        public static string ClassNameStatic = "Solid";
        public SolidGraphicsNode(GraphicsNode nodeHost) : base(nodeHost) { }
        public SolidGraphicsNode() { }

        public override void SetupProperties()
        {
            //self.Properties.Add("color", Props.ColorNodeProperty(self, "Color of the background", "Color"), Color.Red)
            Properties.Add("color", new ColorNodeProperty(this, "Color of the background", "Color"), Color.Red);
            Properties.Add("z_index", new FloatNodeProperty(this, "Index in Z", "Z Index"), -100);
            Properties.Add("transformable", new BoolNodeProperty(this, "Can this solid be moved with transform nodes ?", "Transformable"), false);
        }

        public override void EvaluateFrame(int frame, DataFeed dataFeed)
        {
            Vector4 color = Properties.GetValue<Color>("color", frame).ToVector4();
            
            AnimationTimeline timeline = nodeHost.GetTimeline();
            float height = timeline.CameraHeight;
            float width = timeline.CameraWidth;
            float left = -width / 2;
            float right = width / 2;
            float top = height / 2;
            float bottom = -height / 2;

            Vertex[] vertices = new Vertex[]
            {
                new Vertex(new Vector2(left, bottom), color),
                new Vertex(new Vector2(right, bottom), color),
                new Vertex(new Vector2(right, top), color),
                new Vertex(new Vector2(left, top), color)
            };

            MotioShape shape = new MotioShape();
            shape.vertices = vertices;
            shape.zIndex = (int)Properties.GetValue<float>("z_index", frame);
            shape.transformable = Properties.GetValue<bool>("transformable", frame);

            MotioShapeGroup group;
            if (!dataFeed.TryGetChannelData(POLYGON_CHANNEL, out group))
            {
                group = new MotioShapeGroup();
            }
            group.Add(shape);
            dataFeed.SetChannelData(POLYGON_CHANNEL, group);
        }
    }
}
