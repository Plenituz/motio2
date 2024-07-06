using PreShape = System.Collections.Generic.List<Motio.Geometry.Vector2>;

namespace Motio.FontTesselation
{
    internal class ShapeTree : ShapeNode
    {
        public ShapeTree() : base(null, null)
        {
        }

        public override bool Insert(PreShape shape)
        {
            for (int i = 0; i < children.Count; i++)
            {
                if (children[i].Insert(shape))
                    return true;
            }

            ShapeNode newNode = new ShapeNode(this, shape);
            children.Add(newNode);
            return true;
        }
    }
}
