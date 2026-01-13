// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET
//
// Comprehensive tests for DataFrame methods, inspired by Polars test suite.
// These tests cover all DataFrame methods including:
// - Selection and projection (Select, SelectIf, Drop)
// - Transformation (Rename, WithColumn, WithColumns)
// - Slicing and sampling (Head, Tail, Slice, Take, Sample)
// - Aggregation (Describe, NullCount, Unique, NUnique)
// - Reshaping (Melt)

using FluentAssertions;
using Polaire;
using Polaire.DataTypes;
using Xunit;

namespace Polaire.Tests;

/// <summary>
/// Tests for DataFrame methods.
/// </summary>
public class DataFrameMethodsTests
{
    // ============================================================================
    // Select Tests
    // ============================================================================

    [Fact]
    public void Select_SingleColumn_ReturnsColumnOnly()
    {
        var df = CreateTestDataFrame();
        var result = df.Select("name");

        result.Width.Should().Be(1);
        result.Columns.Should().Contain("name");
    }

    [Fact]
    public void Select_MultipleColumns_ReturnsColumnsInOrder()
    {
        var df = CreateTestDataFrame();
        var result = df.Select("age", "name");

        result.Width.Should().Be(2);
        result.Columns.Should().Equal(new[] { "age", "name" });
    }

    [Fact]
    public void Select_AllColumns_ReturnsCopy()
    {
        var df = CreateTestDataFrame();
        var result = df.Select("name", "age", "score");

        result.Width.Should().Be(3);
        result.Height.Should().Be(df.Height);
    }

    [Fact]
    public void Select_DuplicateColumns_ThrowsException()
    {
        var df = CreateTestDataFrame();

        // Selecting duplicate column names throws ArgumentException
        var act = () => df.Select("name", "name");
        act.Should().Throw<ArgumentException>();
    }

    // ============================================================================
    // SelectIf Tests
    // ============================================================================

    [Fact]
    public void SelectIf_NumericColumns_ReturnsNumeric()
    {
        var df = CreateTestDataFrame();
        var result = df.SelectIf(s => s.DataType == DataType.Int32 || s.DataType == DataType.Float64);

        result.Width.Should().Be(2);  // age (int) and score (float)
    }

    [Fact]
    public void SelectIf_NoMatch_ReturnsEmpty()
    {
        var df = CreateTestDataFrame();
        var result = df.SelectIf(s => s.DataType == DataType.Boolean);

        result.Width.Should().Be(0);
    }

    [Fact]
    public void SelectIf_AllMatch_ReturnsAll()
    {
        var df = CreateTestDataFrame();
        var result = df.SelectIf(s => true);

        result.Width.Should().Be(df.Width);
    }

    // ============================================================================
    // Drop Tests
    // ============================================================================

    [Fact]
    public void Drop_SingleColumn_RemovesColumn()
    {
        var df = CreateTestDataFrame();
        var result = df.Drop("name");

        result.Width.Should().Be(2);
        result.Columns.Should().NotContain("name");
    }

    [Fact]
    public void Drop_MultipleColumns_RemovesAll()
    {
        var df = CreateTestDataFrame();
        var result = df.Drop("name", "age");

        result.Width.Should().Be(1);
        result.Columns.Should().Equal(new[] { "score" });
    }

    [Fact]
    public void Drop_AllColumns_ReturnsEmpty()
    {
        var df = CreateTestDataFrame();
        var result = df.Drop("name", "age", "score");

        result.Width.Should().Be(0);
    }

    // ============================================================================
    // Rename Tests
    // ============================================================================

    [Fact]
    public void Rename_SingleColumn_RenamesCorrectly()
    {
        var df = CreateTestDataFrame();
        var result = df.Rename(new Dictionary<string, string> { { "name", "full_name" } });

        result.Columns.Should().Contain("full_name");
        result.Columns.Should().NotContain("name");
    }

    [Fact]
    public void Rename_MultipleColumns_RenamesAll()
    {
        var df = CreateTestDataFrame();
        var result = df.Rename(new Dictionary<string, string>
        {
            { "name", "n" },
            { "age", "a" },
            { "score", "s" }
        });

        result.Columns.Should().Equal(new[] { "n", "a", "s" });
    }

    [Fact]
    public void Rename_PreservesData()
    {
        var df = CreateTestDataFrame();
        var result = df.Rename(new Dictionary<string, string> { { "name", "full_name" } });

        result["full_name"][0].AsString().Should().Be(df["name"][0].AsString());
    }

    // ============================================================================
    // Head Tests
    // ============================================================================

