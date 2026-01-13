// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using System.Numerics;
using System.Runtime.CompilerServices;
using Polaire.DataTypes;
using Polaire.Core;
using Polaire.Series;

namespace Polaire.Compute;

/// <summary>
/// Aggregation operations for Series.
/// </summary>
public static class SeriesAggregations
{
    // ============================================================================
    // Sum
    // ============================================================================

    public static AnyValue Sum(Series series)
    {
        if (series.Length == 0 || series.Count() == 0)
            return AnyValue.Null;

        return series.DataType switch
        {
            DataType.Int8Type => SumInt8(series),
            DataType.Int16Type => SumInt16(series),
            DataType.Int32Type => SumInt32(series),
            DataType.Int64Type => SumInt64(series),
            DataType.UInt8Type => SumUInt8(series),
            DataType.UInt16Type => SumUInt16(series),
            DataType.UInt32Type => SumUInt32(series),
            DataType.UInt64Type => SumUInt64(series),
            DataType.Float32Type => SumFloat32(series),
            DataType.Float64Type => SumFloat64(series),
            _ => throw new NotSupportedException($"Sum not supported for {series.DataType}")
        };
    }

    private static AnyValue SumInt32(Series series)
    {
        var data = series.Data as ChunkedArray<int>;
        if (data is null) return AnyValue.Null;

        long sum = 0;
        if (!series.HasNulls && data.ChunkCount == 1)
        {
            // SIMD path
            var span = data.GetChunkSpan(0);
            sum = SumVectorized(span);
        }
        else
        {
            for (int i = 0; i < series.Length; i++)
            {
                if (!series.IsNull(i))
                    sum += data.GetValue(i);
            }
        }
        return AnyValue.From(sum);
    }

    private static AnyValue SumInt64(Series series)
    {
        var data = series.Data as ChunkedArray<long>;
        if (data is null) return AnyValue.Null;

        long sum = 0;
        if (!series.HasNulls && data.ChunkCount == 1)
        {
            var span = data.GetChunkSpan(0);
            sum = SumVectorized(span);
        }
        else
        {
            for (int i = 0; i < series.Length; i++)
            {
                if (!series.IsNull(i))
                    sum += data.GetValue(i);
            }
        }
        return AnyValue.From(sum);
    }

    private static AnyValue SumFloat64(Series series)
    {
        var data = series.Data as ChunkedArray<double>;
        if (data is null) return AnyValue.Null;

        double sum = 0;
        if (!series.HasNulls && data.ChunkCount == 1)
        {
            var span = data.GetChunkSpan(0);
            sum = SumVectorized(span);
        }
        else
        {
            for (int i = 0; i < series.Length; i++)
            {
                if (!series.IsNull(i))
                    sum += data.GetValue(i);
            }
        }
        return AnyValue.From(sum);
    }

    private static AnyValue SumInt8(Series series)
    {
        var data = series.Data as ChunkedArray<sbyte>;
        if (data is null) return AnyValue.Null;

        long sum = 0;
        for (int i = 0; i < series.Length; i++)
        {
            if (!series.IsNull(i))
                sum += data.GetValue(i);
        }
        return AnyValue.From(sum);
    }

    private static AnyValue SumInt16(Series series)
    {
        var data = series.Data as ChunkedArray<short>;
        if (data is null) return AnyValue.Null;

        long sum = 0;
        for (int i = 0; i < series.Length; i++)
        {
            if (!series.IsNull(i))
                sum += data.GetValue(i);
        }
        return AnyValue.From(sum);
    }

    private static AnyValue SumUInt8(Series series)
    {
        var data = series.Data as ChunkedArray<byte>;
        if (data is null) return AnyValue.Null;

        ulong sum = 0;
        for (int i = 0; i < series.Length; i++)
        {
            if (!series.IsNull(i))
                sum += data.GetValue(i);
        }
        return AnyValue.From(sum);
    }

    private static AnyValue SumUInt16(Series series)
    {
        var data = series.Data as ChunkedArray<ushort>;
        if (data is null) return AnyValue.Null;

        ulong sum = 0;
        for (int i = 0; i < series.Length; i++)
        {
            if (!series.IsNull(i))
                sum += data.GetValue(i);
        }
        return AnyValue.From(sum);
    }

