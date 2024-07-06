using Motio.Geometry;
using System;
using System.Collections.Generic;

namespace Motio.Meshing
{
    /// <summary>
    /// generate a mesh that is the stroke around a list of points.
    /// NOT USED ANYMORE <see cref="StrokeTracerShape"/>
    /// </summary>
    public class StrokeTracerMesh
    {
        public enum EndCap
        {
            Round,
            Square
            //TODO arrow end cap
        }

        public List<Vector2> points = new List<Vector2>();
        public List<int> triangles = new List<int>();
        public List<Vector2> normals = new List<Vector2>();

        public Mesh Mesh
        {
            get
            {
                Mesh mesh = new Mesh();
                mesh.vertices = Vertices;
                mesh.triangles = triangles;
                mesh.UpdateNormalsGeneration();
                return mesh;
            }
        }

        public List<Vertex> Vertices
        {
            get
            {
                List<Vertex> vertices = new List<Vertex>();
                for(int i = 0; i < points.Count; i++)
                {
                    vertices.Add(new Vertex()
                    {
                        position = points[i],
                        normal = normals[i]
                    });
                }
                return vertices;
            }
        }

        /// <summary>
        /// create a stroke following the given positions. populates <see cref="points"/>, <see cref="triangles"/> and <see cref="normals"/>
        /// </summary>
        /// <param name="positions">list of position to stroke</param>
        /// <param name="thicknesses">
        /// thickness at each position ACTS WEIRD IF NOT UNIFORM FOR NOW,
        /// if null or some elements are missing 1 is used</param>
        public void Stroke(IList<Vector2> positions, IList<float> thicknesses, EndCap cap, bool close)
        {
            if (thicknesses == null)
                thicknesses = new float[] { 1, 1 };
            Vector2 directionCache;
            AddLine(positions[0], positions[1], thicknesses[0], thicknesses[1], out directionCache);
            Vector2 firstDirection = directionCache;
            for (int i = 2; i < positions.Count; i++)
            {
                float thickness = thicknesses.Count > i ? thicknesses[i] : 1;
                AddLineFromLast(positions[i], thickness, positions[i - 1], positions[i - 2], directionCache, out directionCache);
            }
            if (close && positions.Count > 1)
            {
                float thickness = thicknesses.Count > positions.Count + 1 ? thicknesses[positions.Count] : 1;
                Close(positions[0], positions[positions.Count - 1], firstDirection, directionCache, thickness);
            }
            else
            {
                if (cap == EndCap.Round)
                {
                    AddEndRoundCap(10);
                    AddStartRoundCap(10);
                }
            }
        }

        private void Close(Vector2 firstPoint, Vector2 lastPoint, Vector2 firstDirection, Vector2 lastDirection, float thickness)
        {
            Vector2 direction = firstPoint - lastPoint;
            Vector2 normal = direction;
            normal.Rotate90Deg();
            normal.Normalize();
            normal *= thickness;

            int c = triangles.Count;
            Vector2 p1 = points[triangles[0]];
            Vector2 p2 = points[triangles[1]];
            Vector2 p3 = points[triangles[c - 1]];
            Vector2 p4 = points[triangles[c - 2]];

            Vector2 newP3 = lastPoint + normal;
            bool r1 = Vector2.LinesIntersection(ref newP3, ref direction, ref p3, ref lastDirection, out newP3);
            if (r1)
                points[triangles[c - 1]] = newP3;

            Vector2 newP4 = lastPoint - normal;
            bool r2 = Vector2.LinesIntersection(ref newP4, ref direction, ref p4, ref lastDirection, out newP4);
            if (r2)
                points[triangles[c - 2]] = newP4;

            Vector2 newP1 = firstPoint + normal;
            bool r3 = Vector2.LinesIntersection(ref newP1, ref direction, ref p1, ref firstDirection, out newP1);
            if (r3)
                points[triangles[0]] = newP1;

            Vector2 newP2 = firstPoint - normal;
            bool r4 = Vector2.LinesIntersection(ref newP2, ref direction, ref p2, ref firstDirection, out newP2);
            if (r4)
                points[triangles[1]] = newP2;

            triangles.Add(triangles[1]);
            triangles.Add(triangles[c - 1]);
            triangles.Add(triangles[c - 2]);
            triangles.Add(triangles[1]);
            triangles.Add(triangles[0]);
            triangles.Add(triangles[c - 1]);
        }