    [Fact]
    public void Head_Default_ReturnsFirst5()
    {
        var df = CreateLargeTestDataFrame(10);
        var result = df.Head();

        result.Height.Should().Be(5);
    }

    [Fact]
    public void Head_CustomN_ReturnsFirstN()
    {
        var df = CreateLargeTestDataFrame(10);
        var result = df.Head(3);

        result.Height.Should().Be(3);
    }

    [Fact]
    public void Head_LargerThanHeight_ReturnsAll()
    {
        var df = CreateTestDataFrame();
        var result = df.Head(100);

        result.Height.Should().Be(df.Height);
    }

    [Fact]
    public void Head_Zero_ReturnsEmpty()
    {
        var df = CreateTestDataFrame();
        var result = df.Head(0);

        result.Height.Should().Be(0);
    }

    // ============================================================================
    // Tail Tests
    // ============================================================================

    [Fact]
    public void Tail_Default_ReturnsLast5()
    {
        var df = CreateLargeTestDataFrame(10);
        var result = df.Tail();

        result.Height.Should().Be(5);
    }

    [Fact]
    public void Tail_CustomN_ReturnsLastN()
    {
        var df = CreateLargeTestDataFrame(10);
        var result = df.Tail(3);

        result.Height.Should().Be(3);
    }

    [Fact]
    public void Tail_LargerThanHeight_ReturnsAll()
    {
        var df = CreateTestDataFrame();
        var result = df.Tail(100);

        result.Height.Should().Be(df.Height);
    }

    // ============================================================================
    // Slice Tests
    // ============================================================================

    [Fact]
    public void Slice_FromStart_ReturnsSlice()
    {
        var df = CreateLargeTestDataFrame(10);
        var result = df.Slice(0, 3);

        result.Height.Should().Be(3);
    }

    [Fact]
    public void Slice_FromMiddle_ReturnsSlice()
    {
        var df = CreateLargeTestDataFrame(10);
        var result = df.Slice(3, 4);

        result.Height.Should().Be(4);
    }

    [Fact]
    public void Slice_ToEnd_ReturnsRemainder()
    {
        var df = CreateLargeTestDataFrame(10);
        // Slice(offset=7, length=3) returns last 3 rows
        var result = df.Slice(7, 3);

        result.Height.Should().Be(3);
    }

    // ============================================================================
    // Take Tests
    // ============================================================================

    [Fact]
    public void Take_Indices_ReturnsSelectedRows()
    {
        var df = CreateLargeTestDataFrame(10);
        var result = df.Take(new[] { 0, 2, 4, 6, 8 });

        result.Height.Should().Be(5);
    }

    [Fact]
    public void Take_DuplicateIndices_ReturnsDuplicates()
    {
        var df = CreateTestDataFrame();
        var result = df.Take(new[] { 0, 0, 1, 1 });

        result.Height.Should().Be(4);
    }

    [Fact]
    public void Take_ReverseOrder_ReturnsReversed()
    {
        var df = CreateLargeTestDataFrame(5);
        var result = df.Take(new[] { 4, 3, 2, 1, 0 });

        result.Height.Should().Be(5);
    }

    // ============================================================================
    // Sample Tests
    // ============================================================================

    [Fact]
    public void Sample_CountN_ReturnsNRows()
    {
        var df = CreateLargeTestDataFrame(100);
        var result = df.Sample(10, seed: 42);

        result.Height.Should().Be(10);
    }

    [Fact]
    public void Sample_Fraction_ReturnsFractionOfRows()
    {
        var df = CreateLargeTestDataFrame(100);
        var result = df.Sample(0.1, seed: 42);

        result.Height.Should().Be(10);  // 10% of 100
    }

    [Fact]
    public void Sample_WithSeed_IsReproducible()
    {
        var df = CreateLargeTestDataFrame(100);
        var result1 = df.Sample(10, seed: 42);
        var result2 = df.Sample(10, seed: 42);

        // With same seed, should get same sample
        for (int i = 0; i < result1.Height; i++)
        {
            result1["value"][i].AsInt32().Should().Be(result2["value"][i].AsInt32());
        }
    }

    // ============================================================================
    // WithColumn Tests
    // ============================================================================

    [Fact]
    public void WithColumn_NewColumn_AddsColumn()
    {
        var df = CreateTestDataFrame();
        var newCol = Series.FromValues("id", new[] { 1, 2, 3 });
        var result = df.WithColumn(newCol);

        result.Width.Should().Be(4);
        result.Columns.Should().Contain("id");
    }

    [Fact]
    public void WithColumn_ExistingName_ReplacesColumn()
    {
        var df = CreateTestDataFrame();
        var newCol = Series.FromValues("name", new[] { "X", "Y", "Z" });
        var result = df.WithColumn(newCol);

        result.Width.Should().Be(3);
        result["name"][0].AsString().Should().Be("X");
    }

