using Motio.Geometry;
using Motio.NodeCommon.StandardInterfaces;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Motio.Meshing
{
    public class MeshGroup : IEnumerable<Mesh>, ITransformable, IDeformable, ICloneable
    {
        private List<Mesh> meshes = new List<Mesh>();

        public Mesh this[int index]
        {
            get { return meshes[index]; }
            set { meshes[index] = value; }
        }

        public int Count => meshes.Count;

        public Mesh First
        {
            get
            {
                if (meshes.Count > 0)
                    return meshes[0];
                else
                    return New;
            }
        }

        public Mesh New
        {
            get
            {
                Mesh mesh = new Mesh();
                meshes.Add(mesh);
                return mesh;
            }
        }

        public MeshGroup()
        {

        }

        public MeshGroup(Mesh mesh)
        {
            meshes.Add(mesh);
        }

        public void Add(Mesh mesh)
        {
            meshes.Add(mesh);
        }

        public void AddAll(MeshGroup other)
        {
            for(int i = 0; i < other.Count; i++)
            {
                Add(other[i]);
            }
        }

        public void Remove(Mesh mesh)
        {
            meshes.Remove(mesh);
        }

        public void RemoveAt(int index)
        {
            meshes.RemoveAt(index);
        }

        public void Clear()
        {
            meshes.Clear();
        }

        public int IndexOf(Mesh mesh)
        {
            return meshes.IndexOf(mesh);
        }

        public IEnumerator<Mesh> GetEnumerator()
        {
            return meshes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return meshes.GetEnumerator();
        }

        public void AppendTransform(Matrix matrix)
        {
            for(int i = 0; i < meshes.Count; i++)
            {
                if(meshes[i].transformable)
                    meshes[i].transform.Append(matrix);
            }
        }

        public void OverrideTransform(Matrix matrix)
        {
            for (int i = 0; i < meshes.Count; i++)
            {
                if (meshes[i].transformable)
                    meshes[i].transform = matrix;
            }
        }

        public IEnumerable<Vertex> OrderedPoints
        {
            get
            {
                for(int i = 0; i < meshes.Count; i++)
                {
                    if (meshes[i].deformable)
                    {
                        for (int k = 0; k < meshes[i].vertices.Count; k++)
                        {
                            yield return meshes[i].vertices[k];
                        }
                    }
                }
            }

            set
            {
                SetPoints(value);
            }
        }

        public void SetPoints(IEnumerable<Vertex> transformedPoints)
        {
            IEnumerator<Vertex> enumerator = transformedPoints.GetEnumerator();
            enumerator.MoveNext();
            for (int i = 0; i < meshes.Count; i++)
            {
                if (meshes[i].deformable)
                {
                    for (int k = 0; k < meshes[i].vertices.Count; k++)
                    {
                        Vertex current = enumerator.Current;
                        meshes[i].vertices[k] = current;
                        enumerator.MoveNext();
                    }
                }
            }
            if (enumerator.MoveNext())
                throw new System.Exception("still stuff in the enumerator");
        }

        public object Clone()
        {
            MeshGroup group = new MeshGroup();
            for(int i =0; i < meshes.Count; i++)
            {
                group.Add((Mesh)meshes[i].Clone());
            }
            return group;
        }
    }
}
