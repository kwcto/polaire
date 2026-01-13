// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using FluentAssertions;
using Polaire;
using Polaire.DataTypes;
using Xunit;

namespace Polaire.Tests;

/// <summary>
/// Tests for casting and type conversion operations.
/// </summary>
public class CastingConversionTests
{
    // ============================================================================
    // Int32 to Other Types Tests
    // ============================================================================

    [Fact]
    public void Cast_Int32ToInt64_WorksCorrectly()
    {
        var series = Series.FromValues("values", new[] { 1, 2, 3 });

        var result = series.Cast(DataType.Int64);

        result.DataType.Should().Be(DataType.Int64);
        result[0].AsInt64().Should().Be(1);
        result[1].AsInt64().Should().Be(2);
        result[2].AsInt64().Should().Be(3);
    }

    [Fact]
    public void Cast_Int32ToFloat64_WorksCorrectly()
    {
        var series = Series.FromValues("values", new[] { 1, 2, 3 });

        var result = series.Cast(DataType.Float64);

        result.DataType.Should().Be(DataType.Float64);
        result[0].AsFloat64().Should().Be(1.0);
        result[1].AsFloat64().Should().Be(2.0);
        result[2].AsFloat64().Should().Be(3.0);
    }

    [Fact]
    public void Cast_Int32ToString_WorksCorrectly()
    {
        var series = Series.FromValues("values", new[] { 1, 42, 100 });

        var result = series.Cast(DataType.String);

        result.DataType.Should().Be(DataType.String);
        result[0].AsString().Should().Be("1");
        result[1].AsString().Should().Be("42");
        result[2].AsString().Should().Be("100");
    }

    // ============================================================================
    // Float64 to Other Types Tests
    // ============================================================================

    [Fact]
    public void Cast_Float64ToInt32_Truncates()
    {
        var series = Series.FromValues("values", new[] { 1.5, 2.9, 3.1 });

        var result = series.Cast(DataType.Int32);

        result.DataType.Should().Be(DataType.Int32);
        result[0].AsInt32().Should().Be(1);
        result[1].AsInt32().Should().Be(2);
        result[2].AsInt32().Should().Be(3);
    }

    [Fact]
    public void Cast_Float64ToInt64_Truncates()
    {
        var series = Series.FromValues("values", new[] { 1.9, 2.1, 3.5 });

        var result = series.Cast(DataType.Int64);

        result.DataType.Should().Be(DataType.Int64);
        result[0].AsInt64().Should().Be(1);
        result[1].AsInt64().Should().Be(2);
        result[2].AsInt64().Should().Be(3);
    }

    [Fact]
    public void Cast_Float64ToString_WorksCorrectly()
    {
        var series = Series.FromValues("values", new[] { 1.5, 2.25, 3.0 });

        var result = series.Cast(DataType.String);

        result.DataType.Should().Be(DataType.String);
        result[0].AsString().Should().Contain("1.5");
        result[1].AsString().Should().Contain("2.25");
        result[2].AsString().Should().Contain("3");
    }

    [Fact]
    public void Cast_Float64ToFloat32_ReducesPrecision()
    {
        var series = Series.FromValues("values", new[] { 1.5, 2.25, 3.0 });

        var result = series.Cast(DataType.Float32);

        result.DataType.Should().Be(DataType.Float32);
        result[0].AsFloat32().Should().BeApproximately(1.5f, 0.001f);
        result[1].AsFloat32().Should().BeApproximately(2.25f, 0.001f);
        result[2].AsFloat32().Should().BeApproximately(3.0f, 0.001f);
    }

    // ============================================================================
    // String to Other Types Tests
    // Note: String parsing is not yet implemented
    // ============================================================================

    // ============================================================================
    // Boolean Conversions Tests
    // ============================================================================

    [Fact]
    public void Cast_BoolToInt32_ReturnsZeroOrOne()
    {
        var series = Series.FromValues("values", new[] { true, false, true });

        var result = series.Cast(DataType.Int32);

        result.DataType.Should().Be(DataType.Int32);
        result[0].AsInt32().Should().Be(1);
        result[1].AsInt32().Should().Be(0);
        result[2].AsInt32().Should().Be(1);
    }

    [Fact]
    public void Cast_BoolToString_ReturnsTrueOrFalse()
    {
        var series = Series.FromValues("values", new[] { true, false });

        var result = series.Cast(DataType.String);

        result.DataType.Should().Be(DataType.String);
        result[0].AsString().Should().BeOneOf("True", "true", "1");
        result[1].AsString().Should().BeOneOf("False", "false", "0");
    }

