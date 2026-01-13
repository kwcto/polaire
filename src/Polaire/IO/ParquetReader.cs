// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using System.Globalization;
using ParquetSchema = Parquet.Schema;
using PolaireDataType = Polaire.DataTypes.DataType;

namespace Polaire.IO;

/// <summary>
/// High-performance Parquet reader with column pruning and row group filtering.
/// </summary>
public static class ParquetReader
{
    /// <summary>
    /// Reads a Parquet file into a DataFrame.
    /// </summary>
    public static DataFrame Read(string path, ParquetOptions? options = null)
    {
        options ??= ParquetOptions.Default;

        using var stream = File.OpenRead(path);
        return Read(stream, options);
    }

    /// <summary>
    /// Reads a Parquet stream into a DataFrame.
    /// </summary>
    public static DataFrame Read(Stream stream, ParquetOptions? options = null)
    {
        options ??= ParquetOptions.Default;

        using var reader = global::Parquet.ParquetReader.CreateAsync(stream).GetAwaiter().GetResult();
        return ReadInternal(reader, options);
    }

    private static DataFrame ReadInternal(global::Parquet.ParquetReader reader, ParquetOptions options)
    {
        var schema = reader.Schema;

        // Get data fields from schema
        var dataFields = schema.DataFields.ToList();

        // Determine which columns to read
        var selectedFields = options.Columns != null
            ? dataFields.Where(f => options.Columns.Contains(f.Name)).ToList()
            : dataFields;

        if (selectedFields.Count == 0)
            return DataFrame.Empty();

        // Read all row groups
        var columnData = new Dictionary<string, List<object?>>();
        foreach (var field in selectedFields)
        {
            columnData[field.Name] = new List<object?>();
        }

        for (int rowGroup = 0; rowGroup < reader.RowGroupCount; rowGroup++)
        {
            using var rowGroupReader = reader.OpenRowGroupReader(rowGroup);

            foreach (var field in selectedFields)
            {
                var column = rowGroupReader.ReadColumnAsync(field).GetAwaiter().GetResult();
                var dataArray = column.Data;

                // Parquet.Net returns Array, iterate through it
                foreach (var value in dataArray)
                {
                    columnData[field.Name].Add(value);
                }
            }
        }

        // Build DataFrame
        var columns = new List<Series>();
        foreach (var field in selectedFields)
        {
            var values = columnData[field.Name];
            var series = BuildSeries(field.Name, field, values);
            columns.Add(series);
        }

        return new DataFrame(columns);
    }

    private static Series BuildSeries(string name, ParquetSchema.DataField field, List<object?> values)
    {
        // Map Parquet types to Polaire types
        var clrType = field.ClrType;

        if (clrType == typeof(int) || clrType == typeof(int?))
            return BuildInt32Series(name, values);
        if (clrType == typeof(long) || clrType == typeof(long?))
            return BuildInt64Series(name, values);
        if (clrType == typeof(float) || clrType == typeof(float?))
            return BuildFloatSeries(name, values);
        if (clrType == typeof(double) || clrType == typeof(double?))
            return BuildDoubleSeries(name, values);
        if (clrType == typeof(bool) || clrType == typeof(bool?))
            return BuildBoolSeries(name, values);
        if (clrType == typeof(string))
            return BuildStringSeries(name, values);
        if (clrType == typeof(DateTimeOffset) || clrType == typeof(DateTimeOffset?) ||
            clrType == typeof(DateTime) || clrType == typeof(DateTime?))
            return BuildDateTimeSeries(name, values);

        // Default to string
        return BuildStringSeries(name, values);
    }

    private static Series BuildInt32Series(string name, List<object?> values)
    {
        var arr = values.Select(v => v == null ? (int?)null : Convert.ToInt32(v)).ToArray();
        return Series.FromNullable(name, arr);
    }

    private static Series BuildInt64Series(string name, List<object?> values)
    {
        var arr = values.Select(v => v == null ? (long?)null : Convert.ToInt64(v)).ToArray();
        return Series.FromNullable(name, arr);
    }

    private static Series BuildFloatSeries(string name, List<object?> values)
    {
        var arr = values.Select(v => v == null ? (float?)null : Convert.ToSingle(v)).ToArray();
        return Series.FromNullable(name, arr);
    }

    private static Series BuildDoubleSeries(string name, List<object?> values)
    {
        var arr = values.Select(v => v == null ? (double?)null : Convert.ToDouble(v)).ToArray();
        return Series.FromNullable(name, arr);
    }

    private static Series BuildBoolSeries(string name, List<object?> values)
    {
        var arr = values.Select(v => v == null ? (bool?)null : Convert.ToBoolean(v)).ToArray();
        return Series.FromNullable(name, arr);
    }

    private static Series BuildStringSeries(string name, List<object?> values)
    {
        var arr = values.Select(v => v?.ToString()).ToArray();
        return Series.FromValues(name, arr);
    }

    private static Series BuildDateTimeSeries(string name, List<object?> values)
    {
        var arr = values.Select(v =>
        {
            if (v == null) return (DateTime?)null;
            if (v is DateTimeOffset dto) return dto.DateTime;
            if (v is DateTime dt) return dt;
            return (DateTime?)null;
        }).ToArray();
        return Series.FromNullable(name, arr);
    }
}

/// <summary>
/// Configuration options for reading Parquet files.
/// </summary>
public sealed class ParquetOptions
{
    /// <summary>Columns to read (projection). Null means read all columns.</summary>
    public string[]? Columns { get; init; }

    /// <summary>Number of threads for parallel row group reading. 0 = use all available.</summary>
    public int NumThreads { get; init; } = 0;

    /// <summary>Default options.</summary>
    public static ParquetOptions Default { get; } = new();
}