        private void AddRoundCap_new(int nbPoints, bool start)
        {
           /*      m3
            *   x   m2 x
            * x     |    x
            * p4 ---mid- p3
            * |           |
            * 
            */

            //the start points change depending on if we draw a start or end cap
            //for the start we use the first and for the end we use the last and 2nd to last
            int multiplier = start ? 1 : -1;
            int index1 = start ? 0 : triangles.Count - 1;
            int index2 = start ? 1 : triangles.Count - 2;
            Vector2 p4 = points[triangles[index1]];
            Vector2 p3 = points[triangles[index2]];
            Vector2 m2 = (p3 + p4) / 2;
            Vector2 normal = p4 - m2;
            Vector2 normalToMid = normal / 2 * multiplier;
            normalToMid.Rotate90Deg();
            Vector2 m2Centered = m2 + normalToMid;

            int m2pos = points.Count;
            points.Add(m2Centered);
            normals.Add(Vector2.Zero);
            triangles.Add(index1);//the triangle link between the stroke and the circle
            triangles.Add(index2);
            triangles.Add(m2pos);

            float pi = (float)Math.PI;
            //revert the angle if it's a start cap


            for (int i = 1; i < nbPoints; i++)
            {
                float angle = (pi * i) / nbPoints;
                angle *= multiplier;

                Quaternion rotMat = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, angle);
                Vector2 normalRotated = Vector2.Transform(normal, rotMat);
                Vector2 m3 = m2 + normalRotated;

                //c is m3
                int c = points.Count;
                points.Add(m3);
                normals.Add(Vector2.Zero);//temporary
                int[] indexes;
                //flip the indexes backward if it's a start
                if (start)
                    indexes = new int[] { c, m2pos, c - 1 };
                else
                    indexes = new int[] { c - 1, m2pos, c };
                triangles.Add(indexes[0]);
                triangles.Add(indexes[1]);
                triangles.Add(indexes[2]);
            }
        }

