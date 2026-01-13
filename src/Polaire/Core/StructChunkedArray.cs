// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using Apache.Arrow;
using System.Text;
using System.Text.Json;
using Polaire.DataTypes;

namespace Polaire.Core;

/// <summary>
/// Chunked array for Struct type data.
/// Wraps Arrow StructArray and provides struct-specific operations.
/// </summary>
internal sealed class StructChunkedArray : IChunkedArray
{
    private readonly StructArray[] _chunks;
    private readonly DataType _dataType;
    private readonly int _length;
    private readonly int _nullCount;

    public StructChunkedArray(IArrowArray chunk, DataType dataType)
        : this(new[] { (StructArray)chunk }, dataType)
    {
    }

    public StructChunkedArray(StructArray[] chunks, DataType dataType)
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
        return GetStruct(index);
    }

    /// <summary>
    /// Gets the struct at the specified index as a dictionary of field values.
    /// </summary>
    public Dictionary<string, object?> GetStruct(int index)
    {
        var (chunkIndex, localIndex) = GetChunkIndex(index);
        var chunk = _chunks[chunkIndex];
        var structType = (DataType.StructType)_dataType;

        var result = new Dictionary<string, object?>();

        for (int fieldIndex = 0; fieldIndex < structType.Fields.Count; fieldIndex++)
        {
            var field = structType.Fields[fieldIndex];
            var fieldArray = chunk.Fields[fieldIndex];

            if (fieldArray.IsNull(localIndex))
            {
                result[field.Name] = null;
            }
            else
            {
                result[field.Name] = GetValueFromArray(fieldArray, localIndex);
            }
        }

        return result;
    }

    /// <summary>
    /// Gets a specific field from all structs as a Series.
    /// </summary>
    public Series GetField(string fieldName, string outputName)
    {
        var structType = (DataType.StructType)_dataType;
        var fieldIndex = -1;
        DataType? fieldType = null;

        for (int i = 0; i < structType.Fields.Count; i++)
        {
            if (structType.Fields[i].Name == fieldName)
            {
                fieldIndex = i;
                fieldType = structType.Fields[i].Type;
                break;
            }
        }

        if (fieldIndex < 0)
            throw new ArgumentException($"Field '{fieldName}' not found in struct");

        return fieldType switch
        {
            DataType.Int32Type => GetFieldInt32(fieldIndex, outputName),
            DataType.Int64Type => GetFieldInt64(fieldIndex, outputName),
            DataType.Float64Type => GetFieldFloat64(fieldIndex, outputName),
            DataType.Float32Type => GetFieldFloat32(fieldIndex, outputName),
            DataType.StringType => GetFieldString(fieldIndex, outputName),
            DataType.BooleanType => GetFieldBool(fieldIndex, outputName),
            DataType.DateType => GetFieldDate(fieldIndex, outputName),
            _ => GetFieldString(fieldIndex, outputName) // Fallback to string conversion
        };
    }

    private Series GetFieldInt32(int fieldIndex, string name)
    {
        var builder = new Int32Array.Builder();

        int offset = 0;
        foreach (var chunk in _chunks)
        {
            var fieldArray = chunk.Fields[fieldIndex] as Int32Array;
            for (int i = 0; i < chunk.Length; i++)
            {
                if (chunk.IsNull(i) || fieldArray!.IsNull(i))
                {
                    builder.AppendNull();
                }
                else
                {
                    builder.Append(fieldArray!.GetValue(i)!.Value);
                }
            }
            offset += chunk.Length;
        }

        return Series.FromArrowArray(name, builder.Build(), DataType.Int32);
    }

    private Series GetFieldInt64(int fieldIndex, string name)
    {
        var builder = new Int64Array.Builder();

        foreach (var chunk in _chunks)
        {
            var fieldArray = chunk.Fields[fieldIndex] as Int64Array;
            for (int i = 0; i < chunk.Length; i++)
            {
                if (chunk.IsNull(i) || fieldArray!.IsNull(i))
                {
                    builder.AppendNull();
                }
                else
                {
                    builder.Append(fieldArray!.GetValue(i)!.Value);
                }
            }
        }

        return Series.FromArrowArray(name, builder.Build(), DataType.Int64);
    }

    private Series GetFieldFloat64(int fieldIndex, string name)
    {
        var builder = new DoubleArray.Builder();

        foreach (var chunk in _chunks)
        {
            var fieldArray = chunk.Fields[fieldIndex] as DoubleArray;
            for (int i = 0; i < chunk.Length; i++)
            {
                if (chunk.IsNull(i) || fieldArray!.IsNull(i))
                {
                    builder.AppendNull();
                }
                else
                {
                    builder.Append(fieldArray!.GetValue(i)!.Value);
                }
            }
        }

        return Series.FromArrowArray(name, builder.Build(), DataType.Float64);
    }

    private Series GetFieldFloat32(int fieldIndex, string name)
    {
        var builder = new FloatArray.Builder();

        foreach (var chunk in _chunks)
        {
            var fieldArray = chunk.Fields[fieldIndex] as FloatArray;
            for (int i = 0; i < chunk.Length; i++)
            {
                if (chunk.IsNull(i) || fieldArray!.IsNull(i))
                {
                    builder.AppendNull();
                }
                else
                {
                    builder.Append(fieldArray!.GetValue(i)!.Value);
                }
            }
        }

        return Series.FromArrowArray(name, builder.Build(), DataType.Float32);
    }

    private Series GetFieldString(int fieldIndex, string name)
    {
        var builder = new StringArray.Builder();

        foreach (var chunk in _chunks)
        {
            var fieldArray = chunk.Fields[fieldIndex];
            for (int i = 0; i < chunk.Length; i++)
            {
                if (chunk.IsNull(i) || fieldArray.IsNull(i))
                {
                    builder.AppendNull();
                }
                else
                {
                    var value = GetValueFromArray(fieldArray, i);
                    builder.Append(value?.ToString() ?? "");
                }
            }
        }

        return Series.FromArrowArray(name, builder.Build(), DataType.String);
    }

    private Series GetFieldBool(int fieldIndex, string name)
    {
        var builder = new BooleanArray.Builder();

        foreach (var chunk in _chunks)
        {
            var fieldArray = chunk.Fields[fieldIndex] as BooleanArray;
            for (int i = 0; i < chunk.Length; i++)
            {
                if (chunk.IsNull(i) || fieldArray!.IsNull(i))
                {
                    builder.AppendNull();
                }
                else
                {
                    builder.Append(fieldArray!.GetValue(i)!.Value);
                }
            }
        }

        return Series.FromArrowArray(name, builder.Build(), DataType.Boolean);
    }

    private Series GetFieldDate(int fieldIndex, string name)
    {
        var builder = new Date32Array.Builder();
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        foreach (var chunk in _chunks)
        {
            var fieldArray = chunk.Fields[fieldIndex] as Date32Array;
            for (int i = 0; i < chunk.Length; i++)
            {
                if (chunk.IsNull(i) || fieldArray!.IsNull(i))
                {
                    builder.AppendNull();
                }
                else
                {
                    var days = fieldArray!.GetValue(i)!.Value;
                    builder.Append(epoch.AddDays(days));
                }
            }
        }

        return Series.FromArrowArray(name, builder.Build(), DataType.Date);
    }

    /// <summary>
    /// Converts each struct to JSON.
    /// </summary>
    public Series ToJson(string name)
    {
        var builder = new StringArray.Builder();
        var structType = (DataType.StructType)_dataType;

        for (int i = 0; i < _length; i++)
        {
            if (IsNull(i))
            {
                builder.AppendNull();
            }
            else
            {
                var structValue = GetStruct(i);
                var json = JsonSerializer.Serialize(structValue);
                builder.Append(json);
            }
        }

        return Series.FromArrowArray(name, builder.Build(), DataType.String);
    }

    /// <summary>
    /// Creates a new StructChunkedArray with a different data type (for renaming).
    /// </summary>
    public StructChunkedArray WithNewType(DataType.StructType newType)
    {
        return new StructChunkedArray(_chunks, newType);
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
            Date32Array arr => arr.GetValue(index),
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
