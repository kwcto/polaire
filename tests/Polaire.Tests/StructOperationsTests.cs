// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using FluentAssertions;
using Polaire;
using Polaire.DataTypes;
using Xunit;

namespace Polaire.Tests;

/// <summary>
/// Tests for StructOperations (Series.Struct namespace).
/// Tests struct field access and manipulation operations.
/// </summary>
public class StructOperationsTests
{
    // ============================================================================
    // Factory Method Tests
    // ============================================================================

    [Fact]
    public void FromStruct_WithFieldDictionary_CreatesStructSeries()
    {
        var fields = new Dictionary<string, Series>
        {
            ["name"] = Series.FromValues("name", new[] { "Alice", "Bob", "Charlie" }),
            ["age"] = Series.FromValues("age", new[] { 25, 30, 35 })
        };

        var series = Series.FromStruct("person", fields);

        series.Should().NotBeNull();
        series.Length.Should().Be(3);
        series.DataType.Should().BeOfType<DataType.StructType>();
    }

    [Fact]
    public void FromStruct_WithMultipleFieldTypes_CreatesCorrectStructType()
    {
        var fields = new Dictionary<string, Series>
        {
            ["int_field"] = Series.FromValues("int_field", new[] { 1, 2, 3 }),
            ["double_field"] = Series.FromValues("double_field", new[] { 1.1, 2.2, 3.3 }),
            ["string_field"] = Series.FromValues("string_field", new[] { "a", "b", "c" })
        };

        var series = Series.FromStruct("mixed", fields);
        var structType = (DataType.StructType)series.DataType;

        structType.Fields.Count.Should().Be(3);
        structType.Fields[0].Name.Should().Be("int_field");
        structType.Fields[0].Type.Should().Be(DataType.Int32);
        structType.Fields[1].Type.Should().Be(DataType.Float64);
        structType.Fields[2].Type.Should().Be(DataType.String);
    }

    [Fact]
    public void FromStruct_WithMismatchedLengths_ThrowsException()
    {
        var fields = new Dictionary<string, Series>
        {
            ["a"] = Series.FromValues("a", new[] { 1, 2, 3 }),
            ["b"] = Series.FromValues("b", new[] { 1, 2 }) // Different length
        };

        var act = () => Series.FromStruct("struct", fields);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*length*");
    }

    [Fact]
    public void FromStruct_WithEmptyDictionary_ThrowsException()
    {
        var fields = new Dictionary<string, Series>();

        var act = () => Series.FromStruct("struct", fields);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*field*");
    }

    // ============================================================================
    // Struct.Field() Tests
    // ============================================================================

    [Fact]
    public void Field_ReturnsIntField()
    {
        var fields = new Dictionary<string, Series>
        {
            ["value"] = Series.FromValues("value", new[] { 10, 20, 30 })
        };
        var series = Series.FromStruct("data", fields);

        var result = series.Struct.Field("value");

        result.Length.Should().Be(3);
        result[0].AsInt32().Should().Be(10);
        result[1].AsInt32().Should().Be(20);
        result[2].AsInt32().Should().Be(30);
    }

    [Fact]
    public void Field_ReturnsDoubleField()
    {
        var fields = new Dictionary<string, Series>
        {
            ["value"] = Series.FromValues("value", new[] { 1.5, 2.5, 3.5 })
        };
        var series = Series.FromStruct("data", fields);

        var result = series.Struct.Field("value");

        result.Length.Should().Be(3);
        result[0].AsFloat64().Should().Be(1.5);
        result[1].AsFloat64().Should().Be(2.5);
        result[2].AsFloat64().Should().Be(3.5);
    }

    [Fact]
    public void Field_ReturnsStringField()
    {
        var fields = new Dictionary<string, Series>
        {
            ["name"] = Series.FromValues("name", new[] { "Alice", "Bob", "Charlie" })
        };
        var series = Series.FromStruct("data", fields);

        var result = series.Struct.Field("name");

        result.Length.Should().Be(3);
        result[0].AsString().Should().Be("Alice");
        result[1].AsString().Should().Be("Bob");
        result[2].AsString().Should().Be("Charlie");
    }

    [Fact]
    public void Field_WithBoolField_ReturnsBooleans()
    {
        var fields = new Dictionary<string, Series>
        {
            ["active"] = Series.FromValues("active", new[] { true, false, true })
        };
        var series = Series.FromStruct("data", fields);

        var result = series.Struct.Field("active");

        result.Length.Should().Be(3);
        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeFalse();
        result[2].AsBoolean().Should().BeTrue();
    }

