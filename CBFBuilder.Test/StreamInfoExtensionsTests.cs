using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using CNTKBinaryWriter;
using System.Collections.Generic;

namespace CNTKBinaryWriter.Test
{
    [TestClass]
    public class StreamInfoExtensionsTests
    {
        [TestMethod]
        public void TestWrongCountOfValuesException()
        {
            object[] sequences = null;
            StreamInfo streamInfo = null;

            // float data

            streamInfo = StreamInfo.Create("", 0, 3);
            float[] fdata = new float[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            sequences = new object[] { fdata.Clone(), fdata.Clone() };
            Assert.ThrowsException<FormatException>(() => streamInfo.GetDenseData(sequences));
            Assert.ThrowsException<FormatException>(() => streamInfo.GetSparseData(sequences));
            Assert.ThrowsException<FormatException>(() => streamInfo.GetCountOfSamples(sequences));
            Assert.ThrowsException<FormatException>(() => streamInfo.GetSequencesLengths(sequences));

            // double data

            streamInfo = StreamInfo.Create("", 1, 3);
            double[] ddata = new double[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            sequences = new object[] { ddata.Clone(), ddata.Clone() };
            Assert.ThrowsException<FormatException>(() => streamInfo.GetDenseData(sequences));
            Assert.ThrowsException<FormatException>(() => streamInfo.GetSparseData(sequences));
            Assert.ThrowsException<FormatException>(() => streamInfo.GetCountOfSamples(sequences));
            Assert.ThrowsException<FormatException>(() => streamInfo.GetSequencesLengths(sequences));

        }

        [TestMethod]
        public void TestGetCountOfSamples()
        {
            UInt32 result = 0;
            object[] sequences = null;
            StreamInfo streamInfo = null;

            // float data

            streamInfo = StreamInfo.Create("", 0, 3);
            float[] fdata = new float[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            sequences = new object[] { fdata.Clone(), fdata.Clone() };
            result = streamInfo.GetCountOfSamples(sequences);
            Assert.AreEqual(result, 6u, $"Count of samples is wrong (float)");

            // double data

            streamInfo = StreamInfo.Create("", 1, 3);
            double[] ddata = new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            sequences = new object[] { ddata.Clone(), ddata.Clone() };
            result = streamInfo.GetCountOfSamples(sequences);
            Assert.AreEqual(result, 6u, $"Count of samples is wrong (float)");
        }

        [TestMethod]
        public void TestGetCountOfSequences()
        {
            UInt32 result = 0;
            object[] sequences = null;
            StreamInfo streamInfo = null;

            // float data

            streamInfo = StreamInfo.Create("", 0, 3);
            float[] fdata = new float[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            sequences = new object[] { fdata.Clone(), fdata.Clone() };
            result = streamInfo.GetCountOfSequences(sequences);
            Assert.AreEqual(result, (uint)sequences.Length, $"Count of sequences is wrong");
        }

        [TestMethod]
        public void TestGetSequencesLengths()
        {
            UInt32[] expected = null;
            UInt32[] result = null;
            object[] sequences = null;
            StreamInfo streamInfo = null;

            // float data

            streamInfo = StreamInfo.Create("", 0, 3);
            float[] fdata = new float[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            sequences = new object[] { fdata.Clone(), fdata.Clone() };
            result = streamInfo.GetSequencesLengths(sequences).ToArray();
            expected = new UInt32[] { 3, 3 };

            Assert.IsTrue(expected.Length == result.Length, "Lengths are different");
            Assert.IsTrue(result.Zip(expected, (f, s) => f == s).All(v => v), $"Count of sequences is wrong");

            // double data

            streamInfo = StreamInfo.Create("", 1, 3);
            double[] ddata = new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            sequences = new object[] { ddata.Clone(), ddata.Clone() };
            result = streamInfo.GetSequencesLengths(sequences).ToArray();

            expected = new UInt32[] { 3, 3 };

            Assert.IsTrue(expected.Length == result.Length, "Lengths are different");
            Assert.IsTrue(result.Zip(expected, (f, s) => f == s).All(v => v), $"Count of sequences is wrong");
        }

        [TestMethod]
        public void TestGetDenseData()
        {
            byte[] result = null;
            byte[] expected = null;
            object[] sequences = null;
            StreamInfo streamInfo = null;

            // float data

            streamInfo = StreamInfo.Create("", 0, 3);
            float[] fdata = new float[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            sequences = new object[] { fdata.Clone(), fdata.Clone() };
            result = streamInfo.GetDenseData(sequences);

            expected = BitConverter.GetBytes((UInt32)3)
               .Concat(fdata.SelectMany(v => BitConverter.GetBytes(v)))
               .Concat(BitConverter.GetBytes((UInt32)3))
               .Concat(fdata.SelectMany(v => BitConverter.GetBytes(v)))
               .ToArray();

            Assert.IsTrue(result.Count() == expected.Count(), "result count is not equal to expected count");
            Assert.IsTrue(result.Zip(expected, (f, s) => f == s).All(v => v), "result is not equal to expected value");

            // double data

            streamInfo = StreamInfo.Create("", 1, 3);
            double[] ddata = new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            sequences = new object[] { ddata.Clone(), ddata.Clone() };
            result = streamInfo.GetDenseData(sequences);

            expected = BitConverter.GetBytes((UInt32)3)
               .Concat(ddata.SelectMany(v => BitConverter.GetBytes(v)))
               .Concat(BitConverter.GetBytes((UInt32)3))
               .Concat(ddata.SelectMany(v => BitConverter.GetBytes(v)))
               .ToArray();

            Assert.IsTrue(result.Count() == expected.Count(), "result count is not equal to expected count");
            Assert.IsTrue(result.Zip(expected, (f, s) => f == s).All(v => v), "result is not equal to expected value");
        }

        [TestMethod]
        public void TestGetSparseData()
        {
            byte[] result = null;
            byte[] expected = null;
            object[] sequences = null;
            StreamInfo streamInfo = null;

            // float data

            streamInfo = StreamInfo.Create("", 0, 3);
            float[] fdata = new float[] { 0, 2, 3, 4, 0, 6, 7, 8, 0 };
            sequences = new object[] { fdata.Clone(), fdata.Clone() };
            result = streamInfo.GetSparseData(sequences);
            expected = 
                BitConverter.GetBytes((UInt32)3) //samples
                .Concat(BitConverter.GetBytes(6)) // non zero values count
                .Concat(fdata.Where(v => v != 0).SelectMany(BitConverter.GetBytes)) // non zero values
                .Concat(new int[] { 1, 2, 0, 2, 0, 1 }.SelectMany(BitConverter.GetBytes)) // indices
                .Concat(new int[] { 2, 2, 2 }.SelectMany(BitConverter.GetBytes)) // count of non zero per sample
                
                .Concat(BitConverter.GetBytes((UInt32)3))  // samples
                .Concat(BitConverter.GetBytes(6)) // non zero values count
                .Concat(fdata.Where(v => v != 0).SelectMany(BitConverter.GetBytes)) // non zero values
                .Concat(new int[] { 1, 2, 0, 2, 0, 1 }.SelectMany(BitConverter.GetBytes)) // indices
                .Concat(new int[] { 2, 2, 2 }.SelectMany(BitConverter.GetBytes)) // count of non zero per sample
                .ToArray();

            Assert.IsTrue(result.Count() == expected.Count(), "result count is not equal to expected count");
            Assert.IsTrue(result.Zip(expected, (f, s) => f == s).All(v => v), "result is not equal to expected value");

            // double data

            streamInfo = StreamInfo.Create("", 1, 3);
            double[] ddata = new double[] { 0, 2, 3, 4, 0, 6, 7, 8, 0 };
            sequences = new object[] { ddata.Clone(), ddata.Clone() };
            result = streamInfo.GetSparseData(sequences);
            expected =
                BitConverter.GetBytes((UInt32)3) //samples
                .Concat(BitConverter.GetBytes(6)) // non zero values count
                .Concat(ddata.Where(v => v != 0).SelectMany(BitConverter.GetBytes)) // non zero values
                .Concat(new int[] { 1, 2, 0, 2, 0, 1 }.SelectMany(BitConverter.GetBytes)) // indices
                .Concat(new int[] { 2, 2, 2 }.SelectMany(BitConverter.GetBytes)) // count of non zero per sample

                .Concat(BitConverter.GetBytes((UInt32)3))  // samples
                .Concat(BitConverter.GetBytes(6)) // non zero values count
                .Concat(ddata.Where(v => v != 0).SelectMany(BitConverter.GetBytes)) // non zero values
                .Concat(new int[] { 1, 2, 0, 2, 0, 1 }.SelectMany(BitConverter.GetBytes)) // indices
                .Concat(new int[] { 2, 2, 2 }.SelectMany(BitConverter.GetBytes)) // count of non zero per sample
                .ToArray();

            Assert.IsTrue(result.Count() == expected.Count(), "result count is not equal to expected count");
            Assert.IsTrue(result.Zip(expected, (f, s) => f == s).All(v => v), "result is not equal to expected value");
        }
    }
}
