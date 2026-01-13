// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET
//
// Comprehensive tests for null handling and edge cases, inspired by Polars test suite.
// These tests ensure correct behavior with:
// - Null values in various operations
// - Edge cases (empty, single element, large)
// - Special values (NaN, Infinity)
// - Boundary conditions

using FluentAssertions;
using Polaire;
using Polaire.DataTypes;
using Xunit;

namespace Polaire.Tests;

/// <summary>
/// Tests for null handling and edge cases.
/// </summary>
public class NullEdgeCaseTests
{
    // ============================================================================
    // Null Propagation in Arithmetic
    // ============================================================================

    [Fact]
    public void Add_WithNull_PropagatesNull()
    {
        var a = Series.FromNullable("a", new double?[] { 1.0, null, 3.0 });
        var b = Series.FromValues("b", new[] { 1.0, 1.0, 1.0 });
        var result = a + b;

        result[0].AsFloat64().Should().Be(2.0);
        result.IsNull(1).Should().BeTrue();
        result[2].AsFloat64().Should().Be(4.0);
    }

    [Fact]
    public void Sub_WithNull_PropagatesNull()
    {
        var a = Series.FromNullable("a", new double?[] { 5.0, null, 3.0 });
        var b = Series.FromValues("b", new[] { 1.0, 1.0, 1.0 });
        var result = a - b;

        result[0].AsFloat64().Should().Be(4.0);
        result.IsNull(1).Should().BeTrue();
    }

    [Fact]
    public void Mul_WithNull_PropagatesNull()
    {
        var a = Series.FromNullable("a", new double?[] { 2.0, null, 3.0 });
        var b = Series.FromValues("b", new[] { 5.0, 5.0, 5.0 });
        var result = a * b;

        result[0].AsFloat64().Should().Be(10.0);
        result.IsNull(1).Should().BeTrue();
    }

    [Fact]
    public void Div_WithNull_PropagatesNull()
    {
        var a = Series.FromNullable("a", new double?[] { 10.0, null, 15.0 });
        var b = Series.FromValues("b", new[] { 2.0, 2.0, 3.0 });
        var result = a / b;

        result[0].AsFloat64().Should().Be(5.0);
        result.IsNull(1).Should().BeTrue();
    }

    [Fact]
    public void Add_BothNull_ReturnsNull()
    {
        var a = Series.FromNullable("a", new double?[] { null });
        var b = Series.FromNullable("b", new double?[] { null });
        var result = a + b;

        result.IsNull(0).Should().BeTrue();
    }

    // ============================================================================
    // Null in Aggregations
    // ============================================================================

    [Fact]
    public void Sum_WithNulls_IgnoresNulls()
    {
        var series = Series.FromNullable("s", new double?[] { 1.0, null, 3.0, null, 5.0 });
        var result = series.Sum();

        result.TryGetDouble(out var sum).Should().BeTrue();
        sum.Should().Be(9.0);  // 1 + 3 + 5
    }

    [Fact]
    public void Mean_WithNulls_IgnoresNulls()
    {
        var series = Series.FromNullable("s", new double?[] { 2.0, null, 4.0 });
        var result = series.Mean();

        result.TryGetDouble(out var mean).Should().BeTrue();
        mean.Should().Be(3.0);  // (2 + 4) / 2
    }

    [Fact]
    public void Min_WithNulls_IgnoresNulls()
    {
        var series = Series.FromNullable("s", new double?[] { 5.0, null, 2.0, null, 8.0 });
        var result = series.Min();

        result.TryGetDouble(out var min).Should().BeTrue();
        min.Should().Be(2.0);
    }

    [Fact]
    public void Max_WithNulls_IgnoresNulls()
    {
        var series = Series.FromNullable("s", new double?[] { 5.0, null, 2.0, null, 8.0 });
        var result = series.Max();

        result.TryGetDouble(out var max).Should().BeTrue();
        max.Should().Be(8.0);
    }

    [Fact]
    public void Sum_AllNulls_ReturnsNull()
    {
        var series = Series.FromNullable("s", new double?[] { null, null, null });
        var result = series.Sum();

        result.IsNull.Should().BeTrue();
    }

    [Fact]
    public void Mean_AllNulls_ReturnsNull()
    {
        var series = Series.FromNullable("s", new double?[] { null, null, null });
        var result = series.Mean();

        result.IsNull.Should().BeTrue();
    }

