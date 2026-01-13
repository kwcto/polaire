// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using FluentAssertions;
using Polaire;
using Polaire.DataTypes;
using Xunit;

namespace Polaire.Tests;

/// <summary>
/// Tests for arithmetic operations (+, -, *, /, %, unary negation, scalar operations).
/// Note: Binary operations only support Int64 and Float64 types (not Int32).
/// </summary>
public class ArithmeticOperationsTests
{
    // ============================================================================
    // Series-to-Series Addition Tests
    // ============================================================================

    [Fact]
    public void Add_Int64Series_ReturnsCorrectResults()
    {
        var a = Series.FromValues("a", new long[] { 1, 2, 3, 4, 5 });
        var b = Series.FromValues("b", new long[] { 10, 20, 30, 40, 50 });

        var result = a + b;

        result.Length.Should().Be(5);
        result[0].AsInt64().Should().Be(11);
        result[1].AsInt64().Should().Be(22);
        result[2].AsInt64().Should().Be(33);
        result[3].AsInt64().Should().Be(44);
        result[4].AsInt64().Should().Be(55);
    }

    [Fact]
    public void Add_Float64Series_ReturnsCorrectResults()
    {
        var a = Series.FromValues("a", new[] { 1.5, 2.5, 3.5 });
        var b = Series.FromValues("b", new[] { 0.5, 0.5, 0.5 });

        var result = a + b;

        result[0].AsFloat64().Should().BeApproximately(2.0, 0.001);
        result[1].AsFloat64().Should().BeApproximately(3.0, 0.001);
        result[2].AsFloat64().Should().BeApproximately(4.0, 0.001);
    }

    // ============================================================================
    // Series-to-Series Subtraction Tests
    // ============================================================================

    [Fact]
    public void Subtract_Int64Series_ReturnsCorrectResults()
    {
        var a = Series.FromValues("a", new long[] { 10, 20, 30 });
        var b = Series.FromValues("b", new long[] { 1, 2, 3 });

        var result = a - b;

        result[0].AsInt64().Should().Be(9);
        result[1].AsInt64().Should().Be(18);
        result[2].AsInt64().Should().Be(27);
    }

    [Fact]
    public void Subtract_Float64Series_ReturnsCorrectResults()
    {
        var a = Series.FromValues("a", new[] { 5.0, 10.0, 15.0 });
        var b = Series.FromValues("b", new[] { 2.5, 5.0, 7.5 });

        var result = a - b;

        result[0].AsFloat64().Should().BeApproximately(2.5, 0.001);
        result[1].AsFloat64().Should().BeApproximately(5.0, 0.001);
        result[2].AsFloat64().Should().BeApproximately(7.5, 0.001);
    }

    [Fact]
    public void Subtract_NegativeResult_HandlesCorrectly()
    {
        var a = Series.FromValues("a", new long[] { 1, 2, 3 });
        var b = Series.FromValues("b", new long[] { 10, 10, 10 });

        var result = a - b;

        result[0].AsInt64().Should().Be(-9);
        result[1].AsInt64().Should().Be(-8);
        result[2].AsInt64().Should().Be(-7);
    }

    // ============================================================================
    // Series-to-Series Multiplication Tests
    // ============================================================================

    [Fact]
    public void Multiply_Int64Series_ReturnsCorrectResults()
    {
        var a = Series.FromValues("a", new long[] { 2, 3, 4 });
        var b = Series.FromValues("b", new long[] { 5, 6, 7 });

        var result = a * b;

        result[0].AsInt64().Should().Be(10);
        result[1].AsInt64().Should().Be(18);
        result[2].AsInt64().Should().Be(28);
    }

    [Fact]
    public void Multiply_Float64Series_ReturnsCorrectResults()
    {
        var a = Series.FromValues("a", new[] { 2.0, 3.0, 4.0 });
        var b = Series.FromValues("b", new[] { 0.5, 0.5, 0.5 });

        var result = a * b;

        result[0].AsFloat64().Should().BeApproximately(1.0, 0.001);
        result[1].AsFloat64().Should().BeApproximately(1.5, 0.001);
        result[2].AsFloat64().Should().BeApproximately(2.0, 0.001);
    }

