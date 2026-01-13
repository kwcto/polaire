// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET
//
// Comprehensive tests for Series boolean operations, inspired by Polars test suite.
// These tests cover:
// - Boolean operations (And, Or, Xor, Not)
// - All/Any aggregations
// - Boolean series construction and manipulation

using FluentAssertions;
using Polaire;
using Polaire.DataTypes;
using Xunit;

namespace Polaire.Tests;

/// <summary>
/// Tests for Series boolean operations.
/// </summary>
public class SeriesBooleanOperationsTests
{
    // ============================================================================
    // And Tests
    // ============================================================================

    [Fact]
    public void And_TwoSeries_ReturnsElementwiseAnd()
    {
        var a = Series.FromValues("a", new[] { true, true, false, false });
        var b = Series.FromValues("b", new[] { true, false, true, false });
        var result = a.And(b);

        result[0].AsBoolean().Should().BeTrue();   // T && T
        result[1].AsBoolean().Should().BeFalse();  // T && F
        result[2].AsBoolean().Should().BeFalse();  // F && T
        result[3].AsBoolean().Should().BeFalse();  // F && F
    }

    [Fact]
    public void And_AllTrue_ReturnsAllTrue()
    {
        var a = Series.FromValues("a", new[] { true, true, true });
        var b = Series.FromValues("b", new[] { true, true, true });
        var result = a.And(b);

        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeTrue();
        result[2].AsBoolean().Should().BeTrue();
    }

    [Fact]
    public void And_AllFalse_ReturnsAllFalse()
    {
        var a = Series.FromValues("a", new[] { false, false, false });
        var b = Series.FromValues("b", new[] { true, true, true });
        var result = a.And(b);

        result[0].AsBoolean().Should().BeFalse();
        result[1].AsBoolean().Should().BeFalse();
        result[2].AsBoolean().Should().BeFalse();
    }

    [Fact]
    public void And_WithNulls_PropagatesNulls()
    {
        var a = Series.FromNullable("a", new bool?[] { true, true, null, false });
        var b = Series.FromValues("b", new[] { true, false, true, true });
        var result = a.And(b);

        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeFalse();
        result.IsNull(2).Should().BeTrue();  // null && true = null
        result[3].AsBoolean().Should().BeFalse();
    }

    // ============================================================================
    // Or Tests
    // ============================================================================

    [Fact]
    public void Or_TwoSeries_ReturnsElementwiseOr()
    {
        var a = Series.FromValues("a", new[] { true, true, false, false });
        var b = Series.FromValues("b", new[] { true, false, true, false });
        var result = a.Or(b);

        result[0].AsBoolean().Should().BeTrue();   // T || T
        result[1].AsBoolean().Should().BeTrue();   // T || F
        result[2].AsBoolean().Should().BeTrue();   // F || T
        result[3].AsBoolean().Should().BeFalse();  // F || F
    }

    [Fact]
    public void Or_AllFalse_ReturnsAllFalse()
    {
        var a = Series.FromValues("a", new[] { false, false, false });
        var b = Series.FromValues("b", new[] { false, false, false });
        var result = a.Or(b);

        result[0].AsBoolean().Should().BeFalse();
        result[1].AsBoolean().Should().BeFalse();
        result[2].AsBoolean().Should().BeFalse();
    }

    [Fact]
    public void Or_AtLeastOneTrue_ReturnsTrue()
    {
        var a = Series.FromValues("a", new[] { true, false, false });
        var b = Series.FromValues("b", new[] { false, true, false });
        var result = a.Or(b);

        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeTrue();
        result[2].AsBoolean().Should().BeFalse();
    }

    [Fact]
    public void Or_WithNulls_PropagatesNulls()
    {
        var a = Series.FromNullable("a", new bool?[] { true, false, null, null });
        var b = Series.FromValues("b", new[] { false, false, true, false });
        var result = a.Or(b);

        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeFalse();
        result.IsNull(2).Should().BeTrue();  // null || true = true (could also be null depending on impl)
        result.IsNull(3).Should().BeTrue();  // null || false = null
    }

    // ============================================================================
    // Xor Tests
    // ============================================================================

    [Fact]
    public void Xor_TwoSeries_ReturnsElementwiseXor()
    {
        var a = Series.FromValues("a", new[] { true, true, false, false });
        var b = Series.FromValues("b", new[] { true, false, true, false });
        var result = a.Xor(b);

        result[0].AsBoolean().Should().BeFalse();  // T ^ T = F
        result[1].AsBoolean().Should().BeTrue();   // T ^ F = T
        result[2].AsBoolean().Should().BeTrue();   // F ^ T = T
        result[3].AsBoolean().Should().BeFalse();  // F ^ F = F
    }

    [Fact]
    public void Xor_SameSeries_ReturnsAllFalse()
    {
        var a = Series.FromValues("a", new[] { true, false, true, false });
        var result = a.Xor(a);

        result[0].AsBoolean().Should().BeFalse();
        result[1].AsBoolean().Should().BeFalse();
        result[2].AsBoolean().Should().BeFalse();
        result[3].AsBoolean().Should().BeFalse();
    }

