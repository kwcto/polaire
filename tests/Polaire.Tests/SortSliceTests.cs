// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using FluentAssertions;
using Polaire;
using Polaire.DataTypes;
using Xunit;

namespace Polaire.Tests;

/// <summary>
/// Tests for sorting, slicing, and indexing operations.
/// </summary>
public class SortSliceTests
{
    // ============================================================================
    // Series Sort Tests
    // ============================================================================

    [Fact]
    public void Series_Sort_AscendingByDefault()
    {
        var series = Series.FromValues("values", new[] { 3, 1, 4, 1, 5 });

        var result = series.Sort();

        result[0].AsInt32().Should().Be(1);
        result[1].AsInt32().Should().Be(1);
        result[2].AsInt32().Should().Be(3);
        result[3].AsInt32().Should().Be(4);
        result[4].AsInt32().Should().Be(5);
    }

    [Fact]
    public void Series_Sort_Descending()
    {
        var series = Series.FromValues("values", new[] { 3, 1, 4, 1, 5 });

        var result = series.Sort(descending: true);

        result[0].AsInt32().Should().Be(5);
        result[1].AsInt32().Should().Be(4);
        result[2].AsInt32().Should().Be(3);
        result[3].AsInt32().Should().Be(1);
        result[4].AsInt32().Should().Be(1);
    }

    [Fact]
    public void Series_Sort_Float64_WorksCorrectly()
    {
        var series = Series.FromValues("values", new[] { 3.5, 1.2, 4.8, 2.1 });

        var result = series.Sort();

        result[0].AsFloat64().Should().BeApproximately(1.2, 0.001);
        result[1].AsFloat64().Should().BeApproximately(2.1, 0.001);
        result[2].AsFloat64().Should().BeApproximately(3.5, 0.001);
        result[3].AsFloat64().Should().BeApproximately(4.8, 0.001);
    }

    [Fact]
    public void Series_Sort_String_LexicographicOrder()
    {
        var series = Series.FromValues("values", new[] { "banana", "apple", "cherry" });

        var result = series.Sort();

        result[0].AsString().Should().Be("apple");
        result[1].AsString().Should().Be("banana");
        result[2].AsString().Should().Be("cherry");
    }

    [Fact]
    public void Series_Sort_PreservesName()
    {
        var series = Series.FromValues("myname", new[] { 3, 1, 2 });

        var result = series.Sort();

        result.Name.Should().Be("myname");
    }

    [Fact]
    public void Series_Sort_NullsLast_PutsNullsAtEnd()
    {
        var series = Series.FromNullable("values", new int?[] { 3, null, 1, null, 2 });

        var result = series.Sort(nullsLast: true);

        result[0].AsInt32().Should().Be(1);
        result[1].AsInt32().Should().Be(2);
        result[2].AsInt32().Should().Be(3);
        result.IsNull(3).Should().BeTrue();
        result.IsNull(4).Should().BeTrue();
    }

    // ============================================================================
    // Series ArgSort Tests
    // ============================================================================

    [Fact]
    public void Series_ArgSort_ReturnsIndices()
    {
        var series = Series.FromValues("values", new[] { 30, 10, 20 });

        var indices = series.ArgSort();

        indices[0].AsInt32().Should().Be(1); // Index of 10
        indices[1].AsInt32().Should().Be(2); // Index of 20
        indices[2].AsInt32().Should().Be(0); // Index of 30
    }

    [Fact]
    public void Series_ArgSort_Descending_ReturnsCorrectIndices()
    {
        var series = Series.FromValues("values", new[] { 30, 10, 20 });

        var indices = series.ArgSort(descending: true);

        indices[0].AsInt32().Should().Be(0); // Index of 30
        indices[1].AsInt32().Should().Be(2); // Index of 20
        indices[2].AsInt32().Should().Be(1); // Index of 10
    }

    // ============================================================================
    // Series Reverse Tests
    // ============================================================================

    [Fact]
    public void Series_Reverse_ReversesOrder()
    {
        var series = Series.FromValues("values", new[] { 1, 2, 3, 4, 5 });

        var result = series.Reverse();

        result[0].AsInt32().Should().Be(5);
        result[1].AsInt32().Should().Be(4);
        result[2].AsInt32().Should().Be(3);
        result[3].AsInt32().Should().Be(2);
        result[4].AsInt32().Should().Be(1);
    }

    [Fact]
    public void Series_Reverse_SingleElement_ReturnsSame()
    {
        var series = Series.FromValues("values", new[] { 42 });

        var result = series.Reverse();

        result[0].AsInt32().Should().Be(42);
    }

    // ============================================================================
    // Series Slice Tests
    // ============================================================================

    [Fact]
    public void Series_Slice_ReturnsSubsection()
    {
        var series = Series.FromValues("values", new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });

        var result = series.Slice(3, 4);

