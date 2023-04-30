# Fast Range Reducer Cache

Naive searching for a minimum in an array of numbers requires going through the array each time, for every searched range. Which is not great if you need it multiple times for different sub-ranges.
Much better to build a tree of sub-array max/min so that later to find a max/min of each sub-range we will only need to traverse the tree, which is much faster (O(N) vs. O(log N)).
Also: you can pass any reducer, not necessarily only min/max finder. So you can for example pass as a parameter a reducer that will let you pre-cache sums for all nodes in the tree for super-fast calculation of any range sub-sum.


```c#
/*
BenchmarkDotNet=v0.13.2, OS=Windows 11 (10.0.22621.1555)
Intel Core i7-8750H CPU 2.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET SDK=7.0.203
  [Host]     : .NET 7.0.5 (7.0.523.17405), X64 RyuJIT AVX2
  DefaultJob : .NET 7.0.5 (7.0.523.17405), X64 RyuJIT AVX2


|       Method |       Mean |    Error |    StdDev |     Median |    Gen0 |    Gen1 |    Gen2 | Allocated |
|------------- |-----------:|---------:|----------:|-----------:|--------:|--------:|--------:|----------:|
| WithPreCache |   716.0 us | 14.32 us |  18.61 us |   705.4 us | 57.6172 | 49.8047 | 49.8047 | 391.56 KB |
|        Naive | 3,310.3 us | 64.07 us | 180.70 us | 3,221.2 us | 15.6250 |       - |       - |  78.45 KB |
*/

// Simple usage:

            var precached = new FastRangeReducer(arr, FastRangeReducer.Max);
            var max = precached.GetResultForRange(startIdx, endIdx);
```
