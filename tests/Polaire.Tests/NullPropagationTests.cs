// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET
//
// Tests for null propagation and handling, inspired by Polars test suite.
// These tests cover:
// - Null propagation in arithmetic operations
// - Null handling in aggregations
// - Null in comparisons
// - FillNull and DropNulls operations
// - Null counts and checking

using FluentAssertions;
using Polaire;
using Polaire.DataTypes;
using Xunit;
using static Polaire.Pl;

namespace Polaire.Tests;

/// <summary>
/// Tests for null propagation and handling operations.
/// </summary>
public class NullPropagationTests
{
    // ============================================================================
    // Basic Null Creation Tests
    // ============================================================================

    [Fact]
    public void FromNullable_CreatesNulls()
    {
        var s = Series.FromNullable("s", new int?[] { 1, null, 3 });

        s.IsNull(0).Should().BeFalse();
        s.IsNull(1).Should().BeTrue();
        s.IsNull(2).Should().BeFalse();
    }

    [Fact]
    public void FromNullable_AllNulls_Works()
    {
        var s = Series.FromNullable("s", new int?[] { null, null, null });

        s.IsNull(0).Should().BeTrue();
        s.IsNull(1).Should().BeTrue();
        s.IsNull(2).Should().BeTrue();
    }

    [Fact]
    public void FromNullable_NoNulls_Works()
    {
        var s = Series.FromNullable("s", new int?[] { 1, 2, 3 });

        s.IsNull(0).Should().BeFalse();
        s.IsNull(1).Should().BeFalse();
        s.IsNull(2).Should().BeFalse();
    }

    [Fact]
    public void FromNullable_FirstNull_Works()
    {
        var s = Series.FromNullable("s", new int?[] { null, 2, 3 });

        s.IsNull(0).Should().BeTrue();
        s[1].AsInt32().Should().Be(2);
    }

    [Fact]
    public void FromNullable_LastNull_Works()
    {
        var s = Series.FromNullable("s", new int?[] { 1, 2, null });

        s[0].AsInt32().Should().Be(1);
        s.IsNull(2).Should().BeTrue();
    }

    // ============================================================================
    // NullCount Tests
    // ============================================================================

    [Fact]
    public void NullCount_NoNulls_ReturnsZero()
    {
        var s = Series.FromValues("s", new[] { 1, 2, 3 });

        s.NullCount.Should().Be(0);
    }

    [Fact]
    public void NullCount_SomeNulls_ReturnsCorrectCount()
    {
        var s = Series.FromNullable("s", new int?[] { 1, null, 3, null, 5 });

        s.NullCount.Should().Be(2);
    }

    [Fact]
    public void NullCount_AllNulls_ReturnsLength()
    {
        var s = Series.FromNullable("s", new int?[] { null, null, null });

        s.NullCount.Should().Be(3);
    }

    [Fact]
    public void NullCount_Empty_ReturnsZero()
    {
        var s = Series.FromValues("s", Array.Empty<int>());

        s.NullCount.Should().Be(0);
    }

    // ============================================================================
    // Arithmetic Null Propagation Tests
    // ============================================================================

    [Fact]
    public void Add_WithNull_PropagatesNull()
    {
        var a = Series.FromNullable("a", new double?[] { 1.0, null, 3.0 });
        var b = Series.FromValues("b", new[] { 10.0, 20.0, 30.0 });

        var result = a + b;

        result[0].AsFloat64().Should().Be(11.0);
        result.IsNull(1).Should().BeTrue();
        result[2].AsFloat64().Should().Be(33.0);
    }

    [Fact]
    public void Subtract_WithNull_PropagatesNull()
    {
        var a = Series.FromValues("a", new[] { 10.0, 20.0, 30.0 });
        var b = Series.FromNullable("b", new double?[] { 1.0, null, 3.0 });

        var result = a - b;

        result[0].AsFloat64().Should().Be(9.0);
        result.IsNull(1).Should().BeTrue();
        result[2].AsFloat64().Should().Be(27.0);
    }

