using Motio.Meshing;
using Motio.NodeCommon.Utils;
using Motio.NodeCore;

namespace Motio.NodeImpl.GraphicsAffectingNodes
{
    public class BakeTransformGraphicsNode : GraphicsAffectingNode
    {
        public static string ClassNameStatic = "Bake transform";
        public BakeTransformGraphicsNode(GraphicsNode nodeHost) : base(nodeHost) { }
        public BakeTransformGraphicsNode() { }

        public override void SetupProperties()
        {
            
        }

        public override void EvaluateFrame(int frame, DataFeed dataFeed)
        {
            bool gotData = dataFeed.TryGetChannelData(POLYGON_CHANNEL, out MotioShapeGroup group);
            if (!gotData)
                return;
            for (int i = 0; i < group.Count; i++)
            {
                group[i].BakeTransform();
            }
        }
    }
}
