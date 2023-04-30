public sealed class FastRangeReducer
{
    readonly (int, double)[] _preCachedAsTree;
    readonly Func<(int, double), (int, double), (int, double)> _reducer;
    readonly int _arraySize;
    readonly int _maxLevel;

    public FastRangeReducer(Span<double> rangeToInspect, Func<(int, double), (int, double), (int, double)> reducer)
    {
        _reducer = reducer;
        _arraySize = rangeToInspect.Length;
        _maxLevel = (int)Math.Ceiling(1 + Math.Log2(_arraySize)) - 1;
        _preCachedAsTree = PreCacheAsTree(rangeToInspect);
    }

    public static (int, double) Min((int, double) newItem, (int, double) accumulator)
    {
        return newItem.Item2 < accumulator.Item2 ? newItem : accumulator;
    }

    public static (int, double) Max((int, double) newItem, (int, double) accumulator)
    {
        return newItem.Item2 > accumulator.Item2 ? newItem : accumulator;
    }

    public (int, double) GetResultForRange(int start, int end)
    {
        var position = start;
        var level = 0;
        var maxLevelLocal = _maxLevel;
        var best = _preCachedAsTree[GetFlatIndex(0, start, maxLevelLocal)];
        var dividerOnThisLevel = 1;
        var nextLevelDistance = dividerOnThisLevel * 2;
        var distanceToNextRoundNumber = (nextLevelDistance - position % nextLevelDistance) % nextLevelDistance;
        var currentLevel = level;

        do
        {
            if (distanceToNextRoundNumber == 0 && position + nextLevelDistance <= end)
            {
                level++;
                dividerOnThisLevel *= 2;
                nextLevelDistance *= 2;
                distanceToNextRoundNumber = (nextLevelDistance - position % nextLevelDistance) % nextLevelDistance;
                currentLevel = level;
                continue;
            }
            var positionPlusDividerOnThisLevel = position + dividerOnThisLevel;
            if (positionPlusDividerOnThisLevel > end && level > 0)
            {
                level--;
                dividerOnThisLevel /= 2;
                nextLevelDistance /= 2;
                distanceToNextRoundNumber = (nextLevelDistance - position % nextLevelDistance) % nextLevelDistance;
                currentLevel = level;
                continue;
            }
            var addressForThisLevel = position / dividerOnThisLevel;
            var candidate = _preCachedAsTree[GetFlatIndex(currentLevel, addressForThisLevel, maxLevelLocal)];
            best = _reducer(candidate, best);
            position = positionPlusDividerOnThisLevel;
            distanceToNextRoundNumber -= dividerOnThisLevel;
        } while (position <= end);
        return best;
    }

    private static int GetFlatIndex(int level, int nodeIndex, int maxLevel)
    {
        return (2 << (maxLevel - level - 1)) + nodeIndex;
    }

    private (int, double)[] PreCacheAsTree(Span<double> rangeToInspect)
    {
        var maxLevelLocal = _maxLevel;
        var lengthTotal = rangeToInspect.Length;
        var treeLevels = maxLevelLocal + 1;
        var result = new (int, double)[2 << (treeLevels - 1)];

        for (var i = 0; i < lengthTotal; i++)
        {
            result[GetFlatIndex(0, i, maxLevelLocal)] = (i, rangeToInspect[i]);
        }

        for (var level = 1; level < treeLevels; level++)
        {
            var lengthAtThisLevel = GetLevelLength(lengthTotal, level);
            for (var i = 0; i < lengthAtThisLevel; i++)
            {
                var startIndex = i * 2;
                var idx = GetFlatIndex(level - 1, startIndex, maxLevelLocal);
                var best = (result[idx].Item1, result[idx].Item2);
                var loopEnd = Math.Min((i + 1) * 2, GetLevelLength(lengthTotal, level - 1));
                for (var j = startIndex + 1; j < loopEnd; j++)
                {
                    best = _reducer(result[GetFlatIndex(level - 1, j, maxLevelLocal)], best);
                }
                result[GetFlatIndex(level, i, maxLevelLocal)] = best;
            }
        }

        return result;
    }

    private static int[] _powersOf2 = new int[64];

    static FastRangeReducer()
    {
        for (var i=1; i<64; i++)
        {
            _powersOf2[i] = 2 << (i - 1);
        }
        _powersOf2[0] = 1;
    }

    private static int GetLevelLength(int lengthTotal, int level)
    {
        var divider = _powersOf2[level];
        var result = lengthTotal / divider;
        if (lengthTotal % divider != 0) result++;
        return result;
    }

}