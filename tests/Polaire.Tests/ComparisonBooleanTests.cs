// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using FluentAssertions;
using Polaire;
using Polaire.DataTypes;
using Xunit;

namespace Polaire.Tests;

/// <summary>
/// Tests for comparison operations (Eq, Ne, Lt, Le, Gt, Ge) and boolean operations (And, Or, Xor, Not).
/// </summary>
public class ComparisonBooleanTests
{
    // ============================================================================
    // Series-to-Series Comparison Tests
    // ============================================================================

    [Fact]
    public void Eq_Int32Series_ReturnsCorrectResults()
    {
        var a = Series.FromValues("a", new[] { 1, 2, 3, 4, 5 });
        var b = Series.FromValues("b", new[] { 1, 3, 3, 3, 5 });

        var result = a.Eq(b);

        result.DataType.Should().Be(DataType.Boolean);
        result.Length.Should().Be(5);
        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeFalse();
        result[2].AsBoolean().Should().BeTrue();
        result[3].AsBoolean().Should().BeFalse();
        result[4].AsBoolean().Should().BeTrue();
    }

    [Fact]
    public void Eq_Float64Series_ReturnsCorrectResults()
    {
        var a = Series.FromValues("a", new[] { 1.0, 2.0, 3.0 });
        var b = Series.FromValues("b", new[] { 1.0, 2.5, 3.0 });

        var result = a.Eq(b);

        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeFalse();
        result[2].AsBoolean().Should().BeTrue();
    }

    [Fact]
    public void Eq_StringSeries_ReturnsCorrectResults()
    {
        var a = Series.FromValues("a", new[] { "hello", "world", "test" });
        var b = Series.FromValues("b", new[] { "hello", "WORLD", "test" });

        var result = a.Eq(b);

        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeFalse();
        result[2].AsBoolean().Should().BeTrue();
    }

    [Fact]
    public void Ne_ReturnsCorrectResults()
    {
        var a = Series.FromValues("a", new[] { 1, 2, 3 });
        var b = Series.FromValues("b", new[] { 1, 3, 3 });

        var result = a.Ne(b);

        result[0].AsBoolean().Should().BeFalse();
        result[1].AsBoolean().Should().BeTrue();
        result[2].AsBoolean().Should().BeFalse();
    }

    [Fact]
    public void Lt_ReturnsCorrectResults()
    {
        var a = Series.FromValues("a", new[] { 1, 2, 3 });
        var b = Series.FromValues("b", new[] { 2, 2, 2 });

        var result = a.Lt(b);

        result[0].AsBoolean().Should().BeTrue();  // 1 < 2
        result[1].AsBoolean().Should().BeFalse(); // 2 < 2
        result[2].AsBoolean().Should().BeFalse(); // 3 < 2
    }

    [Fact]
    public void Le_ReturnsCorrectResults()
    {
        var a = Series.FromValues("a", new[] { 1, 2, 3 });
        var b = Series.FromValues("b", new[] { 2, 2, 2 });

        var result = a.Le(b);

        result[0].AsBoolean().Should().BeTrue();  // 1 <= 2
        result[1].AsBoolean().Should().BeTrue();  // 2 <= 2
        result[2].AsBoolean().Should().BeFalse(); // 3 <= 2
    }

    [Fact]
    public void Gt_ReturnsCorrectResults()
    {
        var a = Series.FromValues("a", new[] { 1, 2, 3 });
        var b = Series.FromValues("b", new[] { 2, 2, 2 });

        var result = a.Gt(b);

        result[0].AsBoolean().Should().BeFalse(); // 1 > 2
        result[1].AsBoolean().Should().BeFalse(); // 2 > 2
        result[2].AsBoolean().Should().BeTrue();  // 3 > 2
    }

    [Fact]
    public void Ge_ReturnsCorrectResults()
    {
        var a = Series.FromValues("a", new[] { 1, 2, 3 });
        var b = Series.FromValues("b", new[] { 2, 2, 2 });

        var result = a.Ge(b);

        result[0].AsBoolean().Should().BeFalse(); // 1 >= 2
        result[1].AsBoolean().Should().BeTrue();  // 2 >= 2
        result[2].AsBoolean().Should().BeTrue();  // 3 >= 2
    }

    // ============================================================================
    // Scalar Comparison Tests
    // ============================================================================

