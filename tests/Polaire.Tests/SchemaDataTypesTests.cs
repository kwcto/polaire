// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using FluentAssertions;
using Polaire;
using Polaire.DataTypes;
using Xunit;

namespace Polaire.Tests;

/// <summary>
/// Tests for schema operations and data type handling.
/// </summary>
public class SchemaDataTypesTests
{
    // ============================================================================
    // Series DataType Tests
    // ============================================================================

    [Fact]
    public void Series_Int32_HasCorrectDataType()
    {
        var series = Series.FromValues("values", new[] { 1, 2, 3 });

        series.DataType.Should().Be(DataType.Int32);
    }

    [Fact]
    public void Series_Int64_HasCorrectDataType()
    {
        var series = Series.FromValues("values", new long[] { 1, 2, 3 });

        series.DataType.Should().Be(DataType.Int64);
    }

    [Fact]
    public void Series_Float32_HasCorrectDataType()
    {
        var series = Series.FromValues("values", new float[] { 1.0f, 2.0f, 3.0f });

        series.DataType.Should().Be(DataType.Float32);
    }

    [Fact]
    public void Series_Float64_HasCorrectDataType()
    {
        var series = Series.FromValues("values", new double[] { 1.0, 2.0, 3.0 });

        series.DataType.Should().Be(DataType.Float64);
    }

    [Fact]
    public void Series_Boolean_HasCorrectDataType()
    {
        var series = Series.FromValues("values", new[] { true, false, true });

        series.DataType.Should().Be(DataType.Boolean);
    }

    [Fact]
    public void Series_String_HasCorrectDataType()
    {
        var series = Series.FromValues("values", new[] { "a", "b", "c" });

        series.DataType.Should().Be(DataType.String);
    }

    [Fact]
    public void Series_Int8_HasCorrectDataType()
    {
        var series = Series.FromValues("values", new sbyte[] { 1, 2, 3 });

        series.DataType.Should().Be(DataType.Int8);
    }

    [Fact]
    public void Series_Int16_HasCorrectDataType()
    {
        var series = Series.FromValues("values", new short[] { 1, 2, 3 });

        series.DataType.Should().Be(DataType.Int16);
    }

    [Fact]
    public void Series_UInt8_HasCorrectDataType()
    {
        var series = Series.FromValues("values", new byte[] { 1, 2, 3 });

        series.DataType.Should().Be(DataType.UInt8);
    }

    [Fact]
    public void Series_UInt16_HasCorrectDataType()
    {
        var series = Series.FromValues("values", new ushort[] { 1, 2, 3 });

        series.DataType.Should().Be(DataType.UInt16);
    }

    [Fact]
    public void Series_UInt32_HasCorrectDataType()
    {
        var series = Series.FromValues("values", new uint[] { 1, 2, 3 });

        series.DataType.Should().Be(DataType.UInt32);
    }

    [Fact]
    public void Series_UInt64_HasCorrectDataType()
    {
        var series = Series.FromValues("values", new ulong[] { 1, 2, 3 });

        series.DataType.Should().Be(DataType.UInt64);
    }

    // ============================================================================
    // DataFrame Schema Tests
    // ============================================================================

    [Fact]
    public void DataFrame_Schema_MatchesColumns()
    {
        var df = new DataFrame(
            Series.FromValues("int_col", new[] { 1, 2, 3 }),
            Series.FromValues("str_col", new[] { "a", "b", "c" }),
            Series.FromValues("float_col", new[] { 1.0, 2.0, 3.0 })
        );

        df.Schema.Should().NotBeNull();
        df.Schema.Count.Should().Be(3);
    }

    [Fact]
    public void DataFrame_Schema_HasCorrectTypes()
    {
        var df = new DataFrame(
            Series.FromValues("int_col", new[] { 1, 2, 3 }),
            Series.FromValues("str_col", new[] { "a", "b", "c" }),
            Series.FromValues("float_col", new[] { 1.0, 2.0, 3.0 })
        );

        // Schema is a list of (Name, Type) tuples
        df.Schema[0].Name.Should().Be("int_col");
        df.Schema[0].Type.Should().Be(DataType.Int32);
        df.Schema[1].Name.Should().Be("str_col");
        df.Schema[1].Type.Should().Be(DataType.String);
        df.Schema[2].Name.Should().Be("float_col");
        df.Schema[2].Type.Should().Be(DataType.Float64);
    }