        private void AddRoundCap(int nbPoints, bool start)
        {
            /*     m3
            *   x      x
            * x          x
            * p4 ---m2--- p3
            * |           |
            * 
            */
            //TODO ultimatly it would be nice to get ride of quaternions and simply use 
            //sin/cos, the math should be pretty simple

            //the start points change depending on if we draw a start or end cap
            //for the start we use the first and for the end we use the last and 2nd to last
            int index1 = start ? 0 : triangles.Count - 1;
            int index2 = start ? 1 : triangles.Count - 2;
            Vector2 p4 = points[triangles[index1]];
            Vector2 p3 = points[triangles[index2]];

            Vector2 m2 = (p3 + p4) / 2;
            int m2pos = points.Count;
            points.Add(m2);
            normals.Add(Vector2.Zero);
            Vector2 normal = p4 - m2;

            float pi = (float)Math.PI;
            //revert the angle if it's a start cap
            int multiplier = start ? 1 : -1;
            for (int i = 0; i < nbPoints + 1; i++)
            {
                float angle = (pi * i) / nbPoints;
                angle *= multiplier;

                Quaternion rotMat = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, angle);
                Vector2 normalRotated = Vector2.Transform(normal, rotMat);
                Vector2 m3 = m2 + new Vector2(normalRotated.X, normalRotated.Y);

                //c is m3
                int c = points.Count;
                points.Add(m3);
                normals.Add(Vector2.Zero);//temporary
                int[] indexes;
                //flip the indexes backward if it's a start
                if (start)
                    indexes = new int[] { c, m2pos, c - 1 };
                else
                    indexes = new int[] { c - 1, m2pos, c };
                triangles.Add(indexes[0]);
                triangles.Add(indexes[1]);
                triangles.Add(indexes[2]);
            }
        }

        private void AddStartRoundCap(int nbPoints)
        {
            AddRoundCap(nbPoints, true);
        }

        private void AddEndRoundCap(int nbPoints)
        {
            AddRoundCap(nbPoints, false);
        }


        /// <summary>
        /// add a line assuming there is already one.
        /// </summary>
        /// <param name="to">point to trace to</param>
        /// <param name="thickness">thickness of this chunk</param>
        /// <param name="from">"to" value of the previous line</param>
        /// <param name="fromOld">"from" value of the previous line</param>
        /// <param name="directionOld">direction received from the previous line</param>
        /// <param name="direction">direction to pass to the next line</param>
        private void AddLineFromLast(
            Vector2 to, float thickness,
            Vector2 from,
            Vector2 fromOld,
            Vector2 directionOld,
            out Vector2 direction)
        {
            /*
             *      p1-----------p4
             *      |            |
             * p4Old---p3Old     |  <- this rect is the new one, we end up only adding p3 and p4
             *  |   |    |       |          and modifying p4Old and p3Old instead of re-adding p1 and p2
             *  |   p2---+-------p3
             *  |        | 
             *  |        | <- this rect is already here from the last line
             * p1Old---p2Old
             */
            //values from the previous line
            Vector2 p1Old = points[triangles[triangles.Count - 3]];
            Vector2 p2Old = points[triangles[triangles.Count - 5]];
            Vector2 p3Old = points[triangles[triangles.Count - 2]];
            Vector2 p4Old = points[triangles[triangles.Count - 1]];


            //Vector2 from;
            //if (fromOpt.HasValue)
            //    from = fromOpt.Value;
            //else
            //    from = (p4Old + p3Old) / 2;

            //Vector2 fromOld;
            //if (previousFromOpt.HasValue)
            //    fromOld = previousFromOpt.Value;
            //else
            //    fromOld = (p1Old + p2Old) / 2;

            //Vector2 directionOld;
            //if (previousFromOpt.HasValue)
            //    directionOld = previousDirectionOpt.Value;
            //else
            //    directionOld = from - fromOld;//from is also toOld, so this is toOld - fromOld

            direction = to - from;

            Vector2 normal = direction;
            normal.Rotate90Deg();
            normal.Normalize();
            //avoid copies by multiplying by hand
            normal.X *= thickness;
            normal.Y *= thickness;

            //Vector2 p1 = from + normal; useless
            //Vector2 p2 = from - normal; useless
            Vector2 p3 = to - normal;
            Vector2 p4 = to + normal;

            bool res1 = Vector2.LinesIntersection(ref p4, ref direction, ref p4Old, ref directionOld, out p4Old);
            bool res2 = Vector2.LinesIntersection(ref p3, ref direction, ref p3Old, ref directionOld, out p3Old);

            //change the points/normal after to avoid problems with thickness ?
            //normal.X *= thickness;
            //normal.Y *= thickness;
            //p3 = to - normal;
            //p4 = to + normal;


            if (res1)
            {
                int index = triangles[triangles.Count - 1];
                points[index] = p4Old;

                Vector2 oldNormal = normals[index];
                oldNormal += normal;
                oldNormal.Normalize();
                normals[index] = oldNormal;
            }

            if (res2)
            {
                int index = triangles[triangles.Count - 2];
                points[index] = p3Old;

                Vector2 oldNormal = normals[index];
                oldNormal += normal;
                oldNormal.Normalize();
                normals[index] = oldNormal;
            }

            AddQuadFromLast(p3, p4, -normal, normal);
        }

        /// <summary>
        /// add a line assuming the mesh is empty
        /// </summary>
        /// <param name="from">start of the line</param>
        /// <param name="to">end of the line</param>
        /// <param name="thicknessStart">thickness of the first set of point</param>
        /// <param name="thicknessEnd">thickness of the second set of point</param>
        /// <param name="direction">direction to pass to the next segment</param>
        private void AddLine(Vector2 from, Vector2 to, float thicknessStart, float thicknessEnd, out Vector2 direction)
        {
            /*
             *  p4 <------- to -------> p3
             *              ^
             *              |
             *              |  <direction
             *              |
             * p1 <------- from ------> p2
             *       ^normal(length is thickness)
             */
            direction = to - from;
            Vector2 normal = direction;
            normal.Rotate90Deg();
            normal.Normalize();
            Vector2 normalStart = normal * thicknessStart;
            Vector2 normalEnd = normal * thicknessEnd;

            Vector2 p1 = from - normalStart;
            Vector2 p2 = from + normalStart;
            Vector2 p3 = to - normalEnd;
            Vector2 p4 = to + normalEnd;


            Vector2 minusNormal = -normal;
            points.Add(p1);
            normals.Add(minusNormal);
            points.Add(p2);
            normals.Add(normal);
            AddQuadFromLast(p3, p4, minusNormal, normal);
        }

        /// <summary>
        /// add a point assuming there is already 2 points in <see cref="points"/>
        /// </summary>
        /// <param name="p3"></param>
        /// <param name="p4"></param>
        /// <param name="normalP3"></param>
        /// <param name="normalP4"></param>
        private void AddQuadFromLast(Vector2 p3, Vector2 p4, Vector2 normalP3, Vector2 normalP4)
        {
            int c = points.Count;
            points.Add(p3);
            normals.Add(normalP3);
            points.Add(p4);
            normals.Add(normalP4);
            //c is p3
            //make sure the last 2 points are p4 and p3
            //so other functions can assume points[triangles[triangles.Count-1]] is the "front" 
            //of the stroke
            triangles.Add(c - 1);
            triangles.Add(c - 2);
            triangles.Add(c);
            triangles.Add(c - 1);
            triangles.Add(c);
            triangles.Add(c + 1);
            //equivalent to 
            //triangles.AddRange(new int[]
            //{
            //    c-1, c-2, c,
            //    c-1, c, c+1
            //});
            /*
             * p4 ----- p3
             * |        |
             * |        |
             * p1 ----- p2
             * 
             * c+1 ---- c
             * |        |
             * |        |
             * c-1 ---- c-2
             */
        }
    }
}
