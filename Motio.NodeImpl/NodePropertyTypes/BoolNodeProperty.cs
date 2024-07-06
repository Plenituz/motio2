using Motio.NodeCore;
using System;

namespace Motio.NodeImpl.NodePropertyTypes
{
    public class BoolNodeProperty : NodePropertyBase
    {
        public override bool IsKeyframable => false;

        protected bool value;
        public override object StaticValue
        {
            get => value;
            set => this.value = Convert.ToBoolean(value);
        }

        public BoolNodeProperty(Node nodeHost) : base(nodeHost)
        {
            
        }

        public BoolNodeProperty(Node nodeHost, string description, string name)
            : base(nodeHost, description, name)
        {
        }

        public override void CreateAnimationNode()
        {
            //no animation node on bool yet
        }
    }
}
