// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using FluentAssertions;
using Polaire.DataTypes;

namespace Polaire.Tests;

public class DataTypeTests
{
    [Fact]
    public void DataType_Int32_ShouldBeNumeric()
    {
        DataType.Int32.IsNumeric.Should().BeTrue();
        DataType.Int32.IsInteger.Should().BeTrue();
        DataType.Int32.IsFloat.Should().BeFalse();
        DataType.Int32.PhysicalSize.Should().Be(4);
    }

    [Fact]
    public void DataType_Float64_ShouldBeNumeric()
    {
        DataType.Float64.IsNumeric.Should().BeTrue();
        DataType.Float64.IsFloat.Should().BeTrue();
        DataType.Float64.IsInteger.Should().BeFalse();
        DataType.Float64.PhysicalSize.Should().Be(8);
    }

    [Fact]
    public void DataType_String_ShouldNotBeNumeric()
    {
        DataType.String.IsNumeric.Should().BeFalse();
        DataType.String.PhysicalSize.Should().Be(-1); // Variable size
    }

    [Fact]
    public void DataType_List_ShouldBeNested()
    {
        var listType = DataType.List(DataType.Int32);
        listType.IsNested.Should().BeTrue();
        listType.ToString().Should().Be("List(Int32)");
    }

    [Fact]
    public void DataType_DateTime_ShouldBeTemporal()
    {
        var dtType = DataType.DateTime(TimeUnit.Nanoseconds, "UTC");
        dtType.IsTemporal.Should().BeTrue();
        dtType.ToString().Should().Be("DateTime(Nanoseconds, UTC)");
    }

    [Fact]
    public void DataType_Decimal_ShouldHavePrecisionAndScale()
    {
        var decType = DataType.Decimal(38, 9);
        decType.Precision.Should().Be(38);
        decType.Scale.Should().Be(9);
        decType.ToString().Should().Be("Decimal(38, 9)");
    }

    [Fact]
    public void AnyValue_Int32_ShouldStoreCorrectly()
    {
        var value = AnyValue.From(42);
        value.Kind.Should().Be(AnyValueKind.Int32);
        value.AsInt32().Should().Be(42);
        value.IsNull.Should().BeFalse();
    }

    [Fact]
    public void AnyValue_Null_ShouldBeNull()
    {
        var value = AnyValue.Null;
        value.IsNull.Should().BeTrue();
        value.Kind.Should().Be(AnyValueKind.Null);
    }

    [Fact]
    public void AnyValue_Float64_ShouldHandleNaN()
    {
        var nan1 = AnyValue.From(double.NaN);
        var nan2 = AnyValue.From(double.NaN);

        // Polars semantics: NaN == NaN
        nan1.Equals(nan2).Should().BeTrue();

        // NaN > non-NaN
        var normal = AnyValue.From(1.0);
        nan1.CompareTo(normal).Should().BeGreaterThan(0);
    }

    [Fact]
    public void AnyValue_Comparison_ShouldWorkCorrectly()
    {
        var a = AnyValue.From(10);
        var b = AnyValue.From(20);
        var c = AnyValue.From(10);

        (a < b).Should().BeTrue();
        (a == c).Should().BeTrue();
        (b > a).Should().BeTrue();
    }

    [Fact]
    public void AnyValue_StringComparison_ShouldWorkCorrectly()
    {
        var a = AnyValue.From("apple");
        var b = AnyValue.From("banana");
        var c = AnyValue.From("apple");

        (a < b).Should().BeTrue();
        (a == c).Should().BeTrue();
    }

    [Fact]
    public void AnyValue_Cast_ShouldConvertTypes()
    {
        var intVal = AnyValue.From(42);
        var floatVal = intVal.Cast(DataType.Float64);

        floatVal.Kind.Should().Be(AnyValueKind.Float64);
        floatVal.AsFloat64().Should().Be(42.0);
    }

    [Fact]
    public void AnyValue_DateOnly_ShouldStoreCorrectly()
    {
        var date = new DateOnly(2024, 1, 15);
        var value = AnyValue.From(date);

        value.Kind.Should().Be(AnyValueKind.Date);
        value.AsDate().Should().Be(date);
    }

    [Fact]
    public void AnyValue_ImplicitConversion_ShouldWork()
    {
        AnyValue fromInt = 42;
        AnyValue fromDouble = 3.14;
        AnyValue fromString = "hello";
        AnyValue fromBool = true;

        fromInt.AsInt32().Should().Be(42);
        fromDouble.AsFloat64().Should().Be(3.14);
        fromString.AsString().Should().Be("hello");
        fromBool.AsBoolean().Should().BeTrue();
    }
}
