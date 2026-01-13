// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET
//
// Comprehensive tests for Series window operations, inspired by Polars test suite.
// These tests cover:
// - Ranking operations (Rank, DenseRank, OrdinalRank, PercentRank, RowNumber)
// - Lead/Lag operations
// - Fill operations (FillForward, FillBackward, FillInterpolate)
// - Window value operations (FirstValue, LastValue, NthValue)
// - Cumulative count (CumCount)

using FluentAssertions;
using Polaire;
using Polaire.DataTypes;
using Xunit;

namespace Polaire.Tests;

/// <summary>
/// Tests for Series window operations.
/// </summary>
public class SeriesWindowOperationsTests
{
    // ============================================================================
    // RowNumber Tests
    // ============================================================================

    [Fact]
    public void RowNumber_ReturnsSequentialNumbers()
    {
        var series = Series.FromValues("s", new[] { 10.0, 20.0, 30.0, 40.0, 50.0 });
        var result = series.RowNumber();

        result[0].AsInt64().Should().Be(1);
        result[1].AsInt64().Should().Be(2);
        result[2].AsInt64().Should().Be(3);
        result[3].AsInt64().Should().Be(4);
        result[4].AsInt64().Should().Be(5);
    }

    [Fact]
    public void RowNumber_Empty_ReturnsEmpty()
    {
        var series = Series.FromValues("s", Array.Empty<double>());
        var result = series.RowNumber();

        result.Length.Should().Be(0);
    }

    [Fact]
    public void RowNumber_SingleElement_ReturnsOne()
    {
        var series = Series.FromValues("s", new[] { 42.0 });
        var result = series.RowNumber();

        result[0].AsInt64().Should().Be(1);
    }

    // ============================================================================
    // Rank Tests
    // ============================================================================

    [Fact]
    public void Rank_DefaultAverage_ReturnsAverageRanks()
    {
        var series = Series.FromValues("s", new[] { 3.0, 1.0, 2.0, 1.0 });
        var result = series.Rank();

        // Values: 1, 1, 2, 3
        // Ranks: 1.5, 1.5, 3, 4 (average for ties)
        result[0].AsFloat64().Should().Be(4.0);   // 3 is rank 4
        result[1].AsFloat64().Should().Be(1.5);   // 1 is rank 1.5 (avg of 1 and 2)
        result[2].AsFloat64().Should().Be(3.0);   // 2 is rank 3
        result[3].AsFloat64().Should().Be(1.5);   // 1 is rank 1.5 (avg of 1 and 2)
    }

