using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

/*
BenchmarkDotNet=v0.13.2, OS=Windows 11 (10.0.22621.1555)
Intel Core i7-8750H CPU 2.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET SDK=7.0.202
  [Host]     : .NET 7.0.4 (7.0.423.11508), X64 RyuJIT AVX2
  DefaultJob : .NET 7.0.4 (7.0.423.11508), X64 RyuJIT AVX2


|       Method |       Mean |     Error |    StdDev |    Gen0 |    Gen1 |    Gen2 | Allocated |
|------------- |-----------:|----------:|----------:|--------:|--------:|--------:|----------:|
| WithPreCache |   476.5 us |   3.33 us |   2.78 us | 58.1055 | 49.8047 | 49.8047 | 391.56 KB |
|        Naive | 9,663.5 us | 189.67 us | 202.94 us | 15.6250 |       - |       - |  78.45 KB |
*/

PreCachedMinimumMaximum.TestCorrectness();
BenchmarkRunner.Run<PreCachedMinimumMaximum>();

[MemoryDiagnoser]
public class PreCachedMinimumMaximum
{
    [Benchmark]
    public void WithPreCache()
    {
        var rand = new Random(0);
        var arr = GetRandomArrayForTest(rand, out int _, out int _, 10000);
        int startIdx = 0;
        int endIdx = arr.Length - 1;
        var precached = new MinMaxFinder(arr, true);
        for (var q = 0; q < 1000; q++)  // Pre-caching only makes sense when you need the min/max multiple times (otherwise it's slower)
        {
            var max = precached.GetMinMax(true, startIdx, endIdx);
        }
    }

    [Benchmark]
    public void Naive()
    {
        var rand = new Random(0);
        var arr = GetRandomArrayForTest(rand, out int _, out int _, 10000);
        int startIdx = 0;
        int endIdx = arr.Length - 1;
        for (var q = 0; q < 1000; q++) // Same number of repetitions as in pre-cached version
        {
            var best = new ValueTuple<int, double>(startIdx, arr[startIdx]);
            for (var j = startIdx + 1; j <= endIdx; j++)
            {
                if (arr[j] > best.Item2)
                {
                    best = new ValueTuple<int, double>(j, arr[j]);
                }
            }
        }
    }

    static double[] GetRandomArrayForTest(Random random, out int startIdx, out int endIdx, int? suggestedLen = null)
    {
        var len = suggestedLen ?? random.Next(10000) + 1;
        var arr = new double[len];
        for (var j = 0; j < len; j++)
        {
            arr[j] = random.NextDouble();
        }
        startIdx = random.Next(len);
        endIdx = random.Next(len - startIdx) + startIdx;
        return arr;
    }

    public static void TestCorrectness()
    {
        var rand = new Random(0);
        for (var i = 0; i < 1000000; i++)
        {
            var arr = GetRandomArrayForTest(rand, out int startIdx, out int endIdx);

            var best = new ValueTuple<int, double>(startIdx, arr[startIdx]);
            for (var j = startIdx + 1; j <= endIdx; j++)
            {
                if (arr[j] > best.Item2)
                {
                    best = new ValueTuple<int, double>(j, arr[j]);
                }
            }

            var precached = new MinMaxFinder(arr, true);
            var max = precached.GetMinMax(true, startIdx, endIdx);
            if (max.Item1 != best.Item1) throw new Exception();
            if (max.Item2 != best.Item2) throw new Exception();
        }

        for (var i = 0; i < 1000000; i++)
        {
            var arr = GetRandomArrayForTest(rand, out int startIdx, out int endIdx);

            var best = new ValueTuple<int, double>(startIdx, arr[startIdx]);
            for (var j = startIdx + 1; j <= endIdx; j++)
            {
                if (arr[j] < best.Item2)
                {
                    best = new ValueTuple<int, double>(j, arr[j]);
                }
            }

            var precached = new MinMaxFinder(arr, false);
            var min = precached.GetMinMax(false, startIdx, endIdx);
            if (min.Item1 != best.Item1) throw new Exception();
            if (min.Item2 != best.Item2) throw new Exception();
        }
    }
}
