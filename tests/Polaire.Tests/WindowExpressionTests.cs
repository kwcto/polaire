// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using Xunit;
using FluentAssertions;
using Polaire.Expressions;
using static Polaire.Expressions.Expr;

namespace Polaire.Tests;

/// <summary>
/// Tests for window function expressions (over clause).
/// </summary>
public class WindowExpressionTests
{
    // ============================================================================
    // Basic Window Aggregations
    // ============================================================================

    [Fact]
    public void Window_SumOver_ShouldReturnGroupSum()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "B", "B", "B" }),
            Series.FromValues("value", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 })
        );

        var result = df.Lazy()
            .WithColumns(Col("value").Sum().Over(Col("group")).As("group_sum"))
            .Collect();

        // A: 1+2=3, B: 3+4+5=12
        result["group_sum"][0].AsFloat64().Should().Be(3.0);
        result["group_sum"][1].AsFloat64().Should().Be(3.0);
        result["group_sum"][2].AsFloat64().Should().Be(12.0);
        result["group_sum"][3].AsFloat64().Should().Be(12.0);
        result["group_sum"][4].AsFloat64().Should().Be(12.0);
    }

    [Fact]
    public void Window_MeanOver_ShouldReturnGroupMean()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "B", "B" }),
            Series.FromValues("value", new[] { 10.0, 20.0, 100.0, 200.0 })
        );

        var result = df.Lazy()
            .WithColumns(Col("value").Mean().Over(Col("group")).As("group_mean"))
            .Collect();

        // A: (10+20)/2=15, B: (100+200)/2=150
        result["group_mean"][0].AsFloat64().Should().Be(15.0);
        result["group_mean"][1].AsFloat64().Should().Be(15.0);
        result["group_mean"][2].AsFloat64().Should().Be(150.0);
        result["group_mean"][3].AsFloat64().Should().Be(150.0);
    }

    [Fact]
    public void Window_MinMaxOver_ShouldReturnGroupMinMax()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "X", "X", "X", "Y", "Y" }),
            Series.FromValues("value", new[] { 5.0, 1.0, 3.0, 10.0, 20.0 })
        );

        var result = df.Lazy()
            .WithColumns(
                Col("value").Min().Over(Col("group")).As("min"),
                Col("value").Max().Over(Col("group")).As("max")
            )
            .Collect();

        // X: min=1, max=5, Y: min=10, max=20
        result["min"][0].AsFloat64().Should().Be(1.0);
        result["min"][3].AsFloat64().Should().Be(10.0);
        result["max"][0].AsFloat64().Should().Be(5.0);
        result["max"][3].AsFloat64().Should().Be(20.0);
    }

    [Fact]
    public void Window_CountOver_ShouldReturnGroupCount()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "A", "B", "B" }),
            Series.FromValues("value", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 })
        );

        var result = df.Lazy()
            .WithColumns(Col("value").CountExpr().Over(Col("group")).As("count"))
            .Collect();

        // A: 3, B: 2 (count returns as double when broadcast)
        result["count"][0].AsFloat64().Should().Be(3.0);
        result["count"][1].AsFloat64().Should().Be(3.0);
        result["count"][2].AsFloat64().Should().Be(3.0);
        result["count"][3].AsFloat64().Should().Be(2.0);
        result["count"][4].AsFloat64().Should().Be(2.0);
    }

    // ============================================================================
    // Cumulative Window Functions
    // ============================================================================

    [Fact]
    public void Window_CumSumOver_ShouldReturnCumulativeSumWithinGroup()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "A", "B", "B" }),
            Series.FromValues("value", new[] { 1.0, 2.0, 3.0, 10.0, 20.0 })
        );

        var result = df.Lazy()
            .WithColumns(Col("value").CumSumExpr().Over(Col("group")).As("cumsum"))
            .Collect();

        // Group A: 1, 1+2=3, 1+2+3=6
        // Group B: 10, 10+20=30
        result["cumsum"][0].AsFloat64().Should().Be(1.0);
        result["cumsum"][1].AsFloat64().Should().Be(3.0);
        result["cumsum"][2].AsFloat64().Should().Be(6.0);
        result["cumsum"][3].AsFloat64().Should().Be(10.0);
        result["cumsum"][4].AsFloat64().Should().Be(30.0);
    }

    [Fact]
    public void Window_CumMinOver_ShouldReturnCumulativeMinWithinGroup()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "A" }),
            Series.FromValues("value", new[] { 5.0, 2.0, 8.0 })
        );

        var result = df.Lazy()
            .WithColumns(Col("value").CumMinExpr().Over(Col("group")).As("cummin"))
            .Collect();

        // 5, min(5,2)=2, min(5,2,8)=2
        result["cummin"][0].AsFloat64().Should().Be(5.0);
        result["cummin"][1].AsFloat64().Should().Be(2.0);
        result["cummin"][2].AsFloat64().Should().Be(2.0);
    }

    [Fact]
    public void Window_CumMaxOver_ShouldReturnCumulativeMaxWithinGroup()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "A" }),
            Series.FromValues("value", new[] { 5.0, 2.0, 8.0 })
        );

        var result = df.Lazy()
            .WithColumns(Col("value").CumMaxExpr().Over(Col("group")).As("cummax"))
            .Collect();

        // 5, max(5,2)=5, max(5,2,8)=8
        result["cummax"][0].AsFloat64().Should().Be(5.0);
        result["cummax"][1].AsFloat64().Should().Be(5.0);
        result["cummax"][2].AsFloat64().Should().Be(8.0);
    }

    // ============================================================================
    // Ranking Functions
    // ============================================================================

    [Fact]
    public void Window_RankOver_ShouldReturnRankWithinGroup()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "A", "B", "B" }),
            Series.FromValues("value", new[] { 30.0, 10.0, 20.0, 50.0, 40.0 })
        );

        var result = df.Lazy()
            .WithColumns(Col("value").RankExpr().Over(Col("group")).As("rank"))
            .Collect();

        // Group A: 30->3, 10->1, 20->2
        // Group B: 50->2, 40->1
        result["rank"][0].AsFloat64().Should().Be(3.0);
        result["rank"][1].AsFloat64().Should().Be(1.0);
        result["rank"][2].AsFloat64().Should().Be(2.0);
        result["rank"][3].AsFloat64().Should().Be(2.0);
        result["rank"][4].AsFloat64().Should().Be(1.0);
    }

    [Fact]
    public void Window_DenseRankOver_ShouldReturnDenseRankWithinGroup()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "A", "A" }),
            Series.FromValues("value", new[] { 10.0, 20.0, 20.0, 30.0 })
        );

        var result = df.Lazy()
            .WithColumns(Col("value").DenseRankExpr().Over(Col("group")).As("dense_rank"))
            .Collect();

        // 10->1, 20->2 (tied), 20->2 (tied), 30->3 (no gap)
        result["dense_rank"][0].AsFloat64().Should().Be(1.0);
        result["dense_rank"][1].AsFloat64().Should().Be(2.0);
        result["dense_rank"][2].AsFloat64().Should().Be(2.0);
        result["dense_rank"][3].AsFloat64().Should().Be(3.0);
    }

    // ============================================================================
    // Shift/Lead/Lag Functions
    // ============================================================================

    [Fact]
    public void Window_ShiftOver_ShouldShiftWithinGroup()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "A", "B", "B" }),
            Series.FromValues("value", new[] { 1.0, 2.0, 3.0, 10.0, 20.0 })
        );

        var result = df.Lazy()
            .WithColumns(Col("value").ShiftExpr(1).Over(Col("group")).As("shifted"))
            .Collect();

        // Group A: null, 1, 2 (shifted forward by 1)
        // Group B: null, 10
        result["shifted"].IsNull(0).Should().BeTrue();
        result["shifted"][1].AsFloat64().Should().Be(1.0);
        result["shifted"][2].AsFloat64().Should().Be(2.0);
        result["shifted"].IsNull(3).Should().BeTrue();
        result["shifted"][4].AsFloat64().Should().Be(10.0);
    }

    [Fact]
    public void Window_LagOver_ShouldAccessPastValuesWithinGroup()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "A" }),
            Series.FromValues("value", new[] { 1.0, 2.0, 3.0 })
        );

        var result = df.Lazy()
            .WithColumns(Col("value").LagExpr(1).Over(Col("group")).As("lag"))
            .Collect();

        // Lag is same as shift(1): null, 1, 2
        result["lag"].IsNull(0).Should().BeTrue();
        result["lag"][1].AsFloat64().Should().Be(1.0);
        result["lag"][2].AsFloat64().Should().Be(2.0);
    }

    [Fact]
    public void Window_LeadOver_ShouldAccessFutureValuesWithinGroup()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "A" }),
            Series.FromValues("value", new[] { 1.0, 2.0, 3.0 })
        );

        var result = df.Lazy()
            .WithColumns(Col("value").LeadExpr(1).Over(Col("group")).As("lead"))
            .Collect();

        // Lead(1) accesses next value: 2, 3, null
        result["lead"][0].AsFloat64().Should().Be(2.0);
        result["lead"][1].AsFloat64().Should().Be(3.0);
        result["lead"].IsNull(2).Should().BeTrue();
    }

    // ============================================================================
    // Diff and PctChange
    // ============================================================================

    [Fact]
    public void Window_DiffOver_ShouldComputeDifferenceWithinGroup()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "A", "B", "B" }),
            Series.FromValues("value", new[] { 10.0, 15.0, 25.0, 100.0, 150.0 })
        );

        var result = df.Lazy()
            .WithColumns(Col("value").DiffExpr(1).Over(Col("group")).As("diff"))
            .Collect();

        // Group A: null, 15-10=5, 25-15=10
        // Group B: null, 150-100=50
        result["diff"].IsNull(0).Should().BeTrue();
        result["diff"][1].AsFloat64().Should().Be(5.0);
        result["diff"][2].AsFloat64().Should().Be(10.0);
        result["diff"].IsNull(3).Should().BeTrue();
        result["diff"][4].AsFloat64().Should().Be(50.0);
    }

    [Fact]
    public void Window_PctChangeOver_ShouldComputePercentChangeWithinGroup()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "A" }),
            Series.FromValues("value", new[] { 100.0, 150.0, 120.0 })
        );

        var result = df.Lazy()
            .WithColumns(Col("value").PctChangeExpr(1).Over(Col("group")).As("pct"))
            .Collect();

        // null, (150-100)/100=0.5, (120-150)/150=-0.2
        result["pct"].IsNull(0).Should().BeTrue();
        result["pct"][1].AsFloat64().Should().BeApproximately(0.5, 0.001);
        result["pct"][2].AsFloat64().Should().BeApproximately(-0.2, 0.001);
    }

    // ============================================================================
    // Multiple Partition Columns
    // ============================================================================

    [Fact]
    public void Window_MultiplePartitionBy_ShouldPartitionCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { "X", "X", "X", "Y" }),
            Series.FromValues("b", new[] { 1, 1, 2, 1 }),
            Series.FromValues("value", new[] { 10.0, 20.0, 30.0, 40.0 })
        );

        var result = df.Lazy()
            .WithColumns(Col("value").Sum().Over(Col("a"), Col("b")).As("sum"))
            .Collect();

        // (X,1): 10+20=30, (X,2): 30, (Y,1): 40
        result["sum"][0].AsFloat64().Should().Be(30.0);
        result["sum"][1].AsFloat64().Should().Be(30.0);
        result["sum"][2].AsFloat64().Should().Be(30.0);
        result["sum"][3].AsFloat64().Should().Be(40.0);
    }

    // ============================================================================
    // Edge Cases
    // ============================================================================

    [Fact]
    public void Window_EmptyDataFrame_ShouldHandleGracefully()
    {
        var df = new DataFrame(
            Series.FromValues("group", Array.Empty<string>()),
            Series.FromValues("value", Array.Empty<double>())
        );

        var result = df.Lazy()
            .WithColumns(Col("value").Sum().Over(Col("group")).As("sum"))
            .Collect();

        result.Height.Should().Be(0);
    }

    [Fact]
    public void Window_SingleRowPerGroup_ShouldWork()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "B", "C" }),
            Series.FromValues("value", new[] { 1.0, 2.0, 3.0 })
        );

        var result = df.Lazy()
            .WithColumns(Col("value").Sum().Over(Col("group")).As("sum"))
            .Collect();

        result["sum"][0].AsFloat64().Should().Be(1.0);
        result["sum"][1].AsFloat64().Should().Be(2.0);
        result["sum"][2].AsFloat64().Should().Be(3.0);
    }

    [Fact]
    public void Window_WithNulls_ShouldHandleCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "A" }),
            Series.FromNullable("value", new double?[] { 1.0, null, 3.0 })
        );

        var result = df.Lazy()
            .WithColumns(Col("value").Sum().Over(Col("group")).As("sum"))
            .Collect();

        // Sum skips nulls: 1+3=4
        result["sum"][0].AsFloat64().Should().Be(4.0);
        result["sum"][1].AsFloat64().Should().Be(4.0);
        result["sum"][2].AsFloat64().Should().Be(4.0);
    }

    [Fact]
    public void Window_NoPartitionBy_ShouldTreatEntireFrameAsPartition()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 })
        );

        var result = df.Lazy()
            .WithColumns(Col("value").Sum().Over().As("total"))
            .Collect();

        // Sum of entire frame: 15
        result["total"][0].AsFloat64().Should().Be(15.0);
        result["total"][2].AsFloat64().Should().Be(15.0);
        result["total"][4].AsFloat64().Should().Be(15.0);
    }

    [Fact]
    public void Window_CumSumNoPartition_ShouldComputeGlobalCumSum()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 1.0, 2.0, 3.0 })
        );

        var result = df.Lazy()
            .WithColumns(Col("value").CumSumExpr().Over().As("cumsum"))
            .Collect();

        result["cumsum"][0].AsFloat64().Should().Be(1.0);
        result["cumsum"][1].AsFloat64().Should().Be(3.0);
        result["cumsum"][2].AsFloat64().Should().Be(6.0);
    }

    // ============================================================================
    // Expression API Tests
    // ============================================================================

    [Fact]
    public void Expr_WindowMethods_ShouldCreateCorrectExpressions()
    {
        var col = Col("value");

        col.RankExpr().Should().BeOfType<Expr.Function>();
        col.DenseRankExpr().Should().BeOfType<Expr.Function>();
        col.ShiftExpr(1).Should().BeOfType<Expr.Function>();
        col.LeadExpr(1).Should().BeOfType<Expr.Function>();
        col.LagExpr(1).Should().BeOfType<Expr.Function>();
        col.CumSumExpr().Should().BeOfType<Expr.Function>();
        col.CumMinExpr().Should().BeOfType<Expr.Function>();
        col.CumMaxExpr().Should().BeOfType<Expr.Function>();
        col.CumProdExpr().Should().BeOfType<Expr.Function>();
        col.DiffExpr(1).Should().BeOfType<Expr.Function>();
        col.PctChangeExpr(1).Should().BeOfType<Expr.Function>();
    }

    [Fact]
    public void Expr_Over_ShouldCreateWindowExpression()
    {
        var expr = Col("value").Sum().Over(Col("group"));

        expr.Should().BeOfType<Expr.Window>();
        var window = (Expr.Window)expr;
        window.PartitionBy.Should().HaveCount(1);
    }

    // ============================================================================
    // New Window Functions (Rank/Interpolate Expression Methods)
    // ============================================================================

    [Fact]
    public void Window_RowNumber_ShouldReturn1IndexedRowNumber()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 10.0, 20.0, 30.0 })
        );

        var result = df.Lazy()
            .WithColumns(Col("value").RowNumberExpr().Over().As("row_num"))
            .Collect();

        result["row_num"][0].AsInt64().Should().Be(1);
        result["row_num"][1].AsInt64().Should().Be(2);
        result["row_num"][2].AsInt64().Should().Be(3);
    }

    [Fact]
    public void Window_RowNumberOver_ShouldResetPerGroup()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "B", "B", "B" }),
            Series.FromValues("value", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 })
        );

        var result = df.Lazy()
            .WithColumns(Col("value").RowNumberExpr().Over(Col("group")).As("row_num"))
            .Collect();

        // Group A: 1, 2
        // Group B: 1, 2, 3
        result["row_num"][0].AsInt64().Should().Be(1);
        result["row_num"][1].AsInt64().Should().Be(2);
        result["row_num"][2].AsInt64().Should().Be(1);
        result["row_num"][3].AsInt64().Should().Be(2);
        result["row_num"][4].AsInt64().Should().Be(3);
    }

    [Fact]
    public void Window_OrdinalRank_ShouldReturnUniqueRanks()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 30.0, 10.0, 20.0, 10.0 })
        );

        var result = df.Lazy()
            .WithColumns(Col("value").OrdinalRankExpr().Over().As("ordinal_rank"))
            .Collect();

        // Values sorted: 10(idx1), 10(idx3), 20(idx2), 30(idx0)
        // Ordinal ranks for original positions: 30->4, 10->1, 20->3, 10->2
        result["ordinal_rank"][0].AsFloat64().Should().Be(4.0);
        result["ordinal_rank"][1].AsFloat64().Should().Be(1.0);
        result["ordinal_rank"][2].AsFloat64().Should().Be(3.0);
        result["ordinal_rank"][3].AsFloat64().Should().Be(2.0);
    }

    [Fact]
    public void Window_FirstValue_ShouldReturnFirstNonNull()
    {
        var df = new DataFrame(
            Series.FromNullable("value", new double?[] { null, 10.0, 20.0, 30.0 })
        );

        var result = df.Lazy()
            .WithColumns(Col("value").FirstValueExpr().Over().As("first"))
            .Collect();

        // First non-null is 10.0
        result["first"][0].AsFloat64().Should().Be(10.0);
        result["first"][1].AsFloat64().Should().Be(10.0);
        result["first"][2].AsFloat64().Should().Be(10.0);
        result["first"][3].AsFloat64().Should().Be(10.0);
    }

    [Fact]
    public void Window_LastValue_ShouldReturnLastNonNull()
    {
        var df = new DataFrame(
            Series.FromNullable("value", new double?[] { 10.0, 20.0, 30.0, null })
        );

        var result = df.Lazy()
            .WithColumns(Col("value").LastValueExpr().Over().As("last"))
            .Collect();

        // Last non-null is 30.0
        result["last"][0].AsFloat64().Should().Be(30.0);
        result["last"][1].AsFloat64().Should().Be(30.0);
        result["last"][2].AsFloat64().Should().Be(30.0);
        result["last"][3].AsFloat64().Should().Be(30.0);
    }

    [Fact]
    public void Window_NthValue_ShouldReturnNthNonNull()
    {
        var df = new DataFrame(
            Series.FromNullable("value", new double?[] { null, 10.0, 20.0, 30.0 })
        );

        var result = df.Lazy()
            .WithColumns(Col("value").NthValueExpr(2).Over().As("second"))
            .Collect();

        // 2nd non-null is 20.0
        result["second"][0].AsFloat64().Should().Be(20.0);
        result["second"][1].AsFloat64().Should().Be(20.0);
        result["second"][2].AsFloat64().Should().Be(20.0);
        result["second"][3].AsFloat64().Should().Be(20.0);
    }

    [Fact]
    public void Window_FillForward_ShouldFillWithPreviousValue()
    {
        var df = new DataFrame(
            Series.FromNullable("value", new double?[] { 1.0, null, null, 4.0, null })
        );

        var result = df.Lazy()
            .WithColumns(Col("value").FillForwardExpr().Over().As("filled"))
            .Collect();

        // 1.0, 1.0, 1.0, 4.0, 4.0
        result["filled"][0].AsFloat64().Should().Be(1.0);
        result["filled"][1].AsFloat64().Should().Be(1.0);
        result["filled"][2].AsFloat64().Should().Be(1.0);
        result["filled"][3].AsFloat64().Should().Be(4.0);
        result["filled"][4].AsFloat64().Should().Be(4.0);
    }

    [Fact]
    public void Window_FillBackward_ShouldFillWithNextValue()
    {
        var df = new DataFrame(
            Series.FromNullable("value", new double?[] { null, null, 3.0, null, 5.0 })
        );

        var result = df.Lazy()
            .WithColumns(Col("value").FillBackwardExpr().Over().As("filled"))
            .Collect();

        // 3.0, 3.0, 3.0, 5.0, 5.0
        result["filled"][0].AsFloat64().Should().Be(3.0);
        result["filled"][1].AsFloat64().Should().Be(3.0);
        result["filled"][2].AsFloat64().Should().Be(3.0);
        result["filled"][3].AsFloat64().Should().Be(5.0);
        result["filled"][4].AsFloat64().Should().Be(5.0);
    }

    [Fact]
    public void Window_Interpolate_ShouldLinearlyInterpolate()
    {
        var df = new DataFrame(
            Series.FromNullable("value", new double?[] { 0.0, null, null, 3.0 })
        );

        var result = df.Lazy()
            .WithColumns(Col("value").InterpolateExpr().Over().As("interpolated"))
            .Collect();

        // Linear interpolation: 0.0, 1.0, 2.0, 3.0
        result["interpolated"][0].AsFloat64().Should().Be(0.0);
        result["interpolated"][1].AsFloat64().Should().Be(1.0);
        result["interpolated"][2].AsFloat64().Should().Be(2.0);
        result["interpolated"][3].AsFloat64().Should().Be(3.0);
    }

    [Fact]
    public void Window_FillForwardOver_ShouldFillWithinGroup()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "A", "B", "B" }),
            Series.FromNullable("value", new double?[] { 1.0, null, 3.0, null, 5.0 })
        );

        var result = df.Lazy()
            .WithColumns(Col("value").FillForwardExpr().Over(Col("group")).As("filled"))
            .Collect();

        // Group A: 1.0, 1.0, 3.0
        // Group B: null (no previous), 5.0
        result["filled"][0].AsFloat64().Should().Be(1.0);
        result["filled"][1].AsFloat64().Should().Be(1.0);
        result["filled"][2].AsFloat64().Should().Be(3.0);
        result["filled"].IsNull(3).Should().BeTrue();
        result["filled"][4].AsFloat64().Should().Be(5.0);
    }

    [Fact]
    public void Expr_NewWindowMethods_ShouldCreateCorrectExpressions()
    {
        var col = Col("value");

        col.RowNumberExpr().Should().BeOfType<Expr.Function>();
        col.OrdinalRankExpr().Should().BeOfType<Expr.Function>();
        col.FirstValueExpr().Should().BeOfType<Expr.Function>();
        col.LastValueExpr().Should().BeOfType<Expr.Function>();
        col.NthValueExpr(2).Should().BeOfType<Expr.Function>();
        col.FillForwardExpr().Should().BeOfType<Expr.Function>();
        col.FillBackwardExpr().Should().BeOfType<Expr.Function>();
        col.InterpolateExpr().Should().BeOfType<Expr.Function>();
    }
}
