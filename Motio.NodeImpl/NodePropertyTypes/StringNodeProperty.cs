using Motio.NodeCore;
using Motio.ObjectStoring;
using PropertyChanged;

namespace Motio.NodeImpl.NodePropertyTypes
{
    public class StringNodeProperty : NodePropertyBase
    {
        public override bool IsKeyframable => false;

        public string StringValue => _staticValue;
        protected string _staticValue = "";
        public override object StaticValue
        {
            get => _staticValue;
            set => _staticValue = value.ToString();
        }

        public override void CreateAnimationNode()
        {
           
        }

        public StringNodeProperty(Node nodeHost, string description, string name)
            : base(nodeHost, description, name)
        {
        }

        public StringNodeProperty(Node nodeHost) : base(nodeHost)
        {
        }
    }
}
