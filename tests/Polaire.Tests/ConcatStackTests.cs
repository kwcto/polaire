// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using FluentAssertions;
using Polaire;
using Polaire.DataTypes;
using Xunit;
using static Polaire.Pl;

namespace Polaire.Tests;

/// <summary>
/// Tests for concat and stack operations.
/// </summary>
public class ConcatStackTests
{
    // ============================================================================
    // DataFrame VConcat Tests (Vertical Concatenation - append rows)
    // ============================================================================

    [Fact]
    public void DataFrame_VConcat_AppendsRows()
    {
        var df1 = new DataFrame(
            Series.FromValues("a", new[] { 1, 2 }),
            Series.FromValues("b", new[] { 10, 20 })
        );
        var df2 = new DataFrame(
            Series.FromValues("a", new[] { 3, 4 }),
            Series.FromValues("b", new[] { 30, 40 })
        );

        var result = DataFrame.VConcat(df1, df2);

        result.Height.Should().Be(4);
        result.Width.Should().Be(2);
        result["a"][0].AsInt32().Should().Be(1);
        result["a"][3].AsInt32().Should().Be(4);
    }

    [Fact]
    public void DataFrame_VConcat_EmptyDataFrame_ReturnsOther()
    {
        var df1 = new DataFrame(
            Series.FromValues("a", Array.Empty<int>()),
            Series.FromValues("b", Array.Empty<int>())
        );
        var df2 = new DataFrame(
            Series.FromValues("a", new[] { 1, 2 }),
            Series.FromValues("b", new[] { 10, 20 })
        );

        var result = DataFrame.VConcat(df1, df2);

        result.Height.Should().Be(2);
    }

    [Fact]
    public void DataFrame_VConcat_PreservesColumnOrder()
    {
        var df1 = new DataFrame(
            Series.FromValues("a", new[] { 1 }),
            Series.FromValues("b", new[] { 10 }),
            Series.FromValues("c", new[] { 100 })
        );
        var df2 = new DataFrame(
            Series.FromValues("a", new[] { 2 }),
            Series.FromValues("b", new[] { 20 }),
            Series.FromValues("c", new[] { 200 })
        );

        var result = DataFrame.VConcat(df1, df2);

        result.Columns[0].Should().Be("a");
        result.Columns[1].Should().Be("b");
        result.Columns[2].Should().Be("c");
    }

    [Fact]
    public void DataFrame_VConcat_MixedTypes_WorksCorrectly()
    {
        var df1 = new DataFrame(
            Series.FromValues("id", new[] { 1, 2 }),
            Series.FromValues("name", new[] { "Alice", "Bob" }),
            Series.FromValues("score", new[] { 85.5, 90.0 })
        );
        var df2 = new DataFrame(
            Series.FromValues("id", new[] { 3 }),
            Series.FromValues("name", new[] { "Charlie" }),
            Series.FromValues("score", new[] { 88.0 })
        );

        var result = DataFrame.VConcat(df1, df2);

        result.Height.Should().Be(3);
        result["name"][2].AsString().Should().Be("Charlie");
    }

    [Fact]
    public void DataFrame_VConcat_Multiple_WorksCorrectly()
    {
        var df1 = new DataFrame(Series.FromValues("a", new[] { 1 }));
        var df2 = new DataFrame(Series.FromValues("a", new[] { 2 }));
        var df3 = new DataFrame(Series.FromValues("a", new[] { 3 }));

        var result = DataFrame.VConcat(df1, df2, df3);

        result.Height.Should().Be(3);
        result["a"][0].AsInt32().Should().Be(1);
        result["a"][1].AsInt32().Should().Be(2);
        result["a"][2].AsInt32().Should().Be(3);
    }

