// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using Polaire.DataTypes;
using Polaire.Core;

namespace Polaire.Compute;

/// <summary>
/// Rolling window operations for Series.
/// </summary>
public static class RollingOperations
{
    // ============================================================================
    // Rolling Sum
    // ============================================================================

    public static Series RollingSum(Series series, int windowSize, int minPeriods = 1, bool center = false)
    {
        ValidateWindowParams(windowSize, minPeriods);

        var builder = new Apache.Arrow.DoubleArray.Builder();
        int n = series.Length;
        int offset = center ? windowSize / 2 : 0;

        for (int i = 0; i < n; i++)
        {
            int windowStart = center ? i - offset : i - windowSize + 1;
            int windowEnd = center ? i + (windowSize - offset) : i + 1;

            // Clamp to valid range
            windowStart = Math.Max(0, windowStart);
            windowEnd = Math.Min(n, windowEnd);

            double sum = 0;
            int validCount = 0;

            for (int j = windowStart; j < windowEnd; j++)
            {
                if (!series.IsNull(j))
                {
                    var val = series[j];
                    if (val.TryGetDouble(out var d))
                    {
                        sum += d;
                        validCount++;
                    }
                    else if (val.TryGetInt64(out var l))
                    {
                        sum += l;
                        validCount++;
                    }
                }
            }

            if (validCount >= minPeriods)
                builder.Append(sum);
            else
                builder.AppendNull();
        }

        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Float64);
    }

    // ============================================================================
    // Rolling Mean
    // ============================================================================

    public static Series RollingMean(Series series, int windowSize, int minPeriods = 1, bool center = false)
    {
        ValidateWindowParams(windowSize, minPeriods);

        var builder = new Apache.Arrow.DoubleArray.Builder();
        int n = series.Length;
        int offset = center ? windowSize / 2 : 0;

        for (int i = 0; i < n; i++)
        {
            int windowStart = center ? i - offset : i - windowSize + 1;
            int windowEnd = center ? i + (windowSize - offset) : i + 1;

            windowStart = Math.Max(0, windowStart);
            windowEnd = Math.Min(n, windowEnd);

            double sum = 0;
            int validCount = 0;

            for (int j = windowStart; j < windowEnd; j++)
            {
                if (!series.IsNull(j))
                {
                    var val = series[j];
                    if (val.TryGetDouble(out var d))
                    {
                        sum += d;
                        validCount++;
                    }
                    else if (val.TryGetInt64(out var l))
                    {
                        sum += l;
                        validCount++;
                    }
                }
            }

            if (validCount >= minPeriods)
                builder.Append(sum / validCount);
            else
                builder.AppendNull();
        }

        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Float64);
    }

    // ============================================================================
    // Rolling Min
    // ============================================================================

    public static Series RollingMin(Series series, int windowSize, int minPeriods = 1, bool center = false)
    {
        ValidateWindowParams(windowSize, minPeriods);

        var builder = new Apache.Arrow.DoubleArray.Builder();
        int n = series.Length;
        int offset = center ? windowSize / 2 : 0;

        for (int i = 0; i < n; i++)
        {
            int windowStart = center ? i - offset : i - windowSize + 1;
            int windowEnd = center ? i + (windowSize - offset) : i + 1;

            windowStart = Math.Max(0, windowStart);
            windowEnd = Math.Min(n, windowEnd);

            double min = double.PositiveInfinity;
            int validCount = 0;

            for (int j = windowStart; j < windowEnd; j++)
            {
                if (!series.IsNull(j))
                {
                    var val = series[j];
                    double d;
                    if (val.TryGetDouble(out d) || (val.TryGetInt64(out var l) && (d = l) == l))
                    {
                        if (d < min)
                            min = d;
                        validCount++;
                    }
                }
            }

            if (validCount >= minPeriods)
                builder.Append(min);
            else
                builder.AppendNull();
        }

        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Float64);
    }

    // ============================================================================
    // Rolling Max
    // ============================================================================

    public static Series RollingMax(Series series, int windowSize, int minPeriods = 1, bool center = false)
    {
        ValidateWindowParams(windowSize, minPeriods);

        var builder = new Apache.Arrow.DoubleArray.Builder();
        int n = series.Length;
        int offset = center ? windowSize / 2 : 0;

        for (int i = 0; i < n; i++)
        {
            int windowStart = center ? i - offset : i - windowSize + 1;
            int windowEnd = center ? i + (windowSize - offset) : i + 1;

            windowStart = Math.Max(0, windowStart);
            windowEnd = Math.Min(n, windowEnd);

            double max = double.NegativeInfinity;
            int validCount = 0;

            for (int j = windowStart; j < windowEnd; j++)
            {
                if (!series.IsNull(j))
                {
                    var val = series[j];
                    double d;
                    if (val.TryGetDouble(out d) || (val.TryGetInt64(out var l) && (d = l) == l))
                    {
                        if (d > max)
                            max = d;
                        validCount++;
                    }
                }
            }

            if (validCount >= minPeriods)
                builder.Append(max);
            else
                builder.AppendNull();
        }

        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Float64);
    }

    // ============================================================================
    // Rolling Std (Standard Deviation)
    // ============================================================================

    public static Series RollingStd(Series series, int windowSize, int minPeriods = 1, int ddof = 1, bool center = false)
    {
        ValidateWindowParams(windowSize, minPeriods);

        var builder = new Apache.Arrow.DoubleArray.Builder();
        int n = series.Length;
        int offset = center ? windowSize / 2 : 0;

        for (int i = 0; i < n; i++)
        {
            int windowStart = center ? i - offset : i - windowSize + 1;
            int windowEnd = center ? i + (windowSize - offset) : i + 1;

            windowStart = Math.Max(0, windowStart);
            windowEnd = Math.Min(n, windowEnd);

            // Two-pass: first compute mean
            double sum = 0;
            int validCount = 0;
            var values = new List<double>();

            for (int j = windowStart; j < windowEnd; j++)
            {
                if (!series.IsNull(j))
                {
                    var val = series[j];
                    double d;
                    if (val.TryGetDouble(out d) || (val.TryGetInt64(out var l) && (d = l) == l))
                    {
                        sum += d;
                        values.Add(d);
                        validCount++;
                    }
                }
            }

            if (validCount >= minPeriods && validCount > ddof)
            {
                double mean = sum / validCount;
                double sumSquaredDiff = 0;
                foreach (var v in values)
                {
                    var diff = v - mean;
                    sumSquaredDiff += diff * diff;
                }
                double variance = sumSquaredDiff / (validCount - ddof);
                builder.Append(Math.Sqrt(variance));
            }
            else
            {
                builder.AppendNull();
            }
        }

        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Float64);
    }

    // ============================================================================
    // Rolling Var (Variance)
    // ============================================================================

    public static Series RollingVar(Series series, int windowSize, int minPeriods = 1, int ddof = 1, bool center = false)
    {
        ValidateWindowParams(windowSize, minPeriods);

        var builder = new Apache.Arrow.DoubleArray.Builder();
        int n = series.Length;
        int offset = center ? windowSize / 2 : 0;

        for (int i = 0; i < n; i++)
        {
            int windowStart = center ? i - offset : i - windowSize + 1;
            int windowEnd = center ? i + (windowSize - offset) : i + 1;

            windowStart = Math.Max(0, windowStart);
            windowEnd = Math.Min(n, windowEnd);

            double sum = 0;
            int validCount = 0;
            var values = new List<double>();

            for (int j = windowStart; j < windowEnd; j++)
            {
                if (!series.IsNull(j))
                {
                    var val = series[j];
                    double d;
                    if (val.TryGetDouble(out d) || (val.TryGetInt64(out var l) && (d = l) == l))
                    {
                        sum += d;
                        values.Add(d);
                        validCount++;
                    }
                }
            }

            if (validCount >= minPeriods && validCount > ddof)
            {
                double mean = sum / validCount;
                double sumSquaredDiff = 0;
                foreach (var v in values)
                {
                    var diff = v - mean;
                    sumSquaredDiff += diff * diff;
                }
                builder.Append(sumSquaredDiff / (validCount - ddof));
            }
            else
            {
                builder.AppendNull();
            }
        }

        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Float64);
    }

    // ============================================================================
    // Rolling Median
    // ============================================================================

    public static Series RollingMedian(Series series, int windowSize, int minPeriods = 1, bool center = false)
    {
        ValidateWindowParams(windowSize, minPeriods);

        var builder = new Apache.Arrow.DoubleArray.Builder();
        int n = series.Length;
        int offset = center ? windowSize / 2 : 0;

        for (int i = 0; i < n; i++)
        {
            int windowStart = center ? i - offset : i - windowSize + 1;
            int windowEnd = center ? i + (windowSize - offset) : i + 1;

            windowStart = Math.Max(0, windowStart);
            windowEnd = Math.Min(n, windowEnd);

            var values = new List<double>();

            for (int j = windowStart; j < windowEnd; j++)
            {
                if (!series.IsNull(j))
                {
                    var val = series[j];
                    double d;
                    if (val.TryGetDouble(out d) || (val.TryGetInt64(out var l) && (d = l) == l))
                    {
                        values.Add(d);
                    }
                }
            }

            if (values.Count >= minPeriods)
            {
                values.Sort();
                int mid = values.Count / 2;
                if (values.Count % 2 == 0)
                    builder.Append((values[mid - 1] + values[mid]) / 2.0);
                else
                    builder.Append(values[mid]);
            }
            else
            {
                builder.AppendNull();
            }
        }

        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Float64);
    }

    // ============================================================================
    // Rolling Quantile
    // ============================================================================

    public static Series RollingQuantile(Series series, double quantile, int windowSize, int minPeriods = 1, string interpolation = "linear", bool center = false)
    {
        if (quantile < 0.0 || quantile > 1.0)
            throw new ArgumentOutOfRangeException(nameof(quantile), "Quantile must be between 0 and 1");

        ValidateWindowParams(windowSize, minPeriods);

        var builder = new Apache.Arrow.DoubleArray.Builder();
        int n = series.Length;
        int offset = center ? windowSize / 2 : 0;

        for (int i = 0; i < n; i++)
        {
            int windowStart = center ? i - offset : i - windowSize + 1;
            int windowEnd = center ? i + (windowSize - offset) : i + 1;

            windowStart = Math.Max(0, windowStart);
            windowEnd = Math.Min(n, windowEnd);

            var values = new List<double>();

            for (int j = windowStart; j < windowEnd; j++)
            {
                if (!series.IsNull(j))
                {
                    var val = series[j];
                    double d;
                    if (val.TryGetDouble(out d) || (val.TryGetInt64(out var l) && (d = l) == l))
                    {
                        values.Add(d);
                    }
                }
            }

            if (values.Count >= minPeriods)
            {
                values.Sort();
                double pos = quantile * (values.Count - 1);
                int lower = (int)Math.Floor(pos);
                int upper = (int)Math.Ceiling(pos);

                if (lower == upper || interpolation == "lower")
                    builder.Append(values[lower]);
                else if (interpolation == "higher")
                    builder.Append(values[upper]);
                else if (interpolation == "nearest")
                    builder.Append(pos - lower < upper - pos ? values[lower] : values[upper]);
                else if (interpolation == "midpoint")
                    builder.Append((values[lower] + values[upper]) / 2.0);
                else // linear
                {
                    double fraction = pos - lower;
                    builder.Append(values[lower] + fraction * (values[upper] - values[lower]));
                }
            }
            else
            {
                builder.AppendNull();
            }
        }

        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Float64);
    }

    // ============================================================================
    // Rolling Apply (custom function)
    // ============================================================================

    public static Series RollingApply(Series series, int windowSize, Func<double[], double?> func, int minPeriods = 1, bool center = false)
    {
        ValidateWindowParams(windowSize, minPeriods);

        var builder = new Apache.Arrow.DoubleArray.Builder();
        int n = series.Length;
        int offset = center ? windowSize / 2 : 0;

        for (int i = 0; i < n; i++)
        {
            int windowStart = center ? i - offset : i - windowSize + 1;
            int windowEnd = center ? i + (windowSize - offset) : i + 1;

            windowStart = Math.Max(0, windowStart);
            windowEnd = Math.Min(n, windowEnd);

            var values = new List<double>();

            for (int j = windowStart; j < windowEnd; j++)
            {
                if (!series.IsNull(j))
                {
                    var val = series[j];
                    double d;
                    if (val.TryGetDouble(out d) || (val.TryGetInt64(out var l) && (d = l) == l))
                    {
                        values.Add(d);
                    }
                }
            }

            if (values.Count >= minPeriods)
            {
                var result = func(values.ToArray());
                if (result.HasValue)
                    builder.Append(result.Value);
                else
                    builder.AppendNull();
            }
            else
            {
                builder.AppendNull();
            }
        }

        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Float64);
    }

    // ============================================================================
    // Expanding (cumulative from start)
    // ============================================================================

    public static Series ExpandingSum(Series series, int minPeriods = 1)
    {
        return RollingSum(series, int.MaxValue, minPeriods, center: false);
    }

    public static Series ExpandingMean(Series series, int minPeriods = 1)
    {
        return RollingMean(series, int.MaxValue, minPeriods, center: false);
    }

    public static Series ExpandingMin(Series series, int minPeriods = 1)
    {
        return RollingMin(series, int.MaxValue, minPeriods, center: false);
    }

    public static Series ExpandingMax(Series series, int minPeriods = 1)
    {
        return RollingMax(series, int.MaxValue, minPeriods, center: false);
    }

    public static Series ExpandingStd(Series series, int minPeriods = 1, int ddof = 1)
    {
        return RollingStd(series, int.MaxValue, minPeriods, ddof, center: false);
    }

    // ============================================================================
    // Helpers
    // ============================================================================

    private static void ValidateWindowParams(int windowSize, int minPeriods)
    {
        if (windowSize < 1)
            throw new ArgumentOutOfRangeException(nameof(windowSize), "Window size must be >= 1");
        if (minPeriods < 1)
            throw new ArgumentOutOfRangeException(nameof(minPeriods), "Min periods must be >= 1");
    }
}
