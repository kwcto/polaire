// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using FluentAssertions;
using Polaire;
using Polaire.Compute;
using Xunit;

namespace Polaire.Tests;

public class EwmOperationsTests
{
    // ============================================================================
    // Alpha Calculation Tests
    // ============================================================================

    [Fact]
    public void AlphaFromSpan_CalculatesCorrectly()
    {
        // Alpha = 2 / (span + 1)
        EwmOperations.AlphaFromSpan(1).Should().BeApproximately(1.0, 0.0001);     // 2/2 = 1
        EwmOperations.AlphaFromSpan(2).Should().BeApproximately(2.0/3, 0.0001);   // 2/3
        EwmOperations.AlphaFromSpan(3).Should().BeApproximately(0.5, 0.0001);     // 2/4
        EwmOperations.AlphaFromSpan(9).Should().BeApproximately(0.2, 0.0001);     // 2/10
        EwmOperations.AlphaFromSpan(19).Should().BeApproximately(0.1, 0.0001);    // 2/20
    }

    [Fact]
    public void AlphaFromSpan_InvalidSpan_ThrowsException()
    {
        var act = () => EwmOperations.AlphaFromSpan(0.5);
        act.Should().Throw<ArgumentException>().WithMessage("*span must be >= 1*");
    }

    [Fact]
    public void AlphaFromHalflife_CalculatesCorrectly()
    {
        // Alpha = 1 - exp(-ln(2)/halflife)
        // For halflife=1: alpha = 1 - exp(-ln(2)) = 1 - 0.5 = 0.5
        EwmOperations.AlphaFromHalflife(1).Should().BeApproximately(0.5, 0.0001);

        // For halflife=2: alpha = 1 - exp(-ln(2)/2) ≈ 0.2929
        EwmOperations.AlphaFromHalflife(2).Should().BeApproximately(0.2929, 0.001);
    }

    [Fact]
    public void AlphaFromHalflife_InvalidHalflife_ThrowsException()
    {
        var act = () => EwmOperations.AlphaFromHalflife(0);
        act.Should().Throw<ArgumentException>().WithMessage("*halflife must be > 0*");

        var act2 = () => EwmOperations.AlphaFromHalflife(-1);
        act2.Should().Throw<ArgumentException>().WithMessage("*halflife must be > 0*");
    }

    [Fact]
    public void AlphaFromCom_CalculatesCorrectly()
    {
        // Alpha = 1 / (1 + com)
        EwmOperations.AlphaFromCom(0).Should().BeApproximately(1.0, 0.0001);      // 1/1
        EwmOperations.AlphaFromCom(1).Should().BeApproximately(0.5, 0.0001);      // 1/2
        EwmOperations.AlphaFromCom(2).Should().BeApproximately(1.0/3, 0.0001);    // 1/3
        EwmOperations.AlphaFromCom(9).Should().BeApproximately(0.1, 0.0001);      // 1/10
    }

    [Fact]
    public void AlphaFromCom_InvalidCom_ThrowsException()
    {
        var act = () => EwmOperations.AlphaFromCom(-1);
        act.Should().Throw<ArgumentException>().WithMessage("*com must be >= 0*");
    }

    // ============================================================================
    // EWM Mean Tests
    // ============================================================================

    [Fact]
    public void EwmMean_BasicSeries_CalculatesCorrectly()
    {
        var series = Series.FromValues("x", new double[] { 1, 2, 3, 4, 5 });
        var result = series.EwmMean(0.5, adjust: false);

        // Without adjustment:
        // t=0: EWMA = 1
        // t=1: EWMA = 0.5*2 + 0.5*1 = 1.5
        // t=2: EWMA = 0.5*3 + 0.5*1.5 = 2.25
        // t=3: EWMA = 0.5*4 + 0.5*2.25 = 3.125
        // t=4: EWMA = 0.5*5 + 0.5*3.125 = 4.0625
        result[0].AsFloat64().Should().BeApproximately(1.0, 0.001);
        result[1].AsFloat64().Should().BeApproximately(1.5, 0.001);
        result[2].AsFloat64().Should().BeApproximately(2.25, 0.001);
        result[3].AsFloat64().Should().BeApproximately(3.125, 0.001);
        result[4].AsFloat64().Should().BeApproximately(4.0625, 0.001);
    }

