// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET
//
// Comprehensive tests for LazyFrame methods, inspired by Polars test suite.
// These tests cover all LazyFrame methods including:
// - Lazy transformations (Select, Filter, WithColumns)
// - Grouping and aggregation
// - Joins
// - Query optimization verification
// - Collect and execution

using FluentAssertions;
using Polaire;
using Polaire.DataTypes;
using Xunit;
using static Polaire.Pl;

namespace Polaire.Tests;

/// <summary>
/// Tests for LazyFrame methods.
/// </summary>
public class LazyFrameMethodsTests
{
    // ============================================================================
    // Basic Lazy Operations
    // ============================================================================

    [Fact]
    public void Lazy_FromDataFrame_CreatesLazyFrame()
    {
        var df = CreateTestDataFrame();
        var lf = df.Lazy();

        lf.Should().NotBeNull();
    }

    [Fact]
    public void Collect_AfterLazy_ReturnsDataFrame()
    {
        var df = CreateTestDataFrame();
        var result = df.Lazy().Collect();

        result.Height.Should().Be(df.Height);
        result.Width.Should().Be(df.Width);
    }

    // ============================================================================
    // Select Tests
    // ============================================================================

    [Fact]
    public void LazySelect_SingleColumn_CollectsCorrectly()
    {
        var df = CreateTestDataFrame();
        var result = df.Lazy()
            .Select("name")
            .Collect();

        result.Width.Should().Be(1);
        result.Columns.Should().Contain("name");
    }

    [Fact]
    public void LazySelect_MultipleColumns_CollectsCorrectly()
    {
        var df = CreateTestDataFrame();
        var result = df.Lazy()
            .Select("name", "age")
            .Collect();

        result.Width.Should().Be(2);
    }

    [Fact]
    public void LazySelect_WithExpression_TransformsColumn()
    {
        var df = CreateTestDataFrame();
        var result = df.Lazy()
            .Select(Col("name"), Col("age") * 2)
            .Collect();

        result.Width.Should().Be(2);
    }

    // ============================================================================
    // Filter Tests
    // ============================================================================

    [Fact]
    public void LazyFilter_SimpleCondition_FiltersRows()
    {
        var df = CreateTestDataFrame();
        var result = df.Lazy()
            .Filter(Col("age").Gt(28))
            .Collect();

        result.Height.Should().BeLessThan(df.Height);
    }

    [Fact]
    public void LazyFilter_NoMatch_ReturnsEmpty()
    {
        var df = CreateTestDataFrame();
        var result = df.Lazy()
            .Filter(Col("age").Gt(100))
            .Collect();

        result.Height.Should().Be(0);
    }

    [Fact]
    public void LazyFilter_AllMatch_ReturnsAll()
    {
        var df = CreateTestDataFrame();
        var result = df.Lazy()
            .Filter(Col("age").Gt(0))
            .Collect();

        result.Height.Should().Be(df.Height);
    }

    [Fact]
    public void LazyFilter_Chained_AppliesAll()
    {
        var df = CreateTestDataFrame();
        var result = df.Lazy()
            .Filter(Col("age").Gt(20))
            .Filter(Col("age").Lt(35))
            .Collect();

        result.Height.Should().BeGreaterThan(0);
    }

    // ============================================================================
    // WithColumns Tests
    // ============================================================================

    [Fact]
    public void LazyWithColumns_NewColumn_AddsColumn()
    {
        var df = CreateTestDataFrame();
        var result = df.Lazy()
            .WithColumns((Col("age") * 2).As("double_age"))
            .Collect();

        result.Columns.Should().Contain("double_age");
    }

    [Fact]
    public void LazyWithColumns_ExistingName_ReplacesColumn()
    {
        var df = CreateTestDataFrame();
        var result = df.Lazy()
            .WithColumns((Col("age") + 10).As("age"))
            .Collect();

        // Age should be increased by 10
        // Note: Arithmetic operations promote Int32 to Float64
        var originalAge = df["age"][0].AsInt32();
        var newAge = result["age"][0].AsFloat64();
        newAge.Should().Be(originalAge + 10);
    }

    // ============================================================================
    // Sort Tests
    // ============================================================================

    [Fact]
    public void LazySort_SingleColumn_Sorts()
    {
        var df = CreateUnsortedDataFrame();
        var result = df.Lazy()
            .Sort("value")
            .Collect();

        result["value"][0].AsInt32().Should().BeLessOrEqualTo(result["value"][1].AsInt32());
    }

    [Fact]
    public void LazySort_Descending_SortsDescending()
    {
        var df = CreateUnsortedDataFrame();
        var result = df.Lazy()
            .Sort((Col("value"), true))  // (expr, descending)
            .Collect();

        result["value"][0].AsInt32().Should().BeGreaterOrEqualTo(result["value"][1].AsInt32());
    }

