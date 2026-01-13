// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using Apache.Arrow;
using Apache.Arrow.Types;
using Polaire.DataTypes;
using Polaire.Compute;

namespace Polaire.Core;

/// <summary>
/// Chunked array for List type data.
/// Wraps Arrow ListArray and provides list-specific operations.
/// </summary>
internal sealed class ListChunkedArray : IChunkedArray
{
    private readonly ListArray[] _chunks;
    private readonly DataType _dataType;
    private readonly int _length;
    private readonly int _nullCount;

    public ListChunkedArray(IArrowArray chunk, DataType dataType)
        : this(new[] { (ListArray)chunk }, dataType)
    {
    }

    public ListChunkedArray(ListArray[] chunks, DataType dataType)
    {
        _chunks = chunks;
        _dataType = dataType;
        _length = chunks.Sum(c => c.Length);
        _nullCount = chunks.Sum(c => c.NullCount);
    }

    public DataType DataType => _dataType;
    public int Length => _length;
    public int NullCount => _nullCount;
    public bool HasNulls => _nullCount > 0;

    public bool IsNull(int index)
    {
        var (chunk, localIndex) = GetChunkIndex(index);
        return _chunks[chunk].IsNull(localIndex);
    }

    public object? GetBoxedValue(int index)
    {
        if (IsNull(index)) return null;
        return GetList(index);
    }

    /// <summary>
    /// Gets the list at the specified index as an array of values.
    /// </summary>
    public object?[] GetList(int index)
    {
        var (chunkIndex, localIndex) = GetChunkIndex(index);
        var chunk = _chunks[chunkIndex];

        var offsets = chunk.ValueOffsets;
        int start = offsets[localIndex];
        int end = offsets[localIndex + 1];
        int listLength = end - start;

        var values = chunk.Values;
        var result = new object?[listLength];

        for (int i = 0; i < listLength; i++)
        {
            if (values.IsNull(start + i))
            {
                result[i] = null;
            }
            else
            {
                result[i] = GetValueFromArray(values, start + i);
            }
        }

        return result;
    }

    /// <summary>
    /// Gets the length of the list at the specified index.
    /// </summary>
    public int GetListLength(int index)
    {
        var (chunkIndex, localIndex) = GetChunkIndex(index);
        var chunk = _chunks[chunkIndex];

        var offsets = chunk.ValueOffsets;
        return offsets[localIndex + 1] - offsets[localIndex];
    }

    /// <summary>
    /// Gets the element at specified index within each list.
    /// </summary>
    public Series GetElementAt(int elementIndex, string name, DataType innerType)
    {
        return innerType switch
        {
            DataType.Int32Type => GetElementAtInt32(elementIndex, name),
            DataType.Int64Type => GetElementAtInt64(elementIndex, name),
            DataType.Float64Type => GetElementAtFloat64(elementIndex, name),
            DataType.Float32Type => GetElementAtFloat32(elementIndex, name),
            DataType.StringType => GetElementAtString(elementIndex, name),
            DataType.BooleanType => GetElementAtBool(elementIndex, name),
            _ => throw new NotSupportedException($"Element access not supported for {innerType}")
        };
    }

    private Series GetElementAtInt32(int elementIndex, string name)
    {
        var builder = new Int32Array.Builder();

        for (int i = 0; i < _length; i++)
        {
            if (IsNull(i))
            {
                builder.AppendNull();
                continue;
            }

            var list = GetList(i);
            int actualIndex = elementIndex < 0 ? list.Length + elementIndex : elementIndex;

            if (actualIndex < 0 || actualIndex >= list.Length)
            {
                builder.AppendNull();
            }
            else if (list[actualIndex] == null)
            {
                builder.AppendNull();
            }
            else
            {
                builder.Append(Convert.ToInt32(list[actualIndex]));
            }
        }

        return Series.FromArrowArray(name, builder.Build(), DataType.Int32);
    }