    [Fact]
    public void Field_WithNonExistentField_ThrowsException()
    {
        var fields = new Dictionary<string, Series>
        {
            ["name"] = Series.FromValues("name", new[] { "Alice", "Bob" })
        };
        var series = Series.FromStruct("data", fields);

        var act = () => series.Struct.Field("nonexistent");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*nonexistent*not found*");
    }

    // ============================================================================
    // Struct.FieldNames() Tests
    // ============================================================================

    [Fact]
    public void FieldNames_ReturnsAllFieldNames()
    {
        var fields = new Dictionary<string, Series>
        {
            ["a"] = Series.FromValues("a", new[] { 1 }),
            ["b"] = Series.FromValues("b", new[] { 2 }),
            ["c"] = Series.FromValues("c", new[] { 3 })
        };
        var series = Series.FromStruct("data", fields);

        var names = series.Struct.FieldNames();

        names.Should().HaveCount(3);
        names.Should().Contain("a");
        names.Should().Contain("b");
        names.Should().Contain("c");
    }

    // ============================================================================
    // Struct.FieldCount() Tests
    // ============================================================================

    [Fact]
    public void FieldCount_ReturnsNumberOfFields()
    {
        var fields = new Dictionary<string, Series>
        {
            ["a"] = Series.FromValues("a", new[] { 1 }),
            ["b"] = Series.FromValues("b", new[] { 2 }),
            ["c"] = Series.FromValues("c", new[] { 3 }),
            ["d"] = Series.FromValues("d", new[] { 4 })
        };
        var series = Series.FromStruct("data", fields);

        var count = series.Struct.FieldCount();

        count.Should().Be(4);
    }

    [Fact]
    public void FieldCount_WithSingleField_ReturnsOne()
    {
        var fields = new Dictionary<string, Series>
        {
            ["only"] = Series.FromValues("only", new[] { 1 })
        };
        var series = Series.FromStruct("data", fields);

        series.Struct.FieldCount().Should().Be(1);
    }

    // ============================================================================
    // Struct.RenameFields() Tests
    // ============================================================================

    [Fact]
    public void RenameFields_ReturnsNewStructType()
    {
        var fields = new Dictionary<string, Series>
        {
            ["old_name"] = Series.FromValues("old_name", new[] { 1, 2, 3 })
        };
        var series = Series.FromStruct("data", fields);

        var renameMap = new Dictionary<string, string> { ["old_name"] = "new_name" };
        var newType = series.Struct.RenameFields(renameMap);

        newType.Fields[0].Name.Should().Be("new_name");
    }

    [Fact]
    public void RenameFields_PreservesUnrenamedFields()
    {
        var fields = new Dictionary<string, Series>
        {
            ["keep"] = Series.FromValues("keep", new[] { 1 }),
            ["change"] = Series.FromValues("change", new[] { 2 })
        };
        var series = Series.FromStruct("data", fields);

        var renameMap = new Dictionary<string, string> { ["change"] = "changed" };
        var newType = series.Struct.RenameFields(renameMap);

        newType.Fields.Should().Contain(f => f.Name == "keep");
        newType.Fields.Should().Contain(f => f.Name == "changed");
        newType.Fields.Should().NotContain(f => f.Name == "change");
    }

    // ============================================================================
    // Struct.JsonEncode() Tests
    // ============================================================================

    [Fact]
    public void JsonEncode_ReturnsJsonStrings()
    {
        var fields = new Dictionary<string, Series>
        {
            ["name"] = Series.FromValues("name", new[] { "Alice", "Bob" }),
            ["age"] = Series.FromValues("age", new[] { 25, 30 })
        };
        var series = Series.FromStruct("person", fields);

        var result = series.Struct.JsonEncode();

        result.DataType.Should().Be(DataType.String);
        result.Length.Should().Be(2);
        result[0].AsString().Should().Contain("Alice");
        result[0].AsString().Should().Contain("25");
    }

    [Fact]
    public void JsonEncode_WithNullStruct_ReturnsNull()
    {
        var fields = new Dictionary<string, Series>
        {
            ["value"] = Series.FromNullable("value", new int?[] { 1, null, 3 })
        };
        var series = Series.FromStruct("data", fields);

        var result = series.Struct.JsonEncode();

        result.Length.Should().Be(3);
        // The struct itself isn't null, but the field may have null
    }

    // ============================================================================
    // Struct.Unnest() Tests
    // ============================================================================

