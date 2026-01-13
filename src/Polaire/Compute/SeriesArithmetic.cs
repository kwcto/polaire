// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Apache.Arrow;
using Polaire.DataTypes;
using Polaire.Core;


namespace Polaire.Compute;

/// <summary>
/// Delegate for SIMD vector binary operations on double arrays.
/// </summary>
public delegate void VectorBinaryOp(ReadOnlySpan<double> left, ReadOnlySpan<double> right, Span<double> result);

/// <summary>
/// SIMD-optimized arithmetic operations for Series.
/// </summary>
public static class SeriesArithmetic
{
    // ============================================================================
    // Binary Operations
    // ============================================================================

    public static Series Add(Series left, Series right)
    {
        ValidateShapes(left, right);
        return BinaryOp(left, right, "add", (a, b) => a + b, (a, b) => a + b, VectorAdd);
    }

    public static Series Subtract(Series left, Series right)
    {
        ValidateShapes(left, right);
        return BinaryOp(left, right, "sub", (a, b) => a - b, (a, b) => a - b, VectorSubtract);
    }

    public static Series Multiply(Series left, Series right)
    {
        ValidateShapes(left, right);
        return BinaryOp(left, right, "mul", (a, b) => a * b, (a, b) => a * b, VectorMultiply);
    }

    public static Series Divide(Series left, Series right)
    {
        ValidateShapes(left, right);
        return BinaryOp(left, right, "div", (a, b) => a / b, (a, b) => a / b, VectorDivide);
    }

    public static Series Modulo(Series left, Series right)
    {
        ValidateShapes(left, right);
        return BinaryOp(left, right, "mod", (a, b) => a % b, (a, b) => a % b, null);
    }

    // ============================================================================
    // Scalar Operations
    // ============================================================================

    public static Series AddScalar(Series series, double scalar)
    {
        return ScalarOp(series, scalar, "add", (a, b) => a + b, (a, b) => a + b);
    }

    public static Series SubtractScalar(Series series, double scalar)
    {
        return ScalarOp(series, scalar, "sub", (a, b) => a - b, (a, b) => a - b);
    }

    public static Series MultiplyScalar(Series series, double scalar)
    {
        return ScalarOp(series, scalar, "mul", (a, b) => a * b, (a, b) => a * b);
    }

    public static Series DivideScalar(Series series, double scalar)
    {
        return ScalarOp(series, scalar, "div", (a, b) => a / b, (a, b) => a / b);
    }

    public static Series Negate(Series series)
    {
        return series.DataType switch
        {
            DataType.Int32Type => NegateInt32(series),
            DataType.Int64Type => NegateInt64(series),
            DataType.Float32Type => NegateFloat32(series),
            DataType.Float64Type => NegateFloat64(series),
            _ => throw new NotSupportedException($"Negate not supported for {series.DataType}")
        };
    }

    // ============================================================================
    // Implementation
    // ============================================================================

    private static Series BinaryOp(
        Series left, Series right, string opName,
        Func<long, long, long> int64Op,
        Func<double, double, double> float64Op,
        VectorBinaryOp? vectorOp)
    {
        // Promote to common type
        var (promotedLeft, promotedRight, resultType) = PromoteTypes(left, right);

        return resultType switch
        {
            DataType.Int64Type => BinaryOpInt64(promotedLeft, promotedRight, opName, int64Op),
            DataType.Float64Type => BinaryOpFloat64(promotedLeft, promotedRight, opName, float64Op, vectorOp),
            _ => throw new NotSupportedException($"Binary operation not supported for {resultType}")
        };
    }

    private static Series BinaryOpInt64(Series left, Series right, string opName, Func<long, long, long> op)
    {
        var leftData = left.Data as ChunkedArray<long>;
        var rightData = right.Data as ChunkedArray<long>;

        if (leftData is null || rightData is null)
            throw new InvalidOperationException("Expected Int64 arrays");

        var builder = new Int64Array.Builder();
        builder.Reserve(left.Length);

        for (int i = 0; i < left.Length; i++)
        {
            if (left.IsNull(i) || right.IsNull(i))
            {
                builder.AppendNull();
            }
            else
            {
                builder.Append(op(leftData.GetValue(i), rightData.GetValue(i)));
            }
        }

        return Series.FromArrowArray(left.Name, builder.Build(), DataType.Int64);
    }

    private static Series BinaryOpFloat64(
        Series left, Series right, string opName,
        Func<double, double, double> scalarOp,
        VectorBinaryOp? vectorOp)
    {
        var leftData = left.Data as ChunkedArray<double>;
        var rightData = right.Data as ChunkedArray<double>;

        if (leftData is null || rightData is null)
            throw new InvalidOperationException("Expected Float64 arrays");

        var builder = new DoubleArray.Builder();
        builder.Reserve(left.Length);

        // Check if we can use SIMD path (no nulls)
        if (!left.HasNulls && !right.HasNulls && vectorOp != null && leftData.ChunkCount == 1 && rightData.ChunkCount == 1)
        {
            var leftSpan = leftData.GetChunkSpan(0);
            var rightSpan = rightData.GetChunkSpan(0);
            var result = new double[left.Length];
            vectorOp(leftSpan, rightSpan, result);

            foreach (var v in result)
                builder.Append(v);
        }
        else
        {
            // Scalar path with null handling
            for (int i = 0; i < left.Length; i++)
            {
                if (left.IsNull(i) || right.IsNull(i))
                {
                    builder.AppendNull();
                }
                else
                {
                    builder.Append(scalarOp(leftData.GetValue(i), rightData.GetValue(i)));
                }
            }
        }

        return Series.FromArrowArray(left.Name, builder.Build(), DataType.Float64);
    }

