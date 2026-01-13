// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using FluentAssertions;
using Polaire;
using Polaire.DataTypes;
using Xunit;
using static Polaire.Pl;

namespace Polaire.Tests;

/// <summary>
/// Tests for Series construction and manipulation.
/// </summary>
public class SeriesConstructionTests
{
    // ============================================================================
    // FromValues Construction Tests
    // ============================================================================

    [Fact]
    public void FromValues_IntArray_CreatesCorrectSeries()
    {
        var series = Series.FromValues("int_col", new[] { 1, 2, 3, 4, 5 });

        series.Name.Should().Be("int_col");
        series.Length.Should().Be(5);
        series[0].AsInt32().Should().Be(1);
        series[4].AsInt32().Should().Be(5);
    }

    [Fact]
    public void FromValues_DoubleArray_CreatesCorrectSeries()
    {
        var series = Series.FromValues("float_col", new[] { 1.1, 2.2, 3.3 });

        series.Name.Should().Be("float_col");
        series.Length.Should().Be(3);
        series.DataType.Should().Be(DataType.Float64);
    }

    [Fact]
    public void FromValues_StringArray_CreatesCorrectSeries()
    {
        var series = Series.FromValues("str_col", new[] { "a", "b", "c" });

        series.Name.Should().Be("str_col");
        series.Length.Should().Be(3);
        series[0].AsString().Should().Be("a");
    }

    [Fact]
    public void FromValues_BoolArray_CreatesCorrectSeries()
    {
        var series = Series.FromValues("bool_col", new[] { true, false, true });

        series.DataType.Should().Be(DataType.Boolean);
        series[0].AsBoolean().Should().BeTrue();
        series[1].AsBoolean().Should().BeFalse();
    }

    [Fact]
    public void FromValues_EmptyArray_CreatesEmptySeries()
    {
        var series = Series.FromValues("empty", Array.Empty<int>());

        series.Length.Should().Be(0);
        series.NullCount.Should().Be(0);
    }

    [Fact]
    public void FromValues_SingleElement_CreatesSingleElementSeries()
    {
        var series = Series.FromValues("single", new[] { 42 });

        series.Length.Should().Be(1);
        series[0].AsInt32().Should().Be(42);
    }

    // ============================================================================
    // FromNullable Construction Tests
    // ============================================================================

    [Fact]
    public void FromNullable_WithNulls_CreatesCorrectSeries()
    {
        var series = Series.FromNullable("nullable", new int?[] { 1, null, 3, null, 5 });

        series.Length.Should().Be(5);
        series.NullCount.Should().Be(2);
        series[0].AsInt32().Should().Be(1);
        series.IsNull(1).Should().BeTrue();
        series[2].AsInt32().Should().Be(3);
    }

    [Fact]
    public void FromNullable_AllNulls_CreatesAllNullSeries()
    {
        var series = Series.FromNullable("all_null", new int?[] { null, null, null });

        series.Length.Should().Be(3);
        series.NullCount.Should().Be(3);
        series.IsNull(0).Should().BeTrue();
        series.IsNull(1).Should().BeTrue();
        series.IsNull(2).Should().BeTrue();
    }

    [Fact]
    public void FromNullable_NoNulls_CreatesNonNullSeries()
    {
        var series = Series.FromNullable("no_null", new int?[] { 1, 2, 3 });

        series.Length.Should().Be(3);
        series.NullCount.Should().Be(0);
        series.HasNulls.Should().BeFalse();
    }

    [Fact]
    public void FromNullable_Double_WithNulls_WorksCorrectly()
    {
        var series = Series.FromNullable("nullable_float", new double?[] { 1.1, null, 3.3 });

        series.DataType.Should().Be(DataType.Float64);
        series.NullCount.Should().Be(1);
        series[0].AsFloat64().Should().BeApproximately(1.1, 0.001);
    }

    [Fact]
    public void FromNullable_FirstNullThenValues_WorksCorrectly()
    {
        var series = Series.FromNullable("nullable", new int?[] { null, 2, 3 });

        series.DataType.Should().Be(DataType.Int32);
        series.NullCount.Should().Be(1);
        series[1].AsInt32().Should().Be(2);
    }

    // ============================================================================
    // Series Name Tests
    // ============================================================================

    [Fact]
    public void Series_Name_CanBeChanged()
    {
        var series = Series.FromValues("original", new[] { 1, 2, 3 });

        var renamed = series.Rename("new_name");

        renamed.Name.Should().Be("new_name");
    }

