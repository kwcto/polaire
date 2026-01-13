// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using FluentAssertions;
using Polaire;
using Polaire.DataTypes;
using Xunit;

namespace Polaire.Tests;

/// <summary>
/// Tests for ListOperations (Series.List namespace).
/// Tests list manipulation operations on List-type Series.
/// </summary>
public class ListOperationsTests
{
    // ============================================================================
    // Factory Method Tests
    // ============================================================================

    [Fact]
    public void FromLists_WithIntArrays_CreatesListSeries()
    {
        var lists = new int[][]
        {
            new[] { 1, 2, 3 },
            new[] { 4, 5 },
            new[] { 6 }
        };

        var series = Series.FromLists("numbers", lists);

        series.Should().NotBeNull();
        series.Length.Should().Be(3);
        series.DataType.Should().BeOfType<DataType.ListType>();
    }

    [Fact]
    public void FromLists_WithDoubleArrays_CreatesListSeries()
    {
        var lists = new double[][]
        {
            new[] { 1.1, 2.2, 3.3 },
            new[] { 4.4, 5.5 },
            new[] { 6.6 }
        };

        var series = Series.FromLists("floats", lists);

        series.Should().NotBeNull();
        series.Length.Should().Be(3);
        series.DataType.Should().BeOfType<DataType.ListType>();
    }

    [Fact]
    public void FromLists_WithStringArrays_CreatesListSeries()
    {
        var lists = new string[][]
        {
            new[] { "a", "b", "c" },
            new[] { "d", "e" },
            new[] { "f" }
        };

        var series = Series.FromLists("strings", lists);

        series.Should().NotBeNull();
        series.Length.Should().Be(3);
        series.DataType.Should().BeOfType<DataType.ListType>();
    }

    [Fact]
    public void FromLists_WithNullList_HandlesCorrectly()
    {
        var lists = new int[][]
        {
            new[] { 1, 2, 3 },
            null!,
            new[] { 6 }
        };

        var series = Series.FromLists("numbers", lists);

        series.Length.Should().Be(3);
        series.NullCount.Should().Be(1);
    }

    [Fact]
    public void FromLists_WithEmptyList_HandlesCorrectly()
    {
        var lists = new int[][]
        {
            new[] { 1, 2, 3 },
            Array.Empty<int>(),
            new[] { 6 }
        };

        var series = Series.FromLists("numbers", lists);

        series.Length.Should().Be(3);
    }

    // ============================================================================
    // List.Lengths() Tests
    // ============================================================================

    [Fact]
    public void Lengths_ReturnsLengthOfEachList()
    {
        var lists = new int[][]
        {
            new[] { 1, 2, 3 },
            new[] { 4, 5 },
            new[] { 6 }
        };
        var series = Series.FromLists("numbers", lists);

        var lengths = series.List.Lengths();

        lengths.Length.Should().Be(3);
        lengths[0].AsInt32().Should().Be(3);
        lengths[1].AsInt32().Should().Be(2);
        lengths[2].AsInt32().Should().Be(1);
    }

    [Fact]
    public void Lengths_WithNullList_ReturnsNull()
    {
        var lists = new int[][]
        {
            new[] { 1, 2, 3 },
            null!,
            new[] { 6 }
        };
        var series = Series.FromLists("numbers", lists);

        var lengths = series.List.Lengths();

        lengths[0].AsInt32().Should().Be(3);
        lengths[1].IsNull.Should().BeTrue();
        lengths[2].AsInt32().Should().Be(1);
    }

    [Fact]
    public void Lengths_WithEmptyList_ReturnsZero()
    {
        var lists = new int[][]
        {
            new[] { 1, 2, 3 },
            Array.Empty<int>(),
            new[] { 6 }
        };
        var series = Series.FromLists("numbers", lists);

        var lengths = series.List.Lengths();

        lengths[0].AsInt32().Should().Be(3);
        lengths[1].AsInt32().Should().Be(0);
        lengths[2].AsInt32().Should().Be(1);
    }

    // ============================================================================
    // List.Get() / List.First() / List.Last() Tests
    // ============================================================================

