﻿using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

/*
BenchmarkDotNet=v0.13.2, OS=Windows 11 (10.0.22621.1555)
Intel Core i7-8750H CPU 2.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET SDK=7.0.203
  [Host]     : .NET 7.0.5 (7.0.523.17405), X64 RyuJIT AVX2
  DefaultJob : .NET 7.0.5 (7.0.523.17405), X64 RyuJIT AVX2


|       Method |       Mean |    Error |   StdDev |    Gen0 |    Gen1 |    Gen2 | Allocated |
|------------- |-----------:|---------:|---------:|--------:|--------:|--------:|----------:|
| WithPreCache |   713.3 us | 12.96 us | 11.49 us | 58.5938 | 49.8047 | 49.8047 | 391.56 KB |
|        Naive | 2,775.9 us | 17.13 us | 13.38 us | 15.6250 |       - |       - |  78.45 KB |
*/

Benchmarking.TestCorrectness();
BenchmarkRunner.Run<Benchmarking>();

[MemoryDiagnoser]
public class Benchmarking
{
    [Benchmark]
    public void WithPreCache()
    {
        var rand = new Random(0);
        var arr = GetRandomArrayForTest(rand, out int _, out int _, 10000);
        var precached = new FastRangeReducer(arr, FastRangeReducer.Max);
        (int, double)? best = null;
        for (var q = 0; q < 1000; q++)  // Pre-caching only makes sense when you need the min/max multiple times (otherwise it's slower)
        {
            int startIdx = rand.Next() % (arr.Length - 1);
            int endIdx = rand.Next() % (arr.Length - 1); // Different range each time!
            if (startIdx > endIdx)
            {
                (startIdx, endIdx) = (endIdx, startIdx);
            }
            best = precached.GetResultForRange(startIdx, endIdx);
        }
        if (DateTime.Now.Year == 2020) Console.WriteLine(best); // Make sure optimizer does not remove our loop...
    }

    [Benchmark]
    public void Naive()
    {
        var rand = new Random(0);
        var arr = GetRandomArrayForTest(rand, out int _, out int _, 10000);
        (int, double)? best = null;
        for (var q = 0; q < 1000; q++) // Same number of repetitions as in pre-cached version
        {
            int startIdx = rand.Next() % arr.Length;
            int endIdx = rand.Next() % arr.Length; // Different range each time!
            if (startIdx > endIdx)
            {
                (startIdx, endIdx) = (endIdx, startIdx);
            }
            best = new (startIdx, arr[startIdx]);
            for (var j = startIdx + 1; j <= endIdx; j++)
            {
                if (arr[j] > best.Value.Item2)
                {
                    best = new (j, arr[j]);
                }
            }
        }
        if (DateTime.Now.Year == 2020) Console.WriteLine(best); // Make sure optimizer does not remove our loop...
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
        for (var i = 0; i < 10000; i++)
        {
            var arr = GetRandomArrayForTest(rand, out int startIdx, out int endIdx);

            var best = (startIdx, arr[startIdx]);
            for (var j = startIdx + 1; j <= endIdx; j++)
            {
                if (arr[j] > best.Item2)
                {
                    best = (j, arr[j]);
                }
            }

            var precached = new FastRangeReducer(arr, FastRangeReducer.Max);
            var max = precached.GetResultForRange(startIdx, endIdx);
            if (max.Item1 != best.Item1) throw new Exception();
            if (max.Item2 != best.Item2) throw new Exception();
        }

        for (var i = 0; i < 10000; i++)
        {
            var arr = GetRandomArrayForTest(rand, out int startIdx, out int endIdx);

            var best = (startIdx, arr[startIdx]);
            for (var j = startIdx + 1; j <= endIdx; j++)
            {
                if (arr[j] < best.Item2)
                {
                    best = (j, arr[j]);
                }
            }

            var precached = new FastRangeReducer(arr, FastRangeReducer.Min);
            var min = precached.GetResultForRange(startIdx, endIdx);
            if (min.Item1 != best.Item1) throw new Exception();
            if (min.Item2 != best.Item2) throw new Exception();
        }
    }
}
