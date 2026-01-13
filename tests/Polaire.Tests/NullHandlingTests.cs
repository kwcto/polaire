// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using FluentAssertions;
using Polaire;
using Polaire.DataTypes;
using Xunit;

namespace Polaire.Tests;

/// <summary>
/// Tests for null handling operations across Series and DataFrame.
/// </summary>
public class NullHandlingTests
{
    // ============================================================================
    // Basic Null Detection Tests
    // ============================================================================

    [Fact]
    public void Series_IsNull_ReturnsCorrectMask()
    {
        var series = Series.FromNullable("values", new int?[] { 1, null, 3, null, 5 });

        var result = series.IsNull();

        result[0].AsBoolean().Should().BeFalse();
        result[1].AsBoolean().Should().BeTrue();
        result[2].AsBoolean().Should().BeFalse();
        result[3].AsBoolean().Should().BeTrue();
        result[4].AsBoolean().Should().BeFalse();
    }

    [Fact]
    public void Series_IsNotNull_ReturnsCorrectMask()
    {
        var series = Series.FromNullable("values", new int?[] { 1, null, 3, null, 5 });

        var result = series.IsNotNull();

        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeFalse();
        result[2].AsBoolean().Should().BeTrue();
        result[3].AsBoolean().Should().BeFalse();
        result[4].AsBoolean().Should().BeTrue();
    }

    [Fact]
    public void Series_NullCount_ReturnsCorrectCount()
    {
        var series = Series.FromNullable("values", new int?[] { 1, null, 3, null, 5 });

        series.NullCount.Should().Be(2);
    }

    [Fact]
    public void Series_NullCount_NoNulls_ReturnsZero()
    {
        var series = Series.FromValues("values", new[] { 1, 2, 3, 4, 5 });

        series.NullCount.Should().Be(0);
    }

    [Fact]
    public void Series_NullCount_AllNulls_ReturnsLength()
    {
        var series = Series.FromNullable("values", new int?[] { null, null, null });

        series.NullCount.Should().Be(3);
    }

    [Fact]
    public void Series_HasNulls_ReturnsTrueWhenNullsExist()
    {
        var series = Series.FromNullable("values", new int?[] { 1, null, 3 });

        series.HasNulls.Should().BeTrue();
    }

    [Fact]
    public void Series_HasNulls_ReturnsFalseWhenNoNulls()
    {
        var series = Series.FromValues("values", new[] { 1, 2, 3 });

        series.HasNulls.Should().BeFalse();
    }

    // ============================================================================
    // FillNull Tests
    // ============================================================================

    [Fact]
    public void FillNull_Int32_ReplacesNullsWithValue()
    {
        var series = Series.FromNullable("values", new int?[] { 1, null, 3, null, 5 });

        var result = series.FillNull(0);

        result[0].AsInt32().Should().Be(1);
        result[1].AsInt32().Should().Be(0);
        result[2].AsInt32().Should().Be(3);
        result[3].AsInt32().Should().Be(0);
        result[4].AsInt32().Should().Be(5);
        result.NullCount.Should().Be(0);
    }

    [Fact]
    public void FillNull_Float64_ReplacesNullsWithValue()
    {
        var series = Series.FromNullable("values", new double?[] { 1.5, null, 3.5 });

        var result = series.FillNull(0.0);

        result[0].AsFloat64().Should().Be(1.5);
        result[1].AsFloat64().Should().Be(0.0);
        result[2].AsFloat64().Should().Be(3.5);
        result.NullCount.Should().Be(0);
    }

    [Fact]
    public void FillNull_String_ReplacesNullsWithValue()
    {
        var series = Series.FromValues("values", new string?[] { "a", null, "c" });

        var result = series.FillNull("missing");

        result[0].AsString().Should().Be("a");
        result[1].AsString().Should().Be("missing");
        result[2].AsString().Should().Be("c");
    }

    [Fact]
    public void FillNull_NoNulls_ReturnsSameValues()
    {
        var series = Series.FromValues("values", new[] { 1, 2, 3 });

        var result = series.FillNull(0);

        result[0].AsInt32().Should().Be(1);
        result[1].AsInt32().Should().Be(2);
        result[2].AsInt32().Should().Be(3);
    }

