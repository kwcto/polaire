// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using System.Collections.Concurrent;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Polaire.DataTypes;

namespace Polaire.IO;

/// <summary>
/// High-performance CSV reader with streaming and parallel parsing.
/// </summary>
public static class CsvReader
{
    /// <summary>
    /// Reads a CSV file into a DataFrame.
    /// </summary>
    public static DataFrame Read(string path, CsvOptions? options = null)
    {
        options ??= CsvOptions.Default;

        using var stream = File.OpenRead(path);
        return Read(stream, options);
    }

    /// <summary>
    /// Reads a CSV stream into a DataFrame.
    /// </summary>
    public static DataFrame Read(Stream stream, CsvOptions? options = null)
    {
        options ??= CsvOptions.Default;

        // For smaller files or when we need all data, use single-pass read
        var batches = ReadBatchedInternal(stream, options).ToList();

        if (batches.Count == 0)
            return DataFrame.Empty();

        if (batches.Count == 1)
            return batches[0];

        // Concatenate all batches
        return DataFrame.VConcat(batches.ToArray());
    }

    /// <summary>
    /// Reads a CSV file in batches for streaming processing.
    /// </summary>
    public static IEnumerable<DataFrame> ReadBatched(string path, CsvOptions? options = null)
    {
        options ??= CsvOptions.Default;

        using var stream = File.OpenRead(path);
        foreach (var batch in ReadBatchedInternal(stream, options))
        {
            yield return batch;
        }
    }

    /// <summary>
    /// Reads a CSV stream in batches for streaming processing.
    /// </summary>
    public static IEnumerable<DataFrame> ReadBatched(Stream stream, CsvOptions? options = null)
    {
        options ??= CsvOptions.Default;
        return ReadBatchedInternal(stream, options);
    }