    [Fact]
    public void Series_Rename_PreservesData()
    {
        var series = Series.FromValues("original", new[] { 1, 2, 3 });

        var renamed = series.Rename("new_name");

        renamed.Length.Should().Be(3);
        renamed[0].AsInt32().Should().Be(1);
    }

    [Fact]
    public void Series_Rename_ReturnsDifferentInstance()
    {
        var series = Series.FromValues("original", new[] { 1, 2, 3 });

        var renamed = series.Rename("renamed");

        renamed.Should().NotBeSameAs(series);
        series.Name.Should().Be("original");  // Original unchanged
        renamed.Name.Should().Be("renamed");
    }

    // ============================================================================
    // Series Indexing Tests
    // ============================================================================

    [Fact]
    public void Series_Indexer_ReturnsCorrectValue()
    {
        var series = Series.FromValues("test", new[] { 10, 20, 30, 40, 50 });

        series[0].AsInt32().Should().Be(10);
        series[2].AsInt32().Should().Be(30);
        series[4].AsInt32().Should().Be(50);
    }

    [Fact]
    public void Series_Indexer_NullValue_ReturnsNull()
    {
        var series = Series.FromNullable("test", new int?[] { 1, null, 3 });

        series[1].IsNull.Should().BeTrue();
    }

    // ============================================================================
    // Series Properties Tests
    // ============================================================================

    [Fact]
    public void Series_Length_ReturnsCount()
    {
        var series = Series.FromValues("test", new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });

