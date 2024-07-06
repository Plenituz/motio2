using Motio.Geometry;
using System;
using System.Windows;

namespace Motio.Animation
{
    public class DoubleInterpolator : IInterpolator
    {
        /// <summary>
        /// Split a bezier curve at the point the closest to <paramref name="midPoint"/>
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="firstHandle"></param>
        /// <param name="secondHandle"></param>
        /// <param name="endPoint"></param>
        /// <param name="midPoint"></param>
        /// <returns></returns>
        public static (
            Vector2 firstHandle1,
            Vector2 secondHandle1,
            Vector2 midPoint,
            Vector2 firstHandle2,
            Vector2 secondHandle2)
            SplitBezier(
            Vector2 startPoint,
            Vector2 firstHandle,
            Vector2 secondHandle,
            Vector2 endPoint,
            Vector2 midPoint,
            float step = 0.001f)
        {
            float t;
            float lastDistance = Vector2.DistanceSquared(startPoint, midPoint);
            for(t = 0; t < 1f; t += step)
            {
                Vector2 p = GetBezierPoint(startPoint, firstHandle, secondHandle, endPoint, t);
                float distance = Vector2.DistanceSquared(p, midPoint);
                if (distance > lastDistance)
                    break;

                lastDistance = distance;
            }

            return SplitBezier(
                startPoint, 
                firstHandle, 
                secondHandle, 
                endPoint,
                t);
        }

        /// <summary>
        /// Split a bezier curve at <paramref name="t"/> percent between the points given
        /// </summary>
        public static (
            Vector2 firstHandle1,
            Vector2 secondHandle1,
            Vector2 midPoint,
            Vector2 firstHandle2,
            Vector2 secondHandle2)
            SplitBezier(
            Vector2 startPoint,
            Vector2 firstHandle,
            Vector2 secondHandle,
            Vector2 endPoint,
            float t)
        {
            //god bless Jonathan Azulay: https://stackoverflow.com/questions/8369488/splitting-a-bezier-curve
            float x1 = startPoint.X  , y1 = startPoint.Y; 
            float x2 = firstHandle.X , y2 = firstHandle.Y; 
            float x3 = secondHandle.X, y3 = secondHandle.Y;
            float x4 = endPoint.X    , y4 = endPoint.Y;

            float x12 = (x2 - x1) * t + x1;
            float y12 = (y2 - y1) * t + y1;
                                     
            float x23 = (x3 - x2) * t + x2;
            float y23 = (y3 - y2) * t + y2;
                                     
            float x34 = (x4 - x3) * t + x3;
            float y34 = (y4 - y3) * t + y3;
             
            float x123 = (x23 - x12) * t + x12;
            float y123 = (y23 - y12) * t + y12;
                                         
            float x234 = (x34 - x23) * t + x23;
            float y234 = (y34 - y23) * t + y23;
             
            float x1234 = (x234 - x123) * t + x123;
            float y1234 = (y234 - y123) * t + y123;
            return (new Vector2(x12, y12), 
                new Vector2(x123, y123), 
                new Vector2(x1234, y1234), 
                new Vector2(x234, y234), 
                new Vector2(x3‌​4, y34));
        }


        public float InterpolateBetween(KeyframeFloat t1, KeyframeFloat t2, int time)
        {
            return (float)InterpolateBetween(t1.Value, t2.Value, t1.Time, t2.Time, 
                time, new Vector(t1.RightHandle.X, t1.RightHandle.Y), new Vector(t2.LeftHandle.X, t2.LeftHandle.Y));
        }

        public static double InterpolateBetween(double from, double to,
            double startTime, double endTime, double time, Vector handle1, Vector handle2)
        {
            if(from == to)
            {
                to += 0.0000001;
            }
            //put things in interpolation space
            double percent = (time - startTime) / (endTime - startTime);

            System.Windows.Point end = new System.Windows.Point(endTime, to);
            System.Windows.Point start = new System.Windows.Point(startTime, from);
            //System.Windows.Point first = start + handle1;
            System.Windows.Point second = end + handle2;

            Vector endMinusStart = end - start;
            //bring back the handles to "interpolation space" a value between 0 and 1
            handle1 = Div(handle1, endMinusStart);
            handle2 = Div(second - start, endMinusStart);

            if (double.IsNaN(handle1.X) || double.IsInfinity(handle1.X))
                handle1.X = 0;
            if (double.IsNaN(handle1.Y) || double.IsInfinity(handle1.Y))
                handle1.Y = 0;
            if (double.IsNaN(handle2.X) || double.IsInfinity(handle2.X))
                handle2.X = 1;
            if (double.IsNaN(handle2.Y) || double.IsInfinity(handle2.Y))
                handle2.Y = 1;

            return (to - from) * Interpolate(percent, handle1, handle2) + from;
        }

