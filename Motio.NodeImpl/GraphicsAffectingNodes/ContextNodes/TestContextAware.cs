using Motio.Geometry;
using Motio.NodeCommon.Utils;
using Motio.NodeCore;

namespace Motio.NodeImpl.GraphicsAffectingNodes.ContextNodes
{
    public class TestContextAware : ContextAwareNode
    {
        public static string ClassNameStatic = "TestContext";
        public TestContextAware(ContextStarterGAffNode nodeHost) : base(nodeHost) { }
        public TestContextAware() { }

        public override void SetupProperties()
        {

        }

        public override void EvaluateInContext(int frame, DataFeed dataFeed)
        {
            dataFeed.SetChannelData("dup_point", new Vector2(15, 15));
        }
    }
}
