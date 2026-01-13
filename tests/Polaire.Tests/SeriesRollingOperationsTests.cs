// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET
//
// Comprehensive tests for Series rolling operations, inspired by Polars test suite.
// These tests cover:
// - Rolling aggregations (Sum, Mean, Min, Max, Std, Var, Median, Quantile)
// - Expanding aggregations (Sum, Mean, Min, Max, Std)
// - EWM (Exponentially Weighted Moving) operations
// - RollingApply for custom functions

using FluentAssertions;
using Polaire;
using Polaire.DataTypes;
using Xunit;

namespace Polaire.Tests;

/// <summary>
/// Tests for Series rolling and expanding operations.
/// </summary>
public class SeriesRollingOperationsTests
{
    // ============================================================================
    // RollingSum Tests
    // ============================================================================

    [Fact]
    public void RollingSum_WindowSize3_ReturnsRollingSum()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 });
        // minPeriods=3 ensures we need a full window
        var result = series.RollingSum(3, minPeriods: 3);

        result.IsNull(0).Should().BeTrue();   // Not enough values
        result.IsNull(1).Should().BeTrue();   // Not enough values
        result[2].AsFloat64().Should().Be(6.0);   // 1+2+3
        result[3].AsFloat64().Should().Be(9.0);   // 2+3+4
        result[4].AsFloat64().Should().Be(12.0);  // 3+4+5
    }

    [Fact]
    public void RollingSum_MinPeriods1_ReturnsPartialSums()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0, 4.0 });
        var result = series.RollingSum(3, minPeriods: 1);

        result[0].AsFloat64().Should().Be(1.0);   // Just 1
        result[1].AsFloat64().Should().Be(3.0);   // 1+2
        result[2].AsFloat64().Should().Be(6.0);   // 1+2+3
        result[3].AsFloat64().Should().Be(9.0);   // 2+3+4
    }

    [Fact]
    public void RollingSum_WindowSize1_ReturnsSameValues()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0 });
        var result = series.RollingSum(1);

        result[0].AsFloat64().Should().Be(1.0);
        result[1].AsFloat64().Should().Be(2.0);
        result[2].AsFloat64().Should().Be(3.0);
    }

    // ============================================================================
    // RollingMean Tests
    // ============================================================================

    [Fact]
    public void RollingMean_WindowSize3_ReturnsRollingMean()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 });
        var result = series.RollingMean(3, minPeriods: 3);

        result.IsNull(0).Should().BeTrue();
        result.IsNull(1).Should().BeTrue();
        result[2].AsFloat64().Should().BeApproximately(2.0, 0.001);   // (1+2+3)/3
        result[3].AsFloat64().Should().BeApproximately(3.0, 0.001);   // (2+3+4)/3
        result[4].AsFloat64().Should().BeApproximately(4.0, 0.001);   // (3+4+5)/3
    }

    [Fact]
    public void RollingMean_MinPeriods1_ReturnsPartialMeans()
    {
        var series = Series.FromValues("s", new[] { 2.0, 4.0, 6.0, 8.0 });
        var result = series.RollingMean(3, minPeriods: 1);

        result[0].AsFloat64().Should().BeApproximately(2.0, 0.001);   // 2/1
        result[1].AsFloat64().Should().BeApproximately(3.0, 0.001);   // (2+4)/2
        result[2].AsFloat64().Should().BeApproximately(4.0, 0.001);   // (2+4+6)/3
        result[3].AsFloat64().Should().BeApproximately(6.0, 0.001);   // (4+6+8)/3
    }

    // ============================================================================
    // RollingMin Tests
    // ============================================================================

    [Fact]
    public void RollingMin_WindowSize3_ReturnsRollingMin()
    {
        var series = Series.FromValues("s", new[] { 5.0, 3.0, 7.0, 2.0, 8.0 });
        var result = series.RollingMin(3, minPeriods: 3);

        result.IsNull(0).Should().BeTrue();
        result.IsNull(1).Should().BeTrue();
        result[2].AsFloat64().Should().Be(3.0);   // min(5,3,7)
        result[3].AsFloat64().Should().Be(2.0);   // min(3,7,2)
        result[4].AsFloat64().Should().Be(2.0);   // min(7,2,8)
    }

    // ============================================================================
    // RollingMax Tests
    // ============================================================================

    [Fact]
    public void RollingMax_WindowSize3_ReturnsRollingMax()
    {
        var series = Series.FromValues("s", new[] { 1.0, 5.0, 3.0, 7.0, 2.0 });
        var result = series.RollingMax(3, minPeriods: 3);

        result.IsNull(0).Should().BeTrue();
        result.IsNull(1).Should().BeTrue();
        result[2].AsFloat64().Should().Be(5.0);   // max(1,5,3)
        result[3].AsFloat64().Should().Be(7.0);   // max(5,3,7)
        result[4].AsFloat64().Should().Be(7.0);   // max(3,7,2)
    }

    // ============================================================================
    // RollingStd Tests
    // ============================================================================

    [Fact]
    public void RollingStd_WindowSize3_ReturnsRollingStd()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 });
        var result = series.RollingStd(3, minPeriods: 3);

        result.IsNull(0).Should().BeTrue();
        result.IsNull(1).Should().BeTrue();
        // std of [1,2,3] = 1.0 (sample std)
        result[2].AsFloat64().Should().BeApproximately(1.0, 0.001);
    }

    [Fact]
    public void RollingStd_ConstantValues_ReturnsZero()
    {
        var series = Series.FromValues("s", new[] { 5.0, 5.0, 5.0, 5.0, 5.0 });
        var result = series.RollingStd(3, minPeriods: 3);

        result.IsNull(0).Should().BeTrue();
        result.IsNull(1).Should().BeTrue();
        result[2].AsFloat64().Should().BeApproximately(0.0, 0.001);
        result[3].AsFloat64().Should().BeApproximately(0.0, 0.001);
    }

    // ============================================================================
    // RollingVar Tests
    // ============================================================================

    [Fact]
    public void RollingVar_WindowSize3_ReturnsRollingVar()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 });
        var result = series.RollingVar(3, minPeriods: 3);

        result.IsNull(0).Should().BeTrue();
        result.IsNull(1).Should().BeTrue();
        // var of [1,2,3] = 1.0 (sample var)
        result[2].AsFloat64().Should().BeApproximately(1.0, 0.001);
    }

    // ============================================================================
    // RollingMedian Tests
    // ============================================================================

    [Fact]
    public void RollingMedian_WindowSize3_ReturnsRollingMedian()
    {
        var series = Series.FromValues("s", new[] { 1.0, 5.0, 3.0, 7.0, 2.0 });
        var result = series.RollingMedian(3, minPeriods: 3);

        result.IsNull(0).Should().BeTrue();
        result.IsNull(1).Should().BeTrue();
        result[2].AsFloat64().Should().Be(3.0);   // median(1,5,3)
        result[3].AsFloat64().Should().Be(5.0);   // median(5,3,7)
        result[4].AsFloat64().Should().Be(3.0);   // median(3,7,2)
    }

    // ============================================================================
    // RollingQuantile Tests
    // ============================================================================

    [Fact]
    public void RollingQuantile_50Percent_ReturnsRollingMedian()
    {
        var series = Series.FromValues("s", new[] { 1.0, 5.0, 3.0, 7.0 });
        var result = series.RollingQuantile(0.5, 3, minPeriods: 3);

        result.IsNull(0).Should().BeTrue();
        result.IsNull(1).Should().BeTrue();
        result[2].AsFloat64().Should().Be(3.0);   // median(1,5,3)
    }

    // ============================================================================
    // Center Tests
    // ============================================================================

    [Fact]
    public void RollingMean_Center_CentersWindow()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 });
        var result = series.RollingMean(3, minPeriods: 3, center: true);

        // With center=true, window is centered around each point
        result.IsNull(0).Should().BeTrue();
        result[1].AsFloat64().Should().BeApproximately(2.0, 0.001);  // mean(1,2,3)
        result[2].AsFloat64().Should().BeApproximately(3.0, 0.001);  // mean(2,3,4)
        result[3].AsFloat64().Should().BeApproximately(4.0, 0.001);  // mean(3,4,5)
        result.IsNull(4).Should().BeTrue();
    }

    // ============================================================================
    // ExpandingSum Tests
    // ============================================================================

    [Fact]
    public void ExpandingSum_ReturnsExpandingSum()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 });
        var result = series.ExpandingSum();

        result[0].AsFloat64().Should().Be(1.0);
        result[1].AsFloat64().Should().Be(3.0);   // 1+2
        result[2].AsFloat64().Should().Be(6.0);   // 1+2+3
        result[3].AsFloat64().Should().Be(10.0);  // 1+2+3+4
        result[4].AsFloat64().Should().Be(15.0);  // 1+2+3+4+5
    }

    [Fact]
    public void ExpandingSum_MinPeriods2_StartsLater()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0, 4.0 });
        var result = series.ExpandingSum(minPeriods: 2);

        result.IsNull(0).Should().BeTrue();
        result[1].AsFloat64().Should().Be(3.0);   // 1+2
        result[2].AsFloat64().Should().Be(6.0);   // 1+2+3
        result[3].AsFloat64().Should().Be(10.0);  // 1+2+3+4
    }

    // ============================================================================
    // ExpandingMean Tests
    // ============================================================================

    [Fact]
    public void ExpandingMean_ReturnsExpandingMean()
    {
        var series = Series.FromValues("s", new[] { 2.0, 4.0, 6.0, 8.0 });
        var result = series.ExpandingMean();

        result[0].AsFloat64().Should().BeApproximately(2.0, 0.001);   // 2/1
        result[1].AsFloat64().Should().BeApproximately(3.0, 0.001);   // (2+4)/2
        result[2].AsFloat64().Should().BeApproximately(4.0, 0.001);   // (2+4+6)/3
        result[3].AsFloat64().Should().BeApproximately(5.0, 0.001);   // (2+4+6+8)/4
    }

    // ============================================================================
    // ExpandingMin Tests
    // ============================================================================

    [Fact]
    public void ExpandingMin_ReturnsExpandingMin()
    {
        var series = Series.FromValues("s", new[] { 5.0, 3.0, 7.0, 1.0, 4.0 });
        var result = series.ExpandingMin();

        result[0].AsFloat64().Should().Be(5.0);
        result[1].AsFloat64().Should().Be(3.0);   // min(5,3)
        result[2].AsFloat64().Should().Be(3.0);   // min(5,3,7)
        result[3].AsFloat64().Should().Be(1.0);   // min(5,3,7,1)
        result[4].AsFloat64().Should().Be(1.0);   // min(5,3,7,1,4)
    }

    // ============================================================================
    // ExpandingMax Tests
    // ============================================================================

    [Fact]
    public void ExpandingMax_ReturnsExpandingMax()
    {
        var series = Series.FromValues("s", new[] { 1.0, 5.0, 3.0, 7.0, 2.0 });
        var result = series.ExpandingMax();

        result[0].AsFloat64().Should().Be(1.0);
        result[1].AsFloat64().Should().Be(5.0);   // max(1,5)
        result[2].AsFloat64().Should().Be(5.0);   // max(1,5,3)
        result[3].AsFloat64().Should().Be(7.0);   // max(1,5,3,7)
        result[4].AsFloat64().Should().Be(7.0);   // max(1,5,3,7,2)
    }

    // ============================================================================
    // ExpandingStd Tests
    // ============================================================================

    [Fact]
    public void ExpandingStd_ReturnsExpandingStd()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 });
        var result = series.ExpandingStd();

        result.IsNull(0).Should().BeTrue();  // Need at least 2 for std
        result[1].AsFloat64().Should().BeApproximately(0.7071, 0.001);  // std(1,2)
        // Std increases as we add more spread values
        result[4].AsFloat64().Should().BeApproximately(1.5811, 0.001);  // std(1,2,3,4,5)
    }

    // ============================================================================
    // EWM Mean Tests
    // ============================================================================

    [Fact]
    public void EwmMean_Alpha_ReturnsEwmMean()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 });
        var result = series.EwmMean(0.5);

        // EWM with alpha=0.5 gives more weight to recent values
        result[0].AsFloat64().Should().BeApproximately(1.0, 0.001);
        // Values should trend toward the more recent values
        result[4].AsFloat64().Should().BeGreaterThan(result[0].AsFloat64());
    }

    [Fact]
    public void EwmMeanSpan_Span2_ReturnsEwmMean()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0, 4.0 });
        var result = series.EwmMeanSpan(2.0);

        // span=2 means alpha = 2/(2+1) = 0.667
        result[0].AsFloat64().Should().Be(1.0);
        result.Length.Should().Be(4);
    }

    // ============================================================================
    // EWM Var/Std Tests
    // ============================================================================

    [Fact]
    public void EwmVar_ReturnsEwmVar()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 });
        var result = series.EwmVar(0.5);

        // First value should be null or 0 (no variance from single point)
        result.Length.Should().Be(5);
    }

    [Fact]
    public void EwmStd_ReturnsEwmStd()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 });
        var result = series.EwmStd(0.5);

        result.Length.Should().Be(5);
    }

    // ============================================================================
    // EWM Sum Tests
    // ============================================================================

    [Fact]
    public void EwmSum_ReturnsEwmSum()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0, 4.0 });
        var result = series.EwmSum(0.5);

        result[0].AsFloat64().Should().BeApproximately(1.0, 0.001);
        // EWM sum accumulates with exponential decay
        result.Length.Should().Be(4);
    }

    // ============================================================================
    // RollingApply Tests
    // ============================================================================

    [Fact]
    public void RollingApply_CustomSum_ReturnsCustomResult()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 });
        var result = series.RollingApply(3, window => window.Sum(), minPeriods: 3);

        result.IsNull(0).Should().BeTrue();
        result.IsNull(1).Should().BeTrue();
        result[2].AsFloat64().Should().Be(6.0);   // 1+2+3
        result[3].AsFloat64().Should().Be(9.0);   // 2+3+4
        result[4].AsFloat64().Should().Be(12.0);  // 3+4+5
    }

    [Fact]
    public void RollingApply_CustomMax_ReturnsCustomResult()
    {
        var series = Series.FromValues("s", new[] { 3.0, 1.0, 4.0, 1.0, 5.0 });
        var result = series.RollingApply(3, window => window.Max(), minPeriods: 3);

        result.IsNull(0).Should().BeTrue();
        result.IsNull(1).Should().BeTrue();
        result[2].AsFloat64().Should().Be(4.0);   // max(3,1,4)
        result[3].AsFloat64().Should().Be(4.0);   // max(1,4,1)
        result[4].AsFloat64().Should().Be(5.0);   // max(4,1,5)
    }

    [Fact]
    public void RollingApply_CustomRange_ReturnsCustomResult()
    {
        var series = Series.FromValues("s", new[] { 1.0, 5.0, 2.0, 8.0, 3.0 });
        var result = series.RollingApply(3, window => window.Max() - window.Min(), minPeriods: 3);

        result.IsNull(0).Should().BeTrue();
        result.IsNull(1).Should().BeTrue();
        result[2].AsFloat64().Should().Be(4.0);   // max-min of (1,5,2) = 5-1 = 4
        result[3].AsFloat64().Should().Be(6.0);   // max-min of (5,2,8) = 8-2 = 6
        result[4].AsFloat64().Should().Be(6.0);   // max-min of (2,8,3) = 8-2 = 6
    }

    // ============================================================================
    // Edge Cases
    // ============================================================================

    [Fact]
    public void RollingSum_EmptySeries_ReturnsEmpty()
    {
        var series = Series.FromValues("s", Array.Empty<double>());
        var result = series.RollingSum(3);

        result.Length.Should().Be(0);
    }

    [Fact]
    public void RollingSum_WindowLargerThanSeries_AllNull()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0 });
        var result = series.RollingSum(5, minPeriods: 5);

        result.IsNull(0).Should().BeTrue();
        result.IsNull(1).Should().BeTrue();
    }

    [Fact]
    public void ExpandingSum_SingleElement_ReturnsThatElement()
    {
        var series = Series.FromValues("s", new[] { 42.0 });
        var result = series.ExpandingSum();

        result[0].AsFloat64().Should().Be(42.0);
    }
}
