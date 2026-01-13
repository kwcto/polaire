// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using Polaire.DataTypes;


namespace Polaire;

/// <summary>
/// Join operations for DataFrames.
/// </summary>
public static class JoinOperations
{
    public static DataFrame Join(
        DataFrame left,
        DataFrame right,
        string[] leftOn,
        string[] rightOn,
        JoinType how,
        string suffix)
    {
        if (leftOn.Length != rightOn.Length)
            throw new ArgumentException("Left and right join keys must have same length");

        // Build hash table for right DataFrame
        var rightIndex = BuildIndex(right, rightOn);

        return how switch
        {
            JoinType.Inner => InnerJoin(left, right, leftOn, rightOn, rightIndex, suffix),
            JoinType.Left => LeftJoin(left, right, leftOn, rightOn, rightIndex, suffix),
            JoinType.Right => RightJoin(left, right, leftOn, rightOn, suffix),
            JoinType.Outer => OuterJoin(left, right, leftOn, rightOn, rightIndex, suffix),
            JoinType.Semi => SemiJoin(left, leftOn, rightIndex),
            JoinType.Anti => AntiJoin(left, leftOn, rightIndex),
            JoinType.Cross => CrossJoin(left, right, suffix),
            _ => throw new ArgumentException($"Unknown join type: {how}")
        };
    }

    private static Dictionary<string, List<int>> BuildIndex(DataFrame df, string[] keys)
    {
        var index = new Dictionary<string, List<int>>();

        for (int i = 0; i < df.Height; i++)
        {
            var key = GetKey(df, keys, i);
            if (!index.TryGetValue(key, out var indices))
            {
                indices = new List<int>();
                index[key] = indices;
            }
            indices.Add(i);
        }

        return index;
    }

    private static string GetKey(DataFrame df, string[] keys, int row)
    {
        return string.Join("|", keys.Select(k => df[k][row].ToString()));
    }

    // ============================================================================
    // Inner Join
    // ============================================================================

    private static DataFrame InnerJoin(
        DataFrame left, DataFrame right,
        string[] leftOn, string[] rightOn,
        Dictionary<string, List<int>> rightIndex,
        string suffix)
    {
        var leftIndices = new List<int>();
        var rightIndices = new List<int>();

        for (int i = 0; i < left.Height; i++)
        {
            var key = GetKey(left, leftOn, i);
            if (rightIndex.TryGetValue(key, out var matches))
            {
                foreach (var rightIdx in matches)
                {
                    leftIndices.Add(i);
                    rightIndices.Add(rightIdx);
                }
            }
        }

        return BuildJoinResult(left, right, leftIndices, rightIndices, leftOn, rightOn, suffix);
    }

    // ============================================================================
    // Left Join
    // ============================================================================

    private static DataFrame LeftJoin(
        DataFrame left, DataFrame right,
        string[] leftOn, string[] rightOn,
        Dictionary<string, List<int>> rightIndex,
        string suffix)
    {
        var leftIndices = new List<int>();
        var rightIndices = new List<int?>(); // Nullable for non-matches

        for (int i = 0; i < left.Height; i++)
        {
            var key = GetKey(left, leftOn, i);
            if (rightIndex.TryGetValue(key, out var matches))
            {
                foreach (var rightIdx in matches)
                {
                    leftIndices.Add(i);
                    rightIndices.Add(rightIdx);
                }
            }
            else
            {
                leftIndices.Add(i);
                rightIndices.Add(null);
            }
        }

        return BuildJoinResultWithNulls(left, right, leftIndices, rightIndices, leftOn, rightOn, suffix);
    }

    // ============================================================================
    // Right Join
    // ============================================================================

    private static DataFrame RightJoin(
        DataFrame left, DataFrame right,
        string[] leftOn, string[] rightOn,
        string suffix)
    {
        // Right join is left join with swapped DataFrames
        var swappedResult = LeftJoin(right, left, rightOn, leftOn, BuildIndex(left, leftOn), suffix);

        // Reorder columns: left columns first, then right
        var resultColumns = new List<Series>();
        var rightCols = new HashSet<string>(right.Columns);

        foreach (var col in left.Columns)
        {
            var seriesName = rightCols.Contains(col) && !rightOn.Contains(col) ? col + suffix : col;
            if (swappedResult.Columns.Contains(seriesName))
                resultColumns.Add(swappedResult[seriesName]);
        }

        foreach (var col in right.Columns)
        {
            if (!resultColumns.Any(c => c.Name == col))
                resultColumns.Add(swappedResult[col]);
        }

        return new DataFrame(resultColumns);
    }

    // ============================================================================
    // Outer Join
    // ============================================================================

    private static DataFrame OuterJoin(
        DataFrame left, DataFrame right,
        string[] leftOn, string[] rightOn,
        Dictionary<string, List<int>> rightIndex,
        string suffix)
    {
        var leftIndices = new List<int?>();
        var rightIndices = new List<int?>();
        var matchedRight = new HashSet<int>();

        // First pass: all left rows with matches
        for (int i = 0; i < left.Height; i++)
        {
            var key = GetKey(left, leftOn, i);
            if (rightIndex.TryGetValue(key, out var matches))
            {
                foreach (var rightIdx in matches)
                {
                    leftIndices.Add(i);
                    rightIndices.Add(rightIdx);
                    matchedRight.Add(rightIdx);
                }
            }
            else
            {
                leftIndices.Add(i);
                rightIndices.Add(null);
            }
        }

        // Second pass: unmatched right rows
        for (int i = 0; i < right.Height; i++)
        {
            if (!matchedRight.Contains(i))
            {
                leftIndices.Add(null);
                rightIndices.Add(i);
            }
        }

        return BuildOuterJoinResult(left, right, leftIndices, rightIndices, leftOn, rightOn, suffix);
    }

