// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET
//
// Tests for type casting and conversion operations, inspired by Polars test suite.
// These tests cover:
// - Series type casting
// - AnyValue conversions
// - DataType checking
// - Implicit/explicit conversions

using FluentAssertions;
using Polaire;
using Polaire.DataTypes;
using Xunit;
using static Polaire.Pl;

namespace Polaire.Tests;

/// <summary>
/// Tests for type casting and conversion operations.
/// </summary>
public class CastingAndConversionTests
{
    // ============================================================================
    // Series Cast Tests
    // ============================================================================

    [Fact]
    public void Cast_Int32ToInt64_Works()
    {
        var s = Series.FromValues("s", new[] { 1, 2, 3 });

        var result = s.Cast(DataType.Int64);

        result.DataType.Should().Be(DataType.Int64);
        result[0].AsInt64().Should().Be(1);
        result[2].AsInt64().Should().Be(3);
    }

    [Fact]
    public void Cast_Int32ToFloat64_Works()
    {
        var s = Series.FromValues("s", new[] { 1, 2, 3 });

        var result = s.Cast(DataType.Float64);

        result.DataType.Should().Be(DataType.Float64);
        result[0].AsFloat64().Should().Be(1.0);
        result[2].AsFloat64().Should().Be(3.0);
    }

    [Fact]
    public void Cast_Int64ToFloat64_Works()
    {
        var s = Series.FromValues("s", new long[] { 1, 2, 3 });

        var result = s.Cast(DataType.Float64);

        result.DataType.Should().Be(DataType.Float64);
        result[0].AsFloat64().Should().Be(1.0);
    }

    [Fact]
    public void Cast_Float64ToInt32_Truncates()
    {
        var s = Series.FromValues("s", new[] { 1.9, 2.1, 3.5 });

        var result = s.Cast(DataType.Int32);

        result.DataType.Should().Be(DataType.Int32);
        result[0].AsInt32().Should().Be(1);
        result[1].AsInt32().Should().Be(2);
        result[2].AsInt32().Should().Be(3);
    }

    [Fact]
    public void Cast_Float64ToInt64_Truncates()
    {
        var s = Series.FromValues("s", new[] { 100.9, 200.1 });

        var result = s.Cast(DataType.Int64);

        result.DataType.Should().Be(DataType.Int64);
        result[0].AsInt64().Should().Be(100);
        result[1].AsInt64().Should().Be(200);
    }

    [Fact]
    public void Cast_Float32ToFloat64_Works()
    {
        var s = Series.FromValues("s", new float[] { 1.5f, 2.5f, 3.5f });

        var result = s.Cast(DataType.Float64);

        result.DataType.Should().Be(DataType.Float64);
        result[0].AsFloat64().Should().BeApproximately(1.5, 0.001);
    }

    [Fact]
    public void Cast_PreservesNull()
    {
        var s = Series.FromNullable("s", new int?[] { 1, null, 3 });

        var result = s.Cast(DataType.Float64);

        result.IsNull(1).Should().BeTrue();
        result[0].AsFloat64().Should().Be(1.0);
        result[2].AsFloat64().Should().Be(3.0);
    }

    [Fact]
    public void Cast_EmptySeries_Works()
    {
        var s = Series.FromValues("s", Array.Empty<int>());

        var result = s.Cast(DataType.Float64);

        result.DataType.Should().Be(DataType.Float64);
        result.Length.Should().Be(0);
    }

    [Fact]
    public void Cast_SingleElement_Works()
    {
        var s = Series.FromValues("s", new[] { 42 });

        var result = s.Cast(DataType.Float64);

        result.DataType.Should().Be(DataType.Float64);
        result[0].AsFloat64().Should().Be(42.0);
    }

    // ============================================================================
    // DataType Property Tests
    // ============================================================================

    [Fact]
    public void DataType_Int32_ReturnsCorrectType()
    {
        var s = Series.FromValues("s", new[] { 1, 2, 3 });

        s.DataType.Should().Be(DataType.Int32);
    }

    [Fact]
    public void DataType_Int64_ReturnsCorrectType()
    {
        var s = Series.FromValues("s", new long[] { 1, 2, 3 });

        s.DataType.Should().Be(DataType.Int64);
    }