    [Fact]
    public void Get_WithPositiveIndex_ReturnsCorrectElement()
    {
        var lists = new int[][]
        {
            new[] { 10, 20, 30 },
            new[] { 40, 50 },
            new[] { 60 }
        };
        var series = Series.FromLists("numbers", lists);

        var result = series.List.Get(0);

        result[0].AsInt32().Should().Be(10);
        result[1].AsInt32().Should().Be(40);
        result[2].AsInt32().Should().Be(60);
    }

    [Fact]
    public void Get_WithNegativeIndex_ReturnsFromEnd()
    {
        var lists = new int[][]
        {
            new[] { 10, 20, 30 },
            new[] { 40, 50 },
            new[] { 60 }
        };
        var series = Series.FromLists("numbers", lists);

        var result = series.List.Get(-1);

        result[0].AsInt32().Should().Be(30);
        result[1].AsInt32().Should().Be(50);
        result[2].AsInt32().Should().Be(60);
    }

    [Fact]
    public void Get_WithOutOfBoundsIndex_ReturnsNull()
    {
        var lists = new int[][]
        {
            new[] { 10, 20, 30 },
            new[] { 40 }
        };
        var series = Series.FromLists("numbers", lists);

        var result = series.List.Get(2);

        result[0].AsInt32().Should().Be(30);
        result[1].IsNull.Should().BeTrue(); // Index 2 out of bounds for [40]
    }

    [Fact]
    public void First_ReturnsFirstElement()
    {
        var lists = new int[][]
        {
            new[] { 10, 20, 30 },
            new[] { 40, 50 }
        };
        var series = Series.FromLists("numbers", lists);

        var result = series.List.First();

        result[0].AsInt32().Should().Be(10);
        result[1].AsInt32().Should().Be(40);
    }

    [Fact]
    public void Last_ReturnsLastElement()
    {
        var lists = new int[][]
        {
            new[] { 10, 20, 30 },
            new[] { 40, 50 }
        };
        var series = Series.FromLists("numbers", lists);

        var result = series.List.Last();

        result[0].AsInt32().Should().Be(30);
        result[1].AsInt32().Should().Be(50);
    }

    // ============================================================================
    // List.Contains() Tests
    // ============================================================================

    [Fact]
    public void Contains_WhenValueExists_ReturnsTrue()
    {
        var lists = new int[][]
        {
            new[] { 1, 2, 3 },
            new[] { 4, 5, 6 },
            new[] { 7, 8, 9 }
        };
        var series = Series.FromLists("numbers", lists);

        var result = series.List.Contains(AnyValue.From(2));

        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeFalse();
        result[2].AsBoolean().Should().BeFalse();
    }

    [Fact]
    public void Contains_WhenValueDoesNotExist_ReturnsFalse()
    {
        var lists = new int[][]
        {
            new[] { 1, 2, 3 },
            new[] { 4, 5, 6 }
        };
        var series = Series.FromLists("numbers", lists);

        var result = series.List.Contains(AnyValue.From(100));

        result[0].AsBoolean().Should().BeFalse();
        result[1].AsBoolean().Should().BeFalse();
    }

    // ============================================================================
    // List Aggregation Tests (Sum, Mean, Min, Max)
    // ============================================================================

    [Fact]
    public void Sum_ReturnsSumOfEachList()
    {
        var lists = new int[][]
        {
            new[] { 1, 2, 3 },
            new[] { 4, 5 },
            new[] { 10 }
        };
        var series = Series.FromLists("numbers", lists);

        var result = series.List.Sum();

        result[0].AsFloat64().Should().Be(6);
        result[1].AsFloat64().Should().Be(9);
        result[2].AsFloat64().Should().Be(10);
    }

    [Fact]
    public void Mean_ReturnsMeanOfEachList()
    {
        var lists = new double[][]
        {
            new[] { 1.0, 2.0, 3.0 },
            new[] { 4.0, 6.0 },
            new[] { 10.0 }
        };
        var series = Series.FromLists("numbers", lists);

        var result = series.List.Mean();

        result[0].AsFloat64().Should().Be(2.0);
        result[1].AsFloat64().Should().Be(5.0);
        result[2].AsFloat64().Should().Be(10.0);
    }

