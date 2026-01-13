// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET
// Test suite ported from Polars (py-polars/tests/unit/operations/rolling/)

using FluentAssertions;
using Xunit;

namespace Polaire.Tests;

/// <summary>
/// Comprehensive tests for rolling and expanding window operations.
/// Ported from Polars test suite with additional edge case coverage.
/// </summary>
public class RollingTests
{
    // ============================================================================
    // Rolling Sum Tests
    // ============================================================================

    [Fact]
    public void RollingSum_BasicWindow_CorrectResults()
    {
        var s = Series.FromValues("x", new double[] { 1, 2, 3, 4, 5 });
        var result = s.RollingSum(windowSize: 3);

        // Window 3 with minPeriods=1: [1, 3, 6, 9, 12]
        result.Length.Should().Be(5);
        result[0].AsFloat64().Should().Be(1.0);   // Just 1
        result[1].AsFloat64().Should().Be(3.0);   // 1+2
        result[2].AsFloat64().Should().Be(6.0);   // 1+2+3
        result[3].AsFloat64().Should().Be(9.0);   // 2+3+4
        result[4].AsFloat64().Should().Be(12.0);  // 3+4+5
    }

    [Fact]
    public void RollingSum_MinPeriods_RespectsThreshold()
    {
        var s = Series.FromValues("x", new double[] { 1, 2, 3, 4, 5 });
        var result = s.RollingSum(windowSize: 3, minPeriods: 3);

        // With minPeriods=3: [null, null, 6, 9, 12]
        result.IsNull(0).Should().BeTrue();
        result.IsNull(1).Should().BeTrue();
        result[2].AsFloat64().Should().Be(6.0);
        result[3].AsFloat64().Should().Be(9.0);
        result[4].AsFloat64().Should().Be(12.0);
    }

    [Fact]
    public void RollingSum_Center_CentersWindow()
    {
        var s = Series.FromValues("x", new double[] { 1, 2, 3, 4, 5 });
        var result = s.RollingSum(windowSize: 3, center: true);

        // Center=true, window 3: centers on each element
        // i=0: window [-1, 0, 1] clamped to [0, 1] → 1+2 = 3
        // i=1: window [0, 1, 2] → 1+2+3 = 6
        // i=2: window [1, 2, 3] → 2+3+4 = 9
        // i=3: window [2, 3, 4] → 3+4+5 = 12
        // i=4: window [3, 4, 5] clamped to [3, 4] → 4+5 = 9
        result[0].AsFloat64().Should().Be(3.0);
        result[1].AsFloat64().Should().Be(6.0);
        result[2].AsFloat64().Should().Be(9.0);
        result[3].AsFloat64().Should().Be(12.0);
        result[4].AsFloat64().Should().Be(9.0);
    }

    [Fact]
    public void RollingSum_WithNulls_SkipsNulls()
    {
        var s = Series.FromNullable("x", new double?[] { 1, null, 3, 4, 5 });
        var result = s.RollingSum(windowSize: 3, minPeriods: 1);

        // Values: [1, null, 3, 4, 5]
        // i=0: [1] → 1
        // i=1: [1, null] → 1
        // i=2: [1, null, 3] → 4
        // i=3: [null, 3, 4] → 7
        // i=4: [3, 4, 5] → 12
        result[0].AsFloat64().Should().Be(1.0);
        result[1].AsFloat64().Should().Be(1.0);
        result[2].AsFloat64().Should().Be(4.0);
        result[3].AsFloat64().Should().Be(7.0);
        result[4].AsFloat64().Should().Be(12.0);
    }

    [Fact]
    public void RollingSum_WindowSizeOne_EqualsOriginal()
    {
        var s = Series.FromValues("x", new double[] { 1, 2, 3, 4, 5 });
        var result = s.RollingSum(windowSize: 1);

        for (int i = 0; i < 5; i++)
        {
            result[i].AsFloat64().Should().Be(s[i].AsFloat64());
        }
    }

    [Fact]
    public void RollingSum_WindowSizeLargerThanData_SumsAll()
    {
        var s = Series.FromValues("x", new double[] { 1, 2, 3 });
        var result = s.RollingSum(windowSize: 10, minPeriods: 1);

        result[0].AsFloat64().Should().Be(1.0);
        result[1].AsFloat64().Should().Be(3.0);
        result[2].AsFloat64().Should().Be(6.0);
    }

