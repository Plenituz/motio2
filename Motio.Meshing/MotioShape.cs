using Motio.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Motio.Meshing
{
    /// <summary>
    /// polygon that will be triangulated at the end of the node chain to become a mesh and be rendered
    /// </summary>
    public class MotioShape : ICloneable
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
        /// This is better if it's a native c# Array of vertex, it's faster to iterate over
        /// </summary>
        public IList<Vertex> vertices = new Vertex[0];

        /// <summary>
        /// list of polygon that are holes. Note that the holes can have holes, resulting in non holes in the end
        /// </summary>
        public IList<MotioShape> holes = new MotioShape[0];

        /// <summary>
        /// transformation matrix applied at the end of the node chain
        /// </summary>
        private Matrix _transform = Matrix.Identity;

        public Matrix transform
        {
            get => _transform;
            set
            {
                _transform = value;
                RunOnHoles(this, h => h.transform = value);
            }
        }

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
        public void ApplyConditions()
        {
            Vector3 translation = transform.Translation;
            _transform.Translation = new Vector3(translation.X, translation.Y, zIndex);
            RunOnHoles(this, h => h.ApplyConditions());
        }

        /// <summary>
        /// sets <see cref="normalsFromGeneration"/> to <see cref="generation"/>
        /// </summary>
        public void UpdateNormalsGeneration()
        {
            normalsFromGeneration = generation;
            RunOnHoles(this, h => h.UpdateNormalsGeneration());
        }

        public bool ShouldCalculateNormals()
        {
            return normalsFromGeneration < generation - 1;
        }

        /// <summary>
        /// check if a point is inside this shape, not taking into account the holes
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool IsPointInside(Vector2 point)
        {
            // http://www.ecse.rpi.edu/Homepages/wrf/Research/Short_Notes/pnpoly.html

            float x = point.X;
            float y = point.Y;

            var inside = false;
            for (int i = 0, j = vertices.Count - 1; i < vertices.Count; j = i++)
            {
                float xi = vertices[i].position.X, yi = vertices[i].position.Y;
                float xj = vertices[j].position.X, yj = vertices[j].position.Y;

                var intersect = ((yi > y) != (yj > y))
                    && (x < (xj - xi) * (y - yi) / (yj - yi) + xi);
                if (intersect)
                    inside = !inside;
            }

            return inside;
        }

        /// <summary>
        /// check if a point is inside this shape, taking into account the holes
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool HitTest(Vector2 point)
        {
            bool inside = IsPointInside(point);
            if (inside)
            {
                for(int i = 0; i < holes.Count; i++)
                {
                    //can't believe it's that simple
                    if (holes[i].HitTest(point))
                    {
                        inside = !inside;
                        break;
                    }
                }
            }
            return inside;
        }

        /// <summary>
        /// calculate the points normals of this mesh
        /// </summary>
        public void CalculateNormals(bool isHole = false)
        {
            int sign = ShouldReverse() ? 1 : -1;

            // we have to calculate the first and last point manually
            // because the neighbour before needs the last point of the list
            Vertex vertex = vertices[0];
            vertex.normal = CalculateNormal(vertex, vertices[vertices.Count - 1], vertices[1]) * sign;
            vertices[0] = vertex;

            for (int i = 1; i < vertices.Count - 1; i++)
            {
                vertex = vertices[i];
                vertex.normal = CalculateNormal(vertices[i], vertices[i - 1], vertices[i + 1]) * sign;
                vertices[i] = vertex;
            }

            vertex = vertices[vertices.Count - 1];
            vertex.normal = CalculateNormal(vertices[vertices.Count - 1], vertices[vertices.Count - 2], vertices[0]) * sign;
            vertices[vertices.Count - 1] = vertex;
            normalsFromGeneration = generation;

            RunOnHoles(this, h => h.CalculateNormals(!isHole));

            Vector2 CalculateNormal(Vertex point, Vertex before, Vertex after)
            {
                Vector2 normalBefore = before.position - point.position;
                normalBefore.Rotate90Deg();
                Vector2 normalAfter = point.position - after.position;
                normalAfter.Rotate90Deg();

                Vector2 normal = normalBefore + normalAfter;
                normal.Normalize();
                return normal;
            }

            bool ShouldReverse()
            {
                Vector2 normal = vertices[0].position - vertices[1].position;
                normal.Normalize();
                normal *= 0.01f;
                return !IsPointInside(vertices[0].position + normal);
            }
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
            //must run on holes before reseting otherwise holes will have the reset transformed as well
            RunOnHoles(this, h => h.BakeTransform());
            transform = Matrix.Identity;
        }

        private static void RunOnHoles(MotioShape shape, Action<MotioShape> action)
        {
            for (int i = 0; i < shape.holes.Count; i++)
            {
                action(shape.holes[i]);
            }
        }

        public MotioShape Clone()
        {
            return new MotioShape
            {
                generation = generation + 1,
                normalsFromGeneration = this.normalsFromGeneration,
                vertices = this.vertices.ToList(),
                holes = this.holes.Select(h => h.Clone()).ToList(),
                shader = this.shader,
                transform = this.transform,
                transformable = this.transformable,
                deformable = this.deformable,
                zIndex = this.zIndex
            };
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }
}