    // ============================================================================
    // Semi Join
    // ============================================================================

    private static DataFrame SemiJoin(
        DataFrame left,
        string[] leftOn,
        Dictionary<string, List<int>> rightIndex)
    {
        var indices = new List<int>();

        for (int i = 0; i < left.Height; i++)
        {
            var key = GetKey(left, leftOn, i);
            if (rightIndex.ContainsKey(key))
            {
                indices.Add(i);
            }
        }

        return left.Take(indices.ToArray());
    }

    // ============================================================================
    // Anti Join
    // ============================================================================

    private static DataFrame AntiJoin(
        DataFrame left,
        string[] leftOn,
        Dictionary<string, List<int>> rightIndex)
    {
        var indices = new List<int>();

        for (int i = 0; i < left.Height; i++)
        {
            var key = GetKey(left, leftOn, i);
            if (!rightIndex.ContainsKey(key))
            {
                indices.Add(i);
            }
        }

        return left.Take(indices.ToArray());
    }

    // ============================================================================
    // Cross Join
    // ============================================================================

    private static DataFrame CrossJoin(DataFrame left, DataFrame right, string suffix)
    {
        var leftIndices = new List<int>();
        var rightIndices = new List<int>();

        for (int i = 0; i < left.Height; i++)
        {
            for (int j = 0; j < right.Height; j++)
            {
                leftIndices.Add(i);
                rightIndices.Add(j);
            }
        }

        return BuildJoinResult(left, right, leftIndices, rightIndices, Array.Empty<string>(), Array.Empty<string>(), suffix);
    }

    // ============================================================================
    // Result Building
    // ============================================================================

    private static DataFrame BuildJoinResult(
        DataFrame left, DataFrame right,
        List<int> leftIndices, List<int> rightIndices,
        string[] leftOn, string[] rightOn,
        string suffix)
    {
        var resultColumns = new List<Series>();
        var rightOnSet = new HashSet<string>(rightOn);

        // Left columns
        foreach (var col in left.Columns)
        {
            var indices = leftIndices.ToArray();
            resultColumns.Add(Compute.SeriesOperations.Take(left[col], indices));
        }

        // Right columns (excluding join keys that are duplicates)
        foreach (var col in right.Columns)
        {
            if (rightOnSet.Contains(col) && leftOn.Contains(col))
                continue; // Skip duplicate join key

            var indices = rightIndices.ToArray();
            var series = Compute.SeriesOperations.Take(right[col], indices);

            // Rename if collision
            var finalName = left.Columns.Contains(col) ? col + suffix : col;
            resultColumns.Add(series.Rename(finalName));
        }

        return new DataFrame(resultColumns);
    }

    private static DataFrame BuildJoinResultWithNulls(
        DataFrame left, DataFrame right,
        List<int> leftIndices, List<int?> rightIndices,
        string[] leftOn, string[] rightOn,
        string suffix)
    {
        var resultColumns = new List<Series>();
        var rightOnSet = new HashSet<string>(rightOn);

        // Left columns
        foreach (var col in left.Columns)
        {
            var indices = leftIndices.ToArray();
            resultColumns.Add(Compute.SeriesOperations.Take(left[col], indices));
        }

        // Right columns with null handling
        foreach (var col in right.Columns)
        {
            if (rightOnSet.Contains(col) && leftOn.Contains(col))
                continue;

            var series = BuildNullableColumn(right[col], rightIndices);
            var finalName = left.Columns.Contains(col) ? col + suffix : col;
            resultColumns.Add(series.Rename(finalName));
        }

        return new DataFrame(resultColumns);
    }

    private static DataFrame BuildOuterJoinResult(
        DataFrame left, DataFrame right,
        List<int?> leftIndices, List<int?> rightIndices,
        string[] leftOn, string[] rightOn,
        string suffix)
    {
        var resultColumns = new List<Series>();
        var rightOnSet = new HashSet<string>(rightOn);

        // Left columns with null handling
        foreach (var col in left.Columns)
        {
            var series = BuildNullableColumn(left[col], leftIndices);
            resultColumns.Add(series);
        }

        // Right columns with null handling
        foreach (var col in right.Columns)
        {
            if (rightOnSet.Contains(col) && leftOn.Contains(col))
                continue;

            var series = BuildNullableColumn(right[col], rightIndices);
            var finalName = left.Columns.Contains(col) ? col + suffix : col;
            resultColumns.Add(series.Rename(finalName));
        }

        return new DataFrame(resultColumns);
    }

    private static Series BuildNullableColumn(Series source, List<int?> indices)
    {
        var values = new List<AnyValue>();
        foreach (var idx in indices)
        {
            if (idx.HasValue)
                values.Add(source[idx.Value]);
            else
                values.Add(AnyValue.Null);
        }

        return BuildSeriesFromAnyValues(source.Name, values, source.DataType);
    }

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
}
