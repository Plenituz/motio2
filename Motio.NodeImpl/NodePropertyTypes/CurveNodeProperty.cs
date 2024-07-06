using Motio.NodeCore;
using Motio.Pathing;

namespace Motio.NodeImpl.NodePropertyTypes
{
    public class CurveNodeProperty : NodePropertyBase
    {
        public override bool IsKeyframable => false;

        Path _staticValue;
        public override object StaticValue
        {
            get => _staticValue;
            set => _staticValue = (Path)value;
        }
        public Path Path => _staticValue;

        public CurveNodeProperty(Node nodeHost) : base(nodeHost)
        {

        }

        public CurveNodeProperty(Node nodeHost, string description, string name)
            : base(nodeHost, description, name)
        {
        }

        public override void CreateAnimationNode()
        {
            //no animation on curves yet
        }
    }
}