    [Fact]
    public void Eq_ScalarInt_ReturnsCorrectResults()
    {
        var series = Series.FromValues("a", new[] { 1, 2, 3, 2, 4 });

        var result = series.Eq(AnyValue.From(2));

        result[0].AsBoolean().Should().BeFalse();
        result[1].AsBoolean().Should().BeTrue();
        result[2].AsBoolean().Should().BeFalse();
        result[3].AsBoolean().Should().BeTrue();
        result[4].AsBoolean().Should().BeFalse();
    }

    [Fact]
    public void Eq_ScalarDouble_ReturnsCorrectResults()
    {
        var series = Series.FromValues("a", new[] { 1.0, 2.5, 3.0, 2.5 });

        var result = series.Eq(AnyValue.From(2.5));

        result[0].AsBoolean().Should().BeFalse();
        result[1].AsBoolean().Should().BeTrue();
        result[2].AsBoolean().Should().BeFalse();
        result[3].AsBoolean().Should().BeTrue();
    }

    [Fact]
    public void Eq_ScalarString_ReturnsCorrectResults()
    {
        var series = Series.FromValues("a", new[] { "a", "b", "a", "c" });

        var result = series.Eq(AnyValue.From("a"));

        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeFalse();
        result[2].AsBoolean().Should().BeTrue();
        result[3].AsBoolean().Should().BeFalse();
    }

    [Fact]
    public void Ne_Scalar_ReturnsCorrectResults()
    {
        var series = Series.FromValues("a", new[] { 1, 2, 3 });

        var result = series.Ne(AnyValue.From(2));

        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeFalse();
        result[2].AsBoolean().Should().BeTrue();
    }

    [Fact]
    public void Lt_Scalar_ReturnsCorrectResults()
    {
        var series = Series.FromValues("a", new[] { 1, 2, 3, 4, 5 });

        var result = series.Lt(AnyValue.From(3));

        result[0].AsBoolean().Should().BeTrue();  // 1 < 3
        result[1].AsBoolean().Should().BeTrue();  // 2 < 3
        result[2].AsBoolean().Should().BeFalse(); // 3 < 3
        result[3].AsBoolean().Should().BeFalse(); // 4 < 3
        result[4].AsBoolean().Should().BeFalse(); // 5 < 3
    }

    [Fact]
    public void Le_Scalar_ReturnsCorrectResults()
    {
        var series = Series.FromValues("a", new[] { 1, 2, 3, 4, 5 });

        var result = series.Le(AnyValue.From(3));

        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeTrue();
        result[2].AsBoolean().Should().BeTrue();
        result[3].AsBoolean().Should().BeFalse();
        result[4].AsBoolean().Should().BeFalse();
    }

    [Fact]
    public void Gt_Scalar_ReturnsCorrectResults()
    {
        var series = Series.FromValues("a", new[] { 1, 2, 3, 4, 5 });

        var result = series.Gt(AnyValue.From(3));

        result[0].AsBoolean().Should().BeFalse();
        result[1].AsBoolean().Should().BeFalse();
        result[2].AsBoolean().Should().BeFalse();
        result[3].AsBoolean().Should().BeTrue();
        result[4].AsBoolean().Should().BeTrue();
    }

    [Fact]
    public void Ge_Scalar_ReturnsCorrectResults()
    {
        var series = Series.FromValues("a", new[] { 1, 2, 3, 4, 5 });

        var result = series.Ge(AnyValue.From(3));

        result[0].AsBoolean().Should().BeFalse();
        result[1].AsBoolean().Should().BeFalse();
        result[2].AsBoolean().Should().BeTrue();
        result[3].AsBoolean().Should().BeTrue();
        result[4].AsBoolean().Should().BeTrue();
    }

    // ============================================================================
    // Null Checking Tests
    // ============================================================================

    [Fact]
    public void IsNull_ReturnsCorrectResults()
    {
        var series = Series.FromNullable("a", new int?[] { 1, null, 3, null, 5 });

        var result = series.IsNull();

        result.DataType.Should().Be(DataType.Boolean);
        result[0].AsBoolean().Should().BeFalse();
        result[1].AsBoolean().Should().BeTrue();
        result[2].AsBoolean().Should().BeFalse();
        result[3].AsBoolean().Should().BeTrue();
        result[4].AsBoolean().Should().BeFalse();
    }

    [Fact]
    public void IsNotNull_ReturnsCorrectResults()
    {
        var series = Series.FromNullable("a", new int?[] { 1, null, 3, null, 5 });

        var result = series.IsNotNull();

        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeFalse();
        result[2].AsBoolean().Should().BeTrue();
        result[3].AsBoolean().Should().BeFalse();
        result[4].AsBoolean().Should().BeTrue();
    }

