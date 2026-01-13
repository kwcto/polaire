// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET
//
// Comprehensive tests for Series comparison operations, inspired by Polars test suite.
// These tests cover:
// - Equality comparisons (Eq, Ne)
// - Ordering comparisons (Lt, Le, Gt, Ge)
// - Null checks (IsNull, IsNotNull)
// - NaN checks (IsNaN, IsNotNaN)
// - Membership checks (IsIn, Between)

using FluentAssertions;
using Polaire;
using Polaire.DataTypes;
using Xunit;

namespace Polaire.Tests;

/// <summary>
/// Tests for Series comparison operations.
/// </summary>
public class SeriesComparisonTests
{
    // ============================================================================
    // Eq (Equal) Tests - Scalar
    // ============================================================================

    [Fact]
    public void Eq_Scalar_ReturnsCorrectBooleans()
    {
        var series = Series.FromValues("s", new[] { 1, 2, 3, 2, 1 });
        var result = series.Eq(AnyValue.From(2));

        result.DataType.Should().Be(DataType.Boolean);
        result[0].AsBoolean().Should().BeFalse();
        result[1].AsBoolean().Should().BeTrue();
        result[2].AsBoolean().Should().BeFalse();
        result[3].AsBoolean().Should().BeTrue();
        result[4].AsBoolean().Should().BeFalse();
    }

    [Fact]
    public void Eq_Scalar_Strings_ReturnsCorrectBooleans()
    {
        var series = Series.FromValues("s", new[] { "a", "b", "c", "b" });
        var result = series.Eq(AnyValue.From("b"));

        result[0].AsBoolean().Should().BeFalse();
        result[1].AsBoolean().Should().BeTrue();
        result[2].AsBoolean().Should().BeFalse();
        result[3].AsBoolean().Should().BeTrue();
    }

