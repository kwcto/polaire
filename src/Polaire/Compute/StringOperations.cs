// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using System.Text.RegularExpressions;
using Apache.Arrow;
using Polaire.DataTypes;


namespace Polaire.Compute;

/// <summary>
/// String operations namespace for Series (accessed via .Str property).
/// </summary>
public sealed class StringOperations
{
    private readonly Series _series;

    public StringOperations(Series series)
    {
        if (series.DataType is not DataType.StringType and not DataType.LargeStringType)
            throw new ArgumentException("String operations require string series");
        _series = series;
    }

    // ============================================================================
    // Case Operations
    // ============================================================================

    public Series ToLowerCase()
    {
        return TransformStrings(s => s?.ToLowerInvariant());
    }

    public Series ToUpperCase()
    {
        return TransformStrings(s => s?.ToUpperInvariant());
    }

    public Series ToTitleCase()
    {
        return TransformStrings(s => s is null ? null :
            System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s.ToLowerInvariant()));
    }

    // ============================================================================
    // Trimming
    // ============================================================================

    public Series Strip() => TransformStrings(s => s?.Trim());
    public Series StripStart() => TransformStrings(s => s?.TrimStart());
    public Series StripEnd() => TransformStrings(s => s?.TrimEnd());
    public Series Strip(string chars) => TransformStrings(s => s?.Trim(chars.ToCharArray()));

    // ============================================================================
    // Padding
    // ============================================================================

    public Series PadStart(int length, char fillChar = ' ')
    {
        return TransformStrings(s => s?.PadLeft(length, fillChar));
    }

    public Series PadEnd(int length, char fillChar = ' ')
    {
        return TransformStrings(s => s?.PadRight(length, fillChar));
    }

    public Series ZFill(int length)
    {
        return TransformStrings(s =>
        {
            if (s is null) return null;
            if (s.Length >= length) return s;
            if (s.Length > 0 && (s[0] == '-' || s[0] == '+'))
                return s[0] + s.Substring(1).PadLeft(length - 1, '0');
            return s.PadLeft(length, '0');
        });
    }

    // ============================================================================
    // Substring
    // ============================================================================

    public Series Substring(int start, int? length = null)
    {
        return TransformStrings(s =>
        {
            if (s is null) return null;
            if (start >= s.Length) return "";
            int actualStart = start < 0 ? Math.Max(0, s.Length + start) : start;
            int actualLength = length ?? (s.Length - actualStart);
            actualLength = Math.Min(actualLength, s.Length - actualStart);
            return s.Substring(actualStart, actualLength);
        });
    }

    public Series Head(int n) => Substring(0, n);
    public Series Tail(int n) => TransformStrings(s => s is null || s.Length <= n ? s : s.Substring(s.Length - n));

    // ============================================================================
    // Search Operations
    // ============================================================================

    public Series Contains(string pattern, bool literal = true)
    {
        if (literal)
        {
            return ToBooleans(s => s?.Contains(pattern, StringComparison.Ordinal) ?? false);
        }
        else
        {
            var regex = new Regex(pattern, RegexOptions.Compiled);
            return ToBooleans(s => s is not null && regex.IsMatch(s));
        }
    }

    public Series StartsWith(string prefix)
    {
        return ToBooleans(s => s?.StartsWith(prefix, StringComparison.Ordinal) ?? false);
    }

    public Series EndsWith(string suffix)
    {
        return ToBooleans(s => s?.EndsWith(suffix, StringComparison.Ordinal) ?? false);
    }

    public Series Matches(string pattern)
    {
        var regex = new Regex(pattern, RegexOptions.Compiled);
        return ToBooleans(s => s is not null && regex.IsMatch(s));
    }

    // ============================================================================
    // Replace Operations
    // ============================================================================

    public Series Replace(string pattern, string replacement, bool literal = true)
    {
        if (literal)
        {
            return TransformStrings(s => s?.Replace(pattern, replacement));
        }
        else
        {
            var regex = new Regex(pattern, RegexOptions.Compiled);
            return TransformStrings(s => s is null ? null : regex.Replace(s, replacement));
        }
    }

    public Series ReplaceFirst(string pattern, string replacement)
    {
        var regex = new Regex(pattern, RegexOptions.Compiled);
        return TransformStrings(s => s is null ? null : regex.Replace(s, replacement, 1));
    }

    // ============================================================================
    // Split Operations
    // ============================================================================

    public Series[] Split(string separator, int maxSplit = -1)
    {
        var data = _series.Data as StringChunkedArray;
        var results = new List<List<string?>>();

        for (int i = 0; i < _series.Length; i++)
        {
            if (_series.IsNull(i))
            {
                EnsureColumns(results, 1);
                results[0].Add(null);
            }
            else
            {
                var str = data!.GetString(i);
                var parts = maxSplit < 0
                    ? str?.Split(separator)
                    : str?.Split(separator, maxSplit + 1, StringSplitOptions.None);

                EnsureColumns(results, parts?.Length ?? 1);
                for (int j = 0; j < results.Count; j++)
                {
                    results[j].Add(parts is not null && j < parts.Length ? parts[j] : null);
                }
            }
        }

        return results.Select((col, idx) =>
            Series.FromValues($"{_series.Name}_{idx}", col.ToArray())).ToArray();
    }

    private static void EnsureColumns(List<List<string?>> results, int count)
    {
        while (results.Count < count)
        {
            var newList = new List<string?>();
            // Pad with nulls for previous rows
            if (results.Count > 0)
            {
                for (int i = 0; i < results[0].Count; i++)
                    newList.Add(null);
            }
            results.Add(newList);
        }
    }

    // ============================================================================
    // Extract Operations
    // ============================================================================

    public Series Extract(string pattern, int groupIndex = 0)
    {
        var regex = new Regex(pattern, RegexOptions.Compiled);
        return TransformStrings(s =>
        {
            if (s is null) return null;
            var match = regex.Match(s);
            if (!match.Success) return null;
            if (groupIndex == 0) return match.Value;
            if (groupIndex < match.Groups.Count) return match.Groups[groupIndex].Value;
            return null;
        });
    }

    public Series[] ExtractAll(string pattern)
    {
        var regex = new Regex(pattern, RegexOptions.Compiled);
        var groupCount = regex.GetGroupNumbers().Length;
        var results = new List<string?>[groupCount];
        for (int i = 0; i < groupCount; i++)
            results[i] = new List<string?>();

        var data = _series.Data as StringChunkedArray;
        for (int i = 0; i < _series.Length; i++)
        {
            if (_series.IsNull(i))
            {
                for (int j = 0; j < groupCount; j++)
                    results[j].Add(null);
            }
            else
            {
                var str = data!.GetString(i);
                var match = regex.Match(str ?? "");
                for (int j = 0; j < groupCount; j++)
                {
                    results[j].Add(match.Success && j < match.Groups.Count ? match.Groups[j].Value : null);
                }
            }
        }

        return results.Select((col, idx) =>
            Series.FromValues($"{_series.Name}_group{idx}", col.ToArray())).ToArray();
    }

    // ============================================================================
    // Length Operations
    // ============================================================================

    public Series Lengths()
    {
        return ToInts(s => s?.Length ?? 0);
    }

    public Series ByteLengths()
    {
        return ToInts(s => s is null ? 0 : System.Text.Encoding.UTF8.GetByteCount(s));
    }

    // ============================================================================
    // Join/Concatenate
    // ============================================================================

    public Series Concat(params Series[] others)
    {
        var allSeries = new[] { _series }.Concat(others).ToArray();
        foreach (var s in others)
        {
            if (s.Length != _series.Length)
                throw new ArgumentException("All series must have same length");
        }

        var builder = new StringArray.Builder();
        for (int i = 0; i < _series.Length; i++)
        {
            bool hasNull = allSeries.Any(s => s.IsNull(i));
            if (hasNull)
            {
                builder.AppendNull();
            }
            else
            {
                var parts = allSeries.Select(s => s[i].ToString()).ToArray();
                builder.Append(string.Concat(parts));
            }
        }

        return Series.FromArrowArray(_series.Name, builder.Build(), DataType.String);
    }

    // ============================================================================
    // Count Operations
    // ============================================================================

    public Series CountMatches(string pattern, bool literal = true)
    {
        if (literal)
        {
            return ToInts(s =>
            {
                if (s is null) return 0;
                int count = 0;
                int index = 0;
                while ((index = s.IndexOf(pattern, index, StringComparison.Ordinal)) >= 0)
                {
                    count++;
                    index += pattern.Length;
                }
                return count;
            });
        }
        else
        {
            var regex = new Regex(pattern, RegexOptions.Compiled);
            return ToInts(s => s is null ? 0 : regex.Matches(s).Count);
        }
    }

    // ============================================================================
    // JSON Operations
    // ============================================================================

    public Series JsonPathExtract(string jsonPath)
    {
        return TransformStrings(s =>
        {
            if (s is null) return null;
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(s);
                // Simple JSON path implementation for common cases
                var path = jsonPath.TrimStart('$', '.');
                var parts = path.Split('.');
                System.Text.Json.JsonElement current = doc.RootElement;

                foreach (var part in parts)
                {
                    if (string.IsNullOrEmpty(part)) continue;

                    // Handle array indexing [n]
                    if (part.Contains('['))
                    {
                        var bracketIndex = part.IndexOf('[');
                        var propName = part.Substring(0, bracketIndex);
                        var indexStr = part.Substring(bracketIndex + 1).TrimEnd(']');

                        if (!string.IsNullOrEmpty(propName))
                            current = current.GetProperty(propName);

                        if (int.TryParse(indexStr, out var arrayIndex))
                            current = current[arrayIndex];
                    }
                    else
                    {
                        current = current.GetProperty(part);
                    }
                }

                return current.ValueKind switch
                {
                    System.Text.Json.JsonValueKind.String => current.GetString(),
                    System.Text.Json.JsonValueKind.Number => current.GetRawText(),
                    System.Text.Json.JsonValueKind.True => "true",
                    System.Text.Json.JsonValueKind.False => "false",
                    System.Text.Json.JsonValueKind.Null => null,
                    _ => current.GetRawText()
                };
            }
            catch
            {
                return null;
            }
        });
    }

    // ============================================================================
    // Helpers
    // ============================================================================

    private Series TransformStrings(Func<string?, string?> transform)
    {
        var data = _series.Data as StringChunkedArray;
        var builder = new StringArray.Builder();

        for (int i = 0; i < _series.Length; i++)
        {
            if (_series.IsNull(i))
            {
                builder.AppendNull();
            }
            else
            {
                var result = transform(data!.GetString(i));
                if (result is null)
                    builder.AppendNull();
                else
                    builder.Append(result);
            }
        }

        return Series.FromArrowArray(_series.Name, builder.Build(), DataType.String);
    }

    private Series ToBooleans(Func<string?, bool> predicate)
    {
        var data = _series.Data as StringChunkedArray;
        var builder = new BooleanArray.Builder();

        for (int i = 0; i < _series.Length; i++)
        {
            if (_series.IsNull(i))
            {
                builder.AppendNull();
            }
            else
            {
                builder.Append(predicate(data!.GetString(i)));
            }
        }

        return Series.FromArrowArray(_series.Name, builder.Build(), DataType.Boolean);
    }

    private Series ToInts(Func<string?, int> transform)
    {
        var data = _series.Data as StringChunkedArray;
        var builder = new Int32Array.Builder();

        for (int i = 0; i < _series.Length; i++)
        {
            if (_series.IsNull(i))
            {
                builder.AppendNull();
            }
            else
            {
                builder.Append(transform(data!.GetString(i)));
            }
        }

        return Series.FromArrowArray(_series.Name, builder.Build(), DataType.Int32);
    }
}
