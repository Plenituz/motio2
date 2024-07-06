using Motio.Geometry;
using Motio.NodeCommon.StandardInterfaces;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Motio.Meshing
{
    public class MotioShapeGroup : IEnumerable<MotioShape>, ITransformable, IDeformable, ICloneable
    {
        private List<MotioShape> polygons = new List<MotioShape>();
        public MotioShape this[int index]
        {
            get { return polygons[index]; }
            set { polygons[index] = value; }
        }
        public int Count => polygons.Count;

        public MotioShapeGroup()
        {

        }

        public MotioShapeGroup(MotioShape polygon)
        {
            polygons.Add(polygon);
        }

        public void Add(MotioShape polygon)
        {
            polygons.Add(polygon);
        }

        public void Remove(MotioShape polygon)
        {
            polygons.Remove(polygon);
        }

        public void RemoveAt(int index)
        {
            polygons.RemoveAt(index);
        }

        public void Clear()
        {
            polygons.Clear();
        }

        public int IndexOf(MotioShape polygon)
        {
            return polygons.IndexOf(polygon);
        }

        public IEnumerator<MotioShape> GetEnumerator()
        {
            return polygons.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return polygons.GetEnumerator();
        }

        public void AppendTransform(Matrix matrix)
        {
            for (int i = 0; i < polygons.Count; i++)
            {
                if (polygons[i].transformable)
                {
                    polygons[i].transform = Matrix.Multiply(polygons[i].transform, matrix);
                }
            }
        }

        public void OverrideTransform(Matrix matrix)
        {
            for (int i = 0; i < polygons.Count; i++)
            {
                if (polygons[i].transformable)
                    polygons[i].transform = matrix;
            }
        }

        public IEnumerable<Vertex> OrderedPoints
        {
            get => GetPoints();
            set => SetPoints(value);
        }

        private IEnumerable<Vertex> GetPoints()
        {
            for (int i = 0; i < polygons.Count; i++)
            {
                MotioShape shape = polygons[i];

                if (shape.deformable)
                {
                    foreach (Vertex v in GetShapeVertices(shape))
                        yield return v;
                }
            }
        }

        private static IEnumerable<Vertex> GetShapeVertices(MotioShape shape)
        {
            for(int i = 0; i < shape.vertices.Count; i++)
            {
                yield return shape.vertices[i];
            }

            for(int i = 0; i < shape.holes.Count; i++)
            {
                foreach (Vertex v in GetShapeVertices(shape.holes[i]))
                    yield return v;
            }
        }

        private void SetPoints(IEnumerable<Vertex> transformedPoints)
        {
            IEnumerator<Vertex> enumerator = transformedPoints.GetEnumerator();
            enumerator.MoveNext();
            for (int i = 0; i < polygons.Count; i++)
            {
                if (polygons[i].deformable)
                {
                    SetShapePoints(polygons[i], enumerator);
                }
            }
            if (enumerator.MoveNext())
                throw new System.Exception("still stuff in the enumerator");
        }

        private void SetShapePoints(MotioShape shape, IEnumerator<Vertex> transformedPoints)
        {

            for (int i = 0; i < shape.vertices.Count; i++)
            {
                shape.vertices[i] = transformedPoints.Current;
                transformedPoints.MoveNext();
            }

            for (int i = 0; i < shape.holes.Count; i++)
            {
                SetShapePoints(shape.holes[i], transformedPoints);
            }
        }

        public MotioShapeGroup Clone()
        {
            MotioShapeGroup group = new MotioShapeGroup();
            for (int i = 0; i < polygons.Count; i++)
            {
                group.Add(polygons[i].Clone());
            }
            return group;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }
}
