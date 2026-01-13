// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using Xunit;
using Polaire;
using Polaire.DataTypes;
using Polaire.IO;
using Polaire.LazyFrame;
using static Polaire.Pl;

namespace Polaire.Tests.IO;

public class LazyScanTests : IDisposable
{
    private readonly string _tempDir;

    public LazyScanTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"polaire_lazy_scan_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ============================================================================
    // CSV Lazy Scanning
    // ============================================================================

    [Fact]
    public void ScanCsv_BasicRead_ReturnsCorrectData()
    {
        // Arrange
        var csvPath = Path.Combine(_tempDir, "test.csv");
        File.WriteAllText(csvPath, "name,age,city\nAlice,25,NYC\nBob,30,LA\nCharlie,35,Chicago");

        // Act
        var df = ScanCsv(csvPath).Collect();

        // Assert
        Assert.Equal(3, df.Height);
        Assert.Equal(3, df.Width);
        Assert.Equal("Alice", df["name"][0].AsString());
        Assert.Equal(25, df["age"][0].AsInt32());
    }

    [Fact]
    public void ScanCsv_WithFilter_AppliesPredicate()
    {
        // Arrange
        var csvPath = Path.Combine(_tempDir, "filter.csv");
        File.WriteAllText(csvPath, "id,value\n1,10\n2,20\n3,30\n4,40\n5,50");

        // Act
        var df = ScanCsv(csvPath)
            .Filter(Col("value").Gt(25))
            .Collect();

        // Assert
        Assert.Equal(3, df.Height);
        Assert.Equal(30, df["value"][0].AsInt32());
    }

    [Fact]
    public void ScanCsv_WithSelect_ProjectsColumns()
    {
        // Arrange
        var csvPath = Path.Combine(_tempDir, "project.csv");
        File.WriteAllText(csvPath, "a,b,c,d\n1,2,3,4\n5,6,7,8");

        // Act
        var df = ScanCsv(csvPath)
            .Select("a", "c")
            .Collect();

        // Assert
        Assert.Equal(2, df.Width);
        Assert.Contains("a", df.Columns);
        Assert.Contains("c", df.Columns);
        Assert.DoesNotContain("b", df.Columns);
        Assert.DoesNotContain("d", df.Columns);
    }

    [Fact]
    public void ScanCsv_WithFilterAndSelect_CombinesOperations()
    {
        // Arrange
        var csvPath = Path.Combine(_tempDir, "combined.csv");
        File.WriteAllText(csvPath, "name,age,city\nAlice,25,NYC\nBob,30,LA\nCharlie,35,Chicago");

        // Act
        var df = ScanCsv(csvPath)
            .Filter(Col("age").Ge(30))
            .Select("name", "age")
            .Collect();

        // Assert
        Assert.Equal(2, df.Height);
        Assert.Equal(2, df.Width);
        Assert.Equal("Bob", df["name"][0].AsString());
    }

    [Fact]
    public void ScanCsv_Explain_ShowsPlan()
    {
        // Arrange
        var csvPath = Path.Combine(_tempDir, "explain.csv");
        File.WriteAllText(csvPath, "a,b\n1,2");

        // Act
        var lf = ScanCsv(csvPath)
            .Filter(Col("a").Gt(0))
            .Select("a");

        var plan = lf.Explain(optimized: false);
        var optimizedPlan = lf.Explain(optimized: true);

        // Assert
        Assert.Contains("Select", plan);
        Assert.Contains("Filter", plan);
        Assert.Contains("ScanCsv", plan);
        // Optimized plan should have predicate pushed into scan
        Assert.Contains("ScanCsv", optimizedPlan);
    }

    // ============================================================================
    // Parquet Lazy Scanning
    // ============================================================================

    [Fact]
    public void ScanParquet_BasicRead_ReturnsCorrectData()
    {
        // Arrange
        var df = DataFrame(
            Series("name", new[] { "Alice", "Bob" }),
            Series("age", new[] { 25, 30 })
        );
        var parquetPath = Path.Combine(_tempDir, "test.parquet");
        df.WriteParquet(parquetPath);

        // Act
        var result = ScanParquet(parquetPath).Collect();

        // Assert
        Assert.Equal(2, result.Height);
        Assert.Equal("Alice", result["name"][0].AsString());
    }

    [Fact]
    public void ScanParquet_WithFilter_AppliesPredicate()
    {
        // Arrange
        var df = DataFrame(
            Series("id", new[] { 1, 2, 3, 4, 5 }),
            Series("value", new[] { 10, 20, 30, 40, 50 })
        );
        var parquetPath = Path.Combine(_tempDir, "filter.parquet");
        df.WriteParquet(parquetPath);

        // Act
        var result = ScanParquet(parquetPath)
            .Filter(Col("value").Gt(25))
            .Collect();

        // Assert
        Assert.Equal(3, result.Height);
        Assert.Equal(30, result["value"][0].AsInt32());
    }

