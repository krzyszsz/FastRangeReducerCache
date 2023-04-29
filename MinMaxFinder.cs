public sealed class MinMaxFinder
{
    ValueTuple<int, double>[][] _preCachedMinMax;

    public MinMaxFinder(double[] array, bool isMax)
    {
        _preCachedMinMax = PreCacheMaxMin(array, isMax);
    }

    public static ValueTuple<int, double> Min(ValueTuple<int, double> a, ValueTuple<int, double> b)
    {
        return a.Item2 < b.Item2 ? a : b;
    }

    public static ValueTuple<int, double> Max(ValueTuple<int, double> a, ValueTuple<int, double> b)
    {
        return a.Item2 > b.Item2 ? a : b;
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


    public ValueTuple<int, double> GetMinMax(bool isMax, int start, int end)
    {
        var position = start;
        var level = 0;
        var best = _preCachedMinMax[0][start];
        var dividerOnThisLevel = 1;
        var nextLevelDistance = dividerOnThisLevel * 2;
        var distanceToNextRoundNumber = (nextLevelDistance - position % nextLevelDistance) % nextLevelDistance;
        var currentLevel = _preCachedMinMax[level];

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
            if (candidate.Item2 < best.Item2)
            {
                best = candidate;
            }
            position = positionPlusDividerOnThisLevel;
            distanceToNextRoundNumber -= dividerOnThisLevel;
        } while (position <= end);
        return best;
    }
}