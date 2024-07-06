using Motio.Geometry;
using System.Collections.Generic;
using TriangleNet.Geometry;

namespace Motio.Meshing
{
    public static class MotioShape2Mesh
    {

        public static MeshGroup Convert(MotioShape shape)
        {
            if (shape.vertices.Count == 0)
                return new MeshGroup();
            MeshGroup group = new MeshGroup();
            Populate(group, shape);
            return group;
        }

        private static void Populate(MeshGroup group, MotioShape motioPoly)
        {
            Polygon poly = new Polygon();
            poly.Add(new Contour(ConvertVertices(motioPoly.vertices)));

            for (int i = 0; i < motioPoly.holes.Count; i++)
            {
                Contour hole = new Contour(ConvertVertices(motioPoly.holes[i].vertices));
                poly.Add(hole, hole: true);
            }

            Mesh mesh = ToMesh(poly, motioPoly);
            group.Add(mesh);

            for (int i = 0; i < motioPoly.holes.Count; i++)
            {
                MotioShape hole = motioPoly.holes[i];
                for (int k = 0; k < hole.holes.Count; k++)
                {
                    Populate(group, hole.holes[k]);
                }
            }
        }

        private static TriangleNet.Geometry.Vertex[] ConvertVertices(IList<Geometry.Vertex> verticesIn)
        {
            TriangleNet.Geometry.Vertex[] verts = new TriangleNet.Geometry.Vertex[verticesIn.Count];
            for (int i = 0; i < verticesIn.Count; i++)
            {
                Geometry.Vertex v = verticesIn[i];
                //8 attribute for color4/normal2/uv2
                TriangleNet.Geometry.Vertex newVert = new TriangleNet.Geometry.Vertex(v.position.X, v.position.Y, 0, 8);
                PopulateAttributes(newVert, v);
                verts[i] = newVert;
            }
            return verts;
        }

        private static Mesh ToMesh(Polygon poly, MotioShape originalShape)
        {
            TriangleNet.Meshing.GenericMesher mesher = new TriangleNet.Meshing.GenericMesher(new TriangleNet.Meshing.Algorithm.SweepLine());
            TriangleNet.Meshing.IMesh imesh = mesher.Triangulate(poly);

            Mesh mesh = new Mesh
            {
                vertices = new Geometry.Vertex[imesh.Triangles.Count * 3],
                triangles = new int[imesh.Triangles.Count * 3],
                shader = originalShape.shader,
                transform = originalShape.transform,
                transformable = originalShape.transformable,
                deformable = originalShape.deformable,
                zIndex = originalShape.zIndex
                //dont copy generation
            };

            int vertCount = 0;
            int triCount = 0;
            foreach (var t in imesh.Triangles)
            {
                var v1 = t.GetVertex(0);
                var v2 = t.GetVertex(1);
                var v3 = t.GetVertex(2);

                int c = vertCount;
                mesh.vertices[vertCount++] = MakeVertexWAttributes(v1);
                mesh.vertices[vertCount++] = MakeVertexWAttributes(v2);
                mesh.vertices[vertCount++] = MakeVertexWAttributes(v3);

                mesh.triangles[triCount++] = c;
                mesh.triangles[triCount++] = c + 1;
                mesh.triangles[triCount++] = c + 2;
            }
            //TODO may or may not be needed anymore ?
            //it probably saves time when sending data to gpu tho 
            mesh.RemoveDuplicates();
            return mesh;
        }

        private static Geometry.Vertex MakeVertexWAttributes(TriangleNet.Geometry.Vertex attribs)
        {
            return new Geometry.Vertex(
                position: new Vector2((float)attribs.X, (float)attribs.Y),
                color: new Vector4((float)attribs.Attributes[0], (float)attribs.Attributes[1], (float)attribs.Attributes[2], (float)attribs.Attributes[3]),
                normal: new Vector2((float)attribs.Attributes[4], (float)attribs.Attributes[5]),
                uv: new Vector2((float)attribs.Attributes[6], (float)attribs.Attributes[7])
                );
        }

        private static void PopulateAttributes(TriangleNet.Geometry.Vertex populate, Geometry.Vertex vertex)
        {
            populate.Attributes[0] = vertex.color.X;
            populate.Attributes[1] = vertex.color.Y;
            populate.Attributes[2] = vertex.color.Z;
            populate.Attributes[3] = vertex.color.W;

            populate.Attributes[4] = vertex.normal.X;
            populate.Attributes[5] = vertex.normal.Y;

            populate.Attributes[6] = vertex.uv.X;
            populate.Attributes[7] = vertex.uv.Y;
        }
    }
}
