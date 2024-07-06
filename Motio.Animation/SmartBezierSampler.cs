using Motio.Geometry;
using Motio.UICommon;
using System;
using System.Collections.Generic;

namespace Motio.Animation
{
    public class SmartBezierSampler
    {
        public static (IList<Vector2>, IList<double>) SampleCurve(Vector2 start, Vector2 handle1, Vector2 handle2, Vector2 end, double detailMult)
        {
            List<Vector2> vectors = new List<Vector2>();

            for(double i = 0; i < 1; i += 0.1)
            {
                Vector2 currentPoint = DoubleInterpolator.GetBezierPoint(start, handle1, handle2, end, (float)i);
                vectors.Add(currentPoint);
            }

            double curvature = 0;
            for(int i = 0; i < vectors.Count-2; i++)
            {
                Vector2 toNext = vectors[i + 1] - vectors[i];
                Vector2 toDoubleNext = vectors[i + 2] - vectors[i + 1];
                curvature += (toNext - toDoubleNext).Length();
            }
            //Console.WriteLine("curvature:" + curvature);

            vectors.Clear();
            List<double> percents = new List<double>();
            double step = curvature/(Math.Exp(curvature)*0.1*detailMult).Clamp(0, 200);
            for (double i = 0; i < 1; i += step)
            {
                Vector2 currentPoint = DoubleInterpolator.GetBezierPoint(start, handle1, handle2, end, (float)i);
                vectors.Add(currentPoint);
                percents.Add(i);
            }
            if (vectors[vectors.Count - 1] != end)
            {
                vectors.Add(end);
                percents.Add(1);
            }

            return (vectors, percents);
        }
    }
}
