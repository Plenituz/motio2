using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Motio.Animation.UnitTests
{
    [TestClass]
    public class KeyframeHolderTests
    {
        /*
        * deux keyframes en lineaire
        * deux keyframe en bezier 
        * deux keyframe meme valeur
        * 
        * 
        */

        [TestMethod]
        public void KeyframeHolder_Count_Duplicate_Time()
        {
            KeyframeHolder h = new KeyframeHolder();
            h.AddKeyframe(new KeyframeFloat(0, 0));
            h.AddKeyframe(new KeyframeFloat(0, 1));
            h.AddKeyframe(new KeyframeFloat(1, 0));
            h.AddKeyframe(new KeyframeFloat(2, 0));
            Assert.AreEqual(4, h.Count);
        }

        [TestMethod]
        public void KeyframeHolder_AutoSort()
        {
            KeyframeFloat k1 = new KeyframeFloat(0, 0);
            KeyframeFloat k2 = new KeyframeFloat(1, 0);
            KeyframeFloat k3 = new KeyframeFloat(3, 0);
            KeyframeFloat k4 = new KeyframeFloat(5, 0);
            KeyframeHolder h = new KeyframeHolder();

            h.AddKeyframe(k4);
            h.AddKeyframe(k2);
            h.AddKeyframe(k1);
            h.AddKeyframe(k3);

            Assert.AreEqual(k1, h.KeyframeAt(0));
            Assert.AreEqual(k2, h.KeyframeAt(1));
            Assert.AreEqual(k3, h.KeyframeAt(2));
            Assert.AreEqual(k4, h.KeyframeAt(3));
        }

        [TestMethod]
        public void KeyframeHolder_Contains()
        {
            KeyframeFloat k1 = new KeyframeFloat(0, 0);
            KeyframeFloat k2 = new KeyframeFloat(1, 0);
            KeyframeHolder h = new KeyframeHolder();
            h.AddKeyframe(k1);
            h.AddKeyframe(k2);

            Assert.IsTrue(h.Contains(k1));
            Assert.IsTrue(h.Contains(k2));
            Assert.IsFalse(h.Contains(new KeyframeFloat(2, 0)));
        }

        [TestMethod]
        public void KeyframeHolder_KeyframeAtTime()
        {
            KeyframeFloat k1 = new KeyframeFloat(0, 0);
            KeyframeFloat k2 = new KeyframeFloat(15, 0);
            KeyframeHolder h = new KeyframeHolder();
            h.AddKeyframe(k1);
            h.AddKeyframe(k2);

            Assert.AreEqual(k2, h.GetKeyframeAtTime(15));
            Assert.AreEqual(k1, h.GetKeyframeAtTime(0));
            Assert.IsNull(h.GetKeyframeAtTime(1));
        }

        [TestMethod]
        public void KeyframeHolder_NextClosestKeyframe()
        {
            KeyframeFloat k1 = new KeyframeFloat(0, 0);
            KeyframeFloat k2 = new KeyframeFloat(15, 0);
            KeyframeHolder h = new KeyframeHolder();
            h.AddKeyframe(k1);
            h.AddKeyframe(k2);

            h.GetNextClosestKeyframe(5, out KeyframeFloat res, out int index);
            Assert.AreEqual(k2, res);
            Assert.AreEqual(1, index);
        }

        [TestMethod]
        public void KeyframeHolder_PropertyChanged()
        {
            Dictionary<string, int> hits = new Dictionary<string, int>();
            KeyframeFloat k1 = new KeyframeFloat(0, 0);
            KeyframeFloat k2 = new KeyframeFloat(15, 0);
            KeyframeHolder h = new KeyframeHolder();
            h.AddKeyframe(k1);

            h.PropertyChanged += (s, e) =>
            {
                if (hits.ContainsKey(e.PropertyName))
                    hits[e.PropertyName]++;
                else
                    hits.Add(e.PropertyName, 1);
            };

            k1.Time = 5;
            h.AddKeyframe(k2);

            Assert.AreEqual(1, hits[nameof(KeyframeFloat.Time)]);
            Assert.AreEqual(1, hits["keyframes"]);
        }

        [TestMethod]
        public void KeyframeHolder_AutoSort_AfterTimeChanged()
        {
            KeyframeFloat k1 = new KeyframeFloat(0, 0);
            KeyframeFloat k2 = new KeyframeFloat(7, 0);
            KeyframeFloat k3 = new KeyframeFloat(3, 0);
            KeyframeFloat k4 = new KeyframeFloat(5, 0);
            KeyframeHolder h = new KeyframeHolder();
            h.AddKeyframe(k4);
            h.AddKeyframe(k2);
            h.AddKeyframe(k1);
            h.AddKeyframe(k3);

            k2.Time = 1;

            Assert.AreEqual(k1, h.KeyframeAt(0));
            Assert.AreEqual(k2, h.KeyframeAt(1));
            Assert.AreEqual(k3, h.KeyframeAt(2));
            Assert.AreEqual(k4, h.KeyframeAt(3));
        }
    }
}