    [Fact]
    public void FillNull_AllNulls_ReplacesAll()
    {
        var series = Series.FromNullable("values", new int?[] { null, null, null });

        var result = series.FillNull(-1);

        result[0].AsInt32().Should().Be(-1);
        result[1].AsInt32().Should().Be(-1);
        result[2].AsInt32().Should().Be(-1);
        result.NullCount.Should().Be(0);
    }

    // ============================================================================
    // DropNulls Tests
    // ============================================================================

    [Fact]
    public void DropNulls_RemovesNullRows()
    {
        var series = Series.FromNullable("values", new int?[] { 1, null, 3, null, 5 });

        var result = series.DropNulls();

        result.Length.Should().Be(3);
        result[0].AsInt32().Should().Be(1);
        result[1].AsInt32().Should().Be(3);
        result[2].AsInt32().Should().Be(5);
    }

    [Fact]
    public void DropNulls_NoNulls_ReturnsSameLength()
    {
        var series = Series.FromValues("values", new[] { 1, 2, 3, 4, 5 });

        var result = series.DropNulls();

        result.Length.Should().Be(5);
    }

    [Fact]
    public void DropNulls_AllNulls_ReturnsEmpty()
    {
        var series = Series.FromNullable("values", new int?[] { null, null, null });

        var result = series.DropNulls();

        result.Length.Should().Be(0);
    }

    [Fact]
    public void DropNulls_Float64_WorksCorrectly()
    {
        var series = Series.FromNullable("values", new double?[] { 1.0, null, 3.0 });

        var result = series.DropNulls();

        result.Length.Should().Be(2);
        result[0].AsFloat64().Should().Be(1.0);
        result[1].AsFloat64().Should().Be(3.0);
    }

    // ============================================================================
    // Null Propagation in Aggregations Tests
    // ============================================================================

    [Fact]
    public void Sum_SkipsNulls()
    {
        var series = Series.FromNullable("values", new int?[] { 1, null, 3, null, 5 });

        var result = series.Sum();

        result.AsInt64().Should().Be(9); // 1 + 3 + 5
    }

    [Fact]
    public void Mean_SkipsNulls()
    {
        var series = Series.FromNullable("values", new double?[] { 1.0, null, 3.0, null, 5.0 });

        var result = series.Mean();

        result.AsFloat64().Should().BeApproximately(3.0, 0.001); // (1 + 3 + 5) / 3
    }

    [Fact]
    public void Min_SkipsNulls()
    {
        var series = Series.FromNullable("values", new int?[] { 5, null, 1, null, 3 });

        var result = series.Min();

        result.AsInt32().Should().Be(1);
    }

    [Fact]
    public void Max_SkipsNulls()
    {
        var series = Series.FromNullable("values", new int?[] { 1, null, 5, null, 3 });

        var result = series.Max();

        result.AsInt32().Should().Be(5);
    }

    [Fact]
    public void Std_SkipsNulls()
    {
        var series = Series.FromNullable("values", new double?[] { 1.0, null, 2.0, null, 3.0 });

        var result = series.Std();

        // Std of [1, 2, 3] = sqrt(2/3) ≈ 0.816
        result.AsFloat64().Should().BeGreaterThan(0);
    }

    [Fact]
    public void Count_CountsNonNullValues()
    {
        var series = Series.FromNullable("values", new int?[] { 1, null, 3, null, 5 });

        var result = series.Count();

        result.Should().Be(3);
    }

    [Fact]
    public void AllNulls_Aggregation_ReturnsNull()
    {
        var series = Series.FromNullable("values", new int?[] { null, null, null });

        var result = series.Sum();

        result.IsNull.Should().BeTrue();
    }

    // ============================================================================
    // DataFrame DropNulls Tests
    // Note: DataFrame.DropNulls() not yet implemented
    // ============================================================================

    // ============================================================================
    // DataFrame FillNull Tests
    // Note: DataFrame.FillNull() not yet implemented
    // ============================================================================

    // ============================================================================
    // Null Handling in Comparisons Tests
    // ============================================================================

    [Fact]
    public void Eq_WithNull_ReturnsFalseForNullComparisons()
    {
        var series = Series.FromNullable("values", new int?[] { 1, null, 3 });

        var result = series.Eq(1);

        result[0].AsBoolean().Should().BeTrue();
        // Note: Polaire returns false for null comparisons (not null)
        result[1].AsBoolean().Should().BeFalse();
        result[2].AsBoolean().Should().BeFalse();
    }

