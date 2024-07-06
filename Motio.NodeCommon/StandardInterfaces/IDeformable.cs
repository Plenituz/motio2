using Motio.Geometry;
using System.Collections.Generic;

namespace Motio.NodeCommon.StandardInterfaces
{
    public interface IDeformable //TODO this is not optimal, too much copying, we should get the references directly, Pointers ?
    {
        IEnumerable<Vertex> OrderedPoints { get; set; }
        //void SetPoints(IEnumerable<Vector2> transformedPoints);
    }
}
