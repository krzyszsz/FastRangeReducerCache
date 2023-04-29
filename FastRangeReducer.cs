﻿public sealed class FastRangeReducer
{
    ValueTuple<int, double>[][] _preCachedMinMax;
    Func<ValueTuple<int, double>, ValueTuple<int, double>, ValueTuple<int, double>> _reducer;

    public FastRangeReducer(Span<double> rangeToInspect, Func<ValueTuple<int, double>, ValueTuple<int, double>, ValueTuple<int, double>> reducer)
    {
        _reducer = reducer;
        _preCachedMinMax = PreCacheMaxMin(rangeToInspect);
    }

    public static ValueTuple<int, double> Min(ValueTuple<int, double> a, ValueTuple<int, double> b)
    {
        return a.Item2 < b.Item2 ? a : b;
    }

    public static ValueTuple<int, double> Max(ValueTuple<int, double> a, ValueTuple<int, double> b)
    {
        return a.Item2 > b.Item2 ? a : b;
    }

    private ValueTuple<int, double>[][] PreCacheMaxMin(Span<double> rangeToInspect)
    {
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
                var startIndex = i * 2;
                var best = new ValueTuple<int, double>(result[level - 1][startIndex].Item1, result[level - 1][startIndex].Item2);
                var loopEnd = Math.Min((i + 1) * 2, result[level - 1].Length);
                for (var j = startIndex+1; j < loopEnd; j++)
                {
                    best = _reducer(result[level - 1][j], best);
                }
                result[level][i] = best;
            }
        }

        return result;
    }

    public ValueTuple<int, double> GetResultForRange(int start, int end)
    {
        var position = start;
        var level = 0;
        var best = _preCachedMinMax[0][start];
        var dividerOnThisLevel = 1;
        var nextLevelDistance = dividerOnThisLevel * 2;
        var distanceToNextRoundNumber = (nextLevelDistance - position % nextLevelDistance) % nextLevelDistance;
        var currentLevel = _preCachedMinMax[level];

        do
        {
            if (distanceToNextRoundNumber == 0 && position + nextLevelDistance <= end)
            {
                level++;
                dividerOnThisLevel *= 2;
                nextLevelDistance *= 2;
                distanceToNextRoundNumber = (nextLevelDistance - position % nextLevelDistance) % nextLevelDistance;
                currentLevel = _preCachedMinMax[level];
                continue;
            }
            var positionPlusDividerOnThisLevel = position + dividerOnThisLevel;
            if (positionPlusDividerOnThisLevel > end && level > 0)
            {
                level--;
                dividerOnThisLevel /= 2;
                nextLevelDistance /= 2;
                distanceToNextRoundNumber = (nextLevelDistance - position % nextLevelDistance) % nextLevelDistance;
                currentLevel = _preCachedMinMax[level];
                continue;
            }
            var addressForThisLevel = position / dividerOnThisLevel;
            var candidate = currentLevel[addressForThisLevel];
            best = _reducer(candidate, best);
            position = positionPlusDividerOnThisLevel;
            distanceToNextRoundNumber -= dividerOnThisLevel;
        } while (position <= end);
        return best;
    }
}