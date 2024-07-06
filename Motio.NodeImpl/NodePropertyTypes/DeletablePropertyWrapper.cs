using Motio.NodeCore;

namespace Motio.NodeImpl.NodePropertyTypes
{
    public class DeletablePropertyWrapper : GroupNodeProperty
    {
        public DeletablePropertyWrapper(Node nodeHost) : base(nodeHost)
        {
        }

        public DeletablePropertyWrapper(Node nodeHost, string description, string name)
            : base(nodeHost, description, name)
        {
        }
    }
}