    [Fact]
    public void IsNull_WithNoNulls_AllFalse()
    {
        var series = Series.FromValues("a", new[] { 1, 2, 3 });

        var result = series.IsNull();

        result[0].AsBoolean().Should().BeFalse();
        result[1].AsBoolean().Should().BeFalse();
        result[2].AsBoolean().Should().BeFalse();
    }

    [Fact]
    public void IsNull_WithAllNulls_AllTrue()
    {
        var series = Series.FromNullable("a", new int?[] { null, null, null });

        var result = series.IsNull();

        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeTrue();
        result[2].AsBoolean().Should().BeTrue();
    }

    // ============================================================================
    // NaN Checking Tests
    // ============================================================================

    [Fact]
    public void IsNaN_WithFloatNaN_ReturnsTrue()
    {
        var series = Series.FromValues("a", new[] { 1.0, double.NaN, 3.0, double.NaN });

        var result = series.IsNaN();

        result[0].AsBoolean().Should().BeFalse();
        result[1].AsBoolean().Should().BeTrue();
        result[2].AsBoolean().Should().BeFalse();
        result[3].AsBoolean().Should().BeTrue();
    }

    [Fact]
    public void IsNotNaN_WithFloatNaN_ReturnsCorrectly()
    {
        var series = Series.FromValues("a", new[] { 1.0, double.NaN, 3.0 });

        var result = series.IsNotNaN();

        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeFalse();
        result[2].AsBoolean().Should().BeTrue();
    }

    [Fact]
    public void IsNaN_WithInfinity_ReturnsFalse()
    {
        var series = Series.FromValues("a", new[] { double.PositiveInfinity, double.NegativeInfinity, double.NaN });

        var result = series.IsNaN();

        result[0].AsBoolean().Should().BeFalse(); // Inf is not NaN
        result[1].AsBoolean().Should().BeFalse(); // -Inf is not NaN
        result[2].AsBoolean().Should().BeTrue();  // NaN is NaN
    }

    // ============================================================================
    // IsIn Tests
    // ============================================================================

    [Fact]
    public void IsIn_WithMatchingValues_ReturnsTrue()
    {
        var series = Series.FromValues("a", new[] { 1, 2, 3, 4, 5 });

        var result = series.IsIn(AnyValue.From(2), AnyValue.From(4));

        result[0].AsBoolean().Should().BeFalse();
        result[1].AsBoolean().Should().BeTrue();
        result[2].AsBoolean().Should().BeFalse();
        result[3].AsBoolean().Should().BeTrue();
        result[4].AsBoolean().Should().BeFalse();
    }

    [Fact]
    public void IsIn_WithStrings_ReturnsCorrectResults()
    {
        var series = Series.FromValues("a", new[] { "a", "b", "c", "d" });

        var result = series.IsIn(AnyValue.From("a"), AnyValue.From("c"));

        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeFalse();
        result[2].AsBoolean().Should().BeTrue();
        result[3].AsBoolean().Should().BeFalse();
    }

    [Fact]
    public void IsIn_WithNoMatches_AllFalse()
    {
        var series = Series.FromValues("a", new[] { 1, 2, 3 });

        var result = series.IsIn(AnyValue.From(10), AnyValue.From(20));

        result[0].AsBoolean().Should().BeFalse();
        result[1].AsBoolean().Should().BeFalse();
        result[2].AsBoolean().Should().BeFalse();
    }

    // ============================================================================
    // Between Tests
    // ============================================================================

    [Fact]
    public void Between_IncludeBounds_ReturnsCorrectResults()
    {
        var series = Series.FromValues("a", new[] { 1, 2, 3, 4, 5 });

        var result = series.Between(AnyValue.From(2), AnyValue.From(4), includeBounds: true);

        result[0].AsBoolean().Should().BeFalse(); // 1 not in [2,4]
        result[1].AsBoolean().Should().BeTrue();  // 2 in [2,4]
        result[2].AsBoolean().Should().BeTrue();  // 3 in [2,4]
        result[3].AsBoolean().Should().BeTrue();  // 4 in [2,4]
        result[4].AsBoolean().Should().BeFalse(); // 5 not in [2,4]
    }

    [Fact]
    public void Between_ExcludeBounds_ReturnsCorrectResults()
    {
        var series = Series.FromValues("a", new[] { 1, 2, 3, 4, 5 });

        var result = series.Between(AnyValue.From(2), AnyValue.From(4), includeBounds: false);

        result[0].AsBoolean().Should().BeFalse(); // 1 not in (2,4)
        result[1].AsBoolean().Should().BeFalse(); // 2 not in (2,4)
        result[2].AsBoolean().Should().BeTrue();  // 3 in (2,4)
        result[3].AsBoolean().Should().BeFalse(); // 4 not in (2,4)
        result[4].AsBoolean().Should().BeFalse(); // 5 not in (2,4)
    }