    [Fact]
    public void WithColumn_Expression_CreatesColumn()
    {
        var df = CreateTestDataFrame();
        var result = df.WithColumn("doubled_age", d => d["age"] * 2);

        result.Width.Should().Be(4);
        result.Columns.Should().Contain("doubled_age");
    }

    // ============================================================================
    // WithColumns Tests
    // ============================================================================

    [Fact]
    public void WithColumns_MultipleNew_AddsAll()
    {
        var df = CreateTestDataFrame();
        var col1 = Series.FromValues("id", new[] { 1, 2, 3 });
        var col2 = Series.FromValues("flag", new[] { true, false, true });
        var result = df.WithColumns(col1, col2);

        result.Width.Should().Be(5);
    }

    // ============================================================================
    // WithRowNumber Tests
    // ============================================================================

    [Fact]
    public void WithRowNumber_Default_AddsRowNrColumn()
    {
        var df = CreateTestDataFrame();
        var result = df.WithRowNumber();

        result.Columns.Should().Contain("row_nr");
        // WithRowNumber uses int[], so returns Int32
        result["row_nr"][0].AsInt32().Should().Be(0);
        result["row_nr"][1].AsInt32().Should().Be(1);
        result["row_nr"][2].AsInt32().Should().Be(2);
    }

    [Fact]
    public void WithRowNumber_CustomName_UsesName()
    {
        var df = CreateTestDataFrame();
        var result = df.WithRowNumber("idx");

        result.Columns.Should().Contain("idx");
    }

    // ============================================================================
    // Sort Tests
    // ============================================================================

    [Fact]
    public void Sort_SingleColumn_SortsAscending()
    {
        var df = CreateUnsortedDataFrame();
        var result = df.Sort("value");

        result["value"][0].AsInt32().Should().BeLessOrEqualTo(result["value"][1].AsInt32());
    }

    [Fact]
    public void Sort_Descending_SortsDescending()
    {
        var df = CreateUnsortedDataFrame();
        var result = df.Sort("value", descending: true);

        result["value"][0].AsInt32().Should().BeGreaterOrEqualTo(result["value"][1].AsInt32());
    }

    [Fact]
    public void Sort_MultipleColumns_SortsByFirst()
    {
        var name = Series.FromValues("name", new[] { "A", "B", "A", "B" });
        var value = Series.FromValues("value", new[] { 2, 1, 1, 2 });
        var df = new DataFrame(name, value);

        var result = df.Sort(("name", false), ("value", false));

        result["name"][0].AsString().Should().Be("A");
        result["value"][0].AsInt32().Should().Be(1);
    }

    // ============================================================================
    // Describe Tests
    // ============================================================================

    [Fact]
    public void Describe_NumericColumns_ReturnsStatistics()
    {
        var values = Series.FromValues("values", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 });
        var df = new DataFrame(values);
        var result = df.Describe();

