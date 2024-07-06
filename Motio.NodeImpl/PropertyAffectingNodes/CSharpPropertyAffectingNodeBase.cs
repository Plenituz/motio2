using Motio.NodeCommon.StandardInterfaces;
using Motio.NodeCommon.Utils;
using Motio.NodeCore;
using System;
using System.Collections.Generic;

namespace Motio.NodeImpl.PropertyAffectingNodes
{
    public class CSharpPropertyAffectingNodeBase : PropertyAffectingNode, IDynamicNode
    {
        public static IList<Type> AcceptedPropertyTypes = new Type[] { typeof(object) };


        public override void SetupProperties()
        {
        }

        public override void EvaluateFrame(int frame, DataFeed dataFeed)
        {

        }
    }
}
