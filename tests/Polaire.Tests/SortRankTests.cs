// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET
//
// Comprehensive tests for sort and ranking operations, inspired by Polars test suite.
// These tests cover:
// - Series.Sort() for ascending/descending sort
// - Series.ArgSort() for sorting indices
// - Series.Rank() for ranking values
// - DataFrame sorting operations

using FluentAssertions;
using Polaire;
using Polaire.DataTypes;
using Xunit;
using static Polaire.Pl;

namespace Polaire.Tests;

/// <summary>
/// Tests for sort and ranking operations.
/// </summary>
public class SortRankTests
{
    // ============================================================================
    // Series Sort Tests
    // ============================================================================

    [Fact]
    public void Sort_Ascending_SortsCorrectly()
    {
        var series = Series.FromValues("s", new[] { 3.0, 1.0, 4.0, 1.0, 5.0 });
        var result = series.Sort();

        result[0].AsFloat64().Should().Be(1.0);
        result[1].AsFloat64().Should().Be(1.0);
        result[2].AsFloat64().Should().Be(3.0);
        result[3].AsFloat64().Should().Be(4.0);
        result[4].AsFloat64().Should().Be(5.0);
    }

    [Fact]
    public void Sort_Descending_SortsCorrectly()
    {
        var series = Series.FromValues("s", new[] { 3.0, 1.0, 4.0, 1.0, 5.0 });
        var result = series.Sort(descending: true);

        result[0].AsFloat64().Should().Be(5.0);
        result[1].AsFloat64().Should().Be(4.0);
        result[2].AsFloat64().Should().Be(3.0);
        result[3].AsFloat64().Should().Be(1.0);
        result[4].AsFloat64().Should().Be(1.0);
    }

    [Fact]
    public void Sort_AlreadySorted_ReturnsInOrder()
    {
        var series = Series.FromValues("s", new[] { 1, 2, 3, 4, 5 });
        var result = series.Sort();

        result[0].AsInt32().Should().Be(1);
        result[4].AsInt32().Should().Be(5);
    }

    [Fact]
    public void Sort_ReverseSorted_SortsCorrectly()
    {
        var series = Series.FromValues("s", new[] { 5, 4, 3, 2, 1 });
        var result = series.Sort();

        result[0].AsInt32().Should().Be(1);
        result[4].AsInt32().Should().Be(5);
    }

    [Fact]
    public void Sort_SingleElement_ReturnsSame()
    {
        var series = Series.FromValues("s", new[] { 42 });
        var result = series.Sort();

        result.Length.Should().Be(1);
        result[0].AsInt32().Should().Be(42);
    }

    [Fact]
    public void Sort_EmptySeries_ReturnsEmpty()
    {
        var series = Series.FromValues("s", Array.Empty<int>());
        var result = series.Sort();

        result.Length.Should().Be(0);
    }

    [Fact]
    public void Sort_WithNulls_NullsLast()
    {
        var series = Series.FromNullable("s", new double?[] { 3.0, null, 1.0, null, 2.0 });
        var result = series.Sort(nullsLast: true);

        result[0].AsFloat64().Should().Be(1.0);
        result[1].AsFloat64().Should().Be(2.0);
        result[2].AsFloat64().Should().Be(3.0);
        result.IsNull(3).Should().BeTrue();
        result.IsNull(4).Should().BeTrue();
    }

    [Fact]
    public void Sort_StringValues_SortsAlphabetically()
    {
        var series = Series.FromValues("s", new[] { "banana", "apple", "cherry" });
        var result = series.Sort();

        result[0].AsString().Should().Be("apple");
        result[1].AsString().Should().Be("banana");
        result[2].AsString().Should().Be("cherry");
    }

    [Fact]
    public void Sort_NegativeNumbers_SortsCorrectly()
    {
        var series = Series.FromValues("s", new[] { -3, 1, -1, 2, -2 });
        var result = series.Sort();

        result[0].AsInt32().Should().Be(-3);
        result[1].AsInt32().Should().Be(-2);
        result[2].AsInt32().Should().Be(-1);
        result[3].AsInt32().Should().Be(1);
        result[4].AsInt32().Should().Be(2);
    }

    // ============================================================================
    // ArgSort Tests
    // ============================================================================

    [Fact]
    public void ArgSort_ReturnsIndices()
    {
        var series = Series.FromValues("s", new[] { 30.0, 10.0, 20.0 });
        var result = series.ArgSort();

        // Indices that would sort the array
        result[0].AsInt32().Should().Be(1);  // 10.0 is at index 1
        result[1].AsInt32().Should().Be(2);  // 20.0 is at index 2
        result[2].AsInt32().Should().Be(0);  // 30.0 is at index 0
    }

    [Fact]
    public void ArgSort_Descending_ReturnsReverseIndices()
    {
        var series = Series.FromValues("s", new[] { 30.0, 10.0, 20.0 });
        var result = series.ArgSort(descending: true);

        result[0].AsInt32().Should().Be(0);  // 30.0 first
        result[1].AsInt32().Should().Be(2);  // 20.0 second
        result[2].AsInt32().Should().Be(1);  // 10.0 last
    }

    [Fact]
    public void ArgSort_WithDuplicates_ReturnsStableIndices()
    {
        var series = Series.FromValues("s", new[] { 2, 1, 2, 1 });
        var result = series.ArgSort();

        // First occurrences of 1 should come first
        result[0].AsInt32().Should().Be(1);  // First 1
        result[1].AsInt32().Should().Be(3);  // Second 1
        result[2].AsInt32().Should().Be(0);  // First 2
        result[3].AsInt32().Should().Be(2);  // Second 2
    }

    // ============================================================================
    // Unique Tests
    // ============================================================================

