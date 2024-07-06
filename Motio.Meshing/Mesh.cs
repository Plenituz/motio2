using Motio.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EdgeTrain = System.Collections.Generic.IList<int>;

namespace Motio.Meshing
{
    public class Mesh : ICloneable
    {
        /// <summary>
        /// number of time this mesh has been cloned. Used to check data integrity, if this generation is one more
        /// <see cref="normalsFromGeneration"/> then the normals are good to use (unless you modified the points yourself)
        /// </summary>
        public int generation = 0;
        /// <summary>
        /// indicate at what generation the normals have been generated, if you set the normals manually 
        /// you need to set this value manually too. However <see cref="CalculateNormals"/> does it for you.
        /// you can use <see cref="UpdateNormalsGeneration"/> set this value to <see cref="generation"/>
        /// </summary>
        public int normalsFromGeneration = -1;

        /// <summary>
        /// name of the shader to use to render this mesh. shaders are found in <c>[motio root]/Shaders/</c>
        /// The name must follow "[vertex shader name]_[fragment shader name]"
        /// </summary>
        public string shader = "regular_vertexColor";

        /// <summary>
        /// This is better if it's a native c# Array of vertex, it avoids 
        /// </summary>
        public IList<Vertex> vertices = new Vertex[0];

        public IList<int> triangles = new int[0];
        
        public Matrix transform = Matrix.Identity;
        /// <summary>
        /// if false transformations won't apply to this mesh
        /// </summary>
        public bool transformable = true;
        /// <summary>
        /// if false deformations won't apply to this mesh
        /// </summary>
        public bool deformable = true;
        /// <summary>
        /// <para>The higher the closer to the camera.</para>
        /// <para>Note that a value too low or too high will make the mesh 
        /// go invisible due to it being outside the camera rendering region</para>
        /// </summary>
        public int zIndex = 0;

        /// <summary>
        /// apply the ZIndex to the <see cref="transform"/> matrix
        /// </summary>
        public void ApplyConditions(float renderZIndex)
        {
            Vector3 translation = transform.Translation;
            transform.Translation = new Vector3(translation.X, translation.Y, renderZIndex);
        }

        /// <summary>
        /// sets <see cref="normalsFromGeneration"/> to <see cref="generation"/>
        /// </summary>
        public void UpdateNormalsGeneration()
        {
            normalsFromGeneration = generation;
        }

        public bool ShouldCalculateNormals()
        {
            return normalsFromGeneration < generation - 1;
        }

        public object Clone()
        {
            Mesh mesh = new Mesh
            {
                generation = this.generation + 1,
                normalsFromGeneration = this.normalsFromGeneration,
                vertices = this.vertices.ToArray(),
                triangles = this.triangles.ToArray(),
                shader = this.shader,
                transform = this.transform,
                transformable = this.transformable,
                deformable = this.deformable,
                zIndex = this.zIndex
            };
            return mesh;
        }

        public HashSet<EdgeSet> ExternalEdges()
        {
            HashSet<EdgeSet> edges = new HashSet<EdgeSet>();

            void AddOrRemoveEdge(int i1, int i2)
            {
                EdgeSet e1 = new EdgeSet(triangles[i1], triangles[i2]);
                //if the edge is duplicate remove it
                if (edges.Contains(e1))
                    edges.Remove(e1);
                else
                    edges.Add(e1);
            }

            for(int i = 0; i < triangles.Count; i += 3)
            {
                AddOrRemoveEdge(i, i + 1);
                AddOrRemoveEdge(i + 1, i + 2);
                AddOrRemoveEdge(i + 2, i);
            }
            return edges;
        }

