// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET
//
// Extended tests for Series operations, inspired by Polars test suite.
// These tests cover:
// - Series arithmetic operations
// - Series comparison operations
// - Series type conversions
// - Series indexing operations

using FluentAssertions;
using Polaire;
using Polaire.DataTypes;
using Xunit;
using static Polaire.Pl;

namespace Polaire.Tests;

/// <summary>
/// Extended tests for Series operations.
/// </summary>
public class SeriesOperationsExtendedTests
{
    // ============================================================================
    // Arithmetic Operations
    // ============================================================================

    [Fact]
    public void Add_TwoSeries_Works()
    {
        var a = Series.FromValues("a", new[] { 1.0, 2.0, 3.0 });
        var b = Series.FromValues("b", new[] { 10.0, 20.0, 30.0 });

        var result = a + b;

        result[0].AsFloat64().Should().Be(11.0);
        result[1].AsFloat64().Should().Be(22.0);
        result[2].AsFloat64().Should().Be(33.0);
    }

    [Fact]
    public void Subtract_TwoSeries_Works()
    {
        var a = Series.FromValues("a", new[] { 10.0, 20.0, 30.0 });
        var b = Series.FromValues("b", new[] { 1.0, 2.0, 3.0 });

        var result = a - b;

        result[0].AsFloat64().Should().Be(9.0);
        result[1].AsFloat64().Should().Be(18.0);
        result[2].AsFloat64().Should().Be(27.0);
    }

    [Fact]
    public void Multiply_TwoSeries_Works()
    {
        var a = Series.FromValues("a", new[] { 2.0, 3.0, 4.0 });
        var b = Series.FromValues("b", new[] { 10.0, 20.0, 30.0 });

        var result = a * b;

        result[0].AsFloat64().Should().Be(20.0);
        result[1].AsFloat64().Should().Be(60.0);
        result[2].AsFloat64().Should().Be(120.0);
    }

    [Fact]
    public void Divide_TwoSeries_Works()
    {
        var a = Series.FromValues("a", new[] { 10.0, 20.0, 30.0 });
        var b = Series.FromValues("b", new[] { 2.0, 4.0, 5.0 });

        var result = a / b;

        result[0].AsFloat64().Should().Be(5.0);
        result[1].AsFloat64().Should().Be(5.0);
        result[2].AsFloat64().Should().Be(6.0);
    }

    // ============================================================================
    // Scalar Arithmetic
    // ============================================================================

    [Fact]
    public void Add_Scalar_Works()
    {
        var a = Series.FromValues("a", new[] { 1.0, 2.0, 3.0 });

        var result = a + 10.0;

        result[0].AsFloat64().Should().Be(11.0);
        result[1].AsFloat64().Should().Be(12.0);
        result[2].AsFloat64().Should().Be(13.0);
    }

    [Fact]
    public void Multiply_Scalar_Works()
    {
        var a = Series.FromValues("a", new[] { 1.0, 2.0, 3.0 });

        var result = a * 2.0;

        result[0].AsFloat64().Should().Be(2.0);
        result[1].AsFloat64().Should().Be(4.0);
        result[2].AsFloat64().Should().Be(6.0);
    }

    // ============================================================================
    // Boolean Mask Operations
    // ============================================================================

    [Fact]
    public void GreaterThan_ReturnsBoolean()
    {
        var a = Series.FromValues("a", new[] { 1, 2, 3, 4, 5 });

        var result = a.Gt(3);

        result.DataType.Should().Be(DataType.Boolean);
        result[0].AsBoolean().Should().BeFalse();
        result[3].AsBoolean().Should().BeTrue();
        result[4].AsBoolean().Should().BeTrue();
    }

    [Fact]
    public void LessThan_ReturnsBoolean()
    {
        var a = Series.FromValues("a", new[] { 1, 2, 3, 4, 5 });

        var result = a.Lt(3);

        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeTrue();
        result[2].AsBoolean().Should().BeFalse();
    }

    [Fact]
    public void Eq_ReturnsBoolean()
    {
        var a = Series.FromValues("a", new[] { 1, 2, 3, 2, 1 });

        var result = a.Eq(2);

        result[0].AsBoolean().Should().BeFalse();
        result[1].AsBoolean().Should().BeTrue();
        result[3].AsBoolean().Should().BeTrue();
    }

    // ============================================================================
    // Filter by Boolean Mask (via DataFrame)
    // ============================================================================
    // Note: Series.Filter is not available. Use DataFrame.Filter instead.