    [Fact]
    public void Eq_Scalar_Floats_ReturnsCorrectBooleans()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0 });
        var result = series.Eq(AnyValue.From(2.0));

        result[0].AsBoolean().Should().BeFalse();
        result[1].AsBoolean().Should().BeTrue();
        result[2].AsBoolean().Should().BeFalse();
    }

    // ============================================================================
    // Ne (Not Equal) Tests - Scalar
    // ============================================================================

    [Fact]
    public void Ne_Scalar_ReturnsCorrectBooleans()
    {
        var series = Series.FromValues("s", new[] { 1, 2, 3, 2, 1 });
        var result = series.Ne(AnyValue.From(2));

        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeFalse();
        result[2].AsBoolean().Should().BeTrue();
        result[3].AsBoolean().Should().BeFalse();
        result[4].AsBoolean().Should().BeTrue();
    }

    // ============================================================================
    // Lt (Less Than) Tests - Scalar
    // ============================================================================

    [Fact]
    public void Lt_Scalar_ReturnsCorrectBooleans()
    {
        var series = Series.FromValues("s", new[] { 1, 2, 3, 4, 5 });
        var result = series.Lt(AnyValue.From(3));

        result[0].AsBoolean().Should().BeTrue();   // 1 < 3
        result[1].AsBoolean().Should().BeTrue();   // 2 < 3
        result[2].AsBoolean().Should().BeFalse();  // 3 < 3
        result[3].AsBoolean().Should().BeFalse();  // 4 < 3
        result[4].AsBoolean().Should().BeFalse();  // 5 < 3
    }

    [Fact]
    public void Lt_Scalar_Floats_ReturnsCorrectBooleans()
    {
        var series = Series.FromValues("s", new[] { 1.5, 2.5, 3.5 });
        var result = series.Lt(AnyValue.From(3.0));

        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeTrue();
        result[2].AsBoolean().Should().BeFalse();
    }

    // ============================================================================
    // Le (Less Than or Equal) Tests - Scalar
    // ============================================================================

    [Fact]
    public void Le_Scalar_ReturnsCorrectBooleans()
    {
        var series = Series.FromValues("s", new[] { 1, 2, 3, 4, 5 });
        var result = series.Le(AnyValue.From(3));

        result[0].AsBoolean().Should().BeTrue();   // 1 <= 3
        result[1].AsBoolean().Should().BeTrue();   // 2 <= 3
        result[2].AsBoolean().Should().BeTrue();   // 3 <= 3
        result[3].AsBoolean().Should().BeFalse();  // 4 <= 3
        result[4].AsBoolean().Should().BeFalse();  // 5 <= 3
    }

    // ============================================================================
    // Gt (Greater Than) Tests - Scalar
    // ============================================================================

    [Fact]
    public void Gt_Scalar_ReturnsCorrectBooleans()
    {
        var series = Series.FromValues("s", new[] { 1, 2, 3, 4, 5 });
        var result = series.Gt(AnyValue.From(3));

        result[0].AsBoolean().Should().BeFalse();  // 1 > 3
        result[1].AsBoolean().Should().BeFalse();  // 2 > 3
        result[2].AsBoolean().Should().BeFalse();  // 3 > 3
        result[3].AsBoolean().Should().BeTrue();   // 4 > 3
        result[4].AsBoolean().Should().BeTrue();   // 5 > 3
    }

    // ============================================================================
    // Ge (Greater Than or Equal) Tests - Scalar
    // ============================================================================

    [Fact]
    public void Ge_Scalar_ReturnsCorrectBooleans()
    {
        var series = Series.FromValues("s", new[] { 1, 2, 3, 4, 5 });
        var result = series.Ge(AnyValue.From(3));

        result[0].AsBoolean().Should().BeFalse();  // 1 >= 3
        result[1].AsBoolean().Should().BeFalse();  // 2 >= 3
        result[2].AsBoolean().Should().BeTrue();   // 3 >= 3
        result[3].AsBoolean().Should().BeTrue();   // 4 >= 3
        result[4].AsBoolean().Should().BeTrue();   // 5 >= 3
    }

    // ============================================================================
    // Series vs Series Comparisons
    // ============================================================================

    [Fact]
    public void Eq_Series_ReturnsElementwiseComparison()
    {
        var a = Series.FromValues("a", new[] { 1, 2, 3, 4, 5 });
        var b = Series.FromValues("b", new[] { 1, 0, 3, 0, 5 });
        var result = a.Eq(b);

        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeFalse();
        result[2].AsBoolean().Should().BeTrue();
        result[3].AsBoolean().Should().BeFalse();
        result[4].AsBoolean().Should().BeTrue();
    }

    [Fact]
    public void Lt_Series_ReturnsElementwiseComparison()
    {
        var a = Series.FromValues("a", new[] { 1, 2, 3, 4, 5 });
        var b = Series.FromValues("b", new[] { 2, 2, 2, 2, 2 });
        var result = a.Lt(b);

        result[0].AsBoolean().Should().BeTrue();   // 1 < 2
        result[1].AsBoolean().Should().BeFalse();  // 2 < 2
        result[2].AsBoolean().Should().BeFalse();  // 3 < 2
        result[3].AsBoolean().Should().BeFalse();  // 4 < 2
        result[4].AsBoolean().Should().BeFalse();  // 5 < 2
    }

    [Fact]
    public void Gt_Series_ReturnsElementwiseComparison()
    {
        var a = Series.FromValues("a", new[] { 1, 2, 3, 4, 5 });
        var b = Series.FromValues("b", new[] { 2, 2, 2, 2, 2 });
        var result = a.Gt(b);

        result[0].AsBoolean().Should().BeFalse();  // 1 > 2
        result[1].AsBoolean().Should().BeFalse();  // 2 > 2
        result[2].AsBoolean().Should().BeTrue();   // 3 > 2
        result[3].AsBoolean().Should().BeTrue();   // 4 > 2
        result[4].AsBoolean().Should().BeTrue();   // 5 > 2
    }

    // ============================================================================
    // IsNull Tests
    // ============================================================================

    [Fact]
    public void IsNull_WithNulls_ReturnsCorrectBooleans()
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
    public void IsNull_NoNulls_ReturnsFalse()
    {
        var series = Series.FromValues("s", new[] { 1, 2, 3 });
        var result = series.IsNull();

        result[0].AsBoolean().Should().BeFalse();
        result[1].AsBoolean().Should().BeFalse();
        result[2].AsBoolean().Should().BeFalse();
    }

    [Fact]
    public void IsNull_AllNulls_ReturnsTrue()
    {
        var series = Series.FromNullable("s", new int?[] { null, null, null });
        var result = series.IsNull();

        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeTrue();
        result[2].AsBoolean().Should().BeTrue();
    }

    // ============================================================================
    // IsNotNull Tests
    // ============================================================================

    [Fact]
    public void IsNotNull_WithNulls_ReturnsCorrectBooleans()
    {
        var series = Series.FromNullable("s", new int?[] { 1, null, 3, null, 5 });
        var result = series.IsNotNull();

        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeFalse();
        result[2].AsBoolean().Should().BeTrue();
        result[3].AsBoolean().Should().BeFalse();
        result[4].AsBoolean().Should().BeTrue();
    }

    // ============================================================================
    // IsNaN Tests
    // ============================================================================

    [Fact]
    public void IsNaN_WithNaN_ReturnsCorrectBooleans()
    {
        var series = Series.FromValues("s", new[] { 1.0, double.NaN, 3.0, double.NaN });
        var result = series.IsNaN();

        result[0].AsBoolean().Should().BeFalse();
        result[1].AsBoolean().Should().BeTrue();
        result[2].AsBoolean().Should().BeFalse();
        result[3].AsBoolean().Should().BeTrue();
    }

    [Fact]
    public void IsNaN_NoNaN_ReturnsFalse()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0 });
        var result = series.IsNaN();

        result[0].AsBoolean().Should().BeFalse();
        result[1].AsBoolean().Should().BeFalse();
        result[2].AsBoolean().Should().BeFalse();
    }

    [Fact]
    public void IsNaN_Infinity_ReturnsFalse()
    {
        var series = Series.FromValues("s", new[] { double.PositiveInfinity, double.NegativeInfinity });
        var result = series.IsNaN();

        result[0].AsBoolean().Should().BeFalse();
        result[1].AsBoolean().Should().BeFalse();
    }

    // ============================================================================
    // IsNotNaN Tests
    // ============================================================================

    [Fact]
    public void IsNotNaN_WithNaN_ReturnsCorrectBooleans()
    {
        var series = Series.FromValues("s", new[] { 1.0, double.NaN, 3.0 });
        var result = series.IsNotNaN();

        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeFalse();
        result[2].AsBoolean().Should().BeTrue();
    }

    // ============================================================================
    // IsIn Tests
    // ============================================================================

    [Fact]
    public void IsIn_Values_ReturnsCorrectBooleans()
    {
        var series = Series.FromValues("s", new[] { 1, 2, 3, 4, 5 });
        var result = series.IsIn(AnyValue.From(2), AnyValue.From(4));

        result[0].AsBoolean().Should().BeFalse();
        result[1].AsBoolean().Should().BeTrue();
        result[2].AsBoolean().Should().BeFalse();
        result[3].AsBoolean().Should().BeTrue();
        result[4].AsBoolean().Should().BeFalse();
    }

    [Fact]
    public void IsIn_Strings_ReturnsCorrectBooleans()
    {
        var series = Series.FromValues("s", new[] { "a", "b", "c", "d" });
        var result = series.IsIn(AnyValue.From("a"), AnyValue.From("c"));

        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeFalse();
        result[2].AsBoolean().Should().BeTrue();
        result[3].AsBoolean().Should().BeFalse();
    }

    [Fact]
    public void IsIn_SingleValue_ReturnsCorrectBooleans()
    {
        var series = Series.FromValues("s", new[] { 1, 2, 3 });
        var result = series.IsIn(AnyValue.From(2));

        result[0].AsBoolean().Should().BeFalse();
        result[1].AsBoolean().Should().BeTrue();
        result[2].AsBoolean().Should().BeFalse();
    }

    [Fact]
    public void IsIn_NoMatches_ReturnsFalse()
    {
        var series = Series.FromValues("s", new[] { 1, 2, 3 });
        var result = series.IsIn(AnyValue.From(10), AnyValue.From(20));

        result[0].AsBoolean().Should().BeFalse();
        result[1].AsBoolean().Should().BeFalse();
        result[2].AsBoolean().Should().BeFalse();
    }

    // ============================================================================
    // Between Tests
    // ============================================================================

    [Fact]
    public void Between_IncludeBounds_ReturnsCorrectBooleans()
    {
        var series = Series.FromValues("s", new[] { 1, 2, 3, 4, 5 });
        var result = series.Between(AnyValue.From(2), AnyValue.From(4), includeBounds: true);

        result[0].AsBoolean().Should().BeFalse();  // 1 not in [2,4]
        result[1].AsBoolean().Should().BeTrue();   // 2 in [2,4]
        result[2].AsBoolean().Should().BeTrue();   // 3 in [2,4]
        result[3].AsBoolean().Should().BeTrue();   // 4 in [2,4]
        result[4].AsBoolean().Should().BeFalse();  // 5 not in [2,4]
    }

    [Fact]
    public void Between_ExcludeBounds_ReturnsCorrectBooleans()
    {
        var series = Series.FromValues("s", new[] { 1, 2, 3, 4, 5 });
        var result = series.Between(AnyValue.From(2), AnyValue.From(4), includeBounds: false);

        result[0].AsBoolean().Should().BeFalse();  // 1 not in (2,4)
        result[1].AsBoolean().Should().BeFalse();  // 2 not in (2,4)
        result[2].AsBoolean().Should().BeTrue();   // 3 in (2,4)
        result[3].AsBoolean().Should().BeFalse();  // 4 not in (2,4)
        result[4].AsBoolean().Should().BeFalse();  // 5 not in (2,4)
    }

    [Fact]
    public void Between_Floats_ReturnsCorrectBooleans()
    {
        var series = Series.FromValues("s", new[] { 1.5, 2.5, 3.5, 4.5 });
        var result = series.Between(AnyValue.From(2.0), AnyValue.From(4.0), includeBounds: true);

        result[0].AsBoolean().Should().BeFalse();  // 1.5 < 2.0
        result[1].AsBoolean().Should().BeTrue();   // 2.5 in [2.0, 4.0]
        result[2].AsBoolean().Should().BeTrue();   // 3.5 in [2.0, 4.0]
        result[3].AsBoolean().Should().BeFalse();  // 4.5 > 4.0
    }

    // ============================================================================
    // Comparison with Nulls
    // ============================================================================

    [Fact]
    public void Eq_WithNulls_HandlesNullsCorrectly()
    {
        var series = Series.FromNullable("s", new int?[] { 1, null, 3 });
        var result = series.Eq(AnyValue.From(1));

        result[0].AsBoolean().Should().BeTrue();
        // Note: Current implementation: null == non-null → false (not null)
        // This differs from Polars semantics which returns null
        result[1].AsBoolean().Should().BeFalse();
        result[2].AsBoolean().Should().BeFalse();
    }

    [Fact]
    public void Lt_WithNulls_HandlesNullsCorrectly()
    {
        var series = Series.FromNullable("s", new int?[] { 1, null, 3 });
        var result = series.Lt(AnyValue.From(2));

        result[0].AsBoolean().Should().BeTrue();
        // For lt/le/gt/ge, nulls propagate as null
        result.IsNull(1).Should().BeTrue();
        result[2].AsBoolean().Should().BeFalse();
    }

    // ============================================================================
    // Edge Cases
    // ============================================================================

    [Fact]
    public void Eq_EmptySeries_ReturnsEmpty()
    {
        var series = Series.FromValues("s", Array.Empty<int>());
        var result = series.Eq(AnyValue.From(1));

        result.Length.Should().Be(0);
    }

    [Fact]
    public void Lt_NegativeNumbers_ReturnsCorrectBooleans()
    {
        var series = Series.FromValues("s", new[] { -5, -3, 0, 3, 5 });
        var result = series.Lt(AnyValue.From(0));

        result[0].AsBoolean().Should().BeTrue();   // -5 < 0
        result[1].AsBoolean().Should().BeTrue();   // -3 < 0
        result[2].AsBoolean().Should().BeFalse();  // 0 < 0
        result[3].AsBoolean().Should().BeFalse();  // 3 < 0
        result[4].AsBoolean().Should().BeFalse();  // 5 < 0
    }

    [Fact]
    public void Eq_BooleanSeries_ReturnsCorrectBooleans()
    {
        var series = Series.FromValues("s", new[] { true, false, true, false });
        var result = series.Eq(AnyValue.From(true));

        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeFalse();
        result[2].AsBoolean().Should().BeTrue();
        result[3].AsBoolean().Should().BeFalse();
    }
}