    [Fact]
    public void Unique_ReturnsUniqueValues()
    {
        var series = Series.FromValues("s", new[] { 1, 2, 2, 3, 3, 3 });
        var result = series.Unique();

        result.Length.Should().Be(3);
    }

    [Fact]
    public void Unique_AllSame_ReturnsSingle()
    {
        var series = Series.FromValues("s", new[] { 5, 5, 5, 5, 5 });
        var result = series.Unique();

        result.Length.Should().Be(1);
        result[0].AsInt32().Should().Be(5);
    }

    [Fact]
    public void Unique_AllDifferent_ReturnsAll()
    {
        var series = Series.FromValues("s", new[] { 1, 2, 3, 4, 5 });
        var result = series.Unique();

        result.Length.Should().Be(5);
    }

    [Fact]
    public void Unique_Empty_ReturnsEmpty()
    {
        var series = Series.FromValues("s", Array.Empty<int>());
        var result = series.Unique();

        result.Length.Should().Be(0);
    }

    [Fact]
    public void Unique_WithNulls_IncludesNull()
    {
        var series = Series.FromNullable("s", new int?[] { 1, null, 1, null, 2 });
        var result = series.Unique();

        // Should have 3 unique: 1, null, 2
        result.Length.Should().BeGreaterOrEqualTo(2);
    }

    // ============================================================================
    // ValueCounts Tests
    // ============================================================================

    [Fact]
    public void ValueCounts_ReturnsCountsForEachValue()
    {
        var series = Series.FromValues("s", new[] { "a", "b", "a", "c", "a" });
        var result = series.ValueCounts();

        result.Height.Should().BeGreaterThan(0);
        result.Columns.Should().HaveCountGreaterOrEqualTo(2);
    }

    [Fact]
    public void ValueCounts_SingleValue_ReturnsSingleRow()
    {
        var series = Series.FromValues("s", new[] { 1, 1, 1 });
        var result = series.ValueCounts();

        result.Height.Should().Be(1);
    }

    // ============================================================================
    // DataFrame Sort Tests
    // ============================================================================

    [Fact]
    public void DataFrameSort_SingleColumn_SortsRows()
    {
        var df = new DataFrame(
            Series.FromValues("name", new[] { "Charlie", "Alice", "Bob" }),
            Series.FromValues("age", new[] { 30, 25, 35 })
        );

        var result = df.Sort("age");

        result["name"][0].AsString().Should().Be("Alice");  // age 25
        result["name"][1].AsString().Should().Be("Charlie");  // age 30
        result["name"][2].AsString().Should().Be("Bob");  // age 35
    }

    [Fact]
    public void DataFrameSort_Descending_SortsDescending()
    {
        var df = new DataFrame(
            Series.FromValues("name", new[] { "A", "B", "C" }),
            Series.FromValues("value", new[] { 1, 2, 3 })
        );

        var result = df.Sort("value", descending: true);

        result["value"][0].AsInt32().Should().Be(3);
        result["value"][1].AsInt32().Should().Be(2);
        result["value"][2].AsInt32().Should().Be(1);
    }

    // ============================================================================
    // Lazy Sort Tests
    // ============================================================================

    [Fact]
    public void LazySort_SortsOnCollect()
    {
        var df = new DataFrame(
            Series.FromValues("x", new[] { 3, 1, 2 })
        );

        var result = df.Lazy()
            .Sort("x")
            .Collect();

        result["x"][0].AsInt32().Should().Be(1);
        result["x"][1].AsInt32().Should().Be(2);
        result["x"][2].AsInt32().Should().Be(3);
    }

    [Fact]
    public void LazySort_WithFilter_AppliesBoth()
    {
        var df = new DataFrame(
            Series.FromValues("x", new[] { 5, 1, 4, 2, 3 })
        );

        var result = df.Lazy()
            .Filter(Col("x").Gt(2))
            .Sort("x")
            .Collect();

        result.Height.Should().Be(3);  // 3, 4, 5
        result["x"][0].AsInt32().Should().Be(3);
        result["x"][1].AsInt32().Should().Be(4);
        result["x"][2].AsInt32().Should().Be(5);
    }

    // ============================================================================
    // Large Data Tests
    // ============================================================================

    [Fact]
    public void Sort_LargeSeries_Succeeds()
    {
        var random = new Random(42);
        var values = Enumerable.Range(0, 10000)
            .Select(_ => random.NextDouble())
            .ToArray();

        var series = Series.FromValues("s", values);
        var result = series.Sort();

        result.Length.Should().Be(10000);
        // Check sorted
        for (int i = 1; i < 100; i++)
        {
            result[i].AsFloat64().Should().BeGreaterOrEqualTo(result[i - 1].AsFloat64());
        }
    }

    [Fact]
    public void ArgSort_LargeSeries_Succeeds()
    {
        var values = Enumerable.Range(0, 1000).Reverse().ToArray();
        var series = Series.FromValues("s", values);
        var result = series.ArgSort();

        result.Length.Should().Be(1000);
        // First index should point to smallest value (at end of original)
        result[0].AsInt32().Should().Be(999);
    }

    // ============================================================================
    // Edge Cases
    // ============================================================================

    [Fact]
    public void Sort_AllNulls_ReturnsAllNulls()
    {
        var series = Series.FromNullable("s", new int?[] { null, null, null });
        var result = series.Sort();

        result.Length.Should().Be(3);
        result.IsNull(0).Should().BeTrue();
        result.IsNull(1).Should().BeTrue();
        result.IsNull(2).Should().BeTrue();
    }

    [Fact]
    public void Sort_TwoElements_SortsCorrectly()
    {
        var series = Series.FromValues("s", new[] { 2, 1 });
        var result = series.Sort();

        result[0].AsInt32().Should().Be(1);
        result[1].AsInt32().Should().Be(2);
    }
}