    // ============================================================================
    // Indexing Operations
    // ============================================================================

    [Fact]
    public void Index_First_Works()
    {
        var s = Series.FromValues("s", new[] { 10, 20, 30, 40, 50 });

        s[0].AsInt32().Should().Be(10);
    }

    [Fact]
    public void Index_Last_Works()
    {
        var s = Series.FromValues("s", new[] { 10, 20, 30, 40, 50 });

        s[4].AsInt32().Should().Be(50);
    }

    [Fact]
    public void Index_Middle_Works()
    {
        var s = Series.FromValues("s", new[] { 10, 20, 30, 40, 50 });

        s[2].AsInt32().Should().Be(30);
    }

    [Fact]
    public void Index_OutOfRange_Throws()
    {
        var s = Series.FromValues("s", new[] { 10, 20, 30 });

        var act = () => s[10];

        act.Should().Throw<Exception>();
    }

    // ============================================================================
    // Clone and Copy Tests
    // ============================================================================
    // Note: Series.Clone is not available directly.

    // ============================================================================
    // Name Operations
    // ============================================================================

    [Fact]
    public void Rename_ChangesName()
    {
        var s = Series.FromValues("original", new[] { 1, 2, 3 });
        var renamed = s.Rename("new_name");

        renamed.Name.Should().Be("new_name");
        renamed.Length.Should().Be(3);
    }

    // ============================================================================
    // Type-Specific Operations
    // ============================================================================

    [Fact]
    public void Int32Series_Aggregations_Work()
    {
        var s = Series.FromValues("s", new[] { 1, 2, 3, 4, 5 });

        s.Sum().AsInt64().Should().Be(15);
        s.Min().AsInt32().Should().Be(1);
        s.Max().AsInt32().Should().Be(5);
        s.Mean().AsFloat64().Should().Be(3.0);
    }

    [Fact]
    public void Int64Series_Aggregations_Work()
    {
        var s = Series.FromValues("s", new long[] { 1, 2, 3, 4, 5 });

        s.Sum().AsInt64().Should().Be(15);
        s.Min().AsInt64().Should().Be(1);
        s.Max().AsInt64().Should().Be(5);
    }

    [Fact]
    public void Float32Series_Aggregations_Work()
    {
        var s = Series.FromValues("s", new float[] { 1.0f, 2.0f, 3.0f, 4.0f, 5.0f });

        s.Mean().AsFloat64().Should().BeApproximately(3.0, 0.001);
    }

    [Fact]
    public void Float64Series_Aggregations_Work()
    {
        var s = Series.FromValues("s", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 });

