public sealed class FastRangeReducer
{
    readonly (int, double)[][] _preCachedAsTree;
    readonly Func<(int, double), (int, double), (int, double)> _reducer;

    public FastRangeReducer(Span<double> rangeToInspect, Func<(int, double), (int, double), (int, double)> reducer)
    {
        _reducer = reducer;
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

    private (int, double)[][] PreCacheAsTree(Span<double> rangeToInspect)
    {
        var lengthTotal = rangeToInspect.Length;
        var treeLevels = (int)Math.Ceiling(1 + Math.Log2(lengthTotal));
        var result = new (int, double)[treeLevels][];

        if (result[0] == null) result[0] = new (int, double)[lengthTotal];
        for (var i = 0; i < lengthTotal; i++)
        {
            result[0][i] = (i, rangeToInspect[i]);
        }

        for (var level = 1; level < treeLevels; level++)
        {
            var divider = level == 0 ? 1 : 2 << (level - 1);
            var lengthAtThisLevel = lengthTotal / divider + Math.Sign(lengthTotal % divider);

            if (result[level] == null) result[level] = new (int, double)[lengthAtThisLevel];
            var loopEndUpperBound = level == 1 ? lengthTotal : result[level - 1].Length;
            for (var i = 0; i < lengthAtThisLevel; i++)
            {
                var startIndex = i * 2;
                var best = level == 1
                    ? (startIndex, rangeToInspect[startIndex])
                    : (result[level - 1][startIndex].Item1, result[level - 1][startIndex].Item2);
                var loopEnd = Math.Min((i + 1) * 2, loopEndUpperBound);
                if (level == 1)
                {
                    for (var j = startIndex + 1; j < loopEnd; j++)
                    {
                        best = _reducer((j, rangeToInspect[j]), best);
                    }
                }
                else
                {
                    for (var j = startIndex + 1; j < loopEnd; j++)
                    {
                        best = _reducer(result[level - 1][j], best);
                    }
                }
                result[level][i] = best;
            }
        }

        return result;
    }

    public (int, double) GetResultForRange(int start, int end)
    {
        var position = start;
        var level = 0;
        var best = _preCachedAsTree[0][start];
        var dividerOnThisLevel = 1;
        var nextLevelDistance = dividerOnThisLevel * 2;
        var distanceToNextRoundNumber = (nextLevelDistance - position % nextLevelDistance) % nextLevelDistance;
        var currentLevel = _preCachedAsTree[level];

        do
        {
            if (distanceToNextRoundNumber == 0 && position + nextLevelDistance <= end)
            {
                level++;
                dividerOnThisLevel *= 2;
                nextLevelDistance *= 2;
                distanceToNextRoundNumber = (nextLevelDistance - position % nextLevelDistance) % nextLevelDistance;
                currentLevel = _preCachedAsTree[level];
                continue;
            }
            var positionPlusDividerOnThisLevel = position + dividerOnThisLevel;
            if (positionPlusDividerOnThisLevel > end && level > 0)
            {
                level--;
                dividerOnThisLevel /= 2;
                nextLevelDistance /= 2;
                distanceToNextRoundNumber = (nextLevelDistance - position % nextLevelDistance) % nextLevelDistance;
                currentLevel = _preCachedAsTree[level];
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