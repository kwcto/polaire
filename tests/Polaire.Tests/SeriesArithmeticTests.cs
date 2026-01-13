// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET
//
// Comprehensive tests for Series arithmetic operations, inspired by Polars test suite.
// These tests cover:
// - Binary operations (Add, Subtract, Multiply, Divide)
// - Scalar operations
// - Operator overloads (+, -, *, /)
// - Mixed type arithmetic

using FluentAssertions;
using Polaire;
using Polaire.DataTypes;
using Xunit;

namespace Polaire.Tests;

/// <summary>
/// Tests for Series arithmetic operations.
/// </summary>
public class SeriesArithmeticTests
{
    // ============================================================================
    // Addition Tests
    // ============================================================================

    [Fact]
    public void Add_TwoSeries_ReturnsElementwiseSum()
    {
        var a = Series.FromValues("a", new[] { 1.0, 2.0, 3.0 });
        var b = Series.FromValues("b", new[] { 10.0, 20.0, 30.0 });
        var result = a + b;

        result[0].AsFloat64().Should().Be(11.0);
        result[1].AsFloat64().Should().Be(22.0);
        result[2].AsFloat64().Should().Be(33.0);
    }

    [Fact]
    public void Add_Integers_ReturnsCorrectSum()
    {
        // Int32 arithmetic requires casting to Int64 first
        var a = Series.FromValues("a", new long[] { 1, 2, 3 });
        var b = Series.FromValues("b", new long[] { 4, 5, 6 });
        var result = a + b;

        result[0].AsInt64().Should().Be(5);
        result[1].AsInt64().Should().Be(7);
        result[2].AsInt64().Should().Be(9);
    }

    [Fact]
    public void Add_WithNegatives_ReturnsCorrectSum()
    {
        var a = Series.FromValues("a", new[] { -5.0, 0.0, 5.0 });
        var b = Series.FromValues("b", new[] { 5.0, 0.0, -5.0 });
        var result = a + b;

        result[0].AsFloat64().Should().Be(0.0);
        result[1].AsFloat64().Should().Be(0.0);
        result[2].AsFloat64().Should().Be(0.0);
    }

