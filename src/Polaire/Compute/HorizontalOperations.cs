// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using Polaire.DataTypes;

namespace Polaire.Compute;

/// <summary>
/// Row-wise (horizontal) reductions across several numeric series, matching Polars'
/// sum_horizontal, mean_horizontal, min_horizontal and max_horizontal with nulls ignored.
/// The result is named after the first input. Length-1 inputs are broadcast.
/// </summary>
public static class HorizontalOperations
{
    /// <summary>Row-wise sum. Integer inputs give Int64, otherwise Float64. An all-null row sums to 0.</summary>
    public static Series Sum(IReadOnlyList<Series> inputs)
    {
        var (height, allInteger) = Validate(inputs, "sum_horizontal");
        if (allInteger)
        {
            var result = new long?[height];
            for (int row = 0; row < height; row++)
            {
                long sum = 0;
                foreach (var s in inputs)
                {
                    var v = ValueAt(s, row);
                    if (!v.IsNull) sum = checked(sum + v.Cast(DataType.Int64).AsInt64());
                }
                result[row] = sum;
            }
            return Series.FromNullable(inputs[0].Name, result);
        }
        else
        {
            var result = new double?[height];
            for (int row = 0; row < height; row++)
            {
                double sum = 0;
                foreach (var s in inputs)
                {
                    var v = ValueAt(s, row);
                    if (!v.IsNull) sum += v.Cast(DataType.Float64).AsFloat64();
                }
                result[row] = sum;
            }
            return Series.FromNullable(inputs[0].Name, result);
        }
    }

    /// <summary>Row-wise mean of the non-null values (Float64). An all-null row is null.</summary>
    public static Series Mean(IReadOnlyList<Series> inputs)
    {
        var (height, _) = Validate(inputs, "mean_horizontal");
        var result = new double?[height];
        for (int row = 0; row < height; row++)
        {
            double sum = 0;
            int count = 0;
            foreach (var s in inputs)
            {
                var v = ValueAt(s, row);
                if (v.IsNull) continue;
                sum += v.Cast(DataType.Float64).AsFloat64();
                count++;
            }
            result[row] = count == 0 ? null : sum / count;
        }
        return Series.FromNullable(inputs[0].Name, result);
    }

    /// <summary>Row-wise minimum. Integer inputs give Int64, otherwise Float64. An all-null row is null.</summary>
    public static Series Min(IReadOnlyList<Series> inputs) => Extreme(inputs, "min_horizontal", takeLower: true);

    /// <summary>Row-wise maximum. Integer inputs give Int64, otherwise Float64. An all-null row is null.</summary>
    public static Series Max(IReadOnlyList<Series> inputs) => Extreme(inputs, "max_horizontal", takeLower: false);

    private static Series Extreme(IReadOnlyList<Series> inputs, string op, bool takeLower)
    {
        var (height, allInteger) = Validate(inputs, op);
        if (allInteger)
        {
            var result = new long?[height];
            for (int row = 0; row < height; row++)
            {
                long? best = null;
                foreach (var s in inputs)
                {
                    var v = ValueAt(s, row);
                    if (v.IsNull) continue;
                    var x = v.Cast(DataType.Int64).AsInt64();
                    if (best is null || (takeLower ? x < best : x > best)) best = x;
                }
                result[row] = best;
            }
            return Series.FromNullable(inputs[0].Name, result);
        }
        else
        {
            var result = new double?[height];
            for (int row = 0; row < height; row++)
            {
                double? best = null;
                foreach (var s in inputs)
                {
                    var v = ValueAt(s, row);
                    if (v.IsNull) continue;
                    var x = v.Cast(DataType.Float64).AsFloat64();
                    // NaN is skipped, matching the SIMD Min/Max aggregations
                    if (double.IsNaN(x)) continue;
                    if (best is null || (takeLower ? x < best : x > best)) best = x;
                }
                result[row] = best;
            }
            return Series.FromNullable(inputs[0].Name, result);
        }
    }

    private static (int Height, bool AllInteger) Validate(IReadOnlyList<Series> inputs, string op)
    {
        if (inputs.Count == 0)
            throw new ArgumentException($"{op} requires at least one input");

        foreach (var s in inputs)
        {
            if (!s.DataType.IsNumeric || s.DataType is DataType.DecimalType)
                throw new ArgumentException($"{op} requires numeric inputs, but '{s.Name}' is {s.DataType}");
        }

        var height = inputs.Max(s => s.Length);
        foreach (var s in inputs)
        {
            if (s.Length != height && s.Length != 1)
                throw new ArgumentException(
                    $"{op} inputs must have equal lengths, but '{s.Name}' has {s.Length} rows and the longest input has {height}");
        }

        return (height, inputs.All(s => s.DataType.IsInteger));
    }

    private static AnyValue ValueAt(Series s, int row) => s.Length == 1 ? s[0] : s[row];
}
