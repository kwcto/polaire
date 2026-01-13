// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using FluentAssertions;
using Polaire;
using Polaire.Compute;
using Polaire.DataTypes;
using Xunit;

namespace Polaire.Tests;

public class WindowOperationsTests
{
    // ============================================================================
    // RowNumber Tests
    // ============================================================================

    [Fact]
    public void RowNumber_BasicSeries_ReturnsOneIndexedPositions()
    {
        var series = Series.FromValues("x", new double[] { 10, 20, 30, 40, 50 });
        var result = series.RowNumber();

        result.Length.Should().Be(5);
        result[0].AsInt64().Should().Be(1);
        result[1].AsInt64().Should().Be(2);
        result[2].AsInt64().Should().Be(3);
        result[3].AsInt64().Should().Be(4);
        result[4].AsInt64().Should().Be(5);
    }

    [Fact]
    public void RowNumber_EmptySeries_ReturnsEmpty()
    {
        var series = Series.FromValues("x", Array.Empty<double>());
        var result = series.RowNumber();
        result.Length.Should().Be(0);
    }

    [Fact]
    public void RowNumber_SingleElement_ReturnsOne()
    {
        var series = Series.FromValues("x", new double[] { 42 });
        var result = series.RowNumber();
        result.Length.Should().Be(1);
        result[0].AsInt64().Should().Be(1);
    }

    // ============================================================================
    // Rank Tests - Average Method (Default)
    // ============================================================================

    [Fact]
    public void Rank_UniqueValues_ReturnsSequentialRanks()
    {
        var series = Series.FromValues("x", new double[] { 30, 10, 20 });
        var result = series.Rank();

        // 10 -> rank 1, 20 -> rank 2, 30 -> rank 3
        result[0].AsFloat64().Should().Be(3);  // 30 is rank 3
        result[1].AsFloat64().Should().Be(1);  // 10 is rank 1
        result[2].AsFloat64().Should().Be(2);  // 20 is rank 2
    }

    [Fact]
    public void Rank_TiedValues_AverageMethod_ReturnsAverageRank()
    {
        var series = Series.FromValues("x", new double[] { 10, 20, 20, 30 });
        var result = series.Rank("average");

        // 10 -> rank 1, 20 -> avg(2,3) = 2.5, 30 -> rank 4
        result[0].AsFloat64().Should().Be(1);
        result[1].AsFloat64().Should().Be(2.5);  // Tied: average of 2 and 3
        result[2].AsFloat64().Should().Be(2.5);
        result[3].AsFloat64().Should().Be(4);
    }

    [Fact]
    public void Rank_TiedValues_MinMethod_ReturnsMinRank()
    {
        var series = Series.FromValues("x", new double[] { 10, 20, 20, 30 });
        var result = series.Rank("min");

        result[0].AsFloat64().Should().Be(1);
        result[1].AsFloat64().Should().Be(2);  // Min of 2 and 3
        result[2].AsFloat64().Should().Be(2);
        result[3].AsFloat64().Should().Be(4);
    }

    [Fact]
    public void Rank_TiedValues_MaxMethod_ReturnsMaxRank()
    {
        var series = Series.FromValues("x", new double[] { 10, 20, 20, 30 });
        var result = series.Rank("max");

        result[0].AsFloat64().Should().Be(1);
        result[1].AsFloat64().Should().Be(3);  // Max of 2 and 3
        result[2].AsFloat64().Should().Be(3);
        result[3].AsFloat64().Should().Be(4);
    }

    [Fact]
    public void Rank_TiedValues_FirstMethod_ReturnsOrdinalRank()
    {
        var series = Series.FromValues("x", new double[] { 10, 20, 20, 30 });
        var result = series.Rank("first");

        result[0].AsFloat64().Should().Be(1);
        result[1].AsFloat64().Should().Be(2);  // First occurrence gets 2
        result[2].AsFloat64().Should().Be(3);  // Second occurrence gets 3
        result[3].AsFloat64().Should().Be(4);
    }

