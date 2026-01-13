// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using Apache.Arrow;
using Polaire.DataTypes;

namespace Polaire.Compute;

/// <summary>
/// Window functions for Series (rank, row_number, etc.).
/// These operate over ordered data to compute rankings and positions.
/// </summary>
public static class WindowOperations
{
    // ============================================================================
    // Row Number
    // ============================================================================

    /// <summary>
    /// Returns the row number (1-indexed) for each element.
    /// </summary>
    public static Series RowNumber(Series series)
    {
        var builder = new Int64Array.Builder();
        for (int i = 0; i < series.Length; i++)
        {
            builder.Append(i + 1);
        }
        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Int64);
    }

    // ============================================================================
    // Rank Functions
    // ============================================================================

    /// <summary>
    /// Returns the rank (1-indexed) for each element using the "average" tie-breaking method.
    /// Equal values receive the mean of their ordinal ranks.
    /// </summary>
    public static Series Rank(Series series, string method = "average", bool descending = false)
    {
        if (series.Length == 0)
            return Series.FromArrowArray(series.Name, new DoubleArray.Builder().Build(), DataType.Float64);

        // Get sorted indices and values
        var indexedValues = new List<(int index, double value, bool isNull)>();
        for (int i = 0; i < series.Length; i++)
        {
            if (series.IsNull(i))
            {
                indexedValues.Add((i, double.NaN, true));
            }
            else
            {
                var val = series[i];
                if (val.TryGetDouble(out var d))
                    indexedValues.Add((i, d, false));
                else if (val.TryGetInt64(out var l))
                    indexedValues.Add((i, l, false));
                else
                    indexedValues.Add((i, double.NaN, true));
            }
        }

        // Sort by value (nulls go to end)
        indexedValues = descending
            ? indexedValues.OrderBy(x => x.isNull).ThenByDescending(x => x.value).ToList()
            : indexedValues.OrderBy(x => x.isNull).ThenBy(x => x.value).ToList();

        // Assign ranks based on method
        var ranks = new double[series.Length];
        int rank = 1;

        for (int i = 0; i < indexedValues.Count; i++)
        {
            var item = indexedValues[i];
            if (item.isNull)
            {
                ranks[item.index] = double.NaN;
                continue;
            }

            // Find all items with the same value (ties)
            int tieStart = i;
            while (i < indexedValues.Count - 1 &&
                   !indexedValues[i + 1].isNull &&
                   Math.Abs(indexedValues[i + 1].value - item.value) < 1e-15)
            {
                i++;
            }
            int tieEnd = i;
            int tieCount = tieEnd - tieStart + 1;

            // Assign ranks based on method
            switch (method.ToLowerInvariant())
            {
                case "average":
                    double avgRank = (2.0 * rank + tieCount - 1) / 2.0;
                    for (int j = tieStart; j <= tieEnd; j++)
                        ranks[indexedValues[j].index] = avgRank;
                    break;

                case "min":
                    for (int j = tieStart; j <= tieEnd; j++)
                        ranks[indexedValues[j].index] = rank;
                    break;

                case "max":
                    for (int j = tieStart; j <= tieEnd; j++)
                        ranks[indexedValues[j].index] = rank + tieCount - 1;
                    break;

                case "first":
                    for (int j = tieStart; j <= tieEnd; j++)
                        ranks[indexedValues[j].index] = rank + (j - tieStart);
                    break;

                case "dense":
                    for (int j = tieStart; j <= tieEnd; j++)
                        ranks[indexedValues[j].index] = rank;
                    rank -= (tieCount - 1); // Only increment by 1 for dense ranking
                    break;

                default:
                    throw new ArgumentException($"Unknown ranking method: {method}");
            }

            rank += tieCount;
        }

        // Build result
        var builder = new DoubleArray.Builder();
        for (int i = 0; i < series.Length; i++)
        {
            if (double.IsNaN(ranks[i]))
                builder.AppendNull();
            else
                builder.Append(ranks[i]);
        }

        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Float64);
    }

    /// <summary>
    /// Returns the dense rank for each element.
    /// Equal values receive the same rank, with no gaps in rank values.
    /// </summary>
    public static Series DenseRank(Series series, bool descending = false)
    {
        return Rank(series, "dense", descending);
    }

    /// <summary>
    /// Returns the ordinal rank for each element.
    /// Each element receives a unique rank based on position in the sorted order.
    /// </summary>
    public static Series OrdinalRank(Series series, bool descending = false)
    {
        return Rank(series, "first", descending);
    }

    // ============================================================================
    // Percent Rank
    // ============================================================================

    /// <summary>
    /// Returns the percentile rank (0 to 1) for each element.
    /// </summary>
    public static Series PercentRank(Series series)
    {
        if (series.Length == 0)
            return Series.FromArrowArray(series.Name, new DoubleArray.Builder().Build(), DataType.Float64);

        var ranks = Rank(series, "average");
        int validCount = series.Length - series.NullCount;

        var builder = new DoubleArray.Builder();
        for (int i = 0; i < series.Length; i++)
        {
            if (series.IsNull(i))
            {
                builder.AppendNull();
            }
            else
            {
                double rank = ranks[i].AsFloat64();
                // Percent rank formula: (rank - 1) / (n - 1)
                double pctRank = validCount > 1 ? (rank - 1) / (validCount - 1) : 0;
                builder.Append(pctRank);
            }
        }

        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Float64);
    }

    // ============================================================================
    // Lead/Lag
    // ============================================================================

    /// <summary>
    /// Returns values shifted backward by n positions (access future values).
    /// </summary>
    public static Series Lead(Series series, int n = 1, AnyValue? defaultValue = null)
    {
        return ShiftInternal(series, -n, defaultValue);
    }

    /// <summary>
    /// Returns values shifted forward by n positions (access past values).
    /// </summary>
    public static Series Lag(Series series, int n = 1, AnyValue? defaultValue = null)
    {
        return ShiftInternal(series, n, defaultValue);
    }

    private static Series ShiftInternal(Series series, int n, AnyValue? defaultValue)
    {
        var builder = new DoubleArray.Builder();

        for (int i = 0; i < series.Length; i++)
        {
            int sourceIndex = i - n;
            if (sourceIndex < 0 || sourceIndex >= series.Length)
            {
                if (defaultValue.HasValue && !defaultValue.Value.IsNull)
                {
                    if (defaultValue.Value.TryGetDouble(out var d))
                        builder.Append(d);
                    else if (defaultValue.Value.TryGetInt64(out var l))
                        builder.Append(l);
                    else
                        builder.AppendNull();
                }
                else
                {
                    builder.AppendNull();
                }
            }
            else if (series.IsNull(sourceIndex))
            {
                builder.AppendNull();
            }
            else
            {
                var val = series[sourceIndex];
                if (val.TryGetDouble(out var d))
                    builder.Append(d);
                else if (val.TryGetInt64(out var l))
                    builder.Append(l);
                else
                    builder.AppendNull();
            }
        }

        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Float64);
    }

    // ============================================================================
    // First/Last Value
    // ============================================================================

    /// <summary>
    /// Returns the first non-null value in the series, repeated for all rows.
    /// </summary>
    public static Series FirstValue(Series series)
    {
        AnyValue firstVal = AnyValue.Null;
        for (int i = 0; i < series.Length; i++)
        {
            if (!series.IsNull(i))
            {
                firstVal = series[i];
                break;
            }
        }

        var builder = new DoubleArray.Builder();
        for (int i = 0; i < series.Length; i++)
        {
            if (firstVal.IsNull)
            {
                builder.AppendNull();
            }
            else if (firstVal.TryGetDouble(out var d))
            {
                builder.Append(d);
            }
            else if (firstVal.TryGetInt64(out var l))
            {
                builder.Append(l);
            }
            else
            {
                builder.AppendNull();
            }
        }

        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Float64);
    }

    /// <summary>
    /// Returns the last non-null value in the series, repeated for all rows.
    /// </summary>
    public static Series LastValue(Series series)
    {
        AnyValue lastVal = AnyValue.Null;
        for (int i = series.Length - 1; i >= 0; i--)
        {
            if (!series.IsNull(i))
            {
                lastVal = series[i];
                break;
            }
        }

        var builder = new DoubleArray.Builder();
        for (int i = 0; i < series.Length; i++)
        {
            if (lastVal.IsNull)
            {
                builder.AppendNull();
            }
            else if (lastVal.TryGetDouble(out var d))
            {
                builder.Append(d);
            }
            else if (lastVal.TryGetInt64(out var l))
            {
                builder.Append(l);
            }
            else
            {
                builder.AppendNull();
            }
        }

        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Float64);
    }

    // ============================================================================
    // Nth Value
    // ============================================================================

    /// <summary>
    /// Returns the nth non-null value in the series, repeated for all rows.
    /// </summary>
    public static Series NthValue(Series series, int n)
    {
        if (n < 1)
            throw new ArgumentOutOfRangeException(nameof(n), "n must be >= 1");

        AnyValue nthVal = AnyValue.Null;
        int count = 0;
        for (int i = 0; i < series.Length; i++)
        {
            if (!series.IsNull(i))
            {
                count++;
                if (count == n)
                {
                    nthVal = series[i];
                    break;
                }
            }
        }

        var builder = new DoubleArray.Builder();
        for (int i = 0; i < series.Length; i++)
        {
            if (nthVal.IsNull)
            {
                builder.AppendNull();
            }
            else if (nthVal.TryGetDouble(out var d))
            {
                builder.Append(d);
            }
            else if (nthVal.TryGetInt64(out var l))
            {
                builder.Append(l);
            }
            else
            {
                builder.AppendNull();
            }
        }

        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Float64);
    }

    // ============================================================================
    // Cumulative Count
    // ============================================================================

    /// <summary>
    /// Returns the cumulative count of non-null values up to each position.
    /// </summary>
    public static Series CumCount(Series series)
    {
        var builder = new Int64Array.Builder();
        long count = 0;

        for (int i = 0; i < series.Length; i++)
        {
            if (!series.IsNull(i))
                count++;
            builder.Append(count);
        }

        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Int64);
    }

    // ============================================================================
    // Fill Null Operations
    // ============================================================================

    /// <summary>
    /// Fills null values with the previous non-null value (forward fill).
    /// </summary>
    public static Series FillForward(Series series)
    {
        var builder = new DoubleArray.Builder();
        double? lastValid = null;

        for (int i = 0; i < series.Length; i++)
        {
            if (series.IsNull(i))
            {
                if (lastValid.HasValue)
                    builder.Append(lastValid.Value);
                else
                    builder.AppendNull();
            }
            else
            {
                var val = series[i];
                if (val.TryGetDouble(out var d))
                {
                    lastValid = d;
                    builder.Append(d);
                }
                else if (val.TryGetInt64(out var l))
                {
                    lastValid = l;
                    builder.Append(l);
                }
                else
                {
                    builder.AppendNull();
                }
            }
        }

        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Float64);
    }

    /// <summary>
    /// Fills null values with the next non-null value (backward fill).
    /// </summary>
    public static Series FillBackward(Series series)
    {
        // First pass: collect all values
        var values = new double?[series.Length];
        for (int i = 0; i < series.Length; i++)
        {
            if (series.IsNull(i))
            {
                values[i] = null;
            }
            else
            {
                var val = series[i];
                if (val.TryGetDouble(out var d))
                    values[i] = d;
                else if (val.TryGetInt64(out var l))
                    values[i] = l;
                else
                    values[i] = null;
            }
        }

        // Second pass: backward fill
        double? nextValid = null;
        for (int i = series.Length - 1; i >= 0; i--)
        {
            if (!values[i].HasValue)
            {
                values[i] = nextValid;
            }
            else
            {
                nextValid = values[i];
            }
        }

        // Build result
        var builder = new DoubleArray.Builder();
        for (int i = 0; i < series.Length; i++)
        {
            if (values[i].HasValue)
                builder.Append(values[i]!.Value);
            else
                builder.AppendNull();
        }

        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Float64);
    }

    /// <summary>
    /// Fills null values by interpolating linearly between non-null values.
    /// </summary>
    public static Series FillInterpolate(Series series)
    {
        if (series.Length == 0)
            return Series.FromArrowArray(series.Name, new DoubleArray.Builder().Build(), DataType.Float64);

        // First pass: collect all values
        var values = new double?[series.Length];
        for (int i = 0; i < series.Length; i++)
        {
            if (series.IsNull(i))
            {
                values[i] = null;
            }
            else
            {
                var val = series[i];
                if (val.TryGetDouble(out var d))
                    values[i] = d;
                else if (val.TryGetInt64(out var l))
                    values[i] = l;
                else
                    values[i] = null;
            }
        }

        // Second pass: interpolate
        int? lastValidIdx = null;
        for (int i = 0; i < series.Length; i++)
        {
            if (values[i].HasValue)
            {
                // Fill gap if exists
                if (lastValidIdx.HasValue && i - lastValidIdx.Value > 1)
                {
                    double startVal = values[lastValidIdx.Value]!.Value;
                    double endVal = values[i]!.Value;
                    int gapSize = i - lastValidIdx.Value;

                    for (int j = lastValidIdx.Value + 1; j < i; j++)
                    {
                        double t = (double)(j - lastValidIdx.Value) / gapSize;
                        values[j] = startVal + t * (endVal - startVal);
                    }
                }
                lastValidIdx = i;
            }
        }

        // Build result
        var builder = new DoubleArray.Builder();
        for (int i = 0; i < series.Length; i++)
        {
            if (values[i].HasValue)
                builder.Append(values[i]!.Value);
            else
                builder.AppendNull();
        }

        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Float64);
    }
}