    [Fact]
    public void Multiply_WithZero_ReturnsZeros()
    {
        var a = Series.FromValues("a", new long[] { 100, 200, 300 });
        var b = Series.FromValues("b", new long[] { 0, 0, 0 });

        var result = a * b;

        result[0].AsInt64().Should().Be(0);
        result[1].AsInt64().Should().Be(0);
        result[2].AsInt64().Should().Be(0);
    }

    // ============================================================================
    // Series-to-Series Division Tests
    // ============================================================================

    [Fact]
    public void Divide_Float64Series_ReturnsCorrectResults()
    {
        var a = Series.FromValues("a", new[] { 10.0, 20.0, 30.0 });
        var b = Series.FromValues("b", new[] { 2.0, 4.0, 5.0 });

        var result = a / b;

        result[0].AsFloat64().Should().BeApproximately(5.0, 0.001);
        result[1].AsFloat64().Should().BeApproximately(5.0, 0.001);
        result[2].AsFloat64().Should().BeApproximately(6.0, 0.001);
    }

    [Fact]
    public void Divide_Int64Series_ReturnsIntegerResult()
    {
        var a = Series.FromValues("a", new long[] { 10, 20, 30 });
        var b = Series.FromValues("b", new long[] { 2, 4, 5 });

        var result = a / b;

        // Integer division
        result[0].AsInt64().Should().Be(5);
        result[1].AsInt64().Should().Be(5);
        result[2].AsInt64().Should().Be(6);
    }

    [Fact]
    public void Divide_ByZero_ReturnsInfinity()
    {
        var a = Series.FromValues("a", new[] { 1.0, -1.0, 0.0 });
        var b = Series.FromValues("b", new[] { 0.0, 0.0, 0.0 });

        var result = a / b;

        double.IsPositiveInfinity(result[0].AsFloat64()).Should().BeTrue();
        double.IsNegativeInfinity(result[1].AsFloat64()).Should().BeTrue();
        double.IsNaN(result[2].AsFloat64()).Should().BeTrue(); // 0/0 = NaN
    }

    // ============================================================================
    // Series-to-Series Modulo Tests
    // ============================================================================

    [Fact]
    public void Modulo_Int64Series_ReturnsCorrectResults()
    {
        var a = Series.FromValues("a", new long[] { 10, 11, 12, 13, 14 });
        var b = Series.FromValues("b", new long[] { 3, 3, 3, 3, 3 });

        var result = a % b;

        result[0].AsInt64().Should().Be(1);
        result[1].AsInt64().Should().Be(2);
        result[2].AsInt64().Should().Be(0);
        result[3].AsInt64().Should().Be(1);
        result[4].AsInt64().Should().Be(2);
    }

    [Fact]
    public void Modulo_Float64Series_ReturnsCorrectResults()
    {
        var a = Series.FromValues("a", new[] { 5.5, 6.5, 7.5 });
        var b = Series.FromValues("b", new[] { 2.0, 2.0, 2.0 });

        var result = a % b;

        result[0].AsFloat64().Should().BeApproximately(1.5, 0.001);
        result[1].AsFloat64().Should().BeApproximately(0.5, 0.001);
        result[2].AsFloat64().Should().BeApproximately(1.5, 0.001);
    }

    // ============================================================================
    // Scalar Addition Tests
    // ============================================================================

    [Fact]
    public void AddScalar_ReturnsCorrectResults()
    {
        var series = Series.FromValues("a", new[] { 1.0, 2.0, 3.0 });

        var result = series + 10.0;

        result[0].AsFloat64().Should().BeApproximately(11.0, 0.001);
        result[1].AsFloat64().Should().BeApproximately(12.0, 0.001);
        result[2].AsFloat64().Should().BeApproximately(13.0, 0.001);
    }

    [Fact]
    public void AddScalar_Zero_ReturnsOriginal()
    {
        var series = Series.FromValues("a", new[] { 1.0, 2.0, 3.0 });

        var result = series + 0.0;

        result[0].AsFloat64().Should().BeApproximately(1.0, 0.001);
        result[1].AsFloat64().Should().BeApproximately(2.0, 0.001);
        result[2].AsFloat64().Should().BeApproximately(3.0, 0.001);
    }

    [Fact]
    public void AddScalar_Negative_ReturnsCorrectResults()
    {
        var series = Series.FromValues("a", new[] { 10.0, 20.0, 30.0 });

        var result = series + (-5.0);

        result[0].AsFloat64().Should().BeApproximately(5.0, 0.001);
        result[1].AsFloat64().Should().BeApproximately(15.0, 0.001);
        result[2].AsFloat64().Should().BeApproximately(25.0, 0.001);
    }