        s.Sum().AsFloat64().Should().Be(15.0);
        s.Mean().AsFloat64().Should().Be(3.0);
    }

    // ============================================================================
    // Empty Series Operations
    // ============================================================================

    [Fact]
    public void Empty_Length_IsZero()
    {
        var s = Series.FromValues("s", Array.Empty<int>());

        s.Length.Should().Be(0);
    }

    [Fact]
    public void Empty_Sum_ReturnsNull()
    {
        var s = Series.FromValues("s", Array.Empty<int>());

        var result = s.Sum();

        // Empty series sum returns null
        result.IsNull.Should().BeTrue();
    }

    [Fact]
    public void Empty_Head_ReturnsEmpty()
    {
        var s = Series.FromValues("s", Array.Empty<int>());

        var result = s.Head(5);

        result.Length.Should().Be(0);
    }

    [Fact]
    public void Empty_Tail_ReturnsEmpty()
    {
        var s = Series.FromValues("s", Array.Empty<int>());

        var result = s.Tail(5);

        result.Length.Should().Be(0);
    }

    // ============================================================================
    // Single Element Series
    // ============================================================================

    [Fact]
    public void Single_Sum_ReturnsValue()
    {
        var s = Series.FromValues("s", new[] { 42 });

        s.Sum().AsInt64().Should().Be(42);
    }

    [Fact]
    public void Single_Mean_ReturnsValue()
    {
        var s = Series.FromValues("s", new[] { 42.0 });

        s.Mean().AsFloat64().Should().Be(42.0);
    }

    [Fact]
    public void Single_Min_ReturnsValue()
    {
        var s = Series.FromValues("s", new[] { 42 });

        s.Min().AsInt32().Should().Be(42);
    }

    [Fact]
    public void Single_Max_ReturnsValue()
    {
        var s = Series.FromValues("s", new[] { 42 });

        s.Max().AsInt32().Should().Be(42);
    }

    // ============================================================================
    // Boolean Series Operations
    // ============================================================================

    [Fact]
    public void Boolean_Any_ReturnsTrue_IfAnyTrue()
    {
        var s = Series.FromValues("s", new[] { false, false, true, false });

        s.Any().Should().BeTrue();
    }

    [Fact]
    public void Boolean_Any_ReturnsFalse_IfAllFalse()
    {
        var s = Series.FromValues("s", new[] { false, false, false });

        s.Any().Should().BeFalse();
    }

    [Fact]
    public void Boolean_All_ReturnsTrue_IfAllTrue()
    {
        var s = Series.FromValues("s", new[] { true, true, true });

        s.All().Should().BeTrue();
    }

    [Fact]
    public void Boolean_All_ReturnsFalse_IfAnyFalse()
    {
        var s = Series.FromValues("s", new[] { true, false, true });

        s.All().Should().BeFalse();
    }

    // ============================================================================
    // Null Handling
    // ============================================================================

    [Fact]
    public void WithNulls_Sum_SkipsNulls()
    {
        var s = Series.FromNullable("s", new int?[] { 1, null, 3, null, 5 });

        s.Sum().AsInt64().Should().Be(9);  // 1 + 3 + 5
    }

    [Fact]
    public void WithNulls_Mean_SkipsNulls()
    {
        var s = Series.FromNullable("s", new double?[] { 1.0, null, 3.0 });

        s.Mean().AsFloat64().Should().Be(2.0);  // (1 + 3) / 2
    }

    [Fact]
    public void WithNulls_Count_ExcludesNulls()
    {
        var s = Series.FromNullable("s", new int?[] { 1, null, 3, null, 5 });

        s.Count().Should().Be(3);
    }

    // ============================================================================
    // Large Data Tests
    // ============================================================================

    [Fact]
    public void Large_Sum_Works()
    {
        var values = Enumerable.Range(1, 10000).Select(i => (double)i).ToArray();
        var s = Series.FromValues("s", values);

        var expected = (10000.0 * 10001.0) / 2.0;  // n(n+1)/2
        s.Sum().AsFloat64().Should().BeApproximately(expected, 0.001);
    }

    [Fact]
    public void Large_Sort_Works()
    {
        var random = new Random(42);
        var values = Enumerable.Range(0, 10000).Select(_ => random.Next(100000)).ToArray();
        var s = Series.FromValues("s", values);

        var result = s.Sort();

        result.Length.Should().Be(10000);
        // Check first few are sorted
        for (int i = 1; i < 100; i++)
        {
            result[i].AsInt32().Should().BeGreaterOrEqualTo(result[i - 1].AsInt32());
        }
    }

    // Note: Large filter tests use DataFrame.Filter since Series.Filter is not available.

    // ============================================================================
    // Edge Cases
    // ============================================================================

    [Fact]
    public void DivideByZero_ReturnsInfinity()
    {
        var a = Series.FromValues("a", new[] { 10.0, 20.0, 30.0 });
        var b = Series.FromValues("b", new[] { 2.0, 0.0, 5.0 });

        var result = a / b;

        result[0].AsFloat64().Should().Be(5.0);
        double.IsInfinity(result[1].AsFloat64()).Should().BeTrue();
        result[2].AsFloat64().Should().Be(6.0);
    }

    [Fact]
    public void NegativeNumbers_Sort_Works()
    {
        var s = Series.FromValues("s", new[] { -5, 3, -2, 0, 1, -3 });

        var result = s.Sort();

        result[0].AsInt32().Should().Be(-5);
        result[1].AsInt32().Should().Be(-3);
        result[2].AsInt32().Should().Be(-2);
        result[3].AsInt32().Should().Be(0);
    }

    [Fact]
    public void AllSameValue_Unique_ReturnsSingle()
    {
        var s = Series.FromValues("s", new[] { 5, 5, 5, 5, 5 });

        var result = s.Unique();

        result.Length.Should().Be(1);
        result[0].AsInt32().Should().Be(5);
    }

    [Fact]
    public void AllDifferent_Unique_ReturnsAll()
    {
        var s = Series.FromValues("s", new[] { 1, 2, 3, 4, 5 });

        var result = s.Unique();

        result.Length.Should().Be(5);
    }
}