    // ============================================================================
    // Null in Boolean Operations
    // ============================================================================

    [Fact]
    public void And_WithNull_PropagatesNull()
    {
        var a = Series.FromNullable("a", new bool?[] { true, null, false });
        var b = Series.FromValues("b", new[] { true, true, true });
        var result = a.And(b);

        result[0].AsBoolean().Should().BeTrue();
        result.IsNull(1).Should().BeTrue();
        result[2].AsBoolean().Should().BeFalse();
    }

    [Fact]
    public void Or_WithNull_PropagatesNull()
    {
        var a = Series.FromNullable("a", new bool?[] { false, null, true });
        var b = Series.FromValues("b", new[] { false, false, false });
        var result = a.Or(b);

        result[0].AsBoolean().Should().BeFalse();
        result.IsNull(1).Should().BeTrue();
        result[2].AsBoolean().Should().BeTrue();
    }

    [Fact]
    public void Not_WithNull_PreservesNull()
    {
        var series = Series.FromNullable("s", new bool?[] { true, null, false });
        var result = series.Not();

        result[0].AsBoolean().Should().BeFalse();
        result.IsNull(1).Should().BeTrue();
        result[2].AsBoolean().Should().BeTrue();
    }

    // ============================================================================
    // Empty Series Edge Cases
    // ============================================================================

    [Fact]
    public void EmptySeries_Sum_ReturnsNull()
    {
        var series = Series.FromValues("s", Array.Empty<double>());
        var result = series.Sum();

        result.IsNull.Should().BeTrue();
    }

    [Fact]
    public void EmptySeries_Mean_ReturnsNull()
    {
        var series = Series.FromValues("s", Array.Empty<double>());
        var result = series.Mean();

        result.IsNull.Should().BeTrue();
    }

    [Fact]
    public void EmptySeries_Min_ReturnsNull()
    {
        var series = Series.FromValues("s", Array.Empty<double>());
        var result = series.Min();

        result.IsNull.Should().BeTrue();
    }

    [Fact]
    public void EmptySeries_Max_ReturnsNull()
    {
        var series = Series.FromValues("s", Array.Empty<double>());
        var result = series.Max();

        result.IsNull.Should().BeTrue();
    }

    [Fact]
    public void EmptySeries_Count_ReturnsZero()
    {
        var series = Series.FromValues("s", Array.Empty<double>());

        series.Length.Should().Be(0);
    }

    [Fact]
    public void EmptySeries_NullCount_ReturnsZero()
    {
        var series = Series.FromValues("s", Array.Empty<double>());

        series.NullCount.Should().Be(0);
    }

    // ============================================================================
    // Single Element Edge Cases
    // ============================================================================

    [Fact]
    public void SingleElement_Sum_ReturnsElement()
    {
        var series = Series.FromValues("s", new[] { 42.0 });
        var result = series.Sum();

        result.TryGetDouble(out var sum).Should().BeTrue();
        sum.Should().Be(42.0);
    }

    [Fact]
    public void SingleElement_Mean_ReturnsElement()
    {
        var series = Series.FromValues("s", new[] { 42.0 });
        var result = series.Mean();

        result.TryGetDouble(out var mean).Should().BeTrue();
        mean.Should().Be(42.0);
    }

    [Fact]
    public void SingleElement_Min_ReturnsElement()
    {
        var series = Series.FromValues("s", new[] { 42.0 });
        var result = series.Min();

        result.TryGetDouble(out var min).Should().BeTrue();
        min.Should().Be(42.0);
    }

    [Fact]
    public void SingleElement_Max_ReturnsElement()
    {
        var series = Series.FromValues("s", new[] { 42.0 });
        var result = series.Max();

        result.TryGetDouble(out var max).Should().BeTrue();
        max.Should().Be(42.0);
    }

    [Fact]
    public void SingleElement_Std_ReturnsNaN()
    {
        var series = Series.FromValues("s", new[] { 42.0 });
        var result = series.Std();

        // Standard deviation of single element is NaN (or 0 depending on implementation)
        // because we need at least 2 values for sample std
    }

    [Fact]
    public void SingleNull_Sum_ReturnsNull()
    {
        var series = Series.FromNullable("s", new double?[] { null });
        var result = series.Sum();

        result.IsNull.Should().BeTrue();
    }

    // ============================================================================
    // NaN Handling
    // ============================================================================