    [Fact]
    public void RollingSum_IntegerInput_Works()
    {
        var s = Series.FromValues("x", new int[] { 1, 2, 3, 4, 5 });
        var result = s.RollingSum(windowSize: 3);

        result.DataType.Should().Be(Polaire.DataTypes.DataType.Float64);
        result[2].AsFloat64().Should().Be(6.0);
    }

    // ============================================================================
    // Rolling Mean Tests
    // ============================================================================

    [Fact]
    public void RollingMean_BasicWindow_CorrectResults()
    {
        var s = Series.FromValues("x", new double[] { 1, 2, 3, 4, 5 });
        var result = s.RollingMean(windowSize: 3);

        result[0].AsFloat64().Should().Be(1.0);        // 1/1
        result[1].AsFloat64().Should().Be(1.5);        // (1+2)/2
        result[2].AsFloat64().Should().Be(2.0);        // (1+2+3)/3
        result[3].AsFloat64().Should().Be(3.0);        // (2+3+4)/3
        result[4].AsFloat64().Should().Be(4.0);        // (3+4+5)/3
    }

    [Fact]
    public void RollingMean_MinPeriods_RespectsThreshold()
    {
        var s = Series.FromValues("x", new double[] { 1, 2, 3, 4, 5 });
        var result = s.RollingMean(windowSize: 3, minPeriods: 3);

        result.IsNull(0).Should().BeTrue();
        result.IsNull(1).Should().BeTrue();
        result[2].AsFloat64().Should().Be(2.0);
    }

    [Fact]
    public void RollingMean_WithNulls_SkipsNulls()
    {
        var s = Series.FromNullable("x", new double?[] { 1, null, 3, 4, 5 });
        var result = s.RollingMean(windowSize: 3, minPeriods: 1);

        result[0].AsFloat64().Should().Be(1.0);        // 1/1
        result[1].AsFloat64().Should().Be(1.0);        // 1/1 (null skipped)
        result[2].AsFloat64().Should().Be(2.0);        // (1+3)/2
        result[3].AsFloat64().Should().Be(3.5);        // (3+4)/2
        result[4].AsFloat64().Should().Be(4.0);        // (3+4+5)/3
    }

    [Fact]
    public void RollingMean_AllSameValues_ReturnsSameValue()
    {
        var s = Series.FromValues("x", new double[] { 5, 5, 5, 5, 5 });
        var result = s.RollingMean(windowSize: 3);

        for (int i = 0; i < 5; i++)
        {
            result[i].AsFloat64().Should().Be(5.0);
        }
    }

    // ============================================================================
    // Rolling Min Tests
    // ============================================================================

    [Fact]
    public void RollingMin_BasicWindow_CorrectResults()
    {
        var s = Series.FromValues("x", new double[] { 3, 1, 4, 1, 5 });
        var result = s.RollingMin(windowSize: 3);

        result[0].AsFloat64().Should().Be(3.0);        // min(3)
        result[1].AsFloat64().Should().Be(1.0);        // min(3,1)
        result[2].AsFloat64().Should().Be(1.0);        // min(3,1,4)
        result[3].AsFloat64().Should().Be(1.0);        // min(1,4,1)
        result[4].AsFloat64().Should().Be(1.0);        // min(4,1,5)
    }

    [Fact]
    public void RollingMin_Descending_FindsMinimum()
    {
        var s = Series.FromValues("x", new double[] { 5, 4, 3, 2, 1 });
        var result = s.RollingMin(windowSize: 3);

        result[0].AsFloat64().Should().Be(5.0);
        result[1].AsFloat64().Should().Be(4.0);
        result[2].AsFloat64().Should().Be(3.0);
        result[3].AsFloat64().Should().Be(2.0);
        result[4].AsFloat64().Should().Be(1.0);
    }

    [Fact]
    public void RollingMin_WithNegatives_HandlesNegatives()
    {
        var s = Series.FromValues("x", new double[] { -1, -5, 3, -2, 4 });
        var result = s.RollingMin(windowSize: 3);

        result[0].AsFloat64().Should().Be(-1.0);
        result[1].AsFloat64().Should().Be(-5.0);
        result[2].AsFloat64().Should().Be(-5.0);
        result[3].AsFloat64().Should().Be(-5.0);
        result[4].AsFloat64().Should().Be(-2.0);
    }

