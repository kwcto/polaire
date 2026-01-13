// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using System.Globalization;
using System.Text.Json;

namespace Polaire.IO;

/// <summary>
/// Standard JSON reader for arrays of objects.
/// For better performance with large files, prefer NDJSON format.
/// </summary>
public static class JsonReader
{
    /// <summary>
    /// Reads a JSON file into a DataFrame.
    /// Expects a JSON array of objects at the root.
    /// </summary>
    public static DataFrame Read(string path, JsonOptions? options = null)
    {
        options ??= JsonOptions.Default;

        using var stream = File.OpenRead(path);
        return Read(stream, options);
    }

    /// <summary>
    /// Reads a JSON stream into a DataFrame.
    /// </summary>
    public static DataFrame Read(Stream stream, JsonOptions? options = null)
    {
        options ??= JsonOptions.Default;

        using var doc = JsonDocument.Parse(stream, new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        });

        return ParseDocument(doc, options);
    }

    /// <summary>
    /// Reads a JSON string into a DataFrame.
    /// </summary>
    public static DataFrame ReadString(string json, JsonOptions? options = null)
    {
        options ??= JsonOptions.Default;

        using var doc = JsonDocument.Parse(json, new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        });

        return ParseDocument(doc, options);
    }

    private static DataFrame ParseDocument(JsonDocument doc, JsonOptions options)
    {
        var root = doc.RootElement;

        // Handle different root structures
        JsonElement array;
        if (root.ValueKind == JsonValueKind.Array)
        {
            array = root;
        }
        else if (root.ValueKind == JsonValueKind.Object)
        {
            // Try to find a data array in common locations
            if (root.TryGetProperty("data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
                array = dataArray;
            else if (root.TryGetProperty("rows", out var rowsArray) && rowsArray.ValueKind == JsonValueKind.Array)
                array = rowsArray;
            else if (root.TryGetProperty("items", out var itemsArray) && itemsArray.ValueKind == JsonValueKind.Array)
                array = itemsArray;
            else if (root.TryGetProperty("records", out var recordsArray) && recordsArray.ValueKind == JsonValueKind.Array)
                array = recordsArray;
            else
                throw new JsonException("JSON object must contain a 'data', 'rows', 'items', or 'records' array");
        }
        else
        {
            throw new JsonException("JSON root must be an array or an object containing a data array");
        }

        if (array.GetArrayLength() == 0)
            return DataFrame.Empty();

        // Collect all records and keys
        var records = new List<Dictionary<string, JsonElement>>();
        var allKeys = new HashSet<string>();

        foreach (var element in array.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.Object)
                continue;

            var dict = new Dictionary<string, JsonElement>();
            foreach (var prop in element.EnumerateObject())
            {
                dict[prop.Name] = prop.Value.Clone();
                allKeys.Add(prop.Name);
            }
            records.Add(dict);
        }

        if (records.Count == 0)
            return DataFrame.Empty();

        // Determine column order
        var orderedKeys = allKeys.ToArray();

        // Filter columns if specified
        if (options.Columns != null)
        {
            orderedKeys = orderedKeys.Where(k => options.Columns.Contains(k)).ToArray();
        }

        // Infer types and build columns
        var columns = new List<Series>();

        foreach (var key in orderedKeys)
        {
            var dtype = InferColumnType(records, key, options);
            var series = BuildColumn(key, records, dtype);
            columns.Add(series);
        }

        return new DataFrame(columns);
    }

    private static DataType InferColumnType(List<Dictionary<string, JsonElement>> records, string key, JsonOptions options)
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
                    if (allInt && !elem.TryGetInt32(out _))
                        allInt = false;
                    if (allLong && !elem.TryGetInt64(out _))
                        allLong = false;
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
                    break;
            }
        }

        if (allBool) return DataType.Boolean;
        if (allInt) return DataType.Int32;
        if (allLong) return DataType.Int64;
        if (allDouble) return DataType.Float64;
        return DataType.String;
    }

    private static Series BuildColumn(string name, List<Dictionary<string, JsonElement>> records, DataType dtype)
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
        }
        return Series.FromValues(name, values);
    }
}

/// <summary>
/// Configuration options for reading JSON files.
/// </summary>
public sealed class JsonOptions
{
    /// <summary>Number of rows to sample for schema inference.</summary>
    public int InferSchemaRows { get; init; } = 1000;

    /// <summary>Columns to read (projection). Null means read all columns.</summary>
    public string[]? Columns { get; init; }

    /// <summary>Default options.</summary>
    public static JsonOptions Default { get; } = new();
}