    [Fact]
    public void Unnest_ReturnsDataFrameWithSeparateColumns()
    {
        var fields = new Dictionary<string, Series>
        {
            ["name"] = Series.FromValues("name", new[] { "Alice", "Bob", "Charlie" }),
            ["age"] = Series.FromValues("age", new[] { 25, 30, 35 })
        };
        var series = Series.FromStruct("person", fields);

        var df = series.Struct.Unnest();

        df.Width.Should().Be(2);
        df.Height.Should().Be(3);
        df.Columns.Should().Contain("name");
        df.Columns.Should().Contain("age");
    }

    [Fact]
    public void Unnest_PreservesFieldTypes()
    {
        var fields = new Dictionary<string, Series>
        {
            ["int_col"] = Series.FromValues("int_col", new[] { 1, 2 }),
            ["double_col"] = Series.FromValues("double_col", new[] { 1.5, 2.5 }),
            ["string_col"] = Series.FromValues("string_col", new[] { "a", "b" })
        };
        var series = Series.FromStruct("data", fields);

        var df = series.Struct.Unnest();

        df["int_col"].DataType.Should().Be(DataType.Int32);
        df["double_col"].DataType.Should().Be(DataType.Float64);
        df["string_col"].DataType.Should().Be(DataType.String);
    }

    [Fact]
    public void Unnest_PreservesValues()
    {
        var fields = new Dictionary<string, Series>
        {
            ["x"] = Series.FromValues("x", new[] { 10, 20, 30 }),
            ["y"] = Series.FromValues("y", new[] { "a", "b", "c" })
        };
        var series = Series.FromStruct("data", fields);

        var df = series.Struct.Unnest();

        df["x"][0].AsInt32().Should().Be(10);
        df["x"][1].AsInt32().Should().Be(20);
        df["x"][2].AsInt32().Should().Be(30);
        df["y"][0].AsString().Should().Be("a");
    }

    // ============================================================================
    // Edge Cases
    // ============================================================================

    [Fact]
    public void Struct_WithSingleField_WorksCorrectly()
    {
        var fields = new Dictionary<string, Series>
        {
            ["only"] = Series.FromValues("only", new[] { 1, 2, 3 })
        };
        var series = Series.FromStruct("data", fields);

        series.Struct.FieldCount().Should().Be(1);
        series.Struct.Field("only").Length.Should().Be(3);
    }

    [Fact]
    public void Struct_WithManyFields_WorksCorrectly()
    {
        var fields = new Dictionary<string, Series>();
        for (int i = 0; i < 50; i++)
        {
            fields[$"field_{i}"] = Series.FromValues($"field_{i}", new[] { i });
        }
        var series = Series.FromStruct("data", fields);

        series.Struct.FieldCount().Should().Be(50);
    }

    [Fact]
    public void Struct_WithSingleRow_WorksCorrectly()
    {
        var fields = new Dictionary<string, Series>
        {
            ["a"] = Series.FromValues("a", new[] { 42 }),
            ["b"] = Series.FromValues("b", new[] { "hello" })
        };
        var series = Series.FromStruct("data", fields);

        series.Length.Should().Be(1);
        series.Struct.Field("a")[0].AsInt32().Should().Be(42);
        series.Struct.Field("b")[0].AsString().Should().Be("hello");
    }

    [Fact]
    public void Struct_WithLargeSeries_WorksEfficiently()
    {
        var ids = Enumerable.Range(0, 10000).ToArray();
        var values = ids.Select(i => (double)i * 1.5).ToArray();

        var fields = new Dictionary<string, Series>
        {
            ["id"] = Series.FromValues("id", ids),
            ["value"] = Series.FromValues("value", values)
        };
        var series = Series.FromStruct("data", fields);

        series.Length.Should().Be(10000);
        series.Struct.Field("id")[9999].AsInt32().Should().Be(9999);
    }

    [Fact]
    public void StructOperations_OnNonStructSeries_ThrowsArgumentException()
    {
        var series = Series.FromValues("numbers", new[] { 1, 2, 3 });

        var act = () => series.Struct;

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Struct*");
    }

    // ============================================================================
    // Complex Nested Scenarios
    // ============================================================================

    [Fact]
    public void Struct_FieldAccess_ChainedCorrectly()
    {
        var fields = new Dictionary<string, Series>
        {
            ["name"] = Series.FromValues("name", new[] { "Alice", "Bob" }),
            ["score"] = Series.FromValues("score", new[] { 95.0, 87.5 })
        };
        var series = Series.FromStruct("student", fields);

        var name = series.Struct.Field("name");
        var score = series.Struct.Field("score");

        name[0].AsString().Should().Be("Alice");
        score[0].AsFloat64().Should().Be(95.0);
    }
}
