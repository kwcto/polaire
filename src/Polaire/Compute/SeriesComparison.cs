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
/// Comparison operations for Series.
/// </summary>
public static class SeriesComparison
{
    // ============================================================================
    // Series vs Series Comparisons
    // ============================================================================

    public static Series Equal(Series left, Series right)
    {
        ValidateShapes(left, right);
        return CompareOp(left, right, "eq", (a, b) => a == b, (a, b) => a == b, (a, b) => a.Equals(b, StringComparison.Ordinal));
    }

    public static Series NotEqual(Series left, Series right)
    {
        ValidateShapes(left, right);
        return CompareOp(left, right, "ne", (a, b) => a != b, (a, b) => a != b, (a, b) => !a.Equals(b, StringComparison.Ordinal));
    }

    public static Series LessThan(Series left, Series right)
    {
        ValidateShapes(left, right);
        return CompareOp(left, right, "lt", (a, b) => a < b, (a, b) => a < b, (a, b) => string.Compare(a, b, StringComparison.Ordinal) < 0);
    }

    public static Series LessThanOrEqual(Series left, Series right)
    {
        ValidateShapes(left, right);
        return CompareOp(left, right, "le", (a, b) => a <= b, (a, b) => a <= b, (a, b) => string.Compare(a, b, StringComparison.Ordinal) <= 0);
    }

    public static Series GreaterThan(Series left, Series right)
    {
        ValidateShapes(left, right);
        return CompareOp(left, right, "gt", (a, b) => a > b, (a, b) => a > b, (a, b) => string.Compare(a, b, StringComparison.Ordinal) > 0);
    }

    public static Series GreaterThanOrEqual(Series left, Series right)
    {
        ValidateShapes(left, right);
        return CompareOp(left, right, "ge", (a, b) => a >= b, (a, b) => a >= b, (a, b) => string.Compare(a, b, StringComparison.Ordinal) >= 0);
    }

    // ============================================================================
    // Series vs Scalar Comparisons
    // ============================================================================

    public static Series EqualScalar(Series series, AnyValue value)
    {
        return CompareScalarOp(series, value, "eq", (a, b) => a == b, (a, b) => a == b, (a, b) => a?.Equals(b, StringComparison.Ordinal) ?? false);
    }

    public static Series NotEqualScalar(Series series, AnyValue value)
    {
        return CompareScalarOp(series, value, "ne", (a, b) => a != b, (a, b) => a != b, (a, b) => !(a?.Equals(b, StringComparison.Ordinal) ?? false));
    }

    public static Series LessThanScalar(Series series, AnyValue value)
    {
        return CompareScalarOp(series, value, "lt", (a, b) => a < b, (a, b) => a < b, (a, b) => string.Compare(a, b, StringComparison.Ordinal) < 0);
    }

    public static Series LessThanOrEqualScalar(Series series, AnyValue value)
    {
        return CompareScalarOp(series, value, "le", (a, b) => a <= b, (a, b) => a <= b, (a, b) => string.Compare(a, b, StringComparison.Ordinal) <= 0);
    }

    public static Series GreaterThanScalar(Series series, AnyValue value)
    {
        return CompareScalarOp(series, value, "gt", (a, b) => a > b, (a, b) => a > b, (a, b) => string.Compare(a, b, StringComparison.Ordinal) > 0);
    }

    public static Series GreaterThanOrEqualScalar(Series series, AnyValue value)
    {
        return CompareScalarOp(series, value, "ge", (a, b) => a >= b, (a, b) => a >= b, (a, b) => string.Compare(a, b, StringComparison.Ordinal) >= 0);
    }

    // ============================================================================
    // Null Checks
    // ============================================================================