    [Fact]
    public void Rank_Descending_ReturnsDescendingRanks()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0 });
        var result = series.Rank(descending: true);

        result[0].AsFloat64().Should().Be(3.0);   // 1 is lowest, rank 3
        result[1].AsFloat64().Should().Be(2.0);   // 2 is middle, rank 2
        result[2].AsFloat64().Should().Be(1.0);   // 3 is highest, rank 1
    }

    [Fact]
    public void Rank_NoTies_ReturnsSequentialRanks()
    {
        var series = Series.FromValues("s", new[] { 5.0, 10.0, 15.0 });
        var result = series.Rank();

        result[0].AsFloat64().Should().Be(1.0);
        result[1].AsFloat64().Should().Be(2.0);
        result[2].AsFloat64().Should().Be(3.0);
    }

    // ============================================================================
    // DenseRank Tests
    // ============================================================================

    [Fact]
    public void DenseRank_WithTies_ReturnsConsecutiveRanks()
    {
        var series = Series.FromValues("s", new[] { 3.0, 1.0, 2.0, 1.0, 3.0 });
        var result = series.DenseRank();

        // Sorted: 1, 1, 2, 3, 3
        // Dense ranks: 1, 1, 2, 3, 3 (no gaps)
        // DenseRank returns Float64
        result[0].AsFloat64().Should().Be(3);   // 3 is rank 3
        result[1].AsFloat64().Should().Be(1);   // 1 is rank 1
        result[2].AsFloat64().Should().Be(2);   // 2 is rank 2
        result[3].AsFloat64().Should().Be(1);   // 1 is rank 1
        result[4].AsFloat64().Should().Be(3);   // 3 is rank 3
    }

    [Fact]
    public void DenseRank_Descending_ReturnsDescendingDenseRanks()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0 });
        var result = series.DenseRank(descending: true);

        // DenseRank returns Float64
        result[0].AsFloat64().Should().Be(3);
        result[1].AsFloat64().Should().Be(2);
        result[2].AsFloat64().Should().Be(1);
    }

    // ============================================================================
    // OrdinalRank Tests
    // ============================================================================

    [Fact]
    public void OrdinalRank_WithTies_ReturnsUniqueRanks()
    {
        var series = Series.FromValues("s", new[] { 3.0, 1.0, 1.0, 2.0 });
        var result = series.OrdinalRank();

        // Each element gets a unique rank based on position
        result.Length.Should().Be(4);
        // All ranks should be unique (1, 2, 3, 4 in some order)
        // OrdinalRank returns Float64
        var ranks = new[] { result[0].AsFloat64(), result[1].AsFloat64(), result[2].AsFloat64(), result[3].AsFloat64() };
        ranks.Distinct().Count().Should().Be(4);
    }

    // ============================================================================
    // PercentRank Tests
    // ============================================================================

    [Fact]
    public void PercentRank_ReturnsPercentileRanks()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 });
        var result = series.PercentRank();

        result[0].AsFloat64().Should().BeApproximately(0.0, 0.001);     // Lowest
        result[4].AsFloat64().Should().BeApproximately(1.0, 0.001);     // Highest
    }

    // ============================================================================
    // Lead Tests
    // ============================================================================

    [Fact]
    public void Lead_Default_ReturnsNextValue()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 });
        var result = series.Lead();

        result[0].AsFloat64().Should().Be(2.0);
        result[1].AsFloat64().Should().Be(3.0);
        result[2].AsFloat64().Should().Be(4.0);
        result[3].AsFloat64().Should().Be(5.0);
        result.IsNull(4).Should().BeTrue();  // No next value
    }

    [Fact]
    public void Lead_N2_ReturnsTwoAheadValue()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 });
        var result = series.Lead(2);

        result[0].AsFloat64().Should().Be(3.0);
        result[1].AsFloat64().Should().Be(4.0);
        result[2].AsFloat64().Should().Be(5.0);
        result.IsNull(3).Should().BeTrue();
        result.IsNull(4).Should().BeTrue();
    }

    [Fact]
    public void Lead_WithDefaultValue_UsesDefaultForMissing()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0 });
        var result = series.Lead(1, AnyValue.From(0.0));

        result[0].AsFloat64().Should().Be(2.0);
        result[1].AsFloat64().Should().Be(3.0);
        result[2].AsFloat64().Should().Be(0.0);  // Default value
    }

    // ============================================================================
    // Lag Tests
    // ============================================================================

    [Fact]
    public void Lag_Default_ReturnsPreviousValue()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 });
        var result = series.Lag();

        result.IsNull(0).Should().BeTrue();  // No previous value
        result[1].AsFloat64().Should().Be(1.0);
        result[2].AsFloat64().Should().Be(2.0);
        result[3].AsFloat64().Should().Be(3.0);
        result[4].AsFloat64().Should().Be(4.0);
    }

    [Fact]
    public void Lag_N2_ReturnsTwoBehindValue()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 });
        var result = series.Lag(2);

        result.IsNull(0).Should().BeTrue();
        result.IsNull(1).Should().BeTrue();
        result[2].AsFloat64().Should().Be(1.0);
        result[3].AsFloat64().Should().Be(2.0);
        result[4].AsFloat64().Should().Be(3.0);
    }

    [Fact]
    public void Lag_WithDefaultValue_UsesDefaultForMissing()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0 });
        var result = series.Lag(1, AnyValue.From(0.0));

        result[0].AsFloat64().Should().Be(0.0);  // Default value
        result[1].AsFloat64().Should().Be(1.0);
        result[2].AsFloat64().Should().Be(2.0);
    }

    // ============================================================================
    // FillForward Tests
    // ============================================================================

    [Fact]
    public void FillForward_FillsNullsWithPreviousValue()
    {
        var series = Series.FromNullable("s", new double?[] { 1.0, null, null, 4.0, null });
        var result = series.FillForward();

        result[0].AsFloat64().Should().Be(1.0);
        result[1].AsFloat64().Should().Be(1.0);  // Filled from 1
        result[2].AsFloat64().Should().Be(1.0);  // Filled from 1
        result[3].AsFloat64().Should().Be(4.0);
        result[4].AsFloat64().Should().Be(4.0);  // Filled from 4
    }

    [Fact]
    public void FillForward_LeadingNulls_StayNull()
    {
        var series = Series.FromNullable("s", new double?[] { null, null, 3.0, null });
        var result = series.FillForward();

        result.IsNull(0).Should().BeTrue();
        result.IsNull(1).Should().BeTrue();
        result[2].AsFloat64().Should().Be(3.0);
        result[3].AsFloat64().Should().Be(3.0);
    }

    [Fact]
    public void FillForward_NoNulls_ReturnsSame()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0 });
        var result = series.FillForward();

        result[0].AsFloat64().Should().Be(1.0);
        result[1].AsFloat64().Should().Be(2.0);
        result[2].AsFloat64().Should().Be(3.0);
    }

    // ============================================================================
    // FillBackward Tests
    // ============================================================================

    [Fact]
    public void FillBackward_FillsNullsWithNextValue()
    {
        var series = Series.FromNullable("s", new double?[] { null, 2.0, null, null, 5.0 });
        var result = series.FillBackward();

        result[0].AsFloat64().Should().Be(2.0);  // Filled from 2
        result[1].AsFloat64().Should().Be(2.0);
        result[2].AsFloat64().Should().Be(5.0);  // Filled from 5
        result[3].AsFloat64().Should().Be(5.0);  // Filled from 5
        result[4].AsFloat64().Should().Be(5.0);
    }

    [Fact]
    public void FillBackward_TrailingNulls_StayNull()
    {
        var series = Series.FromNullable("s", new double?[] { null, 2.0, null, null });
        var result = series.FillBackward();

        result[0].AsFloat64().Should().Be(2.0);
        result[1].AsFloat64().Should().Be(2.0);
        result.IsNull(2).Should().BeTrue();
        result.IsNull(3).Should().BeTrue();
    }

    // ============================================================================
    // FillInterpolate Tests
    // ============================================================================

    [Fact]
    public void FillInterpolate_LinearInterpolation()
    {
        var series = Series.FromNullable("s", new double?[] { 1.0, null, null, 4.0 });
        var result = series.FillInterpolate();

        result[0].AsFloat64().Should().Be(1.0);
        result[1].AsFloat64().Should().BeApproximately(2.0, 0.001);
        result[2].AsFloat64().Should().BeApproximately(3.0, 0.001);
        result[3].AsFloat64().Should().Be(4.0);
    }

    [Fact]
    public void FillInterpolate_SingleGap_Interpolates()
    {
        var series = Series.FromNullable("s", new double?[] { 0.0, null, 10.0 });
        var result = series.FillInterpolate();

        result[0].AsFloat64().Should().Be(0.0);
        result[1].AsFloat64().Should().BeApproximately(5.0, 0.001);
        result[2].AsFloat64().Should().Be(10.0);
    }

    // ============================================================================
    // FirstValue Tests
    // ============================================================================

    [Fact]
    public void FirstValue_ReturnsFirstValueForAll()
    {
        var series = Series.FromValues("s", new[] { 10.0, 20.0, 30.0, 40.0 });
        var result = series.FirstValue();

        result[0].AsFloat64().Should().Be(10.0);
        result[1].AsFloat64().Should().Be(10.0);
        result[2].AsFloat64().Should().Be(10.0);
        result[3].AsFloat64().Should().Be(10.0);
    }

    // ============================================================================
    // LastValue Tests
    // ============================================================================

    [Fact]
    public void LastValue_ReturnsLastValueForAll()
    {
        var series = Series.FromValues("s", new[] { 10.0, 20.0, 30.0, 40.0 });
        var result = series.LastValue();

        result[0].AsFloat64().Should().Be(40.0);
        result[1].AsFloat64().Should().Be(40.0);
        result[2].AsFloat64().Should().Be(40.0);
        result[3].AsFloat64().Should().Be(40.0);
    }

    // ============================================================================
    // NthValue Tests
    // ============================================================================

    [Fact]
    public void NthValue_ReturnsNthValueForAll()
    {
        var series = Series.FromValues("s", new[] { 10.0, 20.0, 30.0, 40.0, 50.0 });
        // NthValue uses 1-based indexing (n >= 1)
        var result = series.NthValue(3);  // 3rd non-null value = 30.0

        result[0].AsFloat64().Should().Be(30.0);
        result[1].AsFloat64().Should().Be(30.0);
        result[2].AsFloat64().Should().Be(30.0);
        result[3].AsFloat64().Should().Be(30.0);
        result[4].AsFloat64().Should().Be(30.0);
    }

    [Fact]
    public void NthValue_FirstElement_ReturnsFirst()
    {
        var series = Series.FromValues("s", new[] { 10.0, 20.0, 30.0 });
        // NthValue uses 1-based indexing (n >= 1)
        var result = series.NthValue(1);  // 1st non-null value = 10.0

        result[0].AsFloat64().Should().Be(10.0);
        result[1].AsFloat64().Should().Be(10.0);
        result[2].AsFloat64().Should().Be(10.0);
    }

    // ============================================================================
    // CumCount Tests
    // ============================================================================

    [Fact]
    public void CumCount_NoNulls_ReturnsCumulativeCount()
    {
        var series = Series.FromValues("s", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 });
        var result = series.CumCount();

        result[0].AsInt64().Should().Be(1);
        result[1].AsInt64().Should().Be(2);
        result[2].AsInt64().Should().Be(3);
        result[3].AsInt64().Should().Be(4);
        result[4].AsInt64().Should().Be(5);
    }

    [Fact]
    public void CumCount_WithNulls_CountsNonNulls()
    {
        var series = Series.FromNullable("s", new double?[] { 1.0, null, 3.0, null, 5.0 });
        var result = series.CumCount();

        result[0].AsInt64().Should().Be(1);   // 1 non-null so far
        result[1].AsInt64().Should().Be(1);   // Still 1 non-null
        result[2].AsInt64().Should().Be(2);   // 2 non-nulls
        result[3].AsInt64().Should().Be(2);   // Still 2 non-nulls
        result[4].AsInt64().Should().Be(3);   // 3 non-nulls
    }
}
