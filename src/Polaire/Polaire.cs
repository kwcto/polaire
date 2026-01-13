// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET
// Inspired by Polars (https://pola.rs)

global using Polaire.DataTypes;
global using Polaire.Core;
global using static Polaire.Expressions.Expr;

using Polaire.LazyFrame;
using Polaire.Expressions;
using Polaire.IO;

namespace Polaire;

// Type aliases to avoid shadowing when method names match type names
using DataFrameType = Polaire.DataFrame;
using SeriesType = Polaire.Series;
using LazyFrameType = Polaire.LazyFrame.LazyFrame;

/// <summary>
/// Main entry point for creating Polaire DataFrames and Series.
/// Provides a fluent API similar to Python Polars.
/// </summary>
public static class Pl
{
    // ============================================================================
    // DataFrame Creation
    // ============================================================================

    /// <summary>Creates a DataFrame from Series.</summary>
    public static DataFrameType DataFrame(params SeriesType[] columns) => new(columns);

    /// <summary>Creates a DataFrame from a dictionary.</summary>
    public static DataFrameType DataFrame(Dictionary<string, object[]> data) =>
        DataFrameType.FromDictionary(data);

    /// <summary>Creates an empty DataFrame with schema.</summary>
    public static DataFrameType EmptyDataFrame(params (string name, DataType type)[] schema) =>
        DataFrameType.Empty(schema);

    // ============================================================================
    // Series Creation
    // ============================================================================

    /// <summary>Creates a Series from values.</summary>
    public static SeriesType Series(string name, int[] values) => SeriesType.FromValues(name, values);
    public static SeriesType Series(string name, long[] values) => SeriesType.FromValues(name, values);
    public static SeriesType Series(string name, float[] values) => SeriesType.FromValues(name, values);
    public static SeriesType Series(string name, double[] values) => SeriesType.FromValues(name, values);
    public static SeriesType Series(string name, bool[] values) => SeriesType.FromValues(name, values);
    public static SeriesType Series(string name, string?[] values) => SeriesType.FromValues(name, values);
    public static SeriesType Series(string name, DateTime[] values) => SeriesType.FromValues(name, values);
    public static SeriesType Series(string name, DateOnly[] values) => SeriesType.FromValues(name, values);

    /// <summary>Creates a Series from nullable values.</summary>
    public static SeriesType Series<T>(string name, T?[] values) where T : struct =>
        SeriesType.FromNullable(name, values);

    // ============================================================================
    // LazyFrame
    // ============================================================================

    /// <summary>Creates a LazyFrame from a DataFrame.</summary>
    public static LazyFrameType LazyFrame(DataFrameType df) => df.Lazy();

    // ============================================================================
    // Ranges
    // ============================================================================

    /// <summary>Creates a Series with integers from start to end (exclusive).</summary>
    public static SeriesType Arange(string name, int start, int end, int step = 1)
    {
        var values = new List<int>();
        for (int i = start; step > 0 ? i < end : i > end; i += step)
            values.Add(i);
        return SeriesType.FromValues(name, values.ToArray());
    }

    /// <summary>Creates a Series with integers from 0 to end (exclusive).</summary>
    public static SeriesType Arange(string name, int end) => Arange(name, 0, end);

    // ============================================================================
    // Repetition
    // ============================================================================

    /// <summary>Creates a Series with repeated values.</summary>
    public static SeriesType Repeat<T>(string name, T value, int count) where T : struct
    {
        return typeof(T).Name switch
        {
            nameof(Int32) => SeriesType.FromValues(name, Enumerable.Repeat((int)(object)value, count).ToArray()),
            nameof(Int64) => SeriesType.FromValues(name, Enumerable.Repeat((long)(object)value, count).ToArray()),
            nameof(Double) => SeriesType.FromValues(name, Enumerable.Repeat((double)(object)value, count).ToArray()),
            nameof(Boolean) => SeriesType.FromValues(name, Enumerable.Repeat((bool)(object)value, count).ToArray()),
            _ => throw new NotSupportedException($"Repeat not supported for {typeof(T).Name}")
        };
    }

    /// <summary>Creates a Series with repeated string values.</summary>
    public static SeriesType Repeat(string name, string value, int count) =>
        SeriesType.FromValues(name, Enumerable.Repeat(value, count).ToArray());

    // ============================================================================
    // Date Ranges
    // ============================================================================

    /// <summary>Creates a Series with date range.</summary>
    public static SeriesType DateRange(string name, DateOnly start, DateOnly end, TimeSpan? step = null)
    {
        var stepDays = step?.Days ?? 1;
        var values = new List<DateOnly>();
        for (var d = start; d < end; d = d.AddDays(stepDays))
            values.Add(d);
        return SeriesType.FromValues(name, values.ToArray());
    }

    /// <summary>Creates a Series with datetime range.</summary>
    public static SeriesType DateTimeRange(string name, DateTime start, DateTime end, TimeSpan? step = null)
    {
        var stepSpan = step ?? TimeSpan.FromDays(1);
        var values = new List<DateTime>();
        for (var d = start; d < end; d = d.Add(stepSpan))
            values.Add(d);
        return SeriesType.FromValues(name, values.ToArray());
    }

    // ============================================================================
    // Concatenation
    // ============================================================================

    /// <summary>Vertically concatenates DataFrames.</summary>
    public static DataFrameType Concat(params DataFrameType[] dfs) =>
        DataFrameType.VConcat(dfs);

    /// <summary>Horizontally concatenates DataFrames.</summary>
    public static DataFrameType HConcat(params DataFrameType[] dfs) =>
        DataFrameType.HConcat(dfs);