    // ============================================================================
    // Scalar Subtraction Tests
    // ============================================================================

    [Fact]
    public void SubtractScalar_ReturnsCorrectResults()
    {
        var series = Series.FromValues("a", new[] { 10.0, 20.0, 30.0 });

        var result = series - 5.0;

        result[0].AsFloat64().Should().BeApproximately(5.0, 0.001);
        result[1].AsFloat64().Should().BeApproximately(15.0, 0.001);
        result[2].AsFloat64().Should().BeApproximately(25.0, 0.001);
    }

    // ============================================================================
    // Scalar Multiplication Tests
    // ============================================================================

    [Fact]
    public void MultiplyScalar_ReturnsCorrectResults()
    {
        var series = Series.FromValues("a", new[] { 1.0, 2.0, 3.0 });

        var result = series * 10.0;

        result[0].AsFloat64().Should().BeApproximately(10.0, 0.001);
        result[1].AsFloat64().Should().BeApproximately(20.0, 0.001);
        result[2].AsFloat64().Should().BeApproximately(30.0, 0.001);
    }

    [Fact]
    public void MultiplyScalar_Zero_ReturnsZeros()
    {
        var series = Series.FromValues("a", new[] { 100.0, 200.0, 300.0 });

        var result = series * 0.0;

        result[0].AsFloat64().Should().Be(0.0);
        result[1].AsFloat64().Should().Be(0.0);
        result[2].AsFloat64().Should().Be(0.0);
    }

    [Fact]
    public void MultiplyScalar_Negative_ReturnsCorrectResults()
    {
        var series = Series.FromValues("a", new[] { 1.0, 2.0, 3.0 });

        var result = series * -2.0;

        result[0].AsFloat64().Should().BeApproximately(-2.0, 0.001);
        result[1].AsFloat64().Should().BeApproximately(-4.0, 0.001);
        result[2].AsFloat64().Should().BeApproximately(-6.0, 0.001);
    }

    // ============================================================================
    // Scalar Division Tests
    // ============================================================================

    [Fact]
    public void DivideScalar_ReturnsCorrectResults()
    {
        var series = Series.FromValues("a", new[] { 10.0, 20.0, 30.0 });

        var result = series / 5.0;

        result[0].AsFloat64().Should().BeApproximately(2.0, 0.001);
        result[1].AsFloat64().Should().BeApproximately(4.0, 0.001);
        result[2].AsFloat64().Should().BeApproximately(6.0, 0.001);
    }

    [Fact]
    public void DivideScalar_ByZero_ReturnsInfinity()
    {
        var series = Series.FromValues("a", new[] { 1.0, -1.0, 0.0 });

        var result = series / 0.0;

        double.IsPositiveInfinity(result[0].AsFloat64()).Should().BeTrue();
        double.IsNegativeInfinity(result[1].AsFloat64()).Should().BeTrue();
        double.IsNaN(result[2].AsFloat64()).Should().BeTrue();
    }

    // ============================================================================
    // Unary Negation Tests
    // ============================================================================

    [Fact]
    public void Negate_Int32Series_ReturnsCorrectResults()
    {
        var series = Series.FromValues("a", new[] { 1, -2, 3, -4, 0 });

        var result = -series;

        result[0].AsInt32().Should().Be(-1);
        result[1].AsInt32().Should().Be(2);
        result[2].AsInt32().Should().Be(-3);
        result[3].AsInt32().Should().Be(4);
        result[4].AsInt32().Should().Be(0);
    }

    [Fact]
    public void Negate_Float64Series_ReturnsCorrectResults()
    {
        var series = Series.FromValues("a", new[] { 1.5, -2.5, 0.0 });

        var result = -series;

        result[0].AsFloat64().Should().BeApproximately(-1.5, 0.001);
        result[1].AsFloat64().Should().BeApproximately(2.5, 0.001);
        result[2].AsFloat64().Should().BeApproximately(0.0, 0.001);
    }

    [Fact]
    public void Negate_DoubleNegation_ReturnsOriginal()
    {
        var series = Series.FromValues("a", new[] { 1, 2, 3 });

        var result = -(-series);

        result[0].AsInt32().Should().Be(1);
        result[1].AsInt32().Should().Be(2);
        result[2].AsInt32().Should().Be(3);
    }

