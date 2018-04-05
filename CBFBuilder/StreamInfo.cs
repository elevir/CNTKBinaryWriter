using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CNTKBinaryWriter
{
    public class StreamInfo
    {
        public string Name { get; private set; }
        public int DataType { get; private set; }
        public UInt32 Dimension { get; private set; }
        public int IsSparse { get; private set; }

        /// <param name="name">Name of input stream</param>
        /// <param name="dataType">Data type of stream (0 - float32, 1 - double)</param>
        /// <param name="dimension">Dimension of each sample in stream</param>
        private StreamInfo(string name, int dataType, UInt32 dimension, int isSparse)
        {
            Name = name;
            DataType = dataType;
            Dimension = dimension;
            IsSparse = isSparse;
        }

        /// <summary>
        /// Create stream definiton
        /// </summary>
        /// <param name="name">Name of input stream</param>
        /// <param name="dataType">Data type of stream (0 - float32, 1 - double)</param>
        /// <param name="dimension">Dimension of each sample in stream</param>
        /// <returns>Stream</returns>
        public static StreamInfo Create(string name, int dataType, UInt32 dimension, bool isSparse = false)
        {
            if (dataType != 0 && dataType != 1)
                throw new NotSupportedException("Supported only float - 0 and double - 1 data types");
            return new StreamInfo(name, dataType, dimension, isSparse ? 1 : 0);
        }
    }
}
