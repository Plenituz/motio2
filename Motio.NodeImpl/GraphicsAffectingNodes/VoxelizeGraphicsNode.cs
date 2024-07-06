using Motio.Geometry;
using Motio.Meshing;
using Motio.NodeCommon.Utils;
using Motio.NodeCore;
using Motio.NodeImpl.NodePropertyTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Motio.NodeImpl.GraphicsAffectingNodes
{
    public class VoxelizeGraphicsNode : GraphicsAffectingNode
    {
        public static string ClassNameStatic = "Voxelize (old)";

        public override IFrameRange IndividualCalculationRange => throw new NotImplementedException();


        public VoxelizeGraphicsNode(GraphicsNode nodeHost) : base(nodeHost) { }
        public VoxelizeGraphicsNode() { }

        public override void SetupProperties()
        {
            Properties.Add("size",
                new VectorNodeProperty(this, "Size of a box", "Voxel size") { uniform = true },
                new Vector2(1, 1));
            Properties.Add("remove_duplicates",
                new BoolNodeProperty(this, 
                "Remove duplicate points in the mesh, will make the processing time significantly longer",
                "Remove Duplicates"),
                false);
        }

        private void FindMeshBounds(Mesh mesh, out Vector2 meshMin, out Vector2 meshMax)
        {
            float mesh_minX = float.MaxValue;
            float mesh_minY = float.MaxValue;
            float mesh_maxX = float.MinValue;
            float mesh_maxY = float.MinValue;

            for (int i = 0; i < mesh.triangles.Count; i += 3)
            {
                Vector2 p0 = mesh.vertices[mesh.triangles[i]].position;
                Vector2 p1 = mesh.vertices[mesh.triangles[i + 1]].position;
                Vector2 p2 = mesh.vertices[mesh.triangles[i + 2]].position;
                float minX = Math.Min(p0.X, Math.Min(p1.X, p2.X));
                float minY = Math.Min(p0.Y, Math.Min(p1.Y, p2.Y));
                float maxX = Math.Max(p0.X, Math.Max(p1.X, p2.X));
                float maxY = Math.Max(p0.Y, Math.Max(p1.Y, p2.Y));
                if (minX < mesh_minX)
                    mesh_minX = minX;
                if (minY < mesh_minY)
                    mesh_minY = minY;
                if (maxX > mesh_maxX)
                    mesh_maxX = maxX;
                if (maxY > mesh_maxY)
                    mesh_maxY = maxY;
            }
            meshMin = new Vector2(mesh_minX, mesh_minY);
            meshMax = new Vector2(mesh_maxX, mesh_maxY);
        }

        private void BakeTransform(Mesh mesh)
        {
            Matrix matrix = mesh.transform;
            for(int i = 0; i < mesh.vertices.Count; i++)
            {
                Vertex point = mesh.vertices[i];
                Vector2.Transform(ref point.position, ref matrix, out point.position);
                mesh.vertices[i] = point;
            }
        }

        private Mesh CreateMesh(List<Vertex> squareCenters, Vector2 rad)
        {
            int pCount = 4 * squareCenters.Count;
            int tCount = 6 * squareCenters.Count;
            int pAddAt = 0;
            int tAddAt = 0;
            Mesh newMesh = new Mesh
            {
                triangles = new int[tCount],
                vertices = new Vertex[pCount]
            };
            for (int i = 0; i < squareCenters.Count; i++)
            {
                Vertex center = squareCenters[i];
                float left = center.position.X - rad.X;
                float right = center.position.X + rad.X;
                float top = center.position.Y + rad.Y;
                float bottom = center.position.Y - rad.Y;
                Vector2 p0 = new Vector2(left, bottom);
                Vector2 p1 = new Vector2(right, bottom);
                Vector2 p2 = new Vector2(right, top);
                Vector2 p3 = new Vector2(left, top);
                int c = pAddAt; //c is p0
                newMesh.vertices[pAddAt++] = new Vertex(p0, center.color);
                newMesh.vertices[pAddAt++] = new Vertex(p1, center.color);
                newMesh.vertices[pAddAt++] = new Vertex(p2, center.color);
                newMesh.vertices[pAddAt++] = new Vertex(p3, center.color);

                newMesh.triangles[tAddAt++] = c;
                newMesh.triangles[tAddAt++] = c + 1;
                newMesh.triangles[tAddAt++] = c + 2;

                newMesh.triangles[tAddAt++] = c + 2;
                newMesh.triangles[tAddAt++] = c + 3;
                newMesh.triangles[tAddAt++] = c;
            }
            //if (Properties.GetValue<bool>("remove_duplicates", frame))
            //    newMesh.RemoveDuplicates();
            return newMesh;
        }

        private List<Vector2> KeepOnlyEdge(HashSet<Vector2> squareCenters, Vector2 gapSize)
        {
            List<Vector2> edges = new List<Vector2>();
            Vector2 toRight = new Vector2(gapSize.X, 0);
            Vector2 toTop = new Vector2(0, gapSize.Y);

            foreach (Vector2 center in squareCenters)
            {
                Vector2 left = center - toRight;
                Vector2 right = center + toRight;
                Vector2 bottom = center - toTop;
                Vector2 top = center + toTop;

                //if any side is missing, concider it an edge
                if(    !squareCenters.Contains(left) 
                    || !squareCenters.Contains(right)
                    || !squareCenters.Contains(bottom)
                    || !squareCenters.Contains(top))
                {
                    edges.Add(center);
                }
            }
            return edges;
        }

        public override void EvaluateFrame(int frame, DataFeed dataFeed)
        {
            //TODO do the bucket thing (that is done in remove duplicates) here directly instead of using Hashset
            bool gotData = dataFeed.TryGetChannelData(MESH_CHANNEL, out MeshGroup group);
            if (!gotData)
                return;

            Vector2 size = Properties.GetValue<Vector2>("size", frame);
            Vector2 rad = size / 2;
            Vector3 direction = Vector3.UnitZ * -1;
            HashSet<Vertex> squareCenters = new HashSet<Vertex>();

            if(size.X <= 0 || size.Y <= 0)
            {
                Properties["size"].SetError(1, "Value must be greater than 0");
                return;
            }
            else
            {
                Properties["size"].ClearError(1);
            }

            for(int i = 0; i < group.Count; i++)
            {
                Mesh mesh = group[i];
                BakeTransform(mesh);
                FindMeshBounds(mesh, out Vector2 meshMin, out Vector2 meshMax);
                for(float x = meshMin.X; x < meshMax.X; x += size.X)
                {
                    for(float y = meshMin.Y; y < meshMax.Y; y += size.Y)
                    {
                        Vector2 pos = new Vector2(x, y);
                        Ray ray = new Ray(new Vector3(pos, 1f), direction);
                        bool hit = mesh.HitTestParallel(ray, out float dist, out int index);
                        if (hit)
                        {
                            Vertex vt1 = mesh.vertices[mesh.triangles[index]];
                            Vertex vt2 = mesh.vertices[mesh.triangles[index+1]];
                            Vertex vt3 = mesh.vertices[mesh.triangles[index+2]];
                            Vertex result = new Vertex(pos, (vt1.color + vt2.color + vt3.color) / 3f);

                            squareCenters.Add(result);
                        }
                    }
                }
            }

            //List<Vector2> edges = KeepOnlyEdge(squareCenters, size);
            Mesh newMesh = CreateMesh(squareCenters.ToList(), rad);
            newMesh.RemoveDuplicates(size.X);

            dataFeed.SetChannelData(MESH_CHANNEL, new MeshGroup(newMesh));
        }
    }
}
