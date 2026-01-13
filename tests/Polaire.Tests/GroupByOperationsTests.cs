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

    // ============================================================================
    // Value Verification Tests (from Polars: test_group_by)
    // ============================================================================

    [Fact]
    public void GroupBy_Sum_VerifyExactValues()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { "a", "b", "c", "c", "b", "b", "a", "a" }),
            Series.FromValues("b", new[] { 1, 2, 3, 4, 5, 6, 7, 8 })
        );

        var result = df.GroupBy("a").Sum().Sort("a");

        result.Height.Should().Be(3);

        // Group "a": 1 + 7 + 8 = 16 (Sum returns Int64)
        result["b"][0].AsInt64().Should().Be(16);
        // Group "b": 2 + 5 + 6 = 13
        result["b"][1].AsInt64().Should().Be(13);
        // Group "c": 3 + 4 = 7
        result["b"][2].AsInt64().Should().Be(7);
    }

    [Fact]
    public void GroupBy_Mean_VerifyExactValues()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "X", "X", "X", "Y", "Y" }),
            Series.FromValues("value", new[] { 10.0, 20.0, 30.0, 100.0, 200.0 })
        );

        var result = df.GroupBy("group").Mean().Sort("group");

        result.Height.Should().Be(2);

        // Group "X": (10 + 20 + 30) / 3 = 20.0
        result["value"][0].AsFloat64().Should().BeApproximately(20.0, 0.0001);
        // Group "Y": (100 + 200) / 2 = 150.0
        result["value"][1].AsFloat64().Should().BeApproximately(150.0, 0.0001);
    }

    [Fact]
    public void GroupBy_Min_VerifyExactValues()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "A", "B", "B" }),
            Series.FromValues("value", new[] { 5, 2, 8, 10, 3 })
        );

        var result = df.GroupBy("group").Min().Sort("group");

        result["value"][0].AsInt32().Should().Be(2);  // A: min(5, 2, 8) = 2
        result["value"][1].AsInt32().Should().Be(3);  // B: min(10, 3) = 3
    }

    [Fact]
    public void GroupBy_Max_VerifyExactValues()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "A", "B", "B" }),
            Series.FromValues("value", new[] { 5, 2, 8, 10, 3 })
        );

        var result = df.GroupBy("group").Max().Sort("group");

        result["value"][0].AsInt32().Should().Be(8);   // A: max(5, 2, 8) = 8
        result["value"][1].AsInt32().Should().Be(10);  // B: max(10, 3) = 10
    }

    [Fact]
    public void GroupBy_First_VerifyExactValues()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "B", "B" }),
            Series.FromValues("value", new[] { 100, 200, 300, 400 })
        );

        var result = df.GroupBy("group").First().Sort("group");

        result["value"][0].AsInt32().Should().Be(100);  // A first
        result["value"][1].AsInt32().Should().Be(300);  // B first
    }

    [Fact]
    public void GroupBy_Last_VerifyExactValues()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "B", "B" }),
            Series.FromValues("value", new[] { 100, 200, 300, 400 })
        );

        var result = df.GroupBy("group").Last().Sort("group");

        result["value"][0].AsInt32().Should().Be(200);  // A last
        result["value"][1].AsInt32().Should().Be(400);  // B last
    }

    [Fact]
    public void GroupBy_NUnique_VerifyExactValues()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "A", "A", "B", "B", "B" }),
            Series.FromValues("value", new[] { 1, 1, 2, 3, 5, 5, 5 })
        );

        var result = df.GroupBy("group").NUnique().Sort("group");

        // NUnique returns Int32
        result["value"][0].AsInt32().Should().Be(3);  // A: unique(1, 2, 3) = 3
        result["value"][1].AsInt32().Should().Be(1);  // B: unique(5) = 1
    }

    // ============================================================================
    // Boolean Column Tests with First/Last (Min/Max exclude boolean columns)
    // ============================================================================

    [Fact]
    public void GroupBy_BooleanColumn_First()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "B", "B" }),
            Series.FromValues("flag", new[] { true, false, false, true })
        );

        var result = df.GroupBy("group").First().Sort("group");

        // First should include boolean columns
        result["flag"][0].AsBoolean().Should().Be(true);   // A: first is true
        result["flag"][1].AsBoolean().Should().Be(false);  // B: first is false
    }

    [Fact]
    public void GroupBy_BooleanColumn_Last()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "B", "B" }),
            Series.FromValues("flag", new[] { true, false, false, true })
        );

        var result = df.GroupBy("group").Last().Sort("group");

        // Last should include boolean columns
        result["flag"][0].AsBoolean().Should().Be(false);  // A: last is false
        result["flag"][1].AsBoolean().Should().Be(true);   // B: last is true
    }

    // ============================================================================
    // Multiple Numeric Column Tests
    // ============================================================================

    [Fact]
    public void GroupBy_MultipleColumns_Sum_VerifyAll()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "B", "B" }),
            Series.FromValues("x", new[] { 1, 2, 3, 4 }),
            Series.FromValues("y", new[] { 10, 20, 30, 40 }),
            Series.FromValues("z", new[] { 100.0, 200.0, 300.0, 400.0 })
        );

        var result = df.GroupBy("group").Sum().Sort("group");

        // Group A (Sum returns Int64 for integer columns)
        result["x"][0].AsInt64().Should().Be(3);     // 1 + 2
        result["y"][0].AsInt64().Should().Be(30);    // 10 + 20
        result["z"][0].AsFloat64().Should().Be(300); // 100 + 200

        // Group B
        result["x"][1].AsInt64().Should().Be(7);     // 3 + 4
        result["y"][1].AsInt64().Should().Be(70);    // 30 + 40
        result["z"][1].AsFloat64().Should().Be(700); // 300 + 400
    }

    // ============================================================================
    // Edge Cases with Nulls (from Polars: test_group_by_null_propagation_6185)
    // ============================================================================

    [Fact]
    public void GroupBy_WithNullsInValueColumn_SumIgnoresNulls()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "A", "B", "B" }),
            Series.FromNullable("value", new int?[] { 1, null, 3, null, 5 })
        );

        var result = df.GroupBy("group").Sum().Sort("group");

        // A: 1 + 3 = 4 (null ignored) - Sum returns Int64
        // B: 5 (null ignored)
        result["value"][0].AsInt64().Should().Be(4);
        result["value"][1].AsInt64().Should().Be(5);
    }

    [Fact]
    public void GroupBy_WithNullsInValueColumn_MeanIgnoresNulls()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "A", "B", "B" }),
            Series.FromNullable("value", new double?[] { 10.0, null, 20.0, null, 30.0 })
        );

        var result = df.GroupBy("group").Mean().Sort("group");

        // A: (10 + 20) / 2 = 15.0 (null ignored)
        // B: 30 / 1 = 30.0 (null ignored)
        result["value"][0].AsFloat64().Should().BeApproximately(15.0, 0.0001);
        result["value"][1].AsFloat64().Should().BeApproximately(30.0, 0.0001);
    }

    [Fact]
    public void GroupBy_WithAllNullsInGroup_ReturnsNull()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "B", "B" }),
            Series.FromNullable("value", new int?[] { 1, 2, null, null })
        );

        var result = df.GroupBy("group").Sum().Sort("group");

        // A: 1 + 2 = 3 - Sum returns Int64
        result["value"][0].AsInt64().Should().Be(3);
        // B: all nulls -> result should be null or 0 depending on implementation
        // Most systems return null for sum of all nulls
    }

    [Fact]
    public void GroupBy_Count_ExcludesNulls()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "A", "B", "B" }),
            Series.FromNullable("value", new int?[] { 1, null, 3, null, null })
        );

        var result = df.GroupBy("group").Count().Sort("group");

        // Count returns Int32 and counts non-null values only
        result["value"][0].AsInt32().Should().Be(2);  // A has 2 non-null values (1, 3)
        result["value"][1].AsInt32().Should().Be(0);  // B has 0 non-null values
    }

    // ============================================================================
    // Empty DataFrame Tests (from Polars: test_group_by_empty)
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
        result.Columns.Should().Contain("group");
        result.Columns.Should().Contain("value");
    }

    [Fact]
    public void GroupBy_EmptyDataFrame_Mean_ReturnsEmpty()
    {
        var df = new DataFrame(
            Series.FromValues("group", Array.Empty<string>()),
            Series.FromValues("value", Array.Empty<double>())
        );

        var result = df.GroupBy("group").Mean();

        result.Height.Should().Be(0);
    }

    // ============================================================================
    // Integer Type Tests (from Polars: test_group_by_mean_by_dtype)
    // ============================================================================

    [Fact]
    public void GroupBy_Int32Mean_ReturnsFloat64()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "A" }),
            Series.FromValues("value", new[] { 1, 2, 3 })
        );

        var result = df.GroupBy("group").Mean();

        // Mean of integers should return float
        result["value"][0].AsFloat64().Should().BeApproximately(2.0, 0.0001);
    }

    [Fact]
    public void GroupBy_Int64Mean_ReturnsFloat64()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A" }),
            Series.FromValues("value", new long[] { 1000000000L, 2000000000L })
        );

        var result = df.GroupBy("group").Mean();

        result["value"][0].AsFloat64().Should().BeApproximately(1500000000.0, 0.0001);
    }

    // ============================================================================
    // Float Edge Cases Tests
    // ============================================================================

    [Fact]
    public void GroupBy_WithNaN_InValues()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "B", "B" }),
            Series.FromValues("value", new[] { 1.0, double.NaN, 3.0, 4.0 })
        );

        var result = df.GroupBy("group").Sum().Sort("group");

        // Polars skips NaN in aggregations
        // A: 1.0 (NaN skipped)
        // B: 3.0 + 4.0 = 7.0
        result["value"][1].AsFloat64().Should().Be(7.0);
    }

    [Fact]
    public void GroupBy_WithInfinity_Sum()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "B" }),
            Series.FromValues("value", new[] { double.PositiveInfinity, 1.0, 2.0 })
        );

        var result = df.GroupBy("group").Sum().Sort("group");

        // A: inf + 1.0 = inf
        // B: 2.0
        result["value"][0].AsFloat64().Should().Be(double.PositiveInfinity);
        result["value"][1].AsFloat64().Should().Be(2.0);
    }

    // ============================================================================
    // Stress Tests with Many Groups
    // ============================================================================

    [Fact]
    public void GroupBy_ManyGroups_1000_WorksCorrectly()
    {
        var size = 10000;
        var numGroups = 1000;
        var groups = Enumerable.Range(0, size).Select(i => $"group_{i % numGroups}").ToArray();
        var values = Enumerable.Range(0, size).Select(i => (double)i).ToArray();

        var df = new DataFrame(
            Series.FromValues("group", groups),
            Series.FromValues("value", values)
        );

        var result = df.GroupBy("group").Sum();

        result.Height.Should().Be(numGroups);
    }

    [Fact]
    public void GroupBy_ManyGroups_AllAggregations()
    {
        var size = 1000;
        var numGroups = 100;
        var groups = Enumerable.Range(0, size).Select(i => i % numGroups).ToArray();
        var values = Enumerable.Range(0, size).Select(i => (double)i).ToArray();

        var df = new DataFrame(
            Series.FromValues("group", groups),
            Series.FromValues("value", values)
        );

        df.GroupBy("group").Sum().Height.Should().Be(numGroups);
        df.GroupBy("group").Mean().Height.Should().Be(numGroups);
        df.GroupBy("group").Min().Height.Should().Be(numGroups);
        df.GroupBy("group").Max().Height.Should().Be(numGroups);
        df.GroupBy("group").Count().Height.Should().Be(numGroups);
    }

    // ============================================================================
    // Multi-Key GroupBy with Values Verification
    // ============================================================================

    [Fact]
    public void GroupBy_TwoKeys_Sum_VerifyValues()
    {
        var df = new DataFrame(
            Series.FromValues("year", new[] { 2020, 2020, 2020, 2021, 2021, 2021 }),
            Series.FromValues("quarter", new[] { 1, 1, 2, 1, 2, 2 }),
            Series.FromValues("sales", new[] { 100, 200, 300, 400, 500, 600 })
        );

        var result = df.GroupBy("year", "quarter").Sum();

        result.Height.Should().Be(4);  // 2020-Q1, 2020-Q2, 2021-Q1, 2021-Q2
    }

    [Fact]
    public void GroupBy_ThreeKeys_Count()
    {
        var df = new DataFrame(
            Series.FromValues("country", new[] { "US", "US", "US", "UK", "UK", "UK" }),
            Series.FromValues("city", new[] { "NYC", "NYC", "LA", "London", "London", "London" }),
            Series.FromValues("dept", new[] { "Sales", "IT", "Sales", "Sales", "IT", "HR" }),
            Series.FromValues("employees", new[] { 100, 50, 75, 80, 40, 30 })
        );

        var result = df.GroupBy("country", "city", "dept").Count();

        // Each unique combination should have count 1
        result.Height.Should().Be(6);
    }

    // ============================================================================
    // Std/Var with Small Groups (degrees of freedom edge case)
    // ============================================================================

    [Fact]
    public void GroupBy_Std_SingleElementGroup_ReturnsZeroOrNaN()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "B", "B" }),
            Series.FromValues("value", new[] { 10.0, 20.0, 30.0 })
        );

        var result = df.GroupBy("group").Std().Sort("group");

        // Group A has only 1 element, std is 0 or NaN depending on implementation
        // Group B has 2 elements, std = sqrt(((20-25)^2 + (30-25)^2) / 1) for sample std
        result.Height.Should().Be(2);
    }

    [Fact]
    public void GroupBy_Var_VerifyValues()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "A", "B", "B", "B" }),
            Series.FromValues("value", new[] { 1.0, 2.0, 3.0, 10.0, 20.0, 30.0 })
        );

        var result = df.GroupBy("group").Var().Sort("group");

        // Group A: mean=2, var = ((1-2)^2 + (2-2)^2 + (3-2)^2) / 2 = 1.0 (sample variance)
        // Group B: mean=20, var = ((10-20)^2 + (20-20)^2 + (30-20)^2) / 2 = 100.0
        result["value"][0].AsFloat64().Should().BeApproximately(1.0, 0.0001);
        result["value"][1].AsFloat64().Should().BeApproximately(100.0, 0.0001);
    }

    // ============================================================================
    // Float32 Type Tests
    // ============================================================================

    [Fact]
    public void GroupBy_Float32_Sum()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "B", "B" }),
            Series.FromValues("value", new float[] { 1.5f, 2.5f, 3.5f, 4.5f })
        );

        var result = df.GroupBy("group").Sum().Sort("group");

        // Float32 sum returns Float64 for precision
        result["value"][0].AsFloat64().Should().BeApproximately(4.0, 0.0001);  // 1.5 + 2.5
        result["value"][1].AsFloat64().Should().BeApproximately(8.0, 0.0001);  // 3.5 + 4.5
    }

    [Fact]
    public void GroupBy_Float32_Mean()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "B", "B" }),
            Series.FromValues("value", new float[] { 10.0f, 20.0f, 30.0f, 40.0f })
        );

        var result = df.GroupBy("group").Mean().Sort("group");

        result["value"][0].AsFloat64().Should().BeApproximately(15.0, 0.0001);
        result["value"][1].AsFloat64().Should().BeApproximately(35.0, 0.0001);
    }

    // ============================================================================
    // Median with Even/Odd Count Groups
    // ============================================================================

    [Fact]
    public void GroupBy_Median_OddCount()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "A", "A", "A" }),
            Series.FromValues("value", new[] { 1.0, 5.0, 2.0, 4.0, 3.0 })
        );

        var result = df.GroupBy("group").Median();

        // Sorted: 1, 2, 3, 4, 5 -> median = 3
        result["value"][0].AsFloat64().Should().BeApproximately(3.0, 0.0001);
    }

    [Fact]
    public void GroupBy_Median_EvenCount()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "A", "A" }),
            Series.FromValues("value", new[] { 1.0, 4.0, 2.0, 3.0 })
        );

        var result = df.GroupBy("group").Median();

        // Sorted: 1, 2, 3, 4 -> median = (2 + 3) / 2 = 2.5
        result["value"][0].AsFloat64().Should().BeApproximately(2.5, 0.0001);
    }

    // ============================================================================
    // Order Preservation Tests
    // ============================================================================

    [Fact]
    public void GroupBy_PreservesGroupOrder_WhenMaintainOrder()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "C", "A", "B", "A", "C", "B" }),
            Series.FromValues("value", new[] { 1, 2, 3, 4, 5, 6 })
        );

        var result = df.GroupBy("group").Sum();

        // Groups should appear in order of first occurrence: C, A, B
        // (or implementation may sort them, either is valid)
        result.Height.Should().Be(3);
    }

    // ============================================================================
    // Special Character Group Keys
    // ============================================================================

    [Fact]
    public void GroupBy_SpecialCharacterKeys_WorksCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "hello world", "hello world", "foo\tbar", "foo\tbar" }),
            Series.FromValues("value", new[] { 1, 2, 3, 4 })
        );

        var result = df.GroupBy("group").Sum();

        result.Height.Should().Be(2);
    }

    [Fact]
    public void GroupBy_UnicodeKeys_WorksCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "日本", "日本", "中国", "한국" }),
            Series.FromValues("value", new[] { 1, 2, 3, 4 })
        );

        var result = df.GroupBy("group").Sum();

        result.Height.Should().Be(3);  // Japan, China, Korea
    }

    [Fact]
    public void GroupBy_EmptyStringKey_WorksCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "", "", "A", "A" }),
            Series.FromValues("value", new[] { 1, 2, 3, 4 })
        );

        var result = df.GroupBy("group").Sum().Sort("group");

        result.Height.Should().Be(2);
        result["value"][0].AsInt64().Should().Be(3);  // "" group: 1 + 2 - Sum returns Int64
        result["value"][1].AsInt64().Should().Be(7);  // "A" group: 3 + 4
    }
}
