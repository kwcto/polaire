// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using FluentAssertions;
using Polaire.DataTypes;
using Xunit;

namespace Polaire.Tests;

/// <summary>
/// Tests for Series.Concat / Append / NUnique and DataFrame.VStack / HStack / VConcat.
/// </summary>
public class ConcatAndStackTests
{
    // ============================================================================
    // Series.Concat and Append
    // ============================================================================

    [Fact]
    public void SeriesConcat_JoinsValuesAndKeepsFirstName()
    {
        var result = Series.Concat(
            Series.FromValues("a", new[] { 1, 2 }),
            Series.FromValues("b", new[] { 3 }),
            Series.FromValues("c", new[] { 4, 5 }));

        result.Name.Should().Be("a");
        result.DataType.Should().Be(DataType.Int32);
        result.ToArray<int>().Should().Equal(1, 2, 3, 4, 5);
    }

    [Fact]
    public void SeriesConcat_ProducesSingleChunk()
    {
        var result = Series.Concat(Series.FromValues("a", new[] { 1.0, 2.0 }), Series.FromValues("a", new[] { 3.0 }));

        result.AsTyped<double>()!.ChunkCount.Should().Be(1);
        result.Sum().AsFloat64().Should().Be(6.0);
    }

    [Fact]
    public void SeriesAppend_KeepsNulls()
    {
        var result = Series.FromNullable("a", new int?[] { 1, null }).Append(Series.FromNullable("a", new int?[] { null, 4 }));

        result.Length.Should().Be(4);
        result.NullCount.Should().Be(2);
        result[3].AsInt32().Should().Be(4);
    }

    [Fact]
    public void SeriesConcat_Strings()
    {
        var result = Series.Concat(
            Series.FromValues("s", new string?[] { "x", null }),
            Series.FromValues("s", new string?[] { "y" }));

        result.DataType.Should().Be(DataType.String);
        result[0].AsString().Should().Be("x");
        result[1].IsNull.Should().BeTrue();
        result[2].AsString().Should().Be("y");
    }

    [Fact]
    public void SeriesConcat_PreservesSmallIntegerAndFloat32Types()
    {
        Series.Concat(Series.FromValues("i", new short[] { 1 }), Series.FromValues("i", new short[] { -2 }))
            .DataType.Should().Be(DataType.Int16);
        Series.Concat(Series.FromValues("u", new uint[] { 1 }), Series.FromValues("u", new uint[] { 2 }))
            .ToArray<uint>().Should().Equal(1u, 2u);
        Series.Concat(Series.FromValues("f", new[] { 1.5f }), Series.FromValues("f", new[] { 2.5f }))
            .DataType.Should().Be(DataType.Float32);
    }

    [Fact]
    public void SeriesConcat_DateTimeKeepsTimeUnit()
    {
        var ms = Series.FromValues("s", new[] { "2024-01-01 00:00:00.001" })
            .Str.ToDatetime(timeUnit: TimeUnit.Milliseconds);

        var result = Series.Concat(ms, ms);

        result.DataType.Should().Be(DataType.DateTime(TimeUnit.Milliseconds));
        result.AsTyped<long>()!.GetValue(1).Should().Be(1_704_067_200_001L);
    }

    [Fact]
    public void SeriesConcat_MismatchedTypes_Throws()
    {
        var act = () => Series.Concat(Series.FromValues("a", new[] { 1 }), Series.FromValues("b", new[] { 1L }));

        act.Should().Throw<ArgumentException>().WithMessage("*'b'*Int64*Int32*");
    }

