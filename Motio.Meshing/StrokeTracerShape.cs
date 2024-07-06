using Motio.Geometry;
using System;
using System.Collections.Generic;

namespace Motio.Meshing
{
    /// <summary>
    /// generate a mesh that is the stroke around a list of points.
    /// </summary>
    public class StrokeTracerShape
    {
        public enum EndCap
        {
            Round,
            Square
            //TODO arrow end cap
        }

        public List<Vertex> shapeVerts;

        private List<Vertex> rightLane = new List<Vertex>();
        private List<Vertex> firstCap = new List<Vertex>();
        private List<Vertex> leftLane = new List<Vertex>();
        private List<Vertex> secondCap = new List<Vertex>();

        public MotioShape MotioShape;

        /// <summary>
        /// create a stroke following the given positions. populates <see cref="shapePoints"/>, and <see cref="shapeNormals"/>.
        /// You can also get the result in <see cref="MotioShape"/>
        /// </summary>
        /// <param name="positions">list of position to stroke</param>
        /// <param name="thicknesses">if null or some elements are missing 1 is used</param>
        public void Stroke(IList<Vector2> positions, IList<float> thicknesses, EndCap cap, bool close)
        {
            if(positions.Count < 2)
                throw new System.Exception("can't stroke a list with less than 2 positions");
            Vector2[] normals = new Vector2[positions.Count];
            if (close)
            {
                CalculateNormalsClose(normals, positions);
            }
            else
            {
                CalculateNormalsOpen(normals, positions);
            }
            
            ScaleNormals(normals, thicknesses);
            //TODO make it all array base (calculate cap division*2 + positions.Count *2 + join divisions)?

            Vector2 firstOfLeftLane = positions[positions.Count - 1] - normals[normals.Length - 1];
            CalculateRightLane(positions, normals);
            if(!close)
                CalculateCap(positions[positions.Count - 1], rightLane[rightLane.Count - 1].position, normals[normals.Length - 1], firstOfLeftLane, cap, firstCap/*cap divisions*/);
            CalculateLeftLane(positions, normals, firstOfLeftLane);
            if (!close)
                CalculateCap(positions[0], leftLane[leftLane.Count - 1].position, leftLane[leftLane.Count - 1].normal, rightLane[0].position, cap, secondCap);

            StitchArrayAndCreateShape(close);
        }

        private void StitchArrayAndCreateShape(bool close)
        {
            MotioShape = new MotioShape();

            if (!close)
            {
                shapeVerts = new List<Vertex>(rightLane.Count + firstCap.Count + leftLane.Count + secondCap.Count);
                shapeVerts.AddRange(rightLane);
                shapeVerts.AddRange(firstCap);
                shapeVerts.AddRange(leftLane);
                shapeVerts.AddRange(secondCap);
                MotioShape.vertices = shapeVerts;
            }
            else
            {
                shapeVerts = new List<Vertex>(rightLane.Count);
                shapeVerts.AddRange(rightLane);
                MotioShape.vertices = shapeVerts;
                MotioShape.holes = new MotioShape[] { new MotioShape() { vertices = leftLane } };
            }
            MotioShape.UpdateNormalsGeneration();
        }

        private void CalculateNormalsOpen(Vector2[] normals, IList<Vector2> positions)
        {
            Vector2 directionBefore = positions[0] - positions[1];
            normals[0] = directionBefore;
            normals[0].Normalize();
            normals[0].Rotate90Deg();
            Vector2 normalBefore = normals[0];

            for (int i = 1; i < positions.Count - 1; i++)
            {
                normals[i] = CalculateNormal(positions[i - 1], positions[i], positions[i + 1], 
                    directionBefore, normalBefore, out directionBefore, out normalBefore);
            }

            int max = normals.Length - 1;
            normals[max] = normalBefore;
            normals[max].Normalize();
        }

        private void CalculateNormalsClose(Vector2[] normals, IList<Vector2> positions)
        {
            Vector2 directionBefore = positions[positions.Count - 1] - positions[0];
            int lastNormal = normals.Length - 1;
            normals[lastNormal] = directionBefore;
            normals[lastNormal].Normalize();
            normals[lastNormal].Rotate90Deg();
            Vector2 normalBefore = normals[lastNormal];

            for (int i = 0; i < positions.Count - 1; i++)
            {
                normals[i] = CalculateNormal(positions[i - 1], positions[i], positions[i + 1],
                    directionBefore, normalBefore, out directionBefore, out normalBefore);
            }

            //last normal
            int max = normals.Length - 1;
            normals[max] = CalculateNormal(positions[max - 1], positions[max], positions[0],
                    directionBefore, normalBefore, out directionBefore, out normalBefore);
        }

        private Vector2 CalculateNormal(Vector2 before, Vector2 point, Vector2 after, Vector2 directionBefore, Vector2 normalBefore, out Vector2 directionAfter, out Vector2 normalAfter)
        {
            directionAfter = point - after;
            normalAfter = directionAfter;
            normalAfter.Rotate90Deg();
            normalAfter.Normalize();

            Vector2 p1 = before + normalBefore;
            Vector2 p2 = after + normalAfter;

            bool d1 = Vector2.LinesIntersection(ref p1, ref directionBefore, ref p2, ref directionAfter, out Vector2 intersection);

            if (d1)
            {
                return intersection - point;
            }
            else
            {
                Vector2 normal = normalBefore + normalAfter;
                normal.Normalize();
                return normal;
            }
        }

        private void ScaleNormals(Vector2[] normals, IList<float> thicknesses)
        {
            for(int i = 0; i < normals.Length; i++)
            {
                if(thicknesses.Count > i)
                    normals[i] *= thicknesses[i];
            }
        }

        private void CalculateRightLane(IList<Vector2> positions, Vector2[] normals)
        {
            for(int i = 0; i < normals.Length; i++)
            {
                rightLane.Add(new Vertex(positions[i] + normals[i])
                {
                    normal = normals[i]
                });
            }
        }

        private void CalculateCap(Vector2 positionCenter, Vector2 lastRightLane, Vector2 normal, Vector2 firstOfLeftLane, EndCap cap, List<Vertex> vertexList)
        {
            switch (cap)
            {
                case EndCap.Square:
                    return;
                case EndCap.Round:
                    //estimate division
                    float width = Vector2.Distance(lastRightLane, firstOfLeftLane);
                    int capDivision = (int)(width * 5)+3;

                    float pi = (float)Math.PI;
                    float angle = pi / (capDivision + 1);

                    Quaternion rotMat = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, angle);
                    for (int i = 0; i < capDivision; i++)
                    {
                        normal = Vector2.Transform(normal, rotMat);
                        vertexList.Add(new Vertex(positionCenter + normal));
                    }
                    return;
            }
        }

        private void CalculateLeftLane(IList<Vector2> positions, Vector2[] normals, Vector2 firstOfLeftLane)
        {
            leftLane.Add(new Vertex(firstOfLeftLane)
            {
                normal = -normals[normals.Length - 1]
            });

            for (int i = normals.Length - 2; i >= 0; i--) 
            {
                leftLane.Add(new Vertex(positions[i] - normals[i])
                {
                    normal = -normals[i]
                });
            }
        }
    }
}