    private static AnyValue SumUInt32(Series series)
    {
        var data = series.Data as ChunkedArray<uint>;
        if (data is null) return AnyValue.Null;

        ulong sum = 0;
        for (int i = 0; i < series.Length; i++)
        {
            if (!series.IsNull(i))
                sum += data.GetValue(i);
        }
        return AnyValue.From(sum);
    }

    private static AnyValue SumUInt64(Series series)
    {
        var data = series.Data as ChunkedArray<ulong>;
        if (data is null) return AnyValue.Null;

        ulong sum = 0;
        for (int i = 0; i < series.Length; i++)
        {
            if (!series.IsNull(i))
                sum += data.GetValue(i);
        }
        return AnyValue.From(sum);
    }

    private static AnyValue SumFloat32(Series series)
    {
        var data = series.Data as ChunkedArray<float>;
        if (data is null) return AnyValue.Null;

        double sum = 0; // Use double for precision
        for (int i = 0; i < series.Length; i++)
        {
            if (!series.IsNull(i))
                sum += data.GetValue(i);
        }
        return AnyValue.From(sum);
    }

    // ============================================================================
    // Min
    // ============================================================================

    public static AnyValue Min(Series series)
    {
        if (series.Length == 0 || series.Count() == 0)
            return AnyValue.Null;

        return series.DataType switch
        {
            DataType.Int32Type => MinInt32(series),
            DataType.Int64Type => MinInt64(series),
            DataType.Float32Type => MinFloat32(series),
            DataType.Float64Type => MinFloat64(series),
            DataType.DateType => MinDate(series),
            _ => MinGeneric(series)
        };
    }

    private static AnyValue MinInt32(Series series)
    {
        var data = series.Data as ChunkedArray<int>;
        if (data is null) return AnyValue.Null;

        int min = int.MaxValue;
        bool found = false;
        for (int i = 0; i < series.Length; i++)
        {
            if (!series.IsNull(i))
            {
                var val = data.GetValue(i);
                if (val < min)
                {
                    min = val;
                    found = true;
                }
            }
        }
        return found ? AnyValue.From(min) : AnyValue.Null;
    }

    private static AnyValue MinInt64(Series series)
    {
        var data = series.Data as ChunkedArray<long>;
        if (data is null) return AnyValue.Null;

        long min = long.MaxValue;
        bool found = false;
        for (int i = 0; i < series.Length; i++)
        {
            if (!series.IsNull(i))
            {
                var val = data.GetValue(i);
                if (val < min)
                {
                    min = val;
                    found = true;
                }
            }
        }
        return found ? AnyValue.From(min) : AnyValue.Null;
    }

    private static AnyValue MinFloat32(Series series)
    {
        var data = series.Data as ChunkedArray<float>;
        if (data is null) return AnyValue.Null;

        float min = float.PositiveInfinity;
        bool found = false;
        for (int i = 0; i < series.Length; i++)
        {
            if (!series.IsNull(i))
            {
                var val = data.GetValue(i);
                if (!float.IsNaN(val) && val < min)
                {
                    min = val;
                    found = true;
                }
            }
        }
        return found ? AnyValue.From(min) : AnyValue.Null;
    }

    private static AnyValue MinFloat64(Series series)
    {
        var data = series.Data as ChunkedArray<double>;
        if (data is null) return AnyValue.Null;

        double min = double.PositiveInfinity;
        bool found = false;
        for (int i = 0; i < series.Length; i++)
        {
            if (!series.IsNull(i))
            {
                var val = data.GetValue(i);
                if (!double.IsNaN(val) && val < min)
                {
                    min = val;
                    found = true;
                }
            }
        }
        return found ? AnyValue.From(min) : AnyValue.Null;
    }

    private static AnyValue MinDate(Series series)
    {
        var data = series.Data as ChunkedArray<int>;
        if (data is null) return AnyValue.Null;

        int min = int.MaxValue;
        bool found = false;
        for (int i = 0; i < series.Length; i++)
        {
            if (!series.IsNull(i))
            {
                var val = data.GetValue(i);
                if (val < min)
                {
                    min = val;
                    found = true;
                }
            }
        }
        return found ? AnyValue.From(DateOnly.FromDayNumber(min)) : AnyValue.Null;
    }