    /// <summary>Vertically concatenates LazyFrames.</summary>
    public static LazyFrameType Concat(params LazyFrameType[] lfs) =>
        LazyFrameType.Concat(lfs);

    // ============================================================================
    // Expression Helpers (re-export from Expr)
    // ============================================================================

    /// <summary>Reference a column by name.</summary>
    public static Expr Col(string name) => Expr.Col(name);

    /// <summary>Reference multiple columns by name.</summary>
    public static Expr[] Cols(params string[] names) => Expr.Cols(names);

    /// <summary>Select all columns.</summary>
    public static Expr All() => Expr.All();

    /// <summary>Create a literal value.</summary>
    public static Expr Lit(object? value) => Expr.Lit(value);

    /// <summary>Create a when-then-otherwise expression.</summary>
    public static WhenBuilder When(Expr condition) => Expr.WhenExpr(condition);

    /// <summary>Count expression.</summary>
    public static Expr Count() => Expr.Count();

    // ============================================================================
    // IO - CSV
    // ============================================================================

    /// <summary>Reads a CSV file into a DataFrame.</summary>
    public static DataFrameType ReadCsv(string path, bool hasHeader = true, char separator = ',')
    {
        return CsvReader.Read(path, new CsvOptions { HasHeader = hasHeader, Separator = separator });
    }

    /// <summary>Reads a CSV file with full options.</summary>
    public static DataFrameType ReadCsv(string path, CsvOptions options)
    {
        return CsvReader.Read(path, options);
    }

    /// <summary>Reads a CSV stream into a DataFrame.</summary>
    public static DataFrameType ReadCsv(Stream stream, CsvOptions? options = null)
    {
        return CsvReader.Read(stream, options);
    }

    /// <summary>Scans a CSV file lazily (enables query optimization).</summary>
    public static LazyFrameType ScanCsv(string path, bool hasHeader = true, char separator = ',')
    {
        var options = new CsvOptions { HasHeader = hasHeader, Separator = separator };
        return new LazyFrameType(new LogicalPlan.ScanCsv(path, options, null, null));
    }

    /// <summary>Scans a CSV file lazily with full options.</summary>
    public static LazyFrameType ScanCsv(string path, CsvOptions options)
    {
        return new LazyFrameType(new LogicalPlan.ScanCsv(path, options, null, null));
    }

    // ============================================================================
    // IO - Parquet
    // ============================================================================

    /// <summary>Reads a Parquet file into a DataFrame.</summary>
    public static DataFrameType ReadParquet(string path, ParquetOptions? options = null)
    {
        return ParquetReader.Read(path, options);
    }

    /// <summary>Scans a Parquet file lazily (enables query optimization).</summary>
    public static LazyFrameType ScanParquet(string path, ParquetOptions? options = null)
    {
        return new LazyFrameType(new LogicalPlan.ScanParquet(path, options ?? ParquetOptions.Default, null, null));
    }

    // ============================================================================
    // IO - JSON
    // ============================================================================

    /// <summary>Reads JSON into a DataFrame.</summary>
    public static DataFrameType ReadJson(string path)
    {
        return JsonReader.Read(path);
    }

    /// <summary>Reads NDJSON (newline-delimited JSON) into a DataFrame.</summary>
    public static DataFrameType ReadNdjson(string path)
    {
        return NdjsonReader.Read(path);
    }

    /// <summary>Scans an NDJSON file lazily.</summary>
    public static LazyFrameType ScanNdjson(string path, NdjsonOptions? options = null)
    {
        return new LazyFrameType(new LogicalPlan.ScanNdjson(path, options ?? NdjsonOptions.Default, null, null));
    }
}

/// <summary>
/// Extension methods for common operations.
/// </summary>
public static class PolaireExtensions
{
    /// <summary>Converts a DataFrame to a LazyFrame.</summary>
    public static LazyFrameType Lazy(this DataFrame df) => new(df);

    /// <summary>Prints the DataFrame to console.</summary>
    public static void Print(this DataFrame df, int maxRows = 10)
    {
        Console.WriteLine(df.ToString(maxRows));
    }

    /// <summary>Prints the Series to console.</summary>
    public static void Print(this Series series, int maxRows = 10)
    {
        Console.WriteLine(series.ToString(maxRows));
    }

    /// <summary>Explains the query plan.</summary>
    public static void Explain(this LazyFrameType lf, bool optimized = true)
    {
        Console.WriteLine(lf.Explain(optimized));
    }

    // ============================================================================
    // IO Extensions
    // ============================================================================

    /// <summary>Writes the DataFrame to a CSV file.</summary>
    public static void WriteCsv(this DataFrame df, string path, CsvWriteOptions? options = null)
    {
        CsvWriter.Write(df, path, options);
    }

    /// <summary>Writes the DataFrame to a CSV stream.</summary>
    public static void WriteCsv(this DataFrame df, Stream stream, CsvWriteOptions? options = null)
    {
        CsvWriter.Write(df, stream, options);
    }

    /// <summary>Converts the DataFrame to a CSV string.</summary>
    public static string ToCsv(this DataFrame df, CsvWriteOptions? options = null)
    {
        return CsvWriter.WriteString(df, options);
    }

    /// <summary>Writes the DataFrame to a Parquet file.</summary>
    public static void WriteParquet(this DataFrame df, string path, ParquetWriteOptions? options = null)
    {
        ParquetWriter.Write(df, path, options);
    }

    /// <summary>Writes the DataFrame to a Parquet stream.</summary>
    public static void WriteParquet(this DataFrame df, Stream stream, ParquetWriteOptions? options = null)
    {
        ParquetWriter.Write(df, stream, options);
    }
}