    private static Series ScalarOp(Series series, double scalar, string opName, Func<long, long, long> int64Op, Func<double, double, double> float64Op)
    {
        return series.DataType switch
        {
            DataType.Int32Type => ScalarOpInt32(series, (int)scalar, int64Op),
            DataType.Int64Type => ScalarOpInt64(series, (long)scalar, int64Op),
            DataType.Float32Type => ScalarOpFloat32(series, (float)scalar, (a, b) => (float)float64Op(a, b)),
            DataType.Float64Type => ScalarOpFloat64(series, scalar, float64Op),
            _ => throw new NotSupportedException($"Scalar operation not supported for {series.DataType}")
        };
    }

    private static Series ScalarOpInt32(Series series, int scalar, Func<long, long, long> op)
    {
        var data = series.Data as ChunkedArray<int>;
        if (data is null) throw new InvalidOperationException("Expected Int32 array");

        var builder = new Int32Array.Builder();
        for (int i = 0; i < series.Length; i++)
        {
            if (series.IsNull(i))
                builder.AppendNull();
            else
                builder.Append((int)op(data.GetValue(i), scalar));
        }

        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Int32);
    }

    private static Series ScalarOpInt64(Series series, long scalar, Func<long, long, long> op)
    {
        var data = series.Data as ChunkedArray<long>;
        if (data is null) throw new InvalidOperationException("Expected Int64 array");

        var builder = new Int64Array.Builder();
        for (int i = 0; i < series.Length; i++)
        {
            if (series.IsNull(i))
                builder.AppendNull();
            else
                builder.Append(op(data.GetValue(i), scalar));
        }

        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Int64);
    }

    private static Series ScalarOpFloat32(Series series, float scalar, Func<float, float, float> op)
    {
        var data = series.Data as ChunkedArray<float>;
        if (data is null) throw new InvalidOperationException("Expected Float32 array");

        var builder = new FloatArray.Builder();
        for (int i = 0; i < series.Length; i++)
        {
            if (series.IsNull(i))
                builder.AppendNull();
            else
                builder.Append(op(data.GetValue(i), scalar));
        }

        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Float32);
    }

    private static Series ScalarOpFloat64(Series series, double scalar, Func<double, double, double> op)
    {
        var data = series.Data as ChunkedArray<double>;
        if (data is null) throw new InvalidOperationException("Expected Float64 array");

        var builder = new DoubleArray.Builder();

        // SIMD path if no nulls
        if (!series.HasNulls && data.ChunkCount == 1)
        {
            var span = data.GetChunkSpan(0);
            VectorScalarOp(span, scalar, builder, op);
        }
        else
        {
            for (int i = 0; i < series.Length; i++)
            {
                if (series.IsNull(i))
                    builder.AppendNull();
                else
                    builder.Append(op(data.GetValue(i), scalar));
            }
        }

        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Float64);
    }

    // ============================================================================
    // SIMD Operations
    // ============================================================================

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void VectorAdd(ReadOnlySpan<double> left, ReadOnlySpan<double> right, Span<double> result)
    {
        int i = 0;

        // Use SIMD if available
        if (Vector.IsHardwareAccelerated && left.Length >= Vector<double>.Count)
        {
            var vectorCount = left.Length - (left.Length % Vector<double>.Count);
            for (; i < vectorCount; i += Vector<double>.Count)
            {
                var vLeft = new Vector<double>(left.Slice(i));
                var vRight = new Vector<double>(right.Slice(i));
                (vLeft + vRight).CopyTo(result.Slice(i));
            }
        }

        // Scalar remainder
        for (; i < left.Length; i++)
        {
            result[i] = left[i] + right[i];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void VectorSubtract(ReadOnlySpan<double> left, ReadOnlySpan<double> right, Span<double> result)
    {
        int i = 0;

        if (Vector.IsHardwareAccelerated && left.Length >= Vector<double>.Count)
        {
            var vectorCount = left.Length - (left.Length % Vector<double>.Count);
            for (; i < vectorCount; i += Vector<double>.Count)
            {
                var vLeft = new Vector<double>(left.Slice(i));
                var vRight = new Vector<double>(right.Slice(i));
                (vLeft - vRight).CopyTo(result.Slice(i));
            }
        }

        for (; i < left.Length; i++)
        {
            result[i] = left[i] - right[i];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void VectorMultiply(ReadOnlySpan<double> left, ReadOnlySpan<double> right, Span<double> result)
    {
        int i = 0;

        if (Vector.IsHardwareAccelerated && left.Length >= Vector<double>.Count)
        {
            var vectorCount = left.Length - (left.Length % Vector<double>.Count);
            for (; i < vectorCount; i += Vector<double>.Count)
            {
                var vLeft = new Vector<double>(left.Slice(i));
                var vRight = new Vector<double>(right.Slice(i));
                (vLeft * vRight).CopyTo(result.Slice(i));
            }
        }

        for (; i < left.Length; i++)
        {
            result[i] = left[i] * right[i];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void VectorDivide(ReadOnlySpan<double> left, ReadOnlySpan<double> right, Span<double> result)
    {
        int i = 0;

        if (Vector.IsHardwareAccelerated && left.Length >= Vector<double>.Count)
        {
            var vectorCount = left.Length - (left.Length % Vector<double>.Count);
            for (; i < vectorCount; i += Vector<double>.Count)
            {
                var vLeft = new Vector<double>(left.Slice(i));
                var vRight = new Vector<double>(right.Slice(i));
                (vLeft / vRight).CopyTo(result.Slice(i));
            }
        }

        for (; i < left.Length; i++)
        {
            result[i] = left[i] / right[i];
        }
    }

    private static void VectorScalarOp(ReadOnlySpan<double> data, double scalar, DoubleArray.Builder builder, Func<double, double, double> op)
    {
        // Use scalar operations for simplicity (SIMD optimization is used for binary ops)
        {
            for (int i = 0; i < data.Length; i++)
            {
                builder.Append(op(data[i], scalar));
            }
        }
    }

    // ============================================================================
    // Negate Operations
    // ============================================================================

    private static Series NegateInt32(Series series)
    {
        var data = series.Data as ChunkedArray<int>;
        if (data is null) throw new InvalidOperationException("Expected Int32 array");

        var builder = new Int32Array.Builder();
        for (int i = 0; i < series.Length; i++)
        {
            if (series.IsNull(i))
                builder.AppendNull();
            else
                builder.Append(-data.GetValue(i));
        }

        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Int32);
    }

    private static Series NegateInt64(Series series)
    {
        var data = series.Data as ChunkedArray<long>;
        if (data is null) throw new InvalidOperationException("Expected Int64 array");

        var builder = new Int64Array.Builder();
        for (int i = 0; i < series.Length; i++)
        {
            if (series.IsNull(i))
                builder.AppendNull();
            else
                builder.Append(-data.GetValue(i));
        }

        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Int64);
    }

    private static Series NegateFloat32(Series series)
    {
        var data = series.Data as ChunkedArray<float>;
        if (data is null) throw new InvalidOperationException("Expected Float32 array");

        var builder = new FloatArray.Builder();
        for (int i = 0; i < series.Length; i++)
        {
            if (series.IsNull(i))
                builder.AppendNull();
            else
                builder.Append(-data.GetValue(i));
        }

        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Float32);
    }

    private static Series NegateFloat64(Series series)
    {
        var data = series.Data as ChunkedArray<double>;
        if (data is null) throw new InvalidOperationException("Expected Float64 array");

        var builder = new DoubleArray.Builder();
        for (int i = 0; i < series.Length; i++)
        {
            if (series.IsNull(i))
                builder.AppendNull();
            else
                builder.Append(-data.GetValue(i));
        }

        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Float64);
    }

    // ============================================================================
    // Type Promotion
    // ============================================================================

    private static (Series left, Series right, DataType resultType) PromoteTypes(Series left, Series right)
    {
        var leftType = left.DataType;
        var rightType = right.DataType;

        if (leftType == rightType)
            return (left, right, leftType);

        // Numeric promotion rules
        var promotedType = PromoteNumericTypes(leftType, rightType);

        var promotedLeft = leftType == promotedType ? left : left.Cast(promotedType);
        var promotedRight = rightType == promotedType ? right : right.Cast(promotedType);

        return (promotedLeft, promotedRight, promotedType);
    }

    private static DataType PromoteNumericTypes(DataType left, DataType right)
    {
        // Float > Int, larger > smaller
        if (left is DataType.Float64Type || right is DataType.Float64Type)
            return DataType.Float64;
        if (left is DataType.Float32Type || right is DataType.Float32Type)
            return DataType.Float64; // Promote to float64 for precision
        if (left is DataType.Int64Type || right is DataType.Int64Type)
            return DataType.Int64;
        if (left is DataType.UInt64Type || right is DataType.UInt64Type)
            return DataType.Int64; // Could overflow, but safer
        if (left is DataType.Int32Type || right is DataType.Int32Type)
            return DataType.Int64;
        if (left is DataType.UInt32Type || right is DataType.UInt32Type)
            return DataType.Int64;

        return DataType.Int64; // Default
    }

    private static void ValidateShapes(Series left, Series right)
    {
        if (left.Length != right.Length)
            throw new ArgumentException($"Series lengths must match: {left.Length} vs {right.Length}");
    }
}
