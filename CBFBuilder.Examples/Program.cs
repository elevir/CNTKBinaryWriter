using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CNTKBinaryWriter;

namespace CNTKBinaryWriter.Examples
{
    class Program
    {
        static void Main(string[] args)
        {
            // set some file path
            string filePath = "some_file.bin";

            // define desired inputs
            StreamInfo[] inputs = new StreamInfo[]
            {
                StreamInfo.Create(name: "input1", dataType: 0, dimension: 3, isSparse: true),
                StreamInfo.Create(name: "input2", dataType: 1, dimension: 4, isSparse: false),
                StreamInfo.Create(name: "input3", dataType: 0, dimension: 2, isSparse: true)
                // etc...
            };

            // get data for all of this inputs
            // make sure that number of sequences across all inputs are equal and count of values in each sequence is multiple of sample dimension

            Random rand = new Random();

            List<float[]> input1Data = new List<float[]>()
            {
                Enumerable.Range(0, 9).Select(_ => (float)rand.Next()).ToArray(), // sequence 1
                Enumerable.Range(0, 3).Select(_ => (float)rand.Next()).ToArray(), // sequence 2
                Enumerable.Range(0, 12).Select(_ => (float)rand.Next()).ToArray() // sequence 3
            };

            List<double[]> input2Data = new List<double[]>()
            {
                Enumerable.Range(0, 8).Select(_ => rand.NextDouble()).ToArray(),
                Enumerable.Range(0, 4).Select(_ => rand.NextDouble()).ToArray(),
                Enumerable.Range(0, 16).Select(_ => rand.NextDouble()).ToArray()
            };

            List<float[]> input3Data = new List<float[]>()
            {
                Enumerable.Range(0, 8).Select(_ => (float)rand.Next()).ToArray(),
                Enumerable.Range(0, 4).Select(_ => (float)rand.Next()).ToArray(),
                Enumerable.Range(0, 12).Select(_ => (float)rand.Next()).ToArray()
            };

            Dictionary<StreamInfo, IEnumerable<object>> data = new Dictionary<StreamInfo, IEnumerable<object>>()
            {
                { inputs[0], input1Data },
                { inputs[1], input2Data },
                { inputs[2], input3Data }
            };

            // create our builder
            using (var cbf = new CBFBuilder(inputs, filePath))
            {
                cbf
                    .AddChunk(data) // You can finish on this place
                    .AddChunk(data); // But also you can use CBFBuilder such as StringBuilder
                // You can use loops too
                for (int i = 0; i < 10; ++i)
                {
                    cbf.AddChunk(data);
                }
            }

            // check your CNTK binary file! :)
        }
    }
}
