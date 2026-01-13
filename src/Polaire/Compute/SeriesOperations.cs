// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using System.Runtime.CompilerServices;
using Apache.Arrow;
using Polaire.DataTypes;
using Polaire.Core;
using Polaire.Series;

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

    public static DataFrame.DataFrame ValueCounts(Series series)
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
        var valuesSeries = BuildSeriesFromAnyValues(series.Name, valuesList, series.DataType);
        var countsSeries = Series.FromValues("count", countsList.ToArray());

        return new DataFrame.DataFrame(new[] { valuesSeries, countsSeries });
    }

    private static Series BuildSeriesFromAnyValues(string name, List<AnyValue> values, DataType dtype)
    {
        return dtype switch
        {
            DataType.Int32Type => Series.FromNullable(name, values.Select(v => v.IsNull ? null : (int?)v.AsInt32()).ToArray()),
            DataType.Int64Type => Series.FromNullable(name, values.Select(v => v.IsNull ? null : (long?)v.AsInt64()).ToArray()),
            DataType.Float64Type => Series.FromNullable(name, values.Select(v => v.IsNull ? null : (double?)v.AsFloat64()).ToArray()),
            DataType.StringType => Series.FromValues(name, values.Select(v => v.IsNull ? null : v.AsString()).ToArray()),
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

        Array.Sort(indices, (i, j) =>
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
            _ => TakeGeneric(series, indices)
        };
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
