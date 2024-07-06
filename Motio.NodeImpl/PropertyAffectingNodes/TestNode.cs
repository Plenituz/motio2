using System;
using System.Collections.Generic;
using Motio.Geometry;
using Motio.NodeCommon.Utils;
using Motio.NodeCore;
using Motio.NodeImpl.NodePropertyTypes;

namespace Motio.NodeImpl.PropertyAffectingNodes
{
    public class TestNode : PropertyAffectingNode
    {
        public static IList<Type> AcceptedPropertyTypes = new Type[] { typeof(object) };

        //public override string ClassName => ClassNameStatic;
        public static string ClassNameStatic = "Test";

        public override void EvaluateFrame(int frame, DataFeed dataFeed)
        {
            Properties.GetValue("tmp", frame);
        }

        public override void SetupProperties()
        {
            Properties.Add("tmp", new VectorNodeProperty(this, "desc", "name"), Vector2.Zero);
        }
    }
}
