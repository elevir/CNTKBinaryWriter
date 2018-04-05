using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CNTKBinaryWriter.Test
{
    [TestClass]
    public class StreamInfoTests
    {
        [TestMethod]
        public void TestCreate()
        {
            StreamInfo streamInfo = StreamInfo.Create("", 0, 3, true);
            Assert.IsTrue(streamInfo.Name == "" && streamInfo.DataType == 0 && streamInfo.IsSparse == 1 && streamInfo.Dimension == 3);
            streamInfo = StreamInfo.Create("", 1, 3);
            Assert.IsTrue(streamInfo.Name == "" && streamInfo.DataType == 1 && streamInfo.IsSparse == 0 && streamInfo.Dimension == 3);
            Assert.ThrowsException<NotSupportedException>(() => StreamInfo.Create("", 2, 3));
            
        }
    }
}
