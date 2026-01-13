// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using System.Collections;
using System.Text;
using Polaire.DataTypes;

using Polaire.Compute;

namespace Polaire;

/// <summary>
/// A two-dimensional tabular data structure with labeled columns.
/// The primary data structure in Polaire, equivalent to a SQL table or spreadsheet.
/// </summary>
public sealed class DataFrame : IEnumerable<Series>
{
    private readonly Dictionary<string, int> _columnIndex;
    private readonly Series[] _columns;
    private readonly int _height;

    // ============================================================================
    // Constructors
    // ============================================================================

    public DataFrame(IEnumerable<Series> columns)
    {
        _columns = columns.ToArray();

        if (_columns.Length == 0)
        {
            _height = 0;
            _columnIndex = new Dictionary<string, int>();
            return;
        }

        _height = _columns[0].Length;
        _columnIndex = new Dictionary<string, int>();

        for (int i = 0; i < _columns.Length; i++)
        {
            if (_columns[i].Length != _height)
                throw new ArgumentException($"All columns must have same length. Expected {_height}, got {_columns[i].Length} for column '{_columns[i].Name}'");

            if (_columnIndex.ContainsKey(_columns[i].Name))
                throw new ArgumentException($"Duplicate column name: {_columns[i].Name}");

            _columnIndex[_columns[i].Name] = i;
        }
    }

    public DataFrame(params Series[] columns) : this((IEnumerable<Series>)columns)
    {
    }

    // ============================================================================
    // Static Factory Methods
    // ============================================================================

    /// <summary>Creates an empty DataFrame with the given schema.</summary>
    public static DataFrame Empty(params (string name, DataType type)[] schema)
    {
        var columns = schema.Select(s => CreateEmptySeries(s.name, s.type)).ToArray();
        return new DataFrame(columns);
    }

    private static Series CreateEmptySeries(string name, DataType type)
    {
        return type switch
        {
            DataType.Int32Type => Series.FromValues(name, Array.Empty<int>()),
            DataType.Int64Type => Series.FromValues(name, Array.Empty<long>()),
            DataType.Float64Type => Series.FromValues(name, Array.Empty<double>()),
            DataType.StringType => Series.FromValues(name, Array.Empty<string?>()),
            DataType.BooleanType => Series.FromValues(name, Array.Empty<bool>()),
            _ => Series.FromValues(name, Array.Empty<int>())
        };
    }

    /// <summary>Creates a DataFrame from a dictionary of column name -> values.</summary>
    public static DataFrame FromDictionary(Dictionary<string, object[]> data)
    {
        var columns = new List<Series>();
        int? expectedLength = null;

        foreach (var kvp in data)
        {
            if (expectedLength.HasValue && kvp.Value.Length != expectedLength.Value)
                throw new ArgumentException($"All columns must have same length");

            expectedLength = kvp.Value.Length;
            var series = CreateSeriesFromObjects(kvp.Key, kvp.Value);
            columns.Add(series);
        }

        return new DataFrame(columns);
    }

    private static Series CreateSeriesFromObjects(string name, object[] values)
    {
        if (values.Length == 0)
            return Series.FromValues(name, Array.Empty<int>());

        var firstNonNull = values.FirstOrDefault(v => v != null);
        if (firstNonNull == null)
            return Series.FromValues(name, values.Select(_ => (string?)null).ToArray());

        return firstNonNull switch
        {
            int => Series.FromValues(name, values.Cast<int>().ToArray()),
            long => Series.FromValues(name, values.Cast<long>().ToArray()),
            float => Series.FromValues(name, values.Cast<float>().ToArray()),
            double => Series.FromValues(name, values.Cast<double>().ToArray()),
            string => Series.FromValues(name, values.Cast<string>().ToArray()),
            bool => Series.FromValues(name, values.Cast<bool>().ToArray()),
            DateTime => Series.FromValues(name, values.Cast<DateTime>().ToArray()),
            DateOnly => Series.FromValues(name, values.Cast<DateOnly>().ToArray()),
            _ => Series.FromValues(name, values.Select(v => v?.ToString()).ToArray())
        };
    }