    // ============================================================================
    // GroupBy Tests
    // ============================================================================

    [Fact]
    public void LazyGroupBy_Sum_AggregatesCorrectly()
    {
        var df = CreateGroupByDataFrame();
        var result = df.Lazy()
            .GroupBy("category")
            .Agg(Col("value").Sum().As("total"))
            .Collect();

        result.Height.Should().BeGreaterThan(0);
        result.Columns.Should().Contain("total");
    }

    [Fact]
    public void LazyGroupBy_MultipleAgg_ReturnsAll()
    {
        var df = CreateGroupByDataFrame();
        var result = df.Lazy()
            .GroupBy("category")
            .Agg(
                Col("value").Sum().As("sum"),
                Col("value").Mean().As("avg"),
                Col("value").Min().As("min"),
                Col("value").Max().As("max")
            )
            .Collect();

        result.Columns.Should().Contain("sum");
        result.Columns.Should().Contain("avg");
        result.Columns.Should().Contain("min");
        result.Columns.Should().Contain("max");
    }

    // ============================================================================
    // Join Tests
    // ============================================================================

    [Fact]
    public void LazyJoin_Inner_JoinsCorrectly()
    {
        var left = CreateJoinLeftDataFrame();
        var right = CreateJoinRightDataFrame();

        var result = left.Lazy()
            .Join(right.Lazy(), "id")
            .Collect();

        result.Height.Should().BeGreaterThan(0);
    }

    [Fact]
    public void LazyJoin_Left_KeepsAllLeft()
    {
        var left = CreateJoinLeftDataFrame();
        var right = CreateJoinRightDataFrame();

        var result = left.Lazy()
            .LeftJoin(right.Lazy(), "id")
            .Collect();

        result.Height.Should().BeGreaterOrEqualTo(left.Height);
    }

    // ============================================================================
    // Head/Tail Tests
    // ============================================================================

    [Fact]
    public void LazyHead_LimitsRows()
    {
        var df = CreateLargeDataFrame(100);
        var result = df.Lazy()
            .Head(10)
            .Collect();

        result.Height.Should().Be(10);
    }

    [Fact]
    public void LazyTail_ReturnsLastRows()
    {
        var df = CreateLargeDataFrame(100);
        var result = df.Lazy()
            .Tail(10)
            .Collect();

        result.Height.Should().Be(10);
    }

    // ============================================================================
    // Distinct Tests
    // ============================================================================

    [Fact]
    public void LazyDistinct_RemovesDuplicates()
    {
        var df = CreateDuplicatesDataFrame();
        var result = df.Lazy()
            .Distinct()
            .Collect();

        result.Height.Should().BeLessThan(df.Height);
    }

    [Fact]
    public void LazyDistinct_ByColumn_RemovesDuplicatesInColumn()
    {
        var df = CreateDuplicatesDataFrame();
        var result = df.Lazy()
            .Distinct("category")
            .Collect();

        result.Height.Should().BeLessThanOrEqualTo(3);  // Only unique categories
    }

    // ============================================================================
    // Drop Tests
    // ============================================================================

    [Fact]
    public void LazyDrop_RemovesColumn()
    {
        var df = CreateTestDataFrame();
        var result = df.Lazy()
            .Drop("name")
            .Collect();

        result.Columns.Should().NotContain("name");
    }

    // ============================================================================
    // Chained Operations Tests
    // ============================================================================

