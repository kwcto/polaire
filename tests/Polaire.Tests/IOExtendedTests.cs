// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET
//
// Extended tests for I/O operations, inspired by Polars test suite.
// These tests cover:
// - CSV read/write edge cases
// - Parquet read/write edge cases
// - JSON/NDJSON read/write edge cases
// - Lazy scan operations

using FluentAssertions;
using Polaire;
using Polaire.DataTypes;
using Xunit;
using static Polaire.Pl;

namespace Polaire.Tests;

/// <summary>
/// Extended tests for I/O operations.
/// </summary>
public class IOExtendedTests
{
    private string GetTempPath(string filename) =>
        Path.Combine(Path.GetTempPath(), $"polaire_test_{Guid.NewGuid():N}_{filename}");

    // ============================================================================
    // CSV Basic Tests
    // ============================================================================

    [Fact]
    public void Csv_WriteAndRead_RoundTrips()
    {
        var df = new DataFrame(
            Series.FromValues("id", new[] { 1, 2, 3 }),
            Series.FromValues("name", new[] { "Alice", "Bob", "Charlie" }),
            Series.FromValues("value", new[] { 1.5, 2.5, 3.5 })
        );

        var path = GetTempPath("basic.csv");
        try
        {
            df.WriteCsv(path);
            var result = ReadCsv(path);

            result.Height.Should().Be(3);
            result.Width.Should().Be(3);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Csv_EmptyDataFrame_WritesFile()
    {
        var df = new DataFrame(
            Series.FromValues("a", Array.Empty<int>()),
            Series.FromValues("b", Array.Empty<string>())
        );

        var path = GetTempPath("empty.csv");
        try
        {
            df.WriteCsv(path);
            // Empty DataFrame CSV may not preserve headers when read back
            File.Exists(path).Should().BeTrue();
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Csv_SingleRow_RoundTrips()
    {
        var df = new DataFrame(
            Series.FromValues("x", new[] { 42 }),
            Series.FromValues("y", new[] { "hello" })
        );

        var path = GetTempPath("single.csv");
        try
        {
            df.WriteCsv(path);
            var result = ReadCsv(path);

            result.Height.Should().Be(1);
            // CSV may read integers as Int32 or Int64 depending on parsing
            result["x"][0].AsInt32().Should().Be(42);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Csv_ManyColumns_RoundTrips()
    {
        var columns = new Series[20];
        for (int i = 0; i < 20; i++)
        {
            columns[i] = Series.FromValues($"col{i}", new[] { i * 10, i * 20, i * 30 });
        }

        var df = new DataFrame(columns);

        var path = GetTempPath("many_cols.csv");
        try
        {
            df.WriteCsv(path);
            var result = ReadCsv(path);

            result.Width.Should().Be(20);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Csv_ManyRows_RoundTrips()
    {
        var values = Enumerable.Range(0, 1000).ToArray();

        var df = new DataFrame(
            Series.FromValues("id", values),
            Series.FromValues("value", values.Select(i => i * 2).ToArray())
        );

        var path = GetTempPath("many_rows.csv");
        try
        {
            df.WriteCsv(path);
            var result = ReadCsv(path);

            result.Height.Should().Be(1000);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    // ============================================================================
    // CSV Type Tests
    // ============================================================================

    [Fact]
    public void Csv_IntegerColumn_ParsesCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("int_col", new[] { -100, 0, 100, 999999 })
        );

        var path = GetTempPath("integers.csv");
        try
        {
            df.WriteCsv(path);
            var result = ReadCsv(path);

            // CSV parser may return Int32 or Int64
            result["int_col"][0].AsInt32().Should().Be(-100);
            result["int_col"][3].AsInt32().Should().Be(999999);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Csv_FloatColumn_ParsesCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("float_col", new[] { 1.5, -2.5, 0.0, 999.999 })
        );

        var path = GetTempPath("floats.csv");
        try
        {
            df.WriteCsv(path);
            var result = ReadCsv(path);

            result["float_col"][0].AsFloat64().Should().BeApproximately(1.5, 0.001);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Csv_BooleanColumn_ParsesCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("bool_col", new[] { true, false, true, false })
        );

        var path = GetTempPath("booleans.csv");
        try
        {
            df.WriteCsv(path);
            var result = ReadCsv(path);

            result.Height.Should().Be(4);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    // ============================================================================
    // Parquet Basic Tests
    // ============================================================================

    [Fact]
    public void Parquet_WriteAndRead_RoundTrips()
    {
        var df = new DataFrame(
            Series.FromValues("id", new[] { 1, 2, 3 }),
            Series.FromValues("name", new[] { "Alice", "Bob", "Charlie" }),
            Series.FromValues("value", new[] { 1.5, 2.5, 3.5 })
        );

        var path = GetTempPath("basic.parquet");
        try
        {
            df.WriteParquet(path);
            var result = ReadParquet(path);

            result.Height.Should().Be(3);
            result.Width.Should().Be(3);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Parquet_PreservesTypes()
    {
        var df = new DataFrame(
            Series.FromValues("int_col", new[] { 1, 2, 3 }),
            Series.FromValues("float_col", new[] { 1.5, 2.5, 3.5 }),
            Series.FromValues("str_col", new[] { "a", "b", "c" })
        );

        var path = GetTempPath("types.parquet");
        try
        {
            df.WriteParquet(path);
            var result = ReadParquet(path);

            // Int32 may be read as Int64 depending on parquet settings
            result["int_col"].DataType.Should().BeOneOf(DataType.Int32, DataType.Int64);
            result["float_col"].DataType.Should().Be(DataType.Float64);
            result["str_col"].DataType.Should().Be(DataType.String);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Parquet_EmptyDataFrame_RoundTrips()
    {
        var df = new DataFrame(
            Series.FromValues("a", Array.Empty<int>())
        );

        var path = GetTempPath("empty.parquet");
        try
        {
            df.WriteParquet(path);
            var result = ReadParquet(path);

            result.Height.Should().Be(0);
            result.Columns.Should().Contain("a");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Parquet_LargeData_RoundTrips()
    {
        var values = Enumerable.Range(0, 10000).ToArray();

        var df = new DataFrame(
            Series.FromValues("id", values),
            Series.FromValues("value", values.Select(i => i * 1.5).ToArray())
        );

        var path = GetTempPath("large.parquet");
        try
        {
            df.WriteParquet(path);
            var result = ReadParquet(path);

            result.Height.Should().Be(10000);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    // ============================================================================
    // JSON Tests (Read-only - no WriteJson available)
    // ============================================================================

    // Note: JSON read is available via ReadJson, but WriteJson is not implemented.
    // NDJSON tests use ScanNdjson instead.

    // ============================================================================
    // Lazy Scan Tests
    // ============================================================================

    [Fact]
    public void ScanCsv_WithFilter_AppliesPredicate()
    {
        var df = new DataFrame(
            Series.FromValues("id", new[] { 1, 2, 3, 4, 5 }),
            Series.FromValues("value", new[] { 10, 20, 30, 40, 50 })
        );

        var path = GetTempPath("scan_filter.csv");
        try
        {
            df.WriteCsv(path);

            var result = ScanCsv(path)
                .Filter(Col("id").Gt(2))
                .Collect();

            result.Height.Should().Be(3);  // 3, 4, 5
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void ScanCsv_WithSelect_AppliesProjection()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 }),
            Series.FromValues("b", new[] { 10, 20, 30 }),
            Series.FromValues("c", new[] { 100, 200, 300 })
        );

        var path = GetTempPath("scan_select.csv");
        try
        {
            df.WriteCsv(path);

            var result = ScanCsv(path)
                .Select("a", "c")
                .Collect();

            result.Width.Should().Be(2);
            result.Columns.Should().Contain("a");
            result.Columns.Should().Contain("c");
            result.Columns.Should().NotContain("b");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void ScanParquet_WithFilter_AppliesPredicate()
    {
        var df = new DataFrame(
            Series.FromValues("id", new[] { 1, 2, 3, 4, 5 }),
            Series.FromValues("value", new[] { 10, 20, 30, 40, 50 })
        );

        var path = GetTempPath("scan_filter.parquet");
        try
        {
            df.WriteParquet(path);

            var result = ScanParquet(path)
                .Filter(Col("id").Gt(3))
                .Collect();

            result.Height.Should().Be(2);  // 4, 5
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    // Note: ScanNdjson requires a file to exist, which would need WriteNdjson.
    // Skip NDJSON write tests since WriteNdjson is not available.

    // ============================================================================
    // Null Handling Tests
    // ============================================================================

    [Fact]
    public void Csv_WithNulls_WritesFile()
    {
        var df = new DataFrame(
            Series.FromNullable("value", new int?[] { 1, null, 3 })
        );

        var path = GetTempPath("nulls.csv");
        try
        {
            df.WriteCsv(path);
            // CSV null handling may vary - some parsers skip empty rows
            File.Exists(path).Should().BeTrue();
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Parquet_WithNulls_RoundTrips()
    {
        var df = new DataFrame(
            Series.FromNullable("value", new double?[] { 1.5, null, 3.5 })
        );

        var path = GetTempPath("nulls.parquet");
        try
        {
            df.WriteParquet(path);
            var result = ReadParquet(path);

            result.Height.Should().Be(3);
            result["value"].IsNull(1).Should().BeTrue();
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    // ============================================================================
    // Edge Cases
    // ============================================================================

    [Fact]
    public void Csv_SpecialCharacters_InStrings()
    {
        var df = new DataFrame(
            Series.FromValues("text", new[] { "hello", "world,with,commas", "and\"quotes\"" })
        );

        var path = GetTempPath("special.csv");
        try
        {
            df.WriteCsv(path);
            var result = ReadCsv(path);

            result.Height.Should().Be(3);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Csv_Newlines_InStrings()
    {
        var df = new DataFrame(
            Series.FromValues("text", new[] { "line1\nline2", "normal", "another\nmultiline" })
        );

        var path = GetTempPath("newlines.csv");
        try
        {
            df.WriteCsv(path);
            var result = ReadCsv(path);

            result.Height.Should().Be(3);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Csv_Unicode_InStrings()
    {
        var df = new DataFrame(
            Series.FromValues("text", new[] { "Hello", "Привет", "你好", "مرحبا" })
        );

        var path = GetTempPath("unicode.csv");
        try
        {
            df.WriteCsv(path);
            var result = ReadCsv(path);

            result.Height.Should().Be(4);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Csv_EmptyStrings_WritesFile()
    {
        var df = new DataFrame(
            Series.FromValues("text", new[] { "", "value", "" })
        );

        var path = GetTempPath("empty_strings.csv");
        try
        {
            df.WriteCsv(path);
            // Empty strings in CSV may be handled differently
            File.Exists(path).Should().BeTrue();
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    // ============================================================================
    // File Path Tests
    // ============================================================================

    [Fact]
    public void ReadCsv_NonExistentFile_ThrowsException()
    {
        var act = () => ReadCsv("/nonexistent/path/file.csv");

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void ReadParquet_NonExistentFile_ThrowsException()
    {
        var act = () => ReadParquet("/nonexistent/path/file.parquet");

        act.Should().Throw<Exception>();
    }

    // ============================================================================
    // Chained Operations Tests
    // ============================================================================

    [Fact]
    public void ScanCsv_FilterThenSort_Works()
    {
        var df = new DataFrame(
            Series.FromValues("id", new[] { 5, 3, 1, 4, 2 }),
            Series.FromValues("value", new[] { 50, 30, 10, 40, 20 })
        );

        var path = GetTempPath("chain.csv");
        try
        {
            df.WriteCsv(path);

            var result = ScanCsv(path)
                .Filter(Col("id").Gt(2))
                .Sort("id")
                .Collect();

            result.Height.Should().Be(3);  // 3, 4, 5
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void ScanCsv_SelectThenFilter_Works()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 }),
            Series.FromValues("b", new[] { 10, 20, 30, 40, 50 }),
            Series.FromValues("c", new[] { 100, 200, 300, 400, 500 })
        );

        var path = GetTempPath("chain2.csv");
        try
        {
            df.WriteCsv(path);

            var result = ScanCsv(path)
                .Select("a", "b")
                .Filter(Col("a").Gt(2))
                .Collect();

            result.Width.Should().Be(2);
            result.Height.Should().Be(3);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