    private static AnyValue MinGeneric(Series series)
    {
        AnyValue min = AnyValue.Null;
        bool found = false;

        for (int i = 0; i < series.Length; i++)
        {
            if (!series.IsNull(i))
            {
                var val = series[i];
                if (!found || val < min)
                {
                    min = val;
                    found = true;
                }
            }
        }

        return min;
    }

    // ============================================================================
    // Max
    // ============================================================================

    public static AnyValue Max(Series series)
    {
        if (series.Length == 0 || series.Count() == 0)
            return AnyValue.Null;

        return series.DataType switch
        {
            DataType.Int32Type => MaxInt32(series),
            DataType.Int64Type => MaxInt64(series),
            DataType.Float32Type => MaxFloat32(series),
            DataType.Float64Type => MaxFloat64(series),
            _ => MaxGeneric(series)
        };
    }

    private static AnyValue MaxInt32(Series series)
    {
        var data = series.Data as ChunkedArray<int>;
        if (data is null) return AnyValue.Null;

        int max = int.MinValue;
        bool found = false;
        for (int i = 0; i < series.Length; i++)
        {
            if (!series.IsNull(i))
            {
                var val = data.GetValue(i);
                if (val > max)
                {
                    max = val;
                    found = true;
                }
            }
        }
        return found ? AnyValue.From(max) : AnyValue.Null;
    }

    private static AnyValue MaxInt64(Series series)
    {
        var data = series.Data as ChunkedArray<long>;
        if (data is null) return AnyValue.Null;

        long max = long.MinValue;
        bool found = false;
        for (int i = 0; i < series.Length; i++)
        {
            if (!series.IsNull(i))
            {
                var val = data.GetValue(i);
                if (val > max)
                {
                    max = val;
                    found = true;
                }
            }
        }
        return found ? AnyValue.From(max) : AnyValue.Null;
    }

    private static AnyValue MaxFloat32(Series series)
    {
        var data = series.Data as ChunkedArray<float>;
        if (data is null) return AnyValue.Null;

        float max = float.NegativeInfinity;
        bool found = false;
        for (int i = 0; i < series.Length; i++)
        {
            if (!series.IsNull(i))
            {
                var val = data.GetValue(i);
                if (!float.IsNaN(val) && val > max)
                {
                    max = val;
                    found = true;
                }
            }
        }
        return found ? AnyValue.From(max) : AnyValue.Null;
    }

    private static AnyValue MaxFloat64(Series series)
    {
        var data = series.Data as ChunkedArray<double>;
        if (data is null) return AnyValue.Null;

        double max = double.NegativeInfinity;
        bool found = false;
        for (int i = 0; i < series.Length; i++)
        {
            if (!series.IsNull(i))
            {
                var val = data.GetValue(i);
                if (!double.IsNaN(val) && val > max)
                {
                    max = val;
                    found = true;
                }
            }
        }
        return found ? AnyValue.From(max) : AnyValue.Null;
    }

    private static AnyValue MaxGeneric(Series series)
    {
        AnyValue max = AnyValue.Null;
        bool found = false;

        for (int i = 0; i < series.Length; i++)
        {
            if (!series.IsNull(i))
            {
                var val = series[i];
                if (!found || val > max)
                {
                    max = val;
                    found = true;
                }
            }
        }

        return max;
    }

    // ============================================================================
    // Mean
    // ============================================================================

    public static AnyValue Mean(Series series)
    {
        if (series.Length == 0 || series.Count() == 0)
            return AnyValue.Null;

        var sum = Sum(series);
        if (sum.IsNull)
            return AnyValue.Null;

        double sumValue;
        if (sum.TryGetDouble(out var d))
            sumValue = d;
        else if (sum.TryGetInt64(out var l))
            sumValue = l;
        else
            return AnyValue.Null;

        return AnyValue.From(sumValue / series.Count());
    }

    // ============================================================================
    // Median
    // ============================================================================

