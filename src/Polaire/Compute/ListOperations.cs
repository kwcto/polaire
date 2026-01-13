// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using Apache.Arrow;
using Polaire.DataTypes;
using Polaire.Core;

namespace Polaire.Compute;

/// <summary>
/// List operations namespace for Series containing List data.
/// Provides operations for manipulating list elements within each row.
/// Accessed via .List property on a List-type Series.
/// </summary>
public sealed class ListOperations
{
    private readonly Series _series;

    public ListOperations(Series series)
    {
        if (series.DataType is not DataType.ListType)
            throw new ArgumentException("List operations require List series");
        _series = series;
    }

    // ============================================================================
    // Length and Size Operations
    // ============================================================================

    /// <summary>
    /// Returns the length (number of elements) of each list.
    /// </summary>
    public Series Lengths()
    {
        var data = _series.Data as ListChunkedArray;
        if (data == null) throw new InvalidOperationException("Invalid list data");

        var builder = new Int32Array.Builder();

        for (int i = 0; i < _series.Length; i++)
        {
            if (_series.IsNull(i))
            {
                builder.AppendNull();
            }
            else
            {
                builder.Append(data.GetListLength(i));
            }
        }

        return Series.FromArrowArray(_series.Name, builder.Build(), DataType.Int32);
    }

    // ============================================================================
    // Element Access
    // ============================================================================

    /// <summary>
    /// Gets the element at the specified index from each list.
    /// Negative indices count from the end.
    /// </summary>
    public Series Get(int index)
    {
        var data = _series.Data as ListChunkedArray;
        if (data == null) throw new InvalidOperationException("Invalid list data");

        var innerType = ((DataType.ListType)_series.DataType).Inner;
        return data.GetElementAt(index, _series.Name, innerType);
    }

    /// <summary>
    /// Gets the first element from each list.
    /// </summary>
    public Series First() => Get(0);

    /// <summary>
    /// Gets the last element from each list.
    /// </summary>
    public Series Last() => Get(-1);

    // ============================================================================
    // Membership Operations
    // ============================================================================

    /// <summary>
    /// Checks if each list contains the specified value.
    /// </summary>
    public Series Contains(AnyValue value)
    {
        var data = _series.Data as ListChunkedArray;
        if (data == null) throw new InvalidOperationException("Invalid list data");

        var builder = new BooleanArray.Builder();

        for (int i = 0; i < _series.Length; i++)
        {
            if (_series.IsNull(i))
            {
                builder.AppendNull();
            }
            else
            {
                builder.Append(data.ListContains(i, value));
            }
        }

        return Series.FromArrowArray(_series.Name, builder.Build(), DataType.Boolean);
    }

    // ============================================================================
    // Aggregations Over Lists
    // ============================================================================

    /// <summary>
    /// Sums the elements in each list.
    /// </summary>
    public Series Sum()
    {
        var data = _series.Data as ListChunkedArray;
        if (data == null) throw new InvalidOperationException("Invalid list data");

        return data.AggregateList(_series.Name, ListAggregationType.Sum);
    }

    /// <summary>
    /// Gets the mean of elements in each list.
    /// </summary>
    public Series Mean()
    {
        var data = _series.Data as ListChunkedArray;
        if (data == null) throw new InvalidOperationException("Invalid list data");

        return data.AggregateList(_series.Name, ListAggregationType.Mean);
    }

    /// <summary>
    /// Gets the minimum element in each list.
    /// </summary>
    public Series Min()
    {
        var data = _series.Data as ListChunkedArray;
        if (data == null) throw new InvalidOperationException("Invalid list data");

        return data.AggregateList(_series.Name, ListAggregationType.Min);
    }

    /// <summary>
    /// Gets the maximum element in each list.
    /// </summary>
    public Series Max()
    {
        var data = _series.Data as ListChunkedArray;
        if (data == null) throw new InvalidOperationException("Invalid list data");

        return data.AggregateList(_series.Name, ListAggregationType.Max);
    }

    // ============================================================================
    // Transformation Operations
    // ============================================================================

    /// <summary>
    /// Reverses the elements in each list.
    /// </summary>
    public Series Reverse()
    {
        var data = _series.Data as ListChunkedArray;
        if (data == null) throw new InvalidOperationException("Invalid list data");

        return data.TransformList(_series.Name, ListTransformation.Reverse);
    }

    /// <summary>
    /// Sorts the elements in each list.
    /// </summary>
    public Series Sort(bool descending = false)
    {
        var data = _series.Data as ListChunkedArray;
        if (data == null) throw new InvalidOperationException("Invalid list data");

        return data.TransformList(_series.Name,
            descending ? ListTransformation.SortDescending : ListTransformation.SortAscending);
    }

    /// <summary>
    /// Gets unique elements from each list.
    /// </summary>
    public Series Unique()
    {
        var data = _series.Data as ListChunkedArray;
        if (data == null) throw new InvalidOperationException("Invalid list data");

        return data.TransformList(_series.Name, ListTransformation.Unique);
    }

    // ============================================================================
    // Slicing Operations
    // ============================================================================

    /// <summary>
    /// Slices each list from offset to offset + length.
    /// </summary>
    public Series Slice(int offset, int? length = null)
    {
        var data = _series.Data as ListChunkedArray;
        if (data == null) throw new InvalidOperationException("Invalid list data");

        return data.SliceList(_series.Name, offset, length);
    }

    /// <summary>
    /// Gets the first n elements from each list.
    /// </summary>
    public Series Head(int n = 5)
    {
        return Slice(0, n);
    }

    /// <summary>
    /// Gets the last n elements from each list.
    /// </summary>
    public Series Tail(int n = 5)
    {
        var data = _series.Data as ListChunkedArray;
        if (data == null) throw new InvalidOperationException("Invalid list data");

        return data.TailList(_series.Name, n);
    }

    // ============================================================================
    // String Operations
    // ============================================================================

    /// <summary>
    /// Joins list elements into a string with separator.
    /// Only works for list of strings.
    /// </summary>
    public Series Join(string separator = ",")
    {
        var data = _series.Data as ListChunkedArray;
        if (data == null) throw new InvalidOperationException("Invalid list data");

        return data.JoinList(_series.Name, separator);
    }

    // ============================================================================
    // Explode
    // ============================================================================

    /// <summary>
    /// Explodes the list into separate rows.
    /// Returns a tuple of (values Series, indices into original Series).
    /// Use DataFrame.Explode() for a full DataFrame explosion.
    /// </summary>
    public (Series values, int[] indices) Explode()
    {
        var data = _series.Data as ListChunkedArray;
        if (data == null) throw new InvalidOperationException("Invalid list data");

        return data.ExplodeList(_series.Name);
    }
}

/// <summary>
/// Type of aggregation for list operations.
/// </summary>
internal enum ListAggregationType
{
    Sum,
    Mean,
    Min,
    Max
}

/// <summary>
/// Type of transformation for list operations.
/// </summary>
internal enum ListTransformation
{
    Reverse,
    SortAscending,
    SortDescending,
    Unique
}