        series.Length.Should().Be(10);
    }

    [Fact]
    public void Series_NullCount_ReturnsNullCount()
    {
        var series = Series.FromNullable("test", new int?[] { 1, null, 3, null, 5, null });

        series.NullCount.Should().Be(3);
    }

    [Fact]
    public void Series_HasNulls_ReturnsCorrectValue()
    {
        var withNulls = Series.FromNullable("with_null", new int?[] { 1, null, 3 });
        var noNulls = Series.FromValues("no_null", new[] { 1, 2, 3 });

        withNulls.HasNulls.Should().BeTrue();
        noNulls.HasNulls.Should().BeFalse();
    }

    // ============================================================================
    // Series Rename With Same Data Tests
    // ============================================================================

    [Fact]
    public void Series_Rename_PreservesDataType()
    {
        var original = Series.FromValues("test", new[] { 1, 2, 3 });

        var renamed = original.Rename("renamed");

        renamed.DataType.Should().Be(original.DataType);
        renamed.Length.Should().Be(original.Length);
        renamed[0].AsInt32().Should().Be(original[0].AsInt32());
    }

    // ============================================================================
    // Series to Array Tests
    // ============================================================================

    [Fact]
    public void Series_ToArray_ReturnsCorrectArray()
    {
        var series = Series.FromValues("test", new[] { 1, 2, 3 });

        var array = series.ToArray<int>();

        array.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void Series_ToArray_Double_ReturnsCorrectArray()
    {
        var series = Series.FromValues("test", new[] { 1.1, 2.2, 3.3 });

        var array = series.ToArray<double>();

        array[0].Should().BeApproximately(1.1, 0.001);
        array[1].Should().BeApproximately(2.2, 0.001);
    }

    // ============================================================================
    // Series Enumeration Tests
    // ============================================================================

    [Fact]
    public void Series_Enumeration_IteratesAllValues()
    {
        var series = Series.FromValues("test", new[] { 1, 2, 3, 4, 5 });
        var values = new List<int>();

        foreach (var value in series)
        {
            if (!value.IsNull)
            {
                values.Add(value.AsInt32());
            }
        }

        values.Should().BeEquivalentTo(new[] { 1, 2, 3, 4, 5 });
    }

    // ============================================================================
    // Series Factory Methods (Pl class)
    // ============================================================================

    [Fact]
    public void Pl_Series_CreatesSeriesFromArray()
    {
        var series = Series("test", new[] { 1, 2, 3 });

        series.Name.Should().Be("test");
        series.Length.Should().Be(3);
    }

    // ============================================================================
    // Large Series Tests
    // ============================================================================

    [Fact]
    public void Series_LargeArray_CreatesCorrectly()
    {
        var size = 100000;
        var data = Enumerable.Range(0, size).ToArray();
        var series = Series.FromValues("large", data);

        series.Length.Should().Be(size);
        series[0].AsInt32().Should().Be(0);
        series[size - 1].AsInt32().Should().Be(size - 1);
    }

    [Fact]
    public void Series_LargeArray_WithNulls_CreatesCorrectly()
    {
        var size = 10000;
        var data = new int?[size];
        for (int i = 0; i < size; i++)
        {
            data[i] = i % 2 == 0 ? i : null;
        }

        var series = Series.FromNullable("large_nullable", data);

        series.Length.Should().Be(size);
        series.NullCount.Should().Be(size / 2);
    }

    // ============================================================================
    // Special Character Names
    // ============================================================================

    [Fact]
    public void Series_SpecialCharacterName_WorksCorrectly()
    {
        var series = Series.FromValues("column with spaces", new[] { 1, 2, 3 });

        series.Name.Should().Be("column with spaces");
    }

    [Fact]
    public void Series_UnicodeName_WorksCorrectly()
    {
        var series = Series.FromValues("列名", new[] { 1, 2, 3 });

        series.Name.Should().Be("列名");
    }

    [Fact]
    public void Series_EmptyName_WorksCorrectly()
    {
        var series = Series.FromValues("", new[] { 1, 2, 3 });

        series.Name.Should().Be("");
    }

    // ============================================================================
    // Edge Cases
    // ============================================================================

    [Fact]
    public void Series_MaxIntValues_PreservesPrecision()
    {
        var series = Series.FromValues("max_int", new[] { int.MinValue, int.MaxValue });

        series[0].AsInt32().Should().Be(int.MinValue);
        series[1].AsInt32().Should().Be(int.MaxValue);
    }

    [Fact]
    public void Series_MaxLongValues_PreservesPrecision()
    {
        var series = Series.FromValues("max_long", new[] { long.MinValue, long.MaxValue });

        series[0].AsInt64().Should().Be(long.MinValue);
        series[1].AsInt64().Should().Be(long.MaxValue);
    }

    [Fact]
    public void Series_SpecialDoubleValues_PreservesPrecision()
    {
        var series = Series.FromValues("special", new[] { double.MinValue, double.Epsilon, double.MaxValue });

        series[0].AsFloat64().Should().Be(double.MinValue);
        series[1].AsFloat64().Should().Be(double.Epsilon);
        series[2].AsFloat64().Should().Be(double.MaxValue);
    }

    [Fact]
    public void Series_NaN_PreservesNaN()
    {
        var series = Series.FromValues("nan", new[] { double.NaN, 1.0, double.NaN });

        double.IsNaN(series[0].AsFloat64()).Should().BeTrue();
        double.IsNaN(series[2].AsFloat64()).Should().BeTrue();
    }

    [Fact]
    public void Series_Infinity_PreservesInfinity()
    {
        var series = Series.FromValues("inf", new[] { double.NegativeInfinity, 0.0, double.PositiveInfinity });

        double.IsNegativeInfinity(series[0].AsFloat64()).Should().BeTrue();
        double.IsPositiveInfinity(series[2].AsFloat64()).Should().BeTrue();
    }

    // ============================================================================
    // Type Specific Construction
    // ============================================================================

    [Fact]
    public void Series_Byte_CreatesCorrectly()
    {
        var series = Series.FromValues("bytes", new byte[] { 0, 127, 255 });

        series.DataType.Should().Be(DataType.UInt8);
        series[0].AsUInt8().Should().Be(0);
        series[2].AsUInt8().Should().Be(255);
    }

    [Fact]
    public void Series_SByte_CreatesCorrectly()
    {
        var series = Series.FromValues("sbytes", new sbyte[] { -128, 0, 127 });

        series.DataType.Should().Be(DataType.Int8);
    }

    [Fact]
    public void Series_Short_CreatesCorrectly()
    {
        var series = Series.FromValues("shorts", new short[] { -32768, 0, 32767 });

        series.DataType.Should().Be(DataType.Int16);
    }

    [Fact]
    public void Series_UShort_CreatesCorrectly()
    {
        var series = Series.FromValues("ushorts", new ushort[] { 0, 1, 65535 });

        series.DataType.Should().Be(DataType.UInt16);
    }

    [Fact]
    public void Series_UInt_CreatesCorrectly()
    {
        var series = Series.FromValues("uints", new uint[] { 0, 1, uint.MaxValue });

        series.DataType.Should().Be(DataType.UInt32);
    }

    [Fact]
    public void Series_ULong_CreatesCorrectly()
    {
        var series = Series.FromValues("ulongs", new ulong[] { 0, 1, ulong.MaxValue });

        series.DataType.Should().Be(DataType.UInt64);
    }
}
