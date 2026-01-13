// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET
//
// Comprehensive tests for Series operations, inspired by Polars test suite.
// These tests cover all Series methods including:
// - Mathematical operations (Abs, Sqrt, Log, etc.)
// - Cumulative operations (CumSum, CumProd, CumMin, CumMax)
// - Shift and Diff operations
// - Clip operations
// - Trigonometric functions
// - Rounding operations

using FluentAssertions;
using Polaire;
using Polaire.DataTypes;
using Xunit;

namespace Polaire.Tests;

/// <summary>
/// Tests for Series mathematical and transformation operations.
/// </summary>
public class SeriesOperationsTests
{
    // ============================================================================
    // Abs Tests
    // ============================================================================

    [Fact]
    public void Abs_PositiveValues_ReturnsSameValues()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0 });
        var result = series.Abs();

        result[0].AsFloat64().Should().Be(1.0);
        result[1].AsFloat64().Should().Be(2.0);
        result[2].AsFloat64().Should().Be(3.0);
    }

    [Fact]
    public void Abs_NegativeValues_ReturnsPositiveValues()
    {
        var series = Series.FromValues("s", new[] { -1.0, -2.0, -3.0 });
        var result = series.Abs();

        result[0].AsFloat64().Should().Be(1.0);
        result[1].AsFloat64().Should().Be(2.0);
        result[2].AsFloat64().Should().Be(3.0);
    }

    [Fact]
    public void Abs_MixedValues_ReturnsAbsoluteValues()
    {
        var series = Series.FromValues("s", new[] { -5.0, 0.0, 5.0, -10.0 });
        var result = series.Abs();

        result[0].AsFloat64().Should().Be(5.0);
        result[1].AsFloat64().Should().Be(0.0);
        result[2].AsFloat64().Should().Be(5.0);
        result[3].AsFloat64().Should().Be(10.0);
    }

    [Fact]
    public void Abs_WithNulls_PreservesNulls()
    {
        var series = Series.FromNullable("s", new double?[] { -1.0, null, 3.0 });
        var result = series.Abs();

        result[0].AsFloat64().Should().Be(1.0);
        result.IsNull(1).Should().BeTrue();
        result[2].AsFloat64().Should().Be(3.0);
    }

    [Fact]
    public void Abs_IntegerValues_ReturnsAbsoluteValues()
    {
        var series = Series.FromValues("s", new[] { -1, -2, 3, -4, 5 });
        var result = series.Abs();

        result[0].AsInt32().Should().Be(1);
        result[1].AsInt32().Should().Be(2);
        result[2].AsInt32().Should().Be(3);
        result[3].AsInt32().Should().Be(4);
        result[4].AsInt32().Should().Be(5);
    }

    // ============================================================================
    // Sqrt Tests
    // ============================================================================

    [Fact]
    public void Sqrt_PositiveValues_ReturnsSquareRoots()
    {
        var series = Series.FromValues("s", new[] { 1.0, 4.0, 9.0, 16.0 });
        var result = series.Sqrt();

        result[0].AsFloat64().Should().BeApproximately(1.0, 0.0001);
        result[1].AsFloat64().Should().BeApproximately(2.0, 0.0001);
        result[2].AsFloat64().Should().BeApproximately(3.0, 0.0001);
        result[3].AsFloat64().Should().BeApproximately(4.0, 0.0001);
    }

    [Fact]
    public void Sqrt_Zero_ReturnsZero()
    {
        var series = Series.FromValues("s", new[] { 0.0 });
        var result = series.Sqrt();

        result[0].AsFloat64().Should().Be(0.0);
    }

    [Fact]
    public void Sqrt_NegativeValues_ReturnsNaN()
    {
        var series = Series.FromValues("s", new[] { -1.0, -4.0 });
        var result = series.Sqrt();

        double.IsNaN(result[0].AsFloat64()).Should().BeTrue();
        double.IsNaN(result[1].AsFloat64()).Should().BeTrue();
    }

    [Fact]
    public void Sqrt_FractionalValues_ReturnsCorrectRoots()
    {
        var series = Series.FromValues("s", new[] { 0.25, 0.5, 2.0 });
        var result = series.Sqrt();

        result[0].AsFloat64().Should().BeApproximately(0.5, 0.0001);
        result[1].AsFloat64().Should().BeApproximately(0.7071, 0.0001);
        result[2].AsFloat64().Should().BeApproximately(1.4142, 0.0001);
    }

    // ============================================================================
    // Log Tests
    // ============================================================================

    [Fact]
    public void Log_PositiveValues_ReturnsNaturalLog()
    {
        var series = Series.FromValues("s", new[] { 1.0, Math.E, Math.E * Math.E });
        var result = series.Log();

        result[0].AsFloat64().Should().BeApproximately(0.0, 0.0001);
        result[1].AsFloat64().Should().BeApproximately(1.0, 0.0001);
        result[2].AsFloat64().Should().BeApproximately(2.0, 0.0001);
    }

    [Fact]
    public void Log_Zero_ReturnsNegativeInfinity()
    {
        var series = Series.FromValues("s", new[] { 0.0 });
        var result = series.Log();

        double.IsNegativeInfinity(result[0].AsFloat64()).Should().BeTrue();
    }

    [Fact]
    public void Log_NegativeValues_ReturnsNaN()
    {
        var series = Series.FromValues("s", new[] { -1.0 });
        var result = series.Log();

        double.IsNaN(result[0].AsFloat64()).Should().BeTrue();
    }

    // ============================================================================
    // Log10 Tests
    // ============================================================================

    [Fact]
    public void Log10_PositiveValues_ReturnsBase10Log()
    {
        var series = Series.FromValues("s", new[] { 1.0, 10.0, 100.0, 1000.0 });
        var result = series.Log10();

        result[0].AsFloat64().Should().BeApproximately(0.0, 0.0001);
        result[1].AsFloat64().Should().BeApproximately(1.0, 0.0001);
        result[2].AsFloat64().Should().BeApproximately(2.0, 0.0001);
        result[3].AsFloat64().Should().BeApproximately(3.0, 0.0001);
    }

    // ============================================================================
    // Exp Tests
    // ============================================================================

    [Fact]
    public void Exp_Values_ReturnsExponential()
    {
        var series = Series.FromValues("s", new[] { 0.0, 1.0, 2.0 });
        var result = series.Exp();

        result[0].AsFloat64().Should().BeApproximately(1.0, 0.0001);
        result[1].AsFloat64().Should().BeApproximately(Math.E, 0.0001);
        result[2].AsFloat64().Should().BeApproximately(Math.E * Math.E, 0.0001);
    }

    [Fact]
    public void Exp_NegativeValues_ReturnsSmallValues()
    {
        var series = Series.FromValues("s", new[] { -1.0, -2.0 });
        var result = series.Exp();

        result[0].AsFloat64().Should().BeApproximately(1.0 / Math.E, 0.0001);
        result[1].AsFloat64().Should().BeApproximately(1.0 / (Math.E * Math.E), 0.0001);
    }

    // ============================================================================
    // Trigonometric Tests
    // ============================================================================

    [Fact]
    public void Sin_CommonAngles_ReturnsCorrectValues()
    {
        var series = Series.FromValues("s", new[] { 0.0, Math.PI / 2, Math.PI, 3 * Math.PI / 2 });
        var result = series.Sin();

        result[0].AsFloat64().Should().BeApproximately(0.0, 0.0001);
        result[1].AsFloat64().Should().BeApproximately(1.0, 0.0001);
        result[2].AsFloat64().Should().BeApproximately(0.0, 0.0001);
        result[3].AsFloat64().Should().BeApproximately(-1.0, 0.0001);
    }

    [Fact]
    public void Cos_CommonAngles_ReturnsCorrectValues()
    {
        var series = Series.FromValues("s", new[] { 0.0, Math.PI / 2, Math.PI });
        var result = series.Cos();

        result[0].AsFloat64().Should().BeApproximately(1.0, 0.0001);
        result[1].AsFloat64().Should().BeApproximately(0.0, 0.0001);
        result[2].AsFloat64().Should().BeApproximately(-1.0, 0.0001);
    }

    [Fact]
    public void Tan_CommonAngles_ReturnsCorrectValues()
    {
        var series = Series.FromValues("s", new[] { 0.0, Math.PI / 4 });
        var result = series.Tan();

        result[0].AsFloat64().Should().BeApproximately(0.0, 0.0001);
        result[1].AsFloat64().Should().BeApproximately(1.0, 0.0001);
    }

    // ============================================================================
    // Floor Tests
    // ============================================================================

    [Fact]
    public void Floor_PositiveValues_ReturnsFloor()
    {
        var series = Series.FromValues("s", new[] { 1.1, 2.9, 3.5, 4.0 });
        var result = series.Floor();

        result[0].AsFloat64().Should().Be(1.0);
        result[1].AsFloat64().Should().Be(2.0);
        result[2].AsFloat64().Should().Be(3.0);
        result[3].AsFloat64().Should().Be(4.0);
    }

    [Fact]
    public void Floor_NegativeValues_ReturnsFloor()
    {
        var series = Series.FromValues("s", new[] { -1.1, -2.9, -3.5 });
        var result = series.Floor();

        result[0].AsFloat64().Should().Be(-2.0);
        result[1].AsFloat64().Should().Be(-3.0);
        result[2].AsFloat64().Should().Be(-4.0);
    }

    // ============================================================================
    // Ceil Tests
    // ============================================================================

    [Fact]
    public void Ceil_PositiveValues_ReturnsCeiling()
    {
        var series = Series.FromValues("s", new[] { 1.1, 2.9, 3.5, 4.0 });
        var result = series.Ceil();

        result[0].AsFloat64().Should().Be(2.0);
        result[1].AsFloat64().Should().Be(3.0);
        result[2].AsFloat64().Should().Be(4.0);
        result[3].AsFloat64().Should().Be(4.0);
    }

    [Fact]
    public void Ceil_NegativeValues_ReturnsCeiling()
    {
        var series = Series.FromValues("s", new[] { -1.1, -2.9, -3.5 });
        var result = series.Ceil();

        result[0].AsFloat64().Should().Be(-1.0);
        result[1].AsFloat64().Should().Be(-2.0);
        result[2].AsFloat64().Should().Be(-3.0);
    }

    // ============================================================================
    // Round Tests
    // ============================================================================

    [Fact]
    public void Round_Values_RoundsToNearestInteger()
    {
        var series = Series.FromValues("s", new[] { 1.4, 1.5, 1.6, 2.5 });
        var result = series.Round();

        result[0].AsFloat64().Should().Be(1.0);
        result[1].AsFloat64().Should().Be(2.0);  // Rounds to even
        result[2].AsFloat64().Should().Be(2.0);
        result[3].AsFloat64().Should().Be(2.0);  // Rounds to even
    }

    [Fact]
    public void Round_WithDecimals_RoundsToSpecifiedDecimals()
    {
        var series = Series.FromValues("s", new[] { 1.234, 2.567, 3.891 });
        var result = series.Round(2);

        result[0].AsFloat64().Should().BeApproximately(1.23, 0.001);
        result[1].AsFloat64().Should().BeApproximately(2.57, 0.001);
        result[2].AsFloat64().Should().BeApproximately(3.89, 0.001);
    }

    [Fact]
    public void Round_ZeroDecimals_RoundsToInteger()
    {
        // Note: C# Math.Round doesn't support negative decimals like Polars
        // This test verifies rounding to 0 decimals (whole numbers)
        var series = Series.FromValues("s", new[] { 123.4, 456.7, 789.1 });
        var result = series.Round(0);

        result[0].AsFloat64().Should().Be(123.0);
        result[1].AsFloat64().Should().Be(457.0);
        result[2].AsFloat64().Should().Be(789.0);
    }

    // ============================================================================
    // Sign Tests
    // ============================================================================

    [Fact]
    public void Sign_MixedValues_ReturnsSign()
    {
        var series = Series.FromValues("s", new[] { -5.0, 0.0, 5.0 });
        var result = series.Sign();

        // Sign returns Int32: -1, 0, or 1
        result[0].AsInt32().Should().Be(-1);
        result[1].AsInt32().Should().Be(0);
        result[2].AsInt32().Should().Be(1);
    }

    // ============================================================================
    // CumSum Tests
    // ============================================================================

    [Fact]
    public void CumSum_Values_ReturnsCumulativeSum()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 });
        var result = series.CumSum();

        result[0].AsFloat64().Should().Be(1.0);
        result[1].AsFloat64().Should().Be(3.0);
        result[2].AsFloat64().Should().Be(6.0);
        result[3].AsFloat64().Should().Be(10.0);
        result[4].AsFloat64().Should().Be(15.0);
    }

    [Fact]
    public void CumSum_WithNegatives_ReturnsCumulativeSum()
    {
        var series = Series.FromValues("s", new[] { 1.0, -2.0, 3.0, -4.0 });
        var result = series.CumSum();

        result[0].AsFloat64().Should().Be(1.0);
        result[1].AsFloat64().Should().Be(-1.0);
        result[2].AsFloat64().Should().Be(2.0);
        result[3].AsFloat64().Should().Be(-2.0);
    }

    [Fact]
    public void CumSum_Integers_ReturnsCumulativeSum()
    {
        var series = Series.FromValues("s", new[] { 1, 2, 3, 4, 5 });
        var result = series.CumSum();

        result.Length.Should().Be(5);
        // CumSum always returns Float64
        result[4].AsFloat64().Should().Be(15);
    }

    // ============================================================================
    // CumProd Tests
    // ============================================================================

    [Fact]
    public void CumProd_Values_ReturnsCumulativeProduct()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0, 4.0 });
        var result = series.CumProd();

        result[0].AsFloat64().Should().Be(1.0);
        result[1].AsFloat64().Should().Be(2.0);
        result[2].AsFloat64().Should().Be(6.0);
        result[3].AsFloat64().Should().Be(24.0);
    }

    [Fact]
    public void CumProd_WithZero_ReturnsZeroFromThatPoint()
    {
        var series = Series.FromValues("s", new[] { 2.0, 3.0, 0.0, 5.0 });
        var result = series.CumProd();

        result[0].AsFloat64().Should().Be(2.0);
        result[1].AsFloat64().Should().Be(6.0);
        result[2].AsFloat64().Should().Be(0.0);
        result[3].AsFloat64().Should().Be(0.0);
    }

    // ============================================================================
    // CumMin Tests
    // ============================================================================

    [Fact]
    public void CumMin_Values_ReturnsCumulativeMin()
    {
        var series = Series.FromValues("s", new[] { 5.0, 3.0, 7.0, 1.0, 4.0 });
        var result = series.CumMin();

        result[0].AsFloat64().Should().Be(5.0);
        result[1].AsFloat64().Should().Be(3.0);
        result[2].AsFloat64().Should().Be(3.0);
        result[3].AsFloat64().Should().Be(1.0);
        result[4].AsFloat64().Should().Be(1.0);
    }

    // ============================================================================
    // CumMax Tests
    // ============================================================================

    [Fact]
    public void CumMax_Values_ReturnsCumulativeMax()
    {
        var series = Series.FromValues("s", new[] { 1.0, 3.0, 2.0, 5.0, 4.0 });
        var result = series.CumMax();

        result[0].AsFloat64().Should().Be(1.0);
        result[1].AsFloat64().Should().Be(3.0);
        result[2].AsFloat64().Should().Be(3.0);
        result[3].AsFloat64().Should().Be(5.0);
        result[4].AsFloat64().Should().Be(5.0);
    }

    // ============================================================================
    // Shift Tests
    // ============================================================================

    [Fact]
    public void Shift_PositivePeriods_ShiftsForward()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 });
        var result = series.Shift(2);

        result.IsNull(0).Should().BeTrue();
        result.IsNull(1).Should().BeTrue();
        result[2].AsFloat64().Should().Be(1.0);
        result[3].AsFloat64().Should().Be(2.0);
        result[4].AsFloat64().Should().Be(3.0);
    }

    [Fact]
    public void Shift_NegativePeriods_ShiftsBackward()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 });
        var result = series.Shift(-2);

        result[0].AsFloat64().Should().Be(3.0);
        result[1].AsFloat64().Should().Be(4.0);
        result[2].AsFloat64().Should().Be(5.0);
        result.IsNull(3).Should().BeTrue();
        result.IsNull(4).Should().BeTrue();
    }

    [Fact]
    public void Shift_ZeroPeriods_ReturnsSameValues()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0 });
        var result = series.Shift(0);

        result[0].AsFloat64().Should().Be(1.0);
        result[1].AsFloat64().Should().Be(2.0);
        result[2].AsFloat64().Should().Be(3.0);
    }

    // ============================================================================
    // Diff Tests
    // ============================================================================

    [Fact]
    public void Diff_Values_ReturnsDifference()
    {
        var series = Series.FromValues("s", new[] { 1.0, 3.0, 6.0, 10.0 });
        var result = series.Diff(1);

        result.IsNull(0).Should().BeTrue();
        result[1].AsFloat64().Should().Be(2.0);
        result[2].AsFloat64().Should().Be(3.0);
        result[3].AsFloat64().Should().Be(4.0);
    }

    [Fact]
    public void Diff_WithNegatives_ReturnsDifference()
    {
        var series = Series.FromValues("s", new[] { 5.0, 3.0, 1.0, 4.0 });
        var result = series.Diff(1);

        result.IsNull(0).Should().BeTrue();
        result[1].AsFloat64().Should().Be(-2.0);
        result[2].AsFloat64().Should().Be(-2.0);
        result[3].AsFloat64().Should().Be(3.0);
    }

    [Fact]
    public void Diff_N2_ReturnsDifferenceWith2Lag()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 4.0, 7.0, 11.0 });
        var result = series.Diff(2);

        result.IsNull(0).Should().BeTrue();
        result.IsNull(1).Should().BeTrue();
        result[2].AsFloat64().Should().Be(3.0);  // 4 - 1
        result[3].AsFloat64().Should().Be(5.0);  // 7 - 2
        result[4].AsFloat64().Should().Be(7.0);  // 11 - 4
    }

    // ============================================================================
    // PctChange Tests
    // ============================================================================

    [Fact]
    public void PctChange_Values_ReturnsPercentageChange()
    {
        var series = Series.FromValues("s", new[] { 100.0, 110.0, 99.0, 111.0 });
        var result = series.PctChange(1);

        result.IsNull(0).Should().BeTrue();
        result[1].AsFloat64().Should().BeApproximately(0.1, 0.0001);    // (110-100)/100 = 0.1
        result[2].AsFloat64().Should().BeApproximately(-0.1, 0.0001);   // (99-110)/110 ≈ -0.1
        result[3].AsFloat64().Should().BeApproximately(0.1212, 0.0001); // (111-99)/99 ≈ 0.1212
    }

    // ============================================================================
    // Clip Tests
    // ============================================================================

    [Fact]
    public void Clip_BothBounds_ClipsValues()
    {
        var series = Series.FromValues("s", new[] { 1.0, 5.0, 10.0, 15.0, 20.0 });
        var result = series.Clip(5.0, 15.0);

        result[0].AsFloat64().Should().Be(5.0);   // Clipped to lower
        result[1].AsFloat64().Should().Be(5.0);   // At lower bound
        result[2].AsFloat64().Should().Be(10.0);  // Within bounds
        result[3].AsFloat64().Should().Be(15.0);  // At upper bound
        result[4].AsFloat64().Should().Be(15.0);  // Clipped to upper
    }

    [Fact]
    public void Clip_LowerBoundOnly_ClipsLowerValues()
    {
        var series = Series.FromValues("s", new[] { 1.0, 5.0, 10.0 });
        var result = series.Clip(5.0, null);

        result[0].AsFloat64().Should().Be(5.0);
        result[1].AsFloat64().Should().Be(5.0);
        result[2].AsFloat64().Should().Be(10.0);
    }

    [Fact]
    public void Clip_UpperBoundOnly_ClipsUpperValues()
    {
        var series = Series.FromValues("s", new[] { 1.0, 5.0, 10.0 });
        var result = series.Clip(null, 5.0);

        result[0].AsFloat64().Should().Be(1.0);
        result[1].AsFloat64().Should().Be(5.0);
        result[2].AsFloat64().Should().Be(5.0);
    }

    // ============================================================================
    // Reverse Tests
    // ============================================================================

    [Fact]
    public void Reverse_Values_ReversesOrder()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 });
        var result = series.Reverse();

        result[0].AsFloat64().Should().Be(5.0);
        result[1].AsFloat64().Should().Be(4.0);
        result[2].AsFloat64().Should().Be(3.0);
        result[3].AsFloat64().Should().Be(2.0);
        result[4].AsFloat64().Should().Be(1.0);
    }

    [Fact]
    public void Reverse_SingleElement_ReturnsSame()
    {
        var series = Series.FromValues("s", new[] { 42.0 });
        var result = series.Reverse();

        result[0].AsFloat64().Should().Be(42.0);
    }

    [Fact]
    public void Reverse_Empty_ReturnsEmpty()
    {
        var series = Series.FromValues("s", Array.Empty<double>());
        var result = series.Reverse();

        result.Length.Should().Be(0);
    }

    [Fact]
    public void Reverse_WithNulls_ReversesIncludingNulls()
    {
        var series = Series.FromNullable("s", new double?[] { 1.0, null, 3.0 });
        var result = series.Reverse();

        result[0].AsFloat64().Should().Be(3.0);
        result.IsNull(1).Should().BeTrue();
        result[2].AsFloat64().Should().Be(1.0);
    }

    // ============================================================================
    // First/Last Tests
    // ============================================================================

    [Fact]
    public void First_ReturnsFirstElement()
    {
        var series = Series.FromValues("s", new[] { 10.0, 20.0, 30.0 });
        var result = series.First();

        result.AsFloat64().Should().Be(10.0);
    }

    [Fact]
    public void Last_ReturnsLastElement()
    {
        var series = Series.FromValues("s", new[] { 10.0, 20.0, 30.0 });
        var result = series.Last();

        result.AsFloat64().Should().Be(30.0);
    }

    [Fact]
    public void First_EmptySeries_ReturnsNull()
    {
        var series = Series.FromValues("s", Array.Empty<double>());
        var result = series.First();

        result.IsNull.Should().BeTrue();
    }

    [Fact]
    public void Last_EmptySeries_ReturnsNull()
    {
        var series = Series.FromValues("s", Array.Empty<double>());
        var result = series.Last();

        result.IsNull.Should().BeTrue();
    }

    // ============================================================================
    // Product Tests
    // ============================================================================

    [Fact]
    public void Product_Values_ReturnsProduct()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0, 4.0 });
        var result = series.Product();

        result.AsFloat64().Should().Be(24.0);
    }

    [Fact]
    public void Product_WithZero_ReturnsZero()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 0.0, 4.0 });
        var result = series.Product();

        result.AsFloat64().Should().Be(0.0);
    }

    [Fact]
    public void Product_SingleElement_ReturnsThatElement()
    {
        var series = Series.FromValues("s", new[] { 42.0 });
        var result = series.Product();

        result.AsFloat64().Should().Be(42.0);
    }

    // ============================================================================
    // Quantile Tests
    // ============================================================================

    [Fact]
    public void Quantile_Median_ReturnsMedian()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 });
        var result = series.Quantile(0.5);

        result.AsFloat64().Should().Be(3.0);
    }

    [Fact]
    public void Quantile_Q0_ReturnsMin()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 });
        var result = series.Quantile(0.0);

        result.AsFloat64().Should().Be(1.0);
    }

    [Fact]
    public void Quantile_Q1_ReturnsMax()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 });
        var result = series.Quantile(1.0);

        result.AsFloat64().Should().Be(5.0);
    }

    [Fact]
    public void Quantile_Q25_ReturnsFirstQuartile()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 });
        var result = series.Quantile(0.25);

        result.AsFloat64().Should().BeApproximately(2.0, 0.1);
    }

    // ============================================================================
    // Var Tests
    // ============================================================================

    [Fact]
    public void Var_Values_ReturnsVariance()
    {
        var series = Series.FromValues("s", new[] { 2.0, 4.0, 4.0, 4.0, 5.0, 5.0, 7.0, 9.0 });
        var result = series.Var();

        // Sample variance = 4.571...
        result.TryGetDouble(out var value).Should().BeTrue();
        value.Should().BeApproximately(4.571, 0.01);
    }

    [Fact]
    public void Var_ConstantValues_ReturnsZero()
    {
        var series = Series.FromValues("s", new[] { 5.0, 5.0, 5.0, 5.0 });
        var result = series.Var();

        result.TryGetDouble(out var value).Should().BeTrue();
        value.Should().BeApproximately(0.0, 0.0001);
    }

    // ============================================================================
    // ArgSort Tests
    // ============================================================================

    [Fact]
    public void ArgSort_Values_ReturnsSortedIndices()
    {
        var series = Series.FromValues("s", new[] { 30.0, 10.0, 20.0 });
        var result = series.ArgSort();

        // ArgSort returns Int32 indices
        result[0].AsInt32().Should().Be(1);  // Index of 10
        result[1].AsInt32().Should().Be(2);  // Index of 20
        result[2].AsInt32().Should().Be(0);  // Index of 30
    }

    [Fact]
    public void ArgSort_Descending_ReturnsSortedIndices()
    {
        var series = Series.FromValues("s", new[] { 30.0, 10.0, 20.0 });
        var result = series.ArgSort(descending: true);

        // ArgSort returns Int32 indices
        result[0].AsInt32().Should().Be(0);  // Index of 30
        result[1].AsInt32().Should().Be(2);  // Index of 20
        result[2].AsInt32().Should().Be(1);  // Index of 10
    }

    // ============================================================================
    // Unique Tests
    // ============================================================================

    [Fact]
    public void Unique_DuplicateValues_ReturnsUnique()
    {
        var series = Series.FromValues("s", new[] { 1, 2, 2, 3, 3, 3, 4 });
        var result = series.Unique();

        result.Length.Should().Be(4);
    }

    [Fact]
    public void Unique_AllSame_ReturnsSingleElement()
    {
        var series = Series.FromValues("s", new[] { 5, 5, 5, 5, 5 });
        var result = series.Unique();

        result.Length.Should().Be(1);
        result[0].AsInt32().Should().Be(5);
    }

    [Fact]
    public void Unique_AllDifferent_ReturnsSameLength()
    {
        var series = Series.FromValues("s", new[] { 1, 2, 3, 4, 5 });
        var result = series.Unique();

        result.Length.Should().Be(5);
    }

    [Fact]
    public void Unique_Strings_ReturnsUniqueStrings()
    {
        var series = Series.FromValues("s", new[] { "a", "b", "a", "c", "b" });
        var result = series.Unique();

        result.Length.Should().Be(3);
    }

    // ============================================================================
    // DropNulls Tests
    // ============================================================================

    [Fact]
    public void DropNulls_RemovesNulls()
    {
        var series = Series.FromNullable("s", new int?[] { 1, null, 3, null, 5 });
        var result = series.DropNulls();

        result.Length.Should().Be(3);
        result.NullCount.Should().Be(0);
        result[0].AsInt32().Should().Be(1);
        result[1].AsInt32().Should().Be(3);
        result[2].AsInt32().Should().Be(5);
    }

    [Fact]
    public void DropNulls_NoNulls_ReturnsSameLength()
    {
        var series = Series.FromValues("s", new[] { 1, 2, 3 });
        var result = series.DropNulls();

        result.Length.Should().Be(3);
    }

    [Fact]
    public void DropNulls_AllNulls_ReturnsEmpty()
    {
        var series = Series.FromNullable("s", new int?[] { null, null, null });
        var result = series.DropNulls();

        result.Length.Should().Be(0);
    }

    // ============================================================================
    // FillNull Tests
    // ============================================================================

    [Fact]
    public void FillNull_ReplacesNulls()
    {
        var series = Series.FromNullable("s", new int?[] { 1, null, 3, null, 5 });
        var result = series.FillNull(AnyValue.From(0));

        result.NullCount.Should().Be(0);
        result[1].AsInt32().Should().Be(0);
        result[3].AsInt32().Should().Be(0);
    }

    [Fact]
    public void FillNull_NoNulls_ReturnsSame()
    {
        var series = Series.FromValues("s", new[] { 1, 2, 3 });
        var result = series.FillNull(AnyValue.From(0));

        result[0].AsInt32().Should().Be(1);
        result[1].AsInt32().Should().Be(2);
        result[2].AsInt32().Should().Be(3);
    }

    // ============================================================================
    // Cast Tests
    // ============================================================================

    [Fact]
    public void Cast_IntToFloat_ConvertsCorrectly()
    {
        var series = Series.FromValues("s", new[] { 1, 2, 3 });
        var result = series.Cast(DataType.Float64);

        result.DataType.Should().Be(DataType.Float64);
        result[0].AsFloat64().Should().Be(1.0);
        result[1].AsFloat64().Should().Be(2.0);
        result[2].AsFloat64().Should().Be(3.0);
    }

    [Fact]
    public void Cast_FloatToInt_TruncatesCorrectly()
    {
        var series = Series.FromValues("s", new[] { 1.9, 2.1, 3.5 });
        var result = series.Cast(DataType.Int32);

        result.DataType.Should().Be(DataType.Int32);
        result[0].AsInt32().Should().Be(1);
        result[1].AsInt32().Should().Be(2);
        result[2].AsInt32().Should().Be(3);
    }

    [Fact]
    public void Cast_IntToString_ConvertsCorrectly()
    {
        var series = Series.FromValues("s", new[] { 1, 2, 3 });
        var result = series.Cast(DataType.String);

        result.DataType.Should().Be(DataType.String);
        result[0].AsString().Should().Be("1");
        result[1].AsString().Should().Be("2");
        result[2].AsString().Should().Be("3");
    }

    // ============================================================================
    // Slice Tests
    // ============================================================================

    [Fact]
    public void Slice_MiddlePortion_ReturnsCorrectSlice()
    {
        var series = Series.FromValues("s", new[] { 1, 2, 3, 4, 5 });
        var result = series.Slice(1, 3);

        result.Length.Should().Be(3);
        result[0].AsInt32().Should().Be(2);
        result[1].AsInt32().Should().Be(3);
        result[2].AsInt32().Should().Be(4);
    }

    [Fact]
    public void Slice_FromStart_ReturnsCorrectSlice()
    {
        var series = Series.FromValues("s", new[] { 1, 2, 3, 4, 5 });
        var result = series.Slice(0, 2);

        result.Length.Should().Be(2);
        result[0].AsInt32().Should().Be(1);
        result[1].AsInt32().Should().Be(2);
    }

    [Fact]
    public void Slice_ToEnd_ReturnsCorrectSlice()
    {
        var series = Series.FromValues("s", new[] { 1, 2, 3, 4, 5 });
        var result = series.Slice(3, 2);

        result.Length.Should().Be(2);
        result[0].AsInt32().Should().Be(4);
        result[1].AsInt32().Should().Be(5);
    }

    // ============================================================================
    // Head/Tail Tests
    // ============================================================================

    [Fact]
    public void Head_ReturnsFirstN()
    {
        var series = Series.FromValues("s", new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
        var result = series.Head(3);

        result.Length.Should().Be(3);
        result[0].AsInt32().Should().Be(1);
        result[2].AsInt32().Should().Be(3);
    }

    [Fact]
    public void Head_MoreThanLength_ReturnsAll()
    {
        var series = Series.FromValues("s", new[] { 1, 2, 3 });
        var result = series.Head(10);

        result.Length.Should().Be(3);
    }

    [Fact]
    public void Tail_ReturnsLastN()
    {
        var series = Series.FromValues("s", new[] { 1, 2, 3, 4, 5 });
        var result = series.Tail(2);

        result.Length.Should().Be(2);
        result[0].AsInt32().Should().Be(4);
        result[1].AsInt32().Should().Be(5);
    }

    [Fact]
    public void Tail_MoreThanLength_ReturnsAll()
    {
        var series = Series.FromValues("s", new[] { 1, 2, 3 });
        var result = series.Tail(10);

        result.Length.Should().Be(3);
    }
}