    // ============================================================================
    // Properties
    // ============================================================================

    /// <summary>Gets the number of rows in this DataFrame.</summary>
    public int Height => _height;

    /// <summary>Gets the number of columns in this DataFrame.</summary>
    public int Width => _columns.Length;

    /// <summary>Gets the shape as (rows, columns).</summary>
    public (int Rows, int Columns) Shape => (_height, _columns.Length);

    /// <summary>Gets the column names.</summary>
    public IReadOnlyList<string> Columns => _columns.Select(c => c.Name).ToArray();

    /// <summary>Gets the data types of each column.</summary>
    public IReadOnlyList<DataType> Dtypes => _columns.Select(c => c.DataType).ToArray();

    /// <summary>Gets the schema as (name, type) pairs.</summary>
    public IReadOnlyList<(string Name, DataType Type)> Schema =>
        _columns.Select(c => (c.Name, c.DataType)).ToArray();

    /// <summary>Gets a column by name.</summary>
    public Series this[string name]
    {
        get
        {
            if (!_columnIndex.TryGetValue(name, out var index))
                throw new KeyNotFoundException($"Column '{name}' not found");
            return _columns[index];
        }
    }

    /// <summary>Gets a column by index.</summary>
    public Series this[int index] => _columns[index];

    /// <summary>Gets multiple columns by name.</summary>
    public DataFrame this[params string[] names] => Select(names);

    // ============================================================================
    // Column Selection
    // ============================================================================

    /// <summary>Selects specific columns by name.</summary>
    public DataFrame Select(params string[] columnNames)
    {
        var selected = columnNames.Select(name => this[name]).ToArray();
        return new DataFrame(selected);
    }

    /// <summary>Selects columns matching a predicate.</summary>
    public DataFrame SelectIf(Func<Series, bool> predicate)
    {
        var selected = _columns.Where(predicate).ToArray();
        return new DataFrame(selected);
    }

    /// <summary>Excludes specific columns.</summary>
    public DataFrame Drop(params string[] columnNames)
    {
        var toDrop = new HashSet<string>(columnNames);
        var remaining = _columns.Where(c => !toDrop.Contains(c.Name)).ToArray();
        return new DataFrame(remaining);
    }

    /// <summary>Renames columns using a mapping.</summary>
    public DataFrame Rename(Dictionary<string, string> mapping)
    {
        var renamed = _columns.Select(c =>
            mapping.TryGetValue(c.Name, out var newName) ? c.Rename(newName) : c
        ).ToArray();
        return new DataFrame(renamed);
    }

    // ============================================================================
    // Row Selection
    // ============================================================================

    /// <summary>Gets the first n rows.</summary>
    public DataFrame Head(int n = 5)
    {
        n = Math.Min(n, _height);
        var sliced = _columns.Select(c => c.Slice(0, n)).ToArray();
        return new DataFrame(sliced);
    }

    /// <summary>Gets the last n rows.</summary>
    public DataFrame Tail(int n = 5)
    {
        n = Math.Min(n, _height);
        var offset = Math.Max(0, _height - n);
        var sliced = _columns.Select(c => c.Slice(offset, n)).ToArray();
        return new DataFrame(sliced);
    }

    /// <summary>Gets a slice of rows.</summary>
    public DataFrame Slice(int offset, int length)
    {
        var sliced = _columns.Select(c => c.Slice(offset, length)).ToArray();
        return new DataFrame(sliced);
    }

    /// <summary>Filters rows based on a boolean mask.</summary>
    public DataFrame Filter(Series mask)
    {
        if (mask.DataType is not DataType.BooleanType)
            throw new ArgumentException("Filter mask must be boolean");
        if (mask.Length != _height)
            throw new ArgumentException("Mask length must match DataFrame height");

        var filtered = _columns.Select(c => SeriesOperations.Filter(c, mask)).ToArray();
        return new DataFrame(filtered);
    }