        private static double Interpolate(double x, Vector first, Vector second)
        {
            return Evaluate(
                new Vector(0, 0),
                first,
                second,
                new Vector(1, 1),
                x);
        }

        /// <summary>
        /// trouver la valeur de y pour laquelle V.X = x
        /// evaluate a bezier curve 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <param name="end"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public static double Evaluate(Vector start, Vector first,
            Vector second, Vector end, double x)
        {
            if (x == 0)
                return start.Y;
            if (x == 1)
                return end.Y;
            //TODO the below precision of 0.001 might not be optimal
            //may be process the curve before hand to cut it up in optimal
            //segments depending on the curvature and then do some kind of binary search on that ?
            //TODO do a "binary search" on the curve directly ? 
            for (float i = 0f; i < 1f; i += 0.001f)
            {
                Vector v = GetBezierPoint(start, first, second, end, i);
                //as soon as we went over the wanted value, return the value
                if (v.X >= x)
                {
                    return v.Y;
                }
            }
            return end.Y;
        }

        /// <summary>
        /// the math of bezier curve 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <param name="end"></param>
        /// <param name="t">percent to go betwen start and end</param>
        /// <returns></returns>
        public static Vector GetBezierPoint(
            Vector start, Vector first,
            Vector second, Vector end, double t)
        {
            //Console.WriteLine("------------- double prec");
            double omt = 1.0 - t;
            //Console.WriteLine("omt=" + omt);
            double omt2 = omt * omt;
            //Console.WriteLine("omt2=" + omt2);
            double t2 = t * t;
            //Console.WriteLine("t2=" + t2 + "\n");
            //Console.WriteLine("l1=" + (start * (omt2 * omt)) );
            //Console.WriteLine("l2=" + (first * (3.0 * omt2 * t)) );
            //Console.WriteLine("l3=" + (second * (3.0 * omt * t2)) );
            //Console.WriteLine("l4=" + (end * (t2 * t)) );
            return start * (omt2 * omt) +
                   first * (3.0 * omt2 * t) +
                   second * (3.0 * omt * t2) +
                   end * (t2 * t);
        }

        public static Geometry.Vector2 GetBezierPoint(
            Geometry.Vector2 start, Geometry.Vector2 first,
            Geometry.Vector2 second, Geometry.Vector2 end, double t)
        {
            //Console.WriteLine("------------- single prec");
            double omt = 1.0 - t;
            //Console.WriteLine("omt=" + omt);
            double omt2 = omt * omt;
            //Console.WriteLine("omt2=" + omt2);
            double t2 = t * t;
            //Console.WriteLine("t2=" + t2 + "\n");
            //Console.WriteLine("l1=" + (start * (omt2 * omt)));
            //Console.WriteLine("l2=" + (first * (3.0 * omt2 * t)));
            //Console.WriteLine("l3=" + (second * (3.0 * omt * t2)));
            //Console.WriteLine("l4=" + (end * (t2 * t)));
            return start * (omt2 * omt) +
                   first * (3.0 * omt2 * t) +
                   second * (3.0 * omt * t2) +
                   end * (t2 * t);
        }

        public static Geometry.Vector2 GetBezierPoint(
            Geometry.Vector2 start, Geometry.Vector2 first,
            Geometry.Vector2 second, Geometry.Vector2 end, float t)
        {

            float omt = 1.0f - t;
            float omt2 = omt * omt;
            float t2 = t * t;
            return start * (omt2 * omt) +
                   first * (3.0f * omt2 * t) +
                   second * (3.0f * omt * t2) +
                   end * (t2 * t);
        }

        /// <summary>
        /// divide eache component of v1 by each component of v2
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        private static Vector Div(Vector v1, Vector v2)
        {
            v1.X /= v2.X;
            v1.Y /= v2.Y;
            return v1;
        }

        /// <summary>
        /// linear interpolation between 2 values
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static double Linear(double from, double to, double t)
        {
            return (to - from) * t + from;
        }

        /// <summary>
        /// returns at how many percent val is inbetween start and end
        /// </summary>
        /// <param name="val"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static double InverseLinear(double val, double start, double end)
        {
            if (start == end)
                return 0;
            return (val - start) / (end - start);
        }
    }
}
