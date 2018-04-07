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
        /// CBFBuilder allows write data in accordance with CNTK binary format
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

        private IEnumerable<UInt32> GetMaxSeqsLength(Dictionary<StreamInfo, IEnumerable<object>> data)
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
                .Select(kv => kv.Key.GetSequencesLengths(kv.Value))
                .Aggregate((acc, sampleCounts) => acc.Zip(sampleCounts, (f, s) => Math.Max(f, s)));
        }

        private void CheckDataValid(Dictionary<StreamInfo, IEnumerable<object>> data)
        {
            int countOfDifferentSequenceLentghs = data.Select(kv => kv.Key.GetCountOfSequences(kv.Value))
                .Distinct() // after disctinct we must have only one element, otherwise at least one of inputs have more sequences than others
                .Count();
            if (countOfDifferentSequenceLentghs != 1)
                throw new InvalidDataException("sequences count for all inputs must be equal across chunk");
        }

        /// <summary>
        /// Write one chunk into the output file.
        /// </summary>
        /// <param name="data">Sequences of samples for each input (object must be float[] or double[])</param>
        /// <returns>CBFBuilder</returns>
        public CBFBuilder AddChunk(Dictionary<StreamInfo, IEnumerable<object>> data)
        {
            CheckDataValid(data);

            UInt64 offset = (ulong)_binaryWriter.BaseStream.Position;
            UInt32[] maxs = GetMaxSeqsLength(data).ToArray();
            UInt32 numberOfSequences = (UInt32)maxs.Length;
            
            // write maximum number of samples across all sequences in chunk
            _binaryWriter.Write(maxs.SelectMany(BitConverter.GetBytes).ToArray());

            // write data of each input
            UInt32 totalCountOfSamples = 0;
            foreach (var stream in _streams)
            {
                totalCountOfSamples += stream.GetCountOfSamples(data[stream]);
                _binaryWriter.Write(stream.GetData(data[stream]));
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
            _binaryWriter.Write((byte)stream.IsSparse);
            _binaryWriter.Write((UInt32)stream.Name.Length);
            _binaryWriter.Write(Encoding.ASCII.GetBytes(stream.Name));
            _binaryWriter.Write((byte)stream.DataType);
            _binaryWriter.Write(stream.Dimension);
        }

        private void WriteChunkHeader(ChunkHeader chunk)
        {
            _binaryWriter.Write(chunk.Offset);
            _binaryWriter.Write(chunk.NumberOfSequences);
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