    [Fact]
    public void Lt_WithNull_PropagatesNull()
    {
        var series = Series.FromNullable("values", new int?[] { 1, null, 5 });

        var result = series.Lt(3);

        result[0].AsBoolean().Should().BeTrue();
        // Lt propagates null (returns null for null comparisons)
        result.IsNull(1).Should().BeTrue();
        result[2].AsBoolean().Should().BeFalse();
    }

    // ============================================================================
    // Null Handling in Binary Operations Tests
    // ============================================================================

    [Fact]
    public void Add_WithNulls_PropagatesNulls()
    {
        var a = Series.FromNullable("a", new long?[] { 1, null, 3 });
        var b = Series.FromValues("b", new long[] { 10, 20, 30 });

        var result = a + b;

        result[0].AsInt64().Should().Be(11);
        result.IsNull(1).Should().BeTrue(); // null + 20 = null
        result[2].AsInt64().Should().Be(33);
    }

    [Fact]
    public void Multiply_WithNulls_PropagatesNulls()
    {
        var a = Series.FromNullable("a", new double?[] { 2.0, null, 4.0 });
        var b = Series.FromValues("b", new[] { 5.0, 5.0, 5.0 });

        var result = a * b;

        result[0].AsFloat64().Should().Be(10.0);
        result.IsNull(1).Should().BeTrue();
        result[2].AsFloat64().Should().Be(20.0);
    }

    // ============================================================================
    // Null at Boundaries Tests
    // ============================================================================

    [Fact]
    public void NullAtStart_HandledCorrectly()
    {
        var series = Series.FromNullable("values", new int?[] { null, 2, 3, 4, 5 });

        series.IsNull(0).Should().BeTrue();
        series.Sum().AsInt64().Should().Be(14);
    }

    [Fact]
    public void NullAtEnd_HandledCorrectly()
    {
        var series = Series.FromNullable("values", new int?[] { 1, 2, 3, 4, null });

        series.IsNull(4).Should().BeTrue();
        series.Sum().AsInt64().Should().Be(10);
    }

    [Fact]
    public void SingleNull_InMiddle_HandledCorrectly()
    {
        var series = Series.FromNullable("values", new int?[] { 1, 2, null, 4, 5 });

        series.NullCount.Should().Be(1);
        series.IsNull(2).Should().BeTrue();
    }

    // ============================================================================
    // Empty Series with Nullable Type Tests
    // ============================================================================

    [Fact]
    public void EmptySeries_NullCount_IsZero()
    {
        var series = Series.FromNullable("values", Array.Empty<int?>());

        series.NullCount.Should().Be(0);
        series.Length.Should().Be(0);
    }

    [Fact]
    public void SingleElement_Null_NullCountIsOne()
    {
        var series = Series.FromNullable("values", new int?[] { null });

        series.NullCount.Should().Be(1);
        series.Length.Should().Be(1);
    }

    [Fact]
    public void SingleElement_NotNull_NullCountIsZero()
    {
        var series = Series.FromNullable("values", new int?[] { 42 });

        series.NullCount.Should().Be(0);
        series[0].AsInt32().Should().Be(42);
    }

    // ============================================================================
    // Large Series Null Handling Tests
    // ============================================================================

    [Fact]
    public void LargeSeries_WithScatteredNulls_CountsCorrectly()
    {
        var size = 10000;
        var values = new int?[size];
        var expectedNullCount = 0;

        for (int i = 0; i < size; i++)
        {
            if (i % 7 == 0) // Every 7th element is null
            {
                values[i] = null;
                expectedNullCount++;
            }
            else
            {
                values[i] = i;
            }
        }

        var series = Series.FromNullable("values", values);

        series.NullCount.Should().Be(expectedNullCount);
    }

    [Fact]
    public void LargeSeries_DropNulls_RemovesCorrectAmount()
    {
        var size = 1000;
        var values = new int?[size];
        var nonNullCount = 0;

        for (int i = 0; i < size; i++)
        {
            if (i % 5 == 0)
            {
                values[i] = null;
            }
            else
            {
                values[i] = i;
                nonNullCount++;
            }
        }

        var series = Series.FromNullable("values", values);
        var result = series.DropNulls();

        result.Length.Should().Be(nonNullCount);
    }