    [Fact]
    public void DataType_Float32_ReturnsCorrectType()
    {
        var s = Series.FromValues("s", new float[] { 1.0f, 2.0f });

        s.DataType.Should().Be(DataType.Float32);
    }

    [Fact]
    public void DataType_Float64_ReturnsCorrectType()
    {
        var s = Series.FromValues("s", new[] { 1.0, 2.0 });

        s.DataType.Should().Be(DataType.Float64);
    }

    [Fact]
    public void DataType_String_ReturnsCorrectType()
    {
        var s = Series.FromValues("s", new[] { "a", "b" });

        s.DataType.Should().Be(DataType.String);
    }

    [Fact]
    public void DataType_Boolean_ReturnsCorrectType()
    {
        var s = Series.FromValues("s", new[] { true, false });

        s.DataType.Should().Be(DataType.Boolean);
    }

    // ============================================================================
    // AnyValue Conversion Tests
    // ============================================================================

    [Fact]
    public void AnyValue_AsInt32_Works()
    {
        var s = Series.FromValues("s", new[] { 42 });
        var value = s[0];

        value.AsInt32().Should().Be(42);
    }

    [Fact]
    public void AnyValue_AsInt64_Works()
    {
        var s = Series.FromValues("s", new long[] { 9999999999 });
        var value = s[0];

        value.AsInt64().Should().Be(9999999999);
    }

    [Fact]
    public void AnyValue_AsFloat64_Works()
    {
        var s = Series.FromValues("s", new[] { 3.14159 });
        var value = s[0];

        value.AsFloat64().Should().BeApproximately(3.14159, 0.00001);
    }

    [Fact]
    public void AnyValue_AsString_Works()
    {
        var s = Series.FromValues("s", new[] { "hello" });
        var value = s[0];

        value.AsString().Should().Be("hello");
    }

    [Fact]
    public void AnyValue_AsBoolean_Works()
    {
        var s = Series.FromValues("s", new[] { true });
        var value = s[0];

        value.AsBoolean().Should().BeTrue();
    }

    [Fact]
    public void AnyValue_IsNull_ForNullValue()
    {
        var s = Series.FromNullable("s", new int?[] { null });
        var value = s[0];

        value.IsNull.Should().BeTrue();
    }

    [Fact]
    public void AnyValue_IsNull_ForNonNullValue()
    {
        var s = Series.FromValues("s", new[] { 42 });
        var value = s[0];

        value.IsNull.Should().BeFalse();
    }

    [Fact]
    public void AnyValue_WrongTypeConversion_Throws()
    {
        var s = Series.FromValues("s", new[] { "hello" });
        var value = s[0];

        var act = () => value.AsInt32();

        act.Should().Throw<InvalidCastException>();
    }

    // ============================================================================
    // Negative Number Tests
    // ============================================================================

    [Fact]
    public void Cast_NegativeInt32ToFloat64_Works()
    {
        var s = Series.FromValues("s", new[] { -10, -20, -30 });

        var result = s.Cast(DataType.Float64);

        result[0].AsFloat64().Should().Be(-10.0);
        result[2].AsFloat64().Should().Be(-30.0);
    }

    [Fact]
    public void Cast_NegativeFloat64ToInt32_Truncates()
    {
        var s = Series.FromValues("s", new[] { -1.9, -2.1 });

        var result = s.Cast(DataType.Int32);

        result[0].AsInt32().Should().Be(-1);
        result[1].AsInt32().Should().Be(-2);
    }

    // ============================================================================
    // Large Value Tests
    // ============================================================================

    [Fact]
    public void Cast_LargeInt64ToFloat64_Works()
    {
        var s = Series.FromValues("s", new long[] { long.MaxValue / 2 });

        var result = s.Cast(DataType.Float64);

        result[0].AsFloat64().Should().BeGreaterThan(0);
    }

    [Fact]
    public void Cast_LargeArray_Works()
    {
        var values = Enumerable.Range(0, 10000).ToArray();
        var s = Series.FromValues("s", values);

        var result = s.Cast(DataType.Float64);

        result.Length.Should().Be(10000);
        result[9999].AsFloat64().Should().Be(9999.0);
    }