    [Fact]
    public void Rank_TiedValues_DenseMethod_ReturnsConsecutiveRanks()
    {
        var series = Series.FromValues("x", new double[] { 10, 20, 20, 30 });
        var result = series.Rank("dense");

        result[0].AsFloat64().Should().Be(1);
        result[1].AsFloat64().Should().Be(2);  // Same value = same rank
        result[2].AsFloat64().Should().Be(2);
        result[3].AsFloat64().Should().Be(3);  // No gap! Next consecutive rank
    }

    [Fact]
    public void Rank_Descending_ReversesOrder()
    {
        var series = Series.FromValues("x", new double[] { 10, 20, 30 });
        var result = series.Rank("average", descending: true);

        // Descending: 30 -> rank 1, 20 -> rank 2, 10 -> rank 3
        result[0].AsFloat64().Should().Be(3);  // 10 is rank 3
        result[1].AsFloat64().Should().Be(2);  // 20 is rank 2
        result[2].AsFloat64().Should().Be(1);  // 30 is rank 1
    }

    [Fact]
    public void Rank_WithNulls_NullsGetNullRank()
    {
        var series = Series.FromNullable("x", new double?[] { 10, null, 20, null, 30 });
        var result = series.Rank();

        result[0].AsFloat64().Should().Be(1);
        result.IsNull(1).Should().BeTrue();
        result[2].AsFloat64().Should().Be(2);
        result.IsNull(3).Should().BeTrue();
        result[4].AsFloat64().Should().Be(3);
    }

    [Fact]
    public void Rank_EmptySeries_ReturnsEmpty()
    {
        var series = Series.FromValues("x", Array.Empty<double>());
        var result = series.Rank();
        result.Length.Should().Be(0);
    }

    [Fact]
    public void Rank_InvalidMethod_ThrowsException()
    {
        var series = Series.FromValues("x", new double[] { 1, 2, 3 });
        var act = () => series.Rank("invalid_method");
        act.Should().Throw<ArgumentException>().WithMessage("*Unknown ranking method*");
    }

    // ============================================================================
    // DenseRank and OrdinalRank (Convenience Methods)
    // ============================================================================

    [Fact]
    public void DenseRank_BasicSeries_ReturnsConsecutiveRanks()
    {
        var series = Series.FromValues("x", new double[] { 10, 20, 20, 30, 30, 30, 40 });
        var result = series.DenseRank();

        result[0].AsFloat64().Should().Be(1);  // 10
        result[1].AsFloat64().Should().Be(2);  // 20
        result[2].AsFloat64().Should().Be(2);  // 20
        result[3].AsFloat64().Should().Be(3);  // 30
        result[4].AsFloat64().Should().Be(3);  // 30
        result[5].AsFloat64().Should().Be(3);  // 30
        result[6].AsFloat64().Should().Be(4);  // 40
    }

    [Fact]
    public void OrdinalRank_BasicSeries_ReturnsUniqueRanks()
    {
        var series = Series.FromValues("x", new double[] { 10, 20, 20, 30 });
        var result = series.OrdinalRank();

        result[0].AsFloat64().Should().Be(1);
        result[1].AsFloat64().Should().Be(2);
        result[2].AsFloat64().Should().Be(3);
        result[3].AsFloat64().Should().Be(4);
    }

    // ============================================================================
    // PercentRank Tests
    // ============================================================================

    [Fact]
    public void PercentRank_BasicSeries_ReturnsNormalizedRanks()
    {
        var series = Series.FromValues("x", new double[] { 10, 20, 30, 40, 50 });
        var result = series.PercentRank();

        // Percent rank: (rank - 1) / (n - 1)
        result[0].AsFloat64().Should().BeApproximately(0.0, 0.0001);   // (1-1)/(5-1) = 0
        result[1].AsFloat64().Should().BeApproximately(0.25, 0.0001);  // (2-1)/(5-1) = 0.25
        result[2].AsFloat64().Should().BeApproximately(0.5, 0.0001);   // (3-1)/(5-1) = 0.5
        result[3].AsFloat64().Should().BeApproximately(0.75, 0.0001);  // (4-1)/(5-1) = 0.75
        result[4].AsFloat64().Should().BeApproximately(1.0, 0.0001);   // (5-1)/(5-1) = 1.0
    }