    // ============================================================================
    // Chained Operations Tests
    // ============================================================================

    [Fact]
    public void ChainedAddition_WorksCorrectly()
    {
        var a = Series.FromValues("a", new[] { 1.0, 2.0, 3.0 });
        var b = Series.FromValues("b", new[] { 10.0, 20.0, 30.0 });
        var c = Series.FromValues("c", new[] { 100.0, 200.0, 300.0 });

        var result = a + b + c;

        result[0].AsFloat64().Should().BeApproximately(111.0, 0.001);
        result[1].AsFloat64().Should().BeApproximately(222.0, 0.001);
        result[2].AsFloat64().Should().BeApproximately(333.0, 0.001);
    }

    [Fact]
    public void ChainedMixedOperations_WorksCorrectly()
    {
        var a = Series.FromValues("a", new[] { 10.0, 20.0, 30.0 });
        var b = Series.FromValues("b", new[] { 2.0, 4.0, 6.0 });

        var result = (a + b) * 2.0;

        result[0].AsFloat64().Should().BeApproximately(24.0, 0.001);
        result[1].AsFloat64().Should().BeApproximately(48.0, 0.001);
        result[2].AsFloat64().Should().BeApproximately(72.0, 0.001);
    }

    [Fact]
    public void MathExpression_PEMDAS_WorksCorrectly()
    {
        var a = Series.FromValues("a", new[] { 2.0, 4.0, 6.0 });
        var b = Series.FromValues("b", new[] { 3.0, 3.0, 3.0 });
        var c = Series.FromValues("c", new[] { 1.0, 1.0, 1.0 });

        // a * b + c = 2*3+1=7, 4*3+1=13, 6*3+1=19
        var result = a * b + c;

        result[0].AsFloat64().Should().BeApproximately(7.0, 0.001);
        result[1].AsFloat64().Should().BeApproximately(13.0, 0.001);
        result[2].AsFloat64().Should().BeApproximately(19.0, 0.001);
    }

    // ============================================================================
    // Edge Cases
    // ============================================================================

    [Fact]
    public void Arithmetic_EmptySeries_ReturnsEmpty()
    {
        var a = Series.FromValues("a", Array.Empty<double>());
        var b = Series.FromValues("b", Array.Empty<double>());

        var result = a + b;

        result.Length.Should().Be(0);
    }

    [Fact]
    public void Arithmetic_SingleElement_WorksCorrectly()
    {
        var a = Series.FromValues("a", new[] { 5.0 });
        var b = Series.FromValues("b", new[] { 3.0 });

        (a + b)[0].AsFloat64().Should().BeApproximately(8.0, 0.001);
        (a - b)[0].AsFloat64().Should().BeApproximately(2.0, 0.001);
        (a * b)[0].AsFloat64().Should().BeApproximately(15.0, 0.001);
    }

    [Fact]
    public void Arithmetic_LargeSeries_WorksCorrectly()
    {
        var size = 10000;
        var a = Series.FromValues("a", Enumerable.Range(0, size).Select(i => (double)i).ToArray());
        var b = Series.FromValues("b", Enumerable.Range(0, size).Select(i => (double)i).ToArray());

        var result = a + b;

        result.Length.Should().Be(size);
        result[0].AsFloat64().Should().Be(0.0);
        result[9999].AsFloat64().Should().Be(19998.0);
    }

    [Fact]
    public void Arithmetic_WithSpecialValues_HandlesCorrectly()
    {
        var a = Series.FromValues("a", new[] { double.MaxValue, double.MinValue, double.Epsilon });
        var b = Series.FromValues("b", new[] { 1.0, 1.0, 1.0 });

        var result = a + b;

        result[0].AsFloat64().Should().Be(double.MaxValue); // Overflow to same value
        result[1].AsFloat64().Should().Be(double.MinValue + 1); // Very close to MinValue
    }

    [Fact]
    public void Arithmetic_WithNaN_PropagatesNaN()
    {
        var a = Series.FromValues("a", new[] { 1.0, double.NaN, 3.0 });
        var b = Series.FromValues("b", new[] { 1.0, 2.0, 3.0 });

        var result = a + b;

        result[0].AsFloat64().Should().Be(2.0);
        double.IsNaN(result[1].AsFloat64()).Should().BeTrue();
        result[2].AsFloat64().Should().Be(6.0);
    }

