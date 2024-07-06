using Motio.NodeCore;

namespace Motio.NodeImpl.NodeTools
{
    public class MoveTool : NodeTool
    {
        public string positionProperty;

        public MoveTool(Node nodeHost, string positionProperty) : base(nodeHost)
        {
            this.positionProperty = positionProperty;
        }
    }
}
