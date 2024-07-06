using Motio.Geometry;
using System.Collections.Generic;

namespace Motio.Meshing
{
    public class MeshBuilder
    {
        public IList<Vertex> vertices = new List<Vertex>();
        public IList<int> triangles = new List<int>();
        public Mesh Mesh => new Mesh()
        {
            vertices = vertices,
            triangles = triangles
        };

        public void AddTriangle(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            FastMeshBuilder.CreateTriangle(p1, p2, p3, out Vertex[] ps, out int[] ts, vertices.Count);
            AddRangePoint(ps);
            AddRangeTriangles(ts);
        }

        public void AddLine(Vector2 from, Vector2 to, float thickness)
        {
            FastMeshBuilder.CreateLine(from, to, thickness, out Vertex[] ps, out int[] ts, vertices.Count);
            AddRangePoint(ps);
            AddRangeTriangles(ts);
        }

        public void AddCircle(Vector2 center, float radius, int nbCuts)
        {
            FastMeshBuilder.CreateCircle(center, radius, nbCuts, out Vertex[] ps, out int[] ts, vertices.Count);
            AddRangePoint(ps);
            AddRangeTriangles(ts);
        }

        void AddRangePoint(Vertex[] ps)
        {
            for (int i = 0; i < ps.Length; i++)
            {
                vertices.Add(ps[i]);
            }
        }

        void AddRangeTriangles(int[] ts)
        {
            for (int i = 0; i < ts.Length; i++)
            {
                triangles.Add(ts[i]);
            }
        }
    }
}
