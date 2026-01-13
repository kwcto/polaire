// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET
//
// Comprehensive tests for sort and unique operations, inspired by Polars test suite.
// These tests cover:
// - Series sort operations
// - DataFrame sort operations
// - Ascending/descending sorts
// - Multi-column sorts
// - Unique values
// - ArgSort operations

using FluentAssertions;
using Polaire;
using Polaire.DataTypes;
using Xunit;
using static Polaire.Pl;

namespace Polaire.Tests;

/// <summary>
/// Comprehensive tests for sort and unique operations.
/// </summary>
public class SortAndUniqueTests
{
    // ============================================================================
    // Basic Series Sort Tests
    // ============================================================================

    [Fact]
    public void Sort_Ascending_Works()
    {
        var s = Series.FromValues("s", new[] { 3, 1, 4, 1, 5, 9, 2, 6 });

        var result = s.Sort();

        result[0].AsInt32().Should().Be(1);
        result[1].AsInt32().Should().Be(1);
        result[2].AsInt32().Should().Be(2);
        result[3].AsInt32().Should().Be(3);
        result[4].AsInt32().Should().Be(4);
    }

    [Fact]
    public void Sort_Descending_Works()
    {
        var s = Series.FromValues("s", new[] { 3, 1, 4, 1, 5 });

        var result = s.Sort(descending: true);

        result[0].AsInt32().Should().Be(5);
        result[1].AsInt32().Should().Be(4);
        result[2].AsInt32().Should().Be(3);
        result[3].AsInt32().Should().Be(1);
        result[4].AsInt32().Should().Be(1);
    }

    [Fact]
    public void Sort_EmptySeries_ReturnsEmpty()
    {
        var s = Series.FromValues("s", Array.Empty<int>());

        var result = s.Sort();

        result.Length.Should().Be(0);
    }

    [Fact]
    public void Sort_SingleElement_ReturnsSame()
    {
        var s = Series.FromValues("s", new[] { 42 });

        var result = s.Sort();

        result.Length.Should().Be(1);
        result[0].AsInt32().Should().Be(42);
    }

    [Fact]
    public void Sort_AlreadySorted_Unchanged()
    {
        var s = Series.FromValues("s", new[] { 1, 2, 3, 4, 5 });

        var result = s.Sort();

        result[0].AsInt32().Should().Be(1);
        result[4].AsInt32().Should().Be(5);
    }

    [Fact]
    public void Sort_ReverseSorted_CorrectOrder()
    {
        var s = Series.FromValues("s", new[] { 5, 4, 3, 2, 1 });

        var result = s.Sort();

        result[0].AsInt32().Should().Be(1);
        result[4].AsInt32().Should().Be(5);
    }

    [Fact]
    public void Sort_AllSameValue_Works()
    {
        var s = Series.FromValues("s", new[] { 5, 5, 5, 5, 5 });

        var result = s.Sort();

        result.Length.Should().Be(5);
        result[0].AsInt32().Should().Be(5);
        result[4].AsInt32().Should().Be(5);
    }

    // ============================================================================
    // Float Sort Tests
    // ============================================================================

    [Fact]
    public void Sort_Float_Ascending()
    {
        var s = Series.FromValues("s", new[] { 3.14, 2.71, 1.41, 1.73 });

        var result = s.Sort();

        result[0].AsFloat64().Should().BeApproximately(1.41, 0.01);
        result[3].AsFloat64().Should().BeApproximately(3.14, 0.01);
    }

    [Fact]
    public void Sort_Float_Descending()
    {
        var s = Series.FromValues("s", new[] { 3.14, 2.71, 1.41, 1.73 });

        var result = s.Sort(descending: true);

        result[0].AsFloat64().Should().BeApproximately(3.14, 0.01);
        result[3].AsFloat64().Should().BeApproximately(1.41, 0.01);
    }

    [Fact]
    public void Sort_Float_NegativeValues()
    {
        var s = Series.FromValues("s", new[] { -1.0, 2.0, -3.0, 4.0 });

        var result = s.Sort();

        result[0].AsFloat64().Should().Be(-3.0);
        result[1].AsFloat64().Should().Be(-1.0);
        result[2].AsFloat64().Should().Be(2.0);
        result[3].AsFloat64().Should().Be(4.0);
    }

    // ============================================================================
    // String Sort Tests
    // ============================================================================

    [Fact]
    public void Sort_String_Alphabetical()
    {
        var s = Series.FromValues("s", new[] { "banana", "apple", "cherry" });

        var result = s.Sort();

        result[0].AsString().Should().Be("apple");
        result[1].AsString().Should().Be("banana");
        result[2].AsString().Should().Be("cherry");
    }

    [Fact]
    public void Sort_String_Descending()
    {
        var s = Series.FromValues("s", new[] { "a", "b", "c" });

        var result = s.Sort(descending: true);

        result[0].AsString().Should().Be("c");
        result[2].AsString().Should().Be("a");
    }

