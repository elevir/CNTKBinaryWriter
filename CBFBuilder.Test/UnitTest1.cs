using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CNTK.Formatters;
using System.Collections.Generic;

namespace CNTK.Formatters.Tests
{
    [TestClass]
    public class UnitTest1
    {
        private Dictionary<StreamInfo, IEnumerable<float[]>> MakeData()
        {
            StreamInfo a = StreamInfo.Create("a", 0, 1);
            StreamInfo b = StreamInfo.Create("b", 0, 1);
            List<float[]> aData = new List<float[]>();
            aData.Add(new float[] { 1, 2, 3, 4 });
            aData.Add(new float[] { 1, 2, 3, 4, 5 });
            aData.Add(new float[] { 1, 2 });
            List<float[]> bData = new List<float[]>();
            bData.Add(new float[] { 1, 2, 3, 4, 5 });
            bData.Add(new float[] { 1, 2 });
            bData.Add(new float[] { 1, 2, 3 });

            return new Dictionary<StreamInfo, IEnumerable<float[]>>
            {
                { a, aData },
                { b, bData }
            };
        }

        [TestMethod]
        public void TestGenericGetMaxLengths()
        {
            var data = MakeData();
            using (var cbf = new CBFBuilder(data.Select(kv => kv.Key).ToArray(), "test.bin"))
            {
                var cbfPO = new PrivateObject(cbf);
                var retval = cbfPO.Invoke(
                    "GetMaxSeqsLength",
                    new Type[] { typeof(Dictionary<StreamInfo, IEnumerable<float[]>>) },
                    new object[] { data },
                    new Type[] { typeof(float) }) as IEnumerable<UInt32>;
                Assert.IsTrue(retval.Zip(new UInt32[] { 5, 5, 3 }, (f, s) => f == s).All(v => v),
                    $"Expected (5, 5, 3), but returned {String.Join(", ", retval)}");
            }
        }
    }
}
