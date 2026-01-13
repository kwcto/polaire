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

    // ============================================================================
    // Additional Tests - Edge Cases
    // ============================================================================

    [Fact]
    public void DataFrame_Empty_ShouldWork()
    {
        var df = DataFrame.Empty(
            ("a", DataType.Int32),
            ("b", DataType.String)
        );

        df.Height.Should().Be(0);
        df.Width.Should().Be(2);
        df.Columns.Should().BeEquivalentTo(new[] { "a", "b" });
    }

    [Fact]
    public void DataFrame_EmptyNoColumns_ShouldWork()
    {
        var df = new DataFrame();

        df.Height.Should().Be(0);
        df.Width.Should().Be(0);
    }

    [Fact]
    public void DataFrame_SingleRow_ShouldWork()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 42 }),
            Series.FromValues("b", new[] { "single" })
        );

        df.Height.Should().Be(1);
        df["a"][0].AsInt32().Should().Be(42);
    }

    [Fact]
    public void DataFrame_SingleColumn_ShouldWork()
    {
        var df = new DataFrame(
            Series.FromValues("only_col", new[] { 1, 2, 3, 4, 5 })
        );

        df.Width.Should().Be(1);
        df.Height.Should().Be(5);
    }

    // ============================================================================
    // Properties Tests
    // ============================================================================

    [Fact]
    public void DataFrame_Dtypes_ShouldReturnCorrectTypes()
    {
        var df = new DataFrame(
            Series.FromValues("int_col", new[] { 1, 2, 3 }),
            Series.FromValues("float_col", new[] { 1.0, 2.0, 3.0 }),
            Series.FromValues("str_col", new[] { "a", "b", "c" })
        );

        df.Dtypes.Should().HaveCount(3);
        df.Dtypes[0].Should().Be(DataType.Int32);
        df.Dtypes[1].Should().Be(DataType.Float64);
        df.Dtypes[2].Should().Be(DataType.String);
    }

    [Fact]
    public void DataFrame_Schema_ShouldReturnNameAndType()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2 }),
            Series.FromValues("b", new[] { "x", "y" })
        );

        var schema = df.Schema;
        schema.Should().HaveCount(2);
        schema[0].Should().Be(("a", DataType.Int32));
        schema[1].Should().Be(("b", DataType.String));
    }

    // ============================================================================
    // Selection Tests
    // ============================================================================

    [Fact]
    public void DataFrame_SelectIf_ShouldFilterColumns()
    {
        var df = new DataFrame(
            Series.FromValues("int1", new[] { 1, 2, 3 }),
            Series.FromValues("str1", new[] { "a", "b", "c" }),
            Series.FromValues("int2", new[] { 4, 5, 6 })
        );

        // Select only numeric columns
        var numeric = df.SelectIf(s => s.DataType == DataType.Int32);

        numeric.Width.Should().Be(2);
        numeric.Columns.Should().BeEquivalentTo(new[] { "int1", "int2" });
    }

    [Fact]
    public void DataFrame_MultipleColumnSelect_ShouldWork()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1 }),
            Series.FromValues("b", new[] { 2 }),
            Series.FromValues("c", new[] { 3 }),
            Series.FromValues("d", new[] { 4 })
        );

        var selected = df["a", "c", "d"];

        selected.Width.Should().Be(3);
        selected.Columns.Should().BeEquivalentTo(new[] { "a", "c", "d" });
    }

    // ============================================================================
    // Slice Tests
    // ============================================================================

    [Fact]
    public void DataFrame_Slice_ShouldReturnSubset()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 })
        );

        var sliced = df.Slice(2, 5);

        sliced.Height.Should().Be(5);
        sliced["a"][0].AsInt32().Should().Be(3);
        sliced["a"][4].AsInt32().Should().Be(7);
    }

    [Fact]
    public void DataFrame_Slice_WithinBounds_ShouldWork()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 })
        );

        var sliced = df.Slice(1, 2);

        sliced.Height.Should().Be(2);
        sliced["a"][0].AsInt32().Should().Be(2);
        sliced["a"][1].AsInt32().Should().Be(3);
    }

    // ============================================================================
    // Join Tests - Additional
    // ============================================================================

    [Fact]
    public void DataFrame_OuterJoin_ShouldPreserveAllRows()
    {
        var left = new DataFrame(
            Series.FromValues("key", new[] { 1, 2, 3 }),
            Series.FromValues("left_val", new[] { "a", "b", "c" })
        );

        var right = new DataFrame(
            Series.FromValues("key", new[] { 2, 3, 4, 5 }),
            Series.FromValues("right_val", new[] { "x", "y", "z", "w" })
        );

        var joined = left.OuterJoin(right, "key");

        // Should have keys 1, 2, 3, 4, 5
        joined.Height.Should().Be(5);
    }

    [Fact]
    public void DataFrame_Join_NoMatches_ShouldReturnEmpty()
    {
        var left = new DataFrame(
            Series.FromValues("key", new[] { 1, 2, 3 }),
            Series.FromValues("val", new[] { "a", "b", "c" })
        );

        var right = new DataFrame(
            Series.FromValues("key", new[] { 4, 5, 6 }),
            Series.FromValues("val2", new[] { "x", "y", "z" })
        );

        var joined = left.Join(right, "key");

        joined.Height.Should().Be(0);
    }

    // ============================================================================
    // Sorting Tests - Additional
    // ============================================================================

    [Fact]
    public void DataFrame_Sort_MultipleColumns_ShouldWork()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 1, 2, 2 }),
            Series.FromValues("b", new[] { 20, 10, 40, 30 })
        );

        var sorted = df.Sort(("a", false), ("b", false));

        // Should sort by 'a' first, then by 'b' within same 'a' values
        sorted["a"][0].AsInt32().Should().Be(1);
        sorted["b"][0].AsInt32().Should().Be(10);  // 1 with smallest b
        sorted["a"][2].AsInt32().Should().Be(2);
        sorted["b"][2].AsInt32().Should().Be(30);  // 2 with smallest b
    }

    // ============================================================================
    // GroupBy Tests - Additional
    // ============================================================================

    [Fact]
    public void DataFrame_GroupBy_MultipleColumns_ShouldWork()
    {
        var df = new DataFrame(
            Series.FromValues("g1", new[] { "a", "a", "b", "b" }),
            Series.FromValues("g2", new[] { "x", "y", "x", "y" }),
            Series.FromValues("val", new[] { 1, 2, 3, 4 })
        );

        var grouped = df.GroupBy("g1", "g2").Sum();

        grouped.Height.Should().Be(4);  // 4 unique combinations
    }

    [Fact]
    public void DataFrame_GroupBy_Min_Max_ShouldWork()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "a", "a", "b", "b" }),
            Series.FromValues("value", new[] { 10, 20, 5, 15 })
        );

        var minResult = df.GroupBy("group").Min();
        var maxResult = df.GroupBy("group").Max();

        minResult.Height.Should().Be(2);
        maxResult.Height.Should().Be(2);
    }

    // ============================================================================
    // Describe/Statistics Tests
    // ============================================================================

    [Fact]
    public void DataFrame_Describe_ShouldReturnStatistics()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 })
        );

        var desc = df.Describe();

        desc.Height.Should().BeGreaterThan(0);
        // Should contain rows for count, mean, std, min, max, etc.
    }

    [Fact]
    public void DataFrame_NullCount_ShouldCountNulls()
    {
        var df = new DataFrame(
            Series.FromNullable("a", new int?[] { 1, null, 3, null, 5 }),
            Series.FromNullable("b", new int?[] { null, null, null, 4, 5 })
        );

        var nullCounts = df.NullCount();

        // NullCount returns a row per column
        nullCounts.Height.Should().BeGreaterThan(0);
    }

    [Fact]
    public void DataFrame_NUnique_ShouldCountDistinct()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 1, 2, 2, 3 }),
            Series.FromValues("b", new[] { "x", "x", "x", "y", "y" })
        );

        var nunique = df.NUnique();

        // NUnique returns counts per column
        nunique.Height.Should().BeGreaterThan(0);
    }

    // ============================================================================
    // Unique/Dedup Tests
    // ============================================================================

    [Fact]
    public void DataFrame_Unique_SubsetColumns_ShouldWork()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 1, 2, 2 }),
            Series.FromValues("b", new[] { "x", "y", "x", "y" }),
            Series.FromValues("c", new[] { 10, 20, 30, 40 })
        );

        // Unique based only on column "a"
        var unique = df.Unique("a");

        unique.Height.Should().Be(2);  // Only 2 unique values in 'a'
    }

    // ============================================================================
    // Sample Tests
    // ============================================================================

    [Fact]
    public void DataFrame_Sample_Fraction_ShouldWork()
    {
        var df = new DataFrame(
            Series.FromValues("a", Enumerable.Range(0, 100).ToArray())
        );

        var sample = df.Sample(0.1, seed: 42);

        sample.Height.Should().BeInRange(5, 15);  // Around 10% of 100
    }

    [Fact]
    public void DataFrame_Sample_Reproducible_ShouldWork()
    {
        var df = new DataFrame(
            Series.FromValues("a", Enumerable.Range(0, 50).ToArray())
        );

        var sample1 = df.Sample(10, seed: 123);
        var sample2 = df.Sample(10, seed: 123);

        // Same seed should produce same sample
        sample1["a"][0].AsInt32().Should().Be(sample2["a"][0].AsInt32());
    }

    // ============================================================================
    // Melt/Pivot Tests
    // ============================================================================

    [Fact]
    public void DataFrame_Melt_ShouldUnpivot()
    {
        var df = new DataFrame(
            Series.FromValues("id", new[] { 1, 2 }),
            Series.FromValues("A", new[] { 10, 20 }),
            Series.FromValues("B", new[] { 30, 40 })
        );

        var melted = df.Melt(
            idVars: new[] { "id" },
            valueVars: new[] { "A", "B" }
        );

        // Should have 4 rows (2 ids × 2 value columns)
        melted.Height.Should().Be(4);
        melted.Columns.Should().Contain("variable");
        melted.Columns.Should().Contain("value");
    }

    // ============================================================================
    // WithColumn Tests - Additional
    // ============================================================================

    [Fact]
    public void DataFrame_WithColumn_Expression_ShouldWork()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 })
        );

        var result = df.WithColumn("b", d => d["a"] * 2);

        result.Width.Should().Be(2);
        result["b"][0].AsInt32().Should().Be(2);
        result["b"][2].AsInt32().Should().Be(6);
    }

    [Fact]
    public void DataFrame_WithColumns_Multiple_ShouldWork()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 })
        );

        var result = df.WithColumns(
            Series.FromValues("b", new[] { 4, 5, 6 }),
            Series.FromValues("c", new[] { 7, 8, 9 })
        );

        result.Width.Should().Be(3);
    }

    // ============================================================================
    // Concatenation Tests - Additional
    // ============================================================================

    [Fact]
    public void DataFrame_VConcat_MismatchedColumns_ShouldThrow()
    {
        var df1 = new DataFrame(
            Series.FromValues("a", new[] { 1, 2 })
        );

        var df2 = new DataFrame(
            Series.FromValues("a", new[] { 3, 4 }),
            Series.FromValues("b", new[] { 5, 6 })
        );

        // VConcat requires matching schemas
        var act = () => DataFrame.VConcat(df1, df2);
        act.Should().Throw<ArgumentException>().WithMessage("*same schema*");
    }

    [Fact]
    public void DataFrame_VConcat_Multiple_ShouldWork()
    {
        var df1 = new DataFrame(Series.FromValues("a", new[] { 1 }));
        var df2 = new DataFrame(Series.FromValues("a", new[] { 2 }));
        var df3 = new DataFrame(Series.FromValues("a", new[] { 3 }));

        var concat = DataFrame.VConcat(df1, df2, df3);

        concat.Height.Should().Be(3);
        concat["a"][2].AsInt32().Should().Be(3);
    }

    // ============================================================================
    // Filter Tests - Additional
    // ============================================================================

    [Fact]
    public void DataFrame_Filter_NoMatches_ShouldReturnEmpty()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 })
        );

        var mask = df["a"].Gt(AnyValue.From(100));
        var filtered = df.Filter(mask);

        filtered.Height.Should().Be(0);
        filtered.Width.Should().Be(1);  // Still has the column
    }

    [Fact]
    public void DataFrame_Filter_AllMatch_ShouldReturnAll()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 10, 20, 30 })
        );

        var mask = df["a"].Gt(AnyValue.From(0));
        var filtered = df.Filter(mask);

        filtered.Height.Should().Be(3);
    }

    // ============================================================================
    // Iterator Tests
    // ============================================================================

    [Fact]
    public void DataFrame_Iteration_ShouldIterateColumns()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2 }),
            Series.FromValues("b", new[] { 3, 4 }),
            Series.FromValues("c", new[] { 5, 6 })
        );

        var columnNames = new List<string>();
        foreach (var series in df)
        {
            columnNames.Add(series.Name);
        }

        columnNames.Should().BeEquivalentTo(new[] { "a", "b", "c" });
    }

    // ============================================================================
    // Lazy Conversion Tests
    // ============================================================================

    [Fact]
    public void DataFrame_Lazy_ShouldReturnLazyFrame()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 })
        );

        var lazy = df.Lazy();

        lazy.Should().NotBeNull();
    }

    // ============================================================================
    // Error Handling Tests
    // ============================================================================

    [Fact]
    public void DataFrame_DuplicateColumnName_ShouldThrow()
    {
        var act = () => new DataFrame(
            Series.FromValues("a", new[] { 1 }),
            Series.FromValues("a", new[] { 2 })  // Duplicate!
        );

        act.Should().Throw<ArgumentException>().WithMessage("*Duplicate*");
    }

    [Fact]
    public void DataFrame_MismatchedLengths_ShouldThrow()
    {
        var act = () => new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 }),
            Series.FromValues("b", new[] { 1, 2 })  // Different length!
        );

        act.Should().Throw<ArgumentException>().WithMessage("*same length*");
    }

    [Fact]
    public void DataFrame_InvalidColumnAccess_ShouldThrow()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 })
        );

        var act = () => df["nonexistent"];

        act.Should().Throw<KeyNotFoundException>();
    }

    // ============================================================================
    // FromDictionary Tests
    // ============================================================================

    [Fact]
    public void DataFrame_FromDictionary_ShouldWork()
    {
        var data = new Dictionary<string, object[]>
        {
            ["integers"] = new object[] { 1, 2, 3 },
            ["strings"] = new object[] { "a", "b", "c" }
        };

        var df = DataFrame.FromDictionary(data);

        df.Height.Should().Be(3);
        df.Width.Should().Be(2);
    }
}