    [Fact]
    public void Between_WithFloats_ReturnsCorrectResults()
    {
        var series = Series.FromValues("a", new[] { 1.5, 2.5, 3.5, 4.5 });

        var result = series.Between(AnyValue.From(2.0), AnyValue.From(4.0));

        result[0].AsBoolean().Should().BeFalse();
        result[1].AsBoolean().Should().BeTrue();
        result[2].AsBoolean().Should().BeTrue();
        result[3].AsBoolean().Should().BeFalse();
    }

    // ============================================================================
    // Boolean Operations Tests
    // ============================================================================

    [Fact]
    public void And_ReturnsTrueOnlyWhenBothTrue()
    {
        var a = Series.FromValues("a", new[] { true, true, false, false });
        var b = Series.FromValues("b", new[] { true, false, true, false });

        var result = a.And(b);

        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeFalse();
        result[2].AsBoolean().Should().BeFalse();
        result[3].AsBoolean().Should().BeFalse();
    }

    [Fact]
    public void Or_ReturnsTrueWhenEitherTrue()
    {
        var a = Series.FromValues("a", new[] { true, true, false, false });
        var b = Series.FromValues("b", new[] { true, false, true, false });

        var result = a.Or(b);

        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeTrue();
        result[2].AsBoolean().Should().BeTrue();
        result[3].AsBoolean().Should().BeFalse();
    }

    [Fact]
    public void Xor_ReturnsTrueWhenExactlyOneTrue()
    {
        var a = Series.FromValues("a", new[] { true, true, false, false });
        var b = Series.FromValues("b", new[] { true, false, true, false });

        var result = a.Xor(b);

        result[0].AsBoolean().Should().BeFalse();
        result[1].AsBoolean().Should().BeTrue();
        result[2].AsBoolean().Should().BeTrue();
        result[3].AsBoolean().Should().BeFalse();
    }

    [Fact]
    public void Not_InvertsValues()
    {
        var series = Series.FromValues("a", new[] { true, false, true });

        var result = series.Not();

        result[0].AsBoolean().Should().BeFalse();
        result[1].AsBoolean().Should().BeTrue();
        result[2].AsBoolean().Should().BeFalse();
    }

    [Fact]
    public void AndOperator_WorksCorrectly()
    {
        var a = Series.FromValues("a", new[] { true, false });
        var b = Series.FromValues("b", new[] { true, true });

        var result = a & b;

        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeFalse();
    }

    [Fact]
    public void OrOperator_WorksCorrectly()
    {
        var a = Series.FromValues("a", new[] { true, false });
        var b = Series.FromValues("b", new[] { false, false });

        var result = a | b;

        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeFalse();
    }

    [Fact]
    public void XorOperator_WorksCorrectly()
    {
        var a = Series.FromValues("a", new[] { true, true });
        var b = Series.FromValues("b", new[] { true, false });

        var result = a ^ b;

        result[0].AsBoolean().Should().BeFalse();
        result[1].AsBoolean().Should().BeTrue();
    }

    [Fact]
    public void NotOperator_WorksCorrectly()
    {
        var series = Series.FromValues("a", new[] { true, false });

        var result = !series;

        result[0].AsBoolean().Should().BeFalse();
        result[1].AsBoolean().Should().BeTrue();
    }

    // ============================================================================
    // Chained Boolean Operations
    // ============================================================================

    [Fact]
    public void ChainedAnd_WorksCorrectly()
    {
        var a = Series.FromValues("a", new[] { true, true, false });
        var b = Series.FromValues("b", new[] { true, true, true });
        var c = Series.FromValues("c", new[] { true, false, true });

        var result = a.And(b).And(c);

        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeFalse();
        result[2].AsBoolean().Should().BeFalse();
    }

    [Fact]
    public void ChainedOr_WorksCorrectly()
    {
        var a = Series.FromValues("a", new[] { false, false, false });
        var b = Series.FromValues("b", new[] { false, true, false });
        var c = Series.FromValues("c", new[] { false, false, true });

        var result = a.Or(b).Or(c);

        result[0].AsBoolean().Should().BeFalse();
        result[1].AsBoolean().Should().BeTrue();
        result[2].AsBoolean().Should().BeTrue();
    }

    // ============================================================================
    // Boolean Operations with Comparison Results
    // ============================================================================