    [Fact]
    public void Xor_Opposite_ReturnsAllTrue()
    {
        var a = Series.FromValues("a", new[] { true, true, false, false });
        var b = Series.FromValues("b", new[] { false, false, true, true });
        var result = a.Xor(b);

        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeTrue();
        result[2].AsBoolean().Should().BeTrue();
        result[3].AsBoolean().Should().BeTrue();
    }

    // ============================================================================
    // Not Tests
    // ============================================================================

    [Fact]
    public void Not_ReturnsNegation()
    {
        var series = Series.FromValues("s", new[] { true, false, true, false });
        var result = series.Not();

        result[0].AsBoolean().Should().BeFalse();
        result[1].AsBoolean().Should().BeTrue();
        result[2].AsBoolean().Should().BeFalse();
        result[3].AsBoolean().Should().BeTrue();
    }

    [Fact]
    public void Not_AllTrue_ReturnsAllFalse()
    {
        var series = Series.FromValues("s", new[] { true, true, true });
        var result = series.Not();

        result[0].AsBoolean().Should().BeFalse();
        result[1].AsBoolean().Should().BeFalse();
        result[2].AsBoolean().Should().BeFalse();
    }

    [Fact]
    public void Not_AllFalse_ReturnsAllTrue()
    {
        var series = Series.FromValues("s", new[] { false, false, false });
        var result = series.Not();

        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeTrue();
        result[2].AsBoolean().Should().BeTrue();
    }

    [Fact]
    public void Not_WithNulls_PreservesNulls()
    {
        var series = Series.FromNullable("s", new bool?[] { true, null, false });
        var result = series.Not();

        result[0].AsBoolean().Should().BeFalse();
        result.IsNull(1).Should().BeTrue();
        result[2].AsBoolean().Should().BeTrue();
    }

    [Fact]
    public void Not_DoubleNot_ReturnsOriginal()
    {
        var series = Series.FromValues("s", new[] { true, false, true });
        var result = series.Not().Not();

        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeFalse();
        result[2].AsBoolean().Should().BeTrue();
    }

    // ============================================================================
    // All Tests
    // ============================================================================

    [Fact]
    public void All_AllTrue_ReturnsTrue()
    {
        var series = Series.FromValues("s", new[] { true, true, true });

        series.All().Should().BeTrue();
    }

    [Fact]
    public void All_OneIsFalse_ReturnsFalse()
    {
        var series = Series.FromValues("s", new[] { true, false, true });

        series.All().Should().BeFalse();
    }

    [Fact]
    public void All_AllFalse_ReturnsFalse()
    {
        var series = Series.FromValues("s", new[] { false, false, false });

        series.All().Should().BeFalse();
    }

    [Fact]
    public void All_EmptySeries_ReturnsTrue()
    {
        var series = Series.FromValues("s", Array.Empty<bool>());

        // Empty series: vacuously true
        series.All().Should().BeTrue();
    }

    [Fact]
    public void All_SingleTrue_ReturnsTrue()
    {
        var series = Series.FromValues("s", new[] { true });

        series.All().Should().BeTrue();
    }

    [Fact]
    public void All_SingleFalse_ReturnsFalse()
    {
        var series = Series.FromValues("s", new[] { false });

        series.All().Should().BeFalse();
    }

    // ============================================================================
    // Any Tests
    // ============================================================================

    [Fact]
    public void Any_AllTrue_ReturnsTrue()
    {
        var series = Series.FromValues("s", new[] { true, true, true });

        series.Any().Should().BeTrue();
    }

    [Fact]
    public void Any_OneIsTrue_ReturnsTrue()
    {
        var series = Series.FromValues("s", new[] { false, true, false });

        series.Any().Should().BeTrue();
    }

    [Fact]
    public void Any_AllFalse_ReturnsFalse()
    {
        var series = Series.FromValues("s", new[] { false, false, false });

        series.Any().Should().BeFalse();
    }

    [Fact]
    public void Any_EmptySeries_ReturnsFalse()
    {
        var series = Series.FromValues("s", Array.Empty<bool>());

        // Empty series: no elements are true
        series.Any().Should().BeFalse();
    }

    [Fact]
    public void Any_SingleTrue_ReturnsTrue()
    {
        var series = Series.FromValues("s", new[] { true });

        series.Any().Should().BeTrue();
    }

    [Fact]
    public void Any_SingleFalse_ReturnsFalse()
    {
        var series = Series.FromValues("s", new[] { false });

        series.Any().Should().BeFalse();
    }

    // ============================================================================
    // Combining Boolean Operations
    // ============================================================================

