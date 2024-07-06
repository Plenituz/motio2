using Motio.Debuging;
using Motio.Geometry;
using System;

namespace Motio.Meshing
{
    public class FastMeshBuilder
    {
        public Vertex[] vertices;
        public int[] triangles;
        public Mesh Mesh => new Mesh()
        {
            vertices = vertices,
            triangles = triangles
        };

        private int pointAddAt = 0;
        private int triangleAddAt = 0;

        public FastMeshBuilder(int pointCount, int triangleCount)
        {
            vertices = new Vertex[pointCount];
            triangles = new int[triangleCount];
        }


        /// <summary>
        /// returns how many points/triangle indexes will be in a circle created using <see cref="CreateCircle(Vector2, double, int, out Vector2[], out int[])"/>
        /// </summary>
        /// <param name="nbCuts"></param>
        /// <param name="pCount">number of element in the hypotetical points array</param>
        /// <param name="tCount">number of element in the hypotetical triangles array</param>
        public static void EstimateCircle(int nbCuts, out int pCount, out int tCount)
        {
            int odd = nbCuts % 2.0 == 0 ? 1 : 0;
            tCount = (nbCuts + odd) * 3;
            pCount = nbCuts + 1 + odd; //TODO the estimate is of by 1 too much for 12 and probably other even numbers
        }

        //exemple of how you would do it in parallel
        public void AddCircle(Vector2 center, float radius, int nbCuts)
        {
            CreateCircle(center, radius, nbCuts, ref vertices, ref triangles, ref pointAddAt, ref triangleAddAt, pointAddAt);
        }

        public static void CreateCircle(Vector2 center, float radius, int nbCuts, out Vertex[] points, out int[] triangles, int triOffset = 0)
        {
            EstimateCircle(nbCuts, out int pCount, out int tCount);
            points = new Vertex[pCount];
            triangles = new int[tCount];
            int tmp1 = 0, tmp2 = 0;
            CreateCircle(center, radius, nbCuts, ref points, ref triangles, ref tmp1, ref tmp2, triOffset);
        }

        public static void CreateLine(Vector2 from, Vector2 to, float thickness, out Vertex[] points, out int[] triangles, int triOffset = 0)
        {
            EstimateLine(out int pCount, out int tCount);
            points = new Vertex[pCount];
            triangles = new int[tCount];
            int tmp1 = 0, tmp2 = 0;
            CreateLine(from, to, thickness, ref points, ref triangles, ref tmp1, ref tmp2, triOffset);
        }

        public static void CreateCircleTest(Vector2 center, float radius, int nbCuts, ref Vertex[] points, ref int[] triangles,
            ref int pointAddAt, ref int triangleAddAt, out int actualpCount, out int actualtCount, int triOffset = 0)
        {
            int pCountStart = pointAddAt;
            int tCountStart = triangleAddAt;

            if (nbCuts == 0)
                throw new ArgumentException("nbCuts can't be 0");

            double pi2 = 2 * Math.PI;
            double step = pi2 / nbCuts;
            //int pointAddAt = startPoint;
            //int triangleAddAt = startTri;
            int zero = triOffset;
            int prevIndex = triOffset + 1;

            Vector2 previous = center + new Vector2(radius, 0);
            points[pointAddAt++] = new Vertex(center);
            points[pointAddAt++] = new Vertex(previous);

            for (double i = step; i < pi2; i += step)
            {
                Vector2 p = new Vector2((float)Math.Cos(i) * radius, (float)Math.Sin(i) * radius) + center;
                triangles[triangleAddAt++] = zero;
                triangles[triangleAddAt++] = prevIndex;
                triangles[triangleAddAt++] = prevIndex + 1;
                prevIndex++;
                points[pointAddAt++] = new Vertex(p);
            }
            triangles[triangleAddAt++] = zero;
            triangles[triangleAddAt++] = prevIndex;
            triangles[triangleAddAt++] = zero + 1;

            actualpCount = (pointAddAt - pCountStart);
            actualtCount = (triangleAddAt - tCountStart);
            Logger.WriteLine("actualpCount=" + actualpCount + " actualtCount=" + actualtCount);
        }

