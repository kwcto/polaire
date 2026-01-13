// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using Xunit;
using Polaire;
using Polaire.DataTypes;
using Polaire.IO;
using static Polaire.Pl;

namespace Polaire.Tests.IO;

public class ParquetTests : IDisposable
{
    private readonly string _tempDir;

    public ParquetTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"polaire_parquet_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void WriteParquet_RoundTrip_PreservesData()
    {
        // Arrange
        var df = DataFrame(
            Series("name", new[] { "Alice", "Bob", "Charlie" }),
            Series("age", new[] { 25, 30, 35 }),
            Series("score", new[] { 95.5, 88.0, 92.3 })
        );
        var parquetPath = Path.Combine(_tempDir, "test.parquet");

        // Act
        df.WriteParquet(parquetPath);
        var df2 = ReadParquet(parquetPath);

        // Assert
        Assert.Equal(df.Height, df2.Height);
        Assert.Equal(df.Width, df2.Width);
        Assert.Equal("Alice", df2["name"][0].AsString());
        Assert.Equal(25, df2["age"][0].AsInt32());
    }

    [Fact]
    public void ReadParquet_WithColumnProjection_ReadsOnlySelectedColumns()
    {
        // Arrange
        var df = DataFrame(
            Series("a", new[] { 1, 2, 3 }),
            Series("b", new[] { 4, 5, 6 }),
            Series("c", new[] { 7, 8, 9 })
        );
        var parquetPath = Path.Combine(_tempDir, "projection.parquet");
        df.WriteParquet(parquetPath);

        // Act
        var df2 = ReadParquet(parquetPath, new ParquetOptions { Columns = new[] { "a", "c" } });

        // Assert
        Assert.Equal(2, df2.Width);
        Assert.Contains("a", df2.Columns);
        Assert.Contains("c", df2.Columns);
        Assert.DoesNotContain("b", df2.Columns);
    }

    [Fact]
    public void WriteParquet_WithNulls_PreservesNulls()
    {
        // Arrange
        var df = DataFrame(
            Series("id", new[] { 1, 2, 3 }),
            Series.FromNullable("value", new int?[] { 10, null, 30 })
        );
        var parquetPath = Path.Combine(_tempDir, "nulls.parquet");

        // Act
        df.WriteParquet(parquetPath);
        var df2 = ReadParquet(parquetPath);

        // Assert
        Assert.Equal(3, df2.Height);
        Assert.Equal(10, df2["value"][0].AsInt32());
        Assert.True(df2["value"][1].IsNull);
        Assert.Equal(30, df2["value"][2].AsInt32());
    }

    [Fact]
    public void WriteParquet_MultipleDataTypes_Supported()
    {
        // Arrange
        var df = DataFrame(
            Series("int_col", new[] { 1, 2 }),
            Series("long_col", new[] { 100L, 200L }),
            Series("float_col", new[] { 1.5f, 2.5f }),
            Series("double_col", new[] { 3.14, 2.71 }),
            Series("bool_col", new[] { true, false }),
            Series("str_col", new[] { "hello", "world" })
        );
        var parquetPath = Path.Combine(_tempDir, "types.parquet");

        // Act
        df.WriteParquet(parquetPath);
        var df2 = ReadParquet(parquetPath);

        // Assert
        Assert.Equal(df.Height, df2.Height);
        Assert.Equal(df.Width, df2.Width);
        Assert.Equal(1, df2["int_col"][0].AsInt32());
        Assert.Equal(100L, df2["long_col"][0].AsInt64());
        Assert.True(df2["bool_col"][0].AsBoolean());
        Assert.Equal("hello", df2["str_col"][0].AsString());
    }
}
