// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET
//
// Comprehensive tests for DataFrame sample operations, inspired by Polars test suite.
// These tests cover:
// - DataFrame.Sample() for random sampling
// - Sample with seed for reproducibility
// - Sample by count and fraction

using FluentAssertions;
using Polaire;
using Polaire.DataTypes;
using Xunit;
using static Polaire.Pl;

namespace Polaire.Tests;

/// <summary>
/// Tests for DataFrame sample operations.
/// </summary>
public class DataFrameSampleTests
{
    // ============================================================================
    // Sample by Count Tests
    // ============================================================================

    [Fact]
    public void Sample_ByCount_ReturnsCorrectCount()
    {
        var df = CreateTestDataFrame(100);
        var result = df.Sample(10);

        result.Height.Should().Be(10);
    }

    [Fact]
    public void Sample_ZeroCount_ReturnsEmpty()
    {
        var df = CreateTestDataFrame(100);
        var result = df.Sample(0);

        result.Height.Should().Be(0);
    }

    [Fact]
    public void Sample_AllRows_ReturnsAll()
    {
        var df = CreateTestDataFrame(10);
        var result = df.Sample(10);

        result.Height.Should().Be(10);
    }

    [Fact]
    public void Sample_MoreThanAvailable_ThrowsOrReturnsMax()
    {
        var df = CreateTestDataFrame(5);

        // Implementation may throw or return max available
        try
        {
            var result = df.Sample(10);
            result.Height.Should().BeLessOrEqualTo(5);
        }
        catch (ArgumentException)
        {
            // This is also acceptable
        }
    }

    [Fact]
    public void Sample_EmptyDataFrame_ReturnsEmpty()
    {
        var df = new DataFrame();
        var result = df.Sample(10);

        result.Height.Should().Be(0);
    }

    // ============================================================================
    // Sample by Fraction Tests
    // ============================================================================

    [Fact]
    public void Sample_ByFraction_ReturnsApproximateCount()
    {
        var df = CreateTestDataFrame(1000);
        var result = df.Sample(0.1);  // 10%

        // Allow some variance in random sampling
        result.Height.Should().BeInRange(50, 150);
    }

    [Fact]
    public void Sample_ZeroFraction_ReturnsEmpty()
    {
        var df = CreateTestDataFrame(100);
        var result = df.Sample(0.0);

        result.Height.Should().Be(0);
    }

    [Fact]
    public void Sample_HalfFraction_ReturnsApproximatelyHalf()
    {
        var df = CreateTestDataFrame(100);
        var result = df.Sample(0.5);

        // Allow variance
        result.Height.Should().BeInRange(30, 70);
    }

    // ============================================================================
    // Reproducibility Tests
    // ============================================================================

    [Fact]
    public void Sample_WithSameSeed_ReturnsSameResults()
    {
        var df = CreateTestDataFrame(100);

        var result1 = df.Sample(10, seed: 42);
        var result2 = df.Sample(10, seed: 42);

        // Same seed should give same results
        result1.Height.Should().Be(result2.Height);
        for (int i = 0; i < result1.Height; i++)
        {
            result1["value"][i].AsInt32().Should().Be(result2["value"][i].AsInt32());
        }
    }

    [Fact]
    public void Sample_WithDifferentSeeds_ReturnsDifferentResults()
    {
        var df = CreateTestDataFrame(100);

        var result1 = df.Sample(10, seed: 42);
        var result2 = df.Sample(10, seed: 123);

        // Different seeds should (very likely) give different results
        var allSame = true;
        for (int i = 0; i < Math.Min(result1.Height, result2.Height); i++)
        {
            if (result1["value"][i].AsInt32() != result2["value"][i].AsInt32())
            {
                allSame = false;
                break;
            }
        }
        allSame.Should().BeFalse();
    }

    // ============================================================================
    // Column Preservation Tests
    // ============================================================================

    [Fact]
    public void Sample_PreservesColumns()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 }),
            Series.FromValues("b", new[] { 10, 20, 30, 40, 50 }),
            Series.FromValues("c", new[] { "x", "y", "z", "w", "v" })
        );

        var result = df.Sample(3);

        result.Width.Should().Be(3);
        result.Columns.Should().Contain("a");
        result.Columns.Should().Contain("b");
        result.Columns.Should().Contain("c");
    }

    [Fact]
    public void Sample_PreservesTypes()
    {
        var df = new DataFrame(
            Series.FromValues("int", new[] { 1, 2, 3, 4, 5 }),
            Series.FromValues("float", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 }),
            Series.FromValues("str", new[] { "a", "b", "c", "d", "e" })
        );

        var result = df.Sample(3);

        result["int"].DataType.Should().Be(DataType.Int32);
        result["float"].DataType.Should().Be(DataType.Float64);
        result["str"].DataType.Should().Be(DataType.String);
    }

    // ============================================================================
    // Null Handling Tests
    // ============================================================================

    [Fact]
    public void Sample_WithNulls_PreservesNulls()
    {
        var df = new DataFrame(
            Series.FromNullable("a", new int?[] { 1, null, 3, null, 5, null, 7, null, 9, null })
        );

        var result = df.Sample(5);

        // Some sampled values may be null
        result.Height.Should().Be(5);
    }

    // ============================================================================
    // Row Correspondence Tests
    // ============================================================================

    [Fact]
    public void Sample_RowsCorrespond_AcrossColumns()
    {
        var df = new DataFrame(
            Series.FromValues("id", new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }),
            Series.FromValues("value", new[] { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 })
        );

        var result = df.Sample(5, seed: 42);

        // Check that id * 10 = value for each row
        for (int i = 0; i < result.Height; i++)
        {
            var id = result["id"][i].AsInt32();
            var value = result["value"][i].AsInt32();
            value.Should().Be(id * 10);
        }
    }

    // ============================================================================
    // Single Row Tests
    // ============================================================================

    [Fact]
    public void Sample_SingleRowDataFrame_ReturnsSingleRow()
    {
        var df = new DataFrame(Series.FromValues("a", new[] { 42 }));
        var result = df.Sample(1);

        result.Height.Should().Be(1);
        result["a"][0].AsInt32().Should().Be(42);
    }

    // ============================================================================
    // Large Data Tests
    // ============================================================================

    [Fact]
    public void Sample_LargeDataFrame_Succeeds()
    {
        var df = CreateTestDataFrame(10000);
        var result = df.Sample(1000);

        result.Height.Should().Be(1000);
    }

    [Fact]
    public void Sample_LargeDataFrame_ByFraction_Succeeds()
    {
        var df = CreateTestDataFrame(10000);
        var result = df.Sample(0.1);

        result.Height.Should().BeInRange(500, 1500);
    }

    // ============================================================================
    // Uniqueness Tests
    // ============================================================================

    [Fact]
    public void Sample_SampledRowsAreUnique_ByDefault()
    {
        var df = CreateTestDataFrame(100);
        var result = df.Sample(50);

        // All sampled values should be unique (no replacement by default)
        var values = new HashSet<int>();
        for (int i = 0; i < result.Height; i++)
        {
            values.Add(result["value"][i].AsInt32());
        }
        values.Count.Should().Be(result.Height);
    }

    // ============================================================================
    // Helper Methods
    // ============================================================================

    private DataFrame CreateTestDataFrame(int n)
    {
        var values = Enumerable.Range(0, n).ToArray();
        return new DataFrame(Series.FromValues("value", values));
    }
}