    /// <summary>Gets rows at specific indices.</summary>
    public DataFrame Take(int[] indices)
    {
        var taken = _columns.Select(c => SeriesOperations.Take(c, indices)).ToArray();
        return new DataFrame(taken);
    }

    /// <summary>Samples random rows.</summary>
    public DataFrame Sample(int n, int? seed = null)
    {
        var rng = seed.HasValue ? new Random(seed.Value) : new Random();
        var indices = Enumerable.Range(0, _height)
            .OrderBy(_ => rng.Next())
            .Take(Math.Min(n, _height))
            .ToArray();
        return Take(indices);
    }

    /// <summary>Samples a fraction of rows.</summary>
    public DataFrame Sample(double fraction, int? seed = null)
    {
        int n = (int)(_height * fraction);
        return Sample(n, seed);
    }

    // ============================================================================
    // Column Manipulation
    // ============================================================================

    /// <summary>Adds a new column or replaces an existing one.</summary>
    public DataFrame WithColumn(Series column)
    {
        var columns = new List<Series>(_columns);

        if (_columnIndex.TryGetValue(column.Name, out var existingIndex))
        {
            columns[existingIndex] = column;
        }
        else
        {
            columns.Add(column);
        }

        return new DataFrame(columns);
    }

    /// <summary>Adds multiple columns.</summary>
    public DataFrame WithColumns(params Series[] columns)
    {
        var result = this;
        foreach (var col in columns)
        {
            result = result.WithColumn(col);
        }
        return result;
    }

    /// <summary>Adds a column derived from existing data.</summary>
    public DataFrame WithColumn(string name, Func<DataFrame, Series> expression)
    {
        var newColumn = expression(this).Rename(name);
        return WithColumn(newColumn);
    }

    /// <summary>Adds a row number column.</summary>
    public DataFrame WithRowNumber(string name = "row_nr")
    {
        var indices = Enumerable.Range(0, _height).ToArray();
        var rowNumSeries = Series.FromValues(name, indices);
        return WithColumn(rowNumSeries);
    }

    // ============================================================================
    // Sorting
    // ============================================================================

    /// <summary>Sorts by a single column.</summary>
    public DataFrame Sort(string column, bool descending = false, bool nullsLast = true)
    {
        var indices = SeriesOperations.ArgSort(this[column], descending, nullsLast);
        var indexArray = indices.ToArray<int>();
        return Take(indexArray);
    }

    /// <summary>Sorts by multiple columns.</summary>
    public DataFrame Sort(params (string column, bool descending)[] sortSpec)
    {
        if (sortSpec.Length == 0)
            return this;

        // Stable multi-column sort
        var indices = Enumerable.Range(0, _height).ToArray();

        // Sort from last to first column (stable sort property)
        for (int i = sortSpec.Length - 1; i >= 0; i--)
        {
            var (column, desc) = sortSpec[i];
            var col = this[column];

            Array.Sort(indices, (a, b) =>
            {
                var aNull = col.IsNull(a);
                var bNull = col.IsNull(b);
                if (aNull && bNull) return 0;
                if (aNull) return 1;
                if (bNull) return -1;

                var cmp = col[a].CompareTo(col[b]);
                return desc ? -cmp : cmp;
            });
        }

        return Take(indices);
    }

    // ============================================================================
    // Grouping
    // ============================================================================

    /// <summary>Groups by specified columns.</summary>
    public GroupBy GroupBy(params string[] columns)
    {
        return new GroupBy(this, columns);
    }

    // ============================================================================
    // Joins
    // ============================================================================

    /// <summary>Inner join with another DataFrame.</summary>
    public DataFrame Join(DataFrame other, string on, string? suffix = "_right")
    {
        return Join(other, new[] { on }, new[] { on }, JoinType.Inner, suffix);
    }