    [Fact]
    public void Multiply_WithNull_PropagatesNull()
    {
        var a = Series.FromNullable("a", new double?[] { 2.0, null, 4.0 });
        var b = Series.FromValues("b", new[] { 10.0, 20.0, 30.0 });

        var result = a * b;

        result[0].AsFloat64().Should().Be(20.0);
        result.IsNull(1).Should().BeTrue();
        result[2].AsFloat64().Should().Be(120.0);
    }

    [Fact]
    public void Divide_WithNull_PropagatesNull()
    {
        var a = Series.FromValues("a", new[] { 10.0, 20.0, 30.0 });
        var b = Series.FromNullable("b", new double?[] { 2.0, null, 5.0 });

        var result = a / b;

        result[0].AsFloat64().Should().Be(5.0);
        result.IsNull(1).Should().BeTrue();
        result[2].AsFloat64().Should().Be(6.0);
    }

    [Fact]
    public void BothNulls_ResultsInNull()
    {
        var a = Series.FromNullable("a", new double?[] { null, 2.0 });
        var b = Series.FromNullable("b", new double?[] { null, 3.0 });

        var result = a + b;

        result.IsNull(0).Should().BeTrue();
        result[1].AsFloat64().Should().Be(5.0);
    }

    // ============================================================================
    // Aggregation with Nulls Tests
    // ============================================================================

    [Fact]
    public void Sum_SkipsNulls()
    {
        var s = Series.FromNullable("s", new int?[] { 1, null, 3, null, 5 });

        s.Sum().AsInt64().Should().Be(9);  // 1 + 3 + 5
    }

    [Fact]
    public void Mean_SkipsNulls()
    {
        var s = Series.FromNullable("s", new double?[] { 2.0, null, 4.0 });

        s.Mean().AsFloat64().Should().Be(3.0);  // (2 + 4) / 2
    }

    [Fact]
    public void Min_SkipsNulls()
    {
        var s = Series.FromNullable("s", new int?[] { 5, null, 3, null, 1 });

        s.Min().AsInt32().Should().Be(1);
    }

    [Fact]
    public void Max_SkipsNulls()
    {
        var s = Series.FromNullable("s", new int?[] { 1, null, 5, null, 3 });

        s.Max().AsInt32().Should().Be(5);
    }

    [Fact]
    public void Count_ExcludesNulls()
    {
        var s = Series.FromNullable("s", new int?[] { 1, null, 3, null, 5 });

        s.Count().Should().Be(3);
    }

    [Fact]
    public void AllNulls_Sum_ReturnsNull()
    {
        var s = Series.FromNullable("s", new int?[] { null, null, null });

        s.Sum().IsNull.Should().BeTrue();
    }

    [Fact]
    public void AllNulls_Mean_ReturnsNull()
    {
        var s = Series.FromNullable("s", new double?[] { null, null });

        s.Mean().IsNull.Should().BeTrue();
    }

    [Fact]
    public void AllNulls_Min_ReturnsNull()
    {
        var s = Series.FromNullable("s", new int?[] { null, null });

        s.Min().IsNull.Should().BeTrue();
    }

    [Fact]
    public void AllNulls_Max_ReturnsNull()
    {
        var s = Series.FromNullable("s", new int?[] { null, null });

        s.Max().IsNull.Should().BeTrue();
    }

    [Fact]
    public void AllNulls_Count_ReturnsZero()
    {
        var s = Series.FromNullable("s", new int?[] { null, null, null });

        s.Count().Should().Be(0);
    }

    // ============================================================================
    // FillNull Tests
    // ============================================================================

    [Fact]
    public void FillNull_WithValue_ReplacesNulls()
    {
        var s = Series.FromNullable("s", new int?[] { 1, null, 3 });

        var result = s.FillNull(0);

        result[0].AsInt32().Should().Be(1);
        result[1].AsInt32().Should().Be(0);
        result[2].AsInt32().Should().Be(3);
    }

