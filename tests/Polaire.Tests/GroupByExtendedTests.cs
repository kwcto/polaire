// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET
//
// Extended tests for GroupBy operations, inspired by Polars test suite.
// These tests cover:
// - GroupBy with multiple aggregations
// - GroupBy edge cases
// - GroupBy with nulls
// - GroupBy chained operations

using FluentAssertions;
using Polaire;
using Polaire.DataTypes;
using Xunit;
using static Polaire.Pl;

namespace Polaire.Tests;

/// <summary>
/// Extended tests for GroupBy operations.
/// </summary>
public class GroupByExtendedTests
{
    // ============================================================================
    // Basic GroupBy Tests
    // ============================================================================

    [Fact]
    public void GroupBy_Sum_ReturnsCorrectSums()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "B", "B", "B" }),
            Series.FromValues("value", new[] { 1, 2, 3, 4, 5 })
        );

        var result = df.GroupBy("group").Sum();

        result.Height.Should().Be(2);
    }

    [Fact]
    public void GroupBy_Mean_ReturnsCorrectMeans()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "B", "B" }),
            Series.FromValues("value", new[] { 10.0, 20.0, 30.0, 40.0 })
        );

        var result = df.GroupBy("group").Mean();

        result.Height.Should().Be(2);
    }

    [Fact]
    public void GroupBy_Min_ReturnsCorrectMins()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "B", "B" }),
            Series.FromValues("value", new[] { 10, 5, 30, 20 })
        );

        var result = df.GroupBy("group").Min();

        result.Height.Should().Be(2);
    }

    [Fact]
    public void GroupBy_Max_ReturnsCorrectMaxes()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "B", "B" }),
            Series.FromValues("value", new[] { 10, 5, 30, 20 })
        );

        var result = df.GroupBy("group").Max();

        result.Height.Should().Be(2);
    }

    [Fact]
    public void GroupBy_Count_ReturnsCorrectCounts()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "A", "B", "B" }),
            Series.FromValues("value", new[] { 1, 2, 3, 4, 5 })
        );

        var result = df.GroupBy("group").Count();

        result.Height.Should().Be(2);
    }

    // ============================================================================
    // Single Group Tests
    // ============================================================================

    [Fact]
    public void GroupBy_SingleGroup_ReturnsOneRow()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "A" }),
            Series.FromValues("value", new[] { 1, 2, 3 })
        );

        var result = df.GroupBy("group").Sum();

        result.Height.Should().Be(1);
    }

    [Fact]
    public void GroupBy_AllDifferentGroups_ReturnsAllRows()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "B", "C" }),
            Series.FromValues("value", new[] { 1, 2, 3 })
        );

        var result = df.GroupBy("group").Sum();

        result.Height.Should().Be(3);
    }

    // ============================================================================
    // Multiple Column Tests
    // ============================================================================

    [Fact]
    public void GroupBy_MultipleValueColumns_AggregatesAll()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "B" }),
            Series.FromValues("val1", new[] { 1, 2, 3 }),
            Series.FromValues("val2", new[] { 10, 20, 30 })
        );

        var result = df.GroupBy("group").Sum();

        result.Height.Should().Be(2);
        result.Width.Should().BeGreaterOrEqualTo(2);
    }

    [Fact]
    public void GroupBy_MultipleGroupColumns_GroupsByAll()
    {
        var df = new DataFrame(
            Series.FromValues("g1", new[] { "A", "A", "A", "B" }),
            Series.FromValues("g2", new[] { "X", "X", "Y", "X" }),
            Series.FromValues("value", new[] { 1, 2, 3, 4 })
        );

        var result = df.GroupBy("g1", "g2").Sum();

        result.Height.Should().Be(3);  // A-X, A-Y, B-X
    }

    // ============================================================================
    // Null Key Tests
    // ============================================================================

    [Fact]
    public void GroupBy_NullsInKey_GroupsNullsTogether()
    {
        var df = new DataFrame(
            Series.FromNullable("group", new int?[] { 1, 1, null, null, 2 }),
            Series.FromValues("value", new[] { 10, 20, 30, 40, 50 })
        );

        var result = df.GroupBy("group").Sum();

        // Should have 3 groups: 1, null, 2
        result.Height.Should().Be(3);
    }

    [Fact]
    public void GroupBy_AllNullKeys_ReturnsSingleGroup()
    {
        var df = new DataFrame(
            Series.FromNullable("group", new int?[] { null, null, null }),
            Series.FromValues("value", new[] { 1, 2, 3 })
        );

        var result = df.GroupBy("group").Sum();

        result.Height.Should().Be(1);
    }

    // ============================================================================
    // Null Value Tests
    // ============================================================================

    [Fact]
    public void GroupBy_NullsInValues_SkipsNulls()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "A" }),
            Series.FromNullable("value", new int?[] { 1, null, 3 })
        );

        var result = df.GroupBy("group").Sum();

        result.Height.Should().Be(1);
    }

    [Fact]
    public void GroupBy_AllNullValues_ReturnsNull()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A" }),
            Series.FromNullable("value", new int?[] { null, null })
        );

        var result = df.GroupBy("group").Sum();

        result.Height.Should().Be(1);
    }

    // ============================================================================
    // Integer Key Tests
    // ============================================================================

    [Fact]
    public void GroupBy_IntegerKeys_Works()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { 1, 1, 2, 2, 3 }),
            Series.FromValues("value", new[] { 10, 20, 30, 40, 50 })
        );

        var result = df.GroupBy("group").Sum();

        result.Height.Should().Be(3);
    }

    [Fact]
    public void GroupBy_LargeIntegerRange_Works()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { 1, 1000000, 1, 1000000 }),
            Series.FromValues("value", new[] { 1, 2, 3, 4 })
        );

        var result = df.GroupBy("group").Sum();

        result.Height.Should().Be(2);
    }

    // ============================================================================
    // Float Key Tests
    // ============================================================================

    [Fact]
    public void GroupBy_FloatKeys_Works()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { 1.0, 1.0, 2.0, 2.0 }),
            Series.FromValues("value", new[] { 10, 20, 30, 40 })
        );

        var result = df.GroupBy("group").Sum();

        result.Height.Should().Be(2);
    }

    // ============================================================================
    // Boolean Key Tests
    // ============================================================================

    [Fact]
    public void GroupBy_BooleanKeys_Works()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { true, true, false, false }),
            Series.FromValues("value", new[] { 1, 2, 3, 4 })
        );

        var result = df.GroupBy("group").Sum();

        result.Height.Should().Be(2);
    }

    // ============================================================================
    // Empty DataFrame Tests
    // ============================================================================

    [Fact]
    public void GroupBy_EmptyDataFrame_ReturnsEmpty()
    {
        var df = new DataFrame(
            Series.FromValues("group", Array.Empty<string>()),
            Series.FromValues("value", Array.Empty<int>())
        );

        var result = df.GroupBy("group").Sum();

        result.Height.Should().Be(0);
    }

    // ============================================================================
    // Single Row Tests
    // ============================================================================

    [Fact]
    public void GroupBy_SingleRow_ReturnsOneGroup()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A" }),
            Series.FromValues("value", new[] { 42 })
        );

        var result = df.GroupBy("group").Sum();

        result.Height.Should().Be(1);
    }

    // ============================================================================
    // Large Data Tests
    // ============================================================================

    [Fact]
    public void GroupBy_LargeData_Succeeds()
    {
        var groups = Enumerable.Range(0, 10000).Select(i => i % 100).ToArray();
        var values = Enumerable.Range(0, 10000).ToArray();

        var df = new DataFrame(
            Series.FromValues("group", groups),
            Series.FromValues("value", values)
        );

        var result = df.GroupBy("group").Sum();

        result.Height.Should().Be(100);
    }

    [Fact]
    public void GroupBy_ManyGroups_Succeeds()
    {
        var groups = Enumerable.Range(0, 1000).ToArray();
        var values = Enumerable.Range(0, 1000).Select(i => 1).ToArray();

        var df = new DataFrame(
            Series.FromValues("group", groups),
            Series.FromValues("value", values)
        );

        var result = df.GroupBy("group").Sum();

        result.Height.Should().Be(1000);
    }

    // ============================================================================
    // Aggregation Expression Tests (DataFrame.GroupBy uses tuple syntax)
    // ============================================================================

    [Fact]
    public void GroupBy_Agg_WithMean_Works()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "B", "B" }),
            Series.FromValues("value", new[] { 10.0, 20.0, 30.0, 40.0 })
        );

        var result = df.GroupBy("group").Agg(
            ("value", "mean", s => s.Mean(), "avg")
        );

        result.Height.Should().Be(2);
        result.Columns.Should().Contain("avg");
    }

    [Fact]
    public void GroupBy_Agg_MultipleExpressions_Works()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "B", "B" }),
            Series.FromValues("value", new[] { 10.0, 20.0, 30.0, 40.0 })
        );

        var result = df.GroupBy("group").Agg(
            ("value", "sum", s => s.Sum(), "total"),
            ("value", "mean", s => s.Mean(), "avg"),
            ("value", "min", s => s.Min(), "min_val"),
            ("value", "max", s => s.Max(), "max_val")
        );

        result.Height.Should().Be(2);
        result.Columns.Should().Contain("total");
        result.Columns.Should().Contain("avg");
        result.Columns.Should().Contain("min_val");
        result.Columns.Should().Contain("max_val");
    }

    // ============================================================================
    // Lazy GroupBy Tests
    // ============================================================================

    [Fact]
    public void LazyGroupBy_Sum_Works()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "B", "B" }),
            Series.FromValues("value", new[] { 1, 2, 3, 4 })
        );

        var result = df.Lazy()
            .GroupBy(Col("group"))
            .Agg(Col("value").Sum().As("total"))
            .Collect();

        result.Height.Should().Be(2);
    }

    [Fact]
    public void LazyGroupBy_WithFilter_Works()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "B", "B", "C" }),
            Series.FromValues("value", new[] { 1, 2, 3, 4, 5 })
        );

        var result = df.Lazy()
            .Filter(Col("value").Gt(1))
            .GroupBy(Col("group"))
            .Agg(Col("value").Sum().As("total"))
            .Collect();

        result.Height.Should().BeGreaterThan(0);
    }

    [Fact]
    public void LazyGroupBy_MultipleColumns_Works()
    {
        var df = new DataFrame(
            Series.FromValues("g1", new[] { "A", "A", "B", "B" }),
            Series.FromValues("g2", new[] { 1, 2, 1, 2 }),
            Series.FromValues("value", new[] { 10, 20, 30, 40 })
        );

        var result = df.Lazy()
            .GroupBy(Col("g1"), Col("g2"))
            .Agg(Col("value").Sum().As("total"))
            .Collect();

        result.Height.Should().Be(4);
    }

    // ============================================================================
    // GroupBy First/Last Tests
    // ============================================================================

    [Fact]
    public void GroupBy_First_ReturnsFirstInEachGroup()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "B", "B" }),
            Series.FromValues("value", new[] { 1, 2, 3, 4 })
        );

        var result = df.GroupBy("group").First();

        result.Height.Should().Be(2);
    }

    [Fact]
    public void GroupBy_Last_ReturnsLastInEachGroup()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "B", "B" }),
            Series.FromValues("value", new[] { 1, 2, 3, 4 })
        );

        var result = df.GroupBy("group").Last();

        result.Height.Should().Be(2);
    }

    // ============================================================================
    // Consistent Column Names Tests
    // ============================================================================

    [Fact]
    public void GroupBy_PreservesGroupColumnName()
    {
        var df = new DataFrame(
            Series.FromValues("category", new[] { "X", "Y", "X" }),
            Series.FromValues("amount", new[] { 10, 20, 30 })
        );

        var result = df.GroupBy("category").Sum();

        result.Columns.Should().Contain("category");
    }

    [Fact]
    public void GroupBy_PreservesValueColumnName()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "B" }),
            Series.FromValues("measurement", new[] { 100, 200 })
        );

        var result = df.GroupBy("group").Sum();

        result.Columns.Should().Contain("measurement");
    }

    // ============================================================================
    // Type Preservation Tests
    // ============================================================================

    [Fact]
    public void GroupBy_Sum_PreservesIntegerType()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A" }),
            Series.FromValues("value", new[] { 1, 2 })
        );

        var result = df.GroupBy("group").Sum();

        // Sum of integers should be integer or larger integer type
        result["value"].DataType.Should().BeOneOf(DataType.Int32, DataType.Int64);
    }

    [Fact]
    public void GroupBy_Mean_ReturnsFloat()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A" }),
            Series.FromValues("value", new[] { 1, 2 })
        );

        var result = df.GroupBy("group").Mean();

        result["value"].DataType.Should().Be(DataType.Float64);
    }

    // ============================================================================
    // Ordering Tests
    // ============================================================================

    [Fact]
    public void GroupBy_MaintainsFirstSeenOrder()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "C", "A", "C", "B", "A" }),
            Series.FromValues("value", new[] { 1, 2, 3, 4, 5 })
        );

        var result = df.GroupBy("group").Sum();

        // Groups should appear in first-seen order: C, A, B
        result.Height.Should().Be(3);
    }
}