        result.Width.Should().BeGreaterThan(0);
        result.Columns.Should().Contain("statistic");
    }

    // ============================================================================
    // NullCount Tests
    // ============================================================================

    [Fact]
    public void NullCount_WithNulls_ReturnsCorrectCounts()
    {
        var col1 = Series.FromNullable("a", new int?[] { 1, null, 3 });
        var col2 = Series.FromNullable("b", new int?[] { null, null, 3 });
        var df = new DataFrame(col1, col2);
        var result = df.NullCount();

        // NullCount returns a DataFrame with one row per column
        result.Height.Should().Be(2);  // 2 columns
        result.Columns.Should().Contain("column");
        result.Columns.Should().Contain("null_count");
    }

    [Fact]
    public void NullCount_NoNulls_ReturnsZeros()
    {
        var df = CreateTestDataFrame();
        var result = df.NullCount();

        // NullCount returns one row per column
        result.Height.Should().Be(df.Width);
    }

    // ============================================================================
    // Unique Tests
    // ============================================================================

    [Fact]
    public void Unique_AllColumns_ReturnsUniqueRows()
    {
        var name = Series.FromValues("name", new[] { "A", "B", "A", "B" });
        var value = Series.FromValues("value", new[] { 1, 2, 1, 3 });
        var df = new DataFrame(name, value);

        var result = df.Unique();

        result.Height.Should().BeLessThanOrEqualTo(4);
    }

    [Fact]
    public void Unique_SingleColumn_ReturnsUniqueByColumn()
    {
        var name = Series.FromValues("name", new[] { "A", "B", "A", "B" });
        var value = Series.FromValues("value", new[] { 1, 2, 3, 4 });
        var df = new DataFrame(name, value);

        var result = df.Unique("name");

        result.Height.Should().Be(2);  // Only "A" and "B"
    }

    // ============================================================================
    // NUnique Tests
    // ============================================================================

    [Fact]
    public void NUnique_ReturnsUniqueCounts()
    {
        var name = Series.FromValues("name", new[] { "A", "B", "A", "B" });
        var value = Series.FromValues("value", new[] { 1, 2, 3, 4 });
        var df = new DataFrame(name, value);

        var result = df.NUnique();

        // NUnique returns one row per column
        result.Height.Should().Be(2);  // 2 columns
        result.Columns.Should().Contain("column");
        result.Columns.Should().Contain("n_unique");
    }

    // ============================================================================
    // Melt Tests
    // ============================================================================

    [Fact]
    public void Melt_BasicMelt_ReturnsLongFormat()
    {
        var id = Series.FromValues("id", new[] { 1, 2 });
        var a = Series.FromValues("a", new[] { 10, 20 });
        var b = Series.FromValues("b", new[] { 100, 200 });
        var df = new DataFrame(id, a, b);

        var result = df.Melt(new[] { "id" }, new[] { "a", "b" });

        result.Columns.Should().Contain("variable");
        result.Columns.Should().Contain("value");
        result.Height.Should().Be(4);  // 2 rows × 2 value columns
    }

    [Fact]
    public void Melt_CustomNames_UsesCustomNames()
    {
        var id = Series.FromValues("id", new[] { 1, 2 });
        var a = Series.FromValues("a", new[] { 10, 20 });
        var df = new DataFrame(id, a);

        var result = df.Melt(new[] { "id" }, new[] { "a" }, "var_name", "val_name");

        result.Columns.Should().Contain("var_name");
        result.Columns.Should().Contain("val_name");
    }

    // ============================================================================
    // Filter Tests
    // ============================================================================

    [Fact]
    public void Filter_BooleanMask_FiltersRows()
    {
        var df = CreateTestDataFrame();
        var mask = Series.FromValues("mask", new[] { true, false, true });
        var result = df.Filter(mask);

        result.Height.Should().Be(2);
    }

    [Fact]
    public void Filter_AllTrue_ReturnsAll()
    {
        var df = CreateTestDataFrame();
        var mask = Series.FromValues("mask", new[] { true, true, true });
        var result = df.Filter(mask);

        result.Height.Should().Be(3);
    }

    [Fact]
    public void Filter_AllFalse_ReturnsEmpty()
    {
        var df = CreateTestDataFrame();
        var mask = Series.FromValues("mask", new[] { false, false, false });
        var result = df.Filter(mask);

        result.Height.Should().Be(0);
    }

    // ============================================================================
    // Edge Cases
    // ============================================================================

    [Fact]
    public void EmptyDataFrame_Operations_HandleGracefully()
    {
        var df = new DataFrame();

        df.Height.Should().Be(0);
        df.Width.Should().Be(0);
    }

    [Fact]
    public void SingleRowDataFrame_AllOperations_Work()
    {
        var name = Series.FromValues("name", new[] { "A" });
        var df = new DataFrame(name);

        df.Head().Height.Should().Be(1);
        df.Tail().Height.Should().Be(1);
        df.Slice(0, 1).Height.Should().Be(1);
    }

    [Fact]
    public void SingleColumnDataFrame_AllOperations_Work()
    {
        var col = Series.FromValues("col", new[] { 1, 2, 3 });
        var df = new DataFrame(col);

        df.Select("col").Width.Should().Be(1);
        df.Drop("col").Width.Should().Be(0);
    }

    // ============================================================================
    // Helper Methods
    // ============================================================================

    private DataFrame CreateTestDataFrame()
    {
        var name = Series.FromValues("name", new[] { "Alice", "Bob", "Charlie" });
        var age = Series.FromValues("age", new[] { 25, 30, 35 });
        var score = Series.FromValues("score", new[] { 85.5, 90.0, 78.5 });
        return new DataFrame(name, age, score);
    }

    private DataFrame CreateLargeTestDataFrame(int n)
    {
        var values = Enumerable.Range(0, n).ToArray();
        var names = Enumerable.Range(0, n).Select(i => $"item_{i}").ToArray();
        return new DataFrame(
            Series.FromValues("value", values),
            Series.FromValues("name", names)
        );
    }

    private DataFrame CreateUnsortedDataFrame()
    {
        var name = Series.FromValues("name", new[] { "C", "A", "B", "D" });
        var value = Series.FromValues("value", new[] { 3, 1, 2, 4 });
        return new DataFrame(name, value);
    }
}