    private Series GetElementAtInt64(int elementIndex, string name)
    {
        var builder = new Int64Array.Builder();

        for (int i = 0; i < _length; i++)
        {
            if (IsNull(i))
            {
                builder.AppendNull();
                continue;
            }

            var list = GetList(i);
            int actualIndex = elementIndex < 0 ? list.Length + elementIndex : elementIndex;

            if (actualIndex < 0 || actualIndex >= list.Length)
            {
                builder.AppendNull();
            }
            else if (list[actualIndex] == null)
            {
                builder.AppendNull();
            }
            else
            {
                builder.Append(Convert.ToInt64(list[actualIndex]));
            }
        }

        return Series.FromArrowArray(name, builder.Build(), DataType.Int64);
    }

    private Series GetElementAtFloat64(int elementIndex, string name)
    {
        var builder = new DoubleArray.Builder();

        for (int i = 0; i < _length; i++)
        {
            if (IsNull(i))
            {
                builder.AppendNull();
                continue;
            }

            var list = GetList(i);
            int actualIndex = elementIndex < 0 ? list.Length + elementIndex : elementIndex;

            if (actualIndex < 0 || actualIndex >= list.Length)
            {
                builder.AppendNull();
            }
            else if (list[actualIndex] == null)
            {
                builder.AppendNull();
            }
            else
            {
                builder.Append(Convert.ToDouble(list[actualIndex]));
            }
        }

        return Series.FromArrowArray(name, builder.Build(), DataType.Float64);
    }

    private Series GetElementAtFloat32(int elementIndex, string name)
    {
        var builder = new FloatArray.Builder();

        for (int i = 0; i < _length; i++)
        {
            if (IsNull(i))
            {
                builder.AppendNull();
                continue;
            }

            var list = GetList(i);
            int actualIndex = elementIndex < 0 ? list.Length + elementIndex : elementIndex;

            if (actualIndex < 0 || actualIndex >= list.Length)
            {
                builder.AppendNull();
            }
            else if (list[actualIndex] == null)
            {
                builder.AppendNull();
            }
            else
            {
                builder.Append(Convert.ToSingle(list[actualIndex]));
            }
        }

        return Series.FromArrowArray(name, builder.Build(), DataType.Float32);
    }

    private Series GetElementAtString(int elementIndex, string name)
    {
        var builder = new StringArray.Builder();

        for (int i = 0; i < _length; i++)
        {
            if (IsNull(i))
            {
                builder.AppendNull();
                continue;
            }

            var list = GetList(i);
            int actualIndex = elementIndex < 0 ? list.Length + elementIndex : elementIndex;

            if (actualIndex < 0 || actualIndex >= list.Length)
            {
                builder.AppendNull();
            }
            else if (list[actualIndex] == null)
            {
                builder.AppendNull();
            }
            else
            {
                builder.Append(list[actualIndex]!.ToString()!);
            }
        }

        return Series.FromArrowArray(name, builder.Build(), DataType.String);
    }

    private Series GetElementAtBool(int elementIndex, string name)
    {
        var builder = new BooleanArray.Builder();

        for (int i = 0; i < _length; i++)
        {
            if (IsNull(i))
            {
                builder.AppendNull();
                continue;
            }

            var list = GetList(i);
            int actualIndex = elementIndex < 0 ? list.Length + elementIndex : elementIndex;

            if (actualIndex < 0 || actualIndex >= list.Length)
            {
                builder.AppendNull();
            }
            else if (list[actualIndex] == null)
            {
                builder.AppendNull();
            }
            else
            {
                builder.Append(Convert.ToBoolean(list[actualIndex]));
            }
        }

        return Series.FromArrowArray(name, builder.Build(), DataType.Boolean);
    }

    /// <summary>
    /// Checks if the list at index contains the specified value.
    /// </summary>
    public bool ListContains(int index, AnyValue value)
    {
        var list = GetList(index);
        foreach (var item in list)
        {
            if (item == null && value.IsNull) return true;
            if (item != null && AnyValueEquals(item, value)) return true;
        }
        return false;
    }

