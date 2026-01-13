// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET
//
// Additional edge case tests for aggregation operations.

using FluentAssertions;
using Polaire;
using Polaire.DataTypes;
using Xunit;
using static Polaire.Pl;

namespace Polaire.Tests;

/// <summary>
/// Edge case tests for aggregation operations.
/// </summary>
public class AggregationEdgeCaseTests
{
    // ============================================================================
    // Single Value Aggregations
    // ============================================================================

    [Fact]
    public void Sum_SingleValue_ReturnsValue()
    {
        var series = Series.FromValues("s", new[] { 42.0 });
        var result = series.Sum();

        result.AsFloat64().Should().Be(42.0);
    }

    [Fact]
    public void Mean_SingleValue_ReturnsValue()
    {
        var series = Series.FromValues("s", new[] { 42.0 });
        var result = series.Mean();

        result.AsFloat64().Should().Be(42.0);
    }

    [Fact]
    public void Min_SingleValue_ReturnsValue()
    {
        var series = Series.FromValues("s", new[] { 42.0 });
        var result = series.Min();

        result.AsFloat64().Should().Be(42.0);
    }

    [Fact]
    public void Max_SingleValue_ReturnsValue()
    {
        var series = Series.FromValues("s", new[] { 42.0 });
        var result = series.Max();

        result.AsFloat64().Should().Be(42.0);
    }

    [Fact]
    public void Std_SingleValue_ReturnsNull()
    {
        var series = Series.FromValues("s", new[] { 42.0 });
        var result = series.Std();

        // Std requires at least 2 values, returns null for single value
        result.IsNull.Should().BeTrue();
    }

    [Fact]
    public void Var_SingleValue_ReturnsNull()
    {
        var series = Series.FromValues("s", new[] { 42.0 });
        var result = series.Var();

        // Var requires at least 2 values, returns null for single value
        result.IsNull.Should().BeTrue();
    }

    // ============================================================================
    // Two Value Aggregations
    // ============================================================================

    [Fact]
    public void Sum_TwoValues_ReturnsSum()
    {
        var series = Series.FromValues("s", new[] { 10.0, 20.0 });
        var result = series.Sum();

        result.AsFloat64().Should().Be(30.0);
    }

    [Fact]
    public void Mean_TwoValues_ReturnsAverage()
    {
        var series = Series.FromValues("s", new[] { 10.0, 20.0 });
        var result = series.Mean();

        result.AsFloat64().Should().Be(15.0);
    }

    [Fact]
    public void Min_TwoValues_ReturnsSmaller()
    {
        var series = Series.FromValues("s", new[] { 10.0, 20.0 });
        var result = series.Min();

        result.AsFloat64().Should().Be(10.0);
    }

    [Fact]
    public void Max_TwoValues_ReturnsLarger()
    {
        var series = Series.FromValues("s", new[] { 10.0, 20.0 });
        var result = series.Max();

        result.AsFloat64().Should().Be(20.0);
    }

    // ============================================================================
    // Identical Values
    // ============================================================================

    [Fact]
    public void Sum_IdenticalValues_ReturnsProductWithCount()
    {
        var series = Series.FromValues("s", new[] { 5.0, 5.0, 5.0, 5.0 });
        var result = series.Sum();

        result.AsFloat64().Should().Be(20.0);
    }

    [Fact]
    public void Mean_IdenticalValues_ReturnsValue()
    {
        var series = Series.FromValues("s", new[] { 5.0, 5.0, 5.0, 5.0 });
        var result = series.Mean();

        result.AsFloat64().Should().Be(5.0);
    }

    [Fact]
    public void Std_IdenticalValues_ReturnsZero()
    {
        var series = Series.FromValues("s", new[] { 5.0, 5.0, 5.0, 5.0 });
        var result = series.Std();

        result.AsFloat64().Should().Be(0.0);
    }

    // ============================================================================
    // Negative Values
    // ============================================================================

    [Fact]
    public void Sum_NegativeValues_ReturnsSumCorrectly()
    {
        var series = Series.FromValues("s", new[] { -10.0, -20.0, -30.0 });
        var result = series.Sum();

        result.AsFloat64().Should().Be(-60.0);
    }

    [Fact]
    public void Sum_MixedSigns_ReturnsSumCorrectly()
    {
        var series = Series.FromValues("s", new[] { -10.0, 20.0, -5.0, 15.0 });
        var result = series.Sum();

        result.AsFloat64().Should().Be(20.0);
    }

    [Fact]
    public void Min_AllNegative_ReturnsSmallest()
    {
        var series = Series.FromValues("s", new[] { -10.0, -20.0, -5.0 });
        var result = series.Min();

        result.AsFloat64().Should().Be(-20.0);
    }

    [Fact]
    public void Max_AllNegative_ReturnsLargest()
    {
        var series = Series.FromValues("s", new[] { -10.0, -20.0, -5.0 });
        var result = series.Max();

        result.AsFloat64().Should().Be(-5.0);
    }

    // ============================================================================
    // Zero Values
    // ============================================================================

    [Fact]
    public void Sum_AllZeros_ReturnsZero()
    {
        var series = Series.FromValues("s", new[] { 0.0, 0.0, 0.0 });
        var result = series.Sum();

        result.AsFloat64().Should().Be(0.0);
    }

