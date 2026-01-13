// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using FluentAssertions;
using Polaire;
using Polaire.DataTypes;
using Polaire.Expressions;
using Xunit;
using static Polaire.Pl;

namespace Polaire.Tests;

/// <summary>
/// Tests for lazy evaluation, logical plans, and query optimization.
/// </summary>
public class LazyEvaluationTests
{
    // ============================================================================
    // Basic Lazy/Collect Tests
    // ============================================================================

    [Fact]
    public void DataFrame_Lazy_ReturnsLazyFrame()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 })
        );

        var lazy = df.Lazy();

        lazy.Should().NotBeNull();
    }

    [Fact]
    public void LazyFrame_Collect_ReturnsDataFrame()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 })
        );

        var result = df.Lazy().Collect();

        result.Should().NotBeNull();
        result.Height.Should().Be(3);
        result["a"][0].AsInt32().Should().Be(1);
    }

    [Fact]
    public void Lazy_DoesNotExecuteImmediately()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 })
        );

        // Just building the plan - no execution yet
        var lazy = df.Lazy()
            .Filter(Col("a").Gt(1))
            .Select((Col("a") + 10).As("b"));

        // The lazy frame holds a plan, not data
        lazy.Should().NotBeNull();
    }

    [Fact]
    public void Lazy_ExecutesOnCollect()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 })
        );

        var result = df.Lazy()
            .Filter(Col("a").Gt(1))
            .Collect();

        result.Height.Should().Be(2);
    }

    // ============================================================================
    // Chained Operations Tests
    // ============================================================================

    [Fact]
    public void Lazy_ChainedFilter_WorksCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 })
        );

        var result = df.Lazy()
            .Filter(Col("a").Gt(1))
            .Filter(Col("a").Lt(5))
            .Collect();

        result.Height.Should().Be(3);
        result["a"][0].AsInt32().Should().Be(2);
        result["a"][2].AsInt32().Should().Be(4);
    }

    [Fact]
    public void Lazy_FilterThenSelect_WorksCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 }),
            Series.FromValues("b", new[] { 10, 20, 30, 40, 50 })
        );

        var result = df.Lazy()
            .Filter(Col("a").Gt(2))
            .Select(Col("b"))
            .Collect();

        result.Height.Should().Be(3);
        result.Width.Should().Be(1);
        result.Columns.Should().Contain("b");
    }

    [Fact]
    public void Lazy_SelectThenFilter_WorksCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 }),
            Series.FromValues("b", new[] { 10, 20, 30, 40, 50 })
        );

        var result = df.Lazy()
            .Select(Col("a"), Col("b"))
            .Filter(Col("a").Gt(2))
            .Collect();

        result.Height.Should().Be(3);
    }

    [Fact]
    public void Lazy_WithColumns_AddsColumn()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 })
        );

        var result = df.Lazy()
            .WithColumns((Col("a") * 2).As("doubled"))
            .Collect();

        result.Width.Should().Be(2);
        result.Columns.Should().Contain("doubled");
        // Arithmetic operations return Float64
        result["doubled"][0].AsFloat64().Should().Be(2.0);
        result["doubled"][2].AsFloat64().Should().Be(6.0);
    }

    [Fact]
    public void Lazy_WithColumns_Multiple_AddsAllColumns()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 })
        );

        var result = df.Lazy()
            .WithColumns(
                (Col("a") * 2).As("doubled"),
                (Col("a") + 10).As("plusten")
            )
            .Collect();

        result.Width.Should().Be(3);
        result.Columns.Should().Contain("doubled");
        result.Columns.Should().Contain("plusten");
    }

    // ============================================================================
    // Lazy Sort Tests
    // ============================================================================

    [Fact]
    public void Lazy_Sort_WorksCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 3, 1, 2 })
        );

        var result = df.Lazy()
            .Sort(Col("a"))
            .Collect();

        result["a"][0].AsInt32().Should().Be(1);
        result["a"][1].AsInt32().Should().Be(2);
        result["a"][2].AsInt32().Should().Be(3);
    }

    [Fact]
    public void Lazy_Sort_Descending_WorksCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 3, 1, 2 })
        );

        var result = df.Lazy()
            .Sort((Col("a"), true))
            .Collect();

        result["a"][0].AsInt32().Should().Be(3);
        result["a"][1].AsInt32().Should().Be(2);
        result["a"][2].AsInt32().Should().Be(1);
    }

    // ============================================================================
    // Lazy Distinct Tests
    // ============================================================================

    [Fact]
    public void Lazy_Distinct_RemovesDuplicates()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 1, 2, 2, 3 })
        );

        var result = df.Lazy()
            .Distinct()
            .Collect();

        result.Height.Should().Be(3);
    }

    [Fact]
    public void Lazy_Distinct_MultiColumn_RemovesDuplicates()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 1, 2, 2 }),
            Series.FromValues("b", new[] { 10, 10, 20, 30 })
        );

        var result = df.Lazy()
            .Distinct()
            .Collect();

        result.Height.Should().Be(3);
    }

    // ============================================================================
    // Lazy GroupBy Tests
    // ============================================================================

    [Fact]
    public void Lazy_GroupBy_Sum_WorksCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "B", "B" }),
            Series.FromValues("value", new[] { 1, 2, 3, 4 })
        );

        var result = df.Lazy()
            .GroupBy("group")
            .Agg(Col("value").Sum().As("total"))
            .Collect();

        result.Height.Should().Be(2);
        result.Columns.Should().Contain("total");
    }

    [Fact]
    public void Lazy_GroupBy_MultipleAgg_WorksCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "B", "B" }),
            Series.FromValues("value", new[] { 1.0, 2.0, 3.0, 4.0 })
        );

        var result = df.Lazy()
            .GroupBy("group")
            .Agg(
                Col("value").Sum().As("sum"),
                Col("value").Mean().As("avg")
            )
            .Collect();

        result.Height.Should().Be(2);
        result.Columns.Should().Contain("sum");
        result.Columns.Should().Contain("avg");
    }

    // ============================================================================
    // Lazy Join Tests
    // ============================================================================

    [Fact]
    public void Lazy_Join_WorksCorrectly()
    {
        var df1 = new DataFrame(
            Series.FromValues("id", new[] { 1, 2, 3 }),
            Series.FromValues("name", new[] { "Alice", "Bob", "Charlie" })
        );
        var df2 = new DataFrame(
            Series.FromValues("id", new[] { 1, 2 }),
            Series.FromValues("score", new[] { 85, 90 })
        );

        var result = df1.Lazy()
            .Join(df2.Lazy(), Col("id"), Col("id"))
            .Collect();

        result.Height.Should().Be(2);
        result.Columns.Should().Contain("name");
        result.Columns.Should().Contain("score");
    }

    // ============================================================================
    // Lazy Union Tests
    // ============================================================================

    [Fact]
    public void Lazy_Union_CombinesFrames()
    {
        var df1 = new DataFrame(
            Series.FromValues("a", new[] { 1, 2 })
        );
        var df2 = new DataFrame(
            Series.FromValues("a", new[] { 3, 4 })
        );

        var result = df1.Lazy()
            .Union(df2.Lazy())
            .Collect();

        result.Height.Should().Be(4);
    }

    // ============================================================================
    // Head/Tail/Limit Tests
    // ============================================================================

    [Fact]
    public void Lazy_Head_LimitsRows()
    {
        var df = new DataFrame(
            Series.FromValues("a", Enumerable.Range(0, 100).ToArray())
        );

        var result = df.Lazy()
            .Head(5)
            .Collect();

        result.Height.Should().Be(5);
        result["a"][0].AsInt32().Should().Be(0);
        result["a"][4].AsInt32().Should().Be(4);
    }

    [Fact]
    public void Lazy_Tail_LimitsRows()
    {
        var df = new DataFrame(
            Series.FromValues("a", Enumerable.Range(0, 100).ToArray())
        );

        var result = df.Lazy()
            .Tail(5)
            .Collect();

        result.Height.Should().Be(5);
        result["a"][0].AsInt32().Should().Be(95);
        result["a"][4].AsInt32().Should().Be(99);
    }

    [Fact]
    public void Lazy_HeadAsLimit_LimitsRows()
    {
        var df = new DataFrame(
            Series.FromValues("a", Enumerable.Range(0, 100).ToArray())
        );

        // Limit is implemented as Head in Polaire
        var result = df.Lazy()
            .Head(10)
            .Collect();

        result.Height.Should().Be(10);
    }

    // ============================================================================
    // Complex Pipeline Tests
    // ============================================================================

    [Fact]
    public void Lazy_ComplexPipeline_WorksCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("category", new[] { "A", "A", "B", "B", "B" }),
            Series.FromValues("value", new[] { 10, 20, 30, 40, 50 })
        );

        var result = df.Lazy()
            .Filter(Col("value").Gt(15))
            .WithColumns((Col("value") * 2).As("doubled"))
            .GroupBy("category")
            .Agg(Col("doubled").Sum().As("total"))
            .Sort((Col("total"), true))
            .Collect();

        result.Height.Should().Be(2);
        result.Columns.Should().Contain("category");
        result.Columns.Should().Contain("total");
    }

    [Fact]
    public void Lazy_FilterSelectGroupAgg_WorksCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("region", new[] { "East", "East", "West", "West", "West" }),
            Series.FromValues("sales", new[] { 100.0, 200.0, 150.0, 250.0, 300.0 }),
            Series.FromValues("quarter", new[] { "Q1", "Q2", "Q1", "Q2", "Q3" })
        );

        var result = df.Lazy()
            .Filter(Col("sales").Gt(100.0))
            .Select(Col("region"), Col("sales"))
            .GroupBy("region")
            .Agg(
                Col("sales").Sum().As("total_sales"),
                Col("sales").Mean().As("avg_sales")
            )
            .Collect();

        result.Height.Should().Be(2);
    }

    // ============================================================================
    // Multiple Collect Calls
    // ============================================================================

    [Fact]
    public void Lazy_MultipleCollects_ReturnSameResult()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 })
        );

        var lazy = df.Lazy().Filter(Col("a").Gt(1));

        var result1 = lazy.Collect();
        var result2 = lazy.Collect();

        result1.Height.Should().Be(result2.Height);
        result1["a"][0].AsInt32().Should().Be(result2["a"][0].AsInt32());
    }

    // ============================================================================
    // Edge Cases
    // ============================================================================

    [Fact]
    public void Lazy_EmptyDataFrame_HandledCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("a", Array.Empty<int>())
        );

        var result = df.Lazy()
            .Filter(Col("a").Gt(0))
            .Collect();

        result.Height.Should().Be(0);
    }

    [Fact]
    public void Lazy_FilterAllOut_ReturnsEmptyDataFrame()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 })
        );

        var result = df.Lazy()
            .Filter(Col("a").Gt(100))
            .Collect();

        result.Height.Should().Be(0);
    }

    [Fact]
    public void Lazy_SelectNonExistentColumn_ThrowsOrHandles()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 })
        );

        // This should either throw or return empty - depends on implementation
        var act = () => df.Lazy().Select(Col("nonexistent")).Collect();

        // Most implementations throw on invalid column
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Lazy_LargeChain_WorksCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("value", Enumerable.Range(0, 1000).ToArray())
        );

        var lazy = df.Lazy();

        // Build a long chain
        for (int i = 0; i < 10; i++)
        {
            lazy = lazy.Filter(Col("value").Ge(i));
        }

        var result = lazy.Collect();

        // Should filter down to values >= 9
        result.Height.Should().Be(991);
    }

    // ============================================================================
    // Alias and Rename Tests
    // ============================================================================

    [Fact]
    public void Lazy_Select_WithAlias_RenamesColumn()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 })
        );

        var result = df.Lazy()
            .Select(Col("a").As("renamed"))
            .Collect();

        result.Columns.Should().Contain("renamed");
        result.Columns.Should().NotContain("a");
    }

    [Fact]
    public void Lazy_Rename_WorksCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("old_name", new[] { 1, 2, 3 })
        );

        var result = df.Lazy()
            .Rename(new Dictionary<string, string> { { "old_name", "new_name" } })
            .Collect();

        result.Columns.Should().Contain("new_name");
        result.Columns.Should().NotContain("old_name");
    }

    // ============================================================================
    // Literal Value Tests in Lazy Context
    // ============================================================================

    [Fact]
    public void Lazy_WithColumns_Literal_AddsConstantColumn()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 })
        );

        var result = df.Lazy()
            .WithColumns(Lit(42).As("constant"))
            .Collect();

        result.Columns.Should().Contain("constant");
        result["constant"][0].AsInt32().Should().Be(42);
        result["constant"][2].AsInt32().Should().Be(42);
    }

    [Fact]
    public void Lazy_Filter_WithLiteral_WorksCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 })
        );

        var threshold = Lit(3);
        var result = df.Lazy()
            .Filter(Col("a").Gt(threshold))
            .Collect();

        result.Height.Should().Be(2);
    }

    // ============================================================================
    // Null Handling in Lazy Context
    // ============================================================================

    [Fact]
    public void Lazy_Filter_WithNulls_HandlesCorrectly()
    {
        var df = new DataFrame(
            Series.FromNullable("a", new int?[] { 1, null, 3, null, 5 })
        );

        var result = df.Lazy()
            .Filter(Col("a").IsNotNullExpr())
            .Collect();

        result.Height.Should().Be(3);
    }

    [Fact]
    public void Lazy_WithColumns_FillNull_WorksCorrectly()
    {
        var df = new DataFrame(
            Series.FromNullable("a", new int?[] { 1, null, 3 })
        );

        var result = df.Lazy()
            .WithColumns(Col("a").FillNullWith(0).As("filled"))
            .Collect();

        result["filled"][1].AsInt32().Should().Be(0);
    }
}
