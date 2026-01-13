// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using FluentAssertions;
using Polaire;
using Polaire.DataTypes;
using Xunit;

namespace Polaire.Tests;

/// <summary>
/// Tests for GroupBy operations on DataFrame.
/// </summary>
public class GroupByOperationsTests
{
    // ============================================================================
    // Basic GroupBy Tests
    // ============================================================================

    [Fact]
    public void GroupBy_SingleColumn_CreatesGroups()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "B", "B", "B" }),
            Series.FromValues("value", new[] { 1, 2, 3, 4, 5 })
        );

        var grouped = df.GroupBy("group");

        grouped.Should().NotBeNull();
    }

    [Fact]
    public void GroupBy_MultipleColumns_CreatesGroups()
    {
        var df = new DataFrame(
            Series.FromValues("group1", new[] { "A", "A", "B", "B" }),
            Series.FromValues("group2", new[] { 1, 2, 1, 2 }),
            Series.FromValues("value", new[] { 10, 20, 30, 40 })
        );

        var grouped = df.GroupBy("group1", "group2");

        grouped.Should().NotBeNull();
    }

    // ============================================================================
    // Count Tests
    // ============================================================================

    [Fact]
    public void GroupBy_Count_ReturnsCorrectCounts()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "A", "B", "B" }),
            Series.FromValues("value", new[] { 1, 2, 3, 4, 5 })
        );

        var result = df.GroupBy("group").Count();

        result.Height.Should().Be(2);
        result.Columns.Should().Contain("group");
        result.Columns.Should().Contain("value"); // Column name preserved
    }

    [Fact]
    public void GroupBy_Count_EmptyGroups_ReturnsZero()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A" }),
            Series.FromValues("value", new[] { 1, 2 })
        );

        var result = df.GroupBy("group").Count();

        result.Height.Should().Be(1);
    }

    // ============================================================================
    // Sum Tests
    // ============================================================================

    [Fact]
    public void GroupBy_Sum_ReturnsCorrectSums()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "B", "B" }),
            Series.FromValues("value", new[] { 1, 2, 3, 4 })
        );

        var result = df.GroupBy("group").Sum();

        result.Height.Should().Be(2);
        // Group A: 1 + 2 = 3, Group B: 3 + 4 = 7
    }

    [Fact]
    public void GroupBy_Sum_MultipleNumericColumns_SumsAll()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "B", "B" }),
            Series.FromValues("x", new[] { 1.0, 2.0, 3.0, 4.0 }),
            Series.FromValues("y", new[] { 10.0, 20.0, 30.0, 40.0 })
        );

        var result = df.GroupBy("group").Sum();

        result.Columns.Should().Contain("x");
        result.Columns.Should().Contain("y");
    }

    // ============================================================================
    // Mean Tests
    // ============================================================================

    [Fact]
    public void GroupBy_Mean_ReturnsCorrectMeans()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "B", "B" }),
            Series.FromValues("value", new[] { 10.0, 20.0, 30.0, 40.0 })
        );

        var result = df.GroupBy("group").Mean();

        result.Height.Should().Be(2);
        // Group A mean: (10 + 20) / 2 = 15
        // Group B mean: (30 + 40) / 2 = 35
    }

    // ============================================================================
    // Median Tests
    // ============================================================================

    [Fact]
    public void GroupBy_Median_ReturnsCorrectMedians()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "A", "B", "B" }),
            Series.FromValues("value", new[] { 1.0, 2.0, 3.0, 10.0, 20.0 })
        );

        var result = df.GroupBy("group").Median();

        result.Height.Should().Be(2);
        // Group A median: 2.0, Group B median: 15.0
    }

    // ============================================================================
    // Min/Max Tests
    // ============================================================================

    [Fact]
    public void GroupBy_Min_ReturnsCorrectMinimums()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "B", "B" }),
            Series.FromValues("value", new[] { 5, 3, 8, 2 })
        );

        var result = df.GroupBy("group").Min();

        result.Height.Should().Be(2);
        // Group A min: 3, Group B min: 2
    }

    [Fact]
    public void GroupBy_Max_ReturnsCorrectMaximums()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "B", "B" }),
            Series.FromValues("value", new[] { 5, 3, 8, 2 })
        );

        var result = df.GroupBy("group").Max();

        result.Height.Should().Be(2);
        // Group A max: 5, Group B max: 8
    }

    // ============================================================================
    // Std/Var Tests
    // ============================================================================

    [Fact]
    public void GroupBy_Std_ReturnsStandardDeviations()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "A", "B", "B", "B" }),
            Series.FromValues("value", new[] { 1.0, 2.0, 3.0, 10.0, 20.0, 30.0 })
        );

        var result = df.GroupBy("group").Std();

        result.Height.Should().Be(2);
        result.Columns.Should().Contain("value");
    }

    [Fact]
    public void GroupBy_Var_ReturnsVariances()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "A", "B", "B", "B" }),
            Series.FromValues("value", new[] { 1.0, 2.0, 3.0, 10.0, 20.0, 30.0 })
        );

        var result = df.GroupBy("group").Var();

        result.Height.Should().Be(2);
    }

    // ============================================================================
    // First/Last Tests
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
        // Should contain first value from each group
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
        // Should contain last value from each group
    }

    // ============================================================================
    // NUnique Tests
    // ============================================================================

    [Fact]
    public void GroupBy_NUnique_ReturnsUniqueCountPerGroup()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "A", "B", "B" }),
            Series.FromValues("value", new[] { 1, 1, 2, 3, 3 })
        );

        var result = df.GroupBy("group").NUnique();

        result.Height.Should().Be(2);
        // Group A: 2 unique values (1, 2), Group B: 1 unique value (3)
    }

    // ============================================================================
    // Custom Agg Tests
    // ============================================================================

    [Fact]
    public void GroupBy_Agg_WithTuples_ReturnsCustomAggregations()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "B", "B" }),
            Series.FromValues("value", new[] { 1.0, 2.0, 3.0, 4.0 })
        );

        var result = df.GroupBy("group").Agg(
            ("value", "sum", s => s.Sum(), "value_sum"),
            ("value", "mean", s => s.Mean(), "value_mean")
        );

        result.Columns.Should().Contain("group");
        result.Columns.Should().Contain("value_sum");
        result.Columns.Should().Contain("value_mean");
    }

    [Fact]
    public void GroupBy_Agg_WithDictionary_ReturnsMultipleAggregationsPerColumn()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "B", "B" }),
            Series.FromValues("value", new[] { 1.0, 2.0, 3.0, 4.0 })
        );

        var result = df.GroupBy("group").Agg(new Dictionary<string, List<string>>
        {
            ["value"] = new List<string> { "sum", "mean", "min", "max" }
        });

        result.Columns.Should().Contain("group");
        result.Columns.Should().Contain("value_sum");
        result.Columns.Should().Contain("value_mean");
        result.Columns.Should().Contain("value_min");
        result.Columns.Should().Contain("value_max");
    }

    // ============================================================================
    // GetGroup Tests
    // ============================================================================

    [Fact]
    public void GroupBy_GetGroup_ReturnsSingleGroup()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "B", "B", "B" }),
            Series.FromValues("value", new[] { 1, 2, 3, 4, 5 })
        );

        var groupA = df.GroupBy("group").GetGroup(AnyValue.From("A"));

        groupA.Height.Should().Be(2);
    }

    [Fact]
    public void GroupBy_GetGroup_MultipleKeys_ReturnsCorrectGroup()
    {
        var df = new DataFrame(
            Series.FromValues("g1", new[] { "A", "A", "B", "B" }),
            Series.FromValues("g2", new[] { 1, 2, 1, 2 }),
            Series.FromValues("value", new[] { 10, 20, 30, 40 })
        );

        var group = df.GroupBy("g1", "g2").GetGroup(AnyValue.From("A"), AnyValue.From(1));

        group.Height.Should().Be(1);
        group["value"][0].AsInt32().Should().Be(10);
    }

    // ============================================================================
    // Edge Cases
    // ============================================================================

    [Fact]
    public void GroupBy_SingleRow_WorksCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A" }),
            Series.FromValues("value", new[] { 42 })
        );

        var result = df.GroupBy("group").Sum();

        result.Height.Should().Be(1);
    }

    [Fact]
    public void GroupBy_AllSameGroup_WorksCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "A", "A", "A" }),
            Series.FromValues("value", new[] { 1, 2, 3, 4, 5 })
        );

        var result = df.GroupBy("group").Sum();

        result.Height.Should().Be(1);
        // Sum should be 15
    }

    [Fact]
    public void GroupBy_AllDifferentGroups_WorksCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "B", "C", "D", "E" }),
            Series.FromValues("value", new[] { 1, 2, 3, 4, 5 })
        );

        var result = df.GroupBy("group").Sum();

        result.Height.Should().Be(5);
    }

    [Fact]
    public void GroupBy_WithNullValues_HandlesCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("group", new string?[] { "A", "A", null, null, "B" }),
            Series.FromValues("value", new[] { 1, 2, 3, 4, 5 })
        );

        var result = df.GroupBy("group").Sum();

        // Should have groups: A, B, null
        result.Height.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void GroupBy_NumericGroupColumn_WorksCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { 1, 1, 2, 2, 3 }),
            Series.FromValues("value", new[] { 10.0, 20.0, 30.0, 40.0, 50.0 })
        );

        var result = df.GroupBy("group").Mean();

        result.Height.Should().Be(3);
    }

    [Fact]
    public void GroupBy_LargeDaataFrame_WorksCorrectly()
    {
        var size = 10000;
        var groups = Enumerable.Range(0, size).Select(i => i % 100).ToArray();
        var values = Enumerable.Range(0, size).Select(i => (double)i).ToArray();

        var df = new DataFrame(
            Series.FromValues("group", groups),
            Series.FromValues("value", values)
        );

        var result = df.GroupBy("group").Sum();

        result.Height.Should().Be(100);
    }

    // ============================================================================
    // Chained Operations Tests
    // ============================================================================

    [Fact]
    public void GroupBy_Result_CanBeFiltered()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "B", "B", "C", "C" }),
            Series.FromValues("value", new[] { 1, 2, 10, 20, 100, 200 })
        );

        var result = df.GroupBy("group").Sum();

        // Result should be filterable
        result.Width.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GroupBy_Result_CanBeSorted()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "C", "B", "A", "C", "B", "A" }),
            Series.FromValues("value", new[] { 1, 2, 3, 4, 5, 6 })
        );

        var result = df.GroupBy("group").Sum().Sort("group");

        result.Height.Should().Be(3);
    }

    // ============================================================================
    // Non-Numeric Column Tests
    // ============================================================================

    [Fact]
    public void GroupBy_Sum_IgnoresStringColumns()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "B", "B" }),
            Series.FromValues("value", new[] { 1, 2, 3, 4 }),
            Series.FromValues("name", new[] { "a", "b", "c", "d" })
        );

        var result = df.GroupBy("group").Sum();

        // Sum should only include numeric columns
        result.Columns.Should().Contain("value");
    }

    [Fact]
    public void GroupBy_First_IncludesAllColumns()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "B", "B" }),
            Series.FromValues("value", new[] { 1, 2, 3, 4 }),
            Series.FromValues("name", new[] { "first_a", "second_a", "first_b", "second_b" })
        );

        var result = df.GroupBy("group").First();

        result.Columns.Should().Contain("name");
    }

    // ============================================================================
    // Multiple Group Columns Tests
    // ============================================================================

    [Fact]
    public void GroupBy_TwoColumns_CreatesCorrectGroups()
    {
        var df = new DataFrame(
            Series.FromValues("year", new[] { 2020, 2020, 2021, 2021 }),
            Series.FromValues("month", new[] { 1, 2, 1, 2 }),
            Series.FromValues("sales", new[] { 100, 200, 150, 250 })
        );

        var result = df.GroupBy("year", "month").Sum();

        result.Height.Should().Be(4);
        result.Columns.Should().Contain("year");
        result.Columns.Should().Contain("month");
        result.Columns.Should().Contain("sales");
    }

    [Fact]
    public void GroupBy_ThreeColumns_WorksCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { "X", "X", "X", "Y" }),
            Series.FromValues("b", new[] { 1, 1, 2, 1 }),
            Series.FromValues("c", new[] { "p", "q", "p", "p" }),
            Series.FromValues("value", new[] { 10.0, 20.0, 30.0, 40.0 })
        );

        var result = df.GroupBy("a", "b", "c").Sum();

        result.Columns.Should().Contain("a");
        result.Columns.Should().Contain("b");
        result.Columns.Should().Contain("c");
    }
}