    [Fact]
    public void DeMorgan_NotAndEqualsOrNot()
    {
        // De Morgan's law: NOT(A AND B) = (NOT A) OR (NOT B)
        var a = Series.FromValues("a", new[] { true, true, false, false });
        var b = Series.FromValues("b", new[] { true, false, true, false });

        var leftSide = a.And(b).Not();
        var rightSide = a.Not().Or(b.Not());

        leftSide[0].AsBoolean().Should().Be(rightSide[0].AsBoolean());
        leftSide[1].AsBoolean().Should().Be(rightSide[1].AsBoolean());
        leftSide[2].AsBoolean().Should().Be(rightSide[2].AsBoolean());
        leftSide[3].AsBoolean().Should().Be(rightSide[3].AsBoolean());
    }

    [Fact]
    public void DeMorgan_NotOrEqualsAndNot()
    {
        // De Morgan's law: NOT(A OR B) = (NOT A) AND (NOT B)
        var a = Series.FromValues("a", new[] { true, true, false, false });
        var b = Series.FromValues("b", new[] { true, false, true, false });

        var leftSide = a.Or(b).Not();
        var rightSide = a.Not().And(b.Not());

        leftSide[0].AsBoolean().Should().Be(rightSide[0].AsBoolean());
        leftSide[1].AsBoolean().Should().Be(rightSide[1].AsBoolean());
        leftSide[2].AsBoolean().Should().Be(rightSide[2].AsBoolean());
        leftSide[3].AsBoolean().Should().Be(rightSide[3].AsBoolean());
    }

    [Fact]
    public void Xor_EquivalentToAndOrNot()
    {
        // XOR = (A AND NOT B) OR (NOT A AND B)
        var a = Series.FromValues("a", new[] { true, true, false, false });
        var b = Series.FromValues("b", new[] { true, false, true, false });

        var xorResult = a.Xor(b);
        var equivalent = a.And(b.Not()).Or(a.Not().And(b));

        xorResult[0].AsBoolean().Should().Be(equivalent[0].AsBoolean());
        xorResult[1].AsBoolean().Should().Be(equivalent[1].AsBoolean());
        xorResult[2].AsBoolean().Should().Be(equivalent[2].AsBoolean());
        xorResult[3].AsBoolean().Should().Be(equivalent[3].AsBoolean());
    }

    // ============================================================================
    // Boolean Series from Comparisons
    // ============================================================================

    [Fact]
    public void And_FromComparisons_CombinesCorrectly()
    {
        var values = Series.FromValues("v", new[] { 1, 3, 5, 7, 9 });

        // Values greater than 2 AND less than 8
        var gt2 = values.Gt(AnyValue.From(2));
        var lt8 = values.Lt(AnyValue.From(8));
        var result = gt2.And(lt8);

        result[0].AsBoolean().Should().BeFalse();  // 1: F && T = F
        result[1].AsBoolean().Should().BeTrue();   // 3: T && T = T
        result[2].AsBoolean().Should().BeTrue();   // 5: T && T = T
        result[3].AsBoolean().Should().BeTrue();   // 7: T && T = T
        result[4].AsBoolean().Should().BeFalse();  // 9: T && F = F
    }

    [Fact]
    public void Or_FromComparisons_CombinesCorrectly()
    {
        var values = Series.FromValues("v", new[] { 1, 3, 5, 7, 9 });

        // Values less than 3 OR greater than 7
        var lt3 = values.Lt(AnyValue.From(3));
        var gt7 = values.Gt(AnyValue.From(7));
        var result = lt3.Or(gt7);

        result[0].AsBoolean().Should().BeTrue();   // 1: T || F = T
        result[1].AsBoolean().Should().BeFalse();  // 3: F || F = F
        result[2].AsBoolean().Should().BeFalse();  // 5: F || F = F
        result[3].AsBoolean().Should().BeFalse();  // 7: F || F = F
        result[4].AsBoolean().Should().BeTrue();   // 9: F || T = T
    }

    // ============================================================================
    // Edge Cases
    // ============================================================================

    [Fact]
    public void And_EmptySeries_ReturnsEmpty()
    {
        var a = Series.FromValues("a", Array.Empty<bool>());
        var b = Series.FromValues("b", Array.Empty<bool>());
        var result = a.And(b);

        result.Length.Should().Be(0);
    }

    [Fact]
    public void Or_EmptySeries_ReturnsEmpty()
    {
        var a = Series.FromValues("a", Array.Empty<bool>());
        var b = Series.FromValues("b", Array.Empty<bool>());
        var result = a.Or(b);

        result.Length.Should().Be(0);
    }

    [Fact]
    public void Not_EmptySeries_ReturnsEmpty()
    {
        var series = Series.FromValues("s", Array.Empty<bool>());
        var result = series.Not();

        result.Length.Should().Be(0);
    }

    [Fact]
    public void And_LargeSeries_WorksCorrectly()
    {
        var size = 1000;
        var a = Series.FromValues("a", Enumerable.Repeat(true, size).ToArray());
        var b = Series.FromValues("b", Enumerable.Range(0, size).Select(i => i % 2 == 0).ToArray());
        var result = a.And(b);

        result.Length.Should().Be(size);
        result[0].AsBoolean().Should().BeTrue();   // true && true
        result[1].AsBoolean().Should().BeFalse();  // true && false
    }
}
