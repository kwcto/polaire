// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET
// Ported from Polars py-polars/tests/unit/operations/aggregation/test_aggregations.py

using Xunit;
using FluentAssertions;
using Polaire.DataTypes;

namespace Polaire.Tests;

/// <summary>
/// Comprehensive aggregation tests ported from Polars test suite.
/// </summary>
public class AggregationTests
{
    // ============================================================================
    // Sum Tests
    // ============================================================================

    [Fact]
    public void Sum_IntegerSeries_ReturnsCorrectSum()
    {
        var series = Series.FromValues("a", new[] { 1, 2, 3, 4, 5 });
        series.Sum().AsInt64().Should().Be(15);
    }

    [Fact]
    public void Sum_EmptySeries_ReturnsNull()
    {
        var series = Series.FromValues("a", Array.Empty<int>());
        series.Sum().IsNull.Should().BeTrue();
    }

    [Fact]
    public void Sum_AllNullSeries_ReturnsNull()
    {
        var series = Series.FromNullable("a", new int?[] { null, null, null });
        series.Sum().IsNull.Should().BeTrue();
    }

    [Fact]
    public void Sum_WithNulls_IgnoresNulls()
    {
        var series = Series.FromNullable("a", new int?[] { 1, null, 3, null, 5 });
        series.Sum().AsInt64().Should().Be(9);
    }

    [Fact]
    public void Sum_Float64Series_ReturnsCorrectSum()
    {
        var series = Series.FromValues("a", new[] { 1.5, 2.5, 3.5 });
        series.Sum().AsFloat64().Should().BeApproximately(7.5, 0.0001);
    }

    [Fact]
    public void Sum_NegativeNumbers_ReturnsCorrectSum()
    {
        var series = Series.FromValues("a", new[] { -1, 2, -3, 4, -5 });
        series.Sum().AsInt64().Should().Be(-3);
    }

    [Fact]
    public void Sum_LargeNumbers_NoOverflow()
    {
        // Use int.MaxValue / 2 + 1 to ensure sum exceeds int.MaxValue
        var series = Series.FromValues("a", new[] { int.MaxValue / 2 + 1, int.MaxValue / 2 + 1 });
        // Sum uses Int64 accumulator, so no overflow
        // (int.MaxValue / 2 + 1) * 2 = int.MaxValue + 1 (when not truncated)
        series.Sum().AsInt64().Should().BeGreaterThan(int.MaxValue);
    }

    // ============================================================================
    // Mean Tests
    // ============================================================================

    [Fact]
    public void Mean_IntegerSeries_ReturnsCorrectMean()
    {
        var series = Series.FromValues("a", new[] { 1, 2, 3, 4, 5 });
        series.Mean().AsFloat64().Should().BeApproximately(3.0, 0.0001);
    }

    [Fact]
    public void Mean_EmptySeries_ReturnsNull()
    {
        var series = Series.FromValues("a", Array.Empty<double>());
        series.Mean().IsNull.Should().BeTrue();
    }

    [Fact]
    public void Mean_WithNulls_IgnoresNulls()
    {
        var series = Series.FromNullable("a", new double?[] { 1.0, null, 3.0, null, 5.0 });
        series.Mean().AsFloat64().Should().BeApproximately(3.0, 0.0001);
    }

    [Fact]
    public void Mean_SingleElement_ReturnsElement()
    {
        var series = Series.FromValues("a", new[] { 42.0 });
        series.Mean().AsFloat64().Should().Be(42.0);
    }

    [Fact]
    public void Mean_BooleanSeries_TreatedAsNumeric()
    {
        // In Polars, boolean mean is the proportion of True values
        var series = Series.FromValues("a", new[] { true, true, false, false });
        // 2 true / 4 total = 0.5
        series.Mean().AsFloat64().Should().BeApproximately(0.5, 0.0001);
    }

    // ============================================================================
    // Min/Max Tests
    // ============================================================================

    [Fact]
    public void Min_IntegerSeries_ReturnsMinimum()
    {
        var series = Series.FromValues("a", new[] { 5, 2, 8, 1, 9 });
        series.Min().AsInt32().Should().Be(1);
    }