    [Fact]
    public void DataFrame_VConcat_SingleDataFrame_ReturnsSame()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 })
        );

        var result = DataFrame.VConcat(df);

        result.Height.Should().Be(3);
    }

    // ============================================================================
    // DataFrame HConcat Tests (Horizontal Concatenation - append columns)
    // ============================================================================

    [Fact]
    public void DataFrame_HConcat_AppendsColumns()
    {
        var df1 = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 })
        );
        var df2 = new DataFrame(
            Series.FromValues("b", new[] { 10, 20, 30 })
        );

        var result = DataFrame.HConcat(df1, df2);

        result.Height.Should().Be(3);
        result.Width.Should().Be(2);
        result.Columns.Should().Contain("a");
        result.Columns.Should().Contain("b");
    }

    [Fact]
    public void DataFrame_HConcat_MultipleColumns()
    {
        var df1 = new DataFrame(
            Series.FromValues("a", new[] { 1, 2 })
        );
        var df2 = new DataFrame(
            Series.FromValues("b", new[] { 10, 20 }),
            Series.FromValues("c", new[] { 100, 200 })
        );

        var result = DataFrame.HConcat(df1, df2);

        result.Width.Should().Be(3);
        result.Columns.Should().BeEquivalentTo(new[] { "a", "b", "c" });
    }

    [Fact]
    public void DataFrame_HConcat_Multiple_WorksCorrectly()
    {
        var df1 = new DataFrame(Series.FromValues("a", new[] { 1 }));
        var df2 = new DataFrame(Series.FromValues("b", new[] { 2 }));
        var df3 = new DataFrame(Series.FromValues("c", new[] { 3 }));

        var result = DataFrame.HConcat(df1, df2, df3);

        result.Width.Should().Be(3);
        result["a"][0].AsInt32().Should().Be(1);
        result["b"][0].AsInt32().Should().Be(2);
        result["c"][0].AsInt32().Should().Be(3);
    }

    // ============================================================================
    // Pl.Concat Tests (Static Helper)
    // ============================================================================

    [Fact]
    public void Pl_Concat_WorksCorrectly()
    {
        var df1 = new DataFrame(
            Series.FromValues("a", new[] { 1, 2 })
        );
        var df2 = new DataFrame(
            Series.FromValues("a", new[] { 3, 4 })
        );

        var result = Pl.Concat(df1, df2);

        result.Height.Should().Be(4);
    }

    [Fact]
    public void Pl_HConcat_WorksCorrectly()
    {
        var df1 = new DataFrame(
            Series.FromValues("a", new[] { 1, 2 })
        );
        var df2 = new DataFrame(
            Series.FromValues("b", new[] { 10, 20 })
        );

        var result = Pl.HConcat(df1, df2);

        result.Width.Should().Be(2);
    }

    // ============================================================================
    // LazyFrame Union Tests
    // ============================================================================

    [Fact]
    public void LazyFrame_Union_CombinesDataFrames()
    {
        var df1 = new DataFrame(
            Series.FromValues("a", new[] { 1, 2 }),
            Series.FromValues("b", new[] { 10, 20 })
        );
        var df2 = new DataFrame(
            Series.FromValues("a", new[] { 3, 4 }),
            Series.FromValues("b", new[] { 30, 40 })
        );

        var result = df1.Lazy().Union(df2.Lazy()).Collect();

        result.Height.Should().Be(4);
        result["a"][0].AsInt32().Should().Be(1);
        result["a"][3].AsInt32().Should().Be(4);
    }

    [Fact]
    public void LazyFrame_Concat_Static_WorksCorrectly()
    {
        var df1 = new DataFrame(
            Series.FromValues("a", new[] { 1, 2 })
        );
        var df2 = new DataFrame(
            Series.FromValues("a", new[] { 3, 4 })
        );

        var result = Pl.Concat(df1.Lazy(), df2.Lazy()).Collect();

        result.Height.Should().Be(4);
    }

    // ============================================================================
    // Edge Cases
    // ============================================================================

    [Fact]
    public void DataFrame_VConcat_LargeDataFrame_WorksCorrectly()
    {
        var size = 1000;
        var df1 = new DataFrame(
            Series.FromValues("id", Enumerable.Range(0, size).ToArray()),
            Series.FromValues("value", Enumerable.Range(0, size).Select(i => (double)i).ToArray())
        );
        var df2 = new DataFrame(
            Series.FromValues("id", Enumerable.Range(size, size).ToArray()),
            Series.FromValues("value", Enumerable.Range(size, size).Select(i => (double)i).ToArray())
        );

        var result = DataFrame.VConcat(df1, df2);

        result.Height.Should().Be(size * 2);
    }

    [Fact]
    public void DataFrame_VConcat_SingleRow_WorksCorrectly()
    {
        var df1 = new DataFrame(
            Series.FromValues("a", new[] { 1 }),
            Series.FromValues("b", new[] { 10 })
        );
        var df2 = new DataFrame(
            Series.FromValues("a", new[] { 2 }),
            Series.FromValues("b", new[] { 20 })
        );

        var result = DataFrame.VConcat(df1, df2);

        result.Height.Should().Be(2);
    }

    [Fact]
    public void DataFrame_VConcat_Float64_WorksCorrectly()
    {
        var df1 = new DataFrame(
            Series.FromValues("value", new[] { 1.1, 2.2 })
        );
        var df2 = new DataFrame(
            Series.FromValues("value", new[] { 3.3, 4.4 })
        );

        var result = DataFrame.VConcat(df1, df2);

        result.Height.Should().Be(4);
        result["value"][0].AsFloat64().Should().BeApproximately(1.1, 0.001);
        result["value"][3].AsFloat64().Should().BeApproximately(4.4, 0.001);
    }

    [Fact]
    public void DataFrame_VConcat_String_WorksCorrectly()
    {
        var df1 = new DataFrame(
            Series.FromValues("name", new[] { "Alice", "Bob" })
        );
        var df2 = new DataFrame(
            Series.FromValues("name", new[] { "Charlie", "Diana" })
        );

        var result = DataFrame.VConcat(df1, df2);

        result.Height.Should().Be(4);
        result["name"][0].AsString().Should().Be("Alice");
        result["name"][3].AsString().Should().Be("Diana");
    }

    [Fact]
    public void DataFrame_VConcat_Boolean_WorksCorrectly()
    {
        var df1 = new DataFrame(
            Series.FromValues("flag", new[] { true, false })
        );
        var df2 = new DataFrame(
            Series.FromValues("flag", new[] { false, true })
        );

        var result = DataFrame.VConcat(df1, df2);

        result.Height.Should().Be(4);
        result["flag"][0].AsBoolean().Should().BeTrue();
        result["flag"][3].AsBoolean().Should().BeTrue();
    }

    [Fact]
    public void DataFrame_VConcat_WithNulls_PreservesNulls()
    {
        var df1 = new DataFrame(
            Series.FromNullable("value", new int?[] { 1, null })
        );
        var df2 = new DataFrame(
            Series.FromNullable("value", new int?[] { null, 4 })
        );

        var result = DataFrame.VConcat(df1, df2);

        result.Height.Should().Be(4);
        result["value"].NullCount.Should().Be(2);
    }

}
