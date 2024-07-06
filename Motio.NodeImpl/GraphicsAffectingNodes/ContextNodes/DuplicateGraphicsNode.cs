using Motio.NodeCore;

namespace Motio.NodeImpl.GraphicsAffectingNodes.ContextNodes
{
    public class DuplicateGraphicsNode : ContextStarterGAffNode
    {
        public static string ClassNameStatic = "Duplicate w/ Context";
        public DuplicateGraphicsNode(GraphicsNode nodeHost) : base(nodeHost) { }
        public DuplicateGraphicsNode() { }

        public override void SetupProperties()
        {
            attachedNodes.Add(new TestContextAware(this));
        }
    }
}
