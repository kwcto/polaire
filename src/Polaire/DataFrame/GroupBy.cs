// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using Polaire.DataTypes;

using Polaire.Compute;

namespace Polaire;

/// <summary>
/// Represents a grouped DataFrame for aggregation operations.
/// </summary>
public sealed class GroupBy
{
    private readonly DataFrame _df;
    private readonly string[] _groupColumns;
    private readonly Dictionary<string, List<int>> _groups;

    internal GroupBy(DataFrame df, string[] groupColumns)
    {
        _df = df;
        _groupColumns = groupColumns;
        _groups = ComputeGroups();
    }

    private Dictionary<string, List<int>> ComputeGroups()
    {
        var groups = new Dictionary<string, List<int>>();

        for (int i = 0; i < _df.Height; i++)
        {
            // Create group key from group column values
            var keyParts = _groupColumns.Select(col => _df[col][i].ToString()).ToArray();
            var key = string.Join("|", keyParts);

            if (!groups.TryGetValue(key, out var indices))
            {
                indices = new List<int>();
                groups[key] = indices;
            }
            indices.Add(i);
        }

        return groups;
    }

    /// <summary>Gets the number of groups.</summary>
    public int GroupCount => _groups.Count;

    /// <summary>Gets the group keys.</summary>
    public IReadOnlyList<string> Groups => _groups.Keys.ToArray();

    // ============================================================================
    // Aggregation Operations
    // ============================================================================

    /// <summary>Counts rows per group.</summary>
    public DataFrame Count()
    {
        return Aggregate("count", series => AnyValue.From(series.Count()));
    }

    /// <summary>Sums values per group.</summary>
    public DataFrame Sum()
    {
        return AggregateNumeric("sum", series => series.Sum());
    }

    /// <summary>Computes mean per group.</summary>
    public DataFrame Mean()
    {
        return AggregateNumeric("mean", series => series.Mean());
    }

    /// <summary>Computes median per group.</summary>
    public DataFrame Median()
    {
        return AggregateNumeric("median", series => series.Median());
    }

    /// <summary>Finds minimum per group.</summary>
    public DataFrame Min()
    {
        return AggregateNumeric("min", series => series.Min());
    }

    /// <summary>Finds maximum per group.</summary>
    public DataFrame Max()
    {
        return AggregateNumeric("max", series => series.Max());
    }

    /// <summary>Computes standard deviation per group.</summary>
    public DataFrame Std()
    {
        return AggregateNumeric("std", series => series.Std());
    }

    /// <summary>Computes variance per group.</summary>
    public DataFrame Var()
    {
        return AggregateNumeric("var", series => series.Var());
    }

    /// <summary>Gets first value per group.</summary>
    public DataFrame First()
    {
        return Aggregate("first", series => series.First());
    }

    /// <summary>Gets last value per group.</summary>
    public DataFrame Last()
    {
        return Aggregate("last", series => series.Last());
    }

    /// <summary>Counts unique values per group.</summary>
    public DataFrame NUnique()
    {
        return Aggregate("nunique", series => AnyValue.From(series.Unique().Length));
    }

    /// <summary>Applies custom aggregation.</summary>
    public DataFrame Agg(params (string column, string aggName, Func<Series, AnyValue> agg, string? alias)[] aggregations)
    {
        var resultColumns = new List<Series>();

        // Group key columns
        foreach (var groupCol in _groupColumns)
        {
            var values = new List<AnyValue>();
            foreach (var kvp in _groups)
            {
                var firstIndex = kvp.Value[0];
                values.Add(_df[groupCol][firstIndex]);
            }
            resultColumns.Add(BuildSeriesFromAnyValues(groupCol, values, _df[groupCol].DataType));
        }

        // Aggregated columns
        foreach (var (column, aggName, agg, alias) in aggregations)
        {
            var values = new List<AnyValue>();
            var sourceCol = _df[column];

            foreach (var kvp in _groups)
            {
                var indices = kvp.Value.ToArray();
                var groupSeries = SeriesOperations.Take(sourceCol, indices);
                values.Add(agg(groupSeries));
            }

            var resultName = alias ?? $"{column}_{aggName}";
            resultColumns.Add(BuildSeriesFromAnyValues(resultName, values, InferResultType(sourceCol.DataType, aggName)));
        }

        return new DataFrame(resultColumns);
    }

    /// <summary>Applies multiple aggregations.</summary>
    public DataFrame Agg(Dictionary<string, List<string>> columnAggs)
    {
        var aggregations = new List<(string, string, Func<Series, AnyValue>, string?)>();

        foreach (var kvp in columnAggs)
        {
            var column = kvp.Key;
            foreach (var aggName in kvp.Value)
            {
                var agg = GetAggregation(aggName);
                aggregations.Add((column, aggName, agg, null));
            }
        }

        return Agg(aggregations.ToArray());
    }