    [Fact]
    public void PercentRank_SingleElement_ReturnsZero()
    {
        var series = Series.FromValues("x", new double[] { 42 });
        var result = series.PercentRank();
        result[0].AsFloat64().Should().Be(0);  // (1-1)/(1-1) = 0/0 -> 0
    }

    [Fact]
    public void PercentRank_WithNulls_NullsGetNullResult()
    {
        var series = Series.FromNullable("x", new double?[] { 10, null, 30 });
        var result = series.PercentRank();

        result[0].AsFloat64().Should().BeApproximately(0.0, 0.0001);
        result.IsNull(1).Should().BeTrue();
        result[2].AsFloat64().Should().BeApproximately(1.0, 0.0001);
    }

    // ============================================================================
    // Lead Tests
    // ============================================================================

    [Fact]
    public void Lead_Default_ShiftsForwardByOne()
    {
        var series = Series.FromValues("x", new double[] { 1, 2, 3, 4, 5 });
        var result = series.Lead();

        result[0].AsFloat64().Should().Be(2);
        result[1].AsFloat64().Should().Be(3);
        result[2].AsFloat64().Should().Be(4);
        result[3].AsFloat64().Should().Be(5);
        result.IsNull(4).Should().BeTrue();  // No future value
    }

    [Fact]
    public void Lead_WithOffset_ShiftsForwardByN()
    {
        var series = Series.FromValues("x", new double[] { 1, 2, 3, 4, 5 });
        var result = series.Lead(2);

        result[0].AsFloat64().Should().Be(3);
        result[1].AsFloat64().Should().Be(4);
        result[2].AsFloat64().Should().Be(5);
        result.IsNull(3).Should().BeTrue();
        result.IsNull(4).Should().BeTrue();
    }

    [Fact]
    public void Lead_WithDefaultValue_FillsMissing()
    {
        var series = Series.FromValues("x", new double[] { 1, 2, 3 });
        var result = series.Lead(1, AnyValue.From(0.0));

        result[0].AsFloat64().Should().Be(2);
        result[1].AsFloat64().Should().Be(3);
        result[2].AsFloat64().Should().Be(0);  // Default value
    }

    // ============================================================================
    // Lag Tests
    // ============================================================================

    [Fact]
    public void Lag_Default_ShiftsBackwardByOne()
    {
        var series = Series.FromValues("x", new double[] { 1, 2, 3, 4, 5 });
        var result = series.Lag();

        result.IsNull(0).Should().BeTrue();  // No previous value
        result[1].AsFloat64().Should().Be(1);
        result[2].AsFloat64().Should().Be(2);
        result[3].AsFloat64().Should().Be(3);
        result[4].AsFloat64().Should().Be(4);
    }

    [Fact]
    public void Lag_WithOffset_ShiftsBackwardByN()
    {
        var series = Series.FromValues("x", new double[] { 1, 2, 3, 4, 5 });
        var result = series.Lag(2);

        result.IsNull(0).Should().BeTrue();
        result.IsNull(1).Should().BeTrue();
        result[2].AsFloat64().Should().Be(1);
        result[3].AsFloat64().Should().Be(2);
        result[4].AsFloat64().Should().Be(3);
    }

    [Fact]
    public void Lag_WithDefaultValue_FillsMissing()
    {
        var series = Series.FromValues("x", new double[] { 1, 2, 3 });
        var result = series.Lag(1, AnyValue.From(-1.0));

        result[0].AsFloat64().Should().Be(-1);  // Default value
        result[1].AsFloat64().Should().Be(1);
        result[2].AsFloat64().Should().Be(2);
    }

