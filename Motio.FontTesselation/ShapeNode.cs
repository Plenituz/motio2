using Motio.Geometry;
using System.Collections.Generic;
using PreShape = System.Collections.Generic.List<Motio.Geometry.Vector2>;

namespace Motio.FontTesselation
{
    internal class ShapeNode
    {
        ShapeNode parent;
        BoundingBox2D box;
        PreShape _path;
        public PreShape Path
        {
            get => _path;
            set
            {
                if (value != null)
                    box = BoundingBox2D.CreateFromPoints(value);
                _path = value;
            }
        }
        public List<ShapeNode> children = new List<ShapeNode>();

        public ShapeNode(ShapeNode parent, PreShape path)
        {
            this.parent = parent;
            this.Path = path;
        }

        public virtual bool Insert(PreShape shape)
        {
            if (FullyContains(shape))
            {
                //try to insert it into a child
                for (int i = 0; i < children.Count; i++)
                {
                    //if it has been inserted into a child, everything is ok
                    if (children[i].Insert(shape))
                        return true;
                }
                //couldnt be inserted in any children, so create one
                ShapeNode node = new ShapeNode(this, shape);
                children.Add(node);
                return true;
            }
            else if (!ShapeContainsMe(shape))//for some reason we have to invert the result, the function is probably fucked up
            {
                ShapeNode newNode = new ShapeNode(parent, shape);
                newNode.children.Add(this);

                parent.children[parent.children.IndexOf(this)] = newNode;

                parent = newNode;

                return true;
            }
            else
            {
                //shape is completly outside and doesnt englob me, so I have nothing to do with it
                return false;
            }
        }

        private bool FullyContains(PreShape shape)
        {
            BoundingBox2D shapeBox = BoundingBox2D.CreateFromPoints(shape);
            if (box.Contains(shapeBox) == ContainmentType.Contains)
            {
                //if it just intersect it means either:
                // - the other shape is englobing me
                // - the other shape is not fully inside me
                //both case we dont care
                //however if the other shape is contained inside the box, doesnt means it's actually inside
                //we still have to check point by point

                for (int i = 0; i < shape.Count; i++)
                {
                    if (!IsPointInsideShape(Path, shape[i]))
                        return false;
                }
                return true;
            }
            return false;

        }

        private bool ShapeContainsMe(PreShape shape)
        {
            BoundingBox2D shapeBox = BoundingBox2D.CreateFromPoints(shape);

            if (shapeBox.Contains(box) == ContainmentType.Contains)
            {
                for (int i = 0; i < Path.Count; i++)
                {
                    if (!IsPointInsideShape(shape, Path[i]))
                        return false;
                }
                return true;
            }
            return true;
        }

        private static bool IsPointInsideShape(PreShape path, Vector2 point)
        {
            // http://www.ecse.rpi.edu/Homepages/wrf/Research/Short_Notes/pnpoly.html
            float x = point.X;
            float y = point.Y;

            var inside = false;
            for (int i = 0, j = path.Count - 1; i < path.Count; j = i++)
            {
                float xi = path[i].X, yi = path[i].Y;
                float xj = path[j].X, yj = path[j].Y;

                var intersect = ((yi > y) != (yj > y))
                    && (x < (xj - xi) * (y - yi) / (yj - yi) + xi);
                if (intersect)
                    inside = !inside;
            }

            return inside;
        }
    }
}
