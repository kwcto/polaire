// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using System.Collections;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Apache.Arrow;
using Apache.Arrow.Types;
using Polaire.Core;
using Polaire.DataTypes;
using Polaire.Compute;

namespace Polaire;

/// <summary>
/// A named, typed column of data. The primary one-dimensional data structure in Polaire.
/// Series is the type-erased wrapper around ChunkedArray{T}.
/// </summary>
public sealed class Series : IEnumerable<AnyValue>
{
    private readonly string _name;
    private readonly IChunkedArray _data;

    private Series(string name, IChunkedArray data)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _data = data ?? throw new ArgumentNullException(nameof(data));
    }

    // ============================================================================
    // Properties
    // ============================================================================

    /// <summary>Gets the name of this series.</summary>
    public string Name => _name;

    /// <summary>Gets the data type of this series.</summary>
    public DataType DataType => _data.DataType;

    /// <summary>Gets the number of elements in this series.</summary>
    public int Length => _data.Length;

    /// <summary>Gets the number of null values.</summary>
    public int NullCount => _data.NullCount;

    /// <summary>Gets whether this series has any null values.</summary>
    public bool HasNulls => _data.HasNulls;

    /// <summary>Gets the underlying chunked array.</summary>
    internal IChunkedArray Data => _data;

    /// <summary>Gets a value at the specified index.</summary>
    public AnyValue this[int index] => GetValue(index);

    // ============================================================================
    // Factory Methods - Create from arrays
    // ============================================================================

    public static Series FromValues(string name, sbyte[] values) =>
        FromArrowArray(name, new Int8Array.Builder().AppendRange(values).Build(), DataType.Int8);

    public static Series FromValues(string name, short[] values) =>
        FromArrowArray(name, new Int16Array.Builder().AppendRange(values).Build(), DataType.Int16);

    public static Series FromValues(string name, int[] values) =>
        FromArrowArray(name, new Int32Array.Builder().AppendRange(values).Build(), DataType.Int32);

    public static Series FromValues(string name, long[] values) =>
        FromArrowArray(name, new Int64Array.Builder().AppendRange(values).Build(), DataType.Int64);

    public static Series FromValues(string name, byte[] values) =>
        FromArrowArray(name, new UInt8Array.Builder().AppendRange(values).Build(), DataType.UInt8);

    public static Series FromValues(string name, ushort[] values) =>
        FromArrowArray(name, new UInt16Array.Builder().AppendRange(values).Build(), DataType.UInt16);

    public static Series FromValues(string name, uint[] values) =>
        FromArrowArray(name, new UInt32Array.Builder().AppendRange(values).Build(), DataType.UInt32);

    public static Series FromValues(string name, ulong[] values) =>
        FromArrowArray(name, new UInt64Array.Builder().AppendRange(values).Build(), DataType.UInt64);

    public static Series FromValues(string name, float[] values) =>
        FromArrowArray(name, new FloatArray.Builder().AppendRange(values).Build(), DataType.Float32);

    public static Series FromValues(string name, double[] values) =>
        FromArrowArray(name, new DoubleArray.Builder().AppendRange(values).Build(), DataType.Float64);

    public static Series FromValues(string name, bool[] values)
    {
        var builder = new BooleanArray.Builder();
        foreach (var v in values) builder.Append(v);
        return FromArrowArray(name, builder.Build(), DataType.Boolean);
    }

    public static Series FromValues(string name, string?[] values)
    {
        var builder = new StringArray.Builder();
        foreach (var v in values)
        {
            if (v is null) builder.AppendNull();
            else builder.Append(v);
        }
        return FromArrowArray(name, builder.Build(), DataType.String);
    }

    public static Series FromValues(string name, DateOnly[] values)
    {
        var builder = new Date32Array.Builder();
        foreach (var v in values) builder.Append(v.ToDateTime(TimeOnly.MinValue));
        return FromArrowArray(name, builder.Build(), DataType.Date);
    }

    public static Series FromValues(string name, DateTime[] values)
    {
        var builder = new TimestampArray.Builder(Apache.Arrow.Types.TimeUnit.Nanosecond);
        foreach (var v in values) builder.Append(new DateTimeOffset(v, TimeSpan.Zero));
        return FromArrowArray(name, builder.Build(), DataType.DateTime(DataTypes.TimeUnit.Nanoseconds));
    }

    /// <summary>Creates a series from nullable values.</summary>
    public static Series FromNullable<T>(string name, T?[] values) where T : struct
    {
        return typeof(T).Name switch
        {
            nameof(Int32) => FromNullableInt32(name, values as int?[]),
            nameof(Int64) => FromNullableInt64(name, values as long?[]),
            nameof(Double) => FromNullableDouble(name, values as double?[]),
            nameof(Single) => FromNullableFloat(name, values as float?[]),
            nameof(Boolean) => FromNullableBool(name, values as bool?[]),
            nameof(DateOnly) => FromNullableDateOnly(name, values as DateOnly?[]),
            nameof(DateTime) => FromNullableDateTime(name, values as DateTime?[]),
            _ => throw new NotSupportedException($"Nullable {typeof(T).Name} not supported")
        } ?? throw new InvalidOperationException();
    }

    private static Series FromNullableInt32(string name, int?[]? values)
    {
        if (values is null) throw new ArgumentNullException(nameof(values));
        var builder = new Int32Array.Builder();
        foreach (var v in values)
        {
            if (v.HasValue) builder.Append(v.Value);
            else builder.AppendNull();
        }
        return FromArrowArray(name, builder.Build(), DataType.Int32);
    }

    private static Series FromNullableInt64(string name, long?[]? values)
    {
        if (values is null) throw new ArgumentNullException(nameof(values));
        var builder = new Int64Array.Builder();
        foreach (var v in values)
        {
            if (v.HasValue) builder.Append(v.Value);
            else builder.AppendNull();
        }
        return FromArrowArray(name, builder.Build(), DataType.Int64);
    }

    private static Series FromNullableDouble(string name, double?[]? values)
    {
        if (values is null) throw new ArgumentNullException(nameof(values));
        var builder = new DoubleArray.Builder();
        foreach (var v in values)
        {
            if (v.HasValue) builder.Append(v.Value);
            else builder.AppendNull();
        }
        return FromArrowArray(name, builder.Build(), DataType.Float64);
    }

    private static Series FromNullableFloat(string name, float?[]? values)
    {
        if (values is null) throw new ArgumentNullException(nameof(values));
        var builder = new FloatArray.Builder();
        foreach (var v in values)
        {
            if (v.HasValue) builder.Append(v.Value);
            else builder.AppendNull();
        }
        return FromArrowArray(name, builder.Build(), DataType.Float32);
    }

    private static Series FromNullableBool(string name, bool?[]? values)
    {
        if (values is null) throw new ArgumentNullException(nameof(values));
        var builder = new BooleanArray.Builder();
        foreach (var v in values)
        {
            if (v.HasValue) builder.Append(v.Value);
            else builder.AppendNull();
        }
        return FromArrowArray(name, builder.Build(), DataType.Boolean);
    }

    private static Series FromNullableDateOnly(string name, DateOnly?[]? values)
    {
        if (values is null) throw new ArgumentNullException(nameof(values));
        var builder = new Date32Array.Builder();
        foreach (var v in values)
        {
            if (v.HasValue) builder.Append(v.Value.ToDateTime(TimeOnly.MinValue));
            else builder.AppendNull();
        }
        return FromArrowArray(name, builder.Build(), DataType.Date);
    }

    private static Series FromNullableDateTime(string name, DateTime?[]? values)
    {
        if (values is null) throw new ArgumentNullException(nameof(values));
        var builder = new TimestampArray.Builder(Apache.Arrow.Types.TimeUnit.Nanosecond);
        foreach (var v in values)
        {
            if (v.HasValue) builder.Append(new DateTimeOffset(v.Value, TimeSpan.Zero));
            else builder.AppendNull();
        }
        return FromArrowArray(name, builder.Build(), DataType.DateTime(DataTypes.TimeUnit.Nanoseconds));
    }

    /// <summary>Creates a series from an Arrow array.</summary>
    public static Series FromArrowArray(string name, IArrowArray array, DataType dataType)
    {
        IChunkedArray chunked = dataType switch
        {
            DataType.Int8Type => new ChunkedArray<sbyte>(array, dataType),
            DataType.Int16Type => new ChunkedArray<short>(array, dataType),
            DataType.Int32Type => new ChunkedArray<int>(array, dataType),
            DataType.Int64Type => new ChunkedArray<long>(array, dataType),
            DataType.UInt8Type => new ChunkedArray<byte>(array, dataType),
            DataType.UInt16Type => new ChunkedArray<ushort>(array, dataType),
            DataType.UInt32Type => new ChunkedArray<uint>(array, dataType),
            DataType.UInt64Type => new ChunkedArray<ulong>(array, dataType),
            DataType.Float32Type => new ChunkedArray<float>(array, dataType),
            DataType.Float64Type => new ChunkedArray<double>(array, dataType),
            DataType.BooleanType => new ChunkedArray<bool>(array, dataType),
            DataType.DateType => new ChunkedArray<int>(array, dataType),
            DataType.DateTimeType => new ChunkedArray<long>(array, dataType),
            DataType.ListType => new ListChunkedArray(array, dataType),
            DataType.StructType => new StructChunkedArray(array, dataType),
            _ => new StringChunkedArray(array, dataType)
        };
        return new Series(name, chunked);
    }

    /// <summary>Creates a list series from arrays of values.</summary>
    public static Series FromLists(string name, int[][] values)
    {
        return FromListsGeneric<int, Int32Array.Builder, Int32Array>(
            name, values,
            () => new Int32Array.Builder(),
            (b, v) => b.Append(v),
            b => b.Build(),
            DataType.Int32);
    }

    /// <summary>Creates a list series from arrays of values.</summary>
    public static Series FromLists(string name, double[][] values)
    {
        return FromListsGeneric<double, DoubleArray.Builder, DoubleArray>(
            name, values,
            () => new DoubleArray.Builder(),
            (b, v) => b.Append(v),
            b => b.Build(),
            DataType.Float64);
    }

    /// <summary>Creates a list series from arrays of strings.</summary>
    public static Series FromLists(string name, string[][] values)
    {
        var listBuilder = new ListArray.Builder(new StringType());
        var valueBuilder = listBuilder.ValueBuilder as StringArray.Builder;

        foreach (var list in values)
        {
            if (list == null)
            {
                listBuilder.AppendNull();
            }
            else
            {
                listBuilder.Append();
                foreach (var item in list)
                {
                    if (item == null)
                        valueBuilder!.AppendNull();
                    else
                        valueBuilder!.Append(item);
                }
            }
        }

        var array = listBuilder.Build();
        return FromArrowArray(name, array, DataType.List(DataType.String));
    }

    private static Series FromListsGeneric<T, TBuilder, TArray>(
        string name,
        T[][] values,
        Func<TBuilder> createBuilder,
        Action<TBuilder, T> appendValue,
        Func<TBuilder, TArray> buildArray,
        DataType innerType)
        where T : struct
        where TBuilder : class
        where TArray : IArrowArray
    {
        // Build lists using Arrow's ListArray.Builder
        var arrowType = innerType switch
        {
            DataType.Int32Type => new Int32Type() as IArrowType,
            DataType.Int64Type => new Int64Type(),
            DataType.Float64Type => new DoubleType(),
            DataType.Float32Type => new FloatType(),
            _ => throw new NotSupportedException()
        };

        var listBuilder = new ListArray.Builder(arrowType);

        foreach (var list in values)
        {
            if (list == null)
            {
                listBuilder.AppendNull();
            }
            else
            {
                listBuilder.Append();
                foreach (var item in list)
                {
                    switch (listBuilder.ValueBuilder)
                    {
                        case Int32Array.Builder b:
                            b.Append(Convert.ToInt32(item));
                            break;
                        case Int64Array.Builder b:
                            b.Append(Convert.ToInt64(item));
                            break;
                        case DoubleArray.Builder b:
                            b.Append(Convert.ToDouble(item));
                            break;
                        case FloatArray.Builder b:
                            b.Append(Convert.ToSingle(item));
                            break;
                    }
                }
            }
        }

        var array = listBuilder.Build();
        return FromArrowArray(name, array, DataType.List(innerType));
    }

    /// <summary>Creates a struct series from a dictionary of column arrays.</summary>
    public static Series FromStruct(string name, Dictionary<string, Series> fields)
    {
        if (fields.Count == 0)
            throw new ArgumentException("Struct must have at least one field");

        var length = fields.First().Value.Length;
        if (!fields.All(f => f.Value.Length == length))
            throw new ArgumentException("All fields must have the same length");

        // Build the struct type
        var structFields = fields.Select(f =>
            new DataTypes.Field(f.Key, f.Value.DataType, f.Value.HasNulls)).ToArray();
        var structType = DataType.Struct(structFields);

        // Build Arrow arrays for each field
        var fieldArrays = new List<IArrowArray>();
        foreach (var field in fields.Values)
        {
            fieldArrays.Add(GetArrowArrayFromSeries(field));
        }

        // Create Arrow StructType
        var arrowFields = fields.Select(f =>
            new Apache.Arrow.Field(f.Key, GetArrowType(f.Value.DataType), f.Value.HasNulls)).ToList();
        var arrowStructType = new Apache.Arrow.Types.StructType(arrowFields);

        // Build StructArray
        var structArray = new StructArray(arrowStructType, length, fieldArrays, ArrowBuffer.Empty);
        return FromArrowArray(name, structArray, structType);
    }

    private static IArrowArray GetArrowArrayFromSeries(Series series)
    {
        // Extract the Arrow array from the Series
        var data = series.Data;
        return data switch
        {
            ChunkedArray<int> arr => arr.GetChunk(0),
            ChunkedArray<long> arr => arr.GetChunk(0),
            ChunkedArray<double> arr => arr.GetChunk(0),
            ChunkedArray<float> arr => arr.GetChunk(0),
            ChunkedArray<bool> arr => arr.GetChunk(0),
            StringChunkedArray arr => GetStringArrayChunk(arr),
            _ => throw new NotSupportedException($"Cannot extract Arrow array from {data.DataType}")
        };
    }

    private static IArrowArray GetStringArrayChunk(StringChunkedArray arr)
    {
        var builder = new StringArray.Builder();
        for (int i = 0; i < arr.Length; i++)
        {
            if (arr.IsNull(i))
                builder.AppendNull();
            else
                builder.Append(arr.GetString(i) ?? "");
        }
        return builder.Build();
    }

    private static Apache.Arrow.Types.IArrowType GetArrowType(DataType dataType)
    {
        return dataType switch
        {
            DataType.Int8Type => new Int8Type(),
            DataType.Int16Type => new Int16Type(),
            DataType.Int32Type => new Int32Type(),
            DataType.Int64Type => new Int64Type(),
            DataType.UInt8Type => new UInt8Type(),
            DataType.UInt16Type => new UInt16Type(),
            DataType.UInt32Type => new UInt32Type(),
            DataType.UInt64Type => new UInt64Type(),
            DataType.Float32Type => new FloatType(),
            DataType.Float64Type => new DoubleType(),
            DataType.BooleanType => new BooleanType(),
            DataType.StringType => new StringType(),
            DataType.DateType => new Date32Type(),
            _ => new StringType() // Fallback
        };
    }

    // ============================================================================
    // Value Access
    // ============================================================================

    public AnyValue GetValue(int index)
    {
        if (_data.IsNull(index))
            return AnyValue.Null;

        return _data.DataType switch
        {
            DataType.Int8Type => AnyValue.From(((ChunkedArray<sbyte>)_data).GetValue(index)),
            DataType.Int16Type => AnyValue.From(((ChunkedArray<short>)_data).GetValue(index)),
            DataType.Int32Type => AnyValue.From(((ChunkedArray<int>)_data).GetValue(index)),
            DataType.Int64Type => AnyValue.From(((ChunkedArray<long>)_data).GetValue(index)),
            DataType.UInt8Type => AnyValue.From(((ChunkedArray<byte>)_data).GetValue(index)),
            DataType.UInt16Type => AnyValue.From(((ChunkedArray<ushort>)_data).GetValue(index)),
            DataType.UInt32Type => AnyValue.From(((ChunkedArray<uint>)_data).GetValue(index)),
            DataType.UInt64Type => AnyValue.From(((ChunkedArray<ulong>)_data).GetValue(index)),
            DataType.Float32Type => AnyValue.From(((ChunkedArray<float>)_data).GetValue(index)),
            DataType.Float64Type => AnyValue.From(((ChunkedArray<double>)_data).GetValue(index)),
            DataType.BooleanType => AnyValue.From(((ChunkedArray<bool>)_data).GetValue(index)),
            DataType.StringType => AnyValue.From(((StringChunkedArray)_data).GetString(index)),
            DataType.DateType => AnyValue.From(DateOnly.FromDayNumber(((ChunkedArray<int>)_data).GetValue(index))),
            _ => AnyValue.Null
        };
    }

    public bool IsNull(int index) => _data.IsNull(index);

    // ============================================================================
    // Transformations
    // ============================================================================

    /// <summary>Returns a new series with a different name.</summary>
    public Series Rename(string newName) => new(newName, _data);

    /// <summary>Returns a slice of this series.</summary>
    public Series Slice(int offset, int length)
    {
        // Create sliced view
        return _data.DataType switch
        {
            DataType.Int8Type => new Series(_name, ((ChunkedArray<sbyte>)_data).Slice(offset, length)),
            DataType.Int16Type => new Series(_name, ((ChunkedArray<short>)_data).Slice(offset, length)),
            DataType.Int32Type => new Series(_name, ((ChunkedArray<int>)_data).Slice(offset, length)),
            DataType.Int64Type => new Series(_name, ((ChunkedArray<long>)_data).Slice(offset, length)),
            DataType.UInt8Type => new Series(_name, ((ChunkedArray<byte>)_data).Slice(offset, length)),
            DataType.UInt16Type => new Series(_name, ((ChunkedArray<ushort>)_data).Slice(offset, length)),
            DataType.UInt32Type => new Series(_name, ((ChunkedArray<uint>)_data).Slice(offset, length)),
            DataType.UInt64Type => new Series(_name, ((ChunkedArray<ulong>)_data).Slice(offset, length)),
            DataType.Float32Type => new Series(_name, ((ChunkedArray<float>)_data).Slice(offset, length)),
            DataType.Float64Type => new Series(_name, ((ChunkedArray<double>)_data).Slice(offset, length)),
            DataType.BooleanType => new Series(_name, ((ChunkedArray<bool>)_data).Slice(offset, length)),
            DataType.StringType => new Series(_name, ((StringChunkedArray)_data).Slice(offset, length)),
            DataType.DateType => new Series(_name, ((ChunkedArray<int>)_data).Slice(offset, length)),
            DataType.DateTimeType => new Series(_name, ((ChunkedArray<long>)_data).Slice(offset, length)),
            _ => throw new NotSupportedException($"Slice not supported for {_data.DataType}")
        };
    }

    /// <summary>Gets the first n elements.</summary>
    public Series Head(int n = 5) => Slice(0, Math.Min(n, Length));

    /// <summary>Gets the last n elements.</summary>
    public Series Tail(int n = 5) => Slice(Math.Max(0, Length - n), Math.Min(n, Length));

    /// <summary>Cast to a different data type.</summary>
    public Series Cast(DataType targetType)
    {
        return SeriesOperations.Cast(this, targetType);
    }

    /// <summary>Fill null values with a constant.</summary>
    public Series FillNull(AnyValue value)
    {
        return SeriesOperations.FillNull(this, value);
    }

    /// <summary>Drop null values.</summary>
    public Series DropNulls()
    {
        return SeriesOperations.DropNulls(this);
    }

    /// <summary>Returns unique values.</summary>
    public Series Unique()
    {
        return SeriesOperations.Unique(this);
    }

    /// <summary>Returns value counts as a DataFrame.</summary>
    public DataFrame ValueCounts()
    {
        return SeriesOperations.ValueCounts(this);
    }

    /// <summary>Sort this series.</summary>
    public Series Sort(bool descending = false, bool nullsLast = true)
    {
        return SeriesOperations.Sort(this, descending, nullsLast);
    }

    /// <summary>Returns indices that would sort this series.</summary>
    public Series ArgSort(bool descending = false, bool nullsLast = true)
    {
        return SeriesOperations.ArgSort(this, descending, nullsLast);
    }

    /// <summary>Reverse this series.</summary>
    public Series Reverse()
    {
        return SeriesOperations.Reverse(this);
    }

    // ============================================================================
    // Aggregations
    // ============================================================================

    public AnyValue Sum() => SeriesAggregations.Sum(this);
    public AnyValue Min() => SeriesAggregations.Min(this);
    public AnyValue Max() => SeriesAggregations.Max(this);
    public AnyValue Mean() => SeriesAggregations.Mean(this);
    public AnyValue Median() => SeriesAggregations.Median(this);
    public AnyValue Std() => SeriesAggregations.Std(this);
    public AnyValue Var() => SeriesAggregations.Var(this);
    public int Count() => Length - NullCount;
    public AnyValue First() => Length > 0 ? this[0] : AnyValue.Null;
    public AnyValue Last() => Length > 0 ? this[Length - 1] : AnyValue.Null;
    public AnyValue Product() => SeriesAggregations.Product(this);
    public AnyValue Quantile(double q, string interpolation = "linear") =>
        SeriesAggregations.Quantile(this, q, interpolation);

    // Cumulative aggregations
    public Series CumSum() => SeriesAggregations.CumSum(this);
    public Series CumProd() => SeriesAggregations.CumProd(this);
    public Series CumMin() => SeriesAggregations.CumMin(this);
    public Series CumMax() => SeriesAggregations.CumMax(this);

    // Transformations returning Series
    public Series Shift(int periods = 1) => SeriesAggregations.Shift(this, periods);
    public Series Diff(int n = 1) => SeriesAggregations.Diff(this, n);
    public Series PctChange(int n = 1) => SeriesAggregations.PctChange(this, n);
    public Series Clip(double? lower, double? upper) => SeriesAggregations.Clip(this, lower, upper);
    public Series Abs() => SeriesAggregations.Abs(this);

    // Math functions
    public Series Sqrt() => SeriesAggregations.Sqrt(this);
    public Series Log() => SeriesAggregations.Log(this);
    public Series Log10() => SeriesAggregations.Log10(this);
    public Series Exp() => SeriesAggregations.Exp(this);
    public Series Sin() => SeriesAggregations.Sin(this);
    public Series Cos() => SeriesAggregations.Cos(this);
    public Series Tan() => SeriesAggregations.Tan(this);
    public Series Floor() => SeriesAggregations.Floor(this);
    public Series Ceil() => SeriesAggregations.Ceil(this);
    public Series Round() => SeriesAggregations.Round(this);
    public Series Round(int decimals) => SeriesAggregations.Round(this, decimals);
    public Series Sign() => SeriesAggregations.Sign(this);

    // Rolling window operations
    public Series RollingSum(int windowSize, int minPeriods = 1, bool center = false) =>
        RollingOperations.RollingSum(this, windowSize, minPeriods, center);
    public Series RollingMean(int windowSize, int minPeriods = 1, bool center = false) =>
        RollingOperations.RollingMean(this, windowSize, minPeriods, center);
    public Series RollingMin(int windowSize, int minPeriods = 1, bool center = false) =>
        RollingOperations.RollingMin(this, windowSize, minPeriods, center);
    public Series RollingMax(int windowSize, int minPeriods = 1, bool center = false) =>
        RollingOperations.RollingMax(this, windowSize, minPeriods, center);
    public Series RollingStd(int windowSize, int minPeriods = 1, int ddof = 1, bool center = false) =>
        RollingOperations.RollingStd(this, windowSize, minPeriods, ddof, center);
    public Series RollingVar(int windowSize, int minPeriods = 1, int ddof = 1, bool center = false) =>
        RollingOperations.RollingVar(this, windowSize, minPeriods, ddof, center);
    public Series RollingMedian(int windowSize, int minPeriods = 1, bool center = false) =>
        RollingOperations.RollingMedian(this, windowSize, minPeriods, center);
    public Series RollingQuantile(double quantile, int windowSize, int minPeriods = 1, string interpolation = "linear", bool center = false) =>
        RollingOperations.RollingQuantile(this, quantile, windowSize, minPeriods, interpolation, center);
    public Series RollingApply(int windowSize, Func<double[], double?> func, int minPeriods = 1, bool center = false) =>
        RollingOperations.RollingApply(this, windowSize, func, minPeriods, center);

    // Expanding window operations
    public Series ExpandingSum(int minPeriods = 1) => RollingOperations.ExpandingSum(this, minPeriods);
    public Series ExpandingMean(int minPeriods = 1) => RollingOperations.ExpandingMean(this, minPeriods);
    public Series ExpandingMin(int minPeriods = 1) => RollingOperations.ExpandingMin(this, minPeriods);
    public Series ExpandingMax(int minPeriods = 1) => RollingOperations.ExpandingMax(this, minPeriods);
    public Series ExpandingStd(int minPeriods = 1, int ddof = 1) => RollingOperations.ExpandingStd(this, minPeriods, ddof);

    // Window operations (rank, row_number, lead/lag, etc.)
    public Series RowNumber() => WindowOperations.RowNumber(this);
    public Series Rank(string method = "average", bool descending = false) => WindowOperations.Rank(this, method, descending);
    public Series DenseRank(bool descending = false) => WindowOperations.DenseRank(this, descending);
    public Series OrdinalRank(bool descending = false) => WindowOperations.OrdinalRank(this, descending);
    public Series PercentRank() => WindowOperations.PercentRank(this);
    public Series Lead(int n = 1, AnyValue? defaultValue = null) => WindowOperations.Lead(this, n, defaultValue);
    public Series Lag(int n = 1, AnyValue? defaultValue = null) => WindowOperations.Lag(this, n, defaultValue);
    public Series FirstValue() => WindowOperations.FirstValue(this);
    public Series LastValue() => WindowOperations.LastValue(this);
    public Series NthValue(int n) => WindowOperations.NthValue(this, n);
    public Series CumCount() => WindowOperations.CumCount(this);
    public Series FillForward() => WindowOperations.FillForward(this);
    public Series FillBackward() => WindowOperations.FillBackward(this);
    public Series FillInterpolate() => WindowOperations.FillInterpolate(this);

    // Exponentially weighted moving (EWM) operations
    public Series EwmMean(double alpha, bool adjust = true, bool ignoreNulls = true, int minPeriods = 1) =>
        EwmOperations.EwmMean(this, alpha, adjust, ignoreNulls, minPeriods);
    public Series EwmMeanSpan(double span, bool adjust = true, bool ignoreNulls = true, int minPeriods = 1) =>
        EwmOperations.EwmMeanSpan(this, span, adjust, ignoreNulls, minPeriods);
    public Series EwmMeanHalflife(double halflife, bool adjust = true, bool ignoreNulls = true, int minPeriods = 1) =>
        EwmOperations.EwmMeanHalflife(this, halflife, adjust, ignoreNulls, minPeriods);
    public Series EwmMeanCom(double com, bool adjust = true, bool ignoreNulls = true, int minPeriods = 1) =>
        EwmOperations.EwmMeanCom(this, com, adjust, ignoreNulls, minPeriods);

    public Series EwmVar(double alpha, bool adjust = true, bool ignoreNulls = true, int minPeriods = 2, bool bias = false) =>
        EwmOperations.EwmVar(this, alpha, adjust, ignoreNulls, minPeriods, bias);
    public Series EwmVarSpan(double span, bool adjust = true, bool ignoreNulls = true, int minPeriods = 2, bool bias = false) =>
        EwmOperations.EwmVarSpan(this, span, adjust, ignoreNulls, minPeriods, bias);

    public Series EwmStd(double alpha, bool adjust = true, bool ignoreNulls = true, int minPeriods = 2, bool bias = false) =>
        EwmOperations.EwmStd(this, alpha, adjust, ignoreNulls, minPeriods, bias);
    public Series EwmStdSpan(double span, bool adjust = true, bool ignoreNulls = true, int minPeriods = 2, bool bias = false) =>
        EwmOperations.EwmStdSpan(this, span, adjust, ignoreNulls, minPeriods, bias);

    public Series EwmSum(double alpha, bool ignoreNulls = true, int minPeriods = 1) =>
        EwmOperations.EwmSum(this, alpha, ignoreNulls, minPeriods);

    // ============================================================================
    // Arithmetic Operations
    // ============================================================================

    public static Series operator +(Series left, Series right) => SeriesArithmetic.Add(left, right);
    public static Series operator -(Series left, Series right) => SeriesArithmetic.Subtract(left, right);
    public static Series operator *(Series left, Series right) => SeriesArithmetic.Multiply(left, right);
    public static Series operator /(Series left, Series right) => SeriesArithmetic.Divide(left, right);
    public static Series operator %(Series left, Series right) => SeriesArithmetic.Modulo(left, right);

    public static Series operator +(Series left, double right) => SeriesArithmetic.AddScalar(left, right);
    public static Series operator -(Series left, double right) => SeriesArithmetic.SubtractScalar(left, right);
    public static Series operator *(Series left, double right) => SeriesArithmetic.MultiplyScalar(left, right);
    public static Series operator /(Series left, double right) => SeriesArithmetic.DivideScalar(left, right);

    public static Series operator -(Series series) => SeriesArithmetic.Negate(series);

    // ============================================================================
    // Comparison Operations
    // ============================================================================

    public Series Eq(Series other) => SeriesComparison.Equal(this, other);
    public Series Ne(Series other) => SeriesComparison.NotEqual(this, other);
    public Series Lt(Series other) => SeriesComparison.LessThan(this, other);
    public Series Le(Series other) => SeriesComparison.LessThanOrEqual(this, other);
    public Series Gt(Series other) => SeriesComparison.GreaterThan(this, other);
    public Series Ge(Series other) => SeriesComparison.GreaterThanOrEqual(this, other);

    public Series Eq(AnyValue value) => SeriesComparison.EqualScalar(this, value);
    public Series Ne(AnyValue value) => SeriesComparison.NotEqualScalar(this, value);
    public Series Lt(AnyValue value) => SeriesComparison.LessThanScalar(this, value);
    public Series Le(AnyValue value) => SeriesComparison.LessThanOrEqualScalar(this, value);
    public Series Gt(AnyValue value) => SeriesComparison.GreaterThanScalar(this, value);
    public Series Ge(AnyValue value) => SeriesComparison.GreaterThanOrEqualScalar(this, value);

    public Series IsNull() => SeriesComparison.IsNull(this);
    public Series IsNotNull() => SeriesComparison.IsNotNull(this);
    public Series IsNaN() => SeriesComparison.IsNaN(this);
    public Series IsNotNaN() => SeriesComparison.IsNotNaN(this);
    public Series IsIn(params AnyValue[] values) => SeriesComparison.IsIn(this, values);
    public Series Between(AnyValue lower, AnyValue upper, bool includeBounds = true) =>
        SeriesComparison.Between(this, lower, upper, includeBounds);

    // ============================================================================
    // Boolean Operations
    // ============================================================================

    public Series And(Series other) => SeriesBoolean.And(this, other);
    public Series Or(Series other) => SeriesBoolean.Or(this, other);
    public Series Xor(Series other) => SeriesBoolean.Xor(this, other);
    public Series Not() => SeriesBoolean.Not(this);

    public static Series operator &(Series left, Series right) => left.And(right);
    public static Series operator |(Series left, Series right) => left.Or(right);
    public static Series operator ^(Series left, Series right) => left.Xor(right);
    public static Series operator !(Series series) => series.Not();

    public bool All() => SeriesBoolean.All(this);
    public bool Any() => SeriesBoolean.Any(this);

    // ============================================================================
    // String Operations (accessed via .Str namespace)
    // ============================================================================

    private StringOperations? _str;
    public StringOperations Str => _str ??= new StringOperations(this);

    // ============================================================================
    // Temporal Operations (accessed via .Dt namespace)
    // ============================================================================

    private DateTimeOperations? _dt;
    public DateTimeOperations Dt => _dt ??= new DateTimeOperations(this);

    // ============================================================================
    // List Operations (accessed via .List namespace)
    // ============================================================================

    private ListOperations? _list;
    public ListOperations List => _list ??= new ListOperations(this);

    // ============================================================================
    // Struct Operations (accessed via .Struct namespace)
    // ============================================================================

    private StructOperations? _struct;
    public StructOperations Struct => _struct ??= new StructOperations(this);

    // ============================================================================
    // Display
    // ============================================================================

    public override string ToString()
    {
        return ToString(10);
    }

    public string ToString(int maxRows)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Series: '{_name}'");
        sb.AppendLine($"Type: {_data.DataType}");
        sb.AppendLine($"Length: {_data.Length}");
        sb.AppendLine("---");

        int displayRows = Math.Min(maxRows, _data.Length);
        for (int i = 0; i < displayRows; i++)
        {
            var value = GetValue(i);
            sb.AppendLine($"{i}: {value}");
        }

        if (_data.Length > maxRows)
        {
            sb.AppendLine($"... ({_data.Length - maxRows} more rows)");
        }

        return sb.ToString();
    }

    // ============================================================================
    // IEnumerable
    // ============================================================================

    public IEnumerator<AnyValue> GetEnumerator()
    {
        for (int i = 0; i < _data.Length; i++)
        {
            yield return GetValue(i);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    // ============================================================================
    // Typed Access
    // ============================================================================

    /// <summary>Gets the underlying typed array if the type matches.</summary>
    public ChunkedArray<T>? AsTyped<T>() where T : struct
    {
        return _data as ChunkedArray<T>;
    }

    /// <summary>Converts to a .NET array.</summary>
    public T[] ToArray<T>()
    {
        var result = new T[_data.Length];
        for (int i = 0; i < _data.Length; i++)
        {
            var value = _data.GetBoxedValue(i);
            result[i] = value is null ? default! : (T)value;
        }
        return result;
    }

    /// <summary>Converts to a nullable .NET array.</summary>
    public T?[] ToNullableArray<T>() where T : struct
    {
        var result = new T?[_data.Length];
        for (int i = 0; i < _data.Length; i++)
        {
            if (_data.IsNull(i))
                result[i] = null;
            else
                result[i] = (T)_data.GetBoxedValue(i)!;
        }
        return result;
    }
}

/// <summary>
/// String chunked array for String data type.
/// </summary>
internal sealed class StringChunkedArray : IChunkedArray
{
    private readonly IArrowArray[] _chunks;
    private readonly DataType _dataType;
    private readonly int _length;
    private readonly int _nullCount;

    public StringChunkedArray(IArrowArray chunk, DataType dataType)
        : this(new[] { chunk }, dataType)
    {
    }

    public StringChunkedArray(IArrowArray[] chunks, DataType dataType)
    {
        _chunks = chunks;
        _dataType = dataType;
        _length = chunks.Sum(c => c.Length);
        _nullCount = chunks.Sum(c => c.NullCount);
    }

    public DataType DataType => _dataType;
    public int Length => _length;
    public int NullCount => _nullCount;

    public bool IsNull(int index)
    {
        var (chunk, localIndex) = GetChunkIndex(index);
        return _chunks[chunk].IsNull(localIndex);
    }

    public string? GetString(int index)
    {
        var (chunkIndex, localIndex) = GetChunkIndex(index);
        var chunk = _chunks[chunkIndex];

        return chunk switch
        {
            StringArray arr => arr.GetString(localIndex),
            _ => null
        };
    }

    public object? GetBoxedValue(int index) => GetString(index);

    public StringChunkedArray Slice(int offset, int length)
    {
        if (offset < 0 || length < 0 || offset + length > _length)
            throw new ArgumentOutOfRangeException();

        var resultChunks = new List<IArrowArray>();
        int currentOffset = 0;
        int remaining = length;

        foreach (var chunk in _chunks)
        {
            if (currentOffset + chunk.Length <= offset)
            {
                currentOffset += chunk.Length;
                continue;
            }

            int chunkStart = Math.Max(0, offset - currentOffset);
            int chunkLength = Math.Min(remaining, chunk.Length - chunkStart);

            if (chunkLength > 0)
            {
                resultChunks.Add(Apache.Arrow.ArrowArrayFactory.Slice(chunk, chunkStart, chunkLength));
                remaining -= chunkLength;
            }

            currentOffset += chunk.Length;
            if (remaining <= 0) break;
        }

        return new StringChunkedArray(resultChunks.ToArray(), _dataType);
    }

    private (int chunkIndex, int localIndex) GetChunkIndex(int globalIndex)
    {
        int offset = 0;
        for (int i = 0; i < _chunks.Length; i++)
        {
            if (globalIndex < offset + _chunks[i].Length)
                return (i, globalIndex - offset);
            offset += _chunks[i].Length;
        }
        throw new ArgumentOutOfRangeException(nameof(globalIndex));
    }
}
