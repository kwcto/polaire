// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Polaire.Memory;

/// <summary>
/// High-performance memory pool for columnar data operations.
/// Provides pooled allocations with alignment guarantees for SIMD operations.
/// </summary>
public sealed class PolaireMemoryPool : MemoryPool<byte>
{
    private static readonly PolaireMemoryPool _shared = new();
    public static new PolaireMemoryPool Shared => _shared;

    // Align to 64-byte boundaries for optimal SIMD (AVX-512) performance
    private const int Alignment = 64;

    // Maximum allocation size before falling back to LOH
    private const int MaxPooledSize = 1024 * 1024; // 1MB

    // Bucket sizes for pooling (powers of 2, aligned)
    private static readonly int[] BucketSizes = { 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384, 32768, 65536, 131072, 262144, 524288, 1048576 };

    private readonly ArrayPool<byte>[] _buckets;

    public override int MaxBufferSize => int.MaxValue;

    public PolaireMemoryPool()
    {
        _buckets = new ArrayPool<byte>[BucketSizes.Length];
        for (int i = 0; i < BucketSizes.Length; i++)
        {
            _buckets[i] = ArrayPool<byte>.Create(BucketSizes[i], 50);
        }
    }

    public override IMemoryOwner<byte> Rent(int minBufferSize = -1)
    {
        if (minBufferSize <= 0)
            minBufferSize = 4096;

        // Find appropriate bucket
        int bucketIndex = FindBucket(minBufferSize);

        if (bucketIndex >= 0 && bucketIndex < _buckets.Length)
        {
            var pool = _buckets[bucketIndex];
            var buffer = pool.Rent(BucketSizes[bucketIndex]);
            return new PooledMemoryOwner(buffer, minBufferSize, pool);
        }

        // Large allocation - use regular array
        return new LargeMemoryOwner(new byte[minBufferSize]);
    }

    /// <summary>
    /// Rent aligned memory for SIMD operations.
    /// </summary>
    public AlignedMemory<T> RentAligned<T>(int count) where T : unmanaged
    {
        int size = count * Unsafe.SizeOf<T>();
        int alignedSize = AlignUp(size, Alignment);
        var memory = Rent(alignedSize + Alignment);
        return new AlignedMemory<T>(memory, count, Alignment);
    }

    private static int FindBucket(int size)
    {
        for (int i = 0; i < BucketSizes.Length; i++)
        {
            if (BucketSizes[i] >= size)
                return i;
        }
        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int AlignUp(int value, int alignment)
    {
        return (value + alignment - 1) & ~(alignment - 1);
    }

    protected override void Dispose(bool disposing)
    {
        // Pools are shared, don't dispose
    }

    private sealed class PooledMemoryOwner : IMemoryOwner<byte>
    {
        private byte[]? _array;
        private readonly int _length;
        private readonly ArrayPool<byte> _pool;

        public PooledMemoryOwner(byte[] array, int length, ArrayPool<byte> pool)
        {
            _array = array;
            _length = length;
            _pool = pool;
        }

        public Memory<byte> Memory => _array.AsMemory(0, _length);

        public void Dispose()
        {
            var array = _array;
            if (array != null)
            {
                _array = null;
                _pool.Return(array);
            }
        }
    }

    private sealed class LargeMemoryOwner : IMemoryOwner<byte>
    {
        private byte[]? _array;

        public LargeMemoryOwner(byte[] array)
        {
            _array = array;
        }

        public Memory<byte> Memory => _array.AsMemory();

        public void Dispose()
        {
            _array = null;
        }
    }
}

/// <summary>
/// Aligned memory wrapper for SIMD operations.
/// </summary>
public readonly struct AlignedMemory<T> : IDisposable where T : unmanaged
{
    private readonly IMemoryOwner<byte> _owner;
    private readonly int _offset;
    private readonly int _count;

    internal AlignedMemory(IMemoryOwner<byte> owner, int count, int alignment)
    {
        _owner = owner;
        _count = count;

        // Calculate offset to achieve alignment
        var span = owner.Memory.Span;
        unsafe
        {
            fixed (byte* ptr = span)
            {
                var address = (nint)ptr;
                var aligned = (address + alignment - 1) & ~(alignment - 1);
                _offset = (int)(aligned - address);
            }
        }
    }

    public Span<T> Span => MemoryMarshal.Cast<byte, T>(_owner.Memory.Span.Slice(_offset))[.._count];
    public Memory<T> Memory => new Memory<T>(Span.ToArray());
    public int Length => _count;

    public void Dispose() => _owner.Dispose();
}

/// <summary>
/// Validity bitmap for tracking null values.
/// Uses 1 bit per value for memory-efficient null tracking.
/// </summary>
public sealed class ValidityBitmap : IDisposable
{
    private readonly IMemoryOwner<byte>? _owner;
    private readonly Memory<byte> _buffer;
    private readonly int _length;
    private int _nullCount;

    /// <summary>
    /// Creates a new validity bitmap with all values valid.
    /// </summary>
    public ValidityBitmap(int length)
    {
        _length = length;
        int byteCount = (length + 7) / 8;
        _owner = PolaireMemoryPool.Shared.Rent(byteCount);
        _buffer = _owner.Memory.Slice(0, byteCount);
        _buffer.Span.Fill(0xFF); // All valid
        _nullCount = 0;
    }

    /// <summary>
    /// Creates a validity bitmap from existing data.
    /// </summary>
    public ValidityBitmap(ReadOnlySpan<byte> bitmap, int length, int nullCount)
    {
        _length = length;
        int byteCount = (length + 7) / 8;
        _owner = PolaireMemoryPool.Shared.Rent(byteCount);
        _buffer = _owner.Memory.Slice(0, byteCount);
        bitmap.Slice(0, Math.Min(byteCount, bitmap.Length)).CopyTo(_buffer.Span);
        _nullCount = nullCount;
    }

    /// <summary>
    /// Creates an all-null bitmap.
    /// </summary>
    public static ValidityBitmap AllNull(int length)
    {
        var bitmap = new ValidityBitmap(length);
        bitmap._buffer.Span.Clear();
        bitmap._nullCount = length;
        return bitmap;
    }

    /// <summary>
    /// Creates a bitmap with no nulls (no allocation for the bitmap itself).
    /// </summary>
    public static ValidityBitmap? NoNulls => null;

    public int Length => _length;
    public int NullCount => _nullCount;
    public bool HasNulls => _nullCount > 0;
    public ReadOnlySpan<byte> Bytes => _buffer.Span;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValid(int index)
    {
        int byteIndex = index >> 3;
        int bitIndex = index & 7;
        return (_buffer.Span[byteIndex] & (1 << bitIndex)) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsNull(int index) => !IsValid(index);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetValid(int index)
    {
        int byteIndex = index >> 3;
        int bitIndex = index & 7;
        ref byte b = ref _buffer.Span[byteIndex];
        if ((b & (1 << bitIndex)) == 0)
        {
            b |= (byte)(1 << bitIndex);
            _nullCount--;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetNull(int index)
    {
        int byteIndex = index >> 3;
        int bitIndex = index & 7;
        ref byte b = ref _buffer.Span[byteIndex];
        if ((b & (1 << bitIndex)) != 0)
        {
            b &= (byte)~(1 << bitIndex);
            _nullCount++;
        }
    }

    public void Dispose()
    {
        _owner?.Dispose();
    }

    /// <summary>
    /// Creates a copy of this bitmap.
    /// </summary>
    public ValidityBitmap Clone()
    {
        return new ValidityBitmap(_buffer.Span, _length, _nullCount);
    }
}
