// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using FluentAssertions;
using Polaire.DataTypes;
using Polaire.Expressions;
using Xunit;
using static Polaire.Expressions.Expr;

namespace Polaire.Tests;

/// <summary>
/// Tests for SliceExpr, the horizontal (row-wise) functions, LazyFrame.Limit and projection
/// pushdown through slice and window expressions.
/// </summary>
public class ExpressionAdditionsTests
{
    private static DataFrame Numbers() => new(
        Series.FromValues("a", new[] { 1, 2, 3, 4, 5 }),
        Series.FromValues("b", new[] { 10, 20, 30, 40, 50 }));

    // ============================================================================
    // SliceExpr
    // ============================================================================

    [Theory]
    [InlineData(1, 2, new[] { 2, 3 })]
    [InlineData(-2, null, new[] { 4, 5 })]
    [InlineData(3, null, new[] { 4, 5 })]
    [InlineData(3, 10, new[] { 4, 5 })]
    [InlineData(-10, 7, new[] { 1, 2 })]
    [InlineData(-10, 3, new int[0])]
    [InlineData(7, 2, new int[0])]
    [InlineData(0, 0, new int[0])]
    public void SliceExpr_FollowsPolarsOffsets(int offset, int? length, int[] expected)
    {
        var result = Numbers().Lazy().Select(Col("a").SliceExpr(offset, length)).Collect();

        result["a"].ToArray<int>().Should().Equal(expected);
    }

    [Fact]
    public void SliceExpr_OnSeveralColumns()
    {
        var result = Numbers().Lazy().Select(Col("a").SliceExpr(-1), Col("b").SliceExpr(-1).As("last_b")).Collect();

        result.Shape.Should().Be((1, 2));
        result["a"][0].AsInt32().Should().Be(5);
        result["last_b"][0].AsInt32().Should().Be(50);
    }