    // ============================================================================
    // Rolling Max Tests
    // ============================================================================

    [Fact]
    public void RollingMax_BasicWindow_CorrectResults()
    {
        var s = Series.FromValues("x", new double[] { 3, 1, 4, 1, 5 });
        var result = s.RollingMax(windowSize: 3);

        result[0].AsFloat64().Should().Be(3.0);        // max(3)
        result[1].AsFloat64().Should().Be(3.0);        // max(3,1)
        result[2].AsFloat64().Should().Be(4.0);        // max(3,1,4)
        result[3].AsFloat64().Should().Be(4.0);        // max(1,4,1)
        result[4].AsFloat64().Should().Be(5.0);        // max(4,1,5)
    }

    [Fact]
    public void RollingMax_Ascending_FindsMaximum()
    {
        var s = Series.FromValues("x", new double[] { 1, 2, 3, 4, 5 });
        var result = s.RollingMax(windowSize: 3);

        result[0].AsFloat64().Should().Be(1.0);
        result[1].AsFloat64().Should().Be(2.0);
        result[2].AsFloat64().Should().Be(3.0);
        result[3].AsFloat64().Should().Be(4.0);
        result[4].AsFloat64().Should().Be(5.0);
    }

    // ============================================================================
    // Rolling Std Tests
    // ============================================================================

    [Fact]
    public void RollingStd_BasicWindow_CorrectResults()
    {
        var s = Series.FromValues("x", new double[] { 1, 2, 3, 4, 5 });
        var result = s.RollingStd(windowSize: 3);

        // With ddof=1 (sample std):
        // i=2: std([1,2,3]) = 1.0
        // i=3: std([2,3,4]) = 1.0
        // i=4: std([3,4,5]) = 1.0
        result.IsNull(0).Should().BeTrue();  // Only 1 value, ddof=1 makes this undefined
        result[2].AsFloat64().Should().Be(1.0);
        result[3].AsFloat64().Should().Be(1.0);
        result[4].AsFloat64().Should().Be(1.0);
    }

    [Fact]
    public void RollingStd_Ddof0_PopulationStd()
    {
        var s = Series.FromValues("x", new double[] { 1, 2, 3, 4, 5 });
        var result = s.RollingStd(windowSize: 3, ddof: 0);

        // Population std of [1,2,3]: sqrt(((1-2)^2 + (2-2)^2 + (3-2)^2)/3) = sqrt(2/3) ≈ 0.8165
        var expectedStd = Math.Sqrt(2.0 / 3.0);
        result[2].AsFloat64().Should().BeApproximately(expectedStd, 0.0001);
    }

    [Fact]
    public void RollingStd_ConstantValues_ZeroStd()
    {
        var s = Series.FromValues("x", new double[] { 5, 5, 5, 5, 5 });
        var result = s.RollingStd(windowSize: 3);

        result[2].AsFloat64().Should().Be(0.0);
        result[3].AsFloat64().Should().Be(0.0);
        result[4].AsFloat64().Should().Be(0.0);
    }

    // ============================================================================
    // Rolling Var Tests
    // ============================================================================

    [Fact]
    public void RollingVar_BasicWindow_CorrectResults()
    {
        var s = Series.FromValues("x", new double[] { 1, 2, 3, 4, 5 });
        var result = s.RollingVar(windowSize: 3);

        // Sample variance of [1,2,3] = 1.0
        result[2].AsFloat64().Should().Be(1.0);
        result[3].AsFloat64().Should().Be(1.0);
        result[4].AsFloat64().Should().Be(1.0);
    }

    [Fact]
    public void RollingVar_StdSquared_Equals()
    {
        var s = Series.FromValues("x", new double[] { 1, 3, 5, 7, 9 });
        var stdResult = s.RollingStd(windowSize: 3);
        var varResult = s.RollingVar(windowSize: 3);

        for (int i = 2; i < 5; i++)
        {
            var std = stdResult[i].AsFloat64();
            var variance = varResult[i].AsFloat64();
            (std * std).Should().BeApproximately(variance, 0.0001);
        }
    }

    // ============================================================================
    // Rolling Median Tests
    // ============================================================================

