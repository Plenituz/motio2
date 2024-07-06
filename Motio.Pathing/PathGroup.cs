using Motio.Geometry;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Motio.Pathing
{
    public class PathGroup : IEnumerable<Path>/*,ITransformable, IDeformable*/, ICloneable
    {
        public IEnumerable<Vector2> OrderedPoints => throw new System.NotImplementedException();
        private List<Path> paths = new List<Path>();

        public Path this[int index]
        {
            get => paths[index];
            set => paths[index] = value;
        }

        public int Count => paths.Count;

        public PathGroup(Path path)
        {
            paths.Add(path);
        }

        public PathGroup()
        {

        }

        public void Add(Path path)
        {
            paths.Add(path);
        }

        public void Remove(Path path)
        {
            paths.Remove(path);
        }

        public void RemoveAt(int index)
        {
            paths.RemoveAt(index);
        }

        public void Clear()
        {
            paths.Clear();
        }

        public IEnumerator<Path> GetEnumerator()
        {
            return paths.GetEnumerator();
        }

        public void SetPoints(IEnumerable<Vector2> transformedPoints)
        {
            throw new System.NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return paths.GetEnumerator();
        }

        public PathGroup Clone()
        {
            PathGroup group = new PathGroup();
            for (int i = 0; i < paths.Count; i++)
            {
                group.Add(paths[i].Clone());
            }
            return group;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }
}