    [Fact]
    public void EwmMean_WithAdjust_CalculatesCorrectly()
    {
        var series = Series.FromValues("x", new double[] { 1, 2, 3 });
        var result = series.EwmMean(0.5, adjust: true);

        // With adjustment, early values are bias-corrected
        // First value should still be 1 (or close to it with adjustment)
        result[0].AsFloat64().Should().BeApproximately(1.0, 0.001);
        // Second value approaches 2 more (less weight on first value)
        result[1].AsFloat64().Should().BeApproximately(1.6667, 0.001);  // (0.5*2 + 0.5*1) / (0.5 + 0.5) with decay
    }

    [Fact]
    public void EwmMean_ConstantSeries_ReturnsConstant()
    {
        var series = Series.FromValues("x", new double[] { 5, 5, 5, 5 });
        var result = series.EwmMean(0.5);

        // EWMA of constant series should be the constant
        for (int i = 0; i < result.Length; i++)
        {
            result[i].AsFloat64().Should().BeApproximately(5.0, 0.001);
        }
    }

    [Fact]
    public void EwmMean_EmptySeries_ReturnsEmpty()
    {
        var series = Series.FromValues("x", Array.Empty<double>());
        var result = series.EwmMean(0.5);
        result.Length.Should().Be(0);
    }

    [Fact]
    public void EwmMean_SingleElement_ReturnsElement()
    {
        var series = Series.FromValues("x", new double[] { 42 });
        var result = series.EwmMean(0.5);
        result[0].AsFloat64().Should().BeApproximately(42.0, 0.001);
    }

    [Fact]
    public void EwmMean_InvalidAlpha_ThrowsException()
    {
        var series = Series.FromValues("x", new double[] { 1, 2, 3 });

        var act1 = () => series.EwmMean(0);
        act1.Should().Throw<ArgumentException>().WithMessage("*alpha must be in (0, 1]*");

        var act2 = () => series.EwmMean(1.5);
        act2.Should().Throw<ArgumentException>().WithMessage("*alpha must be in (0, 1]*");

        var act3 = () => series.EwmMean(-0.5);
        act3.Should().Throw<ArgumentException>().WithMessage("*alpha must be in (0, 1]*");
    }

    [Fact]
    public void EwmMean_AlphaOne_ReturnsOriginalValues()
    {
        var series = Series.FromValues("x", new double[] { 1, 2, 3 });
        var result = series.EwmMean(1.0, adjust: false);

        // Alpha=1 means only current value matters
        result[0].AsFloat64().Should().BeApproximately(1.0, 0.001);
        result[1].AsFloat64().Should().BeApproximately(2.0, 0.001);
        result[2].AsFloat64().Should().BeApproximately(3.0, 0.001);
    }

    [Fact]
    public void EwmMean_SmallAlpha_HeavierWeightOnHistory()
    {
        var series = Series.FromValues("x", new double[] { 10, 0, 0, 0 });
        var result = series.EwmMean(0.1, adjust: false);

        // Small alpha = slow decay, first value has lasting effect
        result[0].AsFloat64().Should().BeApproximately(10.0, 0.001);
        result[1].AsFloat64().Should().BeApproximately(9.0, 0.001);  // 0.1*0 + 0.9*10 = 9
        result[2].AsFloat64().Should().BeApproximately(8.1, 0.001);  // 0.1*0 + 0.9*9 = 8.1
        result[3].AsFloat64().Should().BeApproximately(7.29, 0.001); // 0.1*0 + 0.9*8.1 = 7.29
    }

    [Fact]
    public void EwmMean_WithNulls_IgnoreNullsTrue_SkipsNulls()
    {
        var series = Series.FromNullable("x", new double?[] { 1, null, 3 });
        var result = series.EwmMean(0.5, adjust: false, ignoreNulls: true);

        result[0].AsFloat64().Should().BeApproximately(1.0, 0.001);
        result[1].AsFloat64().Should().BeApproximately(1.0, 0.001);  // Keeps previous EWMA
        result[2].AsFloat64().Should().BeApproximately(2.0, 0.001);  // 0.5*3 + 0.5*1 = 2
    }

