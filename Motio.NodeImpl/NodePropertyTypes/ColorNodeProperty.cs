using Motio.Graphics;
using Motio.NodeCore;

namespace Motio.NodeImpl.NodePropertyTypes
{
    public class ColorNodeProperty : NodePropertyBase
    {
        public override bool IsKeyframable => false;

        protected Color color;
        public override object StaticValue
        {
            get => color;
            set => color = (Color)value;
        }
        public Color ColorValue => color;

        public ColorNodeProperty(Node nodeHost) : base(nodeHost)
        {
        }

        public ColorNodeProperty(Node nodeHost, string description, string name)
            : base(nodeHost, description, name)
        {
        }

        public override void CreateAnimationNode()
        {
            //not animatable yet
        }
    }
}