    public static AnyValue Median(Series series)
    {
        if (series.Length == 0 || series.Count() == 0)
            return AnyValue.Null;

        // Get non-null values and sort
        var values = new List<double>();
        for (int i = 0; i < series.Length; i++)
        {
            if (!series.IsNull(i))
            {
                var val = series[i];
                if (val.TryGetDouble(out var d))
                    values.Add(d);
                else if (val.TryGetInt64(out var l))
                    values.Add(l);
            }
        }

        if (values.Count == 0)
            return AnyValue.Null;

        values.Sort();

        int mid = values.Count / 2;
        if (values.Count % 2 == 0)
        {
            return AnyValue.From((values[mid - 1] + values[mid]) / 2.0);
        }
        else
        {
            return AnyValue.From(values[mid]);
        }
    }

    // ============================================================================
    // Variance and Standard Deviation
    // ============================================================================

    public static AnyValue Var(Series series, int ddof = 1)
    {
        if (series.Length == 0 || series.Count() <= ddof)
            return AnyValue.Null;

        var mean = Mean(series);
        if (mean.IsNull || !mean.TryGetDouble(out var meanValue))
            return AnyValue.Null;

        double sumSquaredDiff = 0;
        int count = 0;

        for (int i = 0; i < series.Length; i++)
        {
            if (!series.IsNull(i))
            {
                var val = series[i];
                if (val.TryGetDouble(out var d))
                {
                    var diff = d - meanValue;
                    sumSquaredDiff += diff * diff;
                    count++;
                }
                else if (val.TryGetInt64(out var l))
                {
                    var diff = l - meanValue;
                    sumSquaredDiff += diff * diff;
                    count++;
                }
            }
        }

        if (count <= ddof)
            return AnyValue.Null;

        return AnyValue.From(sumSquaredDiff / (count - ddof));
    }

    public static AnyValue Std(Series series, int ddof = 1)
    {
        var variance = Var(series, ddof);
        if (variance.IsNull || !variance.TryGetDouble(out var varValue))
            return AnyValue.Null;

        return AnyValue.From(Math.Sqrt(varValue));
    }

    // ============================================================================
    // Vectorized Helpers
    // ============================================================================

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long SumVectorized(ReadOnlySpan<int> span)
    {
        long sum = 0;
        int i = 0;

        if (Vector.IsHardwareAccelerated && span.Length >= Vector<int>.Count)
        {
            var vSum = Vector<long>.Zero;
            var vectorCount = span.Length - (span.Length % Vector<int>.Count);

            // Process in chunks, widening to long to avoid overflow
            for (; i < vectorCount; i += Vector<int>.Count)
            {
                var v = new Vector<int>(span.Slice(i));
                // Widen to long
                Vector.Widen(v, out var low, out var high);
                vSum += low + high;
            }

            // Sum vector elements
            for (int j = 0; j < Vector<long>.Count; j++)
                sum += vSum[j];
        }

        // Scalar remainder
        for (; i < span.Length; i++)
            sum += span[i];

        return sum;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long SumVectorized(ReadOnlySpan<long> span)
    {
        long sum = 0;
        int i = 0;

        if (Vector.IsHardwareAccelerated && span.Length >= Vector<long>.Count)
        {
            var vSum = Vector<long>.Zero;
            var vectorCount = span.Length - (span.Length % Vector<long>.Count);

            for (; i < vectorCount; i += Vector<long>.Count)
            {
                vSum += new Vector<long>(span.Slice(i));
            }

            for (int j = 0; j < Vector<long>.Count; j++)
                sum += vSum[j];
        }

        for (; i < span.Length; i++)
            sum += span[i];

        return sum;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double SumVectorized(ReadOnlySpan<double> span)
    {
        double sum = 0;
        int i = 0;

        if (Vector.IsHardwareAccelerated && span.Length >= Vector<double>.Count)
        {
            var vSum = Vector<double>.Zero;
            var vectorCount = span.Length - (span.Length % Vector<double>.Count);

            for (; i < vectorCount; i += Vector<double>.Count)
            {
                vSum += new Vector<double>(span.Slice(i));
            }

            for (int j = 0; j < Vector<double>.Count; j++)
                sum += vSum[j];
        }

        for (; i < span.Length; i++)
            sum += span[i];

        return sum;
    }
}