    private bool AnyValueEquals(object item, AnyValue value)
    {
        if (value.TryGetInt64(out var l))
            return Convert.ToInt64(item) == l;
        if (value.TryGetDouble(out var d))
            return Math.Abs(Convert.ToDouble(item) - d) < 1e-10;
        if (value.TryGetString(out var s))
            return item.ToString() == s;
        // Handle boolean by checking Kind
        if (value.Kind == AnyValueKind.Boolean)
            return Convert.ToBoolean(item) == value.AsBoolean();
        return false;
    }

    /// <summary>
    /// Aggregates values within each list.
    /// </summary>
    public Series AggregateList(string name, ListAggregationType aggType)
    {
        var builder = new DoubleArray.Builder();

        for (int i = 0; i < _length; i++)
        {
            if (IsNull(i))
            {
                builder.AppendNull();
                continue;
            }

            var list = GetList(i);
            var numericValues = list
                .Where(v => v != null)
                .Select(v => Convert.ToDouble(v))
                .ToArray();

            if (numericValues.Length == 0)
            {
                builder.AppendNull();
            }
            else
            {
                double result = aggType switch
                {
                    ListAggregationType.Sum => numericValues.Sum(),
                    ListAggregationType.Mean => numericValues.Average(),
                    ListAggregationType.Min => numericValues.Min(),
                    ListAggregationType.Max => numericValues.Max(),
                    _ => throw new NotSupportedException()
                };
                builder.Append(result);
            }
        }

        return Series.FromArrowArray(name, builder.Build(), DataType.Float64);
    }

    /// <summary>
    /// Transforms each list according to the specified transformation.
    /// </summary>
    public Series TransformList(string name, ListTransformation transform)
    {
        // For simplicity, we return a string representation of the transformed list
        // A full implementation would rebuild the Arrow ListArray
        var builder = new StringArray.Builder();

        for (int i = 0; i < _length; i++)
        {
            if (IsNull(i))
            {
                builder.AppendNull();
                continue;
            }

            var list = GetList(i);
            IEnumerable<object?> transformed = transform switch
            {
                ListTransformation.Reverse => list.Reverse(),
                ListTransformation.SortAscending => list.OrderBy(v => v?.ToString()),
                ListTransformation.SortDescending => list.OrderByDescending(v => v?.ToString()),
                ListTransformation.Unique => list.Distinct(),
                _ => list
            };

            var result = "[" + string.Join(", ", transformed.Select(v => v?.ToString() ?? "null")) + "]";
            builder.Append(result);
        }

        return Series.FromArrowArray(name, builder.Build(), DataType.String);
    }

    /// <summary>
    /// Slices each list.
    /// </summary>
    public Series SliceList(string name, int offset, int? length)
    {
        var builder = new StringArray.Builder();

        for (int i = 0; i < _length; i++)
        {
            if (IsNull(i))
            {
                builder.AppendNull();
                continue;
            }

            var list = GetList(i);
            int actualOffset = offset < 0 ? Math.Max(0, list.Length + offset) : Math.Min(offset, list.Length);
            int actualLength = length ?? (list.Length - actualOffset);
            actualLength = Math.Min(actualLength, list.Length - actualOffset);

            var sliced = list.Skip(actualOffset).Take(actualLength);
            var result = "[" + string.Join(", ", sliced.Select(v => v?.ToString() ?? "null")) + "]";
            builder.Append(result);
        }

        return Series.FromArrowArray(name, builder.Build(), DataType.String);
    }

    /// <summary>
    /// Gets the last n elements from each list.
    /// </summary>
    public Series TailList(string name, int n)
    {
        var builder = new StringArray.Builder();

        for (int i = 0; i < _length; i++)
        {
            if (IsNull(i))
            {
                builder.AppendNull();
                continue;
            }

            var list = GetList(i);
            var sliced = list.Skip(Math.Max(0, list.Length - n)).Take(n);
            var result = "[" + string.Join(", ", sliced.Select(v => v?.ToString() ?? "null")) + "]";
            builder.Append(result);
        }

        return Series.FromArrowArray(name, builder.Build(), DataType.String);
    }

