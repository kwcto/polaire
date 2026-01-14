// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using Xunit;
using static Polaire.Pl;

namespace Polaire.Tests;

/// <summary>
/// Tests for rolling/expanding/ewm expression methods.
/// </summary>
public class RollingExpressionTests
{
    // ============================================================================
    // Rolling Operations via Expressions
    // ============================================================================

    [Fact]
    public void RollingSumExpr_BasicWindow_ReturnsCorrectValues()
    {
        var df = DataFrame(
            Series("value", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 })
        );

        var result = df.Lazy()
            .WithColumns(Col("value").RollingSumExpr(3).As("rolling_sum"))
            .Collect();

        Assert.Equal(5, result.Height);
        // First two values don't have full window (minPeriods=1 by default)
        Assert.True(result["rolling_sum"][0].TryGetDouble(out var v0) && Math.Abs(v0 - 1.0) < 0.001);
        Assert.True(result["rolling_sum"][1].TryGetDouble(out var v1) && Math.Abs(v1 - 3.0) < 0.001);
        Assert.True(result["rolling_sum"][2].TryGetDouble(out var v2) && Math.Abs(v2 - 6.0) < 0.001);
        Assert.True(result["rolling_sum"][3].TryGetDouble(out var v3) && Math.Abs(v3 - 9.0) < 0.001);
        Assert.True(result["rolling_sum"][4].TryGetDouble(out var v4) && Math.Abs(v4 - 12.0) < 0.001);
    }

    [Fact]
    public void RollingMeanExpr_BasicWindow_ReturnsCorrectValues()
    {
        var df = DataFrame(
            Series("value", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 })
        );

        var result = df.Lazy()
            .WithColumns(Col("value").RollingMeanExpr(3).As("rolling_mean"))
            .Collect();

        Assert.Equal(5, result.Height);
        // Full window values
        Assert.True(result["rolling_mean"][2].TryGetDouble(out var v2) && Math.Abs(v2 - 2.0) < 0.001);
        Assert.True(result["rolling_mean"][3].TryGetDouble(out var v3) && Math.Abs(v3 - 3.0) < 0.001);
        Assert.True(result["rolling_mean"][4].TryGetDouble(out var v4) && Math.Abs(v4 - 4.0) < 0.001);
    }

    [Fact]
    public void RollingMinExpr_BasicWindow_ReturnsCorrectValues()
    {
        var df = DataFrame(
            Series("value", new[] { 3.0, 1.0, 4.0, 1.0, 5.0 })
        );

        var result = df.Lazy()
            .WithColumns(Col("value").RollingMinExpr(3).As("rolling_min"))
            .Collect();

        Assert.Equal(5, result.Height);
        Assert.True(result["rolling_min"][2].TryGetDouble(out var v2) && Math.Abs(v2 - 1.0) < 0.001);
        Assert.True(result["rolling_min"][3].TryGetDouble(out var v3) && Math.Abs(v3 - 1.0) < 0.001);
        Assert.True(result["rolling_min"][4].TryGetDouble(out var v4) && Math.Abs(v4 - 1.0) < 0.001);
    }

    [Fact]
    public void RollingMaxExpr_BasicWindow_ReturnsCorrectValues()
    {
        var df = DataFrame(
            Series("value", new[] { 3.0, 1.0, 4.0, 1.0, 5.0 })
        );

        var result = df.Lazy()
            .WithColumns(Col("value").RollingMaxExpr(3).As("rolling_max"))
            .Collect();

        Assert.Equal(5, result.Height);
        Assert.True(result["rolling_max"][2].TryGetDouble(out var v2) && Math.Abs(v2 - 4.0) < 0.001);
        Assert.True(result["rolling_max"][3].TryGetDouble(out var v3) && Math.Abs(v3 - 4.0) < 0.001);
        Assert.True(result["rolling_max"][4].TryGetDouble(out var v4) && Math.Abs(v4 - 5.0) < 0.001);
    }

    [Fact]
    public void RollingStdExpr_BasicWindow_ReturnsCorrectValues()
    {
        var df = DataFrame(
            Series("value", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 })
        );

        var result = df.Lazy()
            .WithColumns(Col("value").RollingStdExpr(3).As("rolling_std"))
            .Collect();

        Assert.Equal(5, result.Height);
        // Std of [1,2,3] = 1.0
        Assert.True(result["rolling_std"][2].TryGetDouble(out var v2) && Math.Abs(v2 - 1.0) < 0.001);
    }

    [Fact]
    public void RollingMedianExpr_BasicWindow_ReturnsCorrectValues()
    {
        var df = DataFrame(
            Series("value", new[] { 1.0, 5.0, 2.0, 4.0, 3.0 })
        );

        var result = df.Lazy()
            .WithColumns(Col("value").RollingMedianExpr(3).As("rolling_median"))
            .Collect();

        Assert.Equal(5, result.Height);
        // Median of [1,5,2] = 2
        Assert.True(result["rolling_median"][2].TryGetDouble(out var v2) && Math.Abs(v2 - 2.0) < 0.001);
        // Median of [5,2,4] = 4
        Assert.True(result["rolling_median"][3].TryGetDouble(out var v3) && Math.Abs(v3 - 4.0) < 0.001);
        // Median of [2,4,3] = 3
        Assert.True(result["rolling_median"][4].TryGetDouble(out var v4) && Math.Abs(v4 - 3.0) < 0.001);
    }

    // ============================================================================
    // Expanding Operations via Expressions
    // ============================================================================

    [Fact]
    public void ExpandingSumExpr_ReturnsRunningSum()
    {
        var df = DataFrame(
            Series("value", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 })
        );

        var result = df.Lazy()
            .WithColumns(Col("value").ExpandingSumExpr().As("expanding_sum"))
            .Collect();

        Assert.Equal(5, result.Height);
        Assert.True(result["expanding_sum"][0].TryGetDouble(out var v0) && Math.Abs(v0 - 1.0) < 0.001);
        Assert.True(result["expanding_sum"][1].TryGetDouble(out var v1) && Math.Abs(v1 - 3.0) < 0.001);
        Assert.True(result["expanding_sum"][2].TryGetDouble(out var v2) && Math.Abs(v2 - 6.0) < 0.001);
        Assert.True(result["expanding_sum"][3].TryGetDouble(out var v3) && Math.Abs(v3 - 10.0) < 0.001);
        Assert.True(result["expanding_sum"][4].TryGetDouble(out var v4) && Math.Abs(v4 - 15.0) < 0.001);
    }

    [Fact]
    public void ExpandingMeanExpr_ReturnsRunningMean()
    {
        var df = DataFrame(
            Series("value", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 })
        );

        var result = df.Lazy()
            .WithColumns(Col("value").ExpandingMeanExpr().As("expanding_mean"))
            .Collect();

        Assert.Equal(5, result.Height);
        Assert.True(result["expanding_mean"][0].TryGetDouble(out var v0) && Math.Abs(v0 - 1.0) < 0.001);
        Assert.True(result["expanding_mean"][1].TryGetDouble(out var v1) && Math.Abs(v1 - 1.5) < 0.001);
        Assert.True(result["expanding_mean"][2].TryGetDouble(out var v2) && Math.Abs(v2 - 2.0) < 0.001);
        Assert.True(result["expanding_mean"][3].TryGetDouble(out var v3) && Math.Abs(v3 - 2.5) < 0.001);
        Assert.True(result["expanding_mean"][4].TryGetDouble(out var v4) && Math.Abs(v4 - 3.0) < 0.001);
    }

    [Fact]
    public void ExpandingMinExpr_ReturnsRunningMin()
    {
        var df = DataFrame(
            Series("value", new[] { 3.0, 1.0, 4.0, 1.0, 5.0 })
        );

        var result = df.Lazy()
            .WithColumns(Col("value").ExpandingMinExpr().As("expanding_min"))
            .Collect();

        Assert.Equal(5, result.Height);
        Assert.True(result["expanding_min"][0].TryGetDouble(out var v0) && Math.Abs(v0 - 3.0) < 0.001);
        Assert.True(result["expanding_min"][1].TryGetDouble(out var v1) && Math.Abs(v1 - 1.0) < 0.001);
        Assert.True(result["expanding_min"][2].TryGetDouble(out var v2) && Math.Abs(v2 - 1.0) < 0.001);
    }

    [Fact]
    public void ExpandingMaxExpr_ReturnsRunningMax()
    {
        var df = DataFrame(
            Series("value", new[] { 1.0, 3.0, 2.0, 5.0, 4.0 })
        );

        var result = df.Lazy()
            .WithColumns(Col("value").ExpandingMaxExpr().As("expanding_max"))
            .Collect();

        Assert.Equal(5, result.Height);
        Assert.True(result["expanding_max"][0].TryGetDouble(out var v0) && Math.Abs(v0 - 1.0) < 0.001);
        Assert.True(result["expanding_max"][1].TryGetDouble(out var v1) && Math.Abs(v1 - 3.0) < 0.001);
        Assert.True(result["expanding_max"][2].TryGetDouble(out var v2) && Math.Abs(v2 - 3.0) < 0.001);
        Assert.True(result["expanding_max"][3].TryGetDouble(out var v3) && Math.Abs(v3 - 5.0) < 0.001);
        Assert.True(result["expanding_max"][4].TryGetDouble(out var v4) && Math.Abs(v4 - 5.0) < 0.001);
    }

    // ============================================================================
    // EWM Operations via Expressions
    // ============================================================================

    [Fact]
    public void EwmMeanExpr_ReturnsExponentiallyWeightedMean()
    {
        var df = DataFrame(
            Series("value", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 })
        );

        var result = df.Lazy()
            .WithColumns(Col("value").EwmMeanExpr(0.5).As("ewm_mean"))
            .Collect();

        Assert.Equal(5, result.Height);
        // First value should be itself
        Assert.True(result["ewm_mean"][0].TryGetDouble(out var v0) && Math.Abs(v0 - 1.0) < 0.001);
        // Subsequent values should be weighted averages
        Assert.False(result["ewm_mean"].IsNull(1));
        Assert.False(result["ewm_mean"].IsNull(4));
    }

    [Fact]
    public void EwmStdExpr_ReturnsExponentiallyWeightedStd()
    {
        var df = DataFrame(
            Series("value", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 })
        );

        var result = df.Lazy()
            .WithColumns(Col("value").EwmStdExpr(0.5).As("ewm_std"))
            .Collect();

        Assert.Equal(5, result.Height);
        // First value may be null (needs minPeriods=2)
        Assert.False(result["ewm_std"].IsNull(4));
    }

    // ============================================================================
    // Rolling with Window Functions (Over)
    // ============================================================================

    [Fact]
    public void RollingSumExpr_WithOver_PartitionedRolling()
    {
        var df = DataFrame(
            Series("group", new[] { "A", "A", "A", "B", "B", "B" }),
            Series("value", new[] { 1.0, 2.0, 3.0, 10.0, 20.0, 30.0 })
        );

        var result = df.Lazy()
            .WithColumns(
                Col("value").RollingSumExpr(2).Over(Col("group")).As("group_rolling")
            )
            .Collect();

        Assert.Equal(6, result.Height);
        // Group A: [1], [1+2], [2+3]
        Assert.True(result["group_rolling"][0].TryGetDouble(out var a0) && Math.Abs(a0 - 1.0) < 0.001);
        Assert.True(result["group_rolling"][1].TryGetDouble(out var a1) && Math.Abs(a1 - 3.0) < 0.001);
        Assert.True(result["group_rolling"][2].TryGetDouble(out var a2) && Math.Abs(a2 - 5.0) < 0.001);
        // Group B: [10], [10+20], [20+30]
        Assert.True(result["group_rolling"][3].TryGetDouble(out var b0) && Math.Abs(b0 - 10.0) < 0.001);
        Assert.True(result["group_rolling"][4].TryGetDouble(out var b1) && Math.Abs(b1 - 30.0) < 0.001);
        Assert.True(result["group_rolling"][5].TryGetDouble(out var b2) && Math.Abs(b2 - 50.0) < 0.001);
    }

    [Fact]
    public void ExpandingSumExpr_WithOver_PartitionedExpanding()
    {
        var df = DataFrame(
            Series("group", new[] { "A", "A", "A", "B", "B", "B" }),
            Series("value", new[] { 1.0, 2.0, 3.0, 10.0, 20.0, 30.0 })
        );

        var result = df.Lazy()
            .WithColumns(
                Col("value").ExpandingSumExpr().Over(Col("group")).As("group_expanding")
            )
            .Collect();

        Assert.Equal(6, result.Height);
        // Group A cumulative sums: 1, 3, 6
        Assert.True(result["group_expanding"][0].TryGetDouble(out var a0) && Math.Abs(a0 - 1.0) < 0.001);
        Assert.True(result["group_expanding"][1].TryGetDouble(out var a1) && Math.Abs(a1 - 3.0) < 0.001);
        Assert.True(result["group_expanding"][2].TryGetDouble(out var a2) && Math.Abs(a2 - 6.0) < 0.001);
        // Group B cumulative sums: 10, 30, 60
        Assert.True(result["group_expanding"][3].TryGetDouble(out var b0) && Math.Abs(b0 - 10.0) < 0.001);
        Assert.True(result["group_expanding"][4].TryGetDouble(out var b1) && Math.Abs(b1 - 30.0) < 0.001);
        Assert.True(result["group_expanding"][5].TryGetDouble(out var b2) && Math.Abs(b2 - 60.0) < 0.001);
    }

    // ============================================================================
    // Edge Cases
    // ============================================================================

    [Fact]
    public void RollingSumExpr_EmptyDataFrame_ReturnsEmpty()
    {
        var df = DataFrame(
            Series("value", Array.Empty<double>())
        );

        var result = df.Lazy()
            .WithColumns(Col("value").RollingSumExpr(3).As("rolling_sum"))
            .Collect();

        Assert.Equal(0, result.Height);
    }

    [Fact]
    public void RollingSumExpr_SingleValue_ReturnsValue()
    {
        var df = DataFrame(
            Series("value", new[] { 42.0 })
        );

        var result = df.Lazy()
            .WithColumns(Col("value").RollingSumExpr(3).As("rolling_sum"))
            .Collect();

        Assert.Equal(1, result.Height);
        Assert.True(result["rolling_sum"][0].TryGetDouble(out var v) && Math.Abs(v - 42.0) < 0.001);
    }

    [Fact]
    public void RollingMeanExpr_WithCenter_CenteredWindow()
    {
        var df = DataFrame(
            Series("value", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 })
        );

        var result = df.Lazy()
            .WithColumns(Col("value").RollingMeanExpr(3, center: true).As("centered"))
            .Collect();

        Assert.Equal(5, result.Height);
        // Centered window means window is around the current position
        // Position 1: window includes [0,1,2] -> mean = 2
        Assert.True(result["centered"][1].TryGetDouble(out var v1) && Math.Abs(v1 - 2.0) < 0.001);
    }

    // ============================================================================
    // Multiple Rolling Operations
    // ============================================================================

    [Fact]
    public void MultipleRollingExpr_SameDataFrame_Works()
    {
        var df = DataFrame(
            Series("value", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 })
        );

        var result = df.Lazy()
            .WithColumns(
                Col("value").RollingSumExpr(3).As("rolling_sum"),
                Col("value").RollingMeanExpr(3).As("rolling_mean")
            )
            .Collect();

        Assert.Equal(5, result.Height);
        Assert.Contains("rolling_sum", result.Columns);
        Assert.Contains("rolling_mean", result.Columns);
    }
}