    [Fact]
    public void Arithmetic_WithInfinity_HandlesCorrectly()
    {
        var a = Series.FromValues("a", new[] { double.PositiveInfinity, double.NegativeInfinity });
        var b = Series.FromValues("b", new[] { 1.0, 1.0 });

        var add = a + b;
        var sub = a - b;
        var mul = a * b;

        double.IsPositiveInfinity(add[0].AsFloat64()).Should().BeTrue();
        double.IsNegativeInfinity(add[1].AsFloat64()).Should().BeTrue();

        double.IsPositiveInfinity(sub[0].AsFloat64()).Should().BeTrue();
        double.IsNegativeInfinity(sub[1].AsFloat64()).Should().BeTrue();

        double.IsPositiveInfinity(mul[0].AsFloat64()).Should().BeTrue();
        double.IsNegativeInfinity(mul[1].AsFloat64()).Should().BeTrue();
    }

    // ============================================================================
    // Mathematical Functions Tests
    // ============================================================================

    [Fact]
    public void Abs_ReturnsAbsoluteValues()
    {
        var series = Series.FromValues("a", new[] { -1.0, 2.0, -3.0, 0.0 });

        var result = series.Abs();

        result[0].AsFloat64().Should().BeApproximately(1.0, 0.001);
        result[1].AsFloat64().Should().BeApproximately(2.0, 0.001);
        result[2].AsFloat64().Should().BeApproximately(3.0, 0.001);
        result[3].AsFloat64().Should().BeApproximately(0.0, 0.001);
    }

    [Fact]
    public void Sqrt_ReturnsSquareRoots()
    {
        var series = Series.FromValues("a", new[] { 1.0, 4.0, 9.0, 16.0 });

        var result = series.Sqrt();

        result[0].AsFloat64().Should().BeApproximately(1.0, 0.001);
        result[1].AsFloat64().Should().BeApproximately(2.0, 0.001);
        result[2].AsFloat64().Should().BeApproximately(3.0, 0.001);
        result[3].AsFloat64().Should().BeApproximately(4.0, 0.001);
    }

    [Fact]
    public void Sqrt_NegativeValue_ReturnsNaN()
    {
        var series = Series.FromValues("a", new[] { -1.0 });

        var result = series.Sqrt();

        double.IsNaN(result[0].AsFloat64()).Should().BeTrue();
    }

    [Fact]
    public void Log_ReturnsNaturalLogarithm()
    {
        var series = Series.FromValues("a", new[] { 1.0, Math.E, Math.E * Math.E });

        var result = series.Log();

        result[0].AsFloat64().Should().BeApproximately(0.0, 0.001);
        result[1].AsFloat64().Should().BeApproximately(1.0, 0.001);
        result[2].AsFloat64().Should().BeApproximately(2.0, 0.001);
    }

    [Fact]
    public void Log10_ReturnsBase10Logarithm()
    {
        var series = Series.FromValues("a", new[] { 1.0, 10.0, 100.0 });

        var result = series.Log10();

        result[0].AsFloat64().Should().BeApproximately(0.0, 0.001);
        result[1].AsFloat64().Should().BeApproximately(1.0, 0.001);
        result[2].AsFloat64().Should().BeApproximately(2.0, 0.001);
    }

    [Fact]
    public void Exp_ReturnsExponential()
    {
        var series = Series.FromValues("a", new[] { 0.0, 1.0, 2.0 });

        var result = series.Exp();

        result[0].AsFloat64().Should().BeApproximately(1.0, 0.001);
        result[1].AsFloat64().Should().BeApproximately(Math.E, 0.001);
        result[2].AsFloat64().Should().BeApproximately(Math.E * Math.E, 0.001);
    }

    [Fact]
    public void Floor_ReturnsFloorValues()
    {
        var series = Series.FromValues("a", new[] { 1.1, 2.5, 3.9, -1.5 });

        var result = series.Floor();

        result[0].AsFloat64().Should().Be(1.0);
        result[1].AsFloat64().Should().Be(2.0);
        result[2].AsFloat64().Should().Be(3.0);
        result[3].AsFloat64().Should().Be(-2.0);
    }

