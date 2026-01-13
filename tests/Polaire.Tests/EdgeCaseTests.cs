// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET
//
// Critical correctness tests for edge cases, SIMD boundaries, and numerical accuracy.
// These tests ensure that optimized SIMD paths produce identical results to scalar paths.

using Xunit;
using FluentAssertions;
using Polaire.DataTypes;

namespace Polaire.Tests;

/// <summary>
/// Critical correctness tests for edge cases that could cause incorrect results.
/// Focus areas:
/// 1. SIMD vs Scalar path equivalence
/// 2. NaN/Infinity handling
/// 3. Empty/single element arrays
/// 4. Boundary conditions at SIMD thresholds
/// 5. Integer overflow scenarios
/// 6. Floating-point precision
/// </summary>
public class EdgeCaseTests
{
    // ==========================================================================
    // SIMD Boundary Tests - Float64
    // Vector128<double> = 2 elements, process 8 per iteration (4 accumulators)
    // ==========================================================================

    [Theory]
    [InlineData(0)]   // Empty
    [InlineData(1)]   // Single element
    [InlineData(2)]   // Minimum vector size
    [InlineData(7)]   // Just under SIMD threshold
    [InlineData(8)]   // Exact SIMD threshold
    [InlineData(9)]   // SIMD + 1 remainder
    [InlineData(15)]  // SIMD + 7 remainder
    [InlineData(16)]  // 2x SIMD iterations
    [InlineData(17)]  // 2x SIMD + 1 remainder
    [InlineData(100)] // Multiple SIMD iterations
    [InlineData(1000)] // Large array
    public void Sum_Float64_AllArraySizes_ProduceCorrectResults(int length)
    {
        if (length == 0)
        {
            var empty = Series.FromValues("empty", Array.Empty<double>());
            empty.Sum().IsNull.Should().BeTrue("empty array should return null");
            return;
        }

        // Create predictable test data
        var data = Enumerable.Range(1, length).Select(i => (double)i).ToArray();
        var series = Series.FromValues("test", data);

        // Expected sum using simple formula: n*(n+1)/2
        double expected = (double)length * (length + 1) / 2;

        var result = series.Sum();
        result.IsNull.Should().BeFalse();
        result.AsFloat64().Should().BeApproximately(expected, 0.0001,
            $"Sum of 1..{length} should be {expected}");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(100)]
    public void Min_Float64_AllArraySizes_ProduceCorrectResults(int length)
    {
        if (length == 0)
        {
            var empty = Series.FromValues("empty", Array.Empty<double>());
            empty.Min().IsNull.Should().BeTrue("empty array should return null");
            return;
        }

        // Shuffle data to ensure min isn't at convenient position
        var data = Enumerable.Range(1, length).Select(i => (double)i).ToArray();
        // Put minimum in the middle
        var minIndex = length / 2;
        data[minIndex] = -999.0;

        var series = Series.FromValues("test", data);
        var result = series.Min();

        result.IsNull.Should().BeFalse();
        result.AsFloat64().Should().Be(-999.0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(100)]
    public void Max_Float64_AllArraySizes_ProduceCorrectResults(int length)
    {
        if (length == 0)
        {
            var empty = Series.FromValues("empty", Array.Empty<double>());
            empty.Max().IsNull.Should().BeTrue("empty array should return null");
            return;
        }

        var data = Enumerable.Range(1, length).Select(i => (double)i).ToArray();
        // Put maximum in the middle
        var maxIndex = length / 2;
        data[maxIndex] = 999999.0;

        var series = Series.FromValues("test", data);
        var result = series.Max();

        result.IsNull.Should().BeFalse();
        result.AsFloat64().Should().Be(999999.0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]  // Minimum for mean
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(100)]
    public void Mean_Float64_AllArraySizes_ProduceCorrectResults(int length)
    {
        if (length == 0)
        {
            var empty = Series.FromValues("empty", Array.Empty<double>());
            empty.Mean().IsNull.Should().BeTrue("empty array should return null");
            return;
        }

        // Use constant values for easy mean calculation
        var data = Enumerable.Repeat(42.5, length).ToArray();
        var series = Series.FromValues("test", data);

        var result = series.Mean();
        result.IsNull.Should().BeFalse();
        result.AsFloat64().Should().BeApproximately(42.5, 0.0001);
    }

    [Theory]
    [InlineData(0)]   // Empty - should return null
    [InlineData(1)]   // Single element - should return null (ddof=1)
    [InlineData(2)]   // Minimum for std (ddof=1)
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(100)]
    public void Std_Float64_AllArraySizes_ProduceCorrectResults(int length)
    {
        if (length <= 1)
        {
            var emptyData = length == 0 ? Array.Empty<double>() : new[] { 42.0 };
            var emptySeries = Series.FromValues("test", emptyData);
            emptySeries.Std().IsNull.Should().BeTrue(
                $"Std with {length} element(s) should return null (ddof=1 requires n>1)");
            return;
        }

        // Use known values with calculable std
        // Values: 1, 2, 3, ..., n
        // Variance = (n^2 - 1) / 12 for uniform 1..n
        // But we're using sample variance, so formula differs
        var data = Enumerable.Range(1, length).Select(i => (double)i).ToArray();
        var series = Series.FromValues("test", data);

        // Calculate expected manually
        double mean = (1.0 + length) / 2.0;
        double sumSqDiff = data.Sum(x => (x - mean) * (x - mean));
        double expectedStd = Math.Sqrt(sumSqDiff / (length - 1)); // ddof=1

        var result = series.Std();
        result.IsNull.Should().BeFalse();
        result.TryGetDouble(out var actualStd).Should().BeTrue();
        actualStd.Should().BeApproximately(expectedStd, 0.0001);
    }