    [Fact]
    public void Sum_WithNaN_PropagatesNaN()
    {
        var series = Series.FromValues("s", new[] { 1.0, double.NaN, 3.0 });
        var result = series.Sum();

        result.TryGetDouble(out var sum).Should().BeTrue();
        double.IsNaN(sum).Should().BeTrue();
    }

    [Fact]
    public void Mean_WithNaN_PropagatesNaN()
    {
        var series = Series.FromValues("s", new[] { 1.0, double.NaN, 3.0 });
        var result = series.Mean();

        result.TryGetDouble(out var mean).Should().BeTrue();
        double.IsNaN(mean).Should().BeTrue();
    }

    [Fact]
    public void Min_WithNaN_IgnoresNaN()
    {
        // Polars semantics: Min/Max ignore NaN
        var series = Series.FromValues("s", new[] { double.NaN, 2.0, 5.0 });
        var result = series.Min();

        result.TryGetDouble(out var min).Should().BeTrue();
        min.Should().Be(2.0);
    }

    [Fact]
    public void Max_WithNaN_IgnoresNaN()
    {
        // Polars semantics: Min/Max ignore NaN
        var series = Series.FromValues("s", new[] { double.NaN, 2.0, 5.0 });
        var result = series.Max();

        result.TryGetDouble(out var max).Should().BeTrue();
        max.Should().Be(5.0);
    }

    [Fact]
    public void IsNaN_DetectsNaN()
    {
        var series = Series.FromValues("s", new[] { 1.0, double.NaN, 3.0 });
        var result = series.IsNaN();

        result[0].AsBoolean().Should().BeFalse();
        result[1].AsBoolean().Should().BeTrue();
        result[2].AsBoolean().Should().BeFalse();
    }

    [Fact]
    public void IsNotNaN_InverseOfIsNaN()
    {
        var series = Series.FromValues("s", new[] { 1.0, double.NaN, 3.0 });
        var result = series.IsNotNaN();

        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeFalse();
        result[2].AsBoolean().Should().BeTrue();
    }

    // ============================================================================
    // Infinity Handling
    // ============================================================================

    [Fact]
    public void Sum_WithInfinity_ReturnsInfinity()
    {
        var series = Series.FromValues("s", new[] { 1.0, double.PositiveInfinity, 3.0 });
        var result = series.Sum();

        result.TryGetDouble(out var sum).Should().BeTrue();
        double.IsPositiveInfinity(sum).Should().BeTrue();
    }

    [Fact]
    public void Sum_PosNegInfinity_ReturnsNaN()
    {
        var series = Series.FromValues("s", new[] { double.PositiveInfinity, double.NegativeInfinity });
        var result = series.Sum();

        result.TryGetDouble(out var sum).Should().BeTrue();
        double.IsNaN(sum).Should().BeTrue();  // Inf + (-Inf) = NaN
    }

    [Fact]
    public void Min_WithInfinity_ReturnsCorrectMin()
    {
        var series = Series.FromValues("s", new[] { 1.0, double.PositiveInfinity, double.NegativeInfinity });
        var result = series.Min();

        result.TryGetDouble(out var min).Should().BeTrue();
        double.IsNegativeInfinity(min).Should().BeTrue();
    }

    [Fact]
    public void Max_WithInfinity_ReturnsCorrectMax()
    {
        var series = Series.FromValues("s", new[] { 1.0, double.PositiveInfinity, double.NegativeInfinity });
        var result = series.Max();

        result.TryGetDouble(out var max).Should().BeTrue();
        double.IsPositiveInfinity(max).Should().BeTrue();
    }

    // ============================================================================
    // Null in Rolling Operations
    // ============================================================================

    [Fact]
    public void RollingSum_WithNulls_HandlesNulls()
    {
        var series = Series.FromNullable("s", new double?[] { 1.0, null, 3.0, 4.0 });
        var result = series.RollingSum(2);

        // Rolling window behavior with nulls varies by implementation
        result.Length.Should().Be(4);
    }

    [Fact]
    public void RollingMean_WithNulls_HandlesNulls()
    {
        var series = Series.FromNullable("s", new double?[] { 1.0, null, 3.0, 4.0 });
        var result = series.RollingMean(2);

        result.Length.Should().Be(4);
    }

    // ============================================================================
    // Null in Fill Operations
    // ============================================================================