    [Fact]
    public void DataFrame_GetColumnType_ReturnsCorrectType()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 }),
            Series.FromValues("b", new[] { "x", "y", "z" })
        );

        df["a"].DataType.Should().Be(DataType.Int32);
        df["b"].DataType.Should().Be(DataType.String);
    }

    // ============================================================================
    // AnyValue Type Tests
    // ============================================================================

    [Fact]
    public void AnyValue_From_Int32_WorksCorrectly()
    {
        var value = AnyValue.From(42);

        value.AsInt32().Should().Be(42);
    }

    [Fact]
    public void AnyValue_From_Int64_WorksCorrectly()
    {
        var value = AnyValue.From(42L);

        value.AsInt64().Should().Be(42L);
    }

    [Fact]
    public void AnyValue_From_Float64_WorksCorrectly()
    {
        var value = AnyValue.From(3.14);

        value.AsFloat64().Should().BeApproximately(3.14, 0.001);
    }

    [Fact]
    public void AnyValue_From_Float32_WorksCorrectly()
    {
        var value = AnyValue.From(3.14f);

        value.AsFloat32().Should().BeApproximately(3.14f, 0.001f);
    }

    [Fact]
    public void AnyValue_From_Boolean_WorksCorrectly()
    {
        var valueTrue = AnyValue.From(true);
        var valueFalse = AnyValue.From(false);

        valueTrue.AsBoolean().Should().BeTrue();
        valueFalse.AsBoolean().Should().BeFalse();
    }

    [Fact]
    public void AnyValue_From_String_WorksCorrectly()
    {
        var value = AnyValue.From("hello");

        value.AsString().Should().Be("hello");
    }

    [Fact]
    public void AnyValue_Null_WorksCorrectly()
    {
        var value = AnyValue.Null;

        value.IsNull.Should().BeTrue();
    }

    [Fact]
    public void AnyValue_InvalidCast_ThrowsException()
    {
        var value = AnyValue.From(42);

        var act = () => value.AsString();

        act.Should().Throw<InvalidCastException>();
    }

    // ============================================================================
    // Nullable Type Tests
    // ============================================================================

    [Fact]
    public void Series_NullableInt32_WorksCorrectly()
    {
        var series = Series.FromNullable("values", new int?[] { 1, null, 3 });

        series.DataType.Should().Be(DataType.Int32);
        series.Length.Should().Be(3);
        series[0].AsInt32().Should().Be(1);
        series.IsNull(1).Should().BeTrue();
        series[2].AsInt32().Should().Be(3);
    }

    [Fact]
    public void Series_NullableFloat64_WorksCorrectly()
    {
        var series = Series.FromNullable("values", new double?[] { 1.0, null, 3.0 });

        series.DataType.Should().Be(DataType.Float64);
        series.NullCount.Should().Be(1);
    }

    [Fact]
    public void Series_NullableBoolean_WorksCorrectly()
    {
        var series = Series.FromNullable("values", new bool?[] { true, null, false });

        series.DataType.Should().Be(DataType.Boolean);
        series.NullCount.Should().Be(1);
    }

    // ============================================================================
    // Type Inference Tests
    // ============================================================================

    [Fact]
    public void TypeInference_Int32Array_ReturnsInt32()
    {
        var series = Series.FromValues("test", new[] { 1, 2, 3, 4, 5 });

        series.DataType.Should().Be(DataType.Int32);
    }

    [Fact]
    public void TypeInference_DoubleArray_ReturnsFloat64()
    {
        var series = Series.FromValues("test", new[] { 1.0, 2.0, 3.0 });

        series.DataType.Should().Be(DataType.Float64);
    }

    [Fact]
    public void TypeInference_StringArray_ReturnsString()
    {
        var series = Series.FromValues("test", new[] { "a", "b", "c" });

        series.DataType.Should().Be(DataType.String);
    }

    // ============================================================================
    // DataType Comparison Tests
    // ============================================================================

    [Fact]
    public void DataType_Equality_WorksCorrectly()
    {
        DataType.Int32.Should().Be(DataType.Int32);
        DataType.Int32.Should().NotBe(DataType.Int64);
        DataType.Float64.Should().NotBe(DataType.Float32);
    }

    // ============================================================================
    // Schema Operations Tests
    // ============================================================================

    [Fact]
    public void DataFrame_Columns_ReturnsColumnNames()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1 }),
            Series.FromValues("b", new[] { 2 }),
            Series.FromValues("c", new[] { 3 })
        );

        df.Columns.Should().HaveCount(3);
        df.Columns.Should().Contain("a");
        df.Columns.Should().Contain("b");
        df.Columns.Should().Contain("c");
    }

    [Fact]
    public void DataFrame_Columns_PreservesOrder()
    {
        var df = new DataFrame(
            Series.FromValues("first", new[] { 1 }),
            Series.FromValues("second", new[] { 2 }),
            Series.FromValues("third", new[] { 3 })
        );

        df.Columns[0].Should().Be("first");
        df.Columns[1].Should().Be("second");
        df.Columns[2].Should().Be("third");
    }

    [Fact]
    public void DataFrame_Width_ReturnsColumnCount()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2 }),
            Series.FromValues("b", new[] { 3, 4 }),
            Series.FromValues("c", new[] { 5, 6 })
        );

        df.Width.Should().Be(3);
    }

    [Fact]
    public void DataFrame_Height_ReturnsRowCount()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 })
        );

        df.Height.Should().Be(5);
    }

    // ============================================================================
    // Edge Cases
    // ============================================================================

    [Fact]
    public void Series_EmptyArray_HasCorrectType()
    {
        var series = Series.FromValues("empty", Array.Empty<int>());

        series.DataType.Should().Be(DataType.Int32);
        series.Length.Should().Be(0);
    }

    [Fact]
    public void DataFrame_SingleColumn_HasCorrectSchema()
    {
        var df = new DataFrame(
            Series.FromValues("only_col", new[] { 1, 2, 3 })
        );

        df.Schema.Count.Should().Be(1);
        df.Schema[0].Name.Should().Be("only_col");
        df.Schema[0].Type.Should().Be(DataType.Int32);
    }

    [Fact]
    public void DataFrame_EmptyDataFrame_HasCorrectDimensions()
    {
        var df = new DataFrame(
            Series.FromValues("col", Array.Empty<int>())
        );

        df.Height.Should().Be(0);
        df.Width.Should().Be(1);
    }

    // ============================================================================
    // Special Numeric Values
    // ============================================================================

    [Fact]
    public void Series_Float64_WithNaN_Preserves()
    {
        var series = Series.FromValues("values", new[] { 1.0, double.NaN, 3.0 });

        double.IsNaN(series[1].AsFloat64()).Should().BeTrue();
    }

    [Fact]
    public void Series_Float64_WithInfinity_Preserves()
    {
        var series = Series.FromValues("values", new[] { double.NegativeInfinity, 0.0, double.PositiveInfinity });

        double.IsNegativeInfinity(series[0].AsFloat64()).Should().BeTrue();
        double.IsPositiveInfinity(series[2].AsFloat64()).Should().BeTrue();
    }

    [Fact]
    public void Series_Float32_WithNaN_Preserves()
    {
        var series = Series.FromValues("values", new[] { 1.0f, float.NaN, 3.0f });

        float.IsNaN(series[1].AsFloat32()).Should().BeTrue();
    }

    [Fact]
    public void Series_Float32_WithInfinity_Preserves()
    {
        var series = Series.FromValues("values", new[] { float.NegativeInfinity, 0.0f, float.PositiveInfinity });

        float.IsNegativeInfinity(series[0].AsFloat32()).Should().BeTrue();
        float.IsPositiveInfinity(series[2].AsFloat32()).Should().BeTrue();
    }

    // ============================================================================
    // Integer Range Tests
    // ============================================================================

    [Fact]
    public void Series_Int32_MaxValue_WorksCorrectly()
    {
        var series = Series.FromValues("values", new[] { int.MinValue, 0, int.MaxValue });

        series[0].AsInt32().Should().Be(int.MinValue);
        series[2].AsInt32().Should().Be(int.MaxValue);
    }

    [Fact]
    public void Series_Int64_MaxValue_WorksCorrectly()
    {
        var series = Series.FromValues("values", new[] { long.MinValue, 0L, long.MaxValue });

        series[0].AsInt64().Should().Be(long.MinValue);
        series[2].AsInt64().Should().Be(long.MaxValue);
    }

    [Fact]
    public void Series_UInt64_MaxValue_WorksCorrectly()
    {
        var series = Series.FromValues("values", new[] { ulong.MinValue, 0UL, ulong.MaxValue });

        series[0].AsUInt64().Should().Be(ulong.MinValue);
        series[2].AsUInt64().Should().Be(ulong.MaxValue);
    }

    // ============================================================================
    // String Edge Cases
    // ============================================================================

    [Fact]
    public void Series_String_EmptyStrings_WorksCorrectly()
    {
        var series = Series.FromValues("values", new[] { "", "a", "" });

        series[0].AsString().Should().Be("");
        series[1].AsString().Should().Be("a");
        series[2].AsString().Should().Be("");
    }

    [Fact]
    public void Series_String_WithSpaces_WorksCorrectly()
    {
        var series = Series.FromValues("values", new[] { "  ", " a ", "  b  " });

        series[0].AsString().Should().Be("  ");
        series[1].AsString().Should().Be(" a ");
    }

    [Fact]
    public void Series_String_Unicode_WorksCorrectly()
    {
        var series = Series.FromValues("values", new[] { "Hello", "世界", "🌍" });

        series[0].AsString().Should().Be("Hello");
        series[1].AsString().Should().Be("世界");
        series[2].AsString().Should().Be("🌍");
    }
}