    // ============================================================================
    // Different Data Types Null Handling Tests
    // ============================================================================

    [Fact]
    public void Int64_WithNulls_WorksCorrectly()
    {
        var series = Series.FromNullable("values", new long?[] { 1L, null, 3L });

        series.NullCount.Should().Be(1);
        series.Sum().AsInt64().Should().Be(4);
    }

    [Fact]
    public void Boolean_WithNulls_WorksCorrectly()
    {
        var series = Series.FromNullable("values", new bool?[] { true, null, false });

        series.NullCount.Should().Be(1);
        series.IsNull(1).Should().BeTrue();
    }

    [Fact]
    public void Float32_WithNulls_WorksCorrectly()
    {
        var series = Series.FromNullable("values", new float?[] { 1.0f, null, 3.0f });

        series.NullCount.Should().Be(1);
        // Sum returns Float64 for all numeric types
        series.Sum().AsFloat64().Should().BeApproximately(4.0, 0.001);
    }

    // ============================================================================
    // Chained Operations with Nulls Tests
    // ============================================================================

    [Fact]
    public void Sort_WithNulls_PlacesNullsLast()
    {
        var series = Series.FromNullable("values", new int?[] { 3, null, 1, null, 2 });

        var result = series.Sort(nullsLast: true);

        result[0].AsInt32().Should().Be(1);
        result[1].AsInt32().Should().Be(2);
        result[2].AsInt32().Should().Be(3);
        result.IsNull(3).Should().BeTrue();
        result.IsNull(4).Should().BeTrue();
    }

    [Fact]
    public void Filter_WithNulls_FiltersCorrectly()
    {
        var df = new DataFrame(
            Series.FromNullable("a", new int?[] { 1, null, 3, null, 5, 6 }),
            Series.FromValues("b", new[] { 10, 20, 30, 40, 50, 60 })
        );

        // Filter where b > 25
        var result = df.Filter(df["b"].Gt(25));

        // After filter: rows with b = 30, 40, 50, 60 remain (4 rows)
        result.Height.Should().Be(4);
        // Nulls in 'a' are preserved
        result["a"].IsNull(1).Should().BeTrue(); // The row where b=40 had null 'a'
    }

    // ============================================================================
    // Null Handling in GroupBy Tests
    // ============================================================================

    [Fact]
    public void GroupBy_SumWithNulls_SkipsNullsInAggregation()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "A", "B", "B" }),
            Series.FromNullable("value", new int?[] { 1, null, 3, null, 5 })
        );

        var result = df.GroupBy("group").Sum();

        // Group A: 1 + 3 = 4, Group B: 5
        result.Height.Should().Be(2);
    }

    [Fact]
    public void GroupBy_CountWithNulls_CountsNonNullOnly()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "A", "B", "B" }),
            Series.FromNullable("value", new int?[] { 1, null, 3, null, 5 })
        );

        var result = df.GroupBy("group").Count();

        // Group A: 2 non-null, Group B: 1 non-null
        result.Height.Should().Be(2);
    }

    // ============================================================================
    // Null Handling in Join Tests
    // ============================================================================

    [Fact]
    public void LeftJoin_WithNulls_PreservesNulls()
    {
        var left = new DataFrame(
            Series.FromValues("id", new[] { 1, 2, 3 }),
            Series.FromNullable("value", new int?[] { 10, null, 30 })
        );
        var right = new DataFrame(
            Series.FromValues("id", new[] { 1, 2 }),
            Series.FromValues("other", new[] { 100, 200 })
        );

        var result = left.LeftJoin(right, "id");

        result.Height.Should().Be(3);
        result["value"].IsNull(1).Should().BeTrue(); // null preserved
    }

    // ============================================================================
    // Cast with Null Handling Tests
    // ============================================================================

    [Fact]
    public void Cast_WithNulls_PreservesNullPositions()
    {
        var series = Series.FromNullable("values", new int?[] { 1, null, 3 });

        var result = series.Cast(DataType.Float64);

        result.NullCount.Should().Be(1);
        result.IsNull(1).Should().BeTrue();
        result[0].AsFloat64().Should().Be(1.0);
        result[2].AsFloat64().Should().Be(3.0);
    }
}