    private static IEnumerable<DataFrame> ReadBatchedInternal(Stream stream, CsvOptions options)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = options.HasHeader,
            Delimiter = options.Separator.ToString(),
            Quote = options.QuoteChar,
            BadDataFound = null, // Ignore bad data
            MissingFieldFound = null,
            TrimOptions = TrimOptions.Trim,
        };

        if (options.CommentChar.HasValue)
        {
            config.Comment = options.CommentChar.Value;
            config.AllowComments = true;
        }

        using var reader = new StreamReader(stream, options.Encoding, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        using var csv = new CsvHelper.CsvReader(reader, config);

        // Read header
        string[] headers;
        if (options.HasHeader)
        {
            if (!csv.Read() || !csv.ReadHeader())
                yield break;
            headers = csv.HeaderRecord ?? Array.Empty<string>();
        }
        else
        {
            // Generate column names
            if (!csv.Read())
                yield break;
            headers = Enumerable.Range(0, csv.Parser.Count).Select(i => $"column_{i}").ToArray();
        }

        // Determine which columns to read
        var columnIndices = GetColumnIndices(headers, options.Columns);
        var selectedHeaders = columnIndices.Select(i => headers[i]).ToArray();

        // Infer schema from first N rows
        var (schema, initialRows) = InferSchema(csv, selectedHeaders, columnIndices, options);

        // Skip rows if requested
        var skipRemaining = options.SkipRows;
        while (skipRemaining > 0 && initialRows.Count > 0)
        {
            initialRows.RemoveAt(0);
            skipRemaining--;
        }

        var totalRowsRead = 0;
        var maxRows = options.NRows ?? int.MaxValue;

        // Process initial rows as first batch
        var batchRows = new List<string?[]>();
        foreach (var row in initialRows)
        {
            if (totalRowsRead >= maxRows)
                break;
            batchRows.Add(row);
            totalRowsRead++;
        }

        // Continue reading remaining rows
        if (totalRowsRead < maxRows && !options.HasHeader)
        {
            // We already read one row for column count detection, process it
            var row = ExtractRow(csv, columnIndices);
            if (row != null)
            {
                if (skipRemaining > 0)
                    skipRemaining--;
                else
                {
                    batchRows.Add(row);
                    totalRowsRead++;
                }
            }
        }

        // Read remaining rows
        while (csv.Read() && totalRowsRead < maxRows)
        {
            if (skipRemaining > 0)
            {
                skipRemaining--;
                continue;
            }

            var row = ExtractRow(csv, columnIndices);
            if (row != null)
            {
                batchRows.Add(row);
                totalRowsRead++;

                // Yield batch when full
                if (batchRows.Count >= options.BatchSize)
                {
                    yield return BuildDataFrame(selectedHeaders, schema, batchRows, options);
                    batchRows.Clear();
                }
            }
        }

        // Yield final batch
        if (batchRows.Count > 0)
        {
            yield return BuildDataFrame(selectedHeaders, schema, batchRows, options);
        }
    }

    private static int[] GetColumnIndices(string[] headers, string[]? requestedColumns)
    {
        if (requestedColumns == null || requestedColumns.Length == 0)
            return Enumerable.Range(0, headers.Length).ToArray();

        var indices = new List<int>();
        foreach (var col in requestedColumns)
        {
            var idx = Array.IndexOf(headers, col);
            if (idx >= 0)
                indices.Add(idx);
        }
        return indices.ToArray();
    }

    private static string?[] ExtractRow(CsvHelper.CsvReader csv, int[] columnIndices)
    {
        var row = new string?[columnIndices.Length];
        for (int i = 0; i < columnIndices.Length; i++)
        {
            var idx = columnIndices[i];
            row[i] = idx < csv.Parser.Count ? csv.GetField(idx) : null;
        }
        return row;
    }

    private static (DataType[] schema, List<string?[]> rows) InferSchema(
        CsvHelper.CsvReader csv,
        string[] headers,
        int[] columnIndices,
        CsvOptions options)
    {
        var inferRows = options.InferSchemaRows > 0 ? options.InferSchemaRows : 10000;
        var rows = new List<string?[]>();

        // Read sample rows for inference
        var sampledRows = 0;
        while (csv.Read() && sampledRows < inferRows)
        {
            rows.Add(ExtractRow(csv, columnIndices));
            sampledRows++;
        }

        // Infer types
        var schema = new DataType[headers.Length];
        for (int col = 0; col < headers.Length; col++)
        {
            // Check if explicit schema provided
            if (options.Schema?.TryGetValue(headers[col], out var explicitType) == true)
            {
                schema[col] = explicitType;
                continue;
            }

            schema[col] = InferColumnType(rows, col, options);
        }

        return (schema, rows);
    }

    private static DataType InferColumnType(List<string?[]> rows, int colIndex, CsvOptions options)
    {
        var allInt = true;
        var allLong = true;
        var allDouble = true;
        var allBool = true;
        var allDate = true;
        var allDateTime = true;

        var nullValues = options.NullValues?.ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "", "null", "NULL", "NA", "N/A", "NaN" };

        foreach (var row in rows)
        {
            if (colIndex >= row.Length)
                continue;

            var value = row[colIndex];

            if (string.IsNullOrEmpty(value) || nullValues.Contains(value))
            {
                continue;
            }

            if (allBool && !IsBoolValue(value))
                allBool = false;

            if (allInt && !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                allInt = false;

            if (allLong && !long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                allLong = false;

            if (allDouble && !double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out _))
                allDouble = false;

            if (options.TryParseDates)
            {
                if (allDate && !DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                    allDate = false;

                if (allDateTime && !DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                    allDateTime = false;
            }
            else
            {
                allDate = false;
                allDateTime = false;
            }
        }

        // Priority: Bool > Int > Long > Double > Date > DateTime > String
        if (allBool) return DataType.Boolean;
        if (allInt) return DataType.Int32;
        if (allLong) return DataType.Int64;
        if (allDouble) return DataType.Float64;
        if (allDate) return DataType.Date;
        if (allDateTime) return DataType.DateTime(TimeUnit.Nanoseconds);
        return DataType.String;
    }

    private static bool IsBoolValue(string value)
    {
        return value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("false", StringComparison.OrdinalIgnoreCase) ||
               value == "1" || value == "0";
    }

    private static DataFrame BuildDataFrame(string[] headers, DataType[] schema, List<string?[]> rows, CsvOptions options)
    {
        var nullValues = options.NullValues?.ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "", "null", "NULL", "NA", "N/A", "NaN" };

        var columns = new Series[headers.Length];

        // Build columns in parallel for better performance
        var threadCount = options.NumThreads > 0 ? options.NumThreads : Environment.ProcessorCount;
        Parallel.For(0, headers.Length, new ParallelOptions { MaxDegreeOfParallelism = threadCount }, col =>
        {
            columns[col] = BuildColumn(headers[col], schema[col], rows, col, nullValues);
        });

        return new DataFrame(columns);
    }

    private static Series BuildColumn(string name, DataType dtype, List<string?[]> rows, int colIndex, HashSet<string> nullValues)
    {
        return dtype switch
        {
            DataType.Int32Type => BuildInt32Column(name, rows, colIndex, nullValues),
            DataType.Int64Type => BuildInt64Column(name, rows, colIndex, nullValues),
            DataType.Float64Type => BuildFloat64Column(name, rows, colIndex, nullValues),
            DataType.BooleanType => BuildBoolColumn(name, rows, colIndex, nullValues),
            DataType.DateType => BuildDateColumn(name, rows, colIndex, nullValues),
            DataType.DateTimeType => BuildDateTimeColumn(name, rows, colIndex, nullValues),
            _ => BuildStringColumn(name, rows, colIndex, nullValues)
        };
    }

    private static Series BuildInt32Column(string name, List<string?[]> rows, int colIndex, HashSet<string> nullValues)
    {
        var values = new int?[rows.Count];
        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var str = colIndex < row.Length ? row[colIndex] : null;
            if (string.IsNullOrEmpty(str) || nullValues.Contains(str))
                values[i] = null;
            else if (int.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
                values[i] = v;
            else
                values[i] = null;
        }
        return Series.FromNullable(name, values);
    }

    private static Series BuildInt64Column(string name, List<string?[]> rows, int colIndex, HashSet<string> nullValues)
    {
        var values = new long?[rows.Count];
        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var str = colIndex < row.Length ? row[colIndex] : null;
            if (string.IsNullOrEmpty(str) || nullValues.Contains(str))
                values[i] = null;
            else if (long.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
                values[i] = v;
            else
                values[i] = null;
        }
        return Series.FromNullable(name, values);
    }

    private static Series BuildFloat64Column(string name, List<string?[]> rows, int colIndex, HashSet<string> nullValues)
    {
        var values = new double?[rows.Count];
        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var str = colIndex < row.Length ? row[colIndex] : null;
            if (string.IsNullOrEmpty(str) || nullValues.Contains(str))
                values[i] = null;
            else if (double.TryParse(str, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var v))
                values[i] = v;
            else
                values[i] = null;
        }
        return Series.FromNullable(name, values);
    }

    private static Series BuildBoolColumn(string name, List<string?[]> rows, int colIndex, HashSet<string> nullValues)
    {
        var values = new bool?[rows.Count];
        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var str = colIndex < row.Length ? row[colIndex] : null;
            if (string.IsNullOrEmpty(str) || nullValues.Contains(str))
                values[i] = null;
            else if (str.Equals("true", StringComparison.OrdinalIgnoreCase) || str == "1")
                values[i] = true;
            else if (str.Equals("false", StringComparison.OrdinalIgnoreCase) || str == "0")
                values[i] = false;
            else
                values[i] = null;
        }
        return Series.FromNullable(name, values);
    }

    private static Series BuildDateColumn(string name, List<string?[]> rows, int colIndex, HashSet<string> nullValues)
    {
        var values = new DateOnly?[rows.Count];
        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var str = colIndex < row.Length ? row[colIndex] : null;
            if (string.IsNullOrEmpty(str) || nullValues.Contains(str))
                values[i] = null;
            else if (DateOnly.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out var v))
                values[i] = v;
            else
                values[i] = null;
        }
        return Series.FromNullable(name, values);
    }

    private static Series BuildDateTimeColumn(string name, List<string?[]> rows, int colIndex, HashSet<string> nullValues)
    {
        var values = new DateTime?[rows.Count];
        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var str = colIndex < row.Length ? row[colIndex] : null;
            if (string.IsNullOrEmpty(str) || nullValues.Contains(str))
                values[i] = null;
            else if (DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out var v))
                values[i] = v;
            else
                values[i] = null;
        }
        return Series.FromNullable(name, values);
    }

    private static Series BuildStringColumn(string name, List<string?[]> rows, int colIndex, HashSet<string> nullValues)
    {
        var values = new string?[rows.Count];
        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var str = colIndex < row.Length ? row[colIndex] : null;
            if (string.IsNullOrEmpty(str) || nullValues.Contains(str))
                values[i] = null;
            else
                values[i] = str;
        }
        return Series.FromValues(name, values);
    }
}