    /// <summary>Join with specified type.</summary>
    public DataFrame Join(DataFrame other, string[] leftOn, string[] rightOn, JoinType how = JoinType.Inner, string? suffix = "_right")
    {
        return JoinOperations.Join(this, other, leftOn, rightOn, how, suffix ?? "_right");
    }

    /// <summary>Left join with another DataFrame.</summary>
    public DataFrame LeftJoin(DataFrame other, string on, string? suffix = "_right")
    {
        return Join(other, new[] { on }, new[] { on }, JoinType.Left, suffix);
    }

    /// <summary>Outer (full) join with another DataFrame.</summary>
    public DataFrame OuterJoin(DataFrame other, string on, string? suffix = "_right")
    {
        return Join(other, new[] { on }, new[] { on }, JoinType.Outer, suffix);
    }

    // ============================================================================
    // Concatenation
    // ============================================================================

    /// <summary>Vertically concatenates DataFrames.</summary>
    public static DataFrame VConcat(params DataFrame[] dfs)
    {
        if (dfs.Length == 0)
            return new DataFrame(Array.Empty<Series>());

        var schema = dfs[0].Columns;

        // Verify all DataFrames have the same schema
        foreach (var df in dfs.Skip(1))
        {
            if (!df.Columns.SequenceEqual(schema))
                throw new ArgumentException("All DataFrames must have the same schema for vertical concatenation");
        }

        // Concatenate each column
        var resultColumns = new List<Series>();
        for (int i = 0; i < schema.Count; i++)
        {
            var colName = schema[i];
            var values = new List<AnyValue>();

            foreach (var df in dfs)
            {
                for (int j = 0; j < df.Height; j++)
                {
                    values.Add(df[colName][j]);
                }
            }

            // Build series from values
            var dtype = dfs[0][colName].DataType;
            resultColumns.Add(BuildSeriesFromAnyValues(colName, values, dtype));
        }

        return new DataFrame(resultColumns);
    }

    private static Series BuildSeriesFromAnyValues(string name, List<AnyValue> values, DataType dtype)
    {
        return dtype switch
        {
            DataType.Int32Type => Series.FromNullable(name, values.Select(v => v.IsNull ? null : (int?)v.AsInt32()).ToArray()),
            DataType.Int64Type => Series.FromNullable(name, values.Select(v => v.IsNull ? null : (long?)v.AsInt64()).ToArray()),
            DataType.Float64Type => Series.FromNullable(name, values.Select(v => v.IsNull ? null : (double?)v.AsFloat64()).ToArray()),
            DataType.BooleanType => Series.FromNullable(name, values.Select(v => v.IsNull ? null : (bool?)v.AsBoolean()).ToArray()),
            DataType.StringType => Series.FromValues(name, values.Select(v => v.IsNull ? null : v.AsString()).ToArray()),
            _ => Series.FromValues(name, values.Select(v => v.ToString()).ToArray())
        };
    }

    /// <summary>Horizontally concatenates DataFrames.</summary>
    public static DataFrame HConcat(params DataFrame[] dfs)
    {
        if (dfs.Length == 0)
            return new DataFrame(Array.Empty<Series>());

        var height = dfs[0].Height;
        if (dfs.Any(df => df.Height != height))
            throw new ArgumentException("All DataFrames must have the same height for horizontal concatenation");

        var allColumns = dfs.SelectMany(df => df._columns).ToArray();
        return new DataFrame(allColumns);
    }

    // ============================================================================
    // Aggregations
    // ============================================================================

