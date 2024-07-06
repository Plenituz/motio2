using Motio.NodeCore;
using Motio.NodeImpl.NodePropertyTypes;
using Motio.NodeImpl.PropertyAffectingNodes;
using System.Collections.Generic;
using System.Threading;

namespace Motio.NodeImpl
{
    public static class NodeImplExtensions
    {
        public static AnimationPropertyNode FindAnimationNode(this NodePropertyBase self)
        {
            //extract AnimationNode
            for (int i = 0; i < self.hiddenNodes.Count; i++)
            {
                if (self.hiddenNodes[i] is AnimationPropertyNode animNode)
                {
                    return animNode;
                }
            }
            return null;
        }
    }
}