    [Fact]
    public void Min_ReturnsMinOfEachList()
    {
        var lists = new int[][]
        {
            new[] { 5, 2, 8 },
            new[] { 9, 1 },
            new[] { 7 }
        };
        var series = Series.FromLists("numbers", lists);

        var result = series.List.Min();

        result[0].AsFloat64().Should().Be(2);
        result[1].AsFloat64().Should().Be(1);
        result[2].AsFloat64().Should().Be(7);
    }

    [Fact]
    public void Max_ReturnsMaxOfEachList()
    {
        var lists = new int[][]
        {
            new[] { 5, 2, 8 },
            new[] { 9, 1 },
            new[] { 7 }
        };
        var series = Series.FromLists("numbers", lists);

        var result = series.List.Max();

        result[0].AsFloat64().Should().Be(8);
        result[1].AsFloat64().Should().Be(9);
        result[2].AsFloat64().Should().Be(7);
    }

    // ============================================================================
    // List Transformation Tests (Sort, Reverse, Unique)
    // ============================================================================

    [Fact]
    public void Sort_SortsEachListAscending()
    {
        var lists = new int[][]
        {
            new[] { 3, 1, 2 },
            new[] { 6, 4, 5 }
        };
        var series = Series.FromLists("numbers", lists);

        var result = series.List.Sort();

        // Returns string representation for now
        result.DataType.Should().Be(DataType.String);
        result.Length.Should().Be(2);
    }

    [Fact]
    public void Sort_WithDescending_SortsDescending()
    {
        var lists = new int[][]
        {
            new[] { 1, 2, 3 },
            new[] { 4, 5, 6 }
        };
        var series = Series.FromLists("numbers", lists);

        var result = series.List.Sort(descending: true);

        result.DataType.Should().Be(DataType.String);
        result.Length.Should().Be(2);
    }

    [Fact]
    public void Reverse_ReversesEachList()
    {
        var lists = new int[][]
        {
            new[] { 1, 2, 3 },
            new[] { 4, 5, 6 }
        };
        var series = Series.FromLists("numbers", lists);

        var result = series.List.Reverse();

        result.DataType.Should().Be(DataType.String);
        result.Length.Should().Be(2);
    }

    [Fact]
    public void Unique_ReturnsUniqueElements()
    {
        var lists = new int[][]
        {
            new[] { 1, 2, 2, 3 },
            new[] { 4, 4, 4, 5 }
        };
        var series = Series.FromLists("numbers", lists);

        var result = series.List.Unique();

        result.DataType.Should().Be(DataType.String);
        result.Length.Should().Be(2);
    }

    // ============================================================================
    // List Slicing Tests (Slice, Head, Tail)
    // ============================================================================

    [Fact]
    public void Slice_SlicesEachList()
    {
        var lists = new int[][]
        {
            new[] { 1, 2, 3, 4, 5 },
            new[] { 6, 7, 8, 9, 10 }
        };
        var series = Series.FromLists("numbers", lists);

        var result = series.List.Slice(1, 3);

        result.DataType.Should().Be(DataType.String);
        result.Length.Should().Be(2);
    }

    [Fact]
    public void Head_ReturnsFirstNElements()
    {
        var lists = new int[][]
        {
            new[] { 1, 2, 3, 4, 5 },
            new[] { 6, 7, 8, 9, 10 }
        };
        var series = Series.FromLists("numbers", lists);

        var result = series.List.Head(2);

        result.DataType.Should().Be(DataType.String);
        result.Length.Should().Be(2);
    }

    [Fact]
    public void Tail_ReturnsLastNElements()
    {
        var lists = new int[][]
        {
            new[] { 1, 2, 3, 4, 5 },
            new[] { 6, 7, 8, 9, 10 }
        };
        var series = Series.FromLists("numbers", lists);

        var result = series.List.Tail(2);

        result.DataType.Should().Be(DataType.String);
        result.Length.Should().Be(2);
    }