    [Fact]
    public void Max_IntegerSeries_ReturnsMaximum()
    {
        var series = Series.FromValues("a", new[] { 5, 2, 8, 1, 9 });
        series.Max().AsInt32().Should().Be(9);
    }

    [Fact]
    public void Min_WithNulls_IgnoresNulls()
    {
        var series = Series.FromNullable("a", new int?[] { 5, null, 2, null, 8 });
        series.Min().AsInt32().Should().Be(2);
    }

    [Fact]
    public void Max_WithNulls_IgnoresNulls()
    {
        var series = Series.FromNullable("a", new int?[] { 5, null, 2, null, 8 });
        series.Max().AsInt32().Should().Be(8);
    }

    [Fact]
    public void Min_EmptySeries_ReturnsNull()
    {
        var series = Series.FromValues("a", Array.Empty<int>());
        series.Min().IsNull.Should().BeTrue();
    }

    [Fact]
    public void Max_EmptySeries_ReturnsNull()
    {
        var series = Series.FromValues("a", Array.Empty<int>());
        series.Max().IsNull.Should().BeTrue();
    }

    [Fact]
    public void Min_Float64_SkipsNaN()
    {
        // Polars semantics: NaN is ignored in min/max calculations
        var series = Series.FromValues("a", new[] { double.NaN, 1.0, 2.0, 3.0 });
        series.Min().AsFloat64().Should().Be(1.0);
    }

    [Fact]
    public void Max_Float64_SkipsNaN()
    {
        var series = Series.FromValues("a", new[] { double.NaN, 1.0, 2.0, 3.0 });
        series.Max().AsFloat64().Should().Be(3.0);
    }

    [Fact]
    public void Min_WithInfinity_ReturnsNegativeInfinity()
    {
        var series = Series.FromValues("a", new[] { double.NegativeInfinity, 0.0, double.PositiveInfinity });
        series.Min().AsFloat64().Should().Be(double.NegativeInfinity);
    }

    [Fact]
    public void Max_WithInfinity_ReturnsPositiveInfinity()
    {
        var series = Series.FromValues("a", new[] { double.NegativeInfinity, 0.0, double.PositiveInfinity });
        series.Max().AsFloat64().Should().Be(double.PositiveInfinity);
    }

    // ============================================================================
    // Median Tests
    // ============================================================================

    [Fact]
    public void Median_OddCount_ReturnsMiddle()
    {
        var series = Series.FromValues("a", new[] { 1.0, 3.0, 5.0, 7.0, 9.0 });
        series.Median().AsFloat64().Should().Be(5.0);
    }

    [Fact]
    public void Median_EvenCount_ReturnsAverageOfMiddle()
    {
        var series = Series.FromValues("a", new[] { 1.0, 3.0, 5.0, 7.0 });
        series.Median().AsFloat64().Should().Be(4.0);
    }

    [Fact]
    public void Median_UnsortedData_StillCorrect()
    {
        var series = Series.FromValues("a", new[] { 9.0, 1.0, 5.0, 3.0, 7.0 });
        series.Median().AsFloat64().Should().Be(5.0);
    }

    [Fact]
    public void Median_WithNulls_IgnoresNulls()
    {
        var series = Series.FromNullable("a", new double?[] { 1.0, null, 5.0, null, 9.0 });
        series.Median().AsFloat64().Should().Be(5.0);
    }

    [Fact]
    public void Median_SingleElement_ReturnsElement()
    {
        var series = Series.FromValues("a", new[] { 42.0 });
        series.Median().AsFloat64().Should().Be(42.0);
    }

    // ============================================================================
    // Std/Var Tests
    // ============================================================================

    [Fact]
    public void Std_SampleStandardDeviation_Correct()
    {
        // Values: 2, 4, 4, 4, 5, 5, 7, 9 (sample std ≈ 2.138)
        var series = Series.FromValues("a", new[] { 2.0, 4.0, 4.0, 4.0, 5.0, 5.0, 7.0, 9.0 });
        series.Std().AsFloat64().Should().BeApproximately(2.138, 0.01);
    }