    /// <summary>
    /// Joins list elements into a string.
    /// </summary>
    public Series JoinList(string name, string separator)
    {
        var builder = new StringArray.Builder();

        for (int i = 0; i < _length; i++)
        {
            if (IsNull(i))
            {
                builder.AppendNull();
                continue;
            }

            var list = GetList(i);
            var result = string.Join(separator, list.Select(v => v?.ToString() ?? ""));
            builder.Append(result);
        }

        return Series.FromArrowArray(name, builder.Build(), DataType.String);
    }

    /// <summary>
    /// Explodes the list column into separate rows.
    /// </summary>
    public (Series values, int[] indices) ExplodeList(string name)
    {
        var innerType = ((DataType.ListType)_dataType).Inner;
        var valuesList = new List<object?>();
        var indicesList = new List<int>();

        for (int i = 0; i < _length; i++)
        {
            if (IsNull(i))
            {
                valuesList.Add(null);
                indicesList.Add(i);
            }
            else
            {
                var list = GetList(i);
                if (list.Length == 0)
                {
                    valuesList.Add(null);
                    indicesList.Add(i);
                }
                else
                {
                    foreach (var item in list)
                    {
                        valuesList.Add(item);
                        indicesList.Add(i);
                    }
                }
            }
        }

        // Build the values series based on inner type
        Series valuesSeries = BuildSeriesFromValues(valuesList, name, innerType);
        return (valuesSeries, indicesList.ToArray());
    }

    private Series BuildSeriesFromValues(List<object?> values, string name, DataType innerType)
    {
        return innerType switch
        {
            DataType.Int32Type => BuildInt32Series(values, name),
            DataType.Int64Type => BuildInt64Series(values, name),
            DataType.Float64Type => BuildFloat64Series(values, name),
            DataType.StringType => BuildStringFromValues(values, name),
            _ => BuildStringFromValues(values, name) // Fallback to string
        };
    }

    private Series BuildInt32Series(List<object?> values, string name)
    {
        var builder = new Int32Array.Builder();
        foreach (var v in values)
        {
            if (v == null) builder.AppendNull();
            else builder.Append(Convert.ToInt32(v));
        }
        return Series.FromArrowArray(name, builder.Build(), DataType.Int32);
    }

    private Series BuildInt64Series(List<object?> values, string name)
    {
        var builder = new Int64Array.Builder();
        foreach (var v in values)
        {
            if (v == null) builder.AppendNull();
            else builder.Append(Convert.ToInt64(v));
        }
        return Series.FromArrowArray(name, builder.Build(), DataType.Int64);
    }

    private Series BuildFloat64Series(List<object?> values, string name)
    {
        var builder = new DoubleArray.Builder();
        foreach (var v in values)
        {
            if (v == null) builder.AppendNull();
            else builder.Append(Convert.ToDouble(v));
        }
        return Series.FromArrowArray(name, builder.Build(), DataType.Float64);
    }

    private Series BuildStringFromValues(List<object?> values, string name)
    {
        var builder = new StringArray.Builder();
        foreach (var v in values)
        {
            if (v == null) builder.AppendNull();
            else builder.Append(v.ToString()!);
        }
        return Series.FromArrowArray(name, builder.Build(), DataType.String);
    }

    private static object? GetValueFromArray(IArrowArray array, int index)
    {
        return array switch
        {
            Int32Array arr => arr.GetValue(index),
            Int64Array arr => arr.GetValue(index),
            FloatArray arr => arr.GetValue(index),
            DoubleArray arr => arr.GetValue(index),
            StringArray arr => arr.GetString(index),
            BooleanArray arr => arr.GetValue(index),
            Int8Array arr => arr.GetValue(index),
            Int16Array arr => arr.GetValue(index),
            UInt8Array arr => arr.GetValue(index),
            UInt16Array arr => arr.GetValue(index),
            UInt32Array arr => arr.GetValue(index),
            UInt64Array arr => arr.GetValue(index),
            _ => null
        };
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
