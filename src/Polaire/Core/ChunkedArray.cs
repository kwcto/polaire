// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Apache.Arrow;
using Polaire.DataTypes;
using Polaire.Memory;

namespace Polaire.Core;

/// <summary>
/// A chunked array of values of type T, supporting efficient columnar operations.
/// Built on top of Apache Arrow arrays with additional Polaire optimizations.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public sealed class ChunkedArray<T> : IChunkedArray, IEnumerable<T?> where T : struct
{
    private readonly IArrowArray[] _chunks;
    private readonly DataType _dataType;
    private readonly int _length;
    private readonly int _nullCount;

    public ChunkedArray(IArrowArray[] chunks, DataType dataType)
    {
        _chunks = chunks ?? throw new ArgumentNullException(nameof(chunks));
        _dataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
        _length = chunks.Sum(c => c.Length);
        _nullCount = chunks.Sum(c => c.NullCount);
    }

    public ChunkedArray(IArrowArray chunk, DataType dataType)
        : this(new[] { chunk }, dataType)
    {
    }

    /// <summary>Gets the data type of this array.</summary>
    public DataType DataType => _dataType;

    /// <summary>Gets the total number of elements across all chunks.</summary>
    public int Length => _length;

    /// <summary>Gets the number of null values.</summary>
    public int NullCount => _nullCount;

    /// <summary>Gets whether this array has any null values.</summary>
    public bool HasNulls => _nullCount > 0;

    /// <summary>Gets the number of chunks.</summary>
    public int ChunkCount => _chunks.Length;

    /// <summary>Gets a specific chunk.</summary>
    public IArrowArray GetChunk(int index) => _chunks[index];

    /// <summary>Gets all chunks.</summary>
    public IReadOnlyList<IArrowArray> Chunks => _chunks;

    /// <summary>Gets whether a specific index is null.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsNull(int index)
    {
        var (chunk, localIndex) = GetChunkIndex(index);
        return _chunks[chunk].IsNull(localIndex);
    }

    /// <summary>Gets the value at the specified index, or null if the value is null.</summary>
    public T? this[int index]
    {
        get
        {
            if (IsNull(index)) return null;
            return GetValue(index);
        }
    }

    /// <summary>Gets the value at the specified index (throws if null).</summary>
    public T GetValue(int index)
    {
        var (chunkIndex, localIndex) = GetChunkIndex(index);
        var chunk = _chunks[chunkIndex];

        // Type-specific value retrieval
        return chunk switch
        {
            Int8Array arr when typeof(T) == typeof(sbyte) => (T)(object)arr.GetValue(localIndex)!.Value,
            Int16Array arr when typeof(T) == typeof(short) => (T)(object)arr.GetValue(localIndex)!.Value,
            Int32Array arr when typeof(T) == typeof(int) => (T)(object)arr.GetValue(localIndex)!.Value,
            Int64Array arr when typeof(T) == typeof(long) => (T)(object)arr.GetValue(localIndex)!.Value,
            UInt8Array arr when typeof(T) == typeof(byte) => (T)(object)arr.GetValue(localIndex)!.Value,
            UInt16Array arr when typeof(T) == typeof(ushort) => (T)(object)arr.GetValue(localIndex)!.Value,
            UInt32Array arr when typeof(T) == typeof(uint) => (T)(object)arr.GetValue(localIndex)!.Value,
            UInt64Array arr when typeof(T) == typeof(ulong) => (T)(object)arr.GetValue(localIndex)!.Value,
            FloatArray arr when typeof(T) == typeof(float) => (T)(object)arr.GetValue(localIndex)!.Value,
            DoubleArray arr when typeof(T) == typeof(double) => (T)(object)arr.GetValue(localIndex)!.Value,
            BooleanArray arr when typeof(T) == typeof(bool) => (T)(object)arr.GetValue(localIndex)!.Value,
            Date32Array arr when typeof(T) == typeof(int) => (T)(object)arr.GetValue(localIndex)!.Value,
            Date64Array arr when typeof(T) == typeof(long) => (T)(object)arr.GetValue(localIndex)!.Value,
            TimestampArray arr when typeof(T) == typeof(long) => (T)(object)arr.GetValue(localIndex)!.Value,
            _ => throw new NotSupportedException($"Cannot get value from {chunk.GetType().Name} as {typeof(T).Name}")
        };
    }

    /// <summary>Gets the raw span of values for a specific chunk (for SIMD operations).</summary>
    public ReadOnlySpan<T> GetChunkSpan(int chunkIndex)
    {
        var chunk = _chunks[chunkIndex];
        return chunk switch
        {
            Int8Array arr when typeof(T) == typeof(sbyte) => MemoryMarshal.Cast<sbyte, T>(arr.Values),
            Int16Array arr when typeof(T) == typeof(short) => MemoryMarshal.Cast<short, T>(arr.Values),
            Int32Array arr when typeof(T) == typeof(int) => MemoryMarshal.Cast<int, T>(arr.Values),
            Int64Array arr when typeof(T) == typeof(long) => MemoryMarshal.Cast<long, T>(arr.Values),
            UInt8Array arr when typeof(T) == typeof(byte) => MemoryMarshal.Cast<byte, T>(arr.Values),
            UInt16Array arr when typeof(T) == typeof(ushort) => MemoryMarshal.Cast<ushort, T>(arr.Values),
            UInt32Array arr when typeof(T) == typeof(uint) => MemoryMarshal.Cast<uint, T>(arr.Values),
            UInt64Array arr when typeof(T) == typeof(ulong) => MemoryMarshal.Cast<ulong, T>(arr.Values),
            FloatArray arr when typeof(T) == typeof(float) => MemoryMarshal.Cast<float, T>(arr.Values),
            DoubleArray arr when typeof(T) == typeof(double) => MemoryMarshal.Cast<double, T>(arr.Values),
            _ => throw new NotSupportedException($"Cannot get span from {chunk.GetType().Name}")
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private (int chunkIndex, int localIndex) GetChunkIndex(int globalIndex)
    {
        if (globalIndex < 0 || globalIndex >= _length)
            throw new ArgumentOutOfRangeException(nameof(globalIndex));

        int offset = 0;
        for (int i = 0; i < _chunks.Length; i++)
        {
            if (globalIndex < offset + _chunks[i].Length)
                return (i, globalIndex - offset);
            offset += _chunks[i].Length;
        }

        throw new InvalidOperationException("Index calculation error");
    }

    /// <summary>Slices this array to create a view over a subset of the data.</summary>
    public ChunkedArray<T> Slice(int offset, int length)
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
                resultChunks.Add(ArrowArraySlice(chunk, chunkStart, chunkLength));
                remaining -= chunkLength;
            }

            currentOffset += chunk.Length;
            if (remaining <= 0) break;
        }

        return new ChunkedArray<T>(resultChunks.ToArray(), _dataType);
    }

    private static IArrowArray ArrowArraySlice(IArrowArray array, int offset, int length)
    {
        // Arrow arrays support slicing through their builders
        // For now, return slice view
        return Apache.Arrow.ArrowArrayFactory.Slice(array, offset, length);
    }

    /// <summary>Concatenates multiple chunked arrays.</summary>
    public static ChunkedArray<T> Concat(params ChunkedArray<T>[] arrays)
    {
        if (arrays.Length == 0)
            throw new ArgumentException("At least one array required", nameof(arrays));

        var dataType = arrays[0]._dataType;
        var allChunks = arrays.SelectMany(a => a._chunks).ToArray();
        return new ChunkedArray<T>(allChunks, dataType);
    }

    /// <summary>Rechunks into a single contiguous chunk.</summary>
    public ChunkedArray<T> Rechunk()
    {
        if (_chunks.Length == 1)
            return this;

        // Build a single array containing all values
        var builder = CreateBuilder();
        foreach (var chunk in _chunks)
        {
            AppendChunkToBuilder(builder, chunk);
        }

        return new ChunkedArray<T>(new[] { builder.Build(default) }, _dataType);
    }

    private IArrowArrayBuilder<IArrowArray> CreateBuilder()
    {
        return _dataType switch
        {
            DataType.Int8Type => new Int8Array.Builder() as IArrowArrayBuilder<IArrowArray>,
            DataType.Int16Type => new Int16Array.Builder(),
            DataType.Int32Type => new Int32Array.Builder(),
            DataType.Int64Type => new Int64Array.Builder(),
            DataType.UInt8Type => new UInt8Array.Builder(),
            DataType.UInt16Type => new UInt16Array.Builder(),
            DataType.UInt32Type => new UInt32Array.Builder(),
            DataType.UInt64Type => new UInt64Array.Builder(),
            DataType.Float32Type => new FloatArray.Builder(),
            DataType.Float64Type => new DoubleArray.Builder(),
            DataType.BooleanType => new BooleanArray.Builder(),
            _ => throw new NotSupportedException($"Builder not supported for {_dataType}")
        } ?? throw new InvalidOperationException();
    }

    private void AppendChunkToBuilder(IArrowArrayBuilder<IArrowArray> builder, IArrowArray chunk)
    {
        // Type-specific append operations
        switch (builder)
        {
            case Int32Array.Builder b when chunk is Int32Array arr:
                for (int i = 0; i < arr.Length; i++)
                    if (arr.IsNull(i)) b.AppendNull(); else b.Append(arr.GetValue(i)!.Value);
                break;
            case Int64Array.Builder b when chunk is Int64Array arr:
                for (int i = 0; i < arr.Length; i++)
                    if (arr.IsNull(i)) b.AppendNull(); else b.Append(arr.GetValue(i)!.Value);
                break;
            case DoubleArray.Builder b when chunk is DoubleArray arr:
                for (int i = 0; i < arr.Length; i++)
                    if (arr.IsNull(i)) b.AppendNull(); else b.Append(arr.GetValue(i)!.Value);
                break;
            // Add more types as needed
            default:
                throw new NotSupportedException($"Append not supported for {builder.GetType().Name}");
        }
    }

    public IEnumerator<T?> GetEnumerator()
    {
        for (int i = 0; i < _length; i++)
        {
            yield return this[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    object? IChunkedArray.GetBoxedValue(int index) => this[index];
    DataType IChunkedArray.DataType => _dataType;
    int IChunkedArray.Length => _length;
    int IChunkedArray.NullCount => _nullCount;
}

/// <summary>
/// Non-generic interface for chunked arrays.
/// </summary>
public interface IChunkedArray
{
    DataType DataType { get; }
    int Length { get; }
    int NullCount { get; }
    bool HasNulls => NullCount > 0;
    bool IsNull(int index);
    object? GetBoxedValue(int index);
}

/// <summary>
/// Extension methods for Arrow array slicing.
/// </summary>
public static class ArrowArrayFactory
{
    public static IArrowArray Slice(IArrowArray array, int offset, int length)
    {
        // Apache Arrow supports built-in slicing
        return array switch
        {
            Int8Array arr => arr.Slice(offset, length),
            Int16Array arr => arr.Slice(offset, length),
            Int32Array arr => arr.Slice(offset, length),
            Int64Array arr => arr.Slice(offset, length),
            UInt8Array arr => arr.Slice(offset, length),
            UInt16Array arr => arr.Slice(offset, length),
            UInt32Array arr => arr.Slice(offset, length),
            UInt64Array arr => arr.Slice(offset, length),
            FloatArray arr => arr.Slice(offset, length),
            DoubleArray arr => arr.Slice(offset, length),
            BooleanArray arr => arr.Slice(offset, length),
            StringArray arr => arr.Slice(offset, length),
            BinaryArray arr => arr.Slice(offset, length),
            _ => throw new NotSupportedException($"Slice not supported for {array.GetType().Name}")
        };
    }
}
