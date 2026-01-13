// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET
//
// Tests for DataFrame manipulation operations, inspired by Polars test suite.
// These tests cover:
// - WithColumns operations
// - WithRowIndex
// - Rename operations
// - Drop operations
// - Select operations
// - Column reordering

using FluentAssertions;
using Polaire;
using Polaire.DataTypes;
using Xunit;
using static Polaire.Pl;

namespace Polaire.Tests;

/// <summary>
/// Tests for DataFrame manipulation operations.
/// </summary>
public class DataFrameManipulationTests
{
    // ============================================================================
    // WithColumns Tests
    // ============================================================================

    [Fact]
    public void WithColumns_AddNewColumn_Works()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1.0, 2.0, 3.0 }),
            Series.FromValues("b", new[] { 10.0, 20.0, 30.0 })
        );

        var result = df.Lazy()
            .WithColumns((Col("a") + Col("b")).As("sum"))
            .Collect();

        result.Width.Should().Be(3);
        result.Columns.Should().Contain("sum");
    }

    [Fact]
    public void WithColumns_ReplaceExistingColumn_Works()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1.0, 2.0, 3.0 }),
            Series.FromValues("b", new[] { 10.0, 20.0, 30.0 })
        );

        var result = df.Lazy()
            .WithColumns((Col("a") * 2.0).As("a"))
            .Collect();

        result.Width.Should().Be(2);
        result["a"][0].AsFloat64().Should().Be(2.0);
        result["a"][1].AsFloat64().Should().Be(4.0);
    }

    [Fact]
    public void WithColumns_MultipleNewColumns_Works()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1.0, 2.0, 3.0 }),
            Series.FromValues("b", new[] { 10.0, 20.0, 30.0 })
        );

        var result = df.Lazy()
            .WithColumns(
                (Col("a") + Col("b")).As("sum"),
                (Col("a") * Col("b")).As("product")
            )
            .Collect();

        result.Width.Should().Be(4);
        result.Columns.Should().Contain("sum");
        result.Columns.Should().Contain("product");
    }

    [Fact]
    public void WithColumns_LiteralColumn_Works()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1.0, 2.0, 3.0 })
        );

        var result = df.Lazy()
            .WithColumns(Lit(100.0).As("constant"))
            .Collect();

        result.Width.Should().Be(2);
        result["constant"][0].AsFloat64().Should().Be(100);
        result["constant"][2].AsFloat64().Should().Be(100);
    }

    [Fact]
    public void WithColumns_ChainedOperations_Works()
    {
        var df = new DataFrame(
            Series.FromValues("x", new[] { 1.0, 2.0, 3.0 })
        );

        var result = df.Lazy()
            .WithColumns((Col("x") * 2.0).As("y"))
            .WithColumns((Col("y") + 10.0).As("z"))
            .Collect();

        result.Width.Should().Be(3);
        result["z"][0].AsFloat64().Should().Be(12.0);  // (1*2)+10
        result["z"][2].AsFloat64().Should().Be(16.0);  // (3*2)+10
    }

    // ============================================================================
    // Rename Tests
    // ============================================================================

    [Fact]
    public void Rename_SingleColumn_Works()
    {
        var df = new DataFrame(
            Series.FromValues("old_name", new[] { 1, 2, 3 })
        );

        var result = df.Rename(new Dictionary<string, string> { { "old_name", "new_name" } });

        result.Columns.Should().Contain("new_name");
        result.Columns.Should().NotContain("old_name");
        result["new_name"][0].AsInt32().Should().Be(1);
    }

    [Fact]
    public void Rename_PreservesOrder()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1 }),
            Series.FromValues("b", new[] { 2 }),
            Series.FromValues("c", new[] { 3 })
        );

        var result = df.Rename(new Dictionary<string, string> { { "b", "B" } });

        result.Columns.Should().ContainInOrder("a", "B", "c");
    }

    [Fact]
    public void Rename_PreservesData()
    {
        var df = new DataFrame(
            Series.FromValues("col", new[] { 10, 20, 30 })
        );

        var result = df.Rename(new Dictionary<string, string> { { "col", "renamed" } });

        result["renamed"][0].AsInt32().Should().Be(10);
        result["renamed"][1].AsInt32().Should().Be(20);
        result["renamed"][2].AsInt32().Should().Be(30);
    }

    // ============================================================================
    // Drop Tests
    // ============================================================================

    [Fact]
    public void Drop_SingleColumn_Works()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 }),
            Series.FromValues("b", new[] { 10, 20, 30 }),
            Series.FromValues("c", new[] { 100, 200, 300 })
        );

        var result = df.Drop("b");

        result.Width.Should().Be(2);
        result.Columns.Should().Contain("a");
        result.Columns.Should().Contain("c");
        result.Columns.Should().NotContain("b");
    }

    [Fact]
    public void Drop_MultipleColumns_Works()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1 }),
            Series.FromValues("b", new[] { 2 }),
            Series.FromValues("c", new[] { 3 }),
            Series.FromValues("d", new[] { 4 })
        );

        var result = df.Drop("b", "c");

        result.Width.Should().Be(2);
        result.Columns.Should().ContainInOrder("a", "d");
    }

    [Fact]
    public void Drop_PreservesRowCount()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 }),
            Series.FromValues("b", new[] { 10, 20, 30, 40, 50 })
        );

        var result = df.Drop("b");

        result.Height.Should().Be(5);
    }

    // ============================================================================
    // Select Tests
    // ============================================================================

    [Fact]
    public void Select_SingleColumn_Works()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 }),
            Series.FromValues("b", new[] { 10, 20, 30 }),
            Series.FromValues("c", new[] { 100, 200, 300 })
        );

        var result = df.Select("b");

        result.Width.Should().Be(1);
        result.Columns.Should().Contain("b");
    }

    [Fact]
    public void Select_MultipleColumns_Works()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1 }),
            Series.FromValues("b", new[] { 2 }),
            Series.FromValues("c", new[] { 3 }),
            Series.FromValues("d", new[] { 4 })
        );

        var result = df.Select("a", "c");

        result.Width.Should().Be(2);
        result.Columns.Should().ContainInOrder("a", "c");
    }

    [Fact]
    public void Select_ChangesOrder()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1 }),
            Series.FromValues("b", new[] { 2 }),
            Series.FromValues("c", new[] { 3 })
        );

        var result = df.Select("c", "a", "b");

        result.Columns.Should().ContainInOrder("c", "a", "b");
    }

    [Fact]
    public void Select_PreservesData()
    {
        var df = new DataFrame(
            Series.FromValues("x", new[] { 10, 20, 30 }),
            Series.FromValues("y", new[] { 100, 200, 300 })
        );

        var result = df.Select("y");

        result["y"][0].AsInt32().Should().Be(100);
        result["y"][2].AsInt32().Should().Be(300);
    }

    // ============================================================================
    // Lazy Select Tests
    // ============================================================================

    [Fact]
    public void LazySelect_SingleColumn_Works()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 }),
            Series.FromValues("b", new[] { 10, 20, 30 })
        );

        var result = df.Lazy()
            .Select("a")
            .Collect();

        result.Width.Should().Be(1);
        result.Columns.Should().Contain("a");
    }

    [Fact]
    public void LazySelect_WithExpression_Works()
    {
        var df = new DataFrame(
            Series.FromValues("x", new[] { 1.0, 2.0, 3.0 })
        );

        var result = df.Lazy()
            .Select(Col("x"), (Col("x") * 2.0).As("doubled"))
            .Collect();

        result.Width.Should().Be(2);
        result["doubled"][0].AsFloat64().Should().Be(2.0);
    }

    // ============================================================================
    // Column Access Tests
    // ============================================================================

    [Fact]
    public void Indexer_ByName_ReturnsColumn()
    {
        var df = new DataFrame(
            Series.FromValues("name", new[] { "Alice", "Bob" }),
            Series.FromValues("age", new[] { 30, 25 })
        );

        var column = df["name"];

        column.Name.Should().Be("name");
        column.Length.Should().Be(2);
    }

    [Fact]
    public void Indexer_InvalidName_Throws()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 })
        );

        var act = () => df["nonexistent"];

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Columns_ReturnsAllColumnNames()
    {
        var df = new DataFrame(
            Series.FromValues("x", new[] { 1 }),
            Series.FromValues("y", new[] { 2 }),
            Series.FromValues("z", new[] { 3 })
        );

        df.Columns.Should().ContainInOrder("x", "y", "z");
    }

    [Fact]
    public void Width_ReturnsColumnCount()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1 }),
            Series.FromValues("b", new[] { 2 }),
            Series.FromValues("c", new[] { 3 })
        );

        df.Width.Should().Be(3);
    }

    [Fact]
    public void Height_ReturnsRowCount()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 })
        );

        df.Height.Should().Be(5);
    }

    // ============================================================================
    // Empty DataFrame Tests
    // ============================================================================

    [Fact]
    public void Empty_HasZeroHeight()
    {
        var df = new DataFrame(
            Series.FromValues("a", Array.Empty<int>())
        );

        df.Height.Should().Be(0);
    }

    [Fact]
    public void Empty_PreservesColumns()
    {
        var df = new DataFrame(
            Series.FromValues("x", Array.Empty<int>()),
            Series.FromValues("y", Array.Empty<string>())
        );

        df.Columns.Should().ContainInOrder("x", "y");
    }

    [Fact]
    public void Empty_Select_ReturnsEmpty()
    {
        var df = new DataFrame(
            Series.FromValues("a", Array.Empty<int>()),
            Series.FromValues("b", Array.Empty<int>())
        );

        var result = df.Select("a");

        result.Height.Should().Be(0);
        result.Width.Should().Be(1);
    }

    // ============================================================================
    // Single Row Tests
    // ============================================================================

    [Fact]
    public void SingleRow_Select_Works()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 42 }),
            Series.FromValues("b", new[] { 100 })
        );

        var result = df.Select("a");

        result.Height.Should().Be(1);
        result["a"][0].AsInt32().Should().Be(42);
    }

    [Fact]
    public void SingleRow_Drop_Works()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1 }),
            Series.FromValues("b", new[] { 2 })
        );

        var result = df.Drop("b");

        result.Width.Should().Be(1);
        result.Height.Should().Be(1);
    }

    // ============================================================================
    // Large DataFrame Tests
    // ============================================================================

    [Fact]
    public void Large_Select_Works()
    {
        var values = Enumerable.Range(0, 10000).ToArray();
        var df = new DataFrame(
            Series.FromValues("id", values),
            Series.FromValues("value", values.Select(x => x * 2).ToArray())
        );

        var result = df.Select("id");

        result.Height.Should().Be(10000);
        result.Width.Should().Be(1);
    }

    [Fact]
    public void Large_Drop_Works()
    {
        var values = Enumerable.Range(0, 10000).ToArray();
        var df = new DataFrame(
            Series.FromValues("a", values),
            Series.FromValues("b", values),
            Series.FromValues("c", values)
        );

        var result = df.Drop("b");

        result.Height.Should().Be(10000);
        result.Width.Should().Be(2);
    }

    // ============================================================================
    // Type Preservation Tests
    // ============================================================================

    [Fact]
    public void Select_PreservesInt32Type()
    {
        var df = new DataFrame(
            Series.FromValues("int_col", new[] { 1, 2, 3 })
        );

        var result = df.Select("int_col");

        result["int_col"].DataType.Should().Be(DataType.Int32);
    }

    [Fact]
    public void Select_PreservesFloat64Type()
    {
        var df = new DataFrame(
            Series.FromValues("float_col", new[] { 1.5, 2.5, 3.5 })
        );

        var result = df.Select("float_col");

        result["float_col"].DataType.Should().Be(DataType.Float64);
    }

    [Fact]
    public void Select_PreservesStringType()
    {
        var df = new DataFrame(
            Series.FromValues("str_col", new[] { "a", "b", "c" })
        );

        var result = df.Select("str_col");

        result["str_col"].DataType.Should().Be(DataType.String);
    }

    [Fact]
    public void Select_PreservesBooleanType()
    {
        var df = new DataFrame(
            Series.FromValues("bool_col", new[] { true, false, true })
        );

        var result = df.Select("bool_col");

        result["bool_col"].DataType.Should().Be(DataType.Boolean);
    }

    // ============================================================================
    // Null Handling Tests
    // ============================================================================

    [Fact]
    public void Select_PreservesNulls()
    {
        var df = new DataFrame(
            Series.FromNullable("value", new int?[] { 1, null, 3 })
        );

        var result = df.Select("value");

        result["value"].IsNull(1).Should().BeTrue();
    }

    [Fact]
    public void Drop_PreservesNulls()
    {
        var df = new DataFrame(
            Series.FromNullable("a", new int?[] { 1, null, 3 }),
            Series.FromValues("b", new[] { 10, 20, 30 })
        );

        var result = df.Drop("b");

        result["a"].IsNull(1).Should().BeTrue();
    }

    // ============================================================================
    // Chained Operations Tests
    // ============================================================================

    [Fact]
    public void ChainedOperations_SelectThenFilter_Works()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 }),
            Series.FromValues("b", new[] { 10, 20, 30, 40, 50 })
        );

        var result = df.Lazy()
            .Select("a", "b")
            .Filter(Col("a").Gt(2))
            .Collect();

        result.Height.Should().Be(3);
        result.Width.Should().Be(2);
    }

    [Fact]
    public void ChainedOperations_FilterThenSelect_Works()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 }),
            Series.FromValues("b", new[] { 10, 20, 30, 40, 50 })
        );

        var result = df.Lazy()
            .Filter(Col("a").Gt(2))
            .Select("b")
            .Collect();

        result.Height.Should().Be(3);
        result.Width.Should().Be(1);
    }

    [Fact]
    public void ChainedOperations_WithColumnsThenFilter_Works()
    {
        var df = new DataFrame(
            Series.FromValues("x", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 })
        );

        var result = df.Lazy()
            .WithColumns((Col("x") * 10.0).As("y"))
            .Filter(Col("y").Gt(20))
            .Collect();

        result.Height.Should().Be(3);
        result["y"][0].AsFloat64().Should().Be(30.0);
    }
}