        /// <summary>
        /// Bake transform on <see cref="vertices"/>
        /// </summary>
        public void BakeTransform()
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                Vertex v = vertices[i];
                v.SetPos(Vector2.Transform(v.position, transform));
                vertices[i] = v;
            }
            transform = Matrix.Identity;
        }


        /// <summary>
        /// calculate the points normals of this mesh
        /// </summary>
        public void CalculateNormals()
        {
            HashSet<EdgeSet> edges = ExternalEdges();
            IList<EdgeTrain> trains = EdgeSet.UnfoldAllEdges(edges);

            for(int trainIndex = 0; trainIndex < trains.Count; trainIndex++)
            {
                EdgeTrain train = trains[trainIndex];
                int sign = EdgeSet.ShouldReverse(train, this) ? 1 : -1;

                // we have to calculate the first and last point manually
                // because the neighbour before needs the last point of the list
                int train0 = train[0];
                Vertex vertex = vertices[train0];
                vertex.normal = CalculateNormal(train0, train[train.Count - 1], train[1])*sign;
                vertices[train0] = vertex;

                for (int i = 1; i < train.Count - 1; i++)
                {
                    vertex = vertices[train[i]];
                    vertex.normal = CalculateNormal(train[i], train[i - 1], train[i + 1])*sign;
                    vertices[train[i]] = vertex;
                }

                vertex = vertices[train[train.Count - 1]];
                vertex.normal = CalculateNormal(train[train.Count - 1], train[train.Count - 2], train0);
                vertices[train[train.Count - 1]] = vertex;
            }
            normalsFromGeneration = generation;

            Vector2 CalculateNormal(int pIndex, int beforeIndex, int afterIndex)
            {
                Vector2 before = vertices[beforeIndex].position;
                Vector2 after = vertices[afterIndex].position;
                Vector2 point = vertices[pIndex].position;

                Vector2 normalBefore = before - point;
                normalBefore.Rotate90Deg();
                Vector2 normalAfter = point - after;
                normalAfter.Rotate90Deg();

                Vector2 normal = normalBefore + normalAfter;
                normal.Normalize();
                return normal;
            }
        }

        public void RemoveDuplicates(float bucketStep = 0.1f)
        {
            //TODO compare with the other one
            Vertex[] newVertices = new Vertex[vertices.Count];
            int[] old2new = new int[vertices.Count];
            int newSize = 0;

            // Find AABB
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < vertices.Count; i++)
            {
                Vector2 vI = vertices[i].position;

                if (vI.X < min.X) min.X = vI.X;
                if (vI.Y < min.Y) min.Y = vI.Y;
                if (vI.X > max.X) max.X = vI.X;
                if (vI.Y > max.Y) max.Y = vI.Y;
            }
            min -= Vector2.One * 0.111111f;
            max += Vector2.One * 0.899999f;

            // Make cubic buckets, each with dimensions "bucketStep"
            int bucketSizeX = (int)Math.Floor((max.X - min.X) / bucketStep) + 1;
            int bucketSizeY = (int)Math.Floor((max.Y - min.Y) / bucketStep) + 1;
            List<int>[,] buckets = new List<int>[bucketSizeX, bucketSizeY];

            // Make new vertices
            for (int i = 0; i < vertices.Count; i++)
            {
                // Determine which bucket it belongs to
                Vertex vI = vertices[i];
                int x = (int)Math.Floor((vI.position.X - min.X) / bucketStep);
                int y = (int)Math.Floor((vI.position.Y - min.Y) / bucketStep);

                // Check to see if it's already been added
                List<int> bucket = buckets[x, y];
                if (bucket == null)
                {
                    bucket = new List<int>();// Make buckets lazily
                    buckets[x, y] = bucket; 
                }

                for (int j = 0; j < bucket.Count; j++)
                {
                    int bucketIndex = bucket[j];
                    Vector2 to = newVertices[bucketIndex].position - vI.position;
                    if (to.LengthSquared() < 0.001f)
                    {
                        old2new[i] = bucketIndex;
                        goto skip; // Skip to next old vertex if this one is already there
                    }
                }

                // Add new vertex
                newVertices[newSize] = vI;
                bucket.Add(newSize);
                old2new[i] = newSize;
                newSize++;

                skip:;
            }

            // Make new triangles
            int[] newTris = new int[triangles.Count];
            for (int i = 0; i < triangles.Count; i++)
            {
                newTris[i] = old2new[triangles[i]];
            }

            Vertex[] finalVertices = new Vertex[newSize];
            for (int i = 0; i < newSize; i++)
                finalVertices[i] = newVertices[i];

            
            vertices = finalVertices;
            triangles = newTris;

            // Debug.LogFormat("Weld vert count: {0} vs. {1}", newSize, oldVertices.Length);
        }


        /// <summary>
        /// remove duplicate points in the mesh and replace them by triangles. 
        /// </summary>
        //public void RemoveDuplicates()
        //{
        //    void ReplaceInTriangles(int indexToReplace, int byThisIndex)
        //    {
        //        for (int i = 0; i < triangles.Count; i++)
        //        {
        //            if (triangles[i] == indexToReplace)
        //                triangles[i] = byThisIndex;
        //            if (triangles[i] > indexToReplace)
        //                triangles[i]--;
        //        }
        //    }
        //    IList<Vertex> vertices;
        //    bool fixedSize = true;
        //    try
        //    {
        //        this.vertices.Add(new Vertex());
        //        fixedSize = false;
        //        this.vertices.RemoveAt(this.vertices.Count - 1);
        //    }
        //    catch { }

        //    if (fixedSize)
        //        vertices = this.vertices.ToList();
        //    else
        //        vertices = this.vertices;


        //    //ajouter les points a un ordered hashset au fur et a mesure
        //    //si on trouve un point duplicate on le supprime et on retient
        //    //son numero de triangle et a la fin on remplace tous les triangles

        //    OrderedDictionary<Vector2, int> seen = new OrderedDictionary<Vector2, int>();
            
        //    for(int i = 0; i < vertices.Count; i++)
        //    {
        //        Vector2 p = vertices[i].position;
        //        bool got = seen.TryGetValue(p, out int byThisIndex);
        //        if (got)
        //        {
        //            //index a remplacer dans la liste par l'index de l'autre
        //            int indexToReplace = i;
        //            //replace indexToReplace by byThisIndex in the triangles array
        //            vertices.RemoveAt(i);
        //            i--;
        //            ReplaceInTriangles(indexToReplace, byThisIndex);
        //        }
        //        else
        //        {
        //            seen.Add(p, i);
        //        }
        //    }

        //    this.vertices = vertices;
        //}

        /// <summary>
        /// hit test the mesh
        /// </summary>
        /// <param name="ray">Ray to test the mesh against</param>
        /// <param name="distance">if hit, distance from the ray to the hit</param>
        /// <param name="tIndex">if hit, index of the first point of the triangle in the triangle array</param>
        /// <returns>true if the ray hit</returns>
        public bool HitTest(Ray ray, out float distance, out int tIndex)
        {
            for (int i = 0; i < triangles.Count; i += 3)
            {
                Vector2 p0 = vertices[triangles[i]].position;
                Vector2 p1 = vertices[triangles[i + 1]].position;
                Vector2 p2 = vertices[triangles[i + 2]].position;
                bool hit = ray.IntersectTriangle(p0, p1, p2, out distance);
                if (hit)
                {
                    tIndex = i;
                    return true;
                }
            }
            distance = 0;
            tIndex = 0;
            return false;
        }

        /// <summary>
        /// hit test the mesh in multiple threads
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="distance"></param>
        /// <param name="tIndex">index in the triangle array of the first point of the hit triangle</param>
        /// <returns></returns>
        public bool HitTestParallel(Ray ray, out float distance, out int tIndex)
        {
            //vars used for interlocking 
            float d = 0;
            int t = -1;

            Parallel.For(0, triangles.Count / 3, (i, state) =>
            {
                i *= 3;
                Vector2 p0 = vertices[triangles[i]].position;
                Vector2 p1 = vertices[triangles[i + 1]].position;
                Vector2 p2 = vertices[triangles[i + 2]].position;
                bool hit = ray.IntersectTriangle(p0, p1, p2, out float dist);
                if (hit)
                {
                    state.Break();
                    Interlocked.Exchange(ref d, dist);
                    Interlocked.Exchange(ref t, i);
                }
            });
            distance = d;
            tIndex = t;
            return t != -1;
        }
    }
}
