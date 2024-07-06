using Motio.NodeCommon.StandardInterfaces;
using Motio.NodeCommon.Utils;
using Motio.NodeCore;

namespace Motio.NodeImpl.GraphicsAffectingNodes
{
    public class CSharpGraphicsAffectingNodeBase : GraphicsAffectingNode, IDynamicNode
    {
        //public override string ClassName => "Error ClassName";

        public CSharpGraphicsAffectingNodeBase(GraphicsNode node): base(node)
        {
        }
        protected CSharpGraphicsAffectingNodeBase()
        {
        }

        public override void SetupProperties()
        {

        }

        public override void EvaluateFrame(int frame, DataFeed dataFeed)
        {
            
        }
    }
}
