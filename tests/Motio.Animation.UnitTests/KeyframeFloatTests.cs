using Microsoft.VisualStudio.TestTools.UnitTesting;
using Motio.Geometry;
using System.Collections.Generic;

//
// Vous pouvez utiliser les attributs supplémentaires suivants lorsque vous écrivez vos tests :
//
// Utilisez ClassInitialize pour exécuter du code avant d'exécuter le premier test de la classe
// [ClassInitialize()]
// public static void MyClassInitialize(TestContext testContext) { }
//
// Utilisez ClassCleanup pour exécuter du code une fois que tous les tests d'une classe ont été exécutés
// [ClassCleanup()]
// public static void MyClassCleanup() { }
//
// Utilisez TestInitialize pour exécuter du code avant d'exécuter chaque test 
// [TestInitialize()]
// public void MyTestInitialize() { }
//
// Utilisez TestCleanup pour exécuter du code après que chaque test a été exécuté
// [TestCleanup()]
// public void MyTestCleanup() { }
//
namespace Motio.Animation.UnitTests
{
    [TestClass]
    public class KeyframeFloatTests
    {
        /*
         * in a sorted list / compare
         * notifyproperty changed
         */

        [TestMethod]
        public void KeyframeFloat_Compare()
        {
            KeyframeFloat k1 = new KeyframeFloat(10, 2f);
            KeyframeFloat k2 = new KeyframeFloat(20, 1f);
            KeyframeFloat k3 = new KeyframeFloat(20, 15f);

            Assert.AreEqual(k1.CompareTo(k2), -1);
            Assert.AreEqual(k2.CompareTo(k1), 1);
            Assert.AreEqual(k2.CompareTo(k3), 0);
        }

        [TestMethod]
        public void KeyframeFloat_SortedList()
        {
            KeyframeFloat k1 = new KeyframeFloat(10, 2f);
            KeyframeFloat k2 = new KeyframeFloat(20, 1f);
            KeyframeFloat k3 = new KeyframeFloat(40, 15f);

            List<KeyframeFloat> ks = new List<KeyframeFloat>();
            ks.AddRange(new []{ k2, k3, k1 });

            ks.Sort();

            Assert.AreEqual(ks[0], k1);
            Assert.AreEqual(ks[1], k2);
            Assert.AreEqual(ks[2], k3);
        }

        [TestMethod]
        public void KeyframeFloat_PropertyChanged()
        {
            Dictionary<string, int> hits = new Dictionary<string, int>();

            KeyframeFloat k = new KeyframeFloat(0, 0f);
            k.PropertyChanged += (s, e) =>
            {
                if (hits.ContainsKey(e.PropertyName))
                    hits[e.PropertyName]++;
                else
                    hits.Add(e.PropertyName, 1);
            };
            k.Time = 123;
            k.Value = 1234;
            k.LeftHandle = new Vector2(1, 2);
            k.RightHandle = new Vector2(1, 2);

            Assert.AreEqual(hits[nameof(KeyframeFloat.RightHandle)], 1);
            Assert.AreEqual(hits[nameof(KeyframeFloat.LeftHandle)], 1);
            Assert.AreEqual(hits[nameof(KeyframeFloat.Value)], 1);
            Assert.AreEqual(hits[nameof(KeyframeFloat.Time)], 1);
        }
    }
}