    /// <summary>Returns descriptive statistics for numeric columns.</summary>
    public DataFrame Describe()
    {
        var numericColumns = _columns.Where(c => c.DataType.IsNumeric).ToArray();
        if (numericColumns.Length == 0)
            return new DataFrame(Array.Empty<Series>());

        var stats = new[] { "count", "mean", "std", "min", "25%", "50%", "75%", "max" };
        var columns = new List<Series>();

        // Stat names column
        columns.Add(Series.FromValues("statistic", stats));

        foreach (var col in numericColumns)
        {
            var values = new List<double>();

            values.Add(col.Count());
            values.Add(col.Mean().TryGetDouble(out var m) ? m : double.NaN);
            values.Add(col.Std().TryGetDouble(out var s) ? s : double.NaN);
            values.Add(col.Min().TryGetDouble(out var min) ? min : double.NaN);

            // Percentiles (simplified)
            var sorted = col.Sort().ToArray<double>();
            values.Add(sorted.Length > 0 ? sorted[(int)(sorted.Length * 0.25)] : double.NaN);
            values.Add(sorted.Length > 0 ? sorted[(int)(sorted.Length * 0.50)] : double.NaN);
            values.Add(sorted.Length > 0 ? sorted[(int)(sorted.Length * 0.75)] : double.NaN);

            values.Add(col.Max().TryGetDouble(out var max) ? max : double.NaN);

            columns.Add(Series.FromValues(col.Name, values.ToArray()));
        }

        return new DataFrame(columns);
    }

    /// <summary>Returns null counts per column.</summary>
    public DataFrame NullCount()
    {
        var names = _columns.Select(c => c.Name).ToArray();
        var nullCounts = _columns.Select(c => c.NullCount).ToArray();

        return new DataFrame(new[]
        {
            Series.FromValues("column", names),
            Series.FromValues("null_count", nullCounts)
        });
    }

    // ============================================================================
    // Unique/Distinct
    // ============================================================================

    /// <summary>Returns unique rows based on specified columns.</summary>
    public DataFrame Unique(params string[] columns)
    {
        var subset = columns.Length > 0 ? columns : Columns.ToArray();
        var seen = new HashSet<string>();
        var keepIndices = new List<int>();

        for (int i = 0; i < _height; i++)
        {
            var key = string.Join("|", subset.Select(c => this[c][i].ToString()));
            if (seen.Add(key))
                keepIndices.Add(i);
        }

        return Take(keepIndices.ToArray());
    }

    /// <summary>Returns number of unique values per column.</summary>
    public DataFrame NUnique()
    {
        var names = _columns.Select(c => c.Name).ToArray();
        var uniqueCounts = _columns.Select(c => c.Unique().Length).ToArray();

        return new DataFrame(new[]
        {
            Series.FromValues("column", names),
            Series.FromValues("n_unique", uniqueCounts)
        });
    }

    // ============================================================================
    // Pivoting
    // ============================================================================

    /// <summary>Unpivots/melts the DataFrame from wide to long format.</summary>
    public DataFrame Melt(string[] idVars, string[] valueVars, string variableName = "variable", string valueName = "value")
    {
        var resultRows = new List<Dictionary<string, AnyValue>>();

        for (int i = 0; i < _height; i++)
        {
            foreach (var varCol in valueVars)
            {
                var row = new Dictionary<string, AnyValue>();

                // ID variables
                foreach (var idVar in idVars)
                {
                    row[idVar] = this[idVar][i];
                }

                // Variable name and value
                row[variableName] = AnyValue.From(varCol);
                row[valueName] = this[varCol][i];

                resultRows.Add(row);
            }
        }

        return FromRows(resultRows);
    }

    private static DataFrame FromRows(List<Dictionary<string, AnyValue>> rows)
    {
        if (rows.Count == 0)
            return new DataFrame(Array.Empty<Series>());

        var columnNames = rows[0].Keys.ToArray();
        var columns = new List<Series>();

        foreach (var colName in columnNames)
        {
            var values = rows.Select(r => r[colName]).ToList();
            var dtype = InferType(values);
            columns.Add(BuildSeriesFromAnyValues(colName, values, dtype));
        }

        return new DataFrame(columns);
    }

    private static DataType InferType(List<AnyValue> values)
    {
        var firstNonNull = values.FirstOrDefault(v => !v.IsNull);
        if (firstNonNull.IsNull)
            return DataType.String;

        return firstNonNull.Kind switch
        {
            AnyValueKind.Int32 => DataType.Int32,
            AnyValueKind.Int64 => DataType.Int64,
            AnyValueKind.Float32 => DataType.Float32,
            AnyValueKind.Float64 => DataType.Float64,
            AnyValueKind.Boolean => DataType.Boolean,
            AnyValueKind.String => DataType.String,
            _ => DataType.String
        };
    }