    [Fact]
    public void Ceil_ReturnsCeilingValues()
    {
        var series = Series.FromValues("a", new[] { 1.1, 2.5, 3.9, -1.5 });

        var result = series.Ceil();

        result[0].AsFloat64().Should().Be(2.0);
        result[1].AsFloat64().Should().Be(3.0);
        result[2].AsFloat64().Should().Be(4.0);
        result[3].AsFloat64().Should().Be(-1.0);
    }

    [Fact]
    public void Round_ReturnsRoundedValues()
    {
        var series = Series.FromValues("a", new[] { 1.4, 1.5, 1.6, 2.5 });

        var result = series.Round();

        result[0].AsFloat64().Should().Be(1.0);
        result[1].AsFloat64().Should().Be(2.0); // Banker's rounding or standard
        result[2].AsFloat64().Should().Be(2.0);
    }

    [Fact]
    public void Round_WithDecimals_ReturnsCorrectPrecision()
    {
        var series = Series.FromValues("a", new[] { 1.234, 2.567, 3.891 });

        var result = series.Round(2);

        result[0].AsFloat64().Should().BeApproximately(1.23, 0.001);
        result[1].AsFloat64().Should().BeApproximately(2.57, 0.001);
        result[2].AsFloat64().Should().BeApproximately(3.89, 0.001);
    }

    [Fact]
    public void Sign_ReturnsSignOfValues()
    {
        var series = Series.FromValues("a", new[] { -5.0, 0.0, 5.0 });

        var result = series.Sign();

        result[0].AsInt32().Should().Be(-1);
        result[1].AsInt32().Should().Be(0);
        result[2].AsInt32().Should().Be(1);
    }

    [Fact]
    public void Clip_ClipsToRange()
    {
        var series = Series.FromValues("a", new[] { -10.0, 0.0, 5.0, 10.0, 20.0 });

        var result = series.Clip(0.0, 10.0);

        result[0].AsFloat64().Should().Be(0.0);
        result[1].AsFloat64().Should().Be(0.0);
        result[2].AsFloat64().Should().Be(5.0);
        result[3].AsFloat64().Should().Be(10.0);
        result[4].AsFloat64().Should().Be(10.0);
    }

    [Fact]
    public void Clip_LowerOnly_ClipsLower()
    {
        var series = Series.FromValues("a", new[] { -10.0, 0.0, 10.0 });

        var result = series.Clip(0.0, null);

        result[0].AsFloat64().Should().Be(0.0);
        result[1].AsFloat64().Should().Be(0.0);
        result[2].AsFloat64().Should().Be(10.0);
    }

    [Fact]
    public void Clip_UpperOnly_ClipsUpper()
    {
        var series = Series.FromValues("a", new[] { -10.0, 0.0, 10.0 });

        var result = series.Clip(null, 5.0);

        result[0].AsFloat64().Should().Be(-10.0);
        result[1].AsFloat64().Should().Be(0.0);
        result[2].AsFloat64().Should().Be(5.0);
    }

    // ============================================================================
    // Trigonometric Functions Tests
    // ============================================================================

    [Fact]
    public void Sin_ReturnsCorrectValues()
    {
        var series = Series.FromValues("a", new[] { 0.0, Math.PI / 2, Math.PI });

        var result = series.Sin();

        result[0].AsFloat64().Should().BeApproximately(0.0, 0.001);
        result[1].AsFloat64().Should().BeApproximately(1.0, 0.001);
        result[2].AsFloat64().Should().BeApproximately(0.0, 0.001);
    }

    [Fact]
    public void Cos_ReturnsCorrectValues()
    {
        var series = Series.FromValues("a", new[] { 0.0, Math.PI / 2, Math.PI });

        var result = series.Cos();

        result[0].AsFloat64().Should().BeApproximately(1.0, 0.001);
        result[1].AsFloat64().Should().BeApproximately(0.0, 0.001);
        result[2].AsFloat64().Should().BeApproximately(-1.0, 0.001);
    }

    [Fact]
    public void Tan_ReturnsCorrectValues()
    {
        var series = Series.FromValues("a", new[] { 0.0, Math.PI / 4 });

        var result = series.Tan();

        result[0].AsFloat64().Should().BeApproximately(0.0, 0.001);
        result[1].AsFloat64().Should().BeApproximately(1.0, 0.001);
    }
}