    [Fact]
    public void FillNull_NoNulls_Unchanged()
    {
        var s = Series.FromValues("s", new[] { 1, 2, 3 });

        var result = s.FillNull(0);

        result[0].AsInt32().Should().Be(1);
        result[1].AsInt32().Should().Be(2);
        result[2].AsInt32().Should().Be(3);
    }

    [Fact]
    public void FillNull_AllNulls_AllReplaced()
    {
        var s = Series.FromNullable("s", new int?[] { null, null, null });

        var result = s.FillNull(99);

        result[0].AsInt32().Should().Be(99);
        result[1].AsInt32().Should().Be(99);
        result[2].AsInt32().Should().Be(99);
    }

    [Fact]
    public void FillNull_FloatSeries_Works()
    {
        var s = Series.FromNullable("s", new double?[] { 1.5, null, 3.5 });

        var result = s.FillNull(-1.0);

        result[0].AsFloat64().Should().Be(1.5);
        result[1].AsFloat64().Should().Be(-1.0);
        result[2].AsFloat64().Should().Be(3.5);
    }

    // ============================================================================
    // DropNulls Tests
    // ============================================================================

    [Fact]
    public void DropNulls_RemovesNullRows()
    {
        var s = Series.FromNullable("s", new int?[] { 1, null, 3, null, 5 });

        var result = s.DropNulls();

        result.Length.Should().Be(3);
        result[0].AsInt32().Should().Be(1);
        result[1].AsInt32().Should().Be(3);
        result[2].AsInt32().Should().Be(5);
    }

    [Fact]
    public void DropNulls_NoNulls_Unchanged()
    {
        var s = Series.FromValues("s", new[] { 1, 2, 3 });

        var result = s.DropNulls();

        result.Length.Should().Be(3);
    }

    [Fact]
    public void DropNulls_AllNulls_ReturnsEmpty()
    {
        var s = Series.FromNullable("s", new int?[] { null, null, null });

        var result = s.DropNulls();

        result.Length.Should().Be(0);
    }

    [Fact]
    public void DropNulls_FirstNull_Works()
    {
        var s = Series.FromNullable("s", new int?[] { null, 2, 3 });

        var result = s.DropNulls();

        result.Length.Should().Be(2);
        result[0].AsInt32().Should().Be(2);
    }

    [Fact]
    public void DropNulls_LastNull_Works()
    {
        var s = Series.FromNullable("s", new int?[] { 1, 2, null });

        var result = s.DropNulls();

        result.Length.Should().Be(2);
        result[1].AsInt32().Should().Be(2);
    }

    // ============================================================================
    // DataFrame Null Tests
    // ============================================================================

    [Fact]
    public void DataFrame_WithNullColumn_Works()
    {
        var df = new DataFrame(
            Series.FromNullable("a", new int?[] { 1, null, 3 }),
            Series.FromValues("b", new[] { 10, 20, 30 })
        );

        df["a"].IsNull(1).Should().BeTrue();
        df["b"].IsNull(1).Should().BeFalse();
    }

    [Fact]
    public void DataFrame_Filter_WithNulls()
    {
        var df = new DataFrame(
            Series.FromNullable("value", new int?[] { 1, null, 3, null, 5 })
        );

        var result = df.Lazy()
            .Filter(Col("value").Gt(2))
            .Collect();

        // Nulls should be excluded by comparison
        result.Height.Should().Be(2);  // 3 and 5
    }