    // ============================================================================
    // Null Handling Tests
    // ============================================================================

    [Fact]
    public void Cast_WithNulls_PreservesNulls()
    {
        var series = Series.FromNullable("values", new int?[] { 1, null, 3 });

        var result = series.Cast(DataType.Float64);

        result.NullCount.Should().Be(1);
        result.IsNull(1).Should().BeTrue();
        result[0].AsFloat64().Should().Be(1.0);
        result[2].AsFloat64().Should().Be(3.0);
    }

    // ============================================================================
    // Same Type Cast Tests
    // ============================================================================

    [Fact]
    public void Cast_SameType_ReturnsEquivalent()
    {
        var series = Series.FromValues("values", new[] { 1, 2, 3 });

        var result = series.Cast(DataType.Int32);

        result.DataType.Should().Be(DataType.Int32);
        result[0].AsInt32().Should().Be(1);
        result[1].AsInt32().Should().Be(2);
        result[2].AsInt32().Should().Be(3);
    }

    // ============================================================================
    // Edge Cases
    // ============================================================================

    [Fact]
    public void Cast_EmptySeries_ReturnsEmpty()
    {
        var series = Series.FromValues("values", Array.Empty<int>());

        var result = series.Cast(DataType.Float64);

        result.Length.Should().Be(0);
        result.DataType.Should().Be(DataType.Float64);
    }

    [Fact]
    public void Cast_SingleElement_WorksCorrectly()
    {
        var series = Series.FromValues("values", new[] { 42 });

        var result = series.Cast(DataType.Float64);

        result.Length.Should().Be(1);
        result[0].AsFloat64().Should().Be(42.0);
    }

    [Fact]
    public void Cast_LargeSeries_WorksCorrectly()
    {
        var size = 10000;
        var series = Series.FromValues("values", Enumerable.Range(0, size).ToArray());

        var result = series.Cast(DataType.Float64);

        result.Length.Should().Be(size);
        result[0].AsFloat64().Should().Be(0.0);
        result[9999].AsFloat64().Should().Be(9999.0);
    }

    [Fact]
    public void Cast_NegativeIntegers_WorksCorrectly()
    {
        var series = Series.FromValues("values", new[] { -1, -100, -1000 });

        var result = series.Cast(DataType.Float64);

        result[0].AsFloat64().Should().Be(-1.0);
        result[1].AsFloat64().Should().Be(-100.0);
        result[2].AsFloat64().Should().Be(-1000.0);
    }

    // ============================================================================
    // Int Type Family Tests
    // ============================================================================

    [Fact]
    public void Cast_Int64ToInt32_WorksCorrectly()
    {
        var series = Series.FromValues("values", new long[] { 1, 2, 3 });

        var result = series.Cast(DataType.Int32);

        result.DataType.Should().Be(DataType.Int32);
        result[0].AsInt32().Should().Be(1);
    }

    // Note: Int8, Int16, UInt8, UInt16, UInt32 casts not yet implemented

    // ============================================================================
    // Float Type Family Tests
    // ============================================================================

    [Fact]
    public void Cast_Float32ToFloat64_WorksCorrectly()
    {
        var series = Series.FromValues("values", new float[] { 1.5f, 2.25f, 3.0f });

        var result = series.Cast(DataType.Float64);

        result.DataType.Should().Be(DataType.Float64);
        result[0].AsFloat64().Should().BeApproximately(1.5, 0.001);
        result[1].AsFloat64().Should().BeApproximately(2.25, 0.001);
        result[2].AsFloat64().Should().BeApproximately(3.0, 0.001);
    }

    // ============================================================================
    // Special Float Values Tests
    // ============================================================================

    [Fact]
    public void Cast_FloatWithNaN_PreservesNaN()
    {
        var series = Series.FromValues("values", new[] { 1.0, double.NaN, 3.0 });

        var result = series.Cast(DataType.Float32);

        result.DataType.Should().Be(DataType.Float32);
        float.IsNaN(result[1].AsFloat32()).Should().BeTrue();
    }

    [Fact]
    public void Cast_FloatWithInfinity_PreservesInfinity()
    {
        var series = Series.FromValues("values", new[] { double.PositiveInfinity, double.NegativeInfinity });

        var result = series.Cast(DataType.Float32);

        result.DataType.Should().Be(DataType.Float32);
        float.IsPositiveInfinity(result[0].AsFloat32()).Should().BeTrue();
        float.IsNegativeInfinity(result[1].AsFloat32()).Should().BeTrue();
    }
}
