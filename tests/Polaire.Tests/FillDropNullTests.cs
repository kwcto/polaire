// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET
//
// Comprehensive tests for fill and drop null operations, inspired by Polars test suite.
// These tests cover:
// - Series.FillNull() for filling null values
// - Series.DropNulls() for removing null values
// - DataFrame null handling

using FluentAssertions;
using Polaire;
using Polaire.DataTypes;
using Xunit;
using static Polaire.Pl;

namespace Polaire.Tests;

/// <summary>
/// Tests for fill and drop null operations.
/// </summary>
public class FillDropNullTests
{
    // ============================================================================
    // Series FillNull Tests
    // ============================================================================

    [Fact]
    public void FillNull_Int_FillsWithValue()
    {
        var series = Series.FromNullable("s", new int?[] { 1, null, 3, null, 5 });
        var result = series.FillNull(AnyValue.From(0));

        result.Length.Should().Be(5);
        result[0].AsInt32().Should().Be(1);
        result[1].AsInt32().Should().Be(0);  // Filled
        result[2].AsInt32().Should().Be(3);
        result[3].AsInt32().Should().Be(0);  // Filled
        result[4].AsInt32().Should().Be(5);
    }

    [Fact]
    public void FillNull_Float_FillsWithValue()
    {
        var series = Series.FromNullable("s", new double?[] { 1.5, null, 3.5 });
        var result = series.FillNull(AnyValue.From(0.0));

        result[0].AsFloat64().Should().Be(1.5);
        result[1].AsFloat64().Should().Be(0.0);
        result[2].AsFloat64().Should().Be(3.5);
    }

    [Fact]
    public void FillNull_NoNulls_ReturnsOriginal()
    {
        var series = Series.FromValues("s", new[] { 1, 2, 3 });
        var result = series.FillNull(AnyValue.From(0));

        result[0].AsInt32().Should().Be(1);
        result[1].AsInt32().Should().Be(2);
        result[2].AsInt32().Should().Be(3);
    }

    [Fact]
    public void FillNull_AllNulls_FillsAll()
    {
        var series = Series.FromNullable("s", new int?[] { null, null, null });
        var result = series.FillNull(AnyValue.From(-1));

        result[0].AsInt32().Should().Be(-1);
        result[1].AsInt32().Should().Be(-1);
        result[2].AsInt32().Should().Be(-1);
    }

    [Fact]
    public void FillNull_EmptySeries_ReturnsEmpty()
    {
        var series = Series.FromNullable("s", Array.Empty<int?>());
        var result = series.FillNull(AnyValue.From(0));

        result.Length.Should().Be(0);
    }

    [Fact]
    public void FillNull_SingleNull_Fills()
    {
        var series = Series.FromNullable("s", new int?[] { null });
        var result = series.FillNull(AnyValue.From(42));

        result.Length.Should().Be(1);
        result[0].AsInt32().Should().Be(42);
    }

    // ============================================================================
    // Series DropNulls Tests
    // ============================================================================

    [Fact]
    public void DropNulls_RemovesNulls()
    {
        var series = Series.FromNullable("s", new int?[] { 1, null, 3, null, 5 });
        var result = series.DropNulls();

        result.Length.Should().Be(3);
        result[0].AsInt32().Should().Be(1);
        result[1].AsInt32().Should().Be(3);
        result[2].AsInt32().Should().Be(5);
    }

    [Fact]
    public void DropNulls_NoNulls_ReturnsAll()
    {
        var series = Series.FromValues("s", new[] { 1, 2, 3 });
        var result = series.DropNulls();

        result.Length.Should().Be(3);
    }

    [Fact]
    public void DropNulls_AllNulls_ReturnsEmpty()
    {
        var series = Series.FromNullable("s", new int?[] { null, null, null });
        var result = series.DropNulls();

        result.Length.Should().Be(0);
    }

    [Fact]
    public void DropNulls_EmptySeries_ReturnsEmpty()
    {
        var series = Series.FromNullable("s", Array.Empty<int?>());
        var result = series.DropNulls();

        result.Length.Should().Be(0);
    }

    [Fact]
    public void DropNulls_SingleNull_ReturnsEmpty()
    {
        var series = Series.FromNullable("s", new int?[] { null });
        var result = series.DropNulls();

        result.Length.Should().Be(0);
    }

    [Fact]
    public void DropNulls_SingleValue_ReturnsValue()
    {
        var series = Series.FromNullable("s", new int?[] { 42 });
        var result = series.DropNulls();

        result.Length.Should().Be(1);
        result[0].AsInt32().Should().Be(42);
    }

    [Fact]
    public void DropNulls_NullAtBeginning_RemovesCorrectly()
    {
        var series = Series.FromNullable("s", new int?[] { null, 1, 2 });
        var result = series.DropNulls();

        result.Length.Should().Be(2);
        result[0].AsInt32().Should().Be(1);
        result[1].AsInt32().Should().Be(2);
    }

    [Fact]
    public void DropNulls_NullAtEnd_RemovesCorrectly()
    {
        var series = Series.FromNullable("s", new int?[] { 1, 2, null });
        var result = series.DropNulls();

        result.Length.Should().Be(2);
        result[0].AsInt32().Should().Be(1);
        result[1].AsInt32().Should().Be(2);
    }

