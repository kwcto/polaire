// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using Polaire.DataTypes;
using Polaire.Core;


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

        int min;
        if (!series.HasNulls && data.ChunkCount == 1)
        {
            // SIMD path
            var span = data.GetChunkSpan(0);
            min = MinVectorized(span);
        }
        else
        {
            min = int.MaxValue;
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
            if (!found) return AnyValue.Null;
        }
        return AnyValue.From(min);
    }

    private static AnyValue MinInt64(Series series)
    {
        var data = series.Data as ChunkedArray<long>;
        if (data is null) return AnyValue.Null;

        long min;
        if (!series.HasNulls && data.ChunkCount == 1)
        {
            // SIMD path
            var span = data.GetChunkSpan(0);
            min = MinVectorized(span);
        }
        else
        {
            min = long.MaxValue;
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
            if (!found) return AnyValue.Null;
        }
        return AnyValue.From(min);
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

        double min;
        if (!series.HasNulls && data.ChunkCount == 1)
        {
            // SIMD path (note: NaN handling differs - NaN propagates)
            var span = data.GetChunkSpan(0);
            min = MinVectorized(span);
            if (double.IsNaN(min) || double.IsPositiveInfinity(min))
                return AnyValue.Null;
        }
        else
        {
            min = double.PositiveInfinity;
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
            if (!found) return AnyValue.Null;
        }
        return AnyValue.From(min);
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

        int max;
        if (!series.HasNulls && data.ChunkCount == 1)
        {
            // SIMD path
            var span = data.GetChunkSpan(0);
            max = MaxVectorized(span);
        }
        else
        {
            max = int.MinValue;
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
            if (!found) return AnyValue.Null;
        }
        return AnyValue.From(max);
    }

    private static AnyValue MaxInt64(Series series)
    {
        var data = series.Data as ChunkedArray<long>;
        if (data is null) return AnyValue.Null;

        long max;
        if (!series.HasNulls && data.ChunkCount == 1)
        {
            // SIMD path
            var span = data.GetChunkSpan(0);
            max = MaxVectorized(span);
        }
        else
        {
            max = long.MinValue;
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
            if (!found) return AnyValue.Null;
        }
        return AnyValue.From(max);
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

        double max;
        if (!series.HasNulls && data.ChunkCount == 1)
        {
            // SIMD path (note: NaN handling differs - NaN propagates)
            var span = data.GetChunkSpan(0);
            max = MaxVectorized(span);
            if (double.IsNaN(max) || double.IsNegativeInfinity(max))
                return AnyValue.Null;
        }
        else
        {
            max = double.NegativeInfinity;
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
            if (!found) return AnyValue.Null;
        }
        return AnyValue.From(max);
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

        return series.DataType switch
        {
            DataType.Int32Type => VarInt32(series, ddof),
            DataType.Int64Type => VarInt64(series, ddof),
            DataType.Float64Type => VarFloat64(series, ddof),
            _ => VarGeneric(series, ddof)
        };
    }

    private static AnyValue VarFloat64(Series series, int ddof)
    {
        var data = series.Data as ChunkedArray<double>;
        if (data is null) return AnyValue.Null;

        if (!series.HasNulls && data.ChunkCount == 1)
        {
            // SIMD path: two-pass algorithm
            var span = data.GetChunkSpan(0);
            int count = span.Length;
            if (count <= ddof) return AnyValue.Null;

            // Pass 1: compute mean
            double sum = SumVectorized(span);
            double mean = sum / count;

            // Pass 2: compute sum of squared differences
            double sumSqDiff = SumSquaredDiffVectorized(span, mean);

            return AnyValue.From(sumSqDiff / (count - ddof));
        }
        else
        {
            // Scalar fallback for nulls/multiple chunks
            return VarGeneric(series, ddof);
        }
    }

    private static AnyValue VarInt32(Series series, int ddof)
    {
        var data = series.Data as ChunkedArray<int>;
        if (data is null) return AnyValue.Null;

        if (!series.HasNulls && data.ChunkCount == 1)
        {
            // SIMD path: two-pass algorithm
            var span = data.GetChunkSpan(0);
            int count = span.Length;
            if (count <= ddof) return AnyValue.Null;

            // Pass 1: compute mean using long sum to avoid overflow
            long sum = SumVectorized(span);
            double mean = (double)sum / count;

            // Pass 2: compute sum of squared differences
            double sumSqDiff = SumSquaredDiffVectorized(span, mean);

            return AnyValue.From(sumSqDiff / (count - ddof));
        }
        else
        {
            return VarGeneric(series, ddof);
        }
    }

    private static AnyValue VarInt64(Series series, int ddof)
    {
        var data = series.Data as ChunkedArray<long>;
        if (data is null) return AnyValue.Null;

        if (!series.HasNulls && data.ChunkCount == 1)
        {
            // SIMD path: two-pass algorithm
            var span = data.GetChunkSpan(0);
            int count = span.Length;
            if (count <= ddof) return AnyValue.Null;

            // Pass 1: compute mean
            long sum = SumVectorized(span);
            double mean = (double)sum / count;

            // Pass 2: compute sum of squared differences
            double sumSqDiff = SumSquaredDiffVectorized(span, mean);

            return AnyValue.From(sumSqDiff / (count - ddof));
        }
        else
        {
            return VarGeneric(series, ddof);
        }
    }

    private static AnyValue VarGeneric(Series series, int ddof)
    {
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

        ref double ptr = ref MemoryMarshal.GetReference(span);

        if (AdvSimd.Arm64.IsSupported && span.Length >= 8)
        {
            // ARM NEON path (128-bit = 2 doubles per vector)
            // Use 4 accumulators for better instruction-level parallelism
            var vSum0 = Vector128<double>.Zero;
            var vSum1 = Vector128<double>.Zero;
            var vSum2 = Vector128<double>.Zero;
            var vSum3 = Vector128<double>.Zero;

            int vectorCount = span.Length - (span.Length % 8);

            for (; i < vectorCount; i += 8)
            {
                var v0 = Vector128.LoadUnsafe(ref Unsafe.Add(ref ptr, i));
                var v1 = Vector128.LoadUnsafe(ref Unsafe.Add(ref ptr, i + 2));
                var v2 = Vector128.LoadUnsafe(ref Unsafe.Add(ref ptr, i + 4));
                var v3 = Vector128.LoadUnsafe(ref Unsafe.Add(ref ptr, i + 6));

                vSum0 = AdvSimd.Arm64.Add(vSum0, v0);
                vSum1 = AdvSimd.Arm64.Add(vSum1, v1);
                vSum2 = AdvSimd.Arm64.Add(vSum2, v2);
                vSum3 = AdvSimd.Arm64.Add(vSum3, v3);
            }

            // Combine accumulators
            vSum0 = AdvSimd.Arm64.Add(vSum0, vSum1);
            vSum2 = AdvSimd.Arm64.Add(vSum2, vSum3);
            vSum0 = AdvSimd.Arm64.Add(vSum0, vSum2);

            // Horizontal sum (NEON has AddPairwise for this)
            sum = AdvSimd.Arm64.AddPairwiseScalar(vSum0).ToScalar();
        }
        else if (Avx.IsSupported && span.Length >= 16)
        {
            // x64 AVX path (256-bit = 4 doubles per vector)
            // Use 4 accumulators for better instruction-level parallelism
            var vSum0 = Vector256<double>.Zero;
            var vSum1 = Vector256<double>.Zero;
            var vSum2 = Vector256<double>.Zero;
            var vSum3 = Vector256<double>.Zero;

            int vectorCount = span.Length - (span.Length % 16);

            for (; i < vectorCount; i += 16)
            {
                var v0 = Vector256.LoadUnsafe(ref Unsafe.Add(ref ptr, i));
                var v1 = Vector256.LoadUnsafe(ref Unsafe.Add(ref ptr, i + 4));
                var v2 = Vector256.LoadUnsafe(ref Unsafe.Add(ref ptr, i + 8));
                var v3 = Vector256.LoadUnsafe(ref Unsafe.Add(ref ptr, i + 12));

                vSum0 = Avx.Add(vSum0, v0);
                vSum1 = Avx.Add(vSum1, v1);
                vSum2 = Avx.Add(vSum2, v2);
                vSum3 = Avx.Add(vSum3, v3);
            }

            // Combine accumulators
            vSum0 = Avx.Add(vSum0, vSum1);
            vSum2 = Avx.Add(vSum2, vSum3);
            vSum0 = Avx.Add(vSum0, vSum2);

            // Horizontal sum
            sum = vSum0.GetElement(0) + vSum0.GetElement(1) + vSum0.GetElement(2) + vSum0.GetElement(3);
        }
        else if (Vector.IsHardwareAccelerated && span.Length >= Vector<double>.Count)
        {
            // Fallback to portable SIMD
            var vSum = Vector<double>.Zero;
            var vectorCount = span.Length - (span.Length % Vector<double>.Count);

            for (; i < vectorCount; i += Vector<double>.Count)
            {
                vSum += new Vector<double>(span.Slice(i));
            }

            for (int j = 0; j < Vector<double>.Count; j++)
                sum += vSum[j];
        }

        // Scalar remainder
        for (; i < span.Length; i++)
            sum += span[i];

        return sum;
    }

    // ============================================================================
    // Vectorized Sum of Squared Differences (for Variance)
    // ============================================================================

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double SumSquaredDiffVectorized(ReadOnlySpan<double> span, double mean)
    {
        double sum = 0;
        int i = 0;

        ref double ptr = ref MemoryMarshal.GetReference(span);

        if (AdvSimd.Arm64.IsSupported && span.Length >= 8)
        {
            // ARM NEON path (128-bit = 2 doubles per vector)
            var vMean = Vector128.Create(mean);
            var vSum0 = Vector128<double>.Zero;
            var vSum1 = Vector128<double>.Zero;
            var vSum2 = Vector128<double>.Zero;
            var vSum3 = Vector128<double>.Zero;

            int vectorCount = span.Length - (span.Length % 8);

            for (; i < vectorCount; i += 8)
            {
                var v0 = Vector128.LoadUnsafe(ref Unsafe.Add(ref ptr, i));
                var v1 = Vector128.LoadUnsafe(ref Unsafe.Add(ref ptr, i + 2));
                var v2 = Vector128.LoadUnsafe(ref Unsafe.Add(ref ptr, i + 4));
                var v3 = Vector128.LoadUnsafe(ref Unsafe.Add(ref ptr, i + 6));

                var diff0 = AdvSimd.Arm64.Subtract(v0, vMean);
                var diff1 = AdvSimd.Arm64.Subtract(v1, vMean);
                var diff2 = AdvSimd.Arm64.Subtract(v2, vMean);
                var diff3 = AdvSimd.Arm64.Subtract(v3, vMean);

                vSum0 = AdvSimd.Arm64.Add(vSum0, AdvSimd.Arm64.Multiply(diff0, diff0));
                vSum1 = AdvSimd.Arm64.Add(vSum1, AdvSimd.Arm64.Multiply(diff1, diff1));
                vSum2 = AdvSimd.Arm64.Add(vSum2, AdvSimd.Arm64.Multiply(diff2, diff2));
                vSum3 = AdvSimd.Arm64.Add(vSum3, AdvSimd.Arm64.Multiply(diff3, diff3));
            }

            // Combine accumulators
            vSum0 = AdvSimd.Arm64.Add(vSum0, vSum1);
            vSum2 = AdvSimd.Arm64.Add(vSum2, vSum3);
            vSum0 = AdvSimd.Arm64.Add(vSum0, vSum2);

            // Horizontal sum
            sum = AdvSimd.Arm64.AddPairwiseScalar(vSum0).ToScalar();
        }
        else if (Avx.IsSupported && span.Length >= 16)
        {
            // x64 AVX path (256-bit = 4 doubles per vector)
            var vMean = Vector256.Create(mean);
            var vSum0 = Vector256<double>.Zero;
            var vSum1 = Vector256<double>.Zero;
            var vSum2 = Vector256<double>.Zero;
            var vSum3 = Vector256<double>.Zero;

            int vectorCount = span.Length - (span.Length % 16);

            for (; i < vectorCount; i += 16)
            {
                var v0 = Vector256.LoadUnsafe(ref Unsafe.Add(ref ptr, i));
                var v1 = Vector256.LoadUnsafe(ref Unsafe.Add(ref ptr, i + 4));
                var v2 = Vector256.LoadUnsafe(ref Unsafe.Add(ref ptr, i + 8));
                var v3 = Vector256.LoadUnsafe(ref Unsafe.Add(ref ptr, i + 12));

                var diff0 = Avx.Subtract(v0, vMean);
                var diff1 = Avx.Subtract(v1, vMean);
                var diff2 = Avx.Subtract(v2, vMean);
                var diff3 = Avx.Subtract(v3, vMean);

                vSum0 = Avx.Add(vSum0, Avx.Multiply(diff0, diff0));
                vSum1 = Avx.Add(vSum1, Avx.Multiply(diff1, diff1));
                vSum2 = Avx.Add(vSum2, Avx.Multiply(diff2, diff2));
                vSum3 = Avx.Add(vSum3, Avx.Multiply(diff3, diff3));
            }

            // Combine accumulators
            vSum0 = Avx.Add(vSum0, vSum1);
            vSum2 = Avx.Add(vSum2, vSum3);
            vSum0 = Avx.Add(vSum0, vSum2);

            // Horizontal sum
            sum = vSum0.GetElement(0) + vSum0.GetElement(1) + vSum0.GetElement(2) + vSum0.GetElement(3);
        }
        else if (Vector.IsHardwareAccelerated && span.Length >= Vector<double>.Count)
        {
            // Fallback to portable SIMD
            var vMean = new Vector<double>(mean);
            var vSum = Vector<double>.Zero;
            var vectorCount = span.Length - (span.Length % Vector<double>.Count);

            for (; i < vectorCount; i += Vector<double>.Count)
            {
                var v = new Vector<double>(span.Slice(i));
                var diff = v - vMean;
                vSum += diff * diff;
            }

            // Reduce vector to scalar
            for (int j = 0; j < Vector<double>.Count; j++)
                sum += vSum[j];
        }

        // Scalar remainder
        for (; i < span.Length; i++)
        {
            var diff = span[i] - mean;
            sum += diff * diff;
        }

        return sum;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double SumSquaredDiffVectorized(ReadOnlySpan<int> span, double mean)
    {
        double sum = 0;
        int i = 0;

        if (Vector.IsHardwareAccelerated && span.Length >= Vector<double>.Count)
        {
            var vMean = new Vector<double>(mean);
            var vSum = Vector<double>.Zero;
            var vectorCount = span.Length - (span.Length % Vector<double>.Count);

            // Allocate temp buffer outside loop
            Span<double> temp = stackalloc double[Vector<double>.Count];

            for (; i < vectorCount; i += Vector<double>.Count)
            {
                // Convert ints to doubles for this chunk
                for (int k = 0; k < Vector<double>.Count; k++)
                    temp[k] = span[i + k];

                var v = new Vector<double>(temp);
                var diff = v - vMean;
                vSum += diff * diff;
            }

            for (int j = 0; j < Vector<double>.Count; j++)
                sum += vSum[j];
        }

        for (; i < span.Length; i++)
        {
            var diff = span[i] - mean;
            sum += diff * diff;
        }

        return sum;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double SumSquaredDiffVectorized(ReadOnlySpan<long> span, double mean)
    {
        double sum = 0;
        int i = 0;

        if (Vector.IsHardwareAccelerated && span.Length >= Vector<double>.Count)
        {
            var vMean = new Vector<double>(mean);
            var vSum = Vector<double>.Zero;
            var vectorCount = span.Length - (span.Length % Vector<double>.Count);

            // Allocate temp buffer outside loop
            Span<double> temp = stackalloc double[Vector<double>.Count];

            for (; i < vectorCount; i += Vector<double>.Count)
            {
                // Convert longs to doubles for this chunk
                for (int k = 0; k < Vector<double>.Count; k++)
                    temp[k] = span[i + k];

                var v = new Vector<double>(temp);
                var diff = v - vMean;
                vSum += diff * diff;
            }

            for (int j = 0; j < Vector<double>.Count; j++)
                sum += vSum[j];
        }

        for (; i < span.Length; i++)
        {
            var diff = span[i] - mean;
            sum += diff * diff;
        }

        return sum;
    }

    // ============================================================================
    // Vectorized Min Helpers
    // ============================================================================

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int MinVectorized(ReadOnlySpan<int> span)
    {
        int min = int.MaxValue;
        int i = 0;

        if (Vector.IsHardwareAccelerated && span.Length >= Vector<int>.Count)
        {
            var vMin = new Vector<int>(int.MaxValue);
            var vectorCount = span.Length - (span.Length % Vector<int>.Count);

            for (; i < vectorCount; i += Vector<int>.Count)
            {
                vMin = Vector.Min(vMin, new Vector<int>(span.Slice(i)));
            }

            // Reduce vector to scalar
            for (int j = 0; j < Vector<int>.Count; j++)
            {
                if (vMin[j] < min) min = vMin[j];
            }
        }

        // Scalar remainder
        for (; i < span.Length; i++)
        {
            if (span[i] < min) min = span[i];
        }

        return min;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long MinVectorized(ReadOnlySpan<long> span)
    {
        long min = long.MaxValue;
        int i = 0;

        if (Vector.IsHardwareAccelerated && span.Length >= Vector<long>.Count)
        {
            var vMin = new Vector<long>(long.MaxValue);
            var vectorCount = span.Length - (span.Length % Vector<long>.Count);

            for (; i < vectorCount; i += Vector<long>.Count)
            {
                vMin = Vector.Min(vMin, new Vector<long>(span.Slice(i)));
            }

            for (int j = 0; j < Vector<long>.Count; j++)
            {
                if (vMin[j] < min) min = vMin[j];
            }
        }

        for (; i < span.Length; i++)
        {
            if (span[i] < min) min = span[i];
        }

        return min;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double MinVectorized(ReadOnlySpan<double> span)
    {
        double min = double.PositiveInfinity;
        int i = 0;

        ref double ptr = ref MemoryMarshal.GetReference(span);

        if (AdvSimd.Arm64.IsSupported && span.Length >= 8)
        {
            // ARM NEON path (128-bit = 2 doubles per vector)
            // Use 4 accumulators for better instruction-level parallelism
            var vMin0 = Vector128.Create(double.PositiveInfinity);
            var vMin1 = Vector128.Create(double.PositiveInfinity);
            var vMin2 = Vector128.Create(double.PositiveInfinity);
            var vMin3 = Vector128.Create(double.PositiveInfinity);

            int vectorCount = span.Length - (span.Length % 8);

            for (; i < vectorCount; i += 8)
            {
                var v0 = Vector128.LoadUnsafe(ref Unsafe.Add(ref ptr, i));
                var v1 = Vector128.LoadUnsafe(ref Unsafe.Add(ref ptr, i + 2));
                var v2 = Vector128.LoadUnsafe(ref Unsafe.Add(ref ptr, i + 4));
                var v3 = Vector128.LoadUnsafe(ref Unsafe.Add(ref ptr, i + 6));

                vMin0 = AdvSimd.Arm64.Min(vMin0, v0);
                vMin1 = AdvSimd.Arm64.Min(vMin1, v1);
                vMin2 = AdvSimd.Arm64.Min(vMin2, v2);
                vMin3 = AdvSimd.Arm64.Min(vMin3, v3);
            }

            // Combine accumulators
            vMin0 = AdvSimd.Arm64.Min(vMin0, vMin1);
            vMin2 = AdvSimd.Arm64.Min(vMin2, vMin3);
            vMin0 = AdvSimd.Arm64.Min(vMin0, vMin2);

            // Horizontal min (NEON has MinPairwise for this)
            min = AdvSimd.Arm64.MinPairwiseScalar(vMin0).ToScalar();
        }
        else if (Avx.IsSupported && span.Length >= 16)
        {
            // x64 AVX path (256-bit = 4 doubles per vector)
            var vMin0 = Vector256.Create(double.PositiveInfinity);
            var vMin1 = Vector256.Create(double.PositiveInfinity);
            var vMin2 = Vector256.Create(double.PositiveInfinity);
            var vMin3 = Vector256.Create(double.PositiveInfinity);

            int vectorCount = span.Length - (span.Length % 16);

            for (; i < vectorCount; i += 16)
            {
                var v0 = Vector256.LoadUnsafe(ref Unsafe.Add(ref ptr, i));
                var v1 = Vector256.LoadUnsafe(ref Unsafe.Add(ref ptr, i + 4));
                var v2 = Vector256.LoadUnsafe(ref Unsafe.Add(ref ptr, i + 8));
                var v3 = Vector256.LoadUnsafe(ref Unsafe.Add(ref ptr, i + 12));

                vMin0 = Avx.Min(vMin0, v0);
                vMin1 = Avx.Min(vMin1, v1);
                vMin2 = Avx.Min(vMin2, v2);
                vMin3 = Avx.Min(vMin3, v3);
            }

            // Combine accumulators
            vMin0 = Avx.Min(vMin0, vMin1);
            vMin2 = Avx.Min(vMin2, vMin3);
            vMin0 = Avx.Min(vMin0, vMin2);

            // Horizontal min
            min = Math.Min(Math.Min(vMin0.GetElement(0), vMin0.GetElement(1)),
                          Math.Min(vMin0.GetElement(2), vMin0.GetElement(3)));
        }
        else if (Vector.IsHardwareAccelerated && span.Length >= Vector<double>.Count)
        {
            // Fallback to portable SIMD
            var vMin = new Vector<double>(double.PositiveInfinity);
            var vectorCount = span.Length - (span.Length % Vector<double>.Count);

            for (; i < vectorCount; i += Vector<double>.Count)
            {
                vMin = Vector.Min(vMin, new Vector<double>(span.Slice(i)));
            }

            for (int j = 0; j < Vector<double>.Count; j++)
            {
                if (vMin[j] < min) min = vMin[j];
            }
        }

        // Scalar remainder
        for (; i < span.Length; i++)
        {
            if (span[i] < min) min = span[i];
        }

        return min;
    }

    // ============================================================================
    // Vectorized Max Helpers
    // ============================================================================

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int MaxVectorized(ReadOnlySpan<int> span)
    {
        int max = int.MinValue;
        int i = 0;

        if (Vector.IsHardwareAccelerated && span.Length >= Vector<int>.Count)
        {
            var vMax = new Vector<int>(int.MinValue);
            var vectorCount = span.Length - (span.Length % Vector<int>.Count);

            for (; i < vectorCount; i += Vector<int>.Count)
            {
                vMax = Vector.Max(vMax, new Vector<int>(span.Slice(i)));
            }

            for (int j = 0; j < Vector<int>.Count; j++)
            {
                if (vMax[j] > max) max = vMax[j];
            }
        }

        for (; i < span.Length; i++)
        {
            if (span[i] > max) max = span[i];
        }

        return max;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long MaxVectorized(ReadOnlySpan<long> span)
    {
        long max = long.MinValue;
        int i = 0;

        if (Vector.IsHardwareAccelerated && span.Length >= Vector<long>.Count)
        {
            var vMax = new Vector<long>(long.MinValue);
            var vectorCount = span.Length - (span.Length % Vector<long>.Count);

            for (; i < vectorCount; i += Vector<long>.Count)
            {
                vMax = Vector.Max(vMax, new Vector<long>(span.Slice(i)));
            }

            for (int j = 0; j < Vector<long>.Count; j++)
            {
                if (vMax[j] > max) max = vMax[j];
            }
        }

        for (; i < span.Length; i++)
        {
            if (span[i] > max) max = span[i];
        }

        return max;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double MaxVectorized(ReadOnlySpan<double> span)
    {
        double max = double.NegativeInfinity;
        int i = 0;

        ref double ptr = ref MemoryMarshal.GetReference(span);

        if (AdvSimd.Arm64.IsSupported && span.Length >= 8)
        {
            // ARM NEON path (128-bit = 2 doubles per vector)
            // Use 4 accumulators for better instruction-level parallelism
            var vMax0 = Vector128.Create(double.NegativeInfinity);
            var vMax1 = Vector128.Create(double.NegativeInfinity);
            var vMax2 = Vector128.Create(double.NegativeInfinity);
            var vMax3 = Vector128.Create(double.NegativeInfinity);

            int vectorCount = span.Length - (span.Length % 8);

            for (; i < vectorCount; i += 8)
            {
                var v0 = Vector128.LoadUnsafe(ref Unsafe.Add(ref ptr, i));
                var v1 = Vector128.LoadUnsafe(ref Unsafe.Add(ref ptr, i + 2));
                var v2 = Vector128.LoadUnsafe(ref Unsafe.Add(ref ptr, i + 4));
                var v3 = Vector128.LoadUnsafe(ref Unsafe.Add(ref ptr, i + 6));

                vMax0 = AdvSimd.Arm64.Max(vMax0, v0);
                vMax1 = AdvSimd.Arm64.Max(vMax1, v1);
                vMax2 = AdvSimd.Arm64.Max(vMax2, v2);
                vMax3 = AdvSimd.Arm64.Max(vMax3, v3);
            }

            // Combine accumulators
            vMax0 = AdvSimd.Arm64.Max(vMax0, vMax1);
            vMax2 = AdvSimd.Arm64.Max(vMax2, vMax3);
            vMax0 = AdvSimd.Arm64.Max(vMax0, vMax2);

            // Horizontal max (NEON has MaxPairwise for this)
            max = AdvSimd.Arm64.MaxPairwiseScalar(vMax0).ToScalar();
        }
        else if (Avx.IsSupported && span.Length >= 16)
        {
            // x64 AVX path (256-bit = 4 doubles per vector)
            var vMax0 = Vector256.Create(double.NegativeInfinity);
            var vMax1 = Vector256.Create(double.NegativeInfinity);
            var vMax2 = Vector256.Create(double.NegativeInfinity);
            var vMax3 = Vector256.Create(double.NegativeInfinity);

            int vectorCount = span.Length - (span.Length % 16);

            for (; i < vectorCount; i += 16)
            {
                var v0 = Vector256.LoadUnsafe(ref Unsafe.Add(ref ptr, i));
                var v1 = Vector256.LoadUnsafe(ref Unsafe.Add(ref ptr, i + 4));
                var v2 = Vector256.LoadUnsafe(ref Unsafe.Add(ref ptr, i + 8));
                var v3 = Vector256.LoadUnsafe(ref Unsafe.Add(ref ptr, i + 12));

                vMax0 = Avx.Max(vMax0, v0);
                vMax1 = Avx.Max(vMax1, v1);
                vMax2 = Avx.Max(vMax2, v2);
                vMax3 = Avx.Max(vMax3, v3);
            }

            // Combine accumulators
            vMax0 = Avx.Max(vMax0, vMax1);
            vMax2 = Avx.Max(vMax2, vMax3);
            vMax0 = Avx.Max(vMax0, vMax2);

            // Horizontal max
            max = Math.Max(Math.Max(vMax0.GetElement(0), vMax0.GetElement(1)),
                          Math.Max(vMax0.GetElement(2), vMax0.GetElement(3)));
        }
        else if (Vector.IsHardwareAccelerated && span.Length >= Vector<double>.Count)
        {
            // Fallback to portable SIMD
            var vMax = new Vector<double>(double.NegativeInfinity);
            var vectorCount = span.Length - (span.Length % Vector<double>.Count);

            for (; i < vectorCount; i += Vector<double>.Count)
            {
                vMax = Vector.Max(vMax, new Vector<double>(span.Slice(i)));
            }

            for (int j = 0; j < Vector<double>.Count; j++)
            {
                if (vMax[j] > max) max = vMax[j];
            }
        }

        // Scalar remainder
        for (; i < span.Length; i++)
        {
            if (span[i] > max) max = span[i];
        }

        return max;
    }
}