    [Fact]
    public void RollingMedian_OddWindow_CenterValue()
    {
        var s = Series.FromValues("x", new double[] { 1, 2, 3, 4, 5 });
        var result = s.RollingMedian(windowSize: 3);

        // median([1]) = 1
        // median([1,2]) = 1.5
        // median([1,2,3]) = 2
        // median([2,3,4]) = 3
        // median([3,4,5]) = 4
        result[0].AsFloat64().Should().Be(1.0);
        result[1].AsFloat64().Should().Be(1.5);
        result[2].AsFloat64().Should().Be(2.0);
        result[3].AsFloat64().Should().Be(3.0);
        result[4].AsFloat64().Should().Be(4.0);
    }

    [Fact]
    public void RollingMedian_EvenWindow_AverageCenterValues()
    {
        var s = Series.FromValues("x", new double[] { 1, 2, 3, 4, 5, 6 });
        var result = s.RollingMedian(windowSize: 4, minPeriods: 4);

        // median([1,2,3,4]) = (2+3)/2 = 2.5
        // median([2,3,4,5]) = (3+4)/2 = 3.5
        // median([3,4,5,6]) = (4+5)/2 = 4.5
        result.IsNull(0).Should().BeTrue();
        result.IsNull(1).Should().BeTrue();
        result.IsNull(2).Should().BeTrue();
        result[3].AsFloat64().Should().Be(2.5);
        result[4].AsFloat64().Should().Be(3.5);
        result[5].AsFloat64().Should().Be(4.5);
    }

    [Fact]
    public void RollingMedian_UnsortedData_CorrectMedian()
    {
        var s = Series.FromValues("x", new double[] { 5, 1, 3, 2, 4 });
        var result = s.RollingMedian(windowSize: 3);

        // median([5]) = 5
        // median([5,1]) = 3
        // median([5,1,3]) = 3
        // median([1,3,2]) = 2
        // median([3,2,4]) = 3
        result[0].AsFloat64().Should().Be(5.0);
        result[1].AsFloat64().Should().Be(3.0);
        result[2].AsFloat64().Should().Be(3.0);
        result[3].AsFloat64().Should().Be(2.0);
        result[4].AsFloat64().Should().Be(3.0);
    }

    // ============================================================================
    // Rolling Quantile Tests
    // ============================================================================

    [Fact]
    public void RollingQuantile_Q50_EqualsMedian()
    {
        var s = Series.FromValues("x", new double[] { 1, 2, 3, 4, 5 });
        var median = s.RollingMedian(windowSize: 3);
        var q50 = s.RollingQuantile(0.5, windowSize: 3);

        for (int i = 0; i < 5; i++)
        {
            q50[i].AsFloat64().Should().Be(median[i].AsFloat64());
        }
    }

    [Fact]
    public void RollingQuantile_Q0_EqualsMin()
    {
        var s = Series.FromValues("x", new double[] { 3, 1, 4, 1, 5 });
        var min = s.RollingMin(windowSize: 3);
        var q0 = s.RollingQuantile(0.0, windowSize: 3);

        for (int i = 0; i < 5; i++)
        {
            q0[i].AsFloat64().Should().Be(min[i].AsFloat64());
        }
    }

    [Fact]
    public void RollingQuantile_Q100_EqualsMax()
    {
        var s = Series.FromValues("x", new double[] { 3, 1, 4, 1, 5 });
        var max = s.RollingMax(windowSize: 3);
        var q100 = s.RollingQuantile(1.0, windowSize: 3);

        for (int i = 0; i < 5; i++)
        {
            q100[i].AsFloat64().Should().Be(max[i].AsFloat64());
        }
    }

    [Fact]
    public void RollingQuantile_Q25_CorrectInterpolation()
    {
        var s = Series.FromValues("x", new double[] { 0, 1, 2, 3, 4 });
        var result = s.RollingQuantile(0.25, windowSize: 5, minPeriods: 5);

        // For [0,1,2,3,4], Q25 position = 0.25 * 4 = 1.0 → value at index 1 = 1.0
        result[4].AsFloat64().Should().Be(1.0);
    }

    [Fact]
    public void RollingQuantile_Interpolation_Lower()
    {
        var s = Series.FromValues("x", new double[] { 0, 1, 2, 3, 4 });
        var result = s.RollingQuantile(0.3, windowSize: 5, minPeriods: 5, interpolation: "lower");

        // Q30 position = 0.3 * 4 = 1.2 → lower = 1
        result[4].AsFloat64().Should().Be(1.0);
    }