        /// <summary>
        /// create a circle and return the list of points and triangles in arrays
        /// </summary>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <param name="nbCuts">nb of division of the circle</param>
        /// <param name="points"></param>
        /// <param name="triangles"></param>
        /// <param name="pointAddAt">add points at this index</param>
        /// <param name="triangleAddAt">add triangles at the this index</param>
        /// <param name="triOffset">offset to add to each element of the triangle array</param>
 		public static void CreateCircle(Vector2 center, float radius, int nbCuts, ref Vertex[] points, ref int[] triangles,
            ref int pointAddAt, ref int triangleAddAt, int triOffset = 0)
        {            
            if (nbCuts == 0)
                throw new ArgumentException("nbCuts can't be 0");
            
            double pi2 = 2 * Math.PI;
            double step = pi2 / nbCuts;
            //int pointAddAt = startPoint;
            //int triangleAddAt = startTri;
            int zero = triOffset;
            int prevIndex = triOffset + 1;

            Vector2 previous = center + new Vector2(radius, 0);
            points[pointAddAt++] = new Vertex(center);
            points[pointAddAt++] = new Vertex(previous);

            for (double i = step; i < pi2; i += step)
            {
                Vector2 p = new Vector2((float)Math.Cos(i) * radius, (float)Math.Sin(i) * radius) + center;
                triangles[triangleAddAt++] = zero;
                triangles[triangleAddAt++] = prevIndex;
                triangles[triangleAddAt++] = prevIndex + 1;
                prevIndex++;
                points[pointAddAt++] = new Vertex(p);
            }
            triangles[triangleAddAt++] = zero;
            triangles[triangleAddAt++] = prevIndex;
            triangles[triangleAddAt++] = zero + 1;
        }

        public void AddTriangle(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            CreateTriangle(p1, p2, p3, ref vertices, ref triangles, ref pointAddAt, ref triangleAddAt, pointAddAt);
        }

        public static void CreateTriangle(Vector2 p1, Vector2 p2, Vector2 p3, out Vertex[] points, out int[] triangles, int triOffset = 0)
        {
            points = new Vertex[3];
            triangles = new int[3];
            int tmp1 = 0, tmp2 = 0;
            CreateTriangle(p1, p2, p3, ref points, ref triangles, ref tmp1, ref tmp2, triOffset);
        }

        public static void CreateTriangle(Vector2 p1, Vector2 p2, Vector2 p3, ref Vertex[] points, ref int[] triangles,
            ref int pointAddAt, ref int triangleAddAt, int triOffset = 0)
        {
            points[pointAddAt++] = new Vertex(p1);
            points[pointAddAt++] = new Vertex(p2);
            points[pointAddAt++] = new Vertex(p3);

            triangles[triangleAddAt++] = triOffset;
            triangles[triangleAddAt++] = triOffset + 1;
            triangles[triangleAddAt++] = triOffset + 2;
        }

        public static void EstimateLine(out int pCount, out int tCount)
        {
            pCount = 4;
            tCount = 6;
        }

        public void AddLine(Vector2 from, Vector2 to, float thickness)
        {
            CreateLine(from, to, thickness, ref vertices, ref triangles, ref pointAddAt, ref triangleAddAt, pointAddAt);
        }

        public static void CreateLine(Vector2 from, Vector2 to, float thickness, ref Vertex[] points, ref int[] triangles,
            ref int pointAddAt, ref int triangleAddAt, int triOffset = 0)
        {
            Vector2 normal = to - from;
            normal.Rotate90Deg();
            normal.Normalize();
            normal *= thickness;

            Vector2 p1 = from + normal;
            Vector2 p2 = from - normal;
            Vector2 p3 = to - normal;
            Vector2 p4 = to + normal;
            points[pointAddAt++] = new Vertex(p1);
            points[pointAddAt++] = new Vertex(p2);
            points[pointAddAt++] = new Vertex(p3);
            points[pointAddAt++] = new Vertex(p4);

            triangles[triangleAddAt++] = triOffset;
            triangles[triangleAddAt++] = triOffset + 1;
            triangles[triangleAddAt++] = triOffset + 2;

            triangles[triangleAddAt++] = triOffset + 2;
            triangles[triangleAddAt++] = triOffset + 3;
            triangles[triangleAddAt++] = triOffset;
        }
    }
}
