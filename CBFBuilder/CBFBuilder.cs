using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace CNTKBinaryWriter
{
    public class CBFBuilder : IDisposable
    {
        private const UInt64 MAGIC_NUMBER = 0x636e746b5f62696e;
        private const UInt32 VERSION_NUMBER = 1;

        private ICollection<StreamInfo> _streams;
        private string _filePath;
        private BinaryWriter _binaryWriter;
        private List<ChunkHeader> _chunks;

        /// <summary>
        /// CBFBuilder allows write data in dense format in accordance with CNTK binary format
        /// https://docs.microsoft.com/en-us/cognitive-toolkit/brainscript-cntkbinary-reader
        /// </summary>
        /// <param name="streams">Definitions of streams</param>
        /// <param name="filePath">Path to output file</param>
        public CBFBuilder(ICollection<StreamInfo> streams, string filePath)
        {
            _streams = streams;
            _filePath = filePath;
            _binaryWriter = new BinaryWriter(File.Open(filePath, FileMode.Create));
            _chunks = new List<ChunkHeader>();
            WritePrefix();
        }

        private void WritePrefix()
        {
            _binaryWriter.Write(MAGIC_NUMBER);
            _binaryWriter.Write(VERSION_NUMBER);
        }

        private UInt32 WriteSequenceData<DataType>(
            StreamInfo stream,
            IEnumerable<DataType[]> sequences,
            Func<DataType[], byte[]> getBytes)
        {
            UInt32 totalCountOfSamples = 0;
            foreach (var sequence in sequences)
            {
                //actual number of samples
                if ((UInt32)sequence.Length % stream.Dimension != 0)
                    throw new InvalidDataException($"sequence must be a multiple of the dimension of a sample, but sequence.Length % stream.Dimension = {(UInt32)sequence.Length} % {stream.Dimension} = {(UInt32)sequence.Length % stream.Dimension}");
                UInt32 countOfSamples = (UInt32)sequence.Length / stream.Dimension;
                totalCountOfSamples += countOfSamples;
                _binaryWriter.Write(countOfSamples);
                byte[] bytesOfSequence = getBytes(sequence);
                _binaryWriter.Write(bytesOfSequence);
            }
            return totalCountOfSamples;
        }

        private IEnumerable<UInt32> GetMaxSeqsLength<DataType>(Dictionary<StreamInfo, IEnumerable<DataType[]>> data)
        {
            // suppose that one sample - one float value
            // stream 1:
            //     sequence 0: 1 2 3 4    (4 samples)
            //     sequence 1: 5 6 7 8 9  (5 samples)
            //     sequence 2: 1 2        (2 samples)
            // stream 2:
            //     sequence 0: 1 2 3 4 5  (5 samples)
            //     sequence 1: 1 2        (2 samples)
            //     sequence 2: 1 2 3      (3 samples)

            // for each stream select sequnces lengths, hence:
            // stream 1:
            //     sequence 0 length: 4
            //     sequence 1 length: 5
            //     sequence 2 length: 2
            // stream 2:
            //     sequence 0 length: 5
            //     sequence 1 length: 2
            //     sequence 2 length: 3

            // sequence_0_max = 5
            // sequence_1_max = 5
            // sequence_2_max = 3

            return data
                ?.Select(kv => kv.Value.Select(seq => (UInt32)seq.Length / kv.Key.Dimension))
                .Aggregate((acc, sampleCounts) => acc.Zip(sampleCounts, (f, s) => Math.Max(f, s)));
        }

        private IEnumerable<UInt32> GetMaxSeqsLength(IEnumerable<UInt32> floatDataLength, IEnumerable<UInt32> doubleDataLength)
        {
            if (floatDataLength != null && doubleDataLength != null)
            {
                return floatDataLength.Zip(doubleDataLength, (f, s) => Math.Max(f, s));
            }
            else if (floatDataLength != null)
            {
                return floatDataLength;
            }
            else
            {
                return doubleDataLength;
            }
        }

        private void CheckDataValid(
            Dictionary<StreamInfo, IEnumerable<float[]>> floatData,
            Dictionary<StreamInfo, IEnumerable<double[]>> doubleData,
            UInt32 numberOfSequences)
        {
            if (floatData == null && doubleData == null)
                throw new InvalidDataException("at least one of data source must be not null");
            bool countOfSequencesIsValid = true;
            if (floatData != null)
                countOfSequencesIsValid = countOfSequencesIsValid && floatData.Select(kv => kv.Value).All(values => values.Count() == numberOfSequences);
            if (doubleData != null)
                countOfSequencesIsValid = countOfSequencesIsValid && doubleData.Select(kv => kv.Value).All(values => values.Count() == numberOfSequences);
            if (!countOfSequencesIsValid)
                throw new InvalidDataException("sequences count for all inputs must be equal across chunk");
        }

        /// <summary>
        /// Write one chunk into the output file. Note that the count of sequences in each data parameter
        /// must be equal to number of sequences defined in numberOfSequeces parameter.
        /// </summary>
        /// <param name="floatData">Sequences of samples with type 'float'</param>
        /// <param name="doubleData">Sequences of samples with type 'double'</param>
        /// <param name="numberOfSequences">Number of sequences</param>
        /// <returns>CBFBuilder</returns>
        public CBFBuilder AddChunk(
            Dictionary<StreamInfo, IEnumerable<float[]>> floatData,
            Dictionary<StreamInfo, IEnumerable<double[]>> doubleData,
            UInt32 numberOfSequences)
        {
            CheckDataValid(floatData, doubleData, numberOfSequences);

            UInt64 offset = (ulong)_binaryWriter.BaseStream.Position;
            UInt32 totalCountOfSamples = 0;

            UInt32[] maxs = 
                GetMaxSeqsLength(GetMaxSeqsLength(floatData), GetMaxSeqsLength(doubleData))
                .ToArray();

            // write maximum number of samples across all sequences in chunk
            _binaryWriter.Write(maxs.SelectMany(max => BitConverter.GetBytes(max)).ToArray());

            // write data of each input
            foreach (var stream in _streams)
            {
                if (floatData.ContainsKey(stream))
                    totalCountOfSamples += WriteSequenceData(
                        stream,
                        floatData[stream],
                        sequence => 
                            sequence
                                .SelectMany(value => BitConverter.GetBytes(value))
                                .ToArray());
                else
                    totalCountOfSamples += WriteSequenceData(
                        stream,
                        doubleData[stream],
                        sequence => 
                            sequence
                                .SelectMany(value => BitConverter.GetBytes(value))
                                .ToArray());
            }
            _chunks.Add(
                new ChunkHeader(
                    offset,
                    numberOfSequences,
                    totalCountOfSamples));
            return this;
        }

        public void Dispose()
        {
            EndWriting();
            _binaryWriter.Dispose();
        }

        private void WriteStreamHeader(StreamInfo stream)
        {
            _binaryWriter.Write((byte)0); // only dense data
            _binaryWriter.Write((UInt32)stream.Name.Length);
            _binaryWriter.Write(Encoding.ASCII.GetBytes(stream.Name));
            _binaryWriter.Write((byte)stream.DataType);
            _binaryWriter.Write(stream.Dimension);
        }

        private void WriteChunkHeader(ChunkHeader chunk)
        {
            _binaryWriter.Write(chunk.Offset);
            _binaryWriter.Write(chunk.NumberOfSequencies);
            _binaryWriter.Write(chunk.TotalNumberOfSamples);
        }


        private void EndWriting()
        {
            UInt64 offset = (ulong)_binaryWriter.BaseStream.Position;
            UInt32 numberOfChunks = (UInt32)_chunks.Count;
            UInt32 numberOfStreams = (UInt32)_streams.Count;

            _binaryWriter.Write(MAGIC_NUMBER);
            _binaryWriter.Write(numberOfChunks);
            _binaryWriter.Write(numberOfStreams);

            foreach (var stream in _streams)
            {
                WriteStreamHeader(stream);
            }

            foreach (var chunk in _chunks)
            {
                WriteChunkHeader(chunk);
            }
            _binaryWriter.Write(offset);
            _binaryWriter.Close();
        }
    }
}