    [Fact]
    public void RollingQuantile_Interpolation_Higher()
    {
        var s = Series.FromValues("x", new double[] { 0, 1, 2, 3, 4 });
        var result = s.RollingQuantile(0.3, windowSize: 5, minPeriods: 5, interpolation: "higher");

        // Q30 position = 0.3 * 4 = 1.2 → higher = 2
        result[4].AsFloat64().Should().Be(2.0);
    }

    [Fact]
    public void RollingQuantile_InvalidQuantile_Throws()
    {
        var s = Series.FromValues("x", new double[] { 1, 2, 3 });

        var act1 = () => s.RollingQuantile(-0.1, windowSize: 2);
        var act2 = () => s.RollingQuantile(1.1, windowSize: 2);

        act1.Should().Throw<ArgumentOutOfRangeException>();
        act2.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ============================================================================
    // Rolling Apply Tests
    // ============================================================================

    [Fact]
    public void RollingApply_CustomSum_MatchesBuiltIn()
    {
        var s = Series.FromValues("x", new double[] { 1, 2, 3, 4, 5 });

        Func<double[], double?> customSum = arr => arr.Sum();
        var custom = s.RollingApply(windowSize: 3, customSum);
        var builtin = s.RollingSum(windowSize: 3);

        for (int i = 0; i < 5; i++)
        {
            custom[i].AsFloat64().Should().Be(builtin[i].AsFloat64());
        }
    }

    [Fact]
    public void RollingApply_Range_MaxMinusMin()
    {
        var s = Series.FromValues("x", new double[] { 3, 1, 4, 1, 5 });

        Func<double[], double?> range = arr => arr.Max() - arr.Min();
        var result = s.RollingApply(windowSize: 3, range);

        result[0].AsFloat64().Should().Be(0.0);        // range(3) = 0
        result[1].AsFloat64().Should().Be(2.0);        // range(3,1) = 2
        result[2].AsFloat64().Should().Be(3.0);        // range(3,1,4) = 3
        result[3].AsFloat64().Should().Be(3.0);        // range(1,4,1) = 3
        result[4].AsFloat64().Should().Be(4.0);        // range(4,1,5) = 4
    }

    [Fact]
    public void RollingApply_ReturnsNull_PropagatesNull()
    {
        var s = Series.FromValues("x", new double[] { 1, 2, 3, 4, 5 });

        Func<double[], double?> nullOnSmall = arr => arr.Length < 3 ? null : arr.Sum();
        var result = s.RollingApply(windowSize: 3, nullOnSmall);

        result.IsNull(0).Should().BeTrue();
        result.IsNull(1).Should().BeTrue();
        result[2].AsFloat64().Should().Be(6.0);
    }

    [Fact]
    public void RollingApply_GeometricMean_CustomFunc()
    {
        var s = Series.FromValues("x", new double[] { 1, 2, 4, 8, 16 });

        Func<double[], double?> geoMean = arr =>
        {
            double product = 1.0;
            foreach (var v in arr) product *= v;
            return Math.Pow(product, 1.0 / arr.Length);
        };

        var result = s.RollingApply(windowSize: 3, geoMean);

        // geoMean([1,2,4]) = (8)^(1/3) = 2
        // geoMean([2,4,8]) = (64)^(1/3) = 4
        // geoMean([4,8,16]) = (512)^(1/3) = 8
        result[2].AsFloat64().Should().BeApproximately(2.0, 0.0001);
        result[3].AsFloat64().Should().BeApproximately(4.0, 0.0001);
        result[4].AsFloat64().Should().BeApproximately(8.0, 0.0001);
    }

    // ============================================================================
    // Expanding Window Tests
    // ============================================================================

    [Fact]
    public void ExpandingSum_BasicData_CumulativeSum()
    {
        var s = Series.FromValues("x", new double[] { 1, 2, 3, 4, 5 });
        var result = s.ExpandingSum();

        result[0].AsFloat64().Should().Be(1.0);
        result[1].AsFloat64().Should().Be(3.0);
        result[2].AsFloat64().Should().Be(6.0);
        result[3].AsFloat64().Should().Be(10.0);
        result[4].AsFloat64().Should().Be(15.0);
    }

    [Fact]
    public void ExpandingMean_BasicData_CumulativeMean()
    {
        var s = Series.FromValues("x", new double[] { 1, 2, 3, 4, 5 });
        var result = s.ExpandingMean();

        result[0].AsFloat64().Should().Be(1.0);
        result[1].AsFloat64().Should().Be(1.5);
        result[2].AsFloat64().Should().Be(2.0);
        result[3].AsFloat64().Should().Be(2.5);
        result[4].AsFloat64().Should().Be(3.0);
    }

    [Fact]
    public void ExpandingMin_BasicData_CumulativeMin()
    {
        var s = Series.FromValues("x", new double[] { 5, 3, 4, 1, 2 });
        var result = s.ExpandingMin();

        result[0].AsFloat64().Should().Be(5.0);
        result[1].AsFloat64().Should().Be(3.0);
        result[2].AsFloat64().Should().Be(3.0);
        result[3].AsFloat64().Should().Be(1.0);
        result[4].AsFloat64().Should().Be(1.0);
    }

    [Fact]
    public void ExpandingMax_BasicData_CumulativeMax()
    {
        var s = Series.FromValues("x", new double[] { 1, 3, 2, 5, 4 });
        var result = s.ExpandingMax();

        result[0].AsFloat64().Should().Be(1.0);
        result[1].AsFloat64().Should().Be(3.0);
        result[2].AsFloat64().Should().Be(3.0);
        result[3].AsFloat64().Should().Be(5.0);
        result[4].AsFloat64().Should().Be(5.0);
    }

    [Fact]
    public void ExpandingStd_BasicData_CumulativeStd()
    {
        var s = Series.FromValues("x", new double[] { 1, 2, 3, 4, 5 });
        var result = s.ExpandingStd();

        result.IsNull(0).Should().BeTrue();  // Can't compute std with 1 value and ddof=1
        result[1].AsFloat64().Should().BeApproximately(Math.Sqrt(0.5), 0.0001);  // std([1,2])

        // std([1,2,3]) = 1.0
        result[2].AsFloat64().Should().BeApproximately(1.0, 0.0001);
    }

    [Fact]
    public void ExpandingSum_WithMinPeriods_RespectsThreshold()
    {
        var s = Series.FromValues("x", new double[] { 1, 2, 3, 4, 5 });
        var result = s.ExpandingSum(minPeriods: 3);

        result.IsNull(0).Should().BeTrue();
        result.IsNull(1).Should().BeTrue();
        result[2].AsFloat64().Should().Be(6.0);
        result[3].AsFloat64().Should().Be(10.0);
        result[4].AsFloat64().Should().Be(15.0);
    }

    // ============================================================================
    // Edge Cases
    // ============================================================================

    [Fact]
    public void Rolling_EmptySeries_ReturnsEmpty()
    {
        var s = Series.FromValues("x", Array.Empty<double>());
        var result = s.RollingSum(windowSize: 3);

        result.Length.Should().Be(0);
    }

    [Fact]
    public void Rolling_SingleElement_Works()
    {
        var s = Series.FromValues("x", new double[] { 42 });
        var result = s.RollingSum(windowSize: 3);

        result.Length.Should().Be(1);
        result[0].AsFloat64().Should().Be(42.0);
    }

    [Fact]
    public void Rolling_AllNulls_AllNullResult()
    {
        var s = Series.FromNullable("x", new double?[] { null, null, null });
        var result = s.RollingSum(windowSize: 2);

        result.IsNull(0).Should().BeTrue();
        result.IsNull(1).Should().BeTrue();
        result.IsNull(2).Should().BeTrue();
    }

    [Fact]
    public void Rolling_InvalidWindowSize_Throws()
    {
        var s = Series.FromValues("x", new double[] { 1, 2, 3 });

        var act1 = () => s.RollingSum(windowSize: 0);
        var act2 = () => s.RollingSum(windowSize: -1);

        act1.Should().Throw<ArgumentOutOfRangeException>();
        act2.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Rolling_InvalidMinPeriods_Throws()
    {
        var s = Series.FromValues("x", new double[] { 1, 2, 3 });

        var act1 = () => s.RollingSum(windowSize: 3, minPeriods: 0);
        var act2 = () => s.RollingSum(windowSize: 3, minPeriods: -1);

        act1.Should().Throw<ArgumentOutOfRangeException>();
        act2.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Rolling_NaNValues_AreDataValues()
    {
        var s = Series.FromValues("x", new double[] { 1, double.NaN, 3, 4, 5 });
        var result = s.RollingSum(windowSize: 3, minPeriods: 1);

        // NaN is a valid data value (not null)
        result.IsNull(0).Should().BeFalse();
        result.IsNull(1).Should().BeFalse();
    }

    [Fact]
    public void Rolling_VeryLargeWindow_HandlesCorrectly()
    {
        var s = Series.FromValues("x", new double[] { 1, 2, 3 });
        var result = s.RollingSum(windowSize: int.MaxValue, minPeriods: 1);

        result[0].AsFloat64().Should().Be(1.0);
        result[1].AsFloat64().Should().Be(3.0);
        result[2].AsFloat64().Should().Be(6.0);
    }

    // ============================================================================
    // Consistency Tests (Polars Parity)
    // ============================================================================

    [Fact]
    public void RollingMean_MatchesCumMeanForExpanding()
    {
        var s = Series.FromValues("x", new double[] { 1, 2, 3, 4, 5 });

        // Expanding mean using very large window
        var expandingMean = s.RollingMean(windowSize: int.MaxValue);

        // Should give cumulative mean
        expandingMean[0].AsFloat64().Should().Be(1.0);
        expandingMean[1].AsFloat64().Should().Be(1.5);
        expandingMean[2].AsFloat64().Should().Be(2.0);
        expandingMean[3].AsFloat64().Should().Be(2.5);
        expandingMean[4].AsFloat64().Should().Be(3.0);
    }

    [Fact]
    public void RollingOperations_PreserveSeriesName()
    {
        var s = Series.FromValues("my_series", new double[] { 1, 2, 3, 4, 5 });

        s.RollingSum(3).Name.Should().Be("my_series");
        s.RollingMean(3).Name.Should().Be("my_series");
        s.RollingMin(3).Name.Should().Be("my_series");
        s.RollingMax(3).Name.Should().Be("my_series");
        s.RollingStd(3).Name.Should().Be("my_series");
        s.RollingMedian(3).Name.Should().Be("my_series");
    }

    [Fact]
    public void RollingOperations_ReturnFloat64()
    {
        var intSeries = Series.FromValues("x", new int[] { 1, 2, 3, 4, 5 });
        var floatSeries = Series.FromValues("x", new float[] { 1, 2, 3, 4, 5 });
        var doubleSeries = Series.FromValues("x", new double[] { 1, 2, 3, 4, 5 });

        intSeries.RollingSum(3).DataType.Should().Be(Polaire.DataTypes.DataType.Float64);
        floatSeries.RollingSum(3).DataType.Should().Be(Polaire.DataTypes.DataType.Float64);
        doubleSeries.RollingSum(3).DataType.Should().Be(Polaire.DataTypes.DataType.Float64);
    }

    // ============================================================================
    // Performance-Related Tests (ensure SIMD boundaries are handled)
    // ============================================================================

    [Theory]
    [InlineData(7)]    // Below SIMD threshold
    [InlineData(8)]    // At SIMD threshold
    [InlineData(9)]    // Just above SIMD threshold
    [InlineData(15)]   // Below 16
    [InlineData(16)]   // At 16
    [InlineData(17)]   // Just above 16
    [InlineData(100)]  // Larger
    public void RollingSum_VariousLengths_ConsistentResults(int length)
    {
        var values = Enumerable.Range(1, length).Select(x => (double)x).ToArray();
        var s = Series.FromValues("x", values);
        var result = s.RollingSum(windowSize: 3);

        result.Length.Should().Be(length);

        // Verify the last few values which should always have full windows
        if (length >= 3)
        {
            double expectedLast = values[length - 1] + values[length - 2] + values[length - 3];
            result[length - 1].AsFloat64().Should().Be(expectedLast);
        }
    }

    [Fact]
    public void RollingMean_LargeDataset_CorrectBoundaries()
    {
        // Test with 1000 elements to ensure no off-by-one errors in larger datasets
        var values = Enumerable.Range(1, 1000).Select(x => (double)x).ToArray();
        var s = Series.FromValues("x", values);
        var result = s.RollingMean(windowSize: 10);

        // Check first value with minPeriods=1
        result[0].AsFloat64().Should().Be(1.0);

        // Check value at index 9 (first full window)
        result[9].AsFloat64().Should().Be(5.5);  // mean(1..10) = 5.5

        // Check last value
        result[999].AsFloat64().Should().Be(995.5);  // mean(991..1000)
    }
}
