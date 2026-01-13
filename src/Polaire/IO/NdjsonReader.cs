// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;

namespace Polaire.IO;

/// <summary>
/// High-performance NDJSON (Newline-Delimited JSON) reader with parallel parsing.
/// </summary>
public static class NdjsonReader
{
    /// <summary>
    /// Reads an NDJSON file into a DataFrame.
    /// </summary>
    public static DataFrame Read(string path, NdjsonOptions? options = null)
    {
        options ??= NdjsonOptions.Default;

        var lines = File.ReadAllLines(path, options.Encoding)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToArray();

        return ParseLines(lines, options);
    }

    /// <summary>
    /// Reads an NDJSON stream into a DataFrame.
    /// </summary>
    public static DataFrame Read(Stream stream, NdjsonOptions? options = null)
    {
        options ??= NdjsonOptions.Default;

        using var reader = new StreamReader(stream, options.Encoding);
        var lines = new List<string>();

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (!string.IsNullOrWhiteSpace(line))
                lines.Add(line);
        }

        return ParseLines(lines.ToArray(), options);
    }

    /// <summary>
    /// Reads an NDJSON file in batches for streaming processing.
    /// </summary>
    public static IEnumerable<DataFrame> ReadBatched(string path, NdjsonOptions? options = null)
    {
        options ??= NdjsonOptions.Default;

        var batch = new List<string>();
        foreach (var line in File.ReadLines(path, options.Encoding))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            batch.Add(line);

            if (batch.Count >= options.BatchSize)
            {
                yield return ParseLines(batch.ToArray(), options);
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            yield return ParseLines(batch.ToArray(), options);
        }
    }

    private static DataFrame ParseLines(string[] lines, NdjsonOptions options)
    {
        if (lines.Length == 0)
            return DataFrame.Empty();

        // Parse all lines in parallel to collect data - use array to preserve order
        var records = new Dictionary<string, JsonElement>?[lines.Length];
        var allKeys = new ConcurrentDictionary<string, byte>();

        var threadCount = options.NumThreads > 0 ? options.NumThreads : Environment.ProcessorCount;
        Parallel.For(0, lines.Length, new ParallelOptions { MaxDegreeOfParallelism = threadCount }, i =>
        {
            try
            {
                using var doc = JsonDocument.Parse(lines[i]);
                var dict = new Dictionary<string, JsonElement>();

                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    dict[prop.Name] = prop.Value.Clone();
                    allKeys.TryAdd(prop.Name, 0);
                }

                records[i] = dict;
            }
            catch (JsonException)
            {
                // Skip malformed lines - leave as null
            }
        });

        // Determine column order - use order from first valid record
        var orderedKeys = allKeys.Keys.ToArray();

        // Filter columns if specified
        if (options.Columns != null)
        {
            orderedKeys = orderedKeys.Where(k => options.Columns.Contains(k)).ToArray();
        }

        // Filter out null records (malformed lines) and convert to list
        var recordList = records.Where(r => r != null).Cast<Dictionary<string, JsonElement>>().ToList();

        if (recordList.Count == 0)
            return DataFrame.Empty();

        // Infer types and build columns
        var columns = new List<Series>();

        foreach (var key in orderedKeys)
        {
            var dtype = InferColumnType(recordList, key, options);
            var series = BuildColumn(key, recordList, dtype, options);
            columns.Add(series);
        }

        return new DataFrame(columns);
    }

    private static DataType InferColumnType(List<Dictionary<string, JsonElement>> records, string key, NdjsonOptions options)
    {
        var sampleSize = Math.Min(records.Count, options.InferSchemaRows);
        var allInt = true;
        var allLong = true;
        var allDouble = true;
        var allBool = true;

        for (int i = 0; i < sampleSize; i++)
        {
            if (!records[i].TryGetValue(key, out var elem))
                continue;

            switch (elem.ValueKind)
            {
                case JsonValueKind.Number:
                    if (allInt && (!elem.TryGetInt32(out _)))
                        allInt = false;
                    if (allLong && (!elem.TryGetInt64(out _)))
                        allLong = false;
                    allBool = false;  // Numbers are not booleans
                    break;

                case JsonValueKind.True:
                case JsonValueKind.False:
                    allInt = false;
                    allLong = false;
                    allDouble = false;
                    break;

                case JsonValueKind.String:
                case JsonValueKind.Array:
                case JsonValueKind.Object:
                    allInt = false;
                    allLong = false;
                    allDouble = false;
                    allBool = false;
                    break;

                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    // Nulls don't affect type inference
                    break;
            }
        }

        if (allBool) return DataType.Boolean;
        if (allInt) return DataType.Int32;
        if (allLong) return DataType.Int64;
        if (allDouble) return DataType.Float64;
        return DataType.String;
    }

    private static Series BuildColumn(string name, List<Dictionary<string, JsonElement>> records, DataType dtype, NdjsonOptions options)
    {
        return dtype switch
        {
            DataType.Int32Type => BuildInt32Column(name, records),
            DataType.Int64Type => BuildInt64Column(name, records),
            DataType.Float64Type => BuildDoubleColumn(name, records),
            DataType.BooleanType => BuildBoolColumn(name, records),
            _ => BuildStringColumn(name, records)
        };
    }

    private static Series BuildInt32Column(string name, List<Dictionary<string, JsonElement>> records)
    {
        var values = new int?[records.Count];
        for (int i = 0; i < records.Count; i++)
        {
            if (records[i].TryGetValue(name, out var elem) &&
                elem.ValueKind == JsonValueKind.Number &&
                elem.TryGetInt32(out var v))
            {
                values[i] = v;
            }
            else
            {
                values[i] = null;
            }
        }
        return Series.FromNullable(name, values);
    }

    private static Series BuildInt64Column(string name, List<Dictionary<string, JsonElement>> records)
    {
        var values = new long?[records.Count];
        for (int i = 0; i < records.Count; i++)
        {
            if (records[i].TryGetValue(name, out var elem) &&
                elem.ValueKind == JsonValueKind.Number &&
                elem.TryGetInt64(out var v))
            {
                values[i] = v;
            }
            else
            {
                values[i] = null;
            }
        }
        return Series.FromNullable(name, values);
    }

    private static Series BuildDoubleColumn(string name, List<Dictionary<string, JsonElement>> records)
    {
        var values = new double?[records.Count];
        for (int i = 0; i < records.Count; i++)
        {
            if (records[i].TryGetValue(name, out var elem) &&
                elem.ValueKind == JsonValueKind.Number &&
                elem.TryGetDouble(out var v))
            {
                values[i] = v;
            }
            else
            {
                values[i] = null;
            }
        }
        return Series.FromNullable(name, values);
    }

    private static Series BuildBoolColumn(string name, List<Dictionary<string, JsonElement>> records)
    {
        var values = new bool?[records.Count];
        for (int i = 0; i < records.Count; i++)
        {
            if (records[i].TryGetValue(name, out var elem))
            {
                if (elem.ValueKind == JsonValueKind.True)
                    values[i] = true;
                else if (elem.ValueKind == JsonValueKind.False)
                    values[i] = false;
                else
                    values[i] = null;
            }
            else
            {
                values[i] = null;
            }
        }
        return Series.FromNullable(name, values);
    }

    private static Series BuildStringColumn(string name, List<Dictionary<string, JsonElement>> records)
    {
        var values = new string?[records.Count];
        for (int i = 0; i < records.Count; i++)
        {
            if (records[i].TryGetValue(name, out var elem))
            {
                values[i] = elem.ValueKind switch
                {
                    JsonValueKind.String => elem.GetString(),
                    JsonValueKind.Null or JsonValueKind.Undefined => null,
                    _ => elem.GetRawText()
                };
            }
            else
            {
                values[i] = null;
            }
        }
        return Series.FromValues(name, values);
    }
}

/// <summary>
/// Configuration options for reading NDJSON files.
/// </summary>
public sealed class NdjsonOptions
{
    /// <summary>Number of rows to sample for schema inference.</summary>
    public int InferSchemaRows { get; init; } = 1000;

    /// <summary>Columns to read (projection). Null means read all columns.</summary>
    public string[]? Columns { get; init; }

    /// <summary>Number of rows per batch during streaming reads.</summary>
    public int BatchSize { get; init; } = 100_000;

    /// <summary>Encoding for reading the file. Default is UTF-8.</summary>
    public System.Text.Encoding Encoding { get; init; } = System.Text.Encoding.UTF8;

    /// <summary>Number of threads for parallel parsing. 0 = use all available.</summary>
    public int NumThreads { get; init; } = 0;

    /// <summary>Default options.</summary>
    public static NdjsonOptions Default { get; } = new();
}