    private Func<Series, AnyValue> GetAggregation(string name) => name.ToLower() switch
    {
        "count" => s => AnyValue.From(s.Count()),
        "sum" => s => s.Sum(),
        "mean" => s => s.Mean(),
        "median" => s => s.Median(),
        "min" => s => s.Min(),
        "max" => s => s.Max(),
        "std" => s => s.Std(),
        "var" => s => s.Var(),
        "first" => s => s.First(),
        "last" => s => s.Last(),
        "nunique" => s => AnyValue.From(s.Unique().Length),
        _ => throw new ArgumentException($"Unknown aggregation: {name}")
    };

    // ============================================================================
    // Helpers
    // ============================================================================

    private DataFrame Aggregate(string aggName, Func<Series, AnyValue> agg)
    {
        var resultColumns = new List<Series>();

        // Group key columns
        foreach (var groupCol in _groupColumns)
        {
            var values = new List<AnyValue>();
            foreach (var kvp in _groups)
            {
                var firstIndex = kvp.Value[0];
                values.Add(_df[groupCol][firstIndex]);
            }
            resultColumns.Add(BuildSeriesFromAnyValues(groupCol, values, _df[groupCol].DataType));
        }

        // Aggregated value columns (all non-group columns)
        var valueColumns = _df.Columns.Where(c => !_groupColumns.Contains(c)).ToArray();
        foreach (var colName in valueColumns)
        {
            var sourceCol = _df[colName];
            var values = new List<AnyValue>();

            foreach (var kvp in _groups)
            {
                var indices = kvp.Value.ToArray();
                var groupSeries = SeriesOperations.Take(sourceCol, indices);
                values.Add(agg(groupSeries));
            }

            var resultType = InferResultType(sourceCol.DataType, aggName);
            resultColumns.Add(BuildSeriesFromAnyValues(colName, values, resultType));
        }

        return new DataFrame(resultColumns);
    }

    private DataFrame AggregateNumeric(string aggName, Func<Series, AnyValue> agg)
    {
        var resultColumns = new List<Series>();

        // Group key columns
        foreach (var groupCol in _groupColumns)
        {
            var values = new List<AnyValue>();
            foreach (var kvp in _groups)
            {
                var firstIndex = kvp.Value[0];
                values.Add(_df[groupCol][firstIndex]);
            }
            resultColumns.Add(BuildSeriesFromAnyValues(groupCol, values, _df[groupCol].DataType));
        }

        // Aggregated numeric columns
        var numericColumns = _df.Columns
            .Where(c => !_groupColumns.Contains(c) && _df[c].DataType.IsNumeric)
            .ToArray();

        foreach (var colName in numericColumns)
        {
            var sourceCol = _df[colName];
            var values = new List<AnyValue>();

            foreach (var kvp in _groups)
            {
                var indices = kvp.Value.ToArray();
                var groupSeries = SeriesOperations.Take(sourceCol, indices);
                values.Add(agg(groupSeries));
            }

            var resultType = InferResultType(sourceCol.DataType, aggName);
            resultColumns.Add(BuildSeriesFromAnyValues(colName, values, resultType));
        }

        return new DataFrame(resultColumns);
    }

    private static DataType InferResultType(DataType sourceType, string aggName) => aggName switch
    {
        "count" or "nunique" => DataType.Int32,
        "mean" or "std" or "var" or "median" => DataType.Float64,
        "sum" when sourceType.IsFloat => DataType.Float64,
        "sum" => DataType.Int64,
        _ => sourceType
    };

    private static Series BuildSeriesFromAnyValues(string name, List<AnyValue> values, DataType dtype)
    {
        return dtype switch
        {
            DataType.Int32Type => Series.FromNullable(name, values.Select(v => v.IsNull ? null : (int?)v.AsInt32()).ToArray()),
            DataType.Int64Type => Series.FromNullable(name, values.Select(v => v.IsNull ? null : (long?)v.AsInt64()).ToArray()),
            DataType.Float32Type => Series.FromNullable(name, values.Select(v => v.IsNull ? null : (float?)v.AsFloat32()).ToArray()),
            DataType.Float64Type => Series.FromNullable(name, values.Select(v =>
            {
                if (v.IsNull) return null;
                if (v.TryGetDouble(out var d)) return (double?)d;
                if (v.TryGetInt64(out var l)) return (double?)l;
                return null;
            }).ToArray()),
            DataType.BooleanType => Series.FromNullable(name, values.Select(v => v.IsNull ? null : (bool?)v.AsBoolean()).ToArray()),
            DataType.StringType => Series.FromValues(name, values.Select(v => v.IsNull ? null : v.AsString()).ToArray()),
            _ => Series.FromValues(name, values.Select(v => v.ToString()).ToArray())
        };
    }

    // ============================================================================
    // Iteration
    // ============================================================================

    /// <summary>Iterates over groups.</summary>
    public IEnumerable<(string Key, DataFrame Group)> Iter()
    {
        foreach (var kvp in _groups)
        {
            var indices = kvp.Value.ToArray();
            yield return (kvp.Key, _df.Take(indices));
        }
    }

    /// <summary>Gets a specific group.</summary>
    public DataFrame GetGroup(params AnyValue[] keys)
    {
        var key = string.Join("|", keys.Select(k => k.ToString()));
        if (!_groups.TryGetValue(key, out var indices))
            throw new KeyNotFoundException($"Group '{key}' not found");
        return _df.Take(indices.ToArray());
    }
}