    [Fact]
    public void EwmMean_WithNulls_IgnoreNullsFalse_PropagatesNulls()
    {
        var series = Series.FromNullable("x", new double?[] { 1, null, 3 });
        var result = series.EwmMean(0.5, adjust: false, ignoreNulls: false);

        result[0].AsFloat64().Should().BeApproximately(1.0, 0.001);
        result.IsNull(1).Should().BeTrue();  // Null propagated
        // After null, we continue from where we left off
    }

    [Fact]
    public void EwmMean_MinPeriods_RespectsMinimum()
    {
        var series = Series.FromValues("x", new double[] { 1, 2, 3, 4 });
        var result = series.EwmMean(0.5, minPeriods: 3);

        result.IsNull(0).Should().BeTrue();  // Not enough periods
        result.IsNull(1).Should().BeTrue();  // Not enough periods
        result[2].AsFloat64().Should().BeGreaterThan(0);  // Now has 3 periods
        result[3].AsFloat64().Should().BeGreaterThan(0);
    }

    // ============================================================================
    // EWM Mean with Span/Halflife/Com Tests
    // ============================================================================

    [Fact]
    public void EwmMeanSpan_CalculatesCorrectly()
    {
        var series = Series.FromValues("x", new double[] { 1, 2, 3 });
        var result = series.EwmMeanSpan(3);  // alpha = 2/(3+1) = 0.5

        // Should be equivalent to alpha=0.5
        var resultAlpha = series.EwmMean(0.5);

        result[0].AsFloat64().Should().BeApproximately(resultAlpha[0].AsFloat64(), 0.001);
        result[1].AsFloat64().Should().BeApproximately(resultAlpha[1].AsFloat64(), 0.001);
        result[2].AsFloat64().Should().BeApproximately(resultAlpha[2].AsFloat64(), 0.001);
    }

    [Fact]
    public void EwmMeanHalflife_CalculatesCorrectly()
    {
        var series = Series.FromValues("x", new double[] { 1, 2, 3 });
        var result = series.EwmMeanHalflife(1);  // alpha = 0.5

        // Should be close to alpha=0.5
        result[0].AsFloat64().Should().BeGreaterThan(0);
        result[1].AsFloat64().Should().BeGreaterThan(result[0].AsFloat64());
    }

    [Fact]
    public void EwmMeanCom_CalculatesCorrectly()
    {
        var series = Series.FromValues("x", new double[] { 1, 2, 3 });
        var result = series.EwmMeanCom(1);  // alpha = 1/(1+1) = 0.5

        // Should be equivalent to alpha=0.5
        var resultAlpha = series.EwmMean(0.5);

        result[0].AsFloat64().Should().BeApproximately(resultAlpha[0].AsFloat64(), 0.001);
        result[1].AsFloat64().Should().BeApproximately(resultAlpha[1].AsFloat64(), 0.001);
        result[2].AsFloat64().Should().BeApproximately(resultAlpha[2].AsFloat64(), 0.001);
    }

    // ============================================================================
    // EWM Variance Tests
    // ============================================================================

