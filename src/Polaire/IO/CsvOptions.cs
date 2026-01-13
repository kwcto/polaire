// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

namespace Polaire.IO;

/// <summary>
/// Configuration options for reading CSV files.
/// </summary>
public sealed class CsvOptions
{
    /// <summary>Whether the first row contains column names.</summary>
    public bool HasHeader { get; init; } = true;

    /// <summary>Column separator character.</summary>
    public char Separator { get; init; } = ',';

    /// <summary>Character used for quoting fields.</summary>
    public char QuoteChar { get; init; } = '"';

    /// <summary>Number of rows to sample for schema inference (0 = full scan).</summary>
    public int InferSchemaRows { get; init; } = 1000;

    /// <summary>Explicit schema mapping (column name to DataType). Skips inference for specified columns.</summary>
    public Dictionary<string, DataType>? Schema { get; init; }

    /// <summary>Number of rows per batch during streaming reads.</summary>
    public int BatchSize { get; init; } = 100_000;

    /// <summary>Size of file read chunks in bytes for parallel parsing.</summary>
    public int ChunkSizeBytes { get; init; } = 64 * 1024;  // 64KB

    /// <summary>Columns to read (projection). Null means read all columns.</summary>
    public string[]? Columns { get; init; }

    /// <summary>Maximum number of rows to read. Null means read all rows.</summary>
    public int? NRows { get; init; }

    /// <summary>Number of rows to skip from the start (after header).</summary>
    public int SkipRows { get; init; } = 0;

    /// <summary>String values to interpret as null.</summary>
    public string[]? NullValues { get; init; }

    /// <summary>Comment character. Lines starting with this are skipped.</summary>
    public char? CommentChar { get; init; }

    /// <summary>Encoding for reading the file. Default is UTF-8.</summary>
    public System.Text.Encoding Encoding { get; init; } = System.Text.Encoding.UTF8;

    /// <summary>Whether to parse dates automatically.</summary>
    public bool TryParseDates { get; init; } = true;

    /// <summary>Number of threads for parallel parsing. 0 = use all available.</summary>
    public int NumThreads { get; init; } = 0;

    /// <summary>Default options.</summary>
    public static CsvOptions Default { get; } = new();
}

/// <summary>
/// Configuration options for writing CSV files.
/// </summary>
public sealed class CsvWriteOptions
{
    /// <summary>Whether to write the header row.</summary>
    public bool IncludeHeader { get; init; } = true;

    /// <summary>Column separator character.</summary>
    public char Separator { get; init; } = ',';

    /// <summary>Character used for quoting fields.</summary>
    public char QuoteChar { get; init; } = '"';

    /// <summary>Line ending style.</summary>
    public string LineEnding { get; init; } = Environment.NewLine;

    /// <summary>String to write for null values.</summary>
    public string NullValue { get; init; } = "";

    /// <summary>Date format string.</summary>
    public string? DateFormat { get; init; }

    /// <summary>DateTime format string.</summary>
    public string? DateTimeFormat { get; init; }

    /// <summary>Float format (number of decimal places, -1 for full precision).</summary>
    public int FloatPrecision { get; init; } = -1;

    /// <summary>Encoding for writing the file. Default is UTF-8.</summary>
    public System.Text.Encoding Encoding { get; init; } = System.Text.Encoding.UTF8;

    /// <summary>Default options.</summary>
    public static CsvWriteOptions Default { get; } = new();
}
