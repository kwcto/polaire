// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using Xunit;
using Polaire;
using Polaire.DataTypes;
using Polaire.IO;
using static Polaire.Pl;

namespace Polaire.Tests.IO;

public class JsonTests : IDisposable
{
    private readonly string _tempDir;

    public JsonTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"polaire_json_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void ReadNdjson_BasicFile_ReturnsCorrectData()
    {
        // Arrange
        var ndjsonPath = Path.Combine(_tempDir, "test.ndjson");
        File.WriteAllText(ndjsonPath,
            "{\"name\":\"Alice\",\"age\":25}\n" +
            "{\"name\":\"Bob\",\"age\":30}\n" +
            "{\"name\":\"Charlie\",\"age\":35}");

        // Act
        var df = ReadNdjson(ndjsonPath);

        // Assert
        Assert.Equal(3, df.Height);
        Assert.Contains("name", df.Columns);
        Assert.Contains("age", df.Columns);
        Assert.Equal("Alice", df["name"][0].AsString());
        Assert.Equal(25, df["age"][0].AsInt32());
    }

    [Fact]
    public void ReadJson_ArrayFormat_ReturnsCorrectData()
    {
        // Arrange
        var jsonPath = Path.Combine(_tempDir, "test.json");
        File.WriteAllText(jsonPath,
            "[{\"id\":1,\"value\":\"a\"},{\"id\":2,\"value\":\"b\"}]");

        // Act
        var df = ReadJson(jsonPath);

        // Assert
        Assert.Equal(2, df.Height);
        Assert.Contains("id", df.Columns);
        Assert.Contains("value", df.Columns);
    }

    [Fact]
    public void ReadNdjson_WithNulls_HandlesNullsCorrectly()
    {
        // Arrange
        var ndjsonPath = Path.Combine(_tempDir, "nulls.ndjson");
        File.WriteAllText(ndjsonPath,
            "{\"a\":1,\"b\":\"x\"}\n" +
            "{\"a\":null,\"b\":\"y\"}\n" +
            "{\"a\":3,\"b\":null}");

        // Act
        var df = ReadNdjson(ndjsonPath);

        // Assert
        Assert.Equal(3, df.Height);
        Assert.True(df["a"][1].IsNull);
        Assert.True(df["b"][2].IsNull);
    }

    [Fact]
    public void ReadNdjson_MixedTypes_InfersCorrectTypes()
    {
        // Arrange
        var ndjsonPath = Path.Combine(_tempDir, "types.ndjson");
        File.WriteAllText(ndjsonPath,
            "{\"int_col\":42,\"float_col\":3.14,\"bool_col\":true,\"str_col\":\"hello\"}\n" +
            "{\"int_col\":0,\"float_col\":2.71,\"bool_col\":false,\"str_col\":\"world\"}");

        // Act
        var df = ReadNdjson(ndjsonPath);

        // Assert
        Assert.Equal(2, df.Height);
        Assert.Equal(4, df.Width);
        Assert.Equal(42, df["int_col"][0].AsInt32());
        Assert.True(df["bool_col"][0].AsBoolean());
        Assert.Equal("hello", df["str_col"][0].AsString());
    }

    [Fact]
    public void ReadNdjson_EmptyFile_ReturnsEmptyDataFrame()
    {
        // Arrange
        var ndjsonPath = Path.Combine(_tempDir, "empty.ndjson");
        File.WriteAllText(ndjsonPath, "");

        // Act
        var df = ReadNdjson(ndjsonPath);

        // Assert
        Assert.Equal(0, df.Height);
    }

    [Fact]
    public void ReadJson_EmptyArray_ReturnsEmptyDataFrame()
    {
        // Arrange
        var jsonPath = Path.Combine(_tempDir, "empty.json");
        File.WriteAllText(jsonPath, "[]");

        // Act
        var df = ReadJson(jsonPath);

        // Assert
        Assert.Equal(0, df.Height);
    }
}