    // ============================================================================
    // FirstValue Tests
    // ============================================================================

    [Fact]
    public void FirstValue_BasicSeries_RepeatsFirstValue()
    {
        var series = Series.FromValues("x", new double[] { 10, 20, 30 });
        var result = series.FirstValue();

        result.Length.Should().Be(3);
        result[0].AsFloat64().Should().Be(10);
        result[1].AsFloat64().Should().Be(10);
        result[2].AsFloat64().Should().Be(10);
    }

    [Fact]
    public void FirstValue_WithLeadingNulls_SkipsNulls()
    {
        var series = Series.FromNullable("x", new double?[] { null, null, 30, 40 });
        var result = series.FirstValue();

        result[0].AsFloat64().Should().Be(30);
        result[1].AsFloat64().Should().Be(30);
        result[2].AsFloat64().Should().Be(30);
        result[3].AsFloat64().Should().Be(30);
    }

    [Fact]
    public void FirstValue_AllNulls_ReturnsAllNulls()
    {
        var series = Series.FromNullable("x", new double?[] { null, null, null });
        var result = series.FirstValue();

        result.IsNull(0).Should().BeTrue();
        result.IsNull(1).Should().BeTrue();
        result.IsNull(2).Should().BeTrue();
    }

    // ============================================================================
    // LastValue Tests
    // ============================================================================

    [Fact]
    public void LastValue_BasicSeries_RepeatsLastValue()
    {
        var series = Series.FromValues("x", new double[] { 10, 20, 30 });
        var result = series.LastValue();

        result.Length.Should().Be(3);
        result[0].AsFloat64().Should().Be(30);
        result[1].AsFloat64().Should().Be(30);
        result[2].AsFloat64().Should().Be(30);
    }

    [Fact]
    public void LastValue_WithTrailingNulls_SkipsNulls()
    {
        var series = Series.FromNullable("x", new double?[] { 10, 20, null, null });
        var result = series.LastValue();

        result[0].AsFloat64().Should().Be(20);
        result[1].AsFloat64().Should().Be(20);
        result[2].AsFloat64().Should().Be(20);
        result[3].AsFloat64().Should().Be(20);
    }

    // ============================================================================
    // NthValue Tests
    // ============================================================================

    [Fact]
    public void NthValue_BasicSeries_RepeatsNthValue()
    {
        var series = Series.FromValues("x", new double[] { 10, 20, 30, 40, 50 });
        var result = series.NthValue(3);  // 1-indexed, so 3rd value = 30

        result.Length.Should().Be(5);
        for (int i = 0; i < 5; i++)
        {
            result[i].AsFloat64().Should().Be(30);
        }
    }

    [Fact]
    public void NthValue_WithNulls_CountsOnlyNonNulls()
    {
        var series = Series.FromNullable("x", new double?[] { null, 10, null, 20, 30 });
        var result = series.NthValue(2);  // 2nd non-null value = 20

        for (int i = 0; i < 5; i++)
        {
            result[i].AsFloat64().Should().Be(20);
        }
    }

    [Fact]
    public void NthValue_BeyondLength_ReturnsAllNulls()
    {
        var series = Series.FromValues("x", new double[] { 10, 20 });
        var result = series.NthValue(5);  // Only 2 values exist

        result.IsNull(0).Should().BeTrue();
        result.IsNull(1).Should().BeTrue();
    }