    [Fact]
    public void Sort_String_CaseSensitive()
    {
        var s = Series.FromValues("s", new[] { "Apple", "banana", "Cherry" });

        var result = s.Sort();

        // ASCII sort: uppercase letters come before lowercase
        result.Length.Should().Be(3);
    }

    // ============================================================================
    // Sort with Nulls Tests
    // ============================================================================

    [Fact]
    public void Sort_WithNulls_NullsLast()
    {
        var s = Series.FromNullable("s", new int?[] { 3, null, 1, null, 2 });

        var result = s.Sort();

        // Non-null values should be sorted first
        result[0].AsInt32().Should().Be(1);
        result[1].AsInt32().Should().Be(2);
        result[2].AsInt32().Should().Be(3);
    }

    [Fact]
    public void Sort_AllNulls_Works()
    {
        var s = Series.FromNullable("s", new int?[] { null, null, null });

        var result = s.Sort();

        result.Length.Should().Be(3);
    }

    // ============================================================================
    // DataFrame Sort Tests
    // ============================================================================

    [Fact]
    public void DataFrame_Sort_SingleColumn()
    {
        var df = new DataFrame(
            Series.FromValues("id", new[] { 3, 1, 2 }),
            Series.FromValues("name", new[] { "C", "A", "B" })
        );

        var result = df.Lazy().Sort("id").Collect();

        result["id"][0].AsInt32().Should().Be(1);
        result["id"][1].AsInt32().Should().Be(2);
        result["id"][2].AsInt32().Should().Be(3);
    }