    [Fact]
    public void Var_SampleVariance_Correct()
    {
        var series = Series.FromValues("a", new[] { 2.0, 4.0, 4.0, 4.0, 5.0, 5.0, 7.0, 9.0 });
        // Variance is std^2
        series.Var().AsFloat64().Should().BeApproximately(2.138 * 2.138, 0.1);
    }

    [Fact]
    public void Std_SingleElement_ReturnsNull()
    {
        // With ddof=1, single element returns null (division by zero)
        var series = Series.FromValues("a", new[] { 5.0 });
        series.Std().IsNull.Should().BeTrue();
    }

    [Fact]
    public void Std_TwoIdenticalElements_ReturnsZero()
    {
        var series = Series.FromValues("a", new[] { 5.0, 5.0 });
        series.Std().AsFloat64().Should().BeApproximately(0.0, 0.0001);
    }

    [Fact]
    public void Std_WithNulls_IgnoresNulls()
    {
        var series = Series.FromNullable("a", new double?[] { 2.0, null, 4.0, null, 6.0 });
        // Only non-null values: 2, 4, 6 -> mean=4, var=(4+0+4)/2=4, std=2
        series.Std().AsFloat64().Should().BeApproximately(2.0, 0.01);
    }

    // ============================================================================
    // Quantile Tests
    // ============================================================================

