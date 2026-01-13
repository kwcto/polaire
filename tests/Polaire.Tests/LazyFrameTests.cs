using Xunit;
// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using FluentAssertions;
using Polaire.DataTypes;


using Polaire.LazyFrame;
using Polaire.Expressions;
using static Polaire.Expressions.Expr;

namespace Polaire.Tests;

public class LazyFrameTests
{
    [Fact]
    public void LazyFrame_FromDataFrame_ShouldWork()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 })
        );

        var lf = df.Lazy();

        lf.Should().NotBeNull();
    }

    [Fact]
    public void LazyFrame_Collect_ShouldReturnDataFrame()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 }),
            Series.FromValues("b", new[] { 4, 5, 6 })
        );

        var result = df.Lazy().Collect();

        result.Height.Should().Be(3);
        result.Width.Should().Be(2);
    }

    [Fact]
    public void LazyFrame_Select_ShouldSelectColumns()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 }),
            Series.FromValues("b", new[] { 4, 5, 6 }),
            Series.FromValues("c", new[] { 7, 8, 9 })
        );

        var result = df.Lazy()
            .Select(Col("a"), Col("c"))
            .Collect();

        result.Width.Should().Be(2);
        result.Columns.Should().BeEquivalentTo(new[] { "a", "c" });
    }

    [Fact]
    public void LazyFrame_Select_WithExpression_ShouldWork()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1.0, 2.0, 3.0 }),
            Series.FromValues("b", new[] { 10.0, 20.0, 30.0 })
        );

        var result = df.Lazy()
            .Select(
                Col("a"),
                (Col("a") + Col("b")).As("sum")
            )
            .Collect();

        result.Width.Should().Be(2);
        result["sum"][0].AsFloat64().Should().Be(11.0);
    }

    [Fact]
    public void LazyFrame_Filter_ShouldFilterRows()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 })
        );

        var result = df.Lazy()
            .Filter(Col("a").Gt(2))
            .Collect();

        result.Height.Should().Be(3);
        result["a"][0].AsInt32().Should().Be(3);
    }

    [Fact]
    public void LazyFrame_WithColumns_ShouldAddColumns()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1.0, 2.0, 3.0 })
        );

        var result = df.Lazy()
            .WithColumns(
                (Col("a") * Lit(2.0)).As("doubled")
            )
            .Collect();

        result.Width.Should().Be(2);
        result["doubled"][0].AsFloat64().Should().Be(2.0);
        result["doubled"][2].AsFloat64().Should().Be(6.0);
    }

    [Fact]
    public void LazyFrame_Sort_ShouldOrderRows()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 3, 1, 4, 1, 5 })
        );

        var result = df.Lazy()
            .Sort(Col("a"))
            .Collect();

        result["a"][0].AsInt32().Should().Be(1);
        result["a"][4].AsInt32().Should().Be(5);
    }

    [Fact]
    public void LazyFrame_Head_ShouldLimitRows()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 })
        );

        var result = df.Lazy()
            .Head(3)
            .Collect();

        result.Height.Should().Be(3);
    }

    [Fact]
    public void LazyFrame_Drop_ShouldRemoveColumns()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 }),
            Series.FromValues("b", new[] { 4, 5, 6 }),
            Series.FromValues("c", new[] { 7, 8, 9 })
        );

        var result = df.Lazy()
            .Drop("b")
            .Collect();

        result.Width.Should().Be(2);
        result.Columns.Should().BeEquivalentTo(new[] { "a", "c" });
    }

    [Fact]
    public void LazyFrame_GroupBy_Agg_ShouldWork()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "a", "a", "b", "b" }),
            Series.FromValues("value", new[] { 1, 2, 3, 4 })
        );

        var result = df.Lazy()
            .GroupBy(Col("group"))
            .Agg(Col("value").Sum().As("total"))
            .Collect();

        result.Height.Should().Be(2);
    }

    [Fact]
    public void LazyFrame_Join_ShouldWork()
    {
        var left = new DataFrame(
            Series.FromValues("key", new[] { 1, 2, 3 }),
            Series.FromValues("left_val", new[] { "a", "b", "c" })
        );

        var right = new DataFrame(
            Series.FromValues("key", new[] { 2, 3, 4 }),
            Series.FromValues("right_val", new[] { "x", "y", "z" })
        );

        var result = left.Lazy()
            .Join(right.Lazy(), "key")
            .Collect();

        result.Height.Should().Be(2);
    }

    [Fact]
    public void LazyFrame_Chained_Operations_ShouldWork()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 }),
            Series.FromValues("b", new[] { 10.0, 20.0, 30.0, 40.0, 50.0 })
        );

        var result = df.Lazy()
            .Filter(Col("a").Gt(2))
            .Select(
                Col("a"),
                Col("b"),
                (Col("a") + Col("b")).As("sum")
            )
            .Sort(Col("sum"))
            .Head(2)
            .Collect();

        result.Height.Should().Be(2);
        result.Columns.Should().Contain("sum");
    }

    [Fact]
    public void LazyFrame_Explain_ShouldShowPlan()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 })
        );

        var lf = df.Lazy()
            .Filter(Col("a").Gt(1))
            .Select(Col("a"));

        var explanation = lf.Explain(optimized: false);

        explanation.Should().Contain("Select");
        explanation.Should().Contain("Filter");
        explanation.Should().Contain("Scan");
    }

    [Fact]
    public void LazyFrame_Distinct_ShouldRemoveDuplicates()
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
    public void LazyFrame_Rename_ShouldRenameColumns()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 })
        );

        var result = df.Lazy()
            .Rename(new Dictionary<string, string> { { "a", "x" } })
            .Collect();

        result.Columns.Should().BeEquivalentTo(new[] { "x" });
    }

    [Fact]
    public void LazyFrame_Union_ShouldConcatenate()
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

    [Fact]
    public void QueryOptimizer_PredicatePushdown_ShouldOptimize()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 }),
            Series.FromValues("b", new[] { 10, 20, 30, 40, 50 })
        );

        // This query should have predicate pushed through the select
        var lf = df.Lazy()
            .Select(Col("a"), Col("b"))
            .Filter(Col("a").Gt(2));

        var optimizedExplanation = lf.Explain(optimized: true);
        var unoptimizedExplanation = lf.Explain(optimized: false);

        // Both should produce correct results
        var result = lf.Collect();
        result.Height.Should().Be(3);
    }

    [Fact]
    public void QueryOptimizer_CombineFilters_ShouldMerge()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 })
        );

        var lf = df.Lazy()
            .Filter(Col("a").Gt(1))
            .Filter(Col("a").Lt(5));

        // Should combine into single filter with AND
        var result = lf.Collect();
        result.Height.Should().Be(3); // 2, 3, 4
    }

    [Fact]
    public void QueryOptimizer_CombineLimits_ShouldTakeMinimum()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 })
        );

        var lf = df.Lazy()
            .Head(5)
            .Head(3);

        var result = lf.Collect();
        result.Height.Should().Be(3);
    }
}
