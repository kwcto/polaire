// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using FluentAssertions;
using Polaire;
using Polaire.DataTypes;
using Xunit;
using static Polaire.Pl;

namespace Polaire.Tests;

/// <summary>
/// Tests for DataFrame construction and manipulation.
/// </summary>
public class DataFrameConstructionTests
{
    // ============================================================================
    // Basic Construction Tests
    // ============================================================================

    [Fact]
    public void DataFrame_FromSeries_CreatesCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 }),
            Series.FromValues("b", new[] { 4, 5, 6 })
        );

        df.Width.Should().Be(2);
        df.Height.Should().Be(3);
    }

    [Fact]
    public void DataFrame_SingleSeries_CreatesCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("only", new[] { 1, 2, 3 })
        );

        df.Width.Should().Be(1);
        df.Height.Should().Be(3);
    }

    [Fact]
    public void DataFrame_MultipleSeries_DifferentTypes()
    {
        var df = new DataFrame(
            Series.FromValues("int_col", new[] { 1, 2, 3 }),
            Series.FromValues("str_col", new[] { "a", "b", "c" }),
            Series.FromValues("float_col", new[] { 1.1, 2.2, 3.3 }),
            Series.FromValues("bool_col", new[] { true, false, true })
        );

        df.Width.Should().Be(4);
        df["int_col"].DataType.Should().Be(DataType.Int32);
        df["str_col"].DataType.Should().Be(DataType.String);
        df["float_col"].DataType.Should().Be(DataType.Float64);
        df["bool_col"].DataType.Should().Be(DataType.Boolean);
    }

    [Fact]
    public void DataFrame_EmptySeries_CreatesEmptyDataFrame()
    {
        var df = new DataFrame(
            Series.FromValues("a", Array.Empty<int>()),
            Series.FromValues("b", Array.Empty<string>())
        );

        df.Width.Should().Be(2);
        df.Height.Should().Be(0);
    }

    // ============================================================================
    // Column Access Tests
    // ============================================================================

    [Fact]
    public void DataFrame_Indexer_ReturnsCorrectSeries()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 }),
            Series.FromValues("b", new[] { 4, 5, 6 })
        );

        var seriesA = df["a"];
        var seriesB = df["b"];

        seriesA[0].AsInt32().Should().Be(1);
        seriesB[0].AsInt32().Should().Be(4);
    }

    [Fact]
    public void DataFrame_Indexer_NonExistentColumn_Throws()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 })
        );

        var act = () => df["nonexistent"];

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void DataFrame_Columns_ReturnsAllColumnNames()
    {
        var df = new DataFrame(
            Series.FromValues("col1", new[] { 1 }),
            Series.FromValues("col2", new[] { 2 }),
            Series.FromValues("col3", new[] { 3 })
        );

        df.Columns.Should().HaveCount(3);
        df.Columns.Should().ContainInOrder("col1", "col2", "col3");
    }

    // ============================================================================
    // Select Tests
    // ============================================================================

    [Fact]
    public void DataFrame_Select_SingleColumn_ReturnsSubset()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 }),
            Series.FromValues("b", new[] { 4, 5, 6 }),
            Series.FromValues("c", new[] { 7, 8, 9 })
        );

        var result = df.Select("b");

        result.Width.Should().Be(1);
        result.Columns.Should().Contain("b");
        result["b"][0].AsInt32().Should().Be(4);
    }

    [Fact]
    public void DataFrame_Select_MultipleColumns_ReturnsSubset()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2 }),
            Series.FromValues("b", new[] { 3, 4 }),
            Series.FromValues("c", new[] { 5, 6 })
        );

        var result = df.Select("a", "c");

        result.Width.Should().Be(2);
        result.Columns.Should().ContainInOrder("a", "c");
    }

    [Fact]
    public void DataFrame_Select_ReordersColumns()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1 }),
            Series.FromValues("b", new[] { 2 }),
            Series.FromValues("c", new[] { 3 })
        );

        var result = df.Select("c", "a", "b");

        result.Columns[0].Should().Be("c");
        result.Columns[1].Should().Be("a");
        result.Columns[2].Should().Be("b");
    }

    // ============================================================================
    // Drop Tests
    // ============================================================================

    [Fact]
    public void DataFrame_Drop_SingleColumn_RemovesColumn()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2 }),
            Series.FromValues("b", new[] { 3, 4 }),
            Series.FromValues("c", new[] { 5, 6 })
        );

        var result = df.Drop("b");

        result.Width.Should().Be(2);
        result.Columns.Should().NotContain("b");
        result.Columns.Should().Contain("a");
        result.Columns.Should().Contain("c");
    }

    [Fact]
    public void DataFrame_Drop_MultipleColumns_RemovesAll()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1 }),
            Series.FromValues("b", new[] { 2 }),
            Series.FromValues("c", new[] { 3 }),
            Series.FromValues("d", new[] { 4 })
        );

        var result = df.Drop("a", "c");

        result.Width.Should().Be(2);
        result.Columns.Should().ContainInOrder("b", "d");
    }

    // ============================================================================
    // WithColumn Tests
    // ============================================================================

    [Fact]
    public void DataFrame_WithColumn_AddsNewColumn()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 })
        );

        var newCol = Series.FromValues("b", new[] { 10, 20, 30 });
        var result = df.WithColumn(newCol);

        result.Width.Should().Be(2);
        result.Columns.Should().Contain("b");
        result["b"][0].AsInt32().Should().Be(10);
    }

    [Fact]
    public void DataFrame_WithColumn_ReplacesExistingColumn()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 }),
            Series.FromValues("b", new[] { 10, 20, 30 })
        );

        var replacement = Series.FromValues("b", new[] { 100, 200, 300 });
        var result = df.WithColumn(replacement);

        result.Width.Should().Be(2);
        result["b"][0].AsInt32().Should().Be(100);
    }

    // ============================================================================
    // Rename Tests
    // ============================================================================

    [Fact]
    public void DataFrame_Rename_ChangesColumnName()
    {
        var df = new DataFrame(
            Series.FromValues("old_name", new[] { 1, 2, 3 })
        );

        var result = df.Rename(new Dictionary<string, string> { { "old_name", "new_name" } });

        result.Columns.Should().Contain("new_name");
        result.Columns.Should().NotContain("old_name");
    }

    [Fact]
    public void DataFrame_Rename_MultipleColumns()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1 }),
            Series.FromValues("b", new[] { 2 })
        );

        var result = df.Rename(new Dictionary<string, string>
        {
            { "a", "x" },
            { "b", "y" }
        });

        result.Columns.Should().ContainInOrder("x", "y");
    }

    // ============================================================================
    // Row Access Tests (via Series indexing)
    // ============================================================================

    [Fact]
    public void DataFrame_RowAccess_ViaSeriesIndexing()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 }),
            Series.FromValues("b", new[] { 10, 20, 30 })
        );

        // Row 1 accessed via column indexers
        df["a"][1].AsInt32().Should().Be(2);
        df["b"][1].AsInt32().Should().Be(20);
    }

    // ============================================================================
    // Dimension Properties Tests
    // ============================================================================

    [Fact]
    public void DataFrame_Height_ReturnsRowCount()
    {
        var df = new DataFrame(
            Series.FromValues("col", new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 })
        );

        df.Height.Should().Be(10);
    }

    [Fact]
    public void DataFrame_Width_ReturnsColumnCount()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1 }),
            Series.FromValues("b", new[] { 2 }),
            Series.FromValues("c", new[] { 3 }),
            Series.FromValues("d", new[] { 4 }),
            Series.FromValues("e", new[] { 5 })
        );

        df.Width.Should().Be(5);
    }

    [Fact]
    public void DataFrame_Shape_ReturnsCorrectDimensions()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 }),
            Series.FromValues("b", new[] { 4, 5, 6 })
        );

        df.Height.Should().Be(3);
        df.Width.Should().Be(2);
    }

    // ============================================================================
    // Factory Method Tests (Pl class)
    // ============================================================================

    [Fact]
    public void Pl_DataFrame_CreateFromSeries()
    {
        var df = DataFrame(
            Series("a", new[] { 1, 2, 3 }),
            Series("b", new[] { 4, 5, 6 })
        );

        df.Width.Should().Be(2);
        df.Height.Should().Be(3);
    }

    // ============================================================================
    // Large DataFrame Tests
    // ============================================================================

    [Fact]
    public void DataFrame_LargeRowCount_CreatesCorrectly()
    {
        var size = 100000;
        var df = new DataFrame(
            Series.FromValues("id", Enumerable.Range(0, size).ToArray()),
            Series.FromValues("value", Enumerable.Range(0, size).Select(x => (double)x).ToArray())
        );

        df.Height.Should().Be(size);
        df["id"][0].AsInt32().Should().Be(0);
        df["id"][size - 1].AsInt32().Should().Be(size - 1);
    }

    [Fact]
    public void DataFrame_ManyColumns_CreatesCorrectly()
    {
        var columns = Enumerable.Range(0, 50)
            .Select(i => Series.FromValues($"col_{i}", new[] { i }))
            .ToArray();

        var df = new DataFrame(columns);

        df.Width.Should().Be(50);
        df["col_0"][0].AsInt32().Should().Be(0);
        df["col_49"][0].AsInt32().Should().Be(49);
    }

    // ============================================================================
    // Null Handling Tests
    // ============================================================================

    [Fact]
    public void DataFrame_WithNulls_PreservesNulls()
    {
        var df = new DataFrame(
            Series.FromNullable("nullable", new int?[] { 1, null, 3, null, 5 }),
            Series.FromValues("non_null", new[] { 10, 20, 30, 40, 50 })
        );

        df["nullable"].NullCount.Should().Be(2);
        df["non_null"].NullCount.Should().Be(0);
    }

    [Fact]
    public void DataFrame_AllNullColumn_WorksCorrectly()
    {
        var df = new DataFrame(
            Series.FromNullable("all_null", new int?[] { null, null, null }),
            Series.FromValues("data", new[] { 1, 2, 3 })
        );

        df["all_null"].NullCount.Should().Be(3);
        df.Height.Should().Be(3);
    }

    // ============================================================================
    // Edge Cases
    // ============================================================================

    [Fact]
    public void DataFrame_ColumnNameWithSpaces_WorksCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("column with spaces", new[] { 1, 2, 3 })
        );

        df["column with spaces"][0].AsInt32().Should().Be(1);
    }

    [Fact]
    public void DataFrame_UnicodeColumnName_WorksCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("列名", new[] { 1, 2, 3 })
        );

        df["列名"][0].AsInt32().Should().Be(1);
    }

    [Fact]
    public void DataFrame_EmptyColumnName_WorksCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("", new[] { 1, 2, 3 })
        );

        df[""][0].AsInt32().Should().Be(1);
    }

    // ============================================================================
    // Describe/Summary Tests
    // ============================================================================

    [Fact]
    public void DataFrame_Describe_ReturnsStatistics()
    {
        var df = new DataFrame(
            Series.FromValues("numbers", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 })
        );

        var description = df.Describe();

        description.Should().NotBeNull();
        description.Height.Should().BeGreaterThan(0);
    }

    // ============================================================================
    // Column Order Preservation Tests
    // ============================================================================

    [Fact]
    public void DataFrame_PreservesColumnOrder()
    {
        var df = new DataFrame(
            Series.FromValues("z", new[] { 1 }),
            Series.FromValues("a", new[] { 2 }),
            Series.FromValues("m", new[] { 3 })
        );

        df.Columns[0].Should().Be("z");
        df.Columns[1].Should().Be("a");
        df.Columns[2].Should().Be("m");
    }

    // ============================================================================
    // Type Consistency Tests
    // ============================================================================

    [Fact]
    public void DataFrame_AllIntColumns_MaintainTypes()
    {
        var df = new DataFrame(
            Series.FromValues("int8", new sbyte[] { 1, 2, 3 }),
            Series.FromValues("int16", new short[] { 1, 2, 3 }),
            Series.FromValues("int32", new int[] { 1, 2, 3 }),
            Series.FromValues("int64", new long[] { 1, 2, 3 })
        );

        df["int8"].DataType.Should().Be(DataType.Int8);
        df["int16"].DataType.Should().Be(DataType.Int16);
        df["int32"].DataType.Should().Be(DataType.Int32);
        df["int64"].DataType.Should().Be(DataType.Int64);
    }

    [Fact]
    public void DataFrame_AllFloatColumns_MaintainTypes()
    {
        var df = new DataFrame(
            Series.FromValues("float32", new float[] { 1.0f, 2.0f }),
            Series.FromValues("float64", new double[] { 1.0, 2.0 })
        );

        df["float32"].DataType.Should().Be(DataType.Float32);
        df["float64"].DataType.Should().Be(DataType.Float64);
    }
}