    // ============================================================================
    // Display
    // ============================================================================

    public override string ToString()
    {
        return ToString(10, 80);
    }

    public string ToString(int maxRows, int maxWidth = 120)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine($"Shape: ({_height}, {_columns.Length})");
        sb.Append("┌");
        for (int i = 0; i < _columns.Length; i++)
        {
            var colWidth = GetColumnWidth(_columns[i], maxRows);
            sb.Append(new string('─', colWidth + 2));
            sb.Append(i < _columns.Length - 1 ? "┬" : "┐");
        }
        sb.AppendLine();

        // Column names
        sb.Append("│");
        for (int i = 0; i < _columns.Length; i++)
        {
            var col = _columns[i];
            var colWidth = GetColumnWidth(col, maxRows);
            sb.Append($" {col.Name.PadRight(colWidth)} │");
        }
        sb.AppendLine();

        // Types
        sb.Append("│");
        for (int i = 0; i < _columns.Length; i++)
        {
            var col = _columns[i];
            var colWidth = GetColumnWidth(col, maxRows);
            var typeStr = col.DataType.ToString().ToLower();
            sb.Append($" {typeStr.PadRight(colWidth)} │");
        }
        sb.AppendLine();

        // Separator
        sb.Append("├");
        for (int i = 0; i < _columns.Length; i++)
        {
            var colWidth = GetColumnWidth(_columns[i], maxRows);
            sb.Append(new string('─', colWidth + 2));
            sb.Append(i < _columns.Length - 1 ? "┼" : "┤");
        }
        sb.AppendLine();

        // Data rows
        var displayRows = Math.Min(maxRows, _height);
        for (int row = 0; row < displayRows; row++)
        {
            sb.Append("│");
            for (int col = 0; col < _columns.Length; col++)
            {
                var series = _columns[col];
                var colWidth = GetColumnWidth(series, maxRows);
                var value = series[row].ToString();
                if (value.Length > colWidth)
                    value = value.Substring(0, colWidth - 1) + "…";
                sb.Append($" {value.PadRight(colWidth)} │");
            }
            sb.AppendLine();
        }

        if (_height > maxRows)
        {
            sb.AppendLine($"... ({_height - maxRows} more rows)");
        }

        // Footer
        sb.Append("└");
        for (int i = 0; i < _columns.Length; i++)
        {
            var colWidth = GetColumnWidth(_columns[i], maxRows);
            sb.Append(new string('─', colWidth + 2));
            sb.Append(i < _columns.Length - 1 ? "┴" : "┘");
        }
        sb.AppendLine();

        return sb.ToString();
    }

    private int GetColumnWidth(Series series, int maxRows)
    {
        var maxLen = series.Name.Length;
        maxLen = Math.Max(maxLen, series.DataType.ToString().Length);

        for (int i = 0; i < Math.Min(maxRows, _height); i++)
        {
            var valueLen = series[i].ToString().Length;
            maxLen = Math.Max(maxLen, Math.Min(valueLen, 30));
        }

        return Math.Min(maxLen, 30);
    }

    // ============================================================================
    // IEnumerable
    // ============================================================================

    public IEnumerator<Series> GetEnumerator()
    {
        foreach (var col in _columns)
            yield return col;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    // ============================================================================
    // Conversion to LazyFrame
    // ============================================================================

    /// <summary>Converts to a LazyFrame for lazy evaluation.</summary>
    public LazyFrame.LazyFrame Lazy()
    {
        return new LazyFrame.LazyFrame(this);
    }
}

/// <summary>Join types.</summary>
public enum JoinType
{
    Inner,
    Left,
    Right,
    Outer,
    Cross,
    Semi,
    Anti
}
