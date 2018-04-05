using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using CNTKBinaryWriter;
using System.IO;
using System.Reflection;

namespace CNTK.Formatters.Tests
{
    [TestClass]
    public class CBFBuilderTests
    {
        private Dictionary<StreamInfo, IEnumerable<object>> MakeData()
        {
            StreamInfo a = StreamInfo.Create("a", 0, 1);
            StreamInfo b = StreamInfo.Create("b", 1, 1, true);
            List<float[]> aData = new List<float[]>
            {
                new float[] { 1, 2, 3, 4 },
                new float[] { 1, 2, 3, 4, 5 },
                new float[] { 1, 2 }
            };
            List<double[]> bData = new List<double[]>
            {
                new double[] { 1, 2, 3, 4, 5 },
                new double[] { 1, 2 },
                new double[] { 1, 2, 3 }
            };

            return new Dictionary<StreamInfo, IEnumerable<object>>
            {
                { a, aData },
                { b, bData }
            };
        }

        private Dictionary<StreamInfo, IEnumerable<object>> MakeWrongData()
        {
            StreamInfo a = StreamInfo.Create("a", 0, 1);
            StreamInfo b = StreamInfo.Create("b", 1, 1, true);
            List<float[]> aData = new List<float[]>
            {
                new float[] { 1, 2, 3, 4 },
                new float[] { 1, 2, 3, 4, 5 },
                new float[] { 1, 2 }
            };
            List<double[]> bData = new List<double[]>
            {
                new double[] { 1, 2, 3, 4, 5 },
                new double[] { 1, 2 }
            };

            return new Dictionary<StreamInfo, IEnumerable<object>>
            {
                { a, aData },
                { b, bData }
            };
        }

        [TestMethod]
        public void TestGetMaxLengths()
        {
            var data = MakeData();
            using (var cbf = new CBFBuilder(data.Select(kv => kv.Key).ToArray(), "test.bin"))
            {
                var cbfPO = new PrivateObject(cbf);
                var retval = cbfPO.Invoke(
                    "GetMaxSeqsLength",
                    new object[] { data }) as IEnumerable<UInt32>;
                Assert.IsTrue(retval.Zip(new UInt32[] { 5, 5, 3 }, (f, s) => f == s).All(v => v),
                    $"Expected (5, 5, 3), but returned {String.Join(", ", retval)}");
            }
        }

        [TestMethod]
        public void TestCheckDataValid()
        {
            var data = MakeData();
            using (var cbf = new CBFBuilder(data.Select(kv => kv.Key).ToArray(), "test.bin"))
            {
                var cbfPO = new PrivateObject(cbf);
                cbfPO.Invoke(
                    "CheckDataValid",
                    new object[] { data });
            }

            data = MakeWrongData();
            using (var cbf = new CBFBuilder(data.Select(kv => kv.Key).ToArray(), "test.bin"))
            {
                var cbfPO = new PrivateObject(cbf);
                Assert.ThrowsException<TargetInvocationException>(() => cbfPO.Invoke("CheckDataValid", new object[] { data }));
            }
        }
    }
}