    [Fact]
    public void NthValue_InvalidN_ThrowsException()
    {
        var series = Series.FromValues("x", new double[] { 1, 2, 3 });
        var act = () => series.NthValue(0);  // n must be >= 1
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ============================================================================
    // CumCount Tests
    // ============================================================================

    [Fact]
    public void CumCount_AllValid_ReturnsCumulativeCount()
    {
        var series = Series.FromValues("x", new double[] { 10, 20, 30 });
        var result = series.CumCount();

        result[0].AsInt64().Should().Be(1);
        result[1].AsInt64().Should().Be(2);
        result[2].AsInt64().Should().Be(3);
    }

    [Fact]
    public void CumCount_WithNulls_SkipsNullsInCount()
    {
        var series = Series.FromNullable("x", new double?[] { 10, null, 20, null, 30 });
        var result = series.CumCount();

        result[0].AsInt64().Should().Be(1);  // 10
        result[1].AsInt64().Should().Be(1);  // null - count stays 1
        result[2].AsInt64().Should().Be(2);  // 20
        result[3].AsInt64().Should().Be(2);  // null - count stays 2
        result[4].AsInt64().Should().Be(3);  // 30
    }

    [Fact]
    public void CumCount_AllNulls_ReturnsZeros()
    {
        var series = Series.FromNullable("x", new double?[] { null, null, null });
        var result = series.CumCount();

        result[0].AsInt64().Should().Be(0);
        result[1].AsInt64().Should().Be(0);
        result[2].AsInt64().Should().Be(0);
    }

    // ============================================================================
    // FillForward Tests
    // ============================================================================

    [Fact]
    public void FillForward_BasicSeries_FillsNullsWithPrevious()
    {
        var series = Series.FromNullable("x", new double?[] { 1, null, null, 4, null });
        var result = series.FillForward();

        result[0].AsFloat64().Should().Be(1);
        result[1].AsFloat64().Should().Be(1);
        result[2].AsFloat64().Should().Be(1);
        result[3].AsFloat64().Should().Be(4);
        result[4].AsFloat64().Should().Be(4);
    }

    [Fact]
    public void FillForward_LeadingNulls_RemainNull()
    {
        var series = Series.FromNullable("x", new double?[] { null, null, 3, null });
        var result = series.FillForward();

        result.IsNull(0).Should().BeTrue();
        result.IsNull(1).Should().BeTrue();
        result[2].AsFloat64().Should().Be(3);
        result[3].AsFloat64().Should().Be(3);
    }

    [Fact]
    public void FillForward_NoNulls_ReturnsOriginal()
    {
        var series = Series.FromValues("x", new double[] { 1, 2, 3 });
        var result = series.FillForward();

        result[0].AsFloat64().Should().Be(1);
        result[1].AsFloat64().Should().Be(2);
        result[2].AsFloat64().Should().Be(3);
    }

    // ============================================================================
    // FillBackward Tests
    // ============================================================================

    [Fact]
    public void FillBackward_BasicSeries_FillsNullsWithNext()
    {
        var series = Series.FromNullable("x", new double?[] { null, 2, null, null, 5 });
        var result = series.FillBackward();

        result[0].AsFloat64().Should().Be(2);
        result[1].AsFloat64().Should().Be(2);
        result[2].AsFloat64().Should().Be(5);
        result[3].AsFloat64().Should().Be(5);
        result[4].AsFloat64().Should().Be(5);
    }

    [Fact]
    public void FillBackward_TrailingNulls_RemainNull()
    {
        var series = Series.FromNullable("x", new double?[] { null, 2, null, null });
        var result = series.FillBackward();

        result[0].AsFloat64().Should().Be(2);
        result[1].AsFloat64().Should().Be(2);
        result.IsNull(2).Should().BeTrue();
        result.IsNull(3).Should().BeTrue();
    }

    // ============================================================================
    // FillInterpolate Tests
    // ============================================================================

    [Fact]
    public void FillInterpolate_BasicSeries_InterpolatesLinearly()
    {
        var series = Series.FromNullable("x", new double?[] { 0, null, null, 6 });
        var result = series.FillInterpolate();

        result[0].AsFloat64().Should().Be(0);
        result[1].AsFloat64().Should().BeApproximately(2, 0.0001);  // Linear interpolation
        result[2].AsFloat64().Should().BeApproximately(4, 0.0001);
        result[3].AsFloat64().Should().Be(6);
    }

    [Fact]
    public void FillInterpolate_SingleGap_InterpolatesCorrectly()
    {
        var series = Series.FromNullable("x", new double?[] { 10, null, 20 });
        var result = series.FillInterpolate();

        result[0].AsFloat64().Should().Be(10);
        result[1].AsFloat64().Should().Be(15);  // Midpoint
        result[2].AsFloat64().Should().Be(20);
    }

    [Fact]
    public void FillInterpolate_LeadingNulls_RemainNull()
    {
        var series = Series.FromNullable("x", new double?[] { null, null, 10, 20 });
        var result = series.FillInterpolate();

        result.IsNull(0).Should().BeTrue();
        result.IsNull(1).Should().BeTrue();
        result[2].AsFloat64().Should().Be(10);
        result[3].AsFloat64().Should().Be(20);
    }

    [Fact]
    public void FillInterpolate_TrailingNulls_RemainNull()
    {
        var series = Series.FromNullable("x", new double?[] { 10, 20, null, null });
        var result = series.FillInterpolate();

        result[0].AsFloat64().Should().Be(10);
        result[1].AsFloat64().Should().Be(20);
        result.IsNull(2).Should().BeTrue();
        result.IsNull(3).Should().BeTrue();
    }

    [Fact]
    public void FillInterpolate_MultipleGaps_InterpolatesEach()
    {
        var series = Series.FromNullable("x", new double?[] { 0, null, 2, null, 4 });
        var result = series.FillInterpolate();

        result[0].AsFloat64().Should().Be(0);
        result[1].AsFloat64().Should().BeApproximately(1, 0.0001);
        result[2].AsFloat64().Should().Be(2);
        result[3].AsFloat64().Should().BeApproximately(3, 0.0001);
        result[4].AsFloat64().Should().Be(4);
    }

    // ============================================================================
    // Edge Cases
    // ============================================================================

    [Fact]
    public void AllWindowOperations_EmptySeries_HandleGracefully()
    {
        var empty = Series.FromValues("x", Array.Empty<double>());

        empty.RowNumber().Length.Should().Be(0);
        empty.Rank().Length.Should().Be(0);
        empty.DenseRank().Length.Should().Be(0);
        empty.PercentRank().Length.Should().Be(0);
        empty.Lead().Length.Should().Be(0);
        empty.Lag().Length.Should().Be(0);
        empty.FirstValue().Length.Should().Be(0);
        empty.LastValue().Length.Should().Be(0);
        empty.CumCount().Length.Should().Be(0);
        empty.FillForward().Length.Should().Be(0);
        empty.FillBackward().Length.Should().Be(0);
        empty.FillInterpolate().Length.Should().Be(0);
    }

    [Fact]
    public void Rank_AllSameValues_AllGetSameRank()
    {
        var series = Series.FromValues("x", new double[] { 5, 5, 5, 5 });
        var result = series.Rank("average");

        // All tied: average of ranks 1,2,3,4 = 2.5
        result[0].AsFloat64().Should().Be(2.5);
        result[1].AsFloat64().Should().Be(2.5);
        result[2].AsFloat64().Should().Be(2.5);
        result[3].AsFloat64().Should().Be(2.5);
    }

    [Fact]
    public void Lead_LargeOffset_ReturnsAllNulls()
    {
        var series = Series.FromValues("x", new double[] { 1, 2, 3 });
        var result = series.Lead(100);

        result.IsNull(0).Should().BeTrue();
        result.IsNull(1).Should().BeTrue();
        result.IsNull(2).Should().BeTrue();
    }

    [Fact]
    public void Lag_LargeOffset_ReturnsAllNulls()
    {
        var series = Series.FromValues("x", new double[] { 1, 2, 3 });
        var result = series.Lag(100);

        result.IsNull(0).Should().BeTrue();
        result.IsNull(1).Should().BeTrue();
        result.IsNull(2).Should().BeTrue();
    }

    [Fact]
    public void Rank_WithNaN_CurrentBehavior()
    {
        // NaN handling in ranking: NaN from TryGetDouble is treated as a value
        // (not null) since the validity bitmap doesn't mark it as null.
        // The Rank function processes it but NaN comparison is undefined,
        // so rankings may vary. This test documents current behavior.
        var series = Series.FromValues("x", new double[] { 1, double.NaN, 3 });
        var result = series.Rank();

        // All elements should have rankings (NaN isn't filtered)
        result.Length.Should().Be(3);
        result[0].AsFloat64().Should().BeGreaterThan(0);
        result[2].AsFloat64().Should().BeGreaterThan(0);
        // NaN position gets a rank too (though comparison semantics are complex)
    }

    [Fact]
    public void FillInterpolate_NegativeValues_InterpolatesCorrectly()
    {
        var series = Series.FromNullable("x", new double?[] { -10, null, 10 });
        var result = series.FillInterpolate();

        result[0].AsFloat64().Should().Be(-10);
        result[1].AsFloat64().Should().BeApproximately(0, 0.0001);
        result[2].AsFloat64().Should().Be(10);
    }

    [Fact]
    public void WindowOperations_PreserveSeriesName()
    {
        var series = Series.FromValues("my_column", new double[] { 1, 2, 3 });

        series.RowNumber().Name.Should().Be("my_column");
        series.Rank().Name.Should().Be("my_column");
        series.Lead().Name.Should().Be("my_column");
        series.Lag().Name.Should().Be("my_column");
        series.FirstValue().Name.Should().Be("my_column");
        series.CumCount().Name.Should().Be("my_column");
        series.FillForward().Name.Should().Be("my_column");
    }

    [Fact]
    public void Lead_WithNullsInSeries_PreservesNulls()
    {
        var series = Series.FromNullable("x", new double?[] { 1, null, 3, 4 });
        var result = series.Lead(1);

        result.IsNull(0).Should().BeTrue();  // Original null shifted
        result[1].AsFloat64().Should().Be(3);
        result[2].AsFloat64().Should().Be(4);
        result.IsNull(3).Should().BeTrue();  // End of series
    }

    [Fact]
    public void Lag_WithNullsInSeries_PreservesNulls()
    {
        var series = Series.FromNullable("x", new double?[] { 1, null, 3, 4 });
        var result = series.Lag(1);

        result.IsNull(0).Should().BeTrue();  // Start of series
        result[1].AsFloat64().Should().Be(1);
        result.IsNull(2).Should().BeTrue();  // Original null shifted
        result[3].AsFloat64().Should().Be(3);
    }

    [Fact]
    public void DenseRank_Descending_ReversesOrder()
    {
        var series = Series.FromValues("x", new double[] { 10, 20, 20, 30 });
        var result = series.DenseRank(descending: true);

        // Descending: 30 -> 1, 20 -> 2, 10 -> 3
        result[0].AsFloat64().Should().Be(3);  // 10
        result[1].AsFloat64().Should().Be(2);  // 20
        result[2].AsFloat64().Should().Be(2);  // 20
        result[3].AsFloat64().Should().Be(1);  // 30
    }

    // ============================================================================
    // Integer Series Tests
    // ============================================================================

    [Fact]
    public void WindowOperations_IntegerSeries_WorkCorrectly()
    {
        var series = Series.FromValues("x", new int[] { 30, 10, 20 });

        var rank = series.Rank();
        rank[0].AsFloat64().Should().Be(3);  // 30 -> rank 3
        rank[1].AsFloat64().Should().Be(1);  // 10 -> rank 1
        rank[2].AsFloat64().Should().Be(2);  // 20 -> rank 2

        var lead = series.Lead();
        lead[0].AsFloat64().Should().Be(10);  // Next value
        lead[1].AsFloat64().Should().Be(20);
        lead.IsNull(2).Should().BeTrue();

        var cumCount = series.CumCount();
        cumCount[0].AsInt64().Should().Be(1);
        cumCount[1].AsInt64().Should().Be(2);
        cumCount[2].AsInt64().Should().Be(3);
    }
}