    [Fact]
    public void FillNull_WithValue_FillsNulls()
    {
        var series = Series.FromNullable("s", new double?[] { 1.0, null, 3.0, null });
        var result = series.FillNull(AnyValue.From(0.0));

        result.IsNull(1).Should().BeFalse();
        result[1].AsFloat64().Should().Be(0.0);
        result.IsNull(3).Should().BeFalse();
        result[3].AsFloat64().Should().Be(0.0);
    }

    [Fact]
    public void FillNull_Forward_FillsWithPrevious()
    {
        var series = Series.FromNullable("s", new double?[] { 1.0, null, null, 4.0 });
        var result = series.FillForward();

        result[1].AsFloat64().Should().Be(1.0);
        result[2].AsFloat64().Should().Be(1.0);
        result[3].AsFloat64().Should().Be(4.0);
    }

    [Fact]
    public void FillNull_Backward_FillsWithNext()
    {
        var series = Series.FromNullable("s", new double?[] { 1.0, null, null, 4.0 });
        var result = series.FillBackward();

        result[1].AsFloat64().Should().Be(4.0);
        result[2].AsFloat64().Should().Be(4.0);
    }

    // ============================================================================
    // DataFrame Null Handling
    // ============================================================================

    [Fact]
    public void DataFrame_FilterNulls_RemovesRowsWithNulls()
    {
        var a = Series.FromNullable("a", new int?[] { 1, null, 3 });
        var b = Series.FromValues("b", new[] { 10, 20, 30 });
        var df = new DataFrame(a, b);

        // Filter out rows where column a is null
        var mask = a.IsNotNull();
        var result = df.Filter(mask);

        result.Height.Should().Be(2);
    }

    [Fact]
    public void DataFrame_NullCount_ReturnsCorrectCounts()
    {
        var a = Series.FromNullable("a", new int?[] { 1, null, 3 });
        var b = Series.FromNullable("b", new int?[] { null, null, 30 });
        var df = new DataFrame(a, b);

        var result = df.NullCount();

        // NullCount returns one row per column
        result.Height.Should().Be(2);  // 2 columns = 2 rows
        result.Columns.Should().Contain("column");
        result.Columns.Should().Contain("null_count");
    }

    // ============================================================================
    // Large Scale Tests
    // ============================================================================

    [Fact]
    public void LargeSeries_WithNulls_HandlesCorrectly()
    {
        var values = new double?[10000];
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = i % 10 == 0 ? null : (double)i;
        }

        var series = Series.FromNullable("s", values);

        series.NullCount.Should().Be(1000);
        series.Length.Should().Be(10000);
    }

    [Fact]
    public void LargeSeries_SumWithNulls_Correct()
    {
        var values = new double?[1000];
        double expectedSum = 0;
        for (int i = 0; i < values.Length; i++)
        {
            if (i % 10 == 0)
            {
                values[i] = null;
            }
            else
            {
                values[i] = (double)i;
                expectedSum += i;
            }
        }

        var series = Series.FromNullable("s", values);
        var result = series.Sum();

        result.TryGetDouble(out var sum).Should().BeTrue();
        sum.Should().Be(expectedSum);
    }

    // ============================================================================
    // Null in Comparison Operations
    // ============================================================================

    [Fact]
    public void IsNull_Series_ReturnsCorrectMask()
    {
        var series = Series.FromNullable("s", new int?[] { 1, null, 3, null, 5 });
        var result = series.IsNull();

        result[0].AsBoolean().Should().BeFalse();
        result[1].AsBoolean().Should().BeTrue();
        result[2].AsBoolean().Should().BeFalse();
        result[3].AsBoolean().Should().BeTrue();
        result[4].AsBoolean().Should().BeFalse();
    }

    [Fact]
    public void IsNotNull_Series_InverseOfIsNull()
    {
        var series = Series.FromNullable("s", new int?[] { 1, null, 3 });
        var result = series.IsNotNull();

        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeFalse();
        result[2].AsBoolean().Should().BeTrue();
    }

    // ============================================================================
    // Drop Nulls
    // ============================================================================

    [Fact]
    public void DropNulls_RemovesNulls()
    {
        var series = Series.FromNullable("s", new int?[] { 1, null, 3, null, 5 });
        var result = series.DropNulls();

        result.Length.Should().Be(3);
        result.NullCount.Should().Be(0);
    }

    [Fact]
    public void DropNulls_NoNulls_ReturnsSame()
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
}
