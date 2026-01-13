// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using FluentAssertions;
using Polaire.DataTypes;
using Polaire.Series;

namespace Polaire.Tests;

public class SeriesTests
{
    [Fact]
    public void Series_FromIntArray_ShouldCreateCorrectly()
    {
        var series = Series.FromValues("numbers", new[] { 1, 2, 3, 4, 5 });

        series.Name.Should().Be("numbers");
        series.Length.Should().Be(5);
        series.DataType.Should().Be(DataType.Int32);
        series[0].AsInt32().Should().Be(1);
        series[4].AsInt32().Should().Be(5);
    }

    [Fact]
    public void Series_FromDoubleArray_ShouldCreateCorrectly()
    {
        var series = Series.FromValues("floats", new[] { 1.1, 2.2, 3.3 });

        series.Name.Should().Be("floats");
        series.Length.Should().Be(3);
        series.DataType.Should().Be(DataType.Float64);
        series[0].AsFloat64().Should().BeApproximately(1.1, 0.001);
    }

    [Fact]
    public void Series_FromStringArray_ShouldCreateCorrectly()
    {
        var series = Series.FromValues("strings", new[] { "a", "b", "c" });

        series.Name.Should().Be("strings");
        series.Length.Should().Be(3);
        series.DataType.Should().Be(DataType.String);
        series[0].AsString().Should().Be("a");
    }

    [Fact]
    public void Series_WithNulls_ShouldHandleCorrectly()
    {
        var series = Series.FromNullable("nullable", new int?[] { 1, null, 3, null, 5 });

        series.Length.Should().Be(5);
        series.NullCount.Should().Be(2);
        series.HasNulls.Should().BeTrue();
        series.IsNull(1).Should().BeTrue();
        series.IsNull(2).Should().BeFalse();
    }

    [Fact]
    public void Series_Sum_ShouldCalculateCorrectly()
    {
        var series = Series.FromValues("nums", new[] { 1, 2, 3, 4, 5 });
        var sum = series.Sum();

        sum.AsInt64().Should().Be(15);
    }

    [Fact]
    public void Series_Mean_ShouldCalculateCorrectly()
    {
        var series = Series.FromValues("nums", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 });
        var mean = series.Mean();