    // ============================================================================
    // Special Float Values Tests
    // ============================================================================

    [Fact]
    public void AnyValue_PositiveInfinity_Works()
    {
        var s = Series.FromValues("s", new[] { double.PositiveInfinity });
        var value = s[0];

        double.IsPositiveInfinity(value.AsFloat64()).Should().BeTrue();
    }

    [Fact]
    public void AnyValue_NegativeInfinity_Works()
    {
        var s = Series.FromValues("s", new[] { double.NegativeInfinity });
        var value = s[0];

        double.IsNegativeInfinity(value.AsFloat64()).Should().BeTrue();
    }

    [Fact]
    public void AnyValue_NaN_Works()
    {
        var s = Series.FromValues("s", new[] { double.NaN });
        var value = s[0];

        double.IsNaN(value.AsFloat64()).Should().BeTrue();
    }

    // ============================================================================
    // Zero and One Tests
    // ============================================================================

    [Fact]
    public void Cast_Zero_Works()
    {
        var s = Series.FromValues("s", new[] { 0 });

        var result = s.Cast(DataType.Float64);

        result[0].AsFloat64().Should().Be(0.0);
    }

    [Fact]
    public void Cast_One_Works()
    {
        var s = Series.FromValues("s", new[] { 1 });

        var result = s.Cast(DataType.Float64);

        result[0].AsFloat64().Should().Be(1.0);
    }

    [Fact]
    public void Cast_NegativeZero_Works()
    {
        var s = Series.FromValues("s", new[] { -0.0 });

        var result = s.Cast(DataType.Int32);

        result[0].AsInt32().Should().Be(0);
    }

    // ============================================================================
    // DataFrame Cast Tests
    // ============================================================================

    [Fact]
    public void DataFrame_ColumnCast_Works()
    {
        var df = new DataFrame(
            Series.FromValues("int_col", new[] { 1, 2, 3 })
        );

        var casted = df["int_col"].Cast(DataType.Float64);

        casted.DataType.Should().Be(DataType.Float64);
    }

    [Fact]
    public void DataFrame_MultipleColumns_DifferentTypes()
    {
        var df = new DataFrame(
            Series.FromValues("int_col", new[] { 1, 2 }),
            Series.FromValues("float_col", new[] { 1.5, 2.5 }),
            Series.FromValues("str_col", new[] { "a", "b" })
        );

        df["int_col"].DataType.Should().Be(DataType.Int32);
        df["float_col"].DataType.Should().Be(DataType.Float64);
        df["str_col"].DataType.Should().Be(DataType.String);
    }

    // ============================================================================
    // All Null Tests
    // ============================================================================

    [Fact]
    public void Cast_AllNulls_Works()
    {
        var s = Series.FromNullable("s", new int?[] { null, null, null });

        var result = s.Cast(DataType.Float64);

        result.DataType.Should().Be(DataType.Float64);
        result.IsNull(0).Should().BeTrue();
        result.IsNull(1).Should().BeTrue();
        result.IsNull(2).Should().BeTrue();
    }

    // ============================================================================
    // Boolean Conversion Tests
    // Note: Boolean -> numeric casts are not currently supported
    // ============================================================================

    [Fact]
    public void Boolean_Series_DataType()
    {
        var s = Series.FromValues("s", new[] { true, false, true });

        s.DataType.Should().Be(DataType.Boolean);
        s.Length.Should().Be(3);
    }

    [Fact]
    public void Boolean_Values_CanBeRead()
    {
        var s = Series.FromValues("s", new[] { true, false, true });

        s[0].AsBoolean().Should().BeTrue();
        s[1].AsBoolean().Should().BeFalse();
        s[2].AsBoolean().Should().BeTrue();
    }

    [Fact]
    public void Boolean_CastToString_Works()
    {
        var s = Series.FromValues("s", new[] { true, false });

        var result = s.Cast(DataType.String);

        result.DataType.Should().Be(DataType.String);
        // String representation of booleans
        result[0].AsString().Should().BeOneOf("True", "true", "1");
        result[1].AsString().Should().BeOneOf("False", "false", "0");
    }
}