    [Fact]
    public void Mean_AllZeros_ReturnsZero()
    {
        var series = Series.FromValues("s", new[] { 0.0, 0.0, 0.0 });
        var result = series.Mean();

        result.AsFloat64().Should().Be(0.0);
    }

    [Fact]
    public void Std_AllZeros_ReturnsZero()
    {
        var series = Series.FromValues("s", new[] { 0.0, 0.0, 0.0 });
        var result = series.Std();

        result.AsFloat64().Should().Be(0.0);
    }

    // ============================================================================
    // Integer Types
    // ============================================================================

    [Fact]
    public void Sum_Int32_ReturnsCorrectSum()
    {
        var series = Series.FromValues("s", new[] { 1, 2, 3, 4, 5 });
        var result = series.Sum();

        // Sum of integers returns Int64
        result.AsInt64().Should().Be(15);
    }

    [Fact]
    public void Mean_Int32_ReturnsCorrectMean()
    {
        var series = Series.FromValues("s", new[] { 1, 2, 3, 4, 5 });
        var result = series.Mean();

        result.AsFloat64().Should().Be(3.0);
    }

    [Fact]
    public void Sum_Int64_ReturnsCorrectSum()
    {
        var series = Series.FromValues("s", new long[] { 1, 2, 3, 4, 5 });
        var result = series.Sum();

        // Sum of Int64 returns Int64
        result.AsInt64().Should().Be(15);
    }

    // ============================================================================
    // Float32 Type
    // ============================================================================

    [Fact]
    public void Sum_Float32_ReturnsCorrectSum()
    {
        var series = Series.FromValues("s", new float[] { 1.5f, 2.5f, 3.5f });
        var result = series.Sum();

        result.AsFloat64().Should().BeApproximately(7.5, 0.001);
    }

    [Fact]
    public void Mean_Float32_ReturnsCorrectMean()
    {
        var series = Series.FromValues("s", new float[] { 1.5f, 2.5f, 3.5f });
        var result = series.Mean();

        result.AsFloat64().Should().BeApproximately(2.5, 0.001);
    }

    // ============================================================================
    // Large Values
    // ============================================================================

    [Fact]
    public void Sum_LargeValues_NoOverflow()
    {
        var series = Series.FromValues("s", new[] { 1e15, 2e15, 3e15 });
        var result = series.Sum();

        result.AsFloat64().Should().BeApproximately(6e15, 1e9);
    }

    [Fact]
    public void Mean_LargeValues_Correct()
    {
        var series = Series.FromValues("s", new[] { 1e15, 2e15, 3e15 });
        var result = series.Mean();

        result.AsFloat64().Should().BeApproximately(2e15, 1e9);
    }

    // ============================================================================
    // Small Values
    // ============================================================================

    [Fact]
    public void Sum_SmallValues_NoPrecisionLoss()
    {
        var series = Series.FromValues("s", new[] { 1e-10, 2e-10, 3e-10 });
        var result = series.Sum();

        result.AsFloat64().Should().BeApproximately(6e-10, 1e-16);
    }

    // ============================================================================
    // Count Aggregations
    // ============================================================================

    [Fact]
    public void Count_ReturnsCorrectCount()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 });
        var result = series.Count();

        result.Should().Be(5);
    }

    [Fact]
    public void Count_Empty_ReturnsZero()
    {
        var series = Series.FromValues("s", Array.Empty<double>());
        var result = series.Count();

        result.Should().Be(0);
    }

    [Fact]
    public void Count_WithNulls_ExcludesNulls()
    {
        var series = Series.FromNullable("s", new double?[] { 1.0, null, 3.0, null, 5.0 });
        var result = series.Count();

        result.Should().Be(3);
    }

    // ============================================================================
    // Product (if available)
    // ============================================================================

    [Fact]
    public void Median_OddCount_ReturnsMiddle()
    {
        var series = Series.FromValues("s", new[] { 1.0, 3.0, 2.0 });
        var result = series.Median();

        result.AsFloat64().Should().Be(2.0);
    }

    [Fact]
    public void Median_EvenCount_ReturnsAverageOfMiddle()
    {
        var series = Series.FromValues("s", new[] { 1.0, 4.0, 3.0, 2.0 });
        var result = series.Median();

        result.AsFloat64().Should().Be(2.5);  // (2+3)/2
    }

    [Fact]
    public void Median_SingleValue_ReturnsValue()
    {
        var series = Series.FromValues("s", new[] { 42.0 });
        var result = series.Median();

        result.AsFloat64().Should().Be(42.0);
    }

    // ============================================================================
    // First/Last Aggregations
    // ============================================================================

    [Fact]
    public void First_ReturnsFirstValue()
    {
        var series = Series.FromValues("s", new[] { 10.0, 20.0, 30.0 });
        var result = series.First();

        result.AsFloat64().Should().Be(10.0);
    }

    [Fact]
    public void Last_ReturnsLastValue()
    {
        var series = Series.FromValues("s", new[] { 10.0, 20.0, 30.0 });
        var result = series.Last();

        result.AsFloat64().Should().Be(30.0);
    }
}