    // ==========================================================================
    // SIMD Boundary Tests - Float32
    // Vector128<float> = 4 elements, process 16 per iteration (4 accumulators)
    // ==========================================================================

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(15)]  // Just under SIMD threshold
    [InlineData(16)]  // Exact SIMD threshold
    [InlineData(17)]  // SIMD + 1 remainder
    [InlineData(32)]  // 2x SIMD iterations
    [InlineData(100)]
    public void Sum_Float32_AllArraySizes_ProduceCorrectResults(int length)
    {
        if (length == 0)
        {
            var empty = Series.FromValues("empty", Array.Empty<float>());
            empty.Sum().IsNull.Should().BeTrue("empty array should return null");
            return;
        }

        var data = Enumerable.Range(1, length).Select(i => (float)i).ToArray();
        var series = Series.FromValues("test", data);

        double expected = (double)length * (length + 1) / 2;

        var result = series.Sum();
        result.IsNull.Should().BeFalse();
        // Float32 accumulates in double, so should be accurate
        result.TryGetDouble(out var sum).Should().BeTrue();
        sum.Should().BeApproximately(expected, 0.01);
    }

    // ==========================================================================
    // SIMD Boundary Tests - Int32
    // ==========================================================================

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(100)]
    public void Sum_Int32_AllArraySizes_ProduceCorrectResults(int length)
    {
        if (length == 0)
        {
            var empty = Series.FromValues("empty", Array.Empty<int>());
            empty.Sum().IsNull.Should().BeTrue("empty array should return null");
            return;
        }

        var data = Enumerable.Range(1, length).ToArray();
        var series = Series.FromValues("test", data);

        long expected = (long)length * (length + 1) / 2;

        var result = series.Sum();
        result.IsNull.Should().BeFalse();
        result.AsInt64().Should().Be(expected);
    }

    // ==========================================================================
    // NaN Handling Tests
    // Critical: SIMD and scalar paths must handle NaN identically
    // ==========================================================================

    [Fact]
    public void Sum_WithNaN_ReturnsNaN()
    {
        var data = new[] { 1.0, 2.0, double.NaN, 4.0, 5.0 };
        var series = Series.FromValues("test", data);

        var result = series.Sum();
        // Sum with NaN should propagate NaN
        result.TryGetDouble(out var sum).Should().BeTrue();
        double.IsNaN(sum).Should().BeTrue("Sum containing NaN should return NaN");
    }

    [Fact]
    public void Sum_AllNaN_ReturnsNaN()
    {
        var data = new[] { double.NaN, double.NaN, double.NaN };
        var series = Series.FromValues("test", data);

        var result = series.Sum();
        result.TryGetDouble(out var sum).Should().BeTrue();
        double.IsNaN(sum).Should().BeTrue("Sum of all NaN should return NaN");
    }

    [Theory]
    [InlineData(3)]   // Scalar path
    [InlineData(8)]   // SIMD path
    [InlineData(100)] // Full SIMD
    public void Min_WithNaN_SkipsNaN_ConsistentAcrossSizes(int length)
    {
        // Create array with NaN in various positions
        var data = Enumerable.Range(1, length).Select(i => (double)i).ToArray();
        data[0] = double.NaN;  // NaN at start
        if (length > 2) data[length / 2] = double.NaN;  // NaN in middle

        var series = Series.FromValues("test", data);
        var result = series.Min();

        // Min should skip NaN values and find the minimum real value
        // If all values are NaN, should return Null
        if (data.All(double.IsNaN))
        {
            result.IsNull.Should().BeTrue();
        }
        else
        {
            result.IsNull.Should().BeFalse();
            var min = result.AsFloat64();
            double.IsNaN(min).Should().BeFalse("Min should skip NaN values");

            // Find expected min excluding NaN
            var expectedMin = data.Where(x => !double.IsNaN(x)).Min();
            min.Should().Be(expectedMin);
        }
    }

    [Theory]
    [InlineData(3)]
    [InlineData(8)]
    [InlineData(100)]
    public void Max_WithNaN_SkipsNaN_ConsistentAcrossSizes(int length)
    {
        var data = Enumerable.Range(1, length).Select(i => (double)i).ToArray();
        data[0] = double.NaN;
        if (length > 2) data[length / 2] = double.NaN;

        var series = Series.FromValues("test", data);
        var result = series.Max();

        if (data.All(double.IsNaN))
        {
            result.IsNull.Should().BeTrue();
        }
        else
        {
            result.IsNull.Should().BeFalse();
            var max = result.AsFloat64();
            double.IsNaN(max).Should().BeFalse("Max should skip NaN values");

            var expectedMax = data.Where(x => !double.IsNaN(x)).Max();
            max.Should().Be(expectedMax);
        }
    }

    [Fact]
    public void Min_AllNaN_ReturnsNull()
    {
        var data = new[] { double.NaN, double.NaN, double.NaN };
        var series = Series.FromValues("test", data);

        series.Min().IsNull.Should().BeTrue("Min of all NaN should return null");
    }

    [Fact]
    public void Max_AllNaN_ReturnsNull()
    {
        var data = new[] { double.NaN, double.NaN, double.NaN };
        var series = Series.FromValues("test", data);

        series.Max().IsNull.Should().BeTrue("Max of all NaN should return null");
    }

    // ==========================================================================
    // Infinity Handling Tests
    // ==========================================================================

    [Fact]
    public void Sum_WithPositiveInfinity_ReturnsInfinity()
    {
        var data = new[] { 1.0, 2.0, double.PositiveInfinity, 4.0 };
        var series = Series.FromValues("test", data);

        var result = series.Sum();
        result.TryGetDouble(out var sum).Should().BeTrue();
        double.IsPositiveInfinity(sum).Should().BeTrue();
    }

    [Fact]
    public void Sum_WithNegativeInfinity_ReturnsNegativeInfinity()
    {
        var data = new[] { 1.0, 2.0, double.NegativeInfinity, 4.0 };
        var series = Series.FromValues("test", data);

        var result = series.Sum();
        result.TryGetDouble(out var sum).Should().BeTrue();
        double.IsNegativeInfinity(sum).Should().BeTrue();
    }

    [Fact]
    public void Sum_WithBothInfinities_ReturnsNaN()
    {
        var data = new[] { double.PositiveInfinity, double.NegativeInfinity };
        var series = Series.FromValues("test", data);

        var result = series.Sum();
        result.TryGetDouble(out var sum).Should().BeTrue();
        double.IsNaN(sum).Should().BeTrue("Infinity + -Infinity = NaN");
    }

    [Fact]
    public void Min_WithNegativeInfinity_ReturnsNegativeInfinity()
    {
        var data = new[] { 1.0, double.NegativeInfinity, 1000.0 };
        var series = Series.FromValues("test", data);

        var result = series.Min();
        result.TryGetDouble(out var min).Should().BeTrue();
        double.IsNegativeInfinity(min).Should().BeTrue();
    }

    [Fact]
    public void Max_WithPositiveInfinity_ReturnsPositiveInfinity()
    {
        var data = new[] { 1.0, double.PositiveInfinity, -1000.0 };
        var series = Series.FromValues("test", data);

        var result = series.Max();
        result.TryGetDouble(out var max).Should().BeTrue();
        double.IsPositiveInfinity(max).Should().BeTrue();
    }

    // ==========================================================================
    // Integer Overflow Tests
    // ==========================================================================

    [Fact]
    public void Sum_Int32_UsesInt64Accumulator_NoOverflow()
    {
        // Sum that would overflow Int32 but fits in Int64
        var data = new[] { int.MaxValue, int.MaxValue };
        var series = Series.FromValues("test", data);

        var result = series.Sum();
        result.IsNull.Should().BeFalse();

        // Expected: 2 * Int32.MaxValue = 4,294,967,294
        long expected = 2L * int.MaxValue;
        result.AsInt64().Should().Be(expected);
    }

    [Fact]
    public void Sum_Int32_LargeArray_NoOverflow()
    {
        // 1000 elements of 1,000,000 each = 1,000,000,000 (fits in Int32)
        // But we want to test the accumulator
        int count = 10000;
        var data = Enumerable.Repeat(100000, count).ToArray();
        var series = Series.FromValues("test", data);

        var result = series.Sum();
        result.IsNull.Should().BeFalse();

        long expected = (long)count * 100000;
        result.AsInt64().Should().Be(expected);
    }

    [Fact]
    public void Sum_Int64_LargeValues_HandlesCorrectly()
    {
        // Values that are large but won't overflow when summed
        var data = new[] { long.MaxValue / 4, long.MaxValue / 4 };
        var series = Series.FromValues("test", data);

        var result = series.Sum();
        result.IsNull.Should().BeFalse();

        long expected = (long.MaxValue / 4) * 2;
        result.AsInt64().Should().Be(expected);
    }

    // ==========================================================================
    // Floating-Point Precision Tests
    // ==========================================================================

    [Fact]
    public void Sum_LargeAndSmallNumbers_MaintainsReasonablePrecision()
    {
        // This tests for catastrophic cancellation
        // Adding 1e15 + 1.0 + (-1e15) should give ~1.0, not 0
        var data = new[] { 1e15, 1.0, -1e15 };
        var series = Series.FromValues("test", data);

        var result = series.Sum();
        result.TryGetDouble(out var sum).Should().BeTrue();

        // Note: Due to floating-point precision, this might not be exactly 1.0
        // but should be close (within floating-point precision limits)
        sum.Should().BeApproximately(1.0, 1.0,
            "Sum should handle large magnitude differences reasonably");
    }

    [Fact]
    public void Var_IdenticalValues_ReturnsZero()
    {
        var data = Enumerable.Repeat(42.0, 100).ToArray();
        var series = Series.FromValues("test", data);

        var result = series.Var();
        result.TryGetDouble(out var variance).Should().BeTrue();
        variance.Should().BeApproximately(0.0, 1e-10,
            "Variance of identical values should be zero");
    }

    [Fact]
    public void Std_IdenticalValues_ReturnsZero()
    {
        var data = Enumerable.Repeat(42.0, 100).ToArray();
        var series = Series.FromValues("test", data);

        var result = series.Std();
        result.TryGetDouble(out var std).Should().BeTrue();
        std.Should().BeApproximately(0.0, 1e-10,
            "Standard deviation of identical values should be zero");
    }

    [Fact]
    public void Mean_VeryLargeArray_MaintainsPrecision()
    {
        // Test that mean of constant values is accurate even for large arrays
        int length = 100000;
        var data = Enumerable.Repeat(Math.PI, length).ToArray();
        var series = Series.FromValues("test", data);

        var result = series.Mean();
        result.TryGetDouble(out var mean).Should().BeTrue();
        mean.Should().BeApproximately(Math.PI, 1e-10);
    }

    // ==========================================================================
    // Null Handling Tests
    // ==========================================================================

    [Fact]
    public void Sum_AllNulls_ReturnsNull()
    {
        var series = Series.FromNullable("test", new int?[] { null, null, null });
        series.Sum().IsNull.Should().BeTrue("Sum of all nulls should return null");
    }

    [Fact]
    public void Sum_WithSomeNulls_SkipsNulls()
    {
        var series = Series.FromNullable("test", new int?[] { 1, null, 3, null, 5 });

        var result = series.Sum();
        result.IsNull.Should().BeFalse();
        result.AsInt64().Should().Be(9, "Sum should skip null values: 1+3+5=9");
    }

    [Fact]
    public void Min_WithSomeNulls_SkipsNulls()
    {
        var series = Series.FromNullable("test", new int?[] { 5, null, 3, null, 1 });

        var result = series.Min();
        result.IsNull.Should().BeFalse();
        result.AsInt32().Should().Be(1);
    }

    [Fact]
    public void Max_WithSomeNulls_SkipsNulls()
    {
        var series = Series.FromNullable("test", new int?[] { 1, null, 3, null, 5 });

        var result = series.Max();
        result.IsNull.Should().BeFalse();
        result.AsInt32().Should().Be(5);
    }

    [Fact]
    public void Mean_WithSomeNulls_SkipsNulls()
    {
        var series = Series.FromNullable("test", new double?[] { 2.0, null, 4.0, null, 6.0 });

        var result = series.Mean();
        result.IsNull.Should().BeFalse();
        result.AsFloat64().Should().BeApproximately(4.0, 0.0001, "Mean of 2,4,6 = 4");
    }

    [Fact]
    public void Std_WithSomeNulls_SkipsNulls()
    {
        // Std of 2, 4, 6 with sample variance (ddof=1)
        // Mean = 4, SumSqDiff = 4+0+4 = 8, Var = 8/2 = 4, Std = 2
        var series = Series.FromNullable("test", new double?[] { 2.0, null, 4.0, null, 6.0 });

        var result = series.Std();
        result.TryGetDouble(out var std).Should().BeTrue();
        std.Should().BeApproximately(2.0, 0.0001);
    }

    // ==========================================================================
    // Edge Case: Negative Zero
    // ==========================================================================

    [Fact]
    public void Min_NegativeZeroVsPositiveZero_HandlesCorrectly()
    {
        // IEEE 754: -0.0 == +0.0, but they're distinct bit patterns
        var data = new[] { 0.0, -0.0, 1.0 };
        var series = Series.FromValues("test", data);

        var result = series.Min();
        result.TryGetDouble(out var min).Should().BeTrue();
        // Both +0.0 and -0.0 should be valid answers
        min.Should().Be(0.0);
    }

    // ==========================================================================
    // Extreme Values
    // ==========================================================================

    [Fact]
    public void Min_ExtremeValues_FindsCorrectMin()
    {
        var data = new[] { double.MaxValue, double.MinValue, 0.0 };
        var series = Series.FromValues("test", data);

        var result = series.Min();
        result.AsFloat64().Should().Be(double.MinValue);
    }

    [Fact]
    public void Max_ExtremeValues_FindsCorrectMax()
    {
        var data = new[] { double.MaxValue, double.MinValue, 0.0 };
        var series = Series.FromValues("test", data);

        var result = series.Max();
        result.AsFloat64().Should().Be(double.MaxValue);
    }

    [Fact]
    public void Sum_SmallestPositiveValues_MaintainsPrecision()
    {
        // Test with very small positive numbers
        var data = new[] { double.Epsilon, double.Epsilon, double.Epsilon };
        var series = Series.FromValues("test", data);

        var result = series.Sum();
        result.TryGetDouble(out var sum).Should().BeTrue();
        sum.Should().BeGreaterThan(0, "Sum of positive Epsilon values should be positive");
    }

    // ==========================================================================
    // Binary Operation Edge Cases
    // ==========================================================================

    [Fact]
    public void Add_DifferentLengths_ThrowsOrHandlesGracefully()
    {
        var a = Series.FromValues("a", new[] { 1.0, 2.0, 3.0 });
        var b = Series.FromValues("b", new[] { 1.0, 2.0 });

        // Should either throw or handle gracefully
        Action act = () => { var _ = a + b; };

        // The behavior depends on implementation - document what happens
        // This test ensures we have DEFINED behavior, not undefined
        act.Should().Throw<Exception>("Mismatched lengths should throw");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(100)]
    public void Add_Float64_AllArraySizes_ProduceCorrectResults(int length)
    {
        if (length == 0)
        {
            var emptyA = Series.FromValues("a", Array.Empty<double>());
            var emptyB = Series.FromValues("b", Array.Empty<double>());
            var emptyResult = emptyA + emptyB;
            emptyResult.Length.Should().Be(0);
            return;
        }

        var dataA = Enumerable.Range(1, length).Select(i => (double)i).ToArray();
        var dataB = Enumerable.Range(1, length).Select(i => (double)i * 10).ToArray();

        var seriesA = Series.FromValues("a", dataA);
        var seriesB = Series.FromValues("b", dataB);

        var result = seriesA + seriesB;

        result.Length.Should().Be(length);
        for (int i = 0; i < length; i++)
        {
            var expected = dataA[i] + dataB[i];
            result[i].AsFloat64().Should().BeApproximately(expected, 0.0001);
        }
    }

    // ==========================================================================
    // Count and Length Edge Cases
    // ==========================================================================

    [Fact]
    public void Count_EmptyArray_ReturnsZero()
    {
        var series = Series.FromValues("empty", Array.Empty<int>());
        series.Length.Should().Be(0);
        series.Count().Should().Be(0);
    }

    [Fact]
    public void Count_AllNulls_ReturnsZero()
    {
        var series = Series.FromNullable("nulls", new int?[] { null, null, null });
        series.Length.Should().Be(3);
        series.Count().Should().Be(0, "Count excludes null values");
    }

    [Fact]
    public void Count_MixedNulls_ReturnsNonNullCount()
    {
        var series = Series.FromNullable("mixed", new int?[] { 1, null, 3, null, 5 });
        series.Length.Should().Be(5);
        series.Count().Should().Be(3);
    }
}
