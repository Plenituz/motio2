using Motio.NodeCore;

namespace Motio.NodeImpl.NodePropertyTypes
{
    public class SeparatorNodeProperty : NodePropertyBase
    {
        public SeparatorNodeProperty(Node nodeHost) : base(nodeHost)
        {
        }
        public SeparatorNodeProperty(Node nodeHost, string description, string name)
            : base(nodeHost, description, name)
        {
        }

        public override bool IsKeyframable => false;

        public override object StaticValue { get; set; }

        public override void CreateAnimationNode()
        {
        }
    }
}
