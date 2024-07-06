using Microsoft.VisualStudio.TestTools.UnitTesting;
using Motio.Geometry;
using System;

namespace Motio.Animation.UnitTests
{
    [TestClass]
    public class DoubleInterpolatorTests
    {
        [TestMethod]
        public void DoubleInterpolator_Linear()
        {
            double from = 0;
            double to   = 22;
            double t    = 0.3;

            double res = DoubleInterpolator.Linear(from, to, t);
            Assert.AreEqual(6.6, res);
        }

        [TestMethod]
        public void DoubleInterpolator_InverseLinear()
        {
            double from = 0;
            double to = 22;
            double t = 6.6;

            double res = DoubleInterpolator.InverseLinear(t, from, to);
            Assert.AreEqual(0.3, res);
        }

        [TestMethod]
        public void DoubleInterpolator_BezierPoint()
        {
            Vector2 res = DoubleInterpolator.GetBezierPoint(
                Vector2.Zero,
                new Vector2(0, 1),
                new Vector2(1, 0),
                new Vector2(2, 3),
                0.5f);
            Assert.AreEqual(new Vector2(0.625f, 0.75f), res);
        }

        [TestMethod]
        public void DoubleInterpolator_Evaluate()
        {
            float expected = 0.61f;
            float res = DoubleInterpolator.Evaluate(
                Vector2.Zero,
                new Vector2(0, 0),
                new Vector2(1, 1),
                Vector2.One,
                expected);
            //the evaluation is not always perfect since it has to sample the bezier curve
            //we should garanty at least a precision of 0.001
            Assert.AreEqual(expected, res, 0.001f);
        }

        //[TestMethod] //this test doesn't pass for now TODO
        //public void DoubleInterpolator_InterpolateBetween()
        //{
        //    KeyframeFloat k1 = new KeyframeFloat(0, 0f);
        //    KeyframeFloat k2 = new KeyframeFloat(100, 100f);
        //    DoubleInterpolator inter = new DoubleInterpolator();

        //    float res = DoubleInterpolator.InterpolateBetween(0, 5, 0, 100, 50, Vector2.Zero, Vector2.Zero);
        //    Assert.AreEqual(1f, res);
        //}
    }
}