    public static Series IsNull(Series series)
    {
        var builder = new BooleanArray.Builder();
        for (int i = 0; i < series.Length; i++)
        {
            builder.Append(series.IsNull(i));
        }
        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Boolean);
    }

    public static Series IsNotNull(Series series)
    {
        var builder = new BooleanArray.Builder();
        for (int i = 0; i < series.Length; i++)
        {
            builder.Append(!series.IsNull(i));
        }
        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Boolean);
    }

    // ============================================================================
    // NaN Checks (for floating point)
    // ============================================================================

    public static Series IsNaN(Series series)
    {
        var builder = new BooleanArray.Builder();

        if (series.DataType is DataType.Float32Type)
        {
            var data = series.Data as ChunkedArray<float>;
            for (int i = 0; i < series.Length; i++)
            {
                if (series.IsNull(i))
                    builder.Append(false);
                else
                    builder.Append(float.IsNaN(data!.GetValue(i)));
            }
        }
        else if (series.DataType is DataType.Float64Type)
        {
            var data = series.Data as ChunkedArray<double>;
            for (int i = 0; i < series.Length; i++)
            {
                if (series.IsNull(i))
                    builder.Append(false);
                else
                    builder.Append(double.IsNaN(data!.GetValue(i)));
            }
        }
        else
        {
            // Non-float types are never NaN
            for (int i = 0; i < series.Length; i++)
                builder.Append(false);
        }

        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Boolean);
    }

    public static Series IsNotNaN(Series series)
    {
        var builder = new BooleanArray.Builder();

        if (series.DataType is DataType.Float32Type)
        {
            var data = series.Data as ChunkedArray<float>;
            for (int i = 0; i < series.Length; i++)
            {
                if (series.IsNull(i))
                    builder.Append(true); // null is not NaN
                else
                    builder.Append(!float.IsNaN(data!.GetValue(i)));
            }
        }
        else if (series.DataType is DataType.Float64Type)
        {
            var data = series.Data as ChunkedArray<double>;
            for (int i = 0; i < series.Length; i++)
            {
                if (series.IsNull(i))
                    builder.Append(true);
                else
                    builder.Append(!double.IsNaN(data!.GetValue(i)));
            }
        }
        else
        {
            for (int i = 0; i < series.Length; i++)
                builder.Append(true);
        }

        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Boolean);
    }

    // ============================================================================
    // IsIn
    // ============================================================================

    public static Series IsIn(Series series, params AnyValue[] values)
    {
        var valueSet = new HashSet<AnyValue>(values);
        var builder = new BooleanArray.Builder();

        for (int i = 0; i < series.Length; i++)
        {
            if (series.IsNull(i))
            {
                builder.Append(valueSet.Contains(AnyValue.Null));
            }
            else
            {
                builder.Append(valueSet.Contains(series[i]));
            }
        }

        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Boolean);
    }

    // ============================================================================
    // Between
    // ============================================================================

    public static Series Between(Series series, AnyValue lower, AnyValue upper, bool includeBounds = true)
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
                bool result;
                if (includeBounds)
                    result = val >= lower && val <= upper;
                else
                    result = val > lower && val < upper;
                builder.Append(result);
            }
        }

        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Boolean);
    }

    // ============================================================================
    // Implementation
    // ============================================================================

    private static Series CompareOp(
        Series left, Series right, string opName,
        Func<long, long, bool> int64Op,
        Func<double, double, bool> float64Op,
        Func<string?, string?, bool> stringOp)
    {
        var builder = new BooleanArray.Builder();

        for (int i = 0; i < left.Length; i++)
        {
            if (left.IsNull(i) || right.IsNull(i))
            {
                // Comparison with null yields null (except for eq/ne in some contexts)
                if (opName == "eq")
                    builder.Append(left.IsNull(i) && right.IsNull(i));
                else if (opName == "ne")
                    builder.Append(!(left.IsNull(i) && right.IsNull(i)));
                else
                    builder.AppendNull();
            }
            else
            {
                bool result = CompareValues(left[i], right[i], int64Op, float64Op, stringOp);
                builder.Append(result);
            }
        }

        return Series.FromArrowArray(left.Name, builder.Build(), DataType.Boolean);
    }

    private static Series CompareScalarOp(
        Series series, AnyValue scalar, string opName,
        Func<long, long, bool> int64Op,
        Func<double, double, bool> float64Op,
        Func<string?, string?, bool> stringOp)
    {
        var builder = new BooleanArray.Builder();

        for (int i = 0; i < series.Length; i++)
        {
            if (series.IsNull(i))
            {
                if (opName == "eq")
                    builder.Append(scalar.IsNull);
                else if (opName == "ne")
                    builder.Append(!scalar.IsNull);
                else
                    builder.AppendNull();
            }
            else if (scalar.IsNull)
            {
                if (opName == "eq")
                    builder.Append(false);
                else if (opName == "ne")
                    builder.Append(true);
                else
                    builder.AppendNull();
            }
            else
            {
                bool result = CompareValues(series[i], scalar, int64Op, float64Op, stringOp);
                builder.Append(result);
            }
        }

        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Boolean);
    }

    private static bool CompareValues(
        AnyValue left, AnyValue right,
        Func<long, long, bool> int64Op,
        Func<double, double, bool> float64Op,
        Func<string?, string?, bool> stringOp)
    {
        // Try numeric comparison first
        if (left.TryGetDouble(out var leftDouble) && right.TryGetDouble(out var rightDouble))
        {
            return float64Op(leftDouble, rightDouble);
        }

        if (left.TryGetInt64(out var leftInt) && right.TryGetInt64(out var rightInt))
        {
            return int64Op(leftInt, rightInt);
        }

        // String comparison
        if (left.TryGetString(out var leftStr) && right.TryGetString(out var rightStr))
        {
            return stringOp(leftStr, rightStr);
        }

        // Fallback to AnyValue comparison
        return left.Equals(right);
    }

    private static void ValidateShapes(Series left, Series right)
    {
        if (left.Length != right.Length)
            throw new ArgumentException($"Series lengths must match: {left.Length} vs {right.Length}");
    }
}
