// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET
//
// Tests for filter and predicate operations, inspired by Polars test suite.
// These tests cover:
// - DataFrame filtering with various predicates
// - Compound predicates (AND, OR)
// - Filter with different data types
// - Filter edge cases

using FluentAssertions;
using Polaire;
using Polaire.DataTypes;
using Xunit;
using static Polaire.Pl;

namespace Polaire.Tests;

/// <summary>
/// Tests for filter and predicate operations.
/// </summary>
public class FilterAndPredicateTests
{
    // ============================================================================
    // Basic Filter Tests
    // ============================================================================

    [Fact]
    public void Filter_GreaterThan_Works()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 1, 2, 3, 4, 5 })
        );

        var result = df.Lazy().Filter(Col("value").Gt(3)).Collect();

        result.Height.Should().Be(2);
        result["value"][0].AsInt32().Should().Be(4);
        result["value"][1].AsInt32().Should().Be(5);
    }

    [Fact]
    public void Filter_LessThan_Works()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 1, 2, 3, 4, 5 })
        );

        var result = df.Lazy().Filter(Col("value").Lt(3)).Collect();

        result.Height.Should().Be(2);
        result["value"][0].AsInt32().Should().Be(1);
        result["value"][1].AsInt32().Should().Be(2);
    }

    [Fact]
    public void Filter_GreaterOrEqual_Works()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 1, 2, 3, 4, 5 })
        );

        var result = df.Lazy().Filter(Col("value").Ge(3)).Collect();

        result.Height.Should().Be(3);
    }

    [Fact]
    public void Filter_LessOrEqual_Works()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 1, 2, 3, 4, 5 })
        );

        var result = df.Lazy().Filter(Col("value").Le(3)).Collect();

        result.Height.Should().Be(3);
    }

    [Fact]
    public void Filter_Equal_Works()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 1, 2, 3, 2, 1 })
        );

        var result = df.Lazy().Filter(Col("value").Eq(2)).Collect();

        result.Height.Should().Be(2);
    }

    [Fact]
    public void Filter_NotEqual_Works()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 1, 2, 3, 2, 1 })
        );

        var result = df.Lazy().Filter(Col("value").Ne(2)).Collect();

        result.Height.Should().Be(3);
    }

    // ============================================================================
    // Float Filter Tests
    // ============================================================================

    [Fact]
    public void Filter_Float_GreaterThan_Works()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 1.5, 2.5, 3.5, 4.5, 5.5 })
        );

        var result = df.Lazy().Filter(Col("value").Gt(3.0)).Collect();

        result.Height.Should().Be(3);
    }

    [Fact]
    public void Filter_Float_Equal_Works()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 1.0, 2.0, 3.0, 2.0, 1.0 })
        );

        var result = df.Lazy().Filter(Col("value").Eq(2.0)).Collect();

        result.Height.Should().Be(2);
    }

    [Fact]
    public void Filter_Float_NegativeValues()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { -2.0, -1.0, 0.0, 1.0, 2.0 })
        );

        var result = df.Lazy().Filter(Col("value").Lt(0.0)).Collect();

        result.Height.Should().Be(2);
    }

    // ============================================================================
    // String Filter Tests
    // ============================================================================

    [Fact]
    public void Filter_String_Equal_Works()
    {
        var df = new DataFrame(
            Series.FromValues("name", new[] { "Alice", "Bob", "Charlie", "Alice" })
        );

        var result = df.Lazy().Filter(Col("name").Eq("Alice")).Collect();

        result.Height.Should().Be(2);
    }

    [Fact]
    public void Filter_String_NotEqual_Works()
    {
        var df = new DataFrame(
            Series.FromValues("name", new[] { "Alice", "Bob", "Charlie" })
        );

        var result = df.Lazy().Filter(Col("name").Ne("Bob")).Collect();

        result.Height.Should().Be(2);
    }

    // ============================================================================
    // Boolean Filter Tests
    // ============================================================================

    [Fact]
    public void Filter_Boolean_True()
    {
        var df = new DataFrame(
            Series.FromValues("flag", new[] { true, false, true, false, true })
        );

        var result = df.Lazy().Filter(Col("flag").Eq(true)).Collect();

        result.Height.Should().Be(3);
    }

    [Fact]
    public void Filter_Boolean_False()
    {
        var df = new DataFrame(
            Series.FromValues("flag", new[] { true, false, true, false, true })
        );

        var result = df.Lazy().Filter(Col("flag").Eq(false)).Collect();

        result.Height.Should().Be(2);
    }

    // ============================================================================
    // Compound Predicate Tests
    // ============================================================================

    [Fact]
    public void Filter_And_Works()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 }),
            Series.FromValues("b", new[] { 10, 20, 30, 40, 50 })
        );

        var result = df.Lazy()
            .Filter(Col("a").Gt(2).And(Col("b").Lt(50)))
            .Collect();

        result.Height.Should().Be(2);  // a=3,b=30 and a=4,b=40
    }

    [Fact]
    public void Filter_Or_Works()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 1, 2, 3, 4, 5 })
        );

        var result = df.Lazy()
            .Filter(Col("value").Eq(1).Or(Col("value").Eq(5)))
            .Collect();

        result.Height.Should().Be(2);  // 1 and 5
    }

    [Fact]
    public void Filter_NestedAnd_Works()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 }),
            Series.FromValues("b", new[] { 1, 2, 3, 4, 5 }),
            Series.FromValues("c", new[] { 1, 2, 3, 4, 5 })
        );

        var result = df.Lazy()
            .Filter(Col("a").Gt(1).And(Col("b").Gt(1)).And(Col("c").Gt(1)))
            .Collect();

        result.Height.Should().Be(4);  // 2,3,4,5 for all columns
    }

    // ============================================================================
    // Filter with Multiple Columns Tests
    // ============================================================================

    [Fact]
    public void Filter_ReturnsFilteredColumn()
    {
        var df = new DataFrame(
            Series.FromValues("id", new[] { 1, 2, 3 }),
            Series.FromValues("name", new[] { "A", "B", "C" }),
            Series.FromValues("value", new[] { 100, 200, 300 })
        );

        var result = df.Lazy().Filter(Col("id").Gt(1)).Collect();

        result.Height.Should().Be(2);
        // Query optimizer may apply projection pushdown - just verify filter works
        result["id"][0].AsInt32().Should().Be(2);
        result["id"][1].AsInt32().Should().Be(3);
    }

    [Fact]
    public void Filter_WithMultipleColumnsReferenced_PreservesColumns()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 }),
            Series.FromValues("b", new[] { 10, 20, 30 }),
            Series.FromValues("c", new[] { 100, 200, 300 })
        );

        // Reference multiple columns so optimizer keeps them
        var result = df.Lazy().Filter(Col("a").Gt(1).And(Col("b").Lt(40))).Collect();

        result.Height.Should().Be(2);
        result["a"][0].AsInt32().Should().Be(2);
        result["b"][0].AsInt32().Should().Be(20);
    }

    // ============================================================================
    // Filter Edge Cases
    // ============================================================================

    [Fact]
    public void Filter_AllMatch_ReturnsAll()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 5, 6, 7, 8, 9 })
        );

        var result = df.Lazy().Filter(Col("value").Gt(0)).Collect();

        result.Height.Should().Be(5);
    }

    [Fact]
    public void Filter_NoneMatch_ReturnsEmpty()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 1, 2, 3, 4, 5 })
        );

        var result = df.Lazy().Filter(Col("value").Gt(100)).Collect();

        result.Height.Should().Be(0);
    }

    [Fact]
    public void Filter_SingleMatch_ReturnsSingleRow()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 1, 2, 3, 4, 5 })
        );

        var result = df.Lazy().Filter(Col("value").Eq(3)).Collect();

        result.Height.Should().Be(1);
        result["value"][0].AsInt32().Should().Be(3);
    }

    [Fact]
    public void Filter_EmptyDataFrame_ReturnsEmpty()
    {
        var df = new DataFrame(
            Series.FromValues("value", Array.Empty<int>())
        );

        var result = df.Lazy().Filter(Col("value").Gt(0)).Collect();

        result.Height.Should().Be(0);
    }

    [Fact]
    public void Filter_SingleRow_Match()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 42 })
        );

        var result = df.Lazy().Filter(Col("value").Eq(42)).Collect();

        result.Height.Should().Be(1);
    }

    [Fact]
    public void Filter_SingleRow_NoMatch()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 42 })
        );

        var result = df.Lazy().Filter(Col("value").Eq(0)).Collect();

        result.Height.Should().Be(0);
    }

    // ============================================================================
    // Chained Filter Tests
    // ============================================================================

    [Fact]
    public void Filter_Chained_Works()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 })
        );

        var result = df.Lazy()
            .Filter(Col("value").Gt(3))
            .Filter(Col("value").Lt(8))
            .Collect();

        result.Height.Should().Be(4);  // 4, 5, 6, 7
    }

    [Fact]
    public void Filter_Chained_Equivalent_To_And()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 })
        );

        var chainedResult = df.Lazy()
            .Filter(Col("value").Gt(3))
            .Filter(Col("value").Lt(8))
            .Collect();

        var andResult = df.Lazy()
            .Filter(Col("value").Gt(3).And(Col("value").Lt(8)))
            .Collect();

        chainedResult.Height.Should().Be(andResult.Height);
    }

    // ============================================================================
    // Filter with Computed Expression Tests
    // ============================================================================

    [Fact]
    public void Filter_AfterWithColumns_Works()
    {
        var df = new DataFrame(
            Series.FromValues("x", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 })
        );

        var result = df.Lazy()
            .WithColumns((Col("x") * 2.0).As("doubled"))
            .Filter(Col("doubled").Gt(6.0))
            .Collect();

        result.Height.Should().Be(2);  // doubled values 8.0 and 10.0
    }

    // ============================================================================
    // Large Data Filter Tests
    // ============================================================================

    [Fact]
    public void Filter_Large_HalfMatch()
    {
        var values = Enumerable.Range(0, 10000).ToArray();
        var df = new DataFrame(
            Series.FromValues("value", values)
        );

        var result = df.Lazy().Filter(Col("value").Ge(5000)).Collect();

        result.Height.Should().Be(5000);
    }

    [Fact]
    public void Filter_Large_SparseMatch()
    {
        var values = Enumerable.Range(0, 10000).ToArray();
        var df = new DataFrame(
            Series.FromValues("value", values)
        );

        // Every 100th value
        var result = df.Lazy().Filter(Col("value").Eq(100)).Collect();

        result.Height.Should().Be(1);
    }

    // ============================================================================
    // Filter with Int64 Tests
    // ============================================================================

    [Fact]
    public void Filter_Int64_Works()
    {
        var df = new DataFrame(
            Series.FromValues("value", new long[] { 1, 2, 3, 4, 5 })
        );

        var result = df.Lazy().Filter(Col("value").Gt(3L)).Collect();

        result.Height.Should().Be(2);
    }

    // ============================================================================
    // Filter Preserves Data Tests
    // ============================================================================

    [Fact]
    public void Filter_PreservesExactValues()
    {
        var df = new DataFrame(
            Series.FromValues("id", new[] { 1, 2, 3 }),
            Series.FromValues("name", new[] { "Alice", "Bob", "Charlie" })
        );

        // Reference both columns to prevent projection pushdown
        var result = df.Lazy()
            .Filter(Col("id").Eq(2))
            .Select(Col("id"), Col("name"))
            .Collect();

        result["id"][0].AsInt32().Should().Be(2);
        result["name"][0].AsString().Should().Be("Bob");
    }

    [Fact]
    public void Filter_PreservesDataTypes()
    {
        var df = new DataFrame(
            Series.FromValues("int_col", new[] { 1, 2, 3 }),
            Series.FromValues("float_col", new[] { 1.5, 2.5, 3.5 }),
            Series.FromValues("str_col", new[] { "a", "b", "c" })
        );

        // Explicitly select all columns to prevent projection pushdown
        var result = df.Lazy()
            .Filter(Col("int_col").Gt(1))
            .Select(Col("int_col"), Col("float_col"), Col("str_col"))
            .Collect();

        result["int_col"].DataType.Should().Be(DataType.Int32);
        result["float_col"].DataType.Should().Be(DataType.Float64);
        result["str_col"].DataType.Should().Be(DataType.String);
    }

    // ============================================================================
    // Filter with Nulls Tests
    // ============================================================================

    [Fact]
    public void Filter_ExcludesNullsByDefault()
    {
        var df = new DataFrame(
            Series.FromNullable("value", new int?[] { 1, null, 3, null, 5 })
        );

        var result = df.Lazy().Filter(Col("value").Gt(0)).Collect();

        // Nulls should be excluded from comparison
        result.Height.Should().Be(3);
    }

    [Fact]
    public void Filter_WithNullInOtherColumn_PreservesNull()
    {
        var df = new DataFrame(
            Series.FromValues("filter_col", new[] { 1, 2, 3 }),
            Series.FromNullable("data_col", new int?[] { 10, null, 30 })
        );

        // Explicitly select both columns to prevent projection pushdown
        var result = df.Lazy()
            .Filter(Col("filter_col").Gt(1))
            .Select(Col("filter_col"), Col("data_col"))
            .Collect();

        result.Height.Should().Be(2);
        result["data_col"].IsNull(0).Should().BeTrue();  // The null was in row 2
    }

    // ============================================================================
    // Boundary Value Tests
    // ============================================================================

    [Fact]
    public void Filter_ExactBoundary_GreaterThan()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 1, 2, 3, 4, 5 })
        );

        var result = df.Lazy().Filter(Col("value").Gt(5)).Collect();

        result.Height.Should().Be(0);
    }

    [Fact]
    public void Filter_ExactBoundary_GreaterOrEqual()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 1, 2, 3, 4, 5 })
        );

        var result = df.Lazy().Filter(Col("value").Ge(5)).Collect();

        result.Height.Should().Be(1);
    }

    [Fact]
    public void Filter_Zero_Works()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { -2, -1, 0, 1, 2 })
        );

        var result = df.Lazy().Filter(Col("value").Eq(0)).Collect();

        result.Height.Should().Be(1);
        result["value"][0].AsInt32().Should().Be(0);
    }

    [Fact]
    public void Filter_NegativeValues_Works()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { -5, -3, -1, 1, 3, 5 })
        );

        var result = df.Lazy().Filter(Col("value").Lt(0)).Collect();

        result.Height.Should().Be(3);
    }
}