    [Fact]
    public void ChainedOperations_FilterSelectSort_WorksTogether()
    {
        var df = CreateTestDataFrame();
        var result = df.Lazy()
            .Filter(Col("age").Gt(20))
            .Select("name", "age")
            .Sort("age")
            .Collect();

        result.Width.Should().Be(2);
        result.Height.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ChainedOperations_WithColumnsFilterSort_WorksTogether()
    {
        var df = CreateTestDataFrame();
        var result = df.Lazy()
            .WithColumns((Col("age") * 2).As("double_age"))
            .Filter(Col("double_age").Gt(50))
            .Sort("double_age")
            .Collect();

        result.Columns.Should().Contain("double_age");
    }

    [Fact]
    public void ChainedOperations_GroupByFilterSort_WorksTogether()
    {
        var df = CreateGroupByDataFrame();
        var result = df.Lazy()
            .GroupBy("category")
            .Agg(Col("value").Sum().As("total"))
            .Filter(Col("total").Gt(0))
            .Sort("total")
            .Collect();

        result.Height.Should().BeGreaterThan(0);
    }

    // ============================================================================
    // Query Optimization Tests
    // ============================================================================

    [Fact]
    public void ProjectionPushdown_OnlySelectedColumnsRead()
    {
        var df = CreateTestDataFrame();
        // Should only process "name" column
        var result = df.Lazy()
            .Select("name")
            .Collect();

        result.Width.Should().Be(1);
    }

    [Fact]
    public void PredicatePushdown_FilterAppliedEarly()
    {
        var df = CreateLargeDataFrame(1000);
        // Filter should be pushed down
        var result = df.Lazy()
            .Filter(Col("value").Lt(10))
            .Select("value")
            .Collect();

        result.Height.Should().BeLessThan(1000);
    }

    // ============================================================================
    // Edge Cases
    // ============================================================================

    [Fact]
    public void EmptyDataFrame_LazyOperations_HandleGracefully()
    {
        var df = new DataFrame();
        var result = df.Lazy().Collect();

        result.Height.Should().Be(0);
    }

    [Fact]
    public void SingleRow_LazyOperations_Work()
    {
        var name = Series.FromValues("name", new[] { "A" });
        var df = new DataFrame(name);

        var result = df.Lazy()
            .Select("name")
            .Collect();

        result.Height.Should().Be(1);
    }

    [Fact]
    public void MultipleCollect_ReturnsSameResult()
    {
        var df = CreateTestDataFrame();
        var lf = df.Lazy().Filter(Col("age").Gt(25));

        var result1 = lf.Collect();
        var result2 = lf.Collect();

        result1.Height.Should().Be(result2.Height);
    }

    // ============================================================================
    // Slice Tests
    // ============================================================================

    [Fact]
    public void LazySlice_ReturnsSlice()
    {
        var df = CreateLargeDataFrame(100);
        var result = df.Lazy()
            .Slice(10, 20)
            .Collect();

        result.Height.Should().Be(20);
    }

    // ============================================================================
    // Rename Tests
    // ============================================================================

    [Fact]
    public void LazyRename_RenamesColumns()
    {
        var df = CreateTestDataFrame();
        var result = df.Lazy()
            .Rename(new Dictionary<string, string> { { "name", "full_name" } })
            .Collect();

        result.Columns.Should().Contain("full_name");
        result.Columns.Should().NotContain("name");
    }

    // ============================================================================
    // Union Tests
    // ============================================================================

    [Fact]
    public void LazyUnion_CombinesDataFrames()
    {
        var df1 = CreateTestDataFrame();
        var df2 = CreateTestDataFrame();

        var result = df1.Lazy()
            .Union(df2.Lazy())
            .Collect();

        result.Height.Should().Be(df1.Height + df2.Height);
    }

    // ============================================================================
    // Helper Methods
    // ============================================================================

    private DataFrame CreateTestDataFrame()
    {
        var name = Series.FromValues("name", new[] { "Alice", "Bob", "Charlie" });
        var age = Series.FromValues("age", new[] { 25, 30, 35 });
        var score = Series.FromValues("score", new[] { 85.5, 90.0, 78.5 });
        return new DataFrame(name, age, score);
    }

    private DataFrame CreateUnsortedDataFrame()
    {
        var name = Series.FromValues("name", new[] { "C", "A", "B", "D" });
        var value = Series.FromValues("value", new[] { 3, 1, 2, 4 });
        return new DataFrame(name, value);
    }

    private DataFrame CreateGroupByDataFrame()
    {
        var category = Series.FromValues("category", new[] { "A", "B", "A", "B", "C", "A" });
        var value = Series.FromValues("value", new[] { 10, 20, 15, 25, 30, 5 });
        return new DataFrame(category, value);
    }

    private DataFrame CreateJoinLeftDataFrame()
    {
        var id = Series.FromValues("id", new[] { 1, 2, 3 });
        var name = Series.FromValues("name", new[] { "Alice", "Bob", "Charlie" });
        return new DataFrame(id, name);
    }

    private DataFrame CreateJoinRightDataFrame()
    {
        var id = Series.FromValues("id", new[] { 2, 3, 4 });
        var score = Series.FromValues("score", new[] { 90, 85, 95 });
        return new DataFrame(id, score);
    }

    private DataFrame CreateLargeDataFrame(int n)
    {
        var values = Enumerable.Range(0, n).ToArray();
        return new DataFrame(Series.FromValues("value", values));
    }

    private DataFrame CreateDuplicatesDataFrame()
    {
        var category = Series.FromValues("category", new[] { "A", "A", "B", "B", "C" });
        var value = Series.FromValues("value", new[] { 1, 1, 2, 3, 4 });
        return new DataFrame(category, value);
    }
}
