// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using Apache.Arrow;
using Polaire.DataTypes;
using Polaire.Core;

namespace Polaire.Compute;

/// <summary>
/// Struct operations namespace for Series containing Struct data.
/// Provides operations for accessing and manipulating struct fields.
/// Accessed via .Struct property on a Struct-type Series.
/// </summary>
public sealed class StructOperations
{
    private readonly Series _series;

    public StructOperations(Series series)
    {
        if (series.DataType is not DataType.StructType)
            throw new ArgumentException("Struct operations require Struct series");
        _series = series;
    }

    // ============================================================================
    // Field Access
    // ============================================================================

    /// <summary>
    /// Gets the value of a field from each struct.
    /// </summary>
    public Series Field(string fieldName)
    {
        var data = _series.Data as StructChunkedArray;
        if (data == null) throw new InvalidOperationException("Invalid struct data");

        return data.GetField(fieldName, _series.Name + "." + fieldName);
    }

    /// <summary>
    /// Gets all field names in the struct.
    /// </summary>
    public IReadOnlyList<string> FieldNames()
    {
        var structType = (DataType.StructType)_series.DataType;
        return structType.Fields.Select(f => f.Name).ToList();
    }

    /// <summary>
    /// Gets the number of fields in the struct.
    /// </summary>
    public int FieldCount()
    {
        var structType = (DataType.StructType)_series.DataType;
        return structType.Fields.Count;
    }

    // ============================================================================
    // Rename Fields
    // ============================================================================

    /// <summary>
    /// Renames fields in the struct.
    /// Returns the new struct type metadata (fields cannot be physically renamed without rebuilding).
    /// </summary>
    public DataType.StructType RenameFields(IReadOnlyDictionary<string, string> renameMap)
    {
        var structType = (DataType.StructType)_series.DataType;
        var newFields = structType.Fields.Select(f =>
        {
            var newName = renameMap.TryGetValue(f.Name, out var n) ? n : f.Name;
            return new DataTypes.Field(newName, f.Type, f.Nullable);
        }).ToArray();

        return DataType.Struct(newFields);
    }

    // ============================================================================
    // Conversion Operations
    // ============================================================================

    /// <summary>
    /// Converts each struct to a JSON string representation.
    /// </summary>
    public Series JsonEncode()
    {
        var data = _series.Data as StructChunkedArray;
        if (data == null) throw new InvalidOperationException("Invalid struct data");

        return data.ToJson(_series.Name);
    }

    /// <summary>
    /// Unnests the struct into separate columns.
    /// Returns a DataFrame with one column per struct field.
    /// </summary>
    public DataFrame Unnest()
    {
        var structType = (DataType.StructType)_series.DataType;
        var data = _series.Data as StructChunkedArray;
        if (data == null) throw new InvalidOperationException("Invalid struct data");

        var columns = new List<Series>();
        foreach (var field in structType.Fields)
        {
            columns.Add(data.GetField(field.Name, field.Name));
        }

        return new DataFrame(columns.ToArray());
    }
}
