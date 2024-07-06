using Motio.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using EdgeTrain = System.Collections.Generic.IList<int>;

namespace Motio.Meshing
{
    
    /// <summary>
    /// a group of 2 ints representing an edge in a mesh.
    /// the ints are index in the points array of the mesh
    /// </summary>
    public struct EdgeSet
    {
        public int indexFirst, indexSecond;

        public EdgeSet(int indexFirst, int indexSecond)
        {
            this.indexFirst = indexFirst;
            this.indexSecond = indexSecond;
        }

        public bool Contains(int index)
        {
            return indexFirst == index || indexSecond == index;
        }

        /// <summary>
        /// if index is <see cref="indexFirst"/> returns <see cref="indexSecond"/>, and vise versa
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public int Other(int index)
        {
            if (index == indexFirst)
                return indexSecond;
            else if (index == indexSecond)
                return indexFirst;
            else
                throw new ArgumentException("given index in not in this edge");
        }

        public override string ToString()
        {
            return "[" + indexFirst + ", " + indexSecond + "]";
        }

        public override bool Equals(object obj)
        {
            if (!(obj is EdgeSet))
                return false;

            var set = (EdgeSet)obj;
            return (indexFirst == set.indexFirst && indexSecond == set.indexSecond)
                || (indexFirst == set.indexSecond && indexSecond == set.indexFirst);
        }

        public override int GetHashCode()
        {
            var hashCode = -981945910;
            //the hashcode needs to be the same for [a, b] and [b, a]
            int first = Math.Min(indexFirst, indexSecond);
            int second = Math.Max(indexFirst, indexSecond);
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + first.GetHashCode();
            hashCode = hashCode * -1521134295 + second.GetHashCode();
            return hashCode;
        }

        /// <summary>
        /// calls <see cref="UnfoldEdges(HashSet{EdgeSet})"/> until the <paramref name="edges"/> set is empty
        /// </summary>
        /// <param name="edges"></param>
        /// <returns></returns>
        public static IList<EdgeTrain> UnfoldAllEdges(HashSet<EdgeSet> edges)
        {
            List<EdgeTrain> trains = new List<EdgeTrain>();
            while (edges.Count != 0)
            {
                trains.Add(UnfoldEdges(edges));
            }
            return trains;
        }

        /// <summary>
        /// <para>
        /// unfold an edge train from <paramref name="edges"/>. Note that there may be several edge trains, 
        /// only one is given here.
        /// </para>
        /// <para>
        /// An edge train is a list of point indexes that are following each other in the edges set<br/>
        /// For example if you have edges = [[0, 1] [1, 3] [3, 7] [5, 6]]
        /// then the unfolded edge might be [0, 1, 3, 7] leaving edges = [[5, 6]]
        /// </para>
        /// </summary>
        /// <param name="edges"></param>
        /// <returns></returns>
        public static EdgeTrain UnfoldEdges(HashSet<EdgeSet> edges)
        {
            EdgeTrain train = new List<int>();
            EdgeSet first = edges.First();
            //train.Add(first.indexFirst); do not add the first index because it will be found at the end of the list
            train.Add(first.indexSecond);
            edges.Remove(first);
            int lookingFor = first.indexSecond;

            bool found = true;
            while(found)
            {
                found = false;
                foreach(EdgeSet edge in edges)
                {
                    if(edge.Contains(lookingFor))
                    {
                        edges.Remove(edge);
                        int toAdd = edge.Other(lookingFor);
                        train.Add(toAdd);
                        lookingFor = toAdd;
                        found = true;
                        break;
                    }
                }
            }
            return train;
        }

        /// <summary>
        /// returns true if the given edge train should be reversed because it's rotating in the wrong direction
        /// </summary>
        /// <param name="edgeTrain"></param>
        /// <param name="mesh"></param>
        /// <returns></returns>
        public static bool ShouldReverse(EdgeTrain train, Mesh mesh)
        {
            bool TriangleContains(int contain, int t1, int t2, int t3)
            {
                return contain == t1 || contain == t2 || contain == t3;
            }

            //return the int from t1, t2 or t3 that is not equal to first or second
            int TriangleOther(int first, int second, int t1, int t2, int t3)
            {
                int r1, r2;
                if (first == t1)
                {
                    r1 = t2; r2 = t3;
                }
                else if (first == t2)
                {
                    r1 = t1; r2 = t3;
                }
                else if (first == t3)
                {
                    r1 = t1; r2 = t2;
                }
                else
                {
                    throw new ArgumentException(first + " not in " + t1 + ", " + t2 + ", " + t3);
                }

                int last;
                if (second == r1)
                    last = r2;
                else if (second == r2)
                    last = r1;
                else
                    throw new ArgumentException(second + " not in " + r1 + ", " + r2);
                return last;
            }

            int FindThirdPoint(int index1, int index2)
            {
                for (int i = 0; i < mesh.triangles.Count; i += 3)
                {
                    int t1 = mesh.triangles[i], t2 = mesh.triangles[i + 1], t3 = mesh.triangles[i + 2];
                    if (TriangleContains(index1, t1, t2, t3)
                       && TriangleContains(index2, t1, t2, t3))
                    {
                        return TriangleOther(index1, index2, t1, t2, t3);
                    }
                }
                throw new ArgumentException("not triangle found for edge " + index1 + ", " + index2);
            }



            if (train.Count <= 2)
                throw new ArgumentException("train is too small");

            int thirdPoint = FindThirdPoint(train[0], train[1]);

            Vector2 point = mesh.vertices[train[0]].position;
            Vector2 next = mesh.vertices[train[1]].position;
            Vector2 third = mesh.vertices[thirdPoint].position;

            Vector2 normal1 = point - next;
            normal1.Rotate90Deg();
            Vector2 normal2 = -normal1;

            float distance1 = Vector2.DistanceSquared(third, normal1 + point);
            float distance2 = Vector2.DistanceSquared(third, normal2 + point);

            return distance1 > distance2;
        }
    }
}