        result.Length.Should().Be(4);
        result[0].AsInt32().Should().Be(3);
        result[1].AsInt32().Should().Be(4);
        result[2].AsInt32().Should().Be(5);
        result[3].AsInt32().Should().Be(6);
    }

    [Fact]
    public void Series_Slice_FromBeginning()
    {
        var series = Series.FromValues("values", new[] { 10, 20, 30, 40, 50 });

        var result = series.Slice(0, 3);

        result.Length.Should().Be(3);
        result[0].AsInt32().Should().Be(10);
        result[1].AsInt32().Should().Be(20);
        result[2].AsInt32().Should().Be(30);
    }

    [Fact]
    public void Series_Slice_ToEnd()
    {
        var series = Series.FromValues("values", new[] { 10, 20, 30, 40, 50 });

        var result = series.Slice(3, 2);

        result.Length.Should().Be(2);
        result[0].AsInt32().Should().Be(40);
        result[1].AsInt32().Should().Be(50);
    }

    // ============================================================================
    // Series Head/Tail Tests
    // ============================================================================

    [Fact]
    public void Series_Head_DefaultFive()
    {
        var series = Series.FromValues("values", Enumerable.Range(0, 100).ToArray());

        var result = series.Head();

        result.Length.Should().Be(5);
        result[0].AsInt32().Should().Be(0);
        result[4].AsInt32().Should().Be(4);
    }

    [Fact]
    public void Series_Head_CustomN()
    {
        var series = Series.FromValues("values", Enumerable.Range(0, 100).ToArray());

        var result = series.Head(10);

        result.Length.Should().Be(10);
    }

    [Fact]
    public void Series_Head_NGreaterThanLength_ReturnsAll()
    {
        var series = Series.FromValues("values", new[] { 1, 2, 3 });

        var result = series.Head(10);

        result.Length.Should().Be(3);
    }

    [Fact]
    public void Series_Tail_DefaultFive()
    {
        var series = Series.FromValues("values", Enumerable.Range(0, 100).ToArray());

        var result = series.Tail();

        result.Length.Should().Be(5);
        result[0].AsInt32().Should().Be(95);
        result[4].AsInt32().Should().Be(99);
    }

    [Fact]
    public void Series_Tail_CustomN()
    {
        var series = Series.FromValues("values", Enumerable.Range(0, 100).ToArray());

        var result = series.Tail(10);

        result.Length.Should().Be(10);
        result[0].AsInt32().Should().Be(90);
    }

    [Fact]
    public void Series_Tail_NGreaterThanLength_ReturnsAll()
    {
        var series = Series.FromValues("values", new[] { 1, 2, 3 });

        var result = series.Tail(10);

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
            Series.FromValues("name", new[] { "Charlie", "Alice", "Bob" })
        );

        var result = df.Sort("id");

        result["id"][0].AsInt32().Should().Be(1);
        result["id"][1].AsInt32().Should().Be(2);
        result["id"][2].AsInt32().Should().Be(3);
        result["name"][0].AsString().Should().Be("Alice");
    }

    [Fact]
    public void DataFrame_Sort_Descending()
    {
        var df = new DataFrame(
            Series.FromValues("id", new[] { 1, 2, 3 }),
            Series.FromValues("value", new[] { 10, 20, 30 })
        );

        var result = df.Sort("id", descending: true);

        result["id"][0].AsInt32().Should().Be(3);
        result["id"][1].AsInt32().Should().Be(2);
        result["id"][2].AsInt32().Should().Be(1);
    }

    [Fact]
    public void DataFrame_Sort_MultipleColumns()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "B", "B" }),
            Series.FromValues("subgroup", new[] { 2, 1, 2, 1 }),
            Series.FromValues("value", new[] { 100, 200, 300, 400 })
        );

        var result = df.Sort(("group", false), ("subgroup", false));

        result["group"][0].AsString().Should().Be("A");
        result["subgroup"][0].AsInt32().Should().Be(1);
    }

    // ============================================================================
    // DataFrame Slice Tests
    // ============================================================================

    [Fact]
    public void DataFrame_Slice_ReturnsSubsection()
    {
        var df = new DataFrame(
            Series.FromValues("id", Enumerable.Range(0, 10).ToArray()),
            Series.FromValues("value", Enumerable.Range(0, 10).Select(i => i * 10.0).ToArray())
        );

        var result = df.Slice(3, 4);

        result.Height.Should().Be(4);
        result["id"][0].AsInt32().Should().Be(3);
        result["id"][3].AsInt32().Should().Be(6);
    }

    // ============================================================================
    // DataFrame Head/Tail Tests
    // ============================================================================

    [Fact]
    public void DataFrame_Head_DefaultFive()
    {
        var df = new DataFrame(
            Series.FromValues("id", Enumerable.Range(0, 100).ToArray())
        );

        var result = df.Head();

        result.Height.Should().Be(5);
    }

    [Fact]
    public void DataFrame_Head_CustomN()
    {
        var df = new DataFrame(
            Series.FromValues("id", Enumerable.Range(0, 100).ToArray())
        );

        var result = df.Head(10);

        result.Height.Should().Be(10);
    }

    [Fact]
    public void DataFrame_Tail_DefaultFive()
    {
        var df = new DataFrame(
            Series.FromValues("id", Enumerable.Range(0, 100).ToArray())
        );

        var result = df.Tail();

        result.Height.Should().Be(5);
        result["id"][0].AsInt32().Should().Be(95);
    }

    [Fact]
    public void DataFrame_Tail_CustomN()
    {
        var df = new DataFrame(
            Series.FromValues("id", Enumerable.Range(0, 100).ToArray())
        );

        var result = df.Tail(10);

        result.Height.Should().Be(10);
        result["id"][0].AsInt32().Should().Be(90);
    }

    // ============================================================================
    // DataFrame Take Tests
    // ============================================================================

    [Fact]
    public void DataFrame_Take_SelectsSpecificRows()
    {
        var df = new DataFrame(
            Series.FromValues("id", new[] { 0, 1, 2, 3, 4, 5 }),
            Series.FromValues("name", new[] { "a", "b", "c", "d", "e", "f" })
        );

        var result = df.Take(new[] { 1, 3, 5 });

        result.Height.Should().Be(3);
        result["id"][0].AsInt32().Should().Be(1);
        result["id"][1].AsInt32().Should().Be(3);
        result["id"][2].AsInt32().Should().Be(5);
        result["name"][0].AsString().Should().Be("b");
    }

    [Fact]
    public void DataFrame_Take_DuplicateIndices_Duplicates()
    {
        var df = new DataFrame(
            Series.FromValues("id", new[] { 0, 1, 2 }),
            Series.FromValues("value", new[] { 10, 20, 30 })
        );

        var result = df.Take(new[] { 0, 0, 1, 1 });

        result.Height.Should().Be(4);
        result["id"][0].AsInt32().Should().Be(0);
        result["id"][1].AsInt32().Should().Be(0);
        result["id"][2].AsInt32().Should().Be(1);
    }

    // ============================================================================
    // DataFrame Sample Tests
    // ============================================================================

    [Fact]
    public void DataFrame_Sample_ReturnsSameCountWhenSeeded()
    {
        var df = new DataFrame(
            Series.FromValues("id", Enumerable.Range(0, 100).ToArray())
        );

        var result1 = df.Sample(10, seed: 42);
        var result2 = df.Sample(10, seed: 42);

        result1.Height.Should().Be(10);
        result2.Height.Should().Be(10);
        // With same seed, should get same results
    }

    [Fact]
    public void DataFrame_Sample_Fraction_ReturnsFraction()
    {
        var df = new DataFrame(
            Series.FromValues("id", Enumerable.Range(0, 100).ToArray())
        );

        var result = df.Sample(0.1, seed: 42);

        result.Height.Should().Be(10);
    }

    // ============================================================================
    // Edge Cases
    // ============================================================================

    [Fact]
    public void Sort_EmptySeries_ReturnsEmpty()
    {
        var series = Series.FromValues("values", Array.Empty<int>());

        var result = series.Sort();

        result.Length.Should().Be(0);
    }

    [Fact]
    public void Sort_SingleElement_ReturnsSame()
    {
        var series = Series.FromValues("values", new[] { 42 });

        var result = series.Sort();

        result[0].AsInt32().Should().Be(42);
    }

    [Fact]
    public void Sort_AlreadySorted_ReturnsSameOrder()
    {
        var series = Series.FromValues("values", new[] { 1, 2, 3, 4, 5 });

        var result = series.Sort();

        result[0].AsInt32().Should().Be(1);
        result[4].AsInt32().Should().Be(5);
    }

    [Fact]
    public void Slice_ZeroLength_ReturnsEmpty()
    {
        var series = Series.FromValues("values", new[] { 1, 2, 3 });

        var result = series.Slice(1, 0);

        result.Length.Should().Be(0);
    }

    [Fact]
    public void DataFrame_Sort_PreservesOtherColumns()
    {
        var df = new DataFrame(
            Series.FromValues("key", new[] { 3, 1, 2 }),
            Series.FromValues("data", new[] { "c", "a", "b" })
        );

        var result = df.Sort("key");

        result["key"][0].AsInt32().Should().Be(1);
        result["data"][0].AsString().Should().Be("a");
        result["key"][1].AsInt32().Should().Be(2);
        result["data"][1].AsString().Should().Be("b");
    }

    [Fact]
    public void Series_Unique_RemovesDuplicates()
    {
        var series = Series.FromValues("values", new[] { 1, 2, 2, 3, 3, 3 });

        var result = series.Unique();

        result.Length.Should().Be(3);
    }

    [Fact]
    public void Series_Unique_PreservesOrder()
    {
        var series = Series.FromValues("values", new[] { 3, 1, 3, 2, 1, 2 });

        var result = series.Unique();

        result.Length.Should().Be(3);
        // First occurrence order: 3, 1, 2
    }
}