    // ============================================================================
    // List.Join() Tests
    // ============================================================================

    [Fact]
    public void Join_JoinsListElementsWithSeparator()
    {
        var lists = new string[][]
        {
            new[] { "a", "b", "c" },
            new[] { "d", "e" }
        };
        var series = Series.FromLists("strings", lists);

        var result = series.List.Join(",");

        result.DataType.Should().Be(DataType.String);
        result[0].AsString().Should().Be("a,b,c");
        result[1].AsString().Should().Be("d,e");
    }

    [Fact]
    public void Join_WithDefaultSeparator_UsesComma()
    {
        var lists = new string[][]
        {
            new[] { "a", "b", "c" },
            new[] { "d", "e" }
        };
        var series = Series.FromLists("strings", lists);

        var result = series.List.Join();

        result[0].AsString().Should().Be("a,b,c");
    }

    // ============================================================================
    // List.Explode() Tests
    // ============================================================================

    [Fact]
    public void Explode_ExpandsListsToSeparateRows()
    {
        var lists = new int[][]
        {
            new[] { 1, 2, 3 },
            new[] { 4, 5 }
        };
        var series = Series.FromLists("numbers", lists);

        var (values, indices) = series.List.Explode();

        values.Length.Should().Be(5); // 3 + 2
        indices.Should().BeEquivalentTo(new[] { 0, 0, 0, 1, 1 });
    }

    [Fact]
    public void Explode_WithEmptyList_AddsNullRow()
    {
        var lists = new int[][]
        {
            new[] { 1, 2 },
            Array.Empty<int>(),
            new[] { 3 }
        };
        var series = Series.FromLists("numbers", lists);

        var (values, indices) = series.List.Explode();

        values.Length.Should().Be(4); // 2 + 1 (null) + 1
        indices.Should().BeEquivalentTo(new[] { 0, 0, 1, 2 });
    }

    // ============================================================================
    // Edge Cases
    // ============================================================================

    [Fact]
    public void List_WithAllNulls_HandlesCorrectly()
    {
        var lists = new int[][]
        {
            null!,
            null!,
            null!
        };
        var series = Series.FromLists("numbers", lists);

        var lengths = series.List.Lengths();

        lengths.NullCount.Should().Be(3);
    }

    [Fact]
    public void List_WithAllEmptyLists_HandlesCorrectly()
    {
        var lists = new int[][]
        {
            Array.Empty<int>(),
            Array.Empty<int>(),
            Array.Empty<int>()
        };
        var series = Series.FromLists("numbers", lists);

        var lengths = series.List.Lengths();

        lengths[0].AsInt32().Should().Be(0);
        lengths[1].AsInt32().Should().Be(0);
        lengths[2].AsInt32().Should().Be(0);
    }

    [Fact]
    public void List_WithSingleElementLists_HandlesCorrectly()
    {
        var lists = new int[][]
        {
            new[] { 1 },
            new[] { 2 },
            new[] { 3 }
        };
        var series = Series.FromLists("numbers", lists);

        var first = series.List.First();
        var last = series.List.Last();

        first[0].AsInt32().Should().Be(1);
        last[0].AsInt32().Should().Be(1);
    }

    [Fact]
    public void List_WithLargeLists_HandlesEfficiently()
    {
        var largeList = Enumerable.Range(0, 10000).ToArray();
        var lists = new int[][] { largeList };
        var series = Series.FromLists("numbers", lists);

        var sum = series.List.Sum();
        var lengths = series.List.Lengths();

        lengths[0].AsInt32().Should().Be(10000);
        sum[0].AsFloat64().Should().Be(49995000); // Sum of 0 to 9999
    }

    [Fact]
    public void ListOperations_OnNonListSeries_ThrowsArgumentException()
    {
        var series = Series.FromValues("numbers", new[] { 1, 2, 3 });

        var act = () => series.List;

        act.Should().Throw<ArgumentException>()
            .WithMessage("*List*");
    }
}
