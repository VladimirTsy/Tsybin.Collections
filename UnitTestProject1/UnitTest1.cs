using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using myCollection;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestExpected10And20()
        {
            var extDict = new ExtendedDictionary<int, int, int>();
            extDict[1, 2] = 10;
            extDict[4, 5] = 20;
            Assert.AreEqual(10, extDict[1, 2]);
            Assert.AreEqual(20, extDict[4, 5]);
        }

        [TestMethod]
        public void TestFastAccessByIdExpected_30_40()
        {
            var extDict = new ExtendedDictionary<string, int, int>();
            extDict.Add("test", 1, 30);
            extDict.Add("test", 4, 40);
            extDict.Add("no_test", 6, 7);

            CollectionAssert.AreEqual(new int[] { 30, 40 }, extDict.GetById("test"));
        }

        [TestMethod]
        public void TestFastAccessByNameExpected_30_40()
        {
            var extDict = new ExtendedDictionary<int, string, int>();
            extDict.Add(2, "test", 30);
            extDict.Add(3, "test", 40);
            extDict.Add(5, "no_test", 7);

            CollectionAssert.AreEqual(new int[] { 30, 40 }, extDict.GetByName("test"));
        }

        [TestMethod]
        public void TestCountExpected_5()
        {
            var extDict = new ExtendedDictionary<int, string, int>();
            extDict.Add(2, "test", 2);
            extDict.Add(3, "test", 5);
            extDict.Add(5, "no_test", 7);
            extDict.Add(6, "no_test", 7);
            extDict.Add(7, "no_test", 7);

            Assert.AreEqual(5, extDict.Count);
        }

        [TestMethod]
        public void TestEnumirableExpected_5()
        {
            var extDict = new ExtendedDictionary<int, string, int>();
            extDict.Add(2, "test", 2);
            extDict.Add(3, "test", 5);
            extDict.Add(5, "no_test", 7);
            extDict.Add(6, "no_test", 7);
            extDict.Add(7, "no_test", 7);

            int tmp = 0;
            foreach (var item in extDict)
            {
                tmp++;
            }

            Assert.AreEqual(5, tmp);
        }

        [TestMethod]
        public void TestRemoveExpected_4()
        {
            var extDict = new ExtendedDictionary<int, string, int>();
            extDict.Add(2, "test", 2);
            extDict.Add(3, "test", 5);
            extDict.Add(5, "no_test", 7);
            extDict.Add(6, "no_test", 7);
            extDict.Add(7, "no_test", 7);

            extDict.Remove(7, "no_test");

            Assert.AreEqual(4, extDict.Count);
        }

        [TestMethod]
        public void TestClearExpected_0()
        {
            var extDict = new ExtendedDictionary<int, string, int>();
            extDict.Add(2, "test", 2);
            extDict.Add(3, "test", 5);
            extDict.Add(5, "no_test", 7);
            extDict.Add(6, "no_test", 7);
            extDict.Add(7, "no_test", 7);

            extDict.Clear();

            Assert.AreEqual(0, extDict.Count);
        }

    

        [TestMethod]
        public void TestParallelModificationSafityExpected_4950()
        {
            var extDict = new ExtendedDictionary<int, string, int>();
            extDict.Add(2, "test", 0);

            Parallel.For(0, 100,
                (i) =>
                {
                    extDict.EnterUpgradeableReadLock();
                    extDict[2, "test"] += i;
                    extDict.ExitUpgradeableReadLock();
                });

            Assert.AreEqual(4950, extDict[2, "test"]);
        }
    }
}