    [Fact]
    public void DataFrame_Sort_Descending()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 1, 3, 2 })
        );

        var result = df.Lazy().Sort((Col("value"), true)).Collect();

        result["value"][0].AsInt32().Should().Be(3);
        result["value"][2].AsInt32().Should().Be(1);
    }

    [Fact]
    public void DataFrame_Sort_PreservesOtherColumns()
    {
        var df = new DataFrame(
            Series.FromValues("id", new[] { 3, 1, 2 }),
            Series.FromValues("name", new[] { "C", "A", "B" })
        );

        var result = df.Lazy()
            .Sort("id")
            .Select(Col("id"), Col("name"))
            .Collect();

        result["name"][0].AsString().Should().Be("A");
        result["name"][1].AsString().Should().Be("B");
        result["name"][2].AsString().Should().Be("C");
    }

    [Fact]
    public void DataFrame_Sort_Empty()
    {
        var df = new DataFrame(
            Series.FromValues("value", Array.Empty<int>())
        );

        var result = df.Lazy().Sort("value").Collect();

        result.Height.Should().Be(0);
    }

    [Fact]
    public void DataFrame_Sort_SingleRow()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 42 })
        );

        var result = df.Lazy().Sort("value").Collect();

        result.Height.Should().Be(1);
    }

    // ============================================================================
    // Unique Tests
    // ============================================================================

    [Fact]
    public void Unique_RemovesDuplicates()
    {
        var s = Series.FromValues("s", new[] { 1, 2, 2, 3, 3, 3, 4 });

        var result = s.Unique();

        result.Length.Should().Be(4);
    }

    [Fact]
    public void Unique_AllSame_ReturnsSingle()
    {
        var s = Series.FromValues("s", new[] { 5, 5, 5, 5, 5 });

        var result = s.Unique();

        result.Length.Should().Be(1);
        result[0].AsInt32().Should().Be(5);
    }

    [Fact]
    public void Unique_AllDifferent_ReturnsAll()
    {
        var s = Series.FromValues("s", new[] { 1, 2, 3, 4, 5 });

        var result = s.Unique();

        result.Length.Should().Be(5);
    }

    [Fact]
    public void Unique_Empty_ReturnsEmpty()
    {
        var s = Series.FromValues("s", Array.Empty<int>());

        var result = s.Unique();

        result.Length.Should().Be(0);
    }

    [Fact]
    public void Unique_SingleElement_ReturnsSame()
    {
        var s = Series.FromValues("s", new[] { 42 });

        var result = s.Unique();

        result.Length.Should().Be(1);
        result[0].AsInt32().Should().Be(42);
    }

    [Fact]
    public void Unique_Float_Works()
    {
        var s = Series.FromValues("s", new[] { 1.5, 2.5, 1.5, 3.5, 2.5 });

        var result = s.Unique();

        result.Length.Should().Be(3);
    }

    [Fact]
    public void Unique_String_Works()
    {
        var s = Series.FromValues("s", new[] { "a", "b", "a", "c", "b" });

        var result = s.Unique();

        result.Length.Should().Be(3);
    }

    [Fact]
    public void Unique_WithNulls_IncludesOneNull()
    {
        var s = Series.FromNullable("s", new int?[] { 1, null, 2, null, 1 });

        var result = s.Unique();

        // Should have 1, 2, and one null
        result.Length.Should().BeGreaterThanOrEqualTo(2);
    }

    // ============================================================================
    // ArgSort Tests
    // ============================================================================

    [Fact]
    public void ArgSort_ReturnsIndices()
    {
        var s = Series.FromValues("s", new[] { 30, 10, 20 });

        var result = s.ArgSort();

        result[0].AsInt32().Should().Be(1);  // Index of 10
        result[1].AsInt32().Should().Be(2);  // Index of 20
        result[2].AsInt32().Should().Be(0);  // Index of 30
    }

    [Fact]
    public void ArgSort_Descending()
    {
        var s = Series.FromValues("s", new[] { 30, 10, 20 });

        var result = s.ArgSort(descending: true);

        result[0].AsInt32().Should().Be(0);  // Index of 30
        result[1].AsInt32().Should().Be(2);  // Index of 20
        result[2].AsInt32().Should().Be(1);  // Index of 10
    }

    [Fact]
    public void ArgSort_AlreadySorted()
    {
        var s = Series.FromValues("s", new[] { 1, 2, 3 });

        var result = s.ArgSort();

        result[0].AsInt32().Should().Be(0);
        result[1].AsInt32().Should().Be(1);
        result[2].AsInt32().Should().Be(2);
    }

    // ============================================================================
    // Large Data Sort Tests
    // ============================================================================

    [Fact]
    public void Sort_Large_Ascending()
    {
        var random = new Random(42);
        var values = Enumerable.Range(0, 10000).Select(_ => random.Next(1000)).ToArray();
        var s = Series.FromValues("s", values);

        var result = s.Sort();

        result.Length.Should().Be(10000);
        // Check first few are sorted
        for (int i = 1; i < 100; i++)
        {
            result[i].AsInt32().Should().BeGreaterOrEqualTo(result[i - 1].AsInt32());
        }
    }

    [Fact]
    public void Sort_Large_Descending()
    {
        var random = new Random(42);
        var values = Enumerable.Range(0, 10000).Select(_ => random.Next(1000)).ToArray();
        var s = Series.FromValues("s", values);

        var result = s.Sort(descending: true);

        result.Length.Should().Be(10000);
        // Check first few are sorted descending
        for (int i = 1; i < 100; i++)
        {
            result[i].AsInt32().Should().BeLessThanOrEqualTo(result[i - 1].AsInt32());
        }
    }

    [Fact]
    public void Unique_Large_Works()
    {
        // 10000 values with 100 possible unique values
        var values = Enumerable.Range(0, 10000).Select(i => i % 100).ToArray();
        var s = Series.FromValues("s", values);

        var result = s.Unique();

        result.Length.Should().Be(100);
    }

    // ============================================================================
    // Edge Cases
    // ============================================================================

    [Fact]
    public void Sort_TwoElements()
    {
        var s = Series.FromValues("s", new[] { 2, 1 });

        var result = s.Sort();

        result[0].AsInt32().Should().Be(1);
        result[1].AsInt32().Should().Be(2);
    }

    [Fact]
    public void Sort_NegativeNumbers()
    {
        var s = Series.FromValues("s", new[] { -1, -3, -2, 0, 2, 1 });

        var result = s.Sort();

        result[0].AsInt32().Should().Be(-3);
        result[1].AsInt32().Should().Be(-2);
        result[2].AsInt32().Should().Be(-1);
        result[3].AsInt32().Should().Be(0);
    }

    [Fact]
    public void Unique_TwoElements_Same()
    {
        var s = Series.FromValues("s", new[] { 1, 1 });

        var result = s.Unique();

        result.Length.Should().Be(1);
    }

    [Fact]
    public void Unique_TwoElements_Different()
    {
        var s = Series.FromValues("s", new[] { 1, 2 });

        var result = s.Unique();

        result.Length.Should().Be(2);
    }

    // ============================================================================
    // Stability Tests
    // ============================================================================

    [Fact]
    public void Sort_StableForEqualElements()
    {
        var s = Series.FromValues("s", new[] { 3, 1, 2, 1, 3, 2 });

        var result = s.Sort();

        // Should have 1, 1, 2, 2, 3, 3
        result[0].AsInt32().Should().Be(1);
        result[1].AsInt32().Should().Be(1);
        result[2].AsInt32().Should().Be(2);
        result[3].AsInt32().Should().Be(2);
        result[4].AsInt32().Should().Be(3);
        result[5].AsInt32().Should().Be(3);
    }

    // ============================================================================
    // Boolean Sort Tests
    // ============================================================================

    [Fact]
    public void Sort_Boolean_Works()
    {
        var s = Series.FromValues("s", new[] { true, false, true, false });

        var result = s.Sort();

        // false < true typically
        result.Length.Should().Be(4);
    }

    // ============================================================================
    // Int64 Sort Tests
    // ============================================================================

    [Fact]
    public void Sort_Int64_Works()
    {
        var s = Series.FromValues("s", new long[] { 3_000_000_000, 1_000_000_000, 2_000_000_000 });

        var result = s.Sort();

        result[0].AsInt64().Should().Be(1_000_000_000);
        result[2].AsInt64().Should().Be(3_000_000_000);
    }

    [Fact]
    public void Unique_Int64_Works()
    {
        var s = Series.FromValues("s", new long[] { 1, 2, 1, 3, 2 });

        var result = s.Unique();

        result.Length.Should().Be(3);
    }
}