    [Fact]
    public void ScanParquet_WithSelect_ProjectsColumns()
    {
        // Arrange
        var df = DataFrame(
            Series("a", new[] { 1, 2 }),
            Series("b", new[] { 3, 4 }),
            Series("c", new[] { 5, 6 })
        );
        var parquetPath = Path.Combine(_tempDir, "project.parquet");
        df.WriteParquet(parquetPath);

        // Act
        var result = ScanParquet(parquetPath)
            .Select("a", "c")
            .Collect();

        // Assert
        Assert.Equal(2, result.Width);
        Assert.Contains("a", result.Columns);
        Assert.Contains("c", result.Columns);
    }

    // ============================================================================
    // NDJSON Lazy Scanning
    // ============================================================================

    [Fact]
    public void ScanNdjson_BasicRead_ReturnsCorrectData()
    {
        // Arrange
        var ndjsonPath = Path.Combine(_tempDir, "test.ndjson");
        File.WriteAllText(ndjsonPath,
            "{\"name\":\"Alice\",\"age\":25}\n" +
            "{\"name\":\"Bob\",\"age\":30}");

        // Act
        var df = ScanNdjson(ndjsonPath).Collect();

        // Assert
        Assert.Equal(2, df.Height);
        Assert.Equal("Alice", df["name"][0].AsString());
    }

    [Fact]
    public void ScanNdjson_WithFilter_AppliesPredicate()
    {
        // Arrange
        var ndjsonPath = Path.Combine(_tempDir, "filter.ndjson");
        File.WriteAllText(ndjsonPath,
            "{\"id\":1,\"value\":10}\n" +
            "{\"id\":2,\"value\":20}\n" +
            "{\"id\":3,\"value\":30}");

        // Act
        var df = ScanNdjson(ndjsonPath)
            .Filter(Col("value").Ge(20))
            .Collect();

        // Assert
        Assert.Equal(2, df.Height);
        Assert.Equal(20, df["value"][0].AsInt32());
    }

    [Fact]
    public void ScanNdjson_WithSelect_ProjectsColumns()
    {
        // Arrange
        var ndjsonPath = Path.Combine(_tempDir, "project.ndjson");
        File.WriteAllText(ndjsonPath,
            "{\"a\":1,\"b\":2,\"c\":3}\n" +
            "{\"a\":4,\"b\":5,\"c\":6}");

        // Act
        var df = ScanNdjson(ndjsonPath)
            .Select("a", "c")
            .Collect();

        // Assert
        Assert.Equal(2, df.Width);
        Assert.Contains("a", df.Columns);
        Assert.Contains("c", df.Columns);
    }

    // ============================================================================
    // Query Optimization Tests
    // ============================================================================

    [Fact]
    public void ScanCsv_PredicatePushdown_ShowsInPlan()
    {
        // Arrange
        var csvPath = Path.Combine(_tempDir, "pushdown.csv");
        File.WriteAllText(csvPath, "x,y\n1,2\n3,4");

        // Act
        var lf = ScanCsv(csvPath).Filter(Col("x").Gt(1));
        var optimizedPlan = lf.Explain(optimized: true);

        // Assert - predicate should be pushed into ScanCsv node
        Assert.Contains("predicate=", optimizedPlan);
        Assert.Contains("ScanCsv", optimizedPlan);
    }

    [Fact]
    public void ScanCsv_ProjectionPushdown_ShowsInPlan()
    {
        // Arrange
        var csvPath = Path.Combine(_tempDir, "projection.csv");
        File.WriteAllText(csvPath, "a,b,c\n1,2,3");

        // Act
        var lf = ScanCsv(csvPath).Select("a");
        var optimizedPlan = lf.Explain(optimized: true);

        // Assert - projection should show reduced columns
        Assert.Contains("cols=", optimizedPlan);
    }

    [Fact]
    public void ScanCsv_ChainedOperations_ProducesCorrectResult()
    {
        // Arrange
        var csvPath = Path.Combine(_tempDir, "chained.csv");
        File.WriteAllText(csvPath, "id,name,score\n1,Alice,85\n2,Bob,92\n3,Charlie,78\n4,Diana,95");

        // Act
        var df = ScanCsv(csvPath)
            .Filter(Col("score").Ge(80))
            .Select(Col("name"), Col("score"))
            .Sort(Col("score").SortBy(descending: true))
            .Collect();

        // Assert
        Assert.Equal(3, df.Height);
        Assert.Equal(2, df.Width);
        Assert.Equal("Diana", df["name"][0].AsString());
        Assert.Equal(95, df["score"][0].AsInt32());
    }

    // ============================================================================
    // GroupBy with Lazy Scanning
    // ============================================================================

    [Fact]
    public void ScanCsv_GroupByAgg_WorksCorrectly()
    {
        // Arrange
        var csvPath = Path.Combine(_tempDir, "groupby.csv");
        File.WriteAllText(csvPath, "category,value\nA,10\nB,20\nA,30\nB,40\nA,50");

        // Act
        var df = ScanCsv(csvPath)
            .GroupBy("category")
            .Agg(Col("value").Sum().As("total"))
            .Sort("category")
            .Collect();

        // Assert
        Assert.Equal(2, df.Height);
        Assert.Equal(90, df["total"][0].AsInt64());  // A: 10+30+50 = 90
        Assert.Equal(60, df["total"][1].AsInt64());  // B: 20+40 = 60
    }
}
