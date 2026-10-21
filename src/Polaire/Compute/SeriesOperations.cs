// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using System.Runtime.CompilerServices;
using Apache.Arrow;
using Polaire.DataTypes;
using Polaire.Core;


namespace Polaire.Compute;

/// <summary>
/// General operations for Series (cast, fill, sort, etc.).
/// </summary>
public static class SeriesOperations
{
    // ============================================================================
    // Cast
    // ============================================================================

    public static Series Cast(Series series, DataType targetType)
    {
        if (series.DataType == targetType)
            return series;

        return targetType switch
        {
            DataType.Int32Type => CastToInt32(series),
            DataType.Int64Type => CastToInt64(series),
            DataType.Float32Type => CastToFloat32(series),
            DataType.Float64Type => CastToFloat64(series),
            DataType.StringType => CastToString(series),
            DataType.BooleanType => CastToBoolean(series),
            _ => throw new NotSupportedException($"Cast to {targetType} not supported")
        };
    }

    private static Series CastToInt32(Series series)
    {
        var builder = new Int32Array.Builder();
        for (int i = 0; i < series.Length; i++)
        {
            if (series.IsNull(i))
            {
                builder.AppendNull();
            }
            else
            {
                var val = series[i];
                builder.Append((int)val.Cast(DataType.Int32).AsInt32());
            }
        }
        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Int32);
    }

    private static Series CastToInt64(Series series)
    {
        var builder = new Int64Array.Builder();
        for (int i = 0; i < series.Length; i++)
        {
            if (series.IsNull(i))
            {
                builder.AppendNull();
            }
            else
            {
                var val = series[i];
                if (val.TryGetInt64(out var l))
                    builder.Append(l);
                else if (val.TryGetDouble(out var d))
                    builder.Append((long)d);
                else
                    builder.AppendNull();
            }
        }
        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Int64);
    }

    private static Series CastToFloat32(Series series)
    {
        var builder = new FloatArray.Builder();
        for (int i = 0; i < series.Length; i++)
        {
            if (series.IsNull(i))
            {
                builder.AppendNull();
            }
            else
            {
                var val = series[i];
                if (val.TryGetDouble(out var d))
                    builder.Append((float)d);
                else if (val.TryGetInt64(out var l))
                    builder.Append(l);
                else
                    builder.AppendNull();
            }
        }
        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Float32);
    }

    private static Series CastToFloat64(Series series)
    {
        var builder = new DoubleArray.Builder();
        for (int i = 0; i < series.Length; i++)
        {
            if (series.IsNull(i))
            {
                builder.AppendNull();
            }
            else
            {
                var val = series[i];
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

    private static Series CastToString(Series series)
    {
        var builder = new StringArray.Builder();
        for (int i = 0; i < series.Length; i++)
        {
            if (series.IsNull(i))
                builder.AppendNull();
            else
                builder.Append(series[i].ToString());
        }
        return Series.FromArrowArray(series.Name, builder.Build(), DataType.String);
    }

    private static Series CastToBoolean(Series series)
    {
        var builder = new BooleanArray.Builder();
        for (int i = 0; i < series.Length; i++)
        {
            if (series.IsNull(i))
            {
                builder.AppendNull();
            }
            else
            {
                var val = series[i];
                if (val.TryGetInt64(out var l))
                    builder.Append(l != 0);
                else if (val.TryGetDouble(out var d))
                    builder.Append(d != 0);
                else
                    builder.AppendNull();
            }
        }
        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Boolean);
    }

    // ============================================================================
    // FillNull
    // ============================================================================

    public static Series FillNull(Series series, AnyValue fillValue)
    {
        return series.DataType switch
        {
            DataType.Int32Type => FillNullInt32(series, fillValue.TryGetInt64(out var l) ? (int)l : 0),
            DataType.Int64Type => FillNullInt64(series, fillValue.TryGetInt64(out var l) ? l : 0),
            DataType.Float32Type => FillNullFloat32(series, fillValue.TryGetDouble(out var d) ? (float)d : 0f),
            DataType.Float64Type => FillNullFloat64(series, fillValue.TryGetDouble(out var d) ? d : 0.0),
            DataType.StringType => FillNullString(series, fillValue.TryGetString(out var s) ? s : ""),
            DataType.BooleanType => FillNullBoolean(series, fillValue.Kind == AnyValueKind.Boolean && fillValue.AsBoolean()),
            _ => throw new NotSupportedException($"FillNull not supported for {series.DataType}")
        };
    }

    private static Series FillNullInt32(Series series, int fillValue)
    {
        var data = series.Data as ChunkedArray<int>;
        var builder = new Int32Array.Builder();
        for (int i = 0; i < series.Length; i++)
        {
            if (series.IsNull(i))
                builder.Append(fillValue);
            else
                builder.Append(data!.GetValue(i));
        }
        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Int32);
    }

    private static Series FillNullInt64(Series series, long fillValue)
    {
        var data = series.Data as ChunkedArray<long>;
        var builder = new Int64Array.Builder();
        for (int i = 0; i < series.Length; i++)
        {
            if (series.IsNull(i))
                builder.Append(fillValue);
            else
                builder.Append(data!.GetValue(i));
        }
        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Int64);
    }

    private static Series FillNullFloat32(Series series, float fillValue)
    {
        var data = series.Data as ChunkedArray<float>;
        var builder = new FloatArray.Builder();
        for (int i = 0; i < series.Length; i++)
        {
            if (series.IsNull(i))
                builder.Append(fillValue);
            else
                builder.Append(data!.GetValue(i));
        }
        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Float32);
    }

    private static Series FillNullFloat64(Series series, double fillValue)
    {
        var data = series.Data as ChunkedArray<double>;
        var builder = new DoubleArray.Builder();
        for (int i = 0; i < series.Length; i++)
        {
            if (series.IsNull(i))
                builder.Append(fillValue);
            else
                builder.Append(data!.GetValue(i));
        }
        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Float64);
    }

    private static Series FillNullString(Series series, string fillValue)
    {
        var data = series.Data as StringChunkedArray;
        var builder = new StringArray.Builder();
        for (int i = 0; i < series.Length; i++)
        {
            if (series.IsNull(i))
                builder.Append(fillValue);
            else
                builder.Append(data!.GetString(i));
        }
        return Series.FromArrowArray(series.Name, builder.Build(), DataType.String);
    }

    private static Series FillNullBoolean(Series series, bool fillValue)
    {
        var data = series.Data as ChunkedArray<bool>;
        var builder = new BooleanArray.Builder();
        for (int i = 0; i < series.Length; i++)
        {
            if (series.IsNull(i))
                builder.Append(fillValue);
            else
                builder.Append(data!.GetValue(i));
        }
        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Boolean);
    }

    // ============================================================================
    // DropNulls
    // ============================================================================

    public static Series DropNulls(Series series)
    {
        // Get indices of non-null values
        var indices = new List<int>();
        for (int i = 0; i < series.Length; i++)
        {
            if (!series.IsNull(i))
                indices.Add(i);
        }

        return Take(series, indices.ToArray());
    }

    // ============================================================================
    // Unique
    // ============================================================================

    public static Series Unique(Series series)
    {
        var seen = new HashSet<AnyValue>();
        var uniqueIndices = new List<int>();

        for (int i = 0; i < series.Length; i++)
        {
            var val = series[i];
            if (seen.Add(val))
                uniqueIndices.Add(i);
        }

        return Take(series, uniqueIndices.ToArray());
    }

    // ============================================================================
    // ValueCounts
    // ============================================================================

    public static DataFrame ValueCounts(Series series)
    {
        var counts = new Dictionary<AnyValue, int>();

        for (int i = 0; i < series.Length; i++)
        {
            var val = series[i];
            counts.TryGetValue(val, out var count);
            counts[val] = count + 1;
        }

        // Build result series
        var valuesList = new List<AnyValue>();
        var countsList = new List<int>();

        foreach (var kvp in counts.OrderByDescending(x => x.Value))
        {
            valuesList.Add(kvp.Key);
            countsList.Add(kvp.Value);
        }

        // Create series based on original type
        var valuesSeries = FromAnyValues(series.Name, valuesList, series.DataType);
        var countsSeries = Series.FromValues("count", countsList.ToArray());

        return new DataFrame(new[] { valuesSeries, countsSeries });
    }

    /// <summary>
    /// Builds a series of the given type from boxed values. Integer values are accepted for
    /// float types and any integer kind for integer types. Types without a typed builder fall
    /// back to a string series.
    /// </summary>
    internal static Series FromAnyValues(string name, IReadOnlyList<AnyValue> values, DataType dtype)
    {
        return dtype switch
        {
            DataType.Int32Type => Series.FromNullable(name, values.Select(v =>
                v.IsNull ? null : v.TryGetInt64(out var l) ? checked((int?)l) : (int?)v.AsInt32()).ToArray()),
            DataType.Int64Type => Series.FromNullable(name, values.Select(v =>
                v.IsNull ? null : v.TryGetInt64(out var l) ? (long?)l : v.AsInt64()).ToArray()),
            DataType.Float32Type => Series.FromNullable(name, values.Select(v =>
                v.IsNull ? null : v.TryGetDouble(out var d) ? (float?)d : v.AsFloat32()).ToArray()),
            DataType.Float64Type => Series.FromNullable(name, values.Select(v =>
                v.IsNull ? null : v.TryGetDouble(out var d) ? (double?)d : v.AsFloat64()).ToArray()),
            DataType.BooleanType => Series.FromNullable(name, values.Select(v =>
                v.IsNull ? null : (bool?)v.AsBoolean()).ToArray()),
            DataType.StringType => Series.FromValues(name, values.Select(v =>
                v.IsNull ? null : v.TryGetString(out var s) ? s : v.ToString()).ToArray()),
            DataType.DateType => TemporalConversions.DateSeries(name, values.Select(v =>
                v.IsNull ? null : (DateOnly?)v.AsDate()).ToArray()),
            DataType.DateTimeType dt => TemporalConversions.DateTimeSeries(name, values.Select(v =>
                v.IsNull ? null : (DateTime?)v.AsDateTime()).ToArray(), dt.Unit, dt.TimeZone),
            _ => Series.FromValues(name, values.Select(v => v.ToString()).ToArray())
        };
    }

    // ============================================================================
    // Sort
    // ============================================================================

    public static Series Sort(Series series, bool descending = false, bool nullsLast = true)
    {
        var indices = ArgSortInternal(series, descending, nullsLast);
        return Take(series, indices);
    }

    public static Series ArgSort(Series series, bool descending = false, bool nullsLast = true)
    {
        var indices = ArgSortInternal(series, descending, nullsLast);
        return Series.FromValues(series.Name, indices);
    }

    private static int[] ArgSortInternal(Series series, bool descending, bool nullsLast)
    {
        var indices = Enumerable.Range(0, series.Length).ToArray();

        System.Array.Sort(indices, (i, j) =>
        {
            bool iNull = series.IsNull(i);
            bool jNull = series.IsNull(j);

            if (iNull && jNull) return 0;
            if (iNull) return nullsLast ? 1 : -1;
            if (jNull) return nullsLast ? -1 : 1;

            var comparison = series[i].CompareTo(series[j]);
            return descending ? -comparison : comparison;
        });

        return indices;
    }

    // ============================================================================
    // Reverse
    // ============================================================================

    public static Series Reverse(Series series)
    {
        var indices = Enumerable.Range(0, series.Length).Reverse().ToArray();
        return Take(series, indices);
    }

    // ============================================================================
    // Take (index selection)
    // ============================================================================

    public static Series Take(Series series, int[] indices)
    {
        return series.DataType switch
        {
            DataType.Int32Type => TakeInt32(series, indices),
            DataType.Int64Type => TakeInt64(series, indices),
            DataType.Float32Type => TakeFloat32(series, indices),
            DataType.Float64Type => TakeFloat64(series, indices),
            DataType.StringType => TakeString(series, indices),
            DataType.BooleanType => TakeBoolean(series, indices),
            var dtype when CanGather(dtype) =>
                Gather(series.Name, dtype, indices.Select(i => (series, i)), indices.Length),
            _ => TakeGeneric(series, indices)
        };
    }

    // ============================================================================
    // Concat
    // ============================================================================

    /// <summary>
    /// Concatenates series of the same data type into a single-chunk series named after the first.
    /// </summary>
    public static Series Concat(IReadOnlyList<Series> series)
    {
        if (series.Count == 0)
            throw new ArgumentException("At least one series is required", nameof(series));

        var dtype = series[0].DataType;
        foreach (var s in series)
        {
            if (s.DataType != dtype)
                throw new ArgumentException(
                    $"Cannot concatenate series '{s.Name}' of type {s.DataType} with type {dtype}");
        }

        if (!CanGather(dtype))
            throw new NotSupportedException($"Concatenation not supported for {dtype}");

        var rows = series.SelectMany(s => Enumerable.Range(0, s.Length).Select(i => (s, i)));
        return Gather(series[0].Name, dtype, rows, series.Sum(s => s.Length));
    }

    private static bool CanGather(DataType dtype) =>
        dtype is DataType.Int8Type or DataType.Int16Type or DataType.Int32Type or DataType.Int64Type
            or DataType.UInt8Type or DataType.UInt16Type or DataType.UInt32Type or DataType.UInt64Type
            or DataType.Float32Type or DataType.Float64Type or DataType.BooleanType or DataType.StringType
            or DataType.DateType or DataType.DateTimeType;

    /// <summary>
    /// Builds a single-chunk series of type <paramref name="dtype"/> from (source, row) pairs,
    /// preserving the physical representation (including the unit and time zone of DateTime columns).
    /// </summary>
    private static Series Gather(string name, DataType dtype, IEnumerable<(Series Source, int Index)> rows, int count)
    {
        IArrowArray array = dtype switch
        {
            DataType.Int8Type => GatherValues<sbyte, Int8Array.Builder>(rows, new Int8Array.Builder().Reserve(count),
                (b, v) => b.Append(v), b => b.AppendNull()).Build(),
            DataType.Int16Type => GatherValues<short, Int16Array.Builder>(rows, new Int16Array.Builder().Reserve(count),
                (b, v) => b.Append(v), b => b.AppendNull()).Build(),
            DataType.Int32Type => GatherValues<int, Int32Array.Builder>(rows, new Int32Array.Builder().Reserve(count),
                (b, v) => b.Append(v), b => b.AppendNull()).Build(),
            DataType.Int64Type => GatherValues<long, Int64Array.Builder>(rows, new Int64Array.Builder().Reserve(count),
                (b, v) => b.Append(v), b => b.AppendNull()).Build(),
            DataType.UInt8Type => GatherValues<byte, UInt8Array.Builder>(rows, new UInt8Array.Builder().Reserve(count),
                (b, v) => b.Append(v), b => b.AppendNull()).Build(),
            DataType.UInt16Type => GatherValues<ushort, UInt16Array.Builder>(rows, new UInt16Array.Builder().Reserve(count),
                (b, v) => b.Append(v), b => b.AppendNull()).Build(),
            DataType.UInt32Type => GatherValues<uint, UInt32Array.Builder>(rows, new UInt32Array.Builder().Reserve(count),
                (b, v) => b.Append(v), b => b.AppendNull()).Build(),
            DataType.UInt64Type => GatherValues<ulong, UInt64Array.Builder>(rows, new UInt64Array.Builder().Reserve(count),
                (b, v) => b.Append(v), b => b.AppendNull()).Build(),
            DataType.Float32Type => GatherValues<float, FloatArray.Builder>(rows, new FloatArray.Builder().Reserve(count),
                (b, v) => b.Append(v), b => b.AppendNull()).Build(),
            DataType.Float64Type => GatherValues<double, DoubleArray.Builder>(rows, new DoubleArray.Builder().Reserve(count),
                (b, v) => b.Append(v), b => b.AppendNull()).Build(),
            DataType.BooleanType => GatherValues<bool, BooleanArray.Builder>(rows, new BooleanArray.Builder().Reserve(count),
                (b, v) => b.Append(v), b => b.AppendNull()).Build(),
            DataType.StringType => GatherStrings(rows),
            DataType.DateType => TemporalConversions.AsDate32(
                GatherValues<int, Int32Array.Builder>(rows, new Int32Array.Builder().Reserve(count),
                    (b, v) => b.Append(v), b => b.AppendNull()).Build()),
            DataType.DateTimeType dt => TemporalConversions.AsTimestamp(
                GatherValues<long, Int64Array.Builder>(rows, new Int64Array.Builder().Reserve(count),
                    (b, v) => b.Append(v), b => b.AppendNull()).Build(), dt),
            _ => throw new NotSupportedException($"Gather not supported for {dtype}")
        };

        return Series.FromArrowArray(name, array, dtype);
    }

    private static TBuilder GatherValues<T, TBuilder>(
        IEnumerable<(Series Source, int Index)> rows, TBuilder builder,
        Action<TBuilder, T> append, Action<TBuilder> appendNull) where T : struct
    {
        foreach (var (source, index) in rows)
        {
            if (source.IsNull(index))
                appendNull(builder);
            else
                append(builder, ((ChunkedArray<T>)source.Data).GetValue(index));
        }
        return builder;
    }

    private static StringArray GatherStrings(IEnumerable<(Series Source, int Index)> rows)
    {
        var builder = new StringArray.Builder();
        foreach (var (source, index) in rows)
        {
            if (source.IsNull(index))
                builder.AppendNull();
            else
                builder.Append(((StringChunkedArray)source.Data).GetString(index));
        }
        return builder.Build();
    }

    private static Series TakeInt32(Series series, int[] indices)
    {
        var data = series.Data as ChunkedArray<int>;
        var builder = new Int32Array.Builder();
        builder.Reserve(indices.Length);

        foreach (var i in indices)
        {
            if (series.IsNull(i))
                builder.AppendNull();
            else
                builder.Append(data!.GetValue(i));
        }

        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Int32);
    }

    private static Series TakeInt64(Series series, int[] indices)
    {
        var data = series.Data as ChunkedArray<long>;
        var builder = new Int64Array.Builder();
        builder.Reserve(indices.Length);

        foreach (var i in indices)
        {
            if (series.IsNull(i))
                builder.AppendNull();
            else
                builder.Append(data!.GetValue(i));
        }

        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Int64);
    }

    private static Series TakeFloat32(Series series, int[] indices)
    {
        var data = series.Data as ChunkedArray<float>;
        var builder = new FloatArray.Builder();
        builder.Reserve(indices.Length);

        foreach (var i in indices)
        {
            if (series.IsNull(i))
                builder.AppendNull();
            else
                builder.Append(data!.GetValue(i));
        }

        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Float32);
    }

    private static Series TakeFloat64(Series series, int[] indices)
    {
        var data = series.Data as ChunkedArray<double>;
        var builder = new DoubleArray.Builder();
        builder.Reserve(indices.Length);

        foreach (var i in indices)
        {
            if (series.IsNull(i))
                builder.AppendNull();
            else
                builder.Append(data!.GetValue(i));
        }

        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Float64);
    }

    private static Series TakeString(Series series, int[] indices)
    {
        var data = series.Data as StringChunkedArray;
        var builder = new StringArray.Builder();

        foreach (var i in indices)
        {
            if (series.IsNull(i))
                builder.AppendNull();
            else
                builder.Append(data!.GetString(i));
        }

        return Series.FromArrowArray(series.Name, builder.Build(), DataType.String);
    }

    private static Series TakeBoolean(Series series, int[] indices)
    {
        var data = series.Data as ChunkedArray<bool>;
        var builder = new BooleanArray.Builder();

        foreach (var i in indices)
        {
            if (series.IsNull(i))
                builder.AppendNull();
            else
                builder.Append(data!.GetValue(i));
        }

        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Boolean);
    }

    private static Series TakeGeneric(Series series, int[] indices)
    {
        // Fallback for unsupported types
        var values = indices.Select(i => series[i].ToString()).ToArray();
        return Series.FromValues(series.Name, values);
    }

    // ============================================================================
    // Filter (boolean mask)
    // ============================================================================

    public static Series Filter(Series series, Series mask)
    {
        if (mask.DataType is not DataType.BooleanType)
            throw new ArgumentException("Mask must be boolean series");
        if (series.Length != mask.Length)
            throw new ArgumentException("Series and mask must have same length");

        var maskData = mask.Data as ChunkedArray<bool>;
        var indices = new List<int>();

        for (int i = 0; i < series.Length; i++)
        {
            if (!mask.IsNull(i) && maskData!.GetValue(i))
                indices.Add(i);
        }

        return Take(series, indices.ToArray());
    }
}