    [Fact]
    public void Quantile_Median_Returns50thPercentile()
    {
        var series = Series.FromValues("a", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 });
        series.Quantile(0.5).AsFloat64().Should().Be(3.0);
    }

    [Fact]
    public void Quantile_Min_Returns0thPercentile()
    {
        var series = Series.FromValues("a", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 });
        series.Quantile(0.0).AsFloat64().Should().Be(1.0);
    }

    [Fact]
    public void Quantile_Max_Returns100thPercentile()
    {
        var series = Series.FromValues("a", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 });
        series.Quantile(1.0).AsFloat64().Should().Be(5.0);
    }

    [Fact]
    public void Quantile_LinearInterpolation_Correct()
    {
        var series = Series.FromValues("a", new[] { 1.0, 2.0, 3.0, 4.0 });
        // 0.25 quantile: position = 0.25 * 3 = 0.75
        // Linear interp between index 0 (1.0) and index 1 (2.0)
        // 1.0 + 0.75 * (2.0 - 1.0) = 1.75
        series.Quantile(0.25, "linear").AsFloat64().Should().BeApproximately(1.75, 0.0001);
    }

    [Fact]
    public void Quantile_Lower_ReturnsFloorIndex()
    {
        var series = Series.FromValues("a", new[] { 1.0, 2.0, 3.0, 4.0 });
        series.Quantile(0.25, "lower").AsFloat64().Should().Be(1.0);
    }

    [Fact]
    public void Quantile_Higher_ReturnsCeilingIndex()
    {
        var series = Series.FromValues("a", new[] { 1.0, 2.0, 3.0, 4.0 });
        series.Quantile(0.25, "higher").AsFloat64().Should().Be(2.0);
    }

    [Fact]
    public void Quantile_Midpoint_ReturnsAverageOfBoundaries()
    {
        var series = Series.FromValues("a", new[] { 1.0, 2.0, 3.0, 4.0 });
        // Midpoint between 1.0 and 2.0 = 1.5
        series.Quantile(0.25, "midpoint").AsFloat64().Should().BeApproximately(1.5, 0.0001);
    }

    [Fact]
    public void Quantile_OutOfRange_Throws()
    {
        var series = Series.FromValues("a", new[] { 1.0, 2.0, 3.0 });
        Assert.Throws<ArgumentOutOfRangeException>(() => series.Quantile(-0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => series.Quantile(1.1));
    }

    [Fact]
    public void Quantile_WithNulls_IgnoresNulls()
    {
        var series = Series.FromNullable("a", new double?[] { null, 1.0, null, 3.0, null, 5.0 });
        series.Quantile(0.5).AsFloat64().Should().Be(3.0);
    }

    // ============================================================================
    // Product Tests
    // ============================================================================

    [Fact]
    public void Product_IntegerSeries_ReturnsCorrectProduct()
    {
        var series = Series.FromValues("a", new[] { 1, 2, 3, 4 });
        series.Product().AsInt64().Should().Be(24);
    }

    [Fact]
    public void Product_Float64Series_ReturnsCorrectProduct()
    {
        var series = Series.FromValues("a", new[] { 1.5, 2.0, 3.0 });
        series.Product().AsFloat64().Should().BeApproximately(9.0, 0.0001);
    }

    [Fact]
    public void Product_WithZero_ReturnsZero()
    {
        var series = Series.FromValues("a", new[] { 1, 2, 0, 4 });
        series.Product().AsInt64().Should().Be(0);
    }

    [Fact]
    public void Product_WithNulls_IgnoresNulls()
    {
        var series = Series.FromNullable("a", new int?[] { 2, null, 3, null, 4 });
        series.Product().AsInt64().Should().Be(24);
    }

    [Fact]
    public void Product_EmptySeries_ReturnsNull()
    {
        var series = Series.FromValues("a", Array.Empty<int>());
        series.Product().IsNull.Should().BeTrue();
    }

    [Fact]
    public void Product_SingleElement_ReturnsElement()
    {
        var series = Series.FromValues("a", new[] { 42 });
        series.Product().AsInt64().Should().Be(42);
    }

    [Fact]
    public void Product_NegativeNumbers_Correct()
    {
        var series = Series.FromValues("a", new[] { -2, 3, -4 });
        series.Product().AsInt64().Should().Be(24); // -2 * 3 * -4 = 24
    }

    // ============================================================================
    // Cumulative Sum Tests
    // ============================================================================

    [Fact]
    public void CumSum_IntegerSeries_ReturnsRunningSum()
    {
        var series = Series.FromValues("a", new[] { 1, 2, 3, 4, 5 });
        var result = series.CumSum();

        result.Length.Should().Be(5);
        result[0].AsFloat64().Should().Be(1.0);
        result[1].AsFloat64().Should().Be(3.0);
        result[2].AsFloat64().Should().Be(6.0);
        result[3].AsFloat64().Should().Be(10.0);
        result[4].AsFloat64().Should().Be(15.0);
    }

    [Fact]
    public void CumSum_WithNulls_PreservesNullPositions()
    {
        var series = Series.FromNullable("a", new int?[] { 1, null, 3, null, 5 });
        var result = series.CumSum();

        result[0].AsFloat64().Should().Be(1.0);
        result.IsNull(1).Should().BeTrue();
        result[2].AsFloat64().Should().Be(4.0);  // 1 + 3
        result.IsNull(3).Should().BeTrue();
        result[4].AsFloat64().Should().Be(9.0);  // 1 + 3 + 5
    }

    [Fact]
    public void CumSum_Float64_HandlesDecimals()
    {
        var series = Series.FromValues("a", new[] { 1.5, 2.5, 3.5 });
        var result = series.CumSum();

        result[0].AsFloat64().Should().BeApproximately(1.5, 0.0001);
        result[1].AsFloat64().Should().BeApproximately(4.0, 0.0001);
        result[2].AsFloat64().Should().BeApproximately(7.5, 0.0001);
    }

    // ============================================================================
    // Cumulative Product Tests
    // ============================================================================

    [Fact]
    public void CumProd_IntegerSeries_ReturnsRunningProduct()
    {
        var series = Series.FromValues("a", new[] { 1, 2, 3, 4 });
        var result = series.CumProd();

        result[0].AsFloat64().Should().Be(1.0);
        result[1].AsFloat64().Should().Be(2.0);
        result[2].AsFloat64().Should().Be(6.0);
        result[3].AsFloat64().Should().Be(24.0);
    }

    [Fact]
    public void CumProd_WithZero_ZeroPropagatesToEnd()
    {
        var series = Series.FromValues("a", new[] { 1, 2, 0, 4 });
        var result = series.CumProd();

        result[0].AsFloat64().Should().Be(1.0);
        result[1].AsFloat64().Should().Be(2.0);
        result[2].AsFloat64().Should().Be(0.0);
        result[3].AsFloat64().Should().Be(0.0);  // Zero propagates
    }

    // ============================================================================
    // Cumulative Min/Max Tests
    // ============================================================================

    [Fact]
    public void CumMin_ReturnsRunningMinimum()
    {
        var series = Series.FromValues("a", new[] { 5.0, 3.0, 7.0, 1.0, 4.0 });
        var result = series.CumMin();

        result[0].AsFloat64().Should().Be(5.0);
        result[1].AsFloat64().Should().Be(3.0);  // min(5, 3)
        result[2].AsFloat64().Should().Be(3.0);  // min(3, 7)
        result[3].AsFloat64().Should().Be(1.0);  // min(3, 1)
        result[4].AsFloat64().Should().Be(1.0);  // min(1, 4)
    }

    [Fact]
    public void CumMax_ReturnsRunningMaximum()
    {
        var series = Series.FromValues("a", new[] { 3.0, 5.0, 2.0, 7.0, 4.0 });
        var result = series.CumMax();

        result[0].AsFloat64().Should().Be(3.0);
        result[1].AsFloat64().Should().Be(5.0);  // max(3, 5)
        result[2].AsFloat64().Should().Be(5.0);  // max(5, 2)
        result[3].AsFloat64().Should().Be(7.0);  // max(5, 7)
        result[4].AsFloat64().Should().Be(7.0);  // max(7, 4)
    }

    // ============================================================================
    // Shift Tests
    // ============================================================================

    [Fact]
    public void Shift_PositivePeriods_ShiftsForward()
    {
        var series = Series.FromValues("a", new[] { 1, 2, 3, 4, 5 });
        var result = series.Shift(2);

        result.IsNull(0).Should().BeTrue();
        result.IsNull(1).Should().BeTrue();
        result[2].AsInt32().Should().Be(1);
        result[3].AsInt32().Should().Be(2);
        result[4].AsInt32().Should().Be(3);
    }

    [Fact]
    public void Shift_NegativePeriods_ShiftsBackward()
    {
        var series = Series.FromValues("a", new[] { 1, 2, 3, 4, 5 });
        var result = series.Shift(-2);

        result[0].AsInt32().Should().Be(3);
        result[1].AsInt32().Should().Be(4);
        result[2].AsInt32().Should().Be(5);
        result.IsNull(3).Should().BeTrue();
        result.IsNull(4).Should().BeTrue();
    }

    [Fact]
    public void Shift_ZeroPeriods_ReturnsSameSeries()
    {
        var series = Series.FromValues("a", new[] { 1, 2, 3 });
        var result = series.Shift(0);

        result[0].AsInt32().Should().Be(1);
        result[1].AsInt32().Should().Be(2);
        result[2].AsInt32().Should().Be(3);
    }

    // ============================================================================
    // Diff Tests
    // ============================================================================

    [Fact]
    public void Diff_CalculatesFirstDifference()
    {
        var series = Series.FromValues("a", new[] { 1.0, 3.0, 6.0, 10.0 });
        var result = series.Diff(1);

        result.IsNull(0).Should().BeTrue();
        result[1].AsFloat64().Should().Be(2.0);  // 3 - 1
        result[2].AsFloat64().Should().Be(3.0);  // 6 - 3
        result[3].AsFloat64().Should().Be(4.0);  // 10 - 6
    }

    [Fact]
    public void Diff_SecondDifference_UsesPeriod2()
    {
        var series = Series.FromValues("a", new[] { 1.0, 3.0, 6.0, 10.0, 15.0 });
        var result = series.Diff(2);

        result.IsNull(0).Should().BeTrue();
        result.IsNull(1).Should().BeTrue();
        result[2].AsFloat64().Should().Be(5.0);   // 6 - 1
        result[3].AsFloat64().Should().Be(7.0);   // 10 - 3
        result[4].AsFloat64().Should().Be(9.0);   // 15 - 6
    }

    [Fact]
    public void Diff_WithNulls_PropagatesNulls()
    {
        var series = Series.FromNullable("a", new double?[] { 1.0, null, 3.0, 5.0 });
        var result = series.Diff(1);

        result.IsNull(0).Should().BeTrue();
        result.IsNull(1).Should().BeTrue();  // prev is non-null, current is null
        result.IsNull(2).Should().BeTrue();  // prev is null
        result[3].AsFloat64().Should().Be(2.0);  // 5 - 3
    }

    // ============================================================================
    // PctChange Tests
    // ============================================================================

    [Fact]
    public void PctChange_CalculatesPercentageChange()
    {
        var series = Series.FromValues("a", new[] { 10.0, 15.0, 12.0, 18.0 });
        var result = series.PctChange(1);

        result.IsNull(0).Should().BeTrue();
        result[1].AsFloat64().Should().BeApproximately(0.5, 0.0001);   // (15-10)/10 = 0.5
        result[2].AsFloat64().Should().BeApproximately(-0.2, 0.0001); // (12-15)/15 = -0.2
        result[3].AsFloat64().Should().BeApproximately(0.5, 0.0001);  // (18-12)/12 = 0.5
    }

    [Fact]
    public void PctChange_DivisionByZero_ReturnsNull()
    {
        var series = Series.FromValues("a", new[] { 0.0, 10.0, 20.0 });
        var result = series.PctChange(1);

        result.IsNull(0).Should().BeTrue();
        result.IsNull(1).Should().BeTrue();  // Division by zero (prev=0)
        result[2].AsFloat64().Should().Be(1.0);  // (20-10)/10 = 1.0
    }

    // ============================================================================
    // Clip Tests
    // ============================================================================

    [Fact]
    public void Clip_BothBounds_ClipsValues()
    {
        var series = Series.FromValues("a", new[] { 1.0, 5.0, 10.0, 15.0, 20.0 });
        var result = series.Clip(5.0, 15.0);

        result[0].AsFloat64().Should().Be(5.0);   // clipped up
        result[1].AsFloat64().Should().Be(5.0);
        result[2].AsFloat64().Should().Be(10.0);  // unchanged
        result[3].AsFloat64().Should().Be(15.0);  // unchanged
        result[4].AsFloat64().Should().Be(15.0);  // clipped down
    }

    [Fact]
    public void Clip_LowerOnly_ClipsBelow()
    {
        var series = Series.FromValues("a", new[] { 1.0, 5.0, 10.0 });
        var result = series.Clip(3.0, null);

        result[0].AsFloat64().Should().Be(3.0);   // clipped
        result[1].AsFloat64().Should().Be(5.0);
        result[2].AsFloat64().Should().Be(10.0);
    }

    [Fact]
    public void Clip_UpperOnly_ClipsAbove()
    {
        var series = Series.FromValues("a", new[] { 1.0, 5.0, 10.0 });
        var result = series.Clip(null, 7.0);

        result[0].AsFloat64().Should().Be(1.0);
        result[1].AsFloat64().Should().Be(5.0);
        result[2].AsFloat64().Should().Be(7.0);   // clipped
    }

    [Fact]
    public void Clip_PreservesNulls()
    {
        var series = Series.FromNullable("a", new double?[] { 1.0, null, 10.0 });
        var result = series.Clip(3.0, 8.0);

        result[0].AsFloat64().Should().Be(3.0);
        result.IsNull(1).Should().BeTrue();
        result[2].AsFloat64().Should().Be(8.0);
    }

    // ============================================================================
    // Abs Tests
    // ============================================================================

    [Fact]
    public void Abs_IntegerSeries_ReturnsAbsoluteValues()
    {
        var series = Series.FromValues("a", new[] { -5, 3, -1, 0, 4 });
        var result = series.Abs();

        result[0].AsInt32().Should().Be(5);
        result[1].AsInt32().Should().Be(3);
        result[2].AsInt32().Should().Be(1);
        result[3].AsInt32().Should().Be(0);
        result[4].AsInt32().Should().Be(4);
    }

    [Fact]
    public void Abs_Float64Series_ReturnsAbsoluteValues()
    {
        var series = Series.FromValues("a", new[] { -5.5, 3.3, -1.1 });
        var result = series.Abs();

        result[0].AsFloat64().Should().Be(5.5);
        result[1].AsFloat64().Should().Be(3.3);
        result[2].AsFloat64().Should().Be(1.1);
    }

    [Fact]
    public void Abs_PreservesNulls()
    {
        var series = Series.FromNullable("a", new int?[] { -5, null, 3 });
        var result = series.Abs();

        result[0].AsInt32().Should().Be(5);
        result.IsNull(1).Should().BeTrue();
        result[2].AsInt32().Should().Be(3);
    }

    // ============================================================================
    // Math Functions Tests
    // ============================================================================

    [Fact]
    public void Sqrt_CalculatesSquareRoot()
    {
        var series = Series.FromValues("a", new[] { 4.0, 9.0, 16.0 });
        var result = series.Sqrt();

        result[0].AsFloat64().Should().Be(2.0);
        result[1].AsFloat64().Should().Be(3.0);
        result[2].AsFloat64().Should().Be(4.0);
    }

    [Fact]
    public void Log_CalculatesNaturalLog()
    {
        var series = Series.FromValues("a", new[] { 1.0, Math.E, Math.E * Math.E });
        var result = series.Log();

        result[0].AsFloat64().Should().BeApproximately(0.0, 0.0001);
        result[1].AsFloat64().Should().BeApproximately(1.0, 0.0001);
        result[2].AsFloat64().Should().BeApproximately(2.0, 0.0001);
    }

    [Fact]
    public void Exp_CalculatesExponential()
    {
        var series = Series.FromValues("a", new[] { 0.0, 1.0, 2.0 });
        var result = series.Exp();

        result[0].AsFloat64().Should().BeApproximately(1.0, 0.0001);
        result[1].AsFloat64().Should().BeApproximately(Math.E, 0.0001);
        result[2].AsFloat64().Should().BeApproximately(Math.E * Math.E, 0.0001);
    }

    [Fact]
    public void Sin_CalculatesSine()
    {
        var series = Series.FromValues("a", new[] { 0.0, Math.PI / 2, Math.PI });
        var result = series.Sin();

        result[0].AsFloat64().Should().BeApproximately(0.0, 0.0001);
        result[1].AsFloat64().Should().BeApproximately(1.0, 0.0001);
        result[2].AsFloat64().Should().BeApproximately(0.0, 0.0001);
    }

    [Fact]
    public void Cos_CalculatesCosine()
    {
        var series = Series.FromValues("a", new[] { 0.0, Math.PI / 2, Math.PI });
        var result = series.Cos();

        result[0].AsFloat64().Should().BeApproximately(1.0, 0.0001);
        result[1].AsFloat64().Should().BeApproximately(0.0, 0.0001);
        result[2].AsFloat64().Should().BeApproximately(-1.0, 0.0001);
    }

    [Fact]
    public void Floor_RoundsDown()
    {
        var series = Series.FromValues("a", new[] { 1.1, 2.9, -1.1, -2.9 });
        var result = series.Floor();

        result[0].AsFloat64().Should().Be(1.0);
        result[1].AsFloat64().Should().Be(2.0);
        result[2].AsFloat64().Should().Be(-2.0);
        result[3].AsFloat64().Should().Be(-3.0);
    }

    [Fact]
    public void Ceil_RoundsUp()
    {
        var series = Series.FromValues("a", new[] { 1.1, 2.9, -1.1, -2.9 });
        var result = series.Ceil();

        result[0].AsFloat64().Should().Be(2.0);
        result[1].AsFloat64().Should().Be(3.0);
        result[2].AsFloat64().Should().Be(-1.0);
        result[3].AsFloat64().Should().Be(-2.0);
    }

    [Fact]
    public void Round_RoundsToNearestInteger()
    {
        var series = Series.FromValues("a", new[] { 1.4, 1.5, 1.6, 2.5 });
        var result = series.Round();

        result[0].AsFloat64().Should().Be(1.0);
        result[1].AsFloat64().Should().Be(2.0);  // Banker's rounding
        result[2].AsFloat64().Should().Be(2.0);
        result[3].AsFloat64().Should().Be(2.0);  // Banker's rounding
    }

    [Fact]
    public void Round_WithDecimals_RoundsToSpecifiedPrecision()
    {
        var series = Series.FromValues("a", new[] { 1.234, 5.678 });
        var result = series.Round(2);

        result[0].AsFloat64().Should().Be(1.23);
        result[1].AsFloat64().Should().Be(5.68);
    }

    [Fact]
    public void Sign_ReturnsSignOfValue()
    {
        var series = Series.FromValues("a", new[] { -5.0, 0.0, 3.0 });
        var result = series.Sign();

        result[0].AsInt32().Should().Be(-1);
        result[1].AsInt32().Should().Be(0);
        result[2].AsInt32().Should().Be(1);
    }

    // ============================================================================
    // First/Last Tests
    // ============================================================================

    [Fact]
    public void First_ReturnsFirstElement()
    {
        var series = Series.FromValues("a", new[] { 10, 20, 30 });
        series.First().AsInt32().Should().Be(10);
    }

    [Fact]
    public void Last_ReturnsLastElement()
    {
        var series = Series.FromValues("a", new[] { 10, 20, 30 });
        series.Last().AsInt32().Should().Be(30);
    }

    [Fact]
    public void First_EmptySeries_ReturnsNull()
    {
        var series = Series.FromValues("a", Array.Empty<int>());
        series.First().IsNull.Should().BeTrue();
    }

    [Fact]
    public void Last_EmptySeries_ReturnsNull()
    {
        var series = Series.FromValues("a", Array.Empty<int>());
        series.Last().IsNull.Should().BeTrue();
    }

    // ============================================================================
    // Count Tests
    // ============================================================================

    [Fact]
    public void Count_ReturnsNonNullCount()
    {
        var series = Series.FromNullable("a", new int?[] { 1, null, 3, null, 5 });
        series.Count().Should().Be(3);
    }

    [Fact]
    public void Count_AllNulls_ReturnsZero()
    {
        var series = Series.FromNullable("a", new int?[] { null, null, null });
        series.Count().Should().Be(0);
    }

    [Fact]
    public void Count_NoNulls_ReturnsLength()
    {
        var series = Series.FromValues("a", new[] { 1, 2, 3, 4, 5 });
        series.Count().Should().Be(5);
    }

    // ============================================================================
    // Edge Cases - SIMD Boundary Tests
    // ============================================================================

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(32)]
    [InlineData(100)]
    [InlineData(1000)]
    public void Sum_VariousSizes_ConsistentResults(int size)
    {
        if (size == 0)
        {
            var series = Series.FromValues("a", Array.Empty<double>());
            series.Sum().IsNull.Should().BeTrue();
            return;
        }

        var values = Enumerable.Range(1, size).Select(i => (double)i).ToArray();
        var series2 = Series.FromValues("a", values);
        var expectedSum = (double)size * (size + 1) / 2;  // Sum of 1..n = n*(n+1)/2
        series2.Sum().AsFloat64().Should().BeApproximately(expectedSum, 0.0001);
    }

    [Theory]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(16)]
    [InlineData(32)]
    [InlineData(100)]
    public void Min_VariousSizes_ConsistentResults(int size)
    {
        var values = Enumerable.Range(1, size).Select(i => (double)i).Reverse().ToArray();
        var series = Series.FromValues("a", values);
        series.Min().AsFloat64().Should().Be(1.0);
    }

    [Theory]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(16)]
    [InlineData(32)]
    [InlineData(100)]
    public void Max_VariousSizes_ConsistentResults(int size)
    {
        var values = Enumerable.Range(1, size).Select(i => (double)i).ToArray();
        var series = Series.FromValues("a", values);
        series.Max().AsFloat64().Should().Be(size);
    }
}