    [Fact]
    public void SliceExpr_KeepsColumnThroughProjectionPushdown()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2 }),
            Series.FromValues("b", new[] { 3, 4 }),
            Series.FromValues("c", new[] { 5, 6 }));

        var result = df.Lazy().Select(Col("b").SliceExpr(1)).Collect();

        result["b"].ToArray<int>().Should().Equal(4);
    }

    [Fact]
    public void WindowInSelect_KeepsPartitionColumnThroughProjectionPushdown()
    {
        var df = new DataFrame(
            Series.FromValues("g", new[] { "a", "a", "b" }),
            Series.FromValues("v", new[] { 1.0, 2.0, 5.0 }),
            Series.FromValues("unused", new[] { 0, 0, 0 }));

        var result = df.Lazy().Select(Col("v").Sum().Over(Col("g")).As("s")).Collect();

        result["s"].ToArray<double>().Should().Equal(3.0, 3.0, 5.0);
    }

    // ============================================================================
    // LazyFrame.Limit
    // ============================================================================

    [Fact]
    public void Limit_ReturnsFirstRows()
    {
        var result = Numbers().Lazy().Limit(2).Collect();

        result["a"].ToArray<int>().Should().Equal(1, 2);
    }

    [Fact]
    public void Limit_LargerThanFrame_ReturnsAllRows()
    {
        Numbers().Lazy().Limit(100).Collect().Height.Should().Be(5);
    }

    // ============================================================================
    // Horizontal functions
    // ============================================================================

    private static DataFrame WithNulls() => new(
        Series.FromNullable("x", new int?[] { 1, null, null, 4 }),
        Series.FromNullable("y", new int?[] { 10, 20, null, null }),
        Series.FromNullable("z", new double?[] { 0.5, 1.5, null, double.NaN }));

    [Fact]
    public void SumHorizontal_Integers_IgnoresNullsAndReturnsInt64()
    {
        var result = WithNulls().Lazy().Select(Pl.SumHorizontal("x", "y").As("sum")).Collect();

        result["sum"].DataType.Should().Be(DataType.Int64);
        result["sum"].ToArray<long>().Should().Equal(11L, 20L, 0L, 4L);
    }

    [Fact]
    public void SumHorizontal_WithFloats_ReturnsFloat64()
    {
        var result = WithNulls().Lazy().Select(SumHorizontal(Col("x"), Col("z")).As("sum")).Collect();

        result["sum"].DataType.Should().Be(DataType.Float64);
        result["sum"][0].AsFloat64().Should().Be(1.5);
        result["sum"][1].AsFloat64().Should().Be(1.5);
        result["sum"][2].AsFloat64().Should().Be(0.0);
        double.IsNaN(result["sum"][3].AsFloat64()).Should().BeTrue();
    }

    [Fact]
    public void SumHorizontal_BroadcastsLiterals()
    {
        var result = Numbers().Lazy().Select(SumHorizontal(Col("a"), Lit(100))).Collect();

        result["a"].ToArray<long>().Should().Equal(101L, 102L, 103L, 104L, 105L);
    }

    [Fact]
    public void MeanHorizontal_IgnoresNulls_AllNullRowIsNull()
    {
        var result = WithNulls().Lazy().Select(Pl.MeanHorizontal("x", "y").As("mean")).Collect();

        result["mean"].DataType.Should().Be(DataType.Float64);
        result["mean"][0].AsFloat64().Should().Be(5.5);
        result["mean"][1].AsFloat64().Should().Be(20.0);
        result["mean"][2].IsNull.Should().BeTrue();
        result["mean"][3].AsFloat64().Should().Be(4.0);
    }

    [Fact]
    public void MinMaxHorizontal_Integers()
    {
        var result = WithNulls().Lazy()
            .Select(Pl.MinHorizontal("x", "y").As("min"), Pl.MaxHorizontal("x", "y").As("max"))
            .Collect();

        result["min"].DataType.Should().Be(DataType.Int64);
        result["min"][0].AsInt64().Should().Be(1);
        result["min"][1].AsInt64().Should().Be(20);
        result["min"][2].IsNull.Should().BeTrue();
        result["max"][0].AsInt64().Should().Be(10);
        result["max"][3].AsInt64().Should().Be(4);
    }

    [Fact]
    public void MinMaxHorizontal_SkipNaN()
    {
        var result = WithNulls().Lazy()
            .Select(MinHorizontal(Col("x"), Col("z")).As("min"), MaxHorizontal(Col("y"), Col("z")).As("max"))
            .Collect();

        result["min"][0].AsFloat64().Should().Be(0.5);
        result["min"][3].AsFloat64().Should().Be(4.0);
        result["max"][1].AsFloat64().Should().Be(20.0);
        result["max"][3].IsNull.Should().BeTrue();
    }

    [Fact]
    public void Horizontal_MixedIntegerTypes_ReturnInt64()
    {
        var df = new DataFrame(
            Series.FromValues("i8", new sbyte[] { -3 }),
            Series.FromValues("u32", new uint[] { 7 }),
            Series.FromValues("i64", new[] { 10L }));

        var result = ExprEvaluator.Evaluate(Pl.SumHorizontal("i8", "u32", "i64"), df);

        result.DataType.Should().Be(DataType.Int64);
        result[0].AsInt64().Should().Be(14);
    }

    [Fact]
    public void Horizontal_InWithColumns()
    {
        var result = Numbers().Lazy().WithColumns(Pl.SumHorizontal("a", "b").As("total")).Collect();

        result.Columns.Should().Equal("a", "b", "total");
        result["total"].ToArray<long>().Should().Equal(11L, 22L, 33L, 44L, 55L);
    }

    [Fact]
    public void Horizontal_NonNumericInput_Throws()
    {
        var df = new DataFrame(Series.FromValues("a", new[] { 1 }), Series.FromValues("s", new[] { "x" }));

        var act = () => ExprEvaluator.Evaluate(Pl.SumHorizontal("a", "s"), df);

        act.Should().Throw<ArgumentException>().WithMessage("*numeric*'s'*");
    }
}
