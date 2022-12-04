using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

/*
BenchmarkDotNet=v0.13.2, OS=Windows 11 (10.0.22621.900)
AMD Ryzen Threadripper PRO 5995WX 64-Cores, 1 CPU, 128 logical and 64 physical cores
.NET SDK=7.0.100
  [Host]     : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2
  DefaultJob : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2


|       Method |       Mean |    Error |   StdDev |    Gen0 |    Gen1 |    Gen2 | Allocated |
|------------- |-----------:|---------:|---------:|--------:|--------:|--------:|----------:|
| WithPreCache |   370.7 us |  1.31 us |  1.22 us | 49.8047 | 49.8047 | 49.8047 | 391.53 KB |
|        Naive | 7,558.8 us | 68.12 us | 63.72 us |       - |       - |       - |  78.45 KB |
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
        var precached = PreCacheMaxMin(arr, true);
        for (var q = 0; q < 1000; q++)  // Pre-caching only makes sense when you need the min/max multiple times (otherwise it's slower)
        {
            var max = GetMinMax(true, startIdx, endIdx, precached);
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

    static ValueTuple<int, double>[][] PreCacheMaxMin(Span<double> rangeToInspect, bool isMax)
    {
        // const int nodeChildren = 2; // If changed, change base of log!
        var lengthTotal = rangeToInspect.Length;
        var treeLevels = (int)Math.Ceiling(1 + Math.Log2(lengthTotal));
        var result = new ValueTuple<int, double>[treeLevels][];

        if (result[0] == null) result[0] = new ValueTuple<int, double>[lengthTotal];
        for (var i = 0; i < lengthTotal; i++)
        {
            result[0][i] = new ValueTuple<int, double>(i, rangeToInspect[i]);
        }

        for (var level = 1; level < treeLevels; level++)
        {
            var lengthAtThisLevel = (int)Math.Ceiling(lengthTotal / Math.Pow(2.0, level));
            if (result[level] == null) result[level] = new ValueTuple<int, double>[lengthAtThisLevel];
            for (var i = 0; i < lengthAtThisLevel; i++)
            {
                if (isMax)
                {
                    var best = new ValueTuple<int, double>(-1, double.MinValue);
                    var loopEnd = Math.Min((i + 1) * 2, result[level - 1].Length);
                    for (var j = i * 2; j < loopEnd; j++)
                    {
                        if (result[level - 1][j].Item2 > best.Item2)
                        {
                            best = result[level - 1][j];
                        }
                    }
                    result[level][i] = best;
                }
                else
                {
                    var best = new ValueTuple<int, double>(-1, double.MaxValue);
                    var loopEnd = Math.Min((i + 1) * 2, result[level - 1].Length);
                    for (var j = i * 2; j < loopEnd; j++)
                    {
                        if (result[level - 1][j].Item2 < best.Item2)
                        {
                            best = result[level - 1][j];
                        }
                    }
                    result[level][i] = best;
                }
            }
        }

        return result;
    }

    static ValueTuple<int, double> GetMinMax(bool isMax, int start, int end, ValueTuple<int, double>[][] preCachedMinMax)
    {
        var position = start;
        var level = 0;
        var best = preCachedMinMax[0][start];
        var dividerOnThisLevel = 1;
        var nextLevelDistance = dividerOnThisLevel * 2;
        var distanceToNextRoundNumber = (nextLevelDistance - position % nextLevelDistance) % nextLevelDistance;
        var currentLevel = preCachedMinMax[level];

        if (isMax)
        {
            do
            {
                if (distanceToNextRoundNumber == 0 && position + nextLevelDistance <= end)
                {
                    level++;
                    dividerOnThisLevel *= 2;
                    nextLevelDistance *= 2;
                    distanceToNextRoundNumber = (nextLevelDistance - position % nextLevelDistance) % nextLevelDistance;
                    currentLevel = preCachedMinMax[level];
                    continue;
                }
                var positionPlusDividerOnThisLevel = position + dividerOnThisLevel;
                if (positionPlusDividerOnThisLevel > end && level > 0)
                {
                    level--;
                    dividerOnThisLevel /= 2;
                    nextLevelDistance /= 2;
                    distanceToNextRoundNumber = (nextLevelDistance - position % nextLevelDistance) % nextLevelDistance;
                    currentLevel = preCachedMinMax[level];
                    continue;
                }
                var addressForThisLevel = position / dividerOnThisLevel;
                var candidate = currentLevel[addressForThisLevel];
                if (candidate.Item2 > best.Item2)
                {
                    best = candidate;
                }
                position = positionPlusDividerOnThisLevel;
                distanceToNextRoundNumber -= dividerOnThisLevel;
            } while (position <= end);
            return best;
        }

        do
        {
            if (distanceToNextRoundNumber == 0 && position + nextLevelDistance <= end)
            {
                level++;
                dividerOnThisLevel *= 2;
                nextLevelDistance *= 2;
                distanceToNextRoundNumber = (nextLevelDistance - position % nextLevelDistance) % nextLevelDistance;
                currentLevel = preCachedMinMax[level];
                continue;
            }
            var positionPlusDividerOnThisLevel = position + dividerOnThisLevel;
            if (positionPlusDividerOnThisLevel > end && level > 0)
            {
                level--;
                dividerOnThisLevel /= 2;
                nextLevelDistance /= 2;
                distanceToNextRoundNumber = (nextLevelDistance - position % nextLevelDistance) % nextLevelDistance;
                currentLevel = preCachedMinMax[level];
                continue;
            }

            var addressForThisLevel = position / dividerOnThisLevel;
            var candidate = currentLevel[addressForThisLevel];
            if (candidate.Item2 < best.Item2)
            {
                best = candidate;
            }
            position = positionPlusDividerOnThisLevel;
            distanceToNextRoundNumber -= dividerOnThisLevel;
        } while (position <= end);
        return best;
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

            var precached = PreCacheMaxMin(arr, true);
            var max = GetMinMax(true, startIdx, endIdx, precached);
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

            var precached = PreCacheMaxMin(arr, false);
            var min = GetMinMax(false, startIdx, endIdx, precached);
            if (min.Item1 != best.Item1) throw new Exception();
            if (min.Item2 != best.Item2) throw new Exception();
        }
    }
}