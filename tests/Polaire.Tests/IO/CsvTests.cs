// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using Xunit;
using Polaire;
using Polaire.DataTypes;
using Polaire.IO;
using static Polaire.Pl;

namespace Polaire.Tests.IO;

public class CsvTests : IDisposable
{
    private readonly string _tempDir;

    public CsvTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"polaire_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void ReadCsv_BasicFile_ReturnsCorrectData()
    {
        // Arrange
        var csvPath = Path.Combine(_tempDir, "test.csv");
        File.WriteAllText(csvPath, "name,age,score\nAlice,25,95.5\nBob,30,88.0\nCharlie,35,92.3");

        // Act
        var df = ReadCsv(csvPath);

        // Assert
        Assert.Equal(3, df.Height);
        Assert.Equal(3, df.Width);
        Assert.Contains("name", df.Columns);
        Assert.Contains("age", df.Columns);
        Assert.Contains("score", df.Columns);
    }

    [Fact]
    public void ReadCsv_TypeInference_InfersCorrectTypes()
    {
        // Arrange
        var csvPath = Path.Combine(_tempDir, "types.csv");
        File.WriteAllText(csvPath, "int_col,float_col,bool_col,str_col\n42,3.14,true,hello\n0,2.71,false,world");

        // Act
        var df = ReadCsv(csvPath);

        // Assert
        Assert.Equal(DataType.Int32, df["int_col"].DataType);
        Assert.Equal(DataType.Float64, df["float_col"].DataType);
        Assert.Equal(DataType.Boolean, df["bool_col"].DataType);
        Assert.Equal(DataType.String, df["str_col"].DataType);
    }

    [Fact]
    public void ReadCsv_WithNulls_HandlesNullsCorrectly()
    {
        // Arrange
        var csvPath = Path.Combine(_tempDir, "nulls.csv");
        File.WriteAllText(csvPath, "a,b,c\n1,hello,\n2,,world\n,bye,test");

        // Act
        var df = ReadCsv(csvPath);

        // Assert
        Assert.Equal(3, df.Height);
        Assert.True(df["a"].HasNulls);
        Assert.True(df["b"].HasNulls);
        Assert.True(df["c"].HasNulls);
    }

    [Fact]
    public void ReadCsv_CustomSeparator_ParsesCorrectly()
    {
        // Arrange
        var csvPath = Path.Combine(_tempDir, "semicolon.csv");
        File.WriteAllText(csvPath, "a;b;c\n1;2;3\n4;5;6");

        // Act
        var df = ReadCsv(csvPath, hasHeader: true, separator: ';');

        // Assert
        Assert.Equal(2, df.Height);
        Assert.Equal(3, df.Width);
    }

    [Fact]
    public void ReadCsv_NoHeader_GeneratesColumnNames()
    {
        // Arrange
        var csvPath = Path.Combine(_tempDir, "noheader.csv");
        File.WriteAllText(csvPath, "1,2,3\n4,5,6");

        // Act
        var df = ReadCsv(csvPath, hasHeader: false);

        // Assert
        Assert.Equal(2, df.Height);
        Assert.Contains("column_0", df.Columns);
        Assert.Contains("column_1", df.Columns);
        Assert.Contains("column_2", df.Columns);
    }

    [Fact]
    public void WriteCsv_RoundTrip_PreservesData()
    {
        // Arrange
        var df = DataFrame(
            Series("name", new[] { "Alice", "Bob", "Charlie" }),
            Series("age", new[] { 25, 30, 35 }),
            Series("score", new[] { 95.5, 88.0, 92.3 })
        );
        var csvPath = Path.Combine(_tempDir, "output.csv");

        // Act
        df.WriteCsv(csvPath);
        var df2 = ReadCsv(csvPath);

        // Assert
        Assert.Equal(df.Height, df2.Height);
        Assert.Equal(df.Width, df2.Width);
        Assert.Equal("Alice", df2["name"][0].AsString());
        Assert.Equal(25, df2["age"][0].AsInt32());
    }

    [Fact]
    public void ToCsv_ReturnsValidString()
    {
        // Arrange
        var df = DataFrame(
            Series("a", new[] { 1, 2 }),
            Series("b", new[] { "x", "y" })
        );

        // Act
        var csv = df.ToCsv();

        // Assert
        Assert.Contains("a,b", csv);
        Assert.Contains("1,x", csv);
        Assert.Contains("2,y", csv);
    }

    [Fact]
    public void ReadCsv_WithColumnProjection_ReadsOnlySelectedColumns()
    {
        // Arrange
        var csvPath = Path.Combine(_tempDir, "projection.csv");
        File.WriteAllText(csvPath, "a,b,c,d\n1,2,3,4\n5,6,7,8");

        // Act
        var df = ReadCsv(csvPath, new CsvOptions { Columns = new[] { "a", "c" } });

        // Assert
        Assert.Equal(2, df.Width);
        Assert.Contains("a", df.Columns);
        Assert.Contains("c", df.Columns);
        Assert.DoesNotContain("b", df.Columns);
        Assert.DoesNotContain("d", df.Columns);
    }

    [Fact]
    public void ReadCsv_WithNRows_LimitsRows()
    {
        // Arrange
        var csvPath = Path.Combine(_tempDir, "nrows.csv");
        File.WriteAllText(csvPath, "a,b\n1,2\n3,4\n5,6\n7,8");

        // Act
        var df = ReadCsv(csvPath, new CsvOptions { NRows = 2 });

        // Assert
        Assert.Equal(2, df.Height);
    }

    [Fact]
    public void ReadCsv_WithSkipRows_SkipsInitialRows()
    {
        // Arrange
        var csvPath = Path.Combine(_tempDir, "skiprows.csv");
        File.WriteAllText(csvPath, "a,b\n1,2\n3,4\n5,6\n7,8");

        // Act
        var df = ReadCsv(csvPath, new CsvOptions { SkipRows = 2 });

        // Assert
        Assert.Equal(2, df.Height);
        Assert.Equal(5, df["a"][0].AsInt32());
    }

    [Fact]
    public void ReadCsv_QuotedFields_ParsesCorrectly()
    {
        // Arrange
        var csvPath = Path.Combine(_tempDir, "quoted.csv");
        File.WriteAllText(csvPath, "name,description\nAlice,\"Hello, World\"\nBob,\"Line1\nLine2\"");

        // Act
        var df = ReadCsv(csvPath);

        // Assert
        Assert.Equal(2, df.Height);
        Assert.Equal("Hello, World", df["description"][0].AsString());
    }
}
