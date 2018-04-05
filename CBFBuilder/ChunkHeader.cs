using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CNTKBinaryWriter
{
    internal class ChunkHeader
    {
        public UInt64 Offset { get; private set; }
        public UInt32 NumberOfSequencies { get; private set; }
        public UInt32 TotalNumberOfSamples { get; private set; }

        public ChunkHeader(UInt64 offset, UInt32 numberOfSequencies, UInt32 totalNumberOfSamples)
        {
            Offset = offset;
            NumberOfSequencies = numberOfSequencies;
            TotalNumberOfSamples = totalNumberOfSamples;
        }
    }
}
