// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using System.Numerics;
using System.Runtime.CompilerServices;
using Apache.Arrow;
using Polaire.DataTypes;
using Polaire.Core;
using Polaire.Series;

namespace Polaire.Compute;

/// <summary>
/// Boolean operations for Series.
/// </summary>
public static class SeriesBoolean
{
    // ============================================================================
    // Binary Boolean Operations
    // ============================================================================

    public static Series And(Series left, Series right)
    {
        ValidateBooleanSeries(left, right);
        return BooleanBinaryOp(left, right, (a, b) => a && b);
    }

    public static Series Or(Series left, Series right)
    {
        ValidateBooleanSeries(left, right);
        return BooleanBinaryOp(left, right, (a, b) => a || b);
    }

    public static Series Xor(Series left, Series right)
    {
        ValidateBooleanSeries(left, right);
        return BooleanBinaryOp(left, right, (a, b) => a ^ b);
    }

    // ============================================================================
    // Unary Boolean Operations
    // ============================================================================

    public static Series Not(Series series)
    {
        if (series.DataType is not DataType.BooleanType)
            throw new ArgumentException("Not operation requires boolean series");

        var data = series.Data as ChunkedArray<bool>;
        var builder = new BooleanArray.Builder();

        for (int i = 0; i < series.Length; i++)
        {
            if (series.IsNull(i))
                builder.AppendNull();
            else
                builder.Append(!data!.GetValue(i));
        }

        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Boolean);
    }

    // ============================================================================
    // Reductions
    // ============================================================================

    public static bool All(Series series)
    {
        if (series.DataType is not DataType.BooleanType)
            throw new ArgumentException("All operation requires boolean series");

        var data = series.Data as ChunkedArray<bool>;

        for (int i = 0; i < series.Length; i++)
        {
            if (series.IsNull(i))
                continue; // Skip nulls
            if (!data!.GetValue(i))
                return false;
        }

        return true;
    }

    public static bool Any(Series series)
    {
        if (series.DataType is not DataType.BooleanType)
            throw new ArgumentException("Any operation requires boolean series");

        var data = series.Data as ChunkedArray<bool>;

        for (int i = 0; i < series.Length; i++)
        {
            if (series.IsNull(i))
                continue;
            if (data!.GetValue(i))
                return true;
        }

        return false;
    }

    // ============================================================================
    // Implementation
    // ============================================================================

    private static Series BooleanBinaryOp(Series left, Series right, Func<bool, bool, bool> op)
    {
        var leftData = left.Data as ChunkedArray<bool>;
        var rightData = right.Data as ChunkedArray<bool>;
        var builder = new BooleanArray.Builder();

        for (int i = 0; i < left.Length; i++)
        {
            if (left.IsNull(i) || right.IsNull(i))
            {
                builder.AppendNull();
            }
            else
            {
                builder.Append(op(leftData!.GetValue(i), rightData!.GetValue(i)));
            }
        }

        return Series.FromArrowArray(left.Name, builder.Build(), DataType.Boolean);
    }

    private static void ValidateBooleanSeries(Series left, Series right)
    {
        if (left.DataType is not DataType.BooleanType)
            throw new ArgumentException("Left series must be boolean");
        if (right.DataType is not DataType.BooleanType)
            throw new ArgumentException("Right series must be boolean");
        if (left.Length != right.Length)
            throw new ArgumentException($"Series lengths must match: {left.Length} vs {right.Length}");
    }
}
