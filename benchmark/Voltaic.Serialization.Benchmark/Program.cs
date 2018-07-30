using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using Newtonsoft.Json;

namespace Voltaic.Serialization.Benchmark
{
    public class TestData
    {
        public class SignedData
        {
            [ModelProperty("Value1"), JsonProperty("Value1")]
            public sbyte Value1 { get; set; } = 100;
            [ModelProperty("Value2"), JsonProperty("Value2")]
            public short Value2 { get; set; } = 100;
            [ModelProperty("Value3"), JsonProperty("Value3")]
            public int Value3 { get; set; } = 100;
            [ModelProperty("Value4"), JsonProperty("Value4")]
            public long Value4 { get; set; } = 100;
            [ModelProperty("Array1"), JsonProperty("Array1")]
            public sbyte[] Array1 { get; set; } = new sbyte[] { 100 };
            [ModelProperty("Array2"), JsonProperty("Array2")]
            public short[] Array2 { get; set; } = new short[] { 100 };
            [ModelProperty("Array3"), JsonProperty("Array3")]
            public int[] Array3 { get; set; } = new int[] { 100 };
            [ModelProperty("Array4"), JsonProperty("Array4")]
            public long[] Array4 { get; set; } = new long[] { 100 };
        }
        public class UnsignedData
        {
            [ModelProperty("Value1"), JsonProperty("Value1")]
            public byte Value1 { get; set; } = 100;
            [ModelProperty("Value2"), JsonProperty("Value2")]
            public ushort Value2 { get; set; } = 100;
            [ModelProperty("Value3"), JsonProperty("Value3")]
            public uint Value3 { get; set; } = 100;
            [ModelProperty("Value4"), JsonProperty("Value4")]
            public ulong Value4 { get; set; } = 100;
            [ModelProperty("Array1"), JsonProperty("Array1")]
            public byte[] Array1 { get; set; } = new byte[] { 100 };
            [ModelProperty("Array2"), JsonProperty("Array2")]
            public ushort[] Array2 { get; set; } = new ushort[] { 100 };
            [ModelProperty("Array3"), JsonProperty("Array3")]
            public uint[] Array3 { get; set; } = new uint[] { 100 };
            [ModelProperty("Array4"), JsonProperty("Array4")]
            public ulong[] Array4 { get; set; } = new ulong[] { 100 };
        }
        public class OtherData
        {
            [ModelProperty("Value1"), JsonProperty("Value1")]
            public string Value1 { get; set; } = "100";
            [ModelProperty("Value2"), JsonProperty("Value2")]
            public bool Value2 { get; set; } = true;
        }
        [ModelProperty("Data1"), JsonProperty("Data1")]
        public SignedData Data1 { get; set; } = new SignedData();
        [ModelProperty("Data2"), JsonProperty("Data2")]
        public UnsignedData Data2 { get; set; } = new UnsignedData();
        [ModelProperty("Data3"), JsonProperty("Data3")]
        public OtherData Data3 { get; set; } = new OtherData();
        [ModelProperty("Data4"), JsonProperty("Data4")]
        public Dictionary<string, string> Data4 { get; set; } = new Dictionary<string, string>
        {
            {"1", "1"},
            {"2", "2"},
            {"3", "3"}
        };
    }

    [Config(typeof(Config))]
    [MemoryDiagnoser]
    [CategoriesColumn]
    public abstract class Benchmarks
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                // Add(Job.MediumRun.WithId("X86").With(Runtime.Core).With(Platform.X86).With(Jit.RyuJit));
                Add(Job.LongRun.WithId("X64").With(Runtime.Core).With(Platform.X64).With(Jit.RyuJit));
            }
        }
        private Newtonsoft.Json.JsonSerializer _jsonNet;
        private Voltaic.Serialization.Json.JsonSerializer _voltaic;
        private TestData _testData;
        private string _testString;

        [GlobalSetup]
        public void Setup()
        {
            _jsonNet = new Newtonsoft.Json.JsonSerializer();
            _voltaic = new Voltaic.Serialization.Json.JsonSerializer();
            _testData = new TestData();
            _testString = _voltaic.WriteUtf16String(new TestData());
        }

        [Benchmark(Description = "Json.Net"), BenchmarkCategory("Serialize")]
        public string SerializeJsonNet()
        {
            var builder = new StringBuilder(256);
            using (StringWriter stringWriter = new StringWriter(builder, CultureInfo.InvariantCulture))
            using (JsonTextWriter writer = new JsonTextWriter(stringWriter))
                _jsonNet.Serialize(writer, _testData);
            return builder.ToString();
        }

        [Benchmark(Description = "Voltaic"), BenchmarkCategory("Serialize")]
        public string SerializeVoltaic()
        {
            return _voltaic.WriteUtf16String(_testData);
        }

        [Benchmark(Description = "Json.Net"), BenchmarkCategory("Deserialize")]
        public TestData DeserializeJsonNet()
        {
            using (StringReader stringReader = new StringReader(_testString))
            using (JsonTextReader reader = new JsonTextReader(stringReader))
                return _jsonNet.Deserialize(reader, typeof(TestData)) as TestData;
        }

        [Benchmark(Description = "Voltaic"), BenchmarkCategory("Deserialize")]
        public TestData DeserializeVoltaic()
        {
            return _voltaic.ReadUtf16<TestData>(_testString.AsSpan());
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<Benchmarks>();
        }
    }
}
