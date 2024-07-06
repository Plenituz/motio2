using Motio.Geometry;
using Motio.NodeCommon;
using Motio.NodeCommon.StandardInterfaces;
using Motio.NodeCommon.Utils;
using Motio.NodeCore;
using Motio.NodeImpl.NodePropertyTypes;
using Motio.NodeImpl.PropertyAffectingNodes;
using System.Collections.Generic;

namespace Motio.NodeImpl.GraphicsAffectingNodes
{
    public class TransformGraphicsNode : GraphicsAffectingNode
    {
        public TransformGraphicsNode(GraphicsNode nodeHost) : base(nodeHost) { }
        public TransformGraphicsNode() { }

        public static string ClassNameStatic = "Transform";
        //public override string ClassName => ClassNameStatic;

        public override void SetupProperties()
        {
            //PassiveTools.Add(new DisplayAxesNodeTool(this));
            VectorNodeProperty offset = new VectorNodeProperty(
                this,
                "amount in X and Y to offset the shapes",
                "Translation");
            Properties.Add("offset", 
                offset, 
                new Vector2());
            VectorNodeProperty rotate_around = new VectorNodeProperty(
                this,
                "Rotate around this point",
                "Rotate around");
            Properties.Add("rotate_around",
                rotate_around,
                new Vector2());
            Properties.Add("rotation",
                new FloatNodeProperty(this,
                "rotation of the shapes in Z",
                "Rotation"),
                0);
            VectorNodeProperty scale_from = new VectorNodeProperty(
                this,
                "Scale from this point",
                "Scale from");
            Properties.Add("scale_from",
                scale_from,
                new Vector2());
            Properties.Add("scale",
                new VectorNodeProperty(this,
                "Scale of the object",
                "Scale"){ uniform = true },
                new Vector2(1, 1));
            Properties.Add("override", new BoolNodeProperty(this,
                "Replace the previous transform instead of appending",
                "Override"),
                false);
            Properties.AddManually("order",
                new OrderNodeProperty(this,
                "Order of the transform operations",
                "Order (Click & Drag)",
                new string[] { "Translate", "Rotate", "Scale" }));

            CopyPropertyNode copyRot = new CopyPropertyNode();
            rotate_around.TryAttachNode(copyRot);
            NodePropertyBase refRot = copyRot.Properties["ref"];
            refRot.StaticValue = offset.GetPath();
            refRot.ValidateValue(offset.GetPath());

            CopyPropertyNode copyScl = new CopyPropertyNode();
            scale_from.TryAttachNode(copyScl);
            NodePropertyBase refScl = copyScl.Properties["ref"];
            refScl.StaticValue = offset.GetPath();
            refScl.ValidateValue(offset.GetPath());

            copyRot.UserGivenName = "Copy Translation";
            copyScl.UserGivenName = "Copy Translation";


            //CopyPropertyNode copyNode = new CopyPropertyNode
            //{
            //    propertyToCopy = Properties["offset"]
            //};
            //Properties["rotate_from"].TryAttachNode(copyNode);
        }

        public override void EvaluateFrame(int frame, DataFeed dataFeed)
        {
            IEnumerable<ITransformable> transformables = dataFeed.GetDataOfType<ITransformable>();
            Properties.WaitForProperty("order");
            Vector2 offset = Properties.GetValue<Vector2>("offset", frame);
            Vector2 scale = Properties.GetValue<Vector2>("scale", frame);
            float rotation = Properties.GetValue<float>("rotation", frame);
            Vector2 rotateAround = Properties.GetValue<Vector2>("rotate_around", frame);
            Vector2 scaleFrom = Properties.GetValue<Vector2>("scale_from", frame);
            var order = Properties.Get<OrderNodeProperty>("order").items;
            bool overridePrevious = Properties.GetValue<bool>("override", frame);

            Matrix matrix = ToolBox.CreateMatrix(offset, new Vector3(0, 0, rotation), scale, rotateAround, scaleFrom, order);

            foreach (ITransformable transformable in transformables)
            {
                if (overridePrevious)
                    transformable.OverrideTransform(matrix);
                else
                    transformable.AppendTransform(matrix);
            }
        }
    }
}