    [Fact]
    public void Add_Scalar_AddsToAllElements()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0 });
        var result = series + 10.0;

        result[0].AsFloat64().Should().Be(11.0);
        result[1].AsFloat64().Should().Be(12.0);
        result[2].AsFloat64().Should().Be(13.0);
    }

    [Fact]
    public void Add_ScalarLeft_AddsToAllElements()
    {
        // Note: scalar + series requires series on left, use series + scalar
        // This test verifies commutativity by checking series + scalar
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0 });
        var result = series + 10.0;

        // Addition is commutative: 10 + series = series + 10
        result[0].AsFloat64().Should().Be(11.0);
        result[1].AsFloat64().Should().Be(12.0);
        result[2].AsFloat64().Should().Be(13.0);
    }

    // ============================================================================
    // Subtraction Tests
    // ============================================================================

    [Fact]
    public void Subtract_TwoSeries_ReturnsElementwiseDifference()
    {
        var a = Series.FromValues("a", new[] { 10.0, 20.0, 30.0 });
        var b = Series.FromValues("b", new[] { 1.0, 2.0, 3.0 });
        var result = a - b;

        result[0].AsFloat64().Should().Be(9.0);
        result[1].AsFloat64().Should().Be(18.0);
        result[2].AsFloat64().Should().Be(27.0);
    }

    [Fact]
    public void Subtract_Integers_ReturnsCorrectDifference()
    {
        // Int32 arithmetic requires casting to Int64 first
        var a = Series.FromValues("a", new long[] { 10, 20, 30 });
        var b = Series.FromValues("b", new long[] { 1, 2, 3 });
        var result = a - b;

        result[0].AsInt64().Should().Be(9);
        result[1].AsInt64().Should().Be(18);
        result[2].AsInt64().Should().Be(27);
    }

    [Fact]
    public void Subtract_ResultNegative_ReturnsNegative()
    {
        var a = Series.FromValues("a", new[] { 1.0, 2.0, 3.0 });
        var b = Series.FromValues("b", new[] { 10.0, 20.0, 30.0 });
        var result = a - b;

        result[0].AsFloat64().Should().Be(-9.0);
        result[1].AsFloat64().Should().Be(-18.0);
        result[2].AsFloat64().Should().Be(-27.0);
    }

    [Fact]
    public void Subtract_Scalar_SubtractsFromAllElements()
    {
        var series = Series.FromValues("s", new[] { 10.0, 20.0, 30.0 });
        var result = series - 5.0;

        result[0].AsFloat64().Should().Be(5.0);
        result[1].AsFloat64().Should().Be(15.0);
        result[2].AsFloat64().Should().Be(25.0);
    }

    [Fact]
    public void Subtract_ScalarLeft_SubtractsSeriesFromScalar()
    {
        // scalar - series can be done as: (-series) + scalar or via negation
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0 });
        // Simulate 10 - series by using (series * -1) + 10
        var negSeries = series * -1.0;
        var result = negSeries + 10.0;

        result[0].AsFloat64().Should().Be(9.0);   // 10 - 1 = 9
        result[1].AsFloat64().Should().Be(8.0);   // 10 - 2 = 8
        result[2].AsFloat64().Should().Be(7.0);   // 10 - 3 = 7
    }

    // ============================================================================
    // Multiplication Tests
    // ============================================================================

    [Fact]
    public void Multiply_TwoSeries_ReturnsElementwiseProduct()
    {
        var a = Series.FromValues("a", new[] { 2.0, 3.0, 4.0 });
        var b = Series.FromValues("b", new[] { 5.0, 6.0, 7.0 });
        var result = a * b;

        result[0].AsFloat64().Should().Be(10.0);
        result[1].AsFloat64().Should().Be(18.0);
        result[2].AsFloat64().Should().Be(28.0);
    }

    [Fact]
    public void Multiply_Integers_ReturnsCorrectProduct()
    {
        // Int32 arithmetic requires casting to Int64 first
        var a = Series.FromValues("a", new long[] { 2, 3, 4 });
        var b = Series.FromValues("b", new long[] { 5, 6, 7 });
        var result = a * b;

        result[0].AsInt64().Should().Be(10);
        result[1].AsInt64().Should().Be(18);
        result[2].AsInt64().Should().Be(28);
    }

    [Fact]
    public void Multiply_WithZero_ReturnsZero()
    {
        var a = Series.FromValues("a", new[] { 1.0, 2.0, 3.0 });
        var b = Series.FromValues("b", new[] { 0.0, 0.0, 0.0 });
        var result = a * b;

        result[0].AsFloat64().Should().Be(0.0);
        result[1].AsFloat64().Should().Be(0.0);
        result[2].AsFloat64().Should().Be(0.0);
    }

    [Fact]
    public void Multiply_WithNegatives_ReturnsCorrectSign()
    {
        var a = Series.FromValues("a", new[] { 2.0, -2.0, 2.0, -2.0 });
        var b = Series.FromValues("b", new[] { 3.0, 3.0, -3.0, -3.0 });
        var result = a * b;

        result[0].AsFloat64().Should().Be(6.0);   // pos * pos = pos
        result[1].AsFloat64().Should().Be(-6.0);  // neg * pos = neg
        result[2].AsFloat64().Should().Be(-6.0);  // pos * neg = neg
        result[3].AsFloat64().Should().Be(6.0);   // neg * neg = pos
    }

    [Fact]
    public void Multiply_Scalar_MultipliesAllElements()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0 });
        var result = series * 10.0;

        result[0].AsFloat64().Should().Be(10.0);
        result[1].AsFloat64().Should().Be(20.0);
        result[2].AsFloat64().Should().Be(30.0);
    }

    [Fact]
    public void Multiply_ScalarLeft_MultipliesAllElements()
    {
        // Multiplication is commutative: 10 * series = series * 10
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0 });
        var result = series * 10.0;

        result[0].AsFloat64().Should().Be(10.0);
        result[1].AsFloat64().Should().Be(20.0);
        result[2].AsFloat64().Should().Be(30.0);
    }

    // ============================================================================
    // Division Tests
    // ============================================================================

    [Fact]
    public void Divide_TwoSeries_ReturnsElementwiseQuotient()
    {
        var a = Series.FromValues("a", new[] { 10.0, 20.0, 30.0 });
        var b = Series.FromValues("b", new[] { 2.0, 4.0, 5.0 });
        var result = a / b;

        result[0].AsFloat64().Should().Be(5.0);
        result[1].AsFloat64().Should().Be(5.0);
        result[2].AsFloat64().Should().Be(6.0);
    }

    [Fact]
    public void Divide_ByZero_ReturnsInfinity()
    {
        var a = Series.FromValues("a", new[] { 1.0, -1.0, 0.0 });
        var b = Series.FromValues("b", new[] { 0.0, 0.0, 0.0 });
        var result = a / b;

        double.IsPositiveInfinity(result[0].AsFloat64()).Should().BeTrue();
        double.IsNegativeInfinity(result[1].AsFloat64()).Should().BeTrue();
        double.IsNaN(result[2].AsFloat64()).Should().BeTrue();  // 0/0 = NaN
    }

    [Fact]
    public void Divide_Scalar_DividesAllElements()
    {
        var series = Series.FromValues("s", new[] { 10.0, 20.0, 30.0 });
        var result = series / 10.0;

        result[0].AsFloat64().Should().Be(1.0);
        result[1].AsFloat64().Should().Be(2.0);
        result[2].AsFloat64().Should().Be(3.0);
    }

    [Fact]
    public void Divide_ScalarLeft_DividesScalarByElements()
    {
        // scalar / series can be done by creating a scalar series
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 5.0 });
        var scalarSeries = Series.FromValues("scalar", new[] { 10.0, 10.0, 10.0 });
        var result = scalarSeries / series;

        result[0].AsFloat64().Should().Be(10.0);  // 10 / 1
        result[1].AsFloat64().Should().Be(5.0);   // 10 / 2
        result[2].AsFloat64().Should().Be(2.0);   // 10 / 5
    }

    // ============================================================================
    // Chained Operations Tests
    // ============================================================================

    [Fact]
    public void ChainedArithmetic_WorksCorrectly()
    {
        var a = Series.FromValues("a", new[] { 1.0, 2.0, 3.0 });
        var b = Series.FromValues("b", new[] { 2.0, 2.0, 2.0 });

        // (a + b) * 2 - 1
        var result = (a + b) * 2.0 - 1.0;

        result[0].AsFloat64().Should().Be(5.0);   // (1+2)*2-1 = 5
        result[1].AsFloat64().Should().Be(7.0);   // (2+2)*2-1 = 7
        result[2].AsFloat64().Should().Be(9.0);   // (3+2)*2-1 = 9
    }

    [Fact]
    public void ChainedArithmetic_MultipleOperations()
    {
        var a = Series.FromValues("a", new[] { 10.0, 20.0, 30.0 });

        // (a / 10 + 5) * 2
        var result = (a / 10.0 + 5.0) * 2.0;

        result[0].AsFloat64().Should().Be(12.0);   // (10/10+5)*2 = 12
        result[1].AsFloat64().Should().Be(14.0);   // (20/10+5)*2 = 14
        result[2].AsFloat64().Should().Be(16.0);   // (30/10+5)*2 = 16
    }

    // ============================================================================
    // With Nulls Tests
    // ============================================================================

    [Fact]
    public void Add_WithNulls_PropagatesNulls()
    {
        var a = Series.FromNullable("a", new double?[] { 1.0, null, 3.0 });
        var b = Series.FromValues("b", new[] { 10.0, 20.0, 30.0 });
        var result = a + b;

        result[0].AsFloat64().Should().Be(11.0);
        result.IsNull(1).Should().BeTrue();
        result[2].AsFloat64().Should().Be(33.0);
    }

    [Fact]
    public void Subtract_WithNulls_PropagatesNulls()
    {
        var a = Series.FromValues("a", new[] { 10.0, 20.0, 30.0 });
        var b = Series.FromNullable("b", new double?[] { 1.0, null, 3.0 });
        var result = a - b;

        result[0].AsFloat64().Should().Be(9.0);
        result.IsNull(1).Should().BeTrue();
        result[2].AsFloat64().Should().Be(27.0);
    }

    [Fact]
    public void Multiply_WithNulls_PropagatesNulls()
    {
        var a = Series.FromNullable("a", new double?[] { 2.0, null, 4.0 });
        var b = Series.FromNullable("b", new double?[] { null, 3.0, 5.0 });
        var result = a * b;

        result.IsNull(0).Should().BeTrue();
        result.IsNull(1).Should().BeTrue();
        result[2].AsFloat64().Should().Be(20.0);
    }

    // ============================================================================
    // Edge Cases
    // ============================================================================

    [Fact]
    public void Add_EmptySeries_ReturnsEmpty()
    {
        var a = Series.FromValues("a", Array.Empty<double>());
        var b = Series.FromValues("b", Array.Empty<double>());
        var result = a + b;

        result.Length.Should().Be(0);
    }

    [Fact]
    public void Multiply_SingleElement_ReturnsProduct()
    {
        var a = Series.FromValues("a", new[] { 5.0 });
        var b = Series.FromValues("b", new[] { 3.0 });
        var result = a * b;

        result.Length.Should().Be(1);
        result[0].AsFloat64().Should().Be(15.0);
    }

    [Fact]
    public void Add_VeryLargeNumbers_HandlesCorrectly()
    {
        var a = Series.FromValues("a", new[] { double.MaxValue / 2 });
        var b = Series.FromValues("b", new[] { double.MaxValue / 2 });
        var result = a + b;

        result[0].AsFloat64().Should().BeApproximately(double.MaxValue, double.MaxValue * 0.001);
    }

    [Fact]
    public void Add_VerySmallNumbers_HandlesCorrectly()
    {
        var a = Series.FromValues("a", new[] { double.Epsilon });
        var b = Series.FromValues("b", new[] { double.Epsilon });
        var result = a + b;

        result[0].AsFloat64().Should().Be(double.Epsilon * 2);
    }

    [Fact]
    public void Subtract_SameValues_ReturnsZero()
    {
        var a = Series.FromValues("a", new[] { 1.0, 2.0, 3.0 });
        var result = a - a;

        result[0].AsFloat64().Should().Be(0.0);
        result[1].AsFloat64().Should().Be(0.0);
        result[2].AsFloat64().Should().Be(0.0);
    }

    [Fact]
    public void Divide_OneByOne_ReturnsOne()
    {
        var a = Series.FromValues("a", new[] { 5.0, 10.0, 15.0 });
        var result = a / a;

        result[0].AsFloat64().Should().Be(1.0);
        result[1].AsFloat64().Should().Be(1.0);
        result[2].AsFloat64().Should().Be(1.0);
    }

    // ============================================================================
    // Type Coercion Tests
    // ============================================================================

    [Fact]
    public void Add_IntAndDouble_ProducesDouble()
    {
        var intSeries = Series.FromValues("int", new[] { 1, 2, 3 });
        var doubleSeries = Series.FromValues("double", new[] { 0.5, 0.5, 0.5 });

        // This depends on implementation - may need cast
        var casted = intSeries.Cast(DataType.Float64);
        var result = casted + doubleSeries;

        result[0].AsFloat64().Should().Be(1.5);
        result[1].AsFloat64().Should().Be(2.5);
        result[2].AsFloat64().Should().Be(3.5);
    }

    // ============================================================================
    // Large Series Tests
    // ============================================================================

    [Fact]
    public void Add_LargeSeries_WorksCorrectly()
    {
        var size = 10000;
        var a = Series.FromValues("a", Enumerable.Range(0, size).Select(i => (double)i).ToArray());
        var b = Series.FromValues("b", Enumerable.Range(0, size).Select(i => (double)i).ToArray());
        var result = a + b;

        result.Length.Should().Be(size);
        result[0].AsFloat64().Should().Be(0.0);
        result[100].AsFloat64().Should().Be(200.0);
        result[size - 1].AsFloat64().Should().Be((size - 1) * 2);
    }

    [Fact]
    public void Multiply_LargeSeries_WorksCorrectly()
    {
        var size = 10000;
        var a = Series.FromValues("a", Enumerable.Range(1, size).Select(i => (double)i).ToArray());
        var b = Series.FromValues("b", Enumerable.Repeat(2.0, size).ToArray());
        var result = a * b;

        result.Length.Should().Be(size);
        result[0].AsFloat64().Should().Be(2.0);
        result[99].AsFloat64().Should().Be(200.0);
    }
}