    [Fact]
    public void SeriesConcat_MismatchedTimeUnits_Throws()
    {
        var ns = Series.FromValues("t", new[] { new DateTime(2024, 1, 1) });
        var ms = Series.FromValues("t", new[] { "2024-01-01" }).Str.ToDatetime(timeUnit: TimeUnit.Milliseconds);

        var act = () => Series.Concat(ns, ms);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SeriesConcat_NoInputs_Throws()
    {
        var act = () => Series.Concat();

        act.Should().Throw<ArgumentException>();
    }

    // ============================================================================
    // Series.NUnique
    // ============================================================================

    [Fact]
    public void NUnique_CountsDistinctValues()
    {
        Series.FromValues("a", new[] { 3, 1, 3, 2, 1 }).NUnique().Should().Be(3);
        Series.FromValues("s", new[] { "x", "y", "x" }).NUnique().Should().Be(2);
    }

    [Fact]
    public void NUnique_CountsNullOnce()
    {
        Series.FromNullable("a", new int?[] { 1, null, null, 1 }).NUnique().Should().Be(2);
    }

    [Fact]
    public void NUnique_EmptySeries_IsZero()
    {
        Series.FromValues("a", Array.Empty<int>()).NUnique().Should().Be(0);
    }

    // ============================================================================
    // DataFrame.VStack / HStack
    // ============================================================================

    [Fact]
    public void VStack_AppendsRowsAndKeepsTypes()
    {
        var top = new DataFrame(
            Series.FromValues("id", new[] { 1 }),
            Series.FromValues("f", new[] { 1.5f }),
            Series.FromValues("d", new[] { new DateOnly(2024, 1, 1) }));
        var bottom = new DataFrame(
            Series.FromValues("id", new[] { 2 }),
            Series.FromValues("f", new[] { 2.5f }),
            Series.FromValues("d", new[] { new DateOnly(2024, 1, 2) }));

        var result = top.VStack(bottom);

        result.Height.Should().Be(2);
        result.Dtypes.Should().Equal(DataType.Int32, DataType.Float32, DataType.Date);
        result["f"].ToArray<float>().Should().Equal(1.5f, 2.5f);
        result["d"][1].AsDate().Should().Be(new DateOnly(2024, 1, 2));
    }

    [Fact]
    public void VStack_DifferentColumnNames_Throws()
    {
        var a = new DataFrame(Series.FromValues("x", new[] { 1 }));
        var b = new DataFrame(Series.FromValues("y", new[] { 1 }));

        var act = () => a.VStack(b);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void VStack_DifferentColumnTypes_Throws()
    {
        var a = new DataFrame(Series.FromValues("x", new[] { 1 }));
        var b = new DataFrame(Series.FromValues("x", new[] { 1.0 }));

        var act = () => a.VStack(b);

        act.Should().Throw<ArgumentException>().WithMessage("*Float64*Int32*");
    }

    [Fact]
    public void HStack_AddsSeriesOnTheRight()
    {
        var df = new DataFrame(Series.FromValues("a", new[] { 1, 2 }));

        var result = df.HStack(Series.FromValues("b", new[] { "x", "y" }), Series.FromValues("c", new[] { true, false }));

        result.Columns.Should().Equal("a", "b", "c");
        result["b"][1].AsString().Should().Be("y");
        df.Width.Should().Be(1);
    }

    [Fact]
    public void HStack_DataFrame()
    {
        var left = new DataFrame(Series.FromValues("a", new[] { 1, 2 }));
        var right = new DataFrame(Series.FromValues("b", new[] { 3, 4 }));

        left.HStack(right).Columns.Should().Equal("a", "b");
    }

    [Fact]
    public void HStack_LengthMismatch_Throws()
    {
        var df = new DataFrame(Series.FromValues("a", new[] { 1, 2 }));

        var act = () => df.HStack(Series.FromValues("b", new[] { 1 }));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void HStack_DuplicateName_Throws()
    {
        var df = new DataFrame(Series.FromValues("a", new[] { 1, 2 }));

        var act = () => df.HStack(Series.FromValues("a", new[] { 3, 4 }));

        act.Should().Throw<ArgumentException>().WithMessage("*Duplicate*");
    }
}
