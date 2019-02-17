# CNTKBinaryWriter

| **NuGet** |
|-------------|
[![NuGet version](https://badge.fury.io/nu/CNTKBinaryWriter.svg)](https://badge.fury.io/nu/CNTKBinaryWriter)

This library allows to write data in accordance with [CNTK Binary Format](https://docs.microsoft.com/en-us/cognitive-toolkit/brainscript-cntkbinary-reader).

How to use it?
1. First you have to define data streams:
```
StreamInfo[] inputs = new StreamInfo[]
{
    StreamInfo.Create(name: "input1", dataType: 0, dimension: 3, isSparse: true),
    StreamInfo.Create(name: "input2", dataType: 1, dimension: 4, isSparse: false),
    StreamInfo.Create(name: "input3", dataType: 0, dimension: 2, isSparse: true)
    // etc...
};
```
2. Generate some data. Note that each data block is block of sequences and now only double and float data supported.
```
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
```

3. Next step is making dictionary stream -> data. Make sure that the data size is compatible with streams definitions.
```
Dictionary<StreamInfo, IEnumerable<object>> data = new Dictionary<StreamInfo, IEnumerable<object>>()
{
    { inputs[0], input1Data },
    { inputs[1], input2Data },
    { inputs[2], input3Data }
};
```
4. And last step is writing data chunks into the file:
```
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
```

It is important(!) to use statement `using` or invoke Dispose() after data was written, otherwise data file will be broken, and CNTK couldn't parse your file.

Full code of example you can see [here](https://github.com/elevir/CNTKBinaryWriter/blob/master/CBFBuilder.Examples/Program.cs)