    [Fact]
    public void CombineComparisonWithAnd()
    {
        var x = Series.FromValues("x", new[] { 1, 5, 10, 15, 20 });

        var gtFive = x.Gt(AnyValue.From(5));
        var ltFifteen = x.Lt(AnyValue.From(15));
        var result = gtFive.And(ltFifteen);

        result[0].AsBoolean().Should().BeFalse(); // 1 not > 5
        result[1].AsBoolean().Should().BeFalse(); // 5 not > 5
        result[2].AsBoolean().Should().BeTrue();  // 10 > 5 and < 15
        result[3].AsBoolean().Should().BeFalse(); // 15 not < 15
        result[4].AsBoolean().Should().BeFalse(); // 20 not < 15
    }

    [Fact]
    public void CombineComparisonWithOr()
    {
        var x = Series.FromValues("x", new[] { 1, 5, 10, 15, 20 });

        var ltFive = x.Lt(AnyValue.From(5));
        var gtFifteen = x.Gt(AnyValue.From(15));
        var result = ltFive.Or(gtFifteen);

        result[0].AsBoolean().Should().BeTrue();  // 1 < 5
        result[1].AsBoolean().Should().BeFalse(); // 5 not < 5, not > 15
        result[2].AsBoolean().Should().BeFalse(); // 10 not < 5, not > 15
        result[3].AsBoolean().Should().BeFalse(); // 15 not > 15
        result[4].AsBoolean().Should().BeTrue();  // 20 > 15
    }

    [Fact]
    public void NegateComparison()
    {
        var x = Series.FromValues("x", new[] { 1, 2, 3 });

        var eq2 = x.Eq(AnyValue.From(2));
        var notEq2 = eq2.Not();

        notEq2[0].AsBoolean().Should().BeTrue();
        notEq2[1].AsBoolean().Should().BeFalse();
        notEq2[2].AsBoolean().Should().BeTrue();
    }

    // ============================================================================
    // Edge Cases
    // ============================================================================

    [Fact]
    public void Comparison_WithNulls_HandlesCorrectly()
    {
        var a = Series.FromNullable("a", new int?[] { 1, null, 3 });
        var b = Series.FromNullable("b", new int?[] { 1, 2, null });

        var result = a.Eq(b);

        result[0].AsBoolean().Should().BeTrue();
        // Null comparisons typically yield null or false
    }

    [Fact]
    public void Comparison_EmptySeries_ReturnsEmpty()
    {
        var a = Series.FromValues("a", Array.Empty<int>());
        var b = Series.FromValues("b", Array.Empty<int>());

        var result = a.Eq(b);

        result.Length.Should().Be(0);
    }

    [Fact]
    public void Boolean_SingleElement_WorksCorrectly()
    {
        var a = Series.FromValues("a", new[] { true });
        var b = Series.FromValues("b", new[] { false });

        a.And(b)[0].AsBoolean().Should().BeFalse();
        a.Or(b)[0].AsBoolean().Should().BeTrue();
        a.Xor(b)[0].AsBoolean().Should().BeTrue();
    }

    [Fact]
    public void Comparison_LargeSeries_WorksCorrectly()
    {
        var size = 10000;
        var a = Series.FromValues("a", Enumerable.Range(0, size).ToArray());
        var b = Series.FromValues("b", Enumerable.Range(0, size).Select(i => i % 2 == 0 ? i : i + 1).ToArray());

        var result = a.Eq(b);

        result.Length.Should().Be(size);
        // Even indices should be equal
        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeFalse();
        result[2].AsBoolean().Should().BeTrue();
    }

    // ============================================================================
    // All/Any Tests (using aggregations on boolean series)
    // ============================================================================

    [Fact]
    public void AllTrue_ReturnsTrue()
    {
        var series = Series.FromValues("a", new[] { true, true, true });

        var all = series.All();

        all.Should().BeTrue();
    }

    [Fact]
    public void AllTrue_WithOneFalse_ReturnsFalse()
    {
        var series = Series.FromValues("a", new[] { true, false, true });

        var all = series.All();

        all.Should().BeFalse();
    }

    [Fact]
    public void AnyTrue_WithSomeTrue_ReturnsTrue()
    {
        var series = Series.FromValues("a", new[] { false, true, false });

        var any = series.Any();

        any.Should().BeTrue();
    }

    [Fact]
    public void AnyTrue_WithAllFalse_ReturnsFalse()
    {
        var series = Series.FromValues("a", new[] { false, false, false });

        var any = series.Any();

        any.Should().BeFalse();
    }
}