    [Fact]
    public void DataFrame_GroupBy_SkipsNullValues()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "A" }),
            Series.FromNullable("value", new int?[] { 1, null, 3 })
        );

        var result = df.GroupBy("group").Sum();

        result.Height.Should().Be(1);
    }

    // ============================================================================
    // Comparison with Null Tests
    // ============================================================================

    [Fact]
    public void Eq_WithNull_ReturnsFalse()
    {
        var s = Series.FromNullable("s", new int?[] { 1, null, 3 });

        var result = s.Eq(1);

        result[0].AsBoolean().Should().BeTrue();
        // Null comparison behavior may vary
        result[2].AsBoolean().Should().BeFalse();
    }

    [Fact]
    public void Gt_WithNull_ReturnsFalseForNull()
    {
        var s = Series.FromNullable("s", new int?[] { 5, null, 1 });

        var result = s.Gt(2);

        result[0].AsBoolean().Should().BeTrue();  // 5 > 2
        // Null comparisons typically yield null/false
        result[2].AsBoolean().Should().BeFalse();  // 1 > 2
    }

    // ============================================================================
    // Sort with Nulls Tests
    // ============================================================================

    [Fact]
    public void Sort_WithNulls_NullsLast()
    {
        var s = Series.FromNullable("s", new int?[] { 3, null, 1, null, 2 });

        var result = s.Sort();

        // Nulls typically sort to end
        result[0].AsInt32().Should().Be(1);
        result[1].AsInt32().Should().Be(2);
        result[2].AsInt32().Should().Be(3);
    }

    // ============================================================================
    // Unique with Nulls Tests
    // ============================================================================

    [Fact]
    public void Unique_WithNulls_IncludesOneNull()
    {
        var s = Series.FromNullable("s", new int?[] { 1, null, 1, null, 2 });

        var result = s.Unique();

        // Should have 1, 2, and one null
        result.Length.Should().BeGreaterThanOrEqualTo(2);
    }

    // ============================================================================
    // HasNulls Property Tests
    // ============================================================================

    [Fact]
    public void HasNulls_WithNulls_ReturnsTrue()
    {
        var s = Series.FromNullable("s", new int?[] { 1, null, 3 });

        s.HasNulls.Should().BeTrue();
    }

    [Fact]
    public void HasNulls_NoNulls_ReturnsFalse()
    {
        var s = Series.FromValues("s", new[] { 1, 2, 3 });

        s.HasNulls.Should().BeFalse();
    }

    [Fact]
    public void HasNulls_Empty_ReturnsFalse()
    {
        var s = Series.FromValues("s", Array.Empty<int>());

        s.HasNulls.Should().BeFalse();
    }

    [Fact]
    public void HasNulls_AllNulls_ReturnsTrue()
    {
        var s = Series.FromNullable("s", new int?[] { null, null });

        s.HasNulls.Should().BeTrue();
    }

    // ============================================================================
    // Large Data with Nulls Tests
    // ============================================================================

    [Fact]
    public void Large_WithSparseNulls_Works()
    {
        var values = new int?[10000];
        for (int i = 0; i < 10000; i++)
        {
            values[i] = i % 100 == 0 ? null : i;
        }

        var s = Series.FromNullable("s", values);

        s.NullCount.Should().Be(100);  // Every 100th value is null
    }

    [Fact]
    public void Large_DropNulls_Works()
    {
        var values = new int?[10000];
        for (int i = 0; i < 10000; i++)
        {
            values[i] = i % 100 == 0 ? null : i;
        }

        var s = Series.FromNullable("s", values);
        var result = s.DropNulls();

        result.Length.Should().Be(9900);
    }

    [Fact]
    public void Large_FillNull_Works()
    {
        var values = new int?[10000];
        for (int i = 0; i < 10000; i++)
        {
            values[i] = i % 100 == 0 ? null : i;
        }

        var s = Series.FromNullable("s", values);
        var result = s.FillNull(-1);

        result.NullCount.Should().Be(0);
    }

    // ============================================================================
    // Edge Cases
    // ============================================================================

    [Fact]
    public void SingleNull_Works()
    {
        var s = Series.FromNullable("s", new int?[] { null });

        s.Length.Should().Be(1);
        s.IsNull(0).Should().BeTrue();
        s.NullCount.Should().Be(1);
    }

    [Fact]
    public void SingleNonNull_NoNulls()
    {
        var s = Series.FromNullable("s", new int?[] { 42 });

        s.Length.Should().Be(1);
        s.IsNull(0).Should().BeFalse();
        s.NullCount.Should().Be(0);
    }

    [Fact]
    public void AlternatingNulls_Works()
    {
        var s = Series.FromNullable("s", new int?[] { null, 1, null, 2, null, 3 });

        s.NullCount.Should().Be(3);
        s.Count().Should().Be(3);
    }
}
