using Xunit;
// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using FluentAssertions;
using Polaire.DataTypes;



namespace Polaire.Tests;

public class DataFrameTests
{
    [Fact]
    public void DataFrame_Creation_ShouldWork()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 }),
            Series.FromValues("b", new[] { 4.0, 5.0, 6.0 }),
            Series.FromValues("c", new[] { "x", "y", "z" })
        );

        df.Height.Should().Be(3);
        df.Width.Should().Be(3);
        df.Shape.Should().Be((3, 3));
        df.Columns.Should().BeEquivalentTo(new[] { "a", "b", "c" });
    }

    [Fact]
    public void DataFrame_ColumnAccess_ByName_ShouldWork()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 }),
            Series.FromValues("b", new[] { 4, 5, 6 })
        );

        df["a"].Name.Should().Be("a");
        df["a"][0].AsInt32().Should().Be(1);
        df["b"][2].AsInt32().Should().Be(6);
    }

    [Fact]
    public void DataFrame_ColumnAccess_ByIndex_ShouldWork()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 }),
            Series.FromValues("b", new[] { 4, 5, 6 })
        );

        df[0].Name.Should().Be("a");
        df[1].Name.Should().Be("b");
    }

    [Fact]
    public void DataFrame_Select_ShouldReturnSubset()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 }),
            Series.FromValues("b", new[] { 4, 5, 6 }),
            Series.FromValues("c", new[] { 7, 8, 9 })
        );

        var selected = df.Select("a", "c");

        selected.Width.Should().Be(2);
        selected.Columns.Should().BeEquivalentTo(new[] { "a", "c" });
    }

    [Fact]
    public void DataFrame_Drop_ShouldRemoveColumns()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 }),
            Series.FromValues("b", new[] { 4, 5, 6 }),
            Series.FromValues("c", new[] { 7, 8, 9 })
        );

        var dropped = df.Drop("b");

        dropped.Width.Should().Be(2);
        dropped.Columns.Should().BeEquivalentTo(new[] { "a", "c" });
    }

    [Fact]
    public void DataFrame_Head_ShouldReturnFirstNRows()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 })
        );

        var head = df.Head(3);

        head.Height.Should().Be(3);
        head["a"][0].AsInt32().Should().Be(1);
        head["a"][2].AsInt32().Should().Be(3);
    }

    [Fact]
    public void DataFrame_Tail_ShouldReturnLastNRows()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 })
        );

        var tail = df.Tail(2);

        tail.Height.Should().Be(2);
        tail["a"][0].AsInt32().Should().Be(4);
        tail["a"][1].AsInt32().Should().Be(5);
    }

    [Fact]
    public void DataFrame_Filter_ShouldFilterRows()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 }),
            Series.FromValues("b", new[] { 10, 20, 30, 40, 50 })
        );

        var mask = df["a"].Gt(AnyValue.From(2));
        var filtered = df.Filter(mask);

        filtered.Height.Should().Be(3);
        filtered["a"][0].AsInt32().Should().Be(3);
        filtered["b"][0].AsInt32().Should().Be(30);
    }

    [Fact]
    public void DataFrame_WithColumn_ShouldAddColumn()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 })
        );

        var newCol = Series.FromValues("b", new[] { 10, 20, 30 });
        var result = df.WithColumn(newCol);

        result.Width.Should().Be(2);
        result.Columns.Should().BeEquivalentTo(new[] { "a", "b" });
    }

    [Fact]
    public void DataFrame_WithColumn_ShouldReplaceExisting()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 }),
            Series.FromValues("b", new[] { 4, 5, 6 })
        );

        var newB = Series.FromValues("b", new[] { 40, 50, 60 });
        var result = df.WithColumn(newB);

        result.Width.Should().Be(2);
        result["b"][0].AsInt32().Should().Be(40);
    }

    [Fact]
    public void DataFrame_Rename_ShouldRenameColumns()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 }),
            Series.FromValues("b", new[] { 4, 5, 6 })
        );

        var renamed = df.Rename(new Dictionary<string, string> { { "a", "x" } });

        renamed.Columns.Should().BeEquivalentTo(new[] { "x", "b" });
    }

    [Fact]
    public void DataFrame_Sort_ShouldOrderRows()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 3, 1, 4, 1, 5 }),
            Series.FromValues("b", new[] { "c", "a", "d", "b", "e" })
        );

        var sorted = df.Sort("a");

        sorted["a"][0].AsInt32().Should().Be(1);
        sorted["a"][1].AsInt32().Should().Be(1);
        sorted["a"][4].AsInt32().Should().Be(5);
    }

    [Fact]
    public void DataFrame_Sort_Descending_ShouldWork()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 3, 1, 4, 1, 5 })
        );

        var sorted = df.Sort("a", descending: true);

        sorted["a"][0].AsInt32().Should().Be(5);
        sorted["a"][4].AsInt32().Should().Be(1);
    }

    [Fact]
    public void DataFrame_GroupBy_Count_ShouldWork()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "a", "a", "b", "b", "b" }),
            Series.FromValues("value", new[] { 1, 2, 3, 4, 5 })
        );

        var grouped = df.GroupBy("group").Count();

        grouped.Height.Should().Be(2);
    }

    [Fact]
    public void DataFrame_GroupBy_Sum_ShouldWork()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "a", "a", "b", "b" }),
            Series.FromValues("value", new[] { 1, 2, 3, 4 })
        );

        var grouped = df.GroupBy("group").Sum();

        grouped.Height.Should().Be(2);
        // Sum for "a" should be 3, sum for "b" should be 7
    }

    [Fact]
    public void DataFrame_GroupBy_Mean_ShouldWork()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "a", "a", "b", "b" }),
            Series.FromValues("value", new[] { 2.0, 4.0, 6.0, 8.0 })
        );

        var grouped = df.GroupBy("group").Mean();

        grouped.Height.Should().Be(2);
        // Mean for "a" should be 3.0, mean for "b" should be 7.0
    }

    [Fact]
    public void DataFrame_Join_Inner_ShouldWork()
    {
        var left = new DataFrame(
            Series.FromValues("key", new[] { 1, 2, 3 }),
            Series.FromValues("left_val", new[] { "a", "b", "c" })
        );

        var right = new DataFrame(
            Series.FromValues("key", new[] { 2, 3, 4 }),
            Series.FromValues("right_val", new[] { "x", "y", "z" })
        );

        var joined = left.Join(right, "key");

        joined.Height.Should().Be(2); // Only keys 2 and 3 match
        joined.Columns.Should().Contain("left_val");
        joined.Columns.Should().Contain("right_val");
    }

    [Fact]
    public void DataFrame_LeftJoin_ShouldPreserveLeftRows()
    {
        var left = new DataFrame(
            Series.FromValues("key", new[] { 1, 2, 3 }),
            Series.FromValues("left_val", new[] { "a", "b", "c" })
        );

        var right = new DataFrame(
            Series.FromValues("key", new[] { 2, 3, 4 }),
            Series.FromValues("right_val", new[] { "x", "y", "z" })
        );

        var joined = left.LeftJoin(right, "key");

        joined.Height.Should().Be(3); // All left rows preserved
    }

    [Fact]
    public void DataFrame_VConcat_ShouldStackVertically()
    {
        var df1 = new DataFrame(
            Series.FromValues("a", new[] { 1, 2 }),
            Series.FromValues("b", new[] { 3, 4 })
        );

        var df2 = new DataFrame(
            Series.FromValues("a", new[] { 5, 6 }),
            Series.FromValues("b", new[] { 7, 8 })
        );

        var concat = DataFrame.VConcat(df1, df2);

        concat.Height.Should().Be(4);
        concat["a"][2].AsInt32().Should().Be(5);
    }

    [Fact]
    public void DataFrame_HConcat_ShouldStackHorizontally()
    {
        var df1 = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 })
        );

        var df2 = new DataFrame(
            Series.FromValues("b", new[] { 4, 5, 6 })
        );

        var concat = DataFrame.HConcat(df1, df2);

        concat.Width.Should().Be(2);
        concat.Columns.Should().BeEquivalentTo(new[] { "a", "b" });
    }

    [Fact]
    public void DataFrame_Unique_ShouldRemoveDuplicateRows()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 1, 2, 2, 3 }),
            Series.FromValues("b", new[] { "x", "x", "y", "y", "z" })
        );

        var unique = df.Unique();

        unique.Height.Should().Be(3);
    }

    [Fact]
    public void DataFrame_Sample_ShouldReturnRandomSubset()
    {
        var df = new DataFrame(
            Series.FromValues("a", Enumerable.Range(0, 100).ToArray())
        );

        var sample = df.Sample(10, seed: 42);

        sample.Height.Should().Be(10);
    }

    [Fact]
    public void DataFrame_Take_ShouldSelectByIndices()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 10, 20, 30, 40, 50 })
        );

        var taken = df.Take(new[] { 0, 2, 4 });

        taken.Height.Should().Be(3);
        taken["a"][0].AsInt32().Should().Be(10);
        taken["a"][1].AsInt32().Should().Be(30);
        taken["a"][2].AsInt32().Should().Be(50);
    }

    [Fact]
    public void DataFrame_WithRowNumber_ShouldAddIndexColumn()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { "x", "y", "z" })
        );

        var withRowNum = df.WithRowNumber("idx");

        withRowNum.Width.Should().Be(2);
        withRowNum["idx"][0].AsInt32().Should().Be(0);
        withRowNum["idx"][2].AsInt32().Should().Be(2);
    }

    [Fact]
    public void DataFrame_ToString_ShouldFormatNicely()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 }),
            Series.FromValues("b", new[] { "x", "y", "z" })
        );

        var str = df.ToString();

        str.Should().Contain("Shape: (3, 2)");
        str.Should().Contain("a");
        str.Should().Contain("b");
    }
}