    // ============================================================================
    // IsNull / IsNotNull Tests
    // ============================================================================

    [Fact]
    public void IsNull_ReturnsCorrectMask()
    {
        var series = Series.FromNullable("s", new int?[] { 1, null, 3 });
        var result = series.IsNull();

        result.Length.Should().Be(3);
        result[0].AsBoolean().Should().BeFalse();
        result[1].AsBoolean().Should().BeTrue();
        result[2].AsBoolean().Should().BeFalse();
    }

    [Fact]
    public void IsNotNull_ReturnsCorrectMask()
    {
        var series = Series.FromNullable("s", new int?[] { 1, null, 3 });
        var result = series.IsNotNull();

        result.Length.Should().Be(3);
        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeFalse();
        result[2].AsBoolean().Should().BeTrue();
    }

    [Fact]
    public void IsNull_NoNulls_AllFalse()
    {
        var series = Series.FromValues("s", new[] { 1, 2, 3 });
        var result = series.IsNull();

        result[0].AsBoolean().Should().BeFalse();
        result[1].AsBoolean().Should().BeFalse();
        result[2].AsBoolean().Should().BeFalse();
    }

    [Fact]
    public void IsNull_AllNulls_AllTrue()
    {
        var series = Series.FromNullable("s", new int?[] { null, null, null });
        var result = series.IsNull();

        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeTrue();
        result[2].AsBoolean().Should().BeTrue();
    }

    // ============================================================================
    // NullCount Tests
    // ============================================================================

    [Fact]
    public void NullCount_ReturnsCorrectCount()
    {
        var series = Series.FromNullable("s", new int?[] { 1, null, 3, null, 5 });
        var count = series.NullCount;

        count.Should().Be(2);
    }

    [Fact]
    public void NullCount_NoNulls_ReturnsZero()
    {
        var series = Series.FromValues("s", new[] { 1, 2, 3 });
        var count = series.NullCount;

        count.Should().Be(0);
    }

    [Fact]
    public void NullCount_AllNulls_ReturnsLength()
    {
        var series = Series.FromNullable("s", new int?[] { null, null, null });
        var count = series.NullCount;

        count.Should().Be(3);
    }

    [Fact]
    public void NullCount_Empty_ReturnsZero()
    {
        var series = Series.FromNullable("s", Array.Empty<int?>());
        var count = series.NullCount;

        count.Should().Be(0);
    }

    // ============================================================================
    // Type Preservation Tests
    // ============================================================================

    [Fact]
    public void FillNull_PreservesType()
    {
        var series = Series.FromNullable("s", new double?[] { 1.5, null, 3.5 });
        var result = series.FillNull(AnyValue.From(0.0));

        result.DataType.Should().Be(DataType.Float64);
    }

    [Fact]
    public void DropNulls_PreservesType()
    {
        var series = Series.FromNullable("s", new double?[] { 1.5, null, 3.5 });
        var result = series.DropNulls();

        result.DataType.Should().Be(DataType.Float64);
    }

    // ============================================================================
    // Chained Operations Tests
    // ============================================================================

    [Fact]
    public void FillNullThenSum_Works()
    {
        var series = Series.FromNullable("s", new double?[] { 1.0, null, 3.0 });
        var filled = series.FillNull(AnyValue.From(2.0));
        var sum = filled.Sum();

        sum.AsFloat64().Should().Be(6.0);
    }

    [Fact]
    public void DropNullsThenSort_Works()
    {
        var series = Series.FromNullable("s", new double?[] { 3.0, null, 1.0, null, 2.0 });
        var result = series.DropNulls().Sort();

        result.Length.Should().Be(3);
        result[0].AsFloat64().Should().Be(1.0);
        result[1].AsFloat64().Should().Be(2.0);
        result[2].AsFloat64().Should().Be(3.0);
    }

    // ============================================================================
    // Large Data Tests
    // ============================================================================

    [Fact]
    public void FillNull_LargeSeries_Succeeds()
    {
        var values = new int?[10000];
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = i % 10 == 0 ? null : i;
        }

        var series = Series.FromNullable("s", values);
        var result = series.FillNull(AnyValue.From(-1));

        result.Length.Should().Be(10000);
        result[0].AsInt32().Should().Be(-1);  // Was null
        result[1].AsInt32().Should().Be(1);   // Was not null
    }

    [Fact]
    public void DropNulls_LargeSeries_Succeeds()
    {
        var values = new int?[10000];
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = i % 10 == 0 ? null : i;
        }

        var series = Series.FromNullable("s", values);
        var result = series.DropNulls();

        result.Length.Should().Be(9000);  // 1000 nulls removed
    }

    // ============================================================================
    // Boolean Series Null Tests
    // ============================================================================

    [Fact]
    public void FillNull_Boolean_Works()
    {
        var series = Series.FromNullable("s", new bool?[] { true, null, false });
        var result = series.FillNull(AnyValue.From(false));

        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeFalse();
        result[2].AsBoolean().Should().BeFalse();
    }

    [Fact]
    public void DropNulls_Boolean_Works()
    {
        var series = Series.FromNullable("s", new bool?[] { true, null, false, null });
        var result = series.DropNulls();

        result.Length.Should().Be(2);
        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeFalse();
    }
}