    [Fact]
    public void EwmVar_BasicSeries_CalculatesCorrectly()
    {
        var series = Series.FromValues("x", new double[] { 1, 2, 3, 4, 5 });
        var result = series.EwmVar(0.5);

        // Variance should be non-negative
        // First value can't compute variance (needs 2 points)
        result.IsNull(0).Should().BeTrue();
        result[1].AsFloat64().Should().BeGreaterThanOrEqualTo(0);
        result[2].AsFloat64().Should().BeGreaterThanOrEqualTo(0);
        result[3].AsFloat64().Should().BeGreaterThanOrEqualTo(0);
        result[4].AsFloat64().Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void EwmVar_ConstantSeries_ReturnsZeroVariance()
    {
        var series = Series.FromValues("x", new double[] { 5, 5, 5, 5 });
        var result = series.EwmVar(0.5);

        // Constant series should have zero variance
        result.IsNull(0).Should().BeTrue();  // Need 2 for variance
        result[1].AsFloat64().Should().BeApproximately(0, 0.001);
        result[2].AsFloat64().Should().BeApproximately(0, 0.001);
        result[3].AsFloat64().Should().BeApproximately(0, 0.001);
    }

    [Fact]
    public void EwmVar_EmptySeries_ReturnsEmpty()
    {
        var series = Series.FromValues("x", Array.Empty<double>());
        var result = series.EwmVar(0.5);
        result.Length.Should().Be(0);
    }

    [Fact]
    public void EwmVar_SingleElement_ReturnsNull()
    {
        var series = Series.FromValues("x", new double[] { 42 });
        var result = series.EwmVar(0.5);
        result.IsNull(0).Should().BeTrue();  // Can't compute variance with 1 element
    }

    [Fact]
    public void EwmVar_MinPeriods_RespectsMinimum()
    {
        var series = Series.FromValues("x", new double[] { 1, 2, 3, 4, 5 });
        var result = series.EwmVar(0.5, minPeriods: 4);

        // With minPeriods=4 for variance:
        // Index 0: Only 1 value - null (variance needs 2+, and we need 4 periods)
        // Index 1: 2 values - null (need 4)
        // Index 2: 3 values - null (need 4)
        // Index 3: 4 values - now have enough periods
        // Index 4: 5 values - have enough
        result.IsNull(0).Should().BeTrue();
        result.IsNull(1).Should().BeTrue();
        result.IsNull(2).Should().BeTrue();
        result[3].AsFloat64().Should().BeGreaterThanOrEqualTo(0);  // Now has 4 periods
        result[4].AsFloat64().Should().BeGreaterThanOrEqualTo(0);
    }

    // ============================================================================
    // EWM Standard Deviation Tests
    // ============================================================================

    [Fact]
    public void EwmStd_BasicSeries_CalculatesCorrectly()
    {
        var series = Series.FromValues("x", new double[] { 1, 2, 3, 4, 5 });
        var result = series.EwmStd(0.5);
        var variance = series.EwmVar(0.5);

        // Std should be sqrt of variance
        result.IsNull(0).Should().BeTrue();
        for (int i = 1; i < result.Length; i++)
        {
            var expectedStd = Math.Sqrt(Math.Max(0, variance[i].AsFloat64()));
            result[i].AsFloat64().Should().BeApproximately(expectedStd, 0.001);
        }
    }

    [Fact]
    public void EwmStd_ConstantSeries_ReturnsZero()
    {
        var series = Series.FromValues("x", new double[] { 5, 5, 5, 5 });
        var result = series.EwmStd(0.5);

        result.IsNull(0).Should().BeTrue();
        result[1].AsFloat64().Should().BeApproximately(0, 0.001);
        result[2].AsFloat64().Should().BeApproximately(0, 0.001);
        result[3].AsFloat64().Should().BeApproximately(0, 0.001);
    }

    [Fact]
    public void EwmStdSpan_CalculatesCorrectly()
    {
        var series = Series.FromValues("x", new double[] { 1, 2, 3, 4 });
        var result = series.EwmStdSpan(3);  // alpha = 0.5

        // Should produce non-negative values (after first)
        result.IsNull(0).Should().BeTrue();
        result[1].AsFloat64().Should().BeGreaterThanOrEqualTo(0);
        result[2].AsFloat64().Should().BeGreaterThanOrEqualTo(0);
        result[3].AsFloat64().Should().BeGreaterThanOrEqualTo(0);
    }

    // ============================================================================
    // EWM Sum Tests
    // ============================================================================

    [Fact]
    public void EwmSum_BasicSeries_CalculatesCorrectly()
    {
        var series = Series.FromValues("x", new double[] { 1, 2, 3, 4 });
        var result = series.EwmSum(0.5);

        // EWM Sum: sum_t = x_t + (1-alpha) * sum_{t-1}
        // t=0: 1
        // t=1: 2 + 0.5*1 = 2.5
        // t=2: 3 + 0.5*2.5 = 4.25
        // t=3: 4 + 0.5*4.25 = 6.125
        result[0].AsFloat64().Should().BeApproximately(1.0, 0.001);
        result[1].AsFloat64().Should().BeApproximately(2.5, 0.001);
        result[2].AsFloat64().Should().BeApproximately(4.25, 0.001);
        result[3].AsFloat64().Should().BeApproximately(6.125, 0.001);
    }

    [Fact]
    public void EwmSum_AlphaOne_ReturnsCurrent()
    {
        var series = Series.FromValues("x", new double[] { 1, 2, 3, 4 });
        var result = series.EwmSum(1.0);

        // Alpha=1 means no decay, so each sum is just the current value
        result[0].AsFloat64().Should().BeApproximately(1.0, 0.001);
        result[1].AsFloat64().Should().BeApproximately(2.0, 0.001);
        result[2].AsFloat64().Should().BeApproximately(3.0, 0.001);
        result[3].AsFloat64().Should().BeApproximately(4.0, 0.001);
    }

    [Fact]
    public void EwmSum_SmallAlpha_AccumulatesMore()
    {
        var series = Series.FromValues("x", new double[] { 1, 1, 1, 1 });
        var result = series.EwmSum(0.1);

        // Small alpha = slow decay, values accumulate
        result[0].AsFloat64().Should().BeApproximately(1.0, 0.001);
        result[1].AsFloat64().Should().BeApproximately(1.9, 0.001);  // 1 + 0.9*1
        result[2].AsFloat64().Should().BeApproximately(2.71, 0.001); // 1 + 0.9*1.9
        result[3].AsFloat64().Should().BeApproximately(3.439, 0.001); // 1 + 0.9*2.71
    }

    [Fact]
    public void EwmSum_EmptySeries_ReturnsEmpty()
    {
        var series = Series.FromValues("x", Array.Empty<double>());
        var result = series.EwmSum(0.5);
        result.Length.Should().Be(0);
    }

    // ============================================================================
    // Edge Cases
    // ============================================================================

    [Fact]
    public void Ewm_PreservesSeriesName()
    {
        var series = Series.FromValues("my_column", new double[] { 1, 2, 3 });

        series.EwmMean(0.5).Name.Should().Be("my_column");
        series.EwmVar(0.5).Name.Should().Be("my_column");
        series.EwmStd(0.5).Name.Should().Be("my_column");
        series.EwmSum(0.5).Name.Should().Be("my_column");
    }

    [Fact]
    public void Ewm_IntegerSeries_WorksCorrectly()
    {
        var series = Series.FromValues("x", new int[] { 1, 2, 3, 4 });
        var result = series.EwmMean(0.5, adjust: false);

        result[0].AsFloat64().Should().BeApproximately(1.0, 0.001);
        result[1].AsFloat64().Should().BeApproximately(1.5, 0.001);
    }

    [Fact]
    public void Ewm_AllNulls_ReturnsAllNulls()
    {
        var series = Series.FromNullable("x", new double?[] { null, null, null });

        var mean = series.EwmMean(0.5);
        mean.IsNull(0).Should().BeTrue();
        mean.IsNull(1).Should().BeTrue();
        mean.IsNull(2).Should().BeTrue();
    }

    [Fact]
    public void EwmMean_TrendingSeries_TracksUpward()
    {
        // Verify EWMA tracks an upward trend but lags behind
        var series = Series.FromValues("x", new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
        var result = series.EwmMean(0.3, adjust: false);

        // EWMA should be increasing
        for (int i = 1; i < result.Length; i++)
        {
            result[i].AsFloat64().Should().BeGreaterThan(result[i-1].AsFloat64());
        }

        // EWMA should lag behind actual values (be less than current value after first)
        for (int i = 1; i < result.Length; i++)
        {
            result[i].AsFloat64().Should().BeLessThan(series[i].AsFloat64());
        }
    }

    [Fact]
    public void EwmVar_HighVarianceSeries_ShowsHighVariance()
    {
        // Series with large swings should have high variance
        var series = Series.FromValues("x", new double[] { 0, 100, 0, 100, 0, 100 });
        var result = series.EwmVar(0.5);

        // After the first few values, variance should be significant
        result[3].AsFloat64().Should().BeGreaterThan(100);  // High variance due to swings
    }

    [Fact]
    public void EwmVarSpan_EquivalentToAlpha()
    {
        var series = Series.FromValues("x", new double[] { 1, 2, 3, 4, 5 });

        // Span=3 gives alpha=0.5
        var resultSpan = series.EwmVarSpan(3);
        var resultAlpha = series.EwmVar(0.5);

        for (int i = 0; i < series.Length; i++)
        {
            if (resultSpan.IsNull(i))
            {
                resultAlpha.IsNull(i).Should().BeTrue();
            }
            else
            {
                resultSpan[i].AsFloat64().Should().BeApproximately(resultAlpha[i].AsFloat64(), 0.001);
            }
        }
    }
}
