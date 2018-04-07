using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CNTKBinaryWriter
{
    public static class StreamInfoExtensions
    {
        public static UInt32 GetCountOfSequences(this StreamInfo streamInfo, IEnumerable<object> sequences)
        {
            return (UInt32)sequences.Count();
        }

        public static IEnumerable<UInt32> GetSequencesLengths(this StreamInfo streamInfo, IEnumerable<object> data)
        {
            IEnumerable<UInt32> result = null;
            if (streamInfo.DataType == 0)
            {
                IEnumerable<float[]> floatData = data.Cast<float[]>().ToArray();
                if (!floatData.All(sequence => sequence.Length % streamInfo.Dimension == 0))
                    throw new FormatException(
                        $"sequence must be a multiple of the dimension of a sample, but of sequences has mod > 0");
                result = floatData.Select(sequence => (UInt32)sequence.Length / streamInfo.Dimension);
            }
            else if (streamInfo.DataType == 1)
            {
                IEnumerable<double[]> doubleData = data.Cast<double[]>().ToArray();
                if (!doubleData.All(sequence => sequence.Length % streamInfo.Dimension == 0))
                    throw new FormatException(
                        $"sequence must be a multiple of the dimension of a sample, but of sequences has mod > 0");
                result = doubleData.Select(sequence => (UInt32)sequence.Length / streamInfo.Dimension);
            }
            return result.ToArray(); // avoid recalculation
        }

        public static UInt32 GetCountOfSamples(this StreamInfo streamInfo, IEnumerable<object> data)
        {
            return (UInt32)streamInfo.GetSequencesLengths(data).Sum(v => (int)v);
        }

        public static void GetSparseSequence<DataType>(
            DataType[] sequence, DataType zero, UInt32 dimension,
            out DataType[] nonZeroElements, out Int32[] nonZeroIndices, out Int32[] nonZeroElementsCountPerSample)
            where DataType : struct, IComparable
        {
            var result = sequence
                .Select((v, i) => new { Value = v, SampleId = i / dimension }) // get samples idxs for each value
                .GroupBy(v => v.SampleId) // grouping by sample id
                .Select(g =>
                    g.Select((v, idx) => new { v.Value, Idx = idx }) // add to value its index in sample
                     .Where(v => v.Value.CompareTo(zero) != 0) // get only non zero values from sample
                     )
                .Select(nz => new { NZWithIdx = nz, CountOfNZ = nz.Count() }); // add count of non zero values count to each sample

            // getting result
            nonZeroElements = result.SelectMany(g => g.NZWithIdx.Select(nz => nz.Value)).ToArray(); 
            nonZeroIndices = result.SelectMany(g => g.NZWithIdx.Select(nz => nz.Idx)).ToArray();
            nonZeroElementsCountPerSample = result.Select(g => g.CountOfNZ).ToArray();
        }

        public static byte[] GetSparseData(this StreamInfo streamInfo, IEnumerable<object> data)
        {
            IEnumerable<uint> lengths = streamInfo.GetSequencesLengths(data);
            var lengthsDatas = lengths.Zip(data, (l, d) => Tuple.Create(l, d));
            List<byte[]> results = new List<byte[]>((int)streamInfo.GetCountOfSequences(data) * 5);
            foreach (var lengthData in lengthsDatas)
            {
                byte[] nonZeroValuesInBytes = null;
                Int32[] nonZeroIndices = null;
                Int32[] nonZeroCountPerSample = null;

                if (streamInfo.DataType == 0)
                {
                    GetSparseSequence(
                        lengthData.Item2 as float[], 0.0f, streamInfo.Dimension,
                        out float[] nonZeroElements, out nonZeroIndices, out nonZeroCountPerSample);
                    nonZeroValuesInBytes = nonZeroElements.SelectMany(BitConverter.GetBytes).ToArray();
                }
                if (streamInfo.DataType == 1)
                {
                    GetSparseSequence(
                        lengthData.Item2 as double[], 0.0, streamInfo.Dimension,
                        out double[] nonZeroElements, out nonZeroIndices, out nonZeroCountPerSample);
                    nonZeroValuesInBytes = nonZeroElements.SelectMany(BitConverter.GetBytes).ToArray();
                }

                byte[] actualNumberOfSamplesInBytes = BitConverter.GetBytes(lengthData.Item1);
                byte[] nonZeroIndicesInBytes = nonZeroIndices.SelectMany(BitConverter.GetBytes).ToArray();
                byte[] nonZeroCountPerSampleInBytes = nonZeroCountPerSample.SelectMany(BitConverter.GetBytes).ToArray();
                byte[] totalNumberOfNonZeroElementsInBytes = BitConverter.GetBytes(nonZeroCountPerSample.Sum());

                results.Add(actualNumberOfSamplesInBytes);
                results.Add(totalNumberOfNonZeroElementsInBytes);
                results.Add(nonZeroValuesInBytes);
                results.Add(nonZeroIndicesInBytes);
                results.Add(nonZeroCountPerSampleInBytes);
            }
            return results.SelectMany(v => v).ToArray();
        }

        public static byte[] GetDenseData(this StreamInfo streamInfo, IEnumerable<object> data)
        {
            IEnumerable<uint> lengths = streamInfo.GetSequencesLengths(data);
            var lengthsDatas = lengths.Zip(data, (l, d) => Tuple.Create(l, d));
            int valueSize = streamInfo.DataType == 0 ? sizeof(float) : sizeof(double);
            List<byte[]> results = new List<byte[]>((int)streamInfo.GetCountOfSequences(data) * 2);
            foreach (var lengthData in lengthsDatas)
            {
                //actual number of samples
                byte[] countOfSamplesBytes = BitConverter.GetBytes(lengthData.Item1);

                byte[] bytesOfSequence = null;

                if (streamInfo.DataType == 0)
                {
                    float[] sequence = lengthData.Item2 as float[];
                    bytesOfSequence = sequence
                        .Select(BitConverter.GetBytes)
                        .SelectMany(v => v).ToArray();
                }
                if (streamInfo.DataType == 1)
                {
                    double[] sequence = lengthData.Item2 as double[];
                    bytesOfSequence = sequence
                        .Select(BitConverter.GetBytes)
                        .SelectMany(v => v).ToArray();
                }
                results.Add(countOfSamplesBytes);
                results.Add(bytesOfSequence);
            }
            return results.SelectMany(v => v).ToArray();
        }

        public static byte[] GetData(this StreamInfo streamInfo, IEnumerable<object> data)
        {
            if (streamInfo.IsSparse == 1)
                return streamInfo.GetSparseData(data);
            return streamInfo.GetDenseData(data);
        }
    }
}