        mean.AsFloat64().Should().BeApproximately(3.0, 0.001);
    }

    [Fact]
    public void Series_Min_ShouldFindMinimum()
    {
        var series = Series.FromValues("nums", new[] { 5, 2, 8, 1, 9 });
        var min = series.Min();

        min.AsInt32().Should().Be(1);
    }

    [Fact]
    public void Series_Max_ShouldFindMaximum()
    {
        var series = Series.FromValues("nums", new[] { 5, 2, 8, 1, 9 });
        var max = series.Max();

        max.AsInt32().Should().Be(9);
    }

    [Fact]
    public void Series_Arithmetic_Addition_ShouldWork()
    {
        var a = Series.FromValues("a", new[] { 1.0, 2.0, 3.0 });
        var b = Series.FromValues("b", new[] { 10.0, 20.0, 30.0 });

        var result = a + b;

        result[0].AsFloat64().Should().Be(11.0);
        result[1].AsFloat64().Should().Be(22.0);
        result[2].AsFloat64().Should().Be(33.0);
    }

    [Fact]
    public void Series_Arithmetic_ScalarMultiplication_ShouldWork()
    {
        var series = Series.FromValues("nums", new[] { 1.0, 2.0, 3.0 });
        var result = series * 10.0;

        result[0].AsFloat64().Should().Be(10.0);
        result[1].AsFloat64().Should().Be(20.0);
        result[2].AsFloat64().Should().Be(30.0);
    }

    [Fact]
    public void Series_Comparison_Equal_ShouldWork()
    {
        var series = Series.FromValues("nums", new[] { 1, 2, 3, 2, 1 });
        var result = series.Eq(AnyValue.From(2));

        result.DataType.Should().Be(DataType.Boolean);
        result[0].AsBoolean().Should().BeFalse();
        result[1].AsBoolean().Should().BeTrue();
        result[2].AsBoolean().Should().BeFalse();
        result[3].AsBoolean().Should().BeTrue();
    }

    [Fact]
    public void Series_Comparison_GreaterThan_ShouldWork()
    {
        var series = Series.FromValues("nums", new[] { 1, 2, 3, 4, 5 });
        var result = series.Gt(AnyValue.From(3));

        result[2].AsBoolean().Should().BeFalse(); // 3 > 3 is false
        result[3].AsBoolean().Should().BeTrue();  // 4 > 3 is true
        result[4].AsBoolean().Should().BeTrue();  // 5 > 3 is true
    }

    [Fact]
    public void Series_Boolean_And_ShouldWork()
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
    public void Series_Sort_ShouldOrderCorrectly()
    {
        var series = Series.FromValues("nums", new[] { 3, 1, 4, 1, 5, 9, 2, 6 });
        var sorted = series.Sort();

        sorted[0].AsInt32().Should().Be(1);
        sorted[1].AsInt32().Should().Be(1);
        sorted[2].AsInt32().Should().Be(2);
        sorted[7].AsInt32().Should().Be(9);
    }

    [Fact]
    public void Series_Sort_Descending_ShouldOrderCorrectly()
    {
        var series = Series.FromValues("nums", new[] { 3, 1, 4, 1, 5 });
        var sorted = series.Sort(descending: true);

        sorted[0].AsInt32().Should().Be(5);
        sorted[1].AsInt32().Should().Be(4);
    }

    [Fact]
    public void Series_Unique_ShouldRemoveDuplicates()
    {
        var series = Series.FromValues("nums", new[] { 1, 2, 2, 3, 3, 3, 4 });
        var unique = series.Unique();

        unique.Length.Should().Be(4);
    }

    [Fact]
    public void Series_DropNulls_ShouldRemoveNulls()
    {
        var series = Series.FromNullable("nums", new int?[] { 1, null, 2, null, 3 });
        var dropped = series.DropNulls();

        dropped.Length.Should().Be(3);
        dropped.NullCount.Should().Be(0);
    }

    [Fact]
    public void Series_FillNull_ShouldReplaceNulls()
    {
        var series = Series.FromNullable("nums", new int?[] { 1, null, 3, null, 5 });
        var filled = series.FillNull(AnyValue.From(0));

        filled.NullCount.Should().Be(0);
        filled[1].AsInt32().Should().Be(0);
        filled[3].AsInt32().Should().Be(0);
    }

    [Fact]
    public void Series_Rename_ShouldChangeName()
    {
        var series = Series.FromValues("old_name", new[] { 1, 2, 3 });
        var renamed = series.Rename("new_name");

        renamed.Name.Should().Be("new_name");
        renamed.Length.Should().Be(3);
    }

    [Fact]
    public void Series_Head_ShouldReturnFirstN()
    {
        var series = Series.FromValues("nums", new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
        var head = series.Head(3);

        head.Length.Should().Be(3);
        head[0].AsInt32().Should().Be(1);
        head[2].AsInt32().Should().Be(3);
    }

    [Fact]
    public void Series_Tail_ShouldReturnLastN()
    {
        var series = Series.FromValues("nums", new[] { 1, 2, 3, 4, 5 });
        var tail = series.Tail(2);

        tail.Length.Should().Be(2);
        tail[0].AsInt32().Should().Be(4);
        tail[1].AsInt32().Should().Be(5);
    }

    [Fact]
    public void Series_IsIn_ShouldCheckMembership()
    {
        var series = Series.FromValues("nums", new[] { 1, 2, 3, 4, 5 });
        var result = series.IsIn(AnyValue.From(2), AnyValue.From(4));

        result[0].AsBoolean().Should().BeFalse(); // 1
        result[1].AsBoolean().Should().BeTrue();  // 2
        result[2].AsBoolean().Should().BeFalse(); // 3
        result[3].AsBoolean().Should().BeTrue();  // 4
    }

    [Fact]
    public void Series_Between_ShouldCheckRange()
    {
        var series = Series.FromValues("nums", new[] { 1, 2, 3, 4, 5 });
        var result = series.Between(AnyValue.From(2), AnyValue.From(4));

        result[0].AsBoolean().Should().BeFalse(); // 1 not in [2,4]
        result[1].AsBoolean().Should().BeTrue();  // 2 in [2,4]
        result[2].AsBoolean().Should().BeTrue();  // 3 in [2,4]
        result[3].AsBoolean().Should().BeTrue();  // 4 in [2,4]
        result[4].AsBoolean().Should().BeFalse(); // 5 not in [2,4]
    }

    [Fact]
    public void Series_All_ShouldCheckAllTrue()
    {
        var allTrue = Series.FromValues("a", new[] { true, true, true });
        var somefalse = Series.FromValues("b", new[] { true, false, true });

        allTrue.All().Should().BeTrue();
        somefalse.All().Should().BeFalse();
    }

    [Fact]
    public void Series_Any_ShouldCheckAnyTrue()
    {
        var allFalse = Series.FromValues("a", new[] { false, false, false });
        var someTrue = Series.FromValues("b", new[] { false, true, false });

        allFalse.Any().Should().BeFalse();
        someTrue.Any().Should().BeTrue();
    }

    [Fact]
    public void Series_Std_ShouldCalculateCorrectly()
    {
        var series = Series.FromValues("nums", new[] { 2.0, 4.0, 4.0, 4.0, 5.0, 5.0, 7.0, 9.0 });
        var std = series.Std();

        // Population std dev ≈ 2.0, sample std dev ≈ 2.138
        std.TryGetDouble(out var value).Should().BeTrue();
        value.Should().BeApproximately(2.138, 0.01);
    }

    [Fact]
    public void Series_Median_ShouldCalculateCorrectly()
    {
        var series = Series.FromValues("nums", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 });
        var median = series.Median();

        median.AsFloat64().Should().Be(3.0);
    }

    [Fact]
    public void Series_Cast_ShouldConvertTypes()
    {
        var intSeries = Series.FromValues("nums", new[] { 1, 2, 3 });
        var floatSeries = intSeries.Cast(DataType.Float64);

        floatSeries.DataType.Should().Be(DataType.Float64);
        floatSeries[0].AsFloat64().Should().Be(1.0);
    }
}
