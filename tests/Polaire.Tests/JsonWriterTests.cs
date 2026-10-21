// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using FluentAssertions;
using Polaire.DataTypes;
using Polaire.IO;
using Xunit;
using static Polaire.Expressions.Expr;

namespace Polaire.Tests;

/// <summary>
/// Tests for writing DataFrames as JSON (array of row objects) and NDJSON (one object per line).
/// </summary>
public class JsonWriterTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"polaire_json_{Guid.NewGuid():N}");

    public JsonWriterTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private static DataFrame Mixed() => new(
        Series.FromValues("a", new[] { 1, 2 }),
        Series.FromValues("b", new[] { 1.5, double.NaN }),
        Series.FromValues("c", new string?[] { "x", null }),
        Series.FromValues("d", new[] { true, false }));

    // ============================================================================
    // Output format
    // ============================================================================

    [Fact]
    public void ToJson_WritesArrayOfRowObjects()
    {
        Mixed().ToJson().Should().Be(
            "[{\"a\":1,\"b\":1.5,\"c\":\"x\",\"d\":true},{\"a\":2,\"b\":null,\"c\":null,\"d\":false}]");
    }

    [Fact]
    public void ToNdjson_WritesOneObjectPerLine()
    {
        Mixed().ToNdjson().Should().Be(
            "{\"a\":1,\"b\":1.5,\"c\":\"x\",\"d\":true}\n" +
            "{\"a\":2,\"b\":null,\"c\":null,\"d\":false}\n");
    }

    [Fact]
    public void ToJson_EmptyFrame()
    {
        var df = DataFrame.Empty(("a", DataType.Int32));

        df.ToJson().Should().Be("[]");
        df.ToNdjson().Should().BeEmpty();
    }

    [Fact]
    public void ToJson_FloatsAlwaysHaveFractionOrExponent()
    {
        var df = new DataFrame(
            Series.FromValues("f64", new[] { 2.0, 0.1, 1e20, -0.0 }),
            Series.FromValues("f32", new[] { 3f, 0.1f, 1.5f, 2.5e-8f }));

        df.ToNdjson().Should().Be(
            "{\"f64\":2.0,\"f32\":3.0}\n" +
            "{\"f64\":0.1,\"f32\":0.1}\n" +
            "{\"f64\":1E+20,\"f32\":1.5}\n" +
            "{\"f64\":-0.0,\"f32\":2.5E-08}\n");
    }

    [Fact]
    public void ToJson_NonFiniteFloatsBecomeNull()
    {
        var df = new DataFrame(Series.FromValues("f", new[] { double.PositiveInfinity, double.NegativeInfinity, double.NaN }));

        df.ToJson().Should().Be("[{\"f\":null},{\"f\":null},{\"f\":null}]");
    }

    [Fact]
    public void ToJson_IntegerTypes()
    {
        var df = new DataFrame(
            Series.FromValues("i64", new[] { long.MaxValue }),
            Series.FromValues("i8", new sbyte[] { -5 }),
            Series.FromValues("u64", new[] { ulong.MaxValue }));

        df.ToJson().Should().Be("[{\"i64\":9223372036854775807,\"i8\":-5,\"u64\":18446744073709551615}]");
    }

    [Fact]
    public void ToJson_EscapesStringsAndKeepsNonAsciiReadable()
    {
        var df = new DataFrame(Series.FromValues("say \"hi\"", new[] { "quote \" backslash \\ newline \n tab \t", "José <b>&" }));

        df.ToJson().Should().Be(
            "[{\"say \\\"hi\\\"\":\"quote \\\" backslash \\\\ newline \\n tab \\t\"},{\"say \\\"hi\\\"\":\"José <b>&\"}]");
    }

    [Fact]
    public void ToJson_WritesDatesAndDateTimesAsIsoStrings()
    {
        var df = new DataFrame(
            Series.FromValues("d", new[] { new DateOnly(2024, 6, 15) }),
            Series.FromValues("ts", new[] { new DateTime(2024, 6, 15, 12, 30, 45, 123) }),
            Series.FromValues("whole", new[] { new DateTime(2023, 1, 2) }));

        df.ToJson().Should().Be(
            "[{\"d\":\"2024-06-15\",\"ts\":\"2024-06-15T12:30:45.123\",\"whole\":\"2023-01-02T00:00:00\"}]");
    }

    [Fact]
    public void ToJson_WritesListColumnsAsArrays()
    {
        var df = new DataFrame(Series.FromLists("l", new[] { new[] { 1, 2 }, new[] { 3 }, null! }));

        df.ToJson().Should().Be("[{\"l\":[1,2]},{\"l\":[3]},{\"l\":null}]");
    }

    [Fact]
    public void ToJson_WritesStructColumnsAsObjects()
    {
        var df = new DataFrame(Series.FromStruct("s", new Dictionary<string, Series>
        {
            ["a"] = Series.FromValues("a", new[] { 1, 2 }),
            ["b"] = Series.FromValues("b", new[] { "x", "y" })
        }));

        df.ToJson().Should().Be("[{\"s\":{\"a\":1,\"b\":\"x\"}},{\"s\":{\"a\":2,\"b\":\"y\"}}]");
    }

    // ============================================================================
    // Round trips
    // ============================================================================

    [Fact]
    public void JsonReader_InfersNumericAndBooleanColumns()
    {
        var df = JsonReader.ReadString("[{\"i\": 1, \"f\": 2.5, \"b\": true}, {\"i\": null, \"f\": 3, \"b\": false}]");

        df["i"].DataType.Should().Be(DataType.Int32);
        df["i"][0].AsInt32().Should().Be(1);
        df["i"][1].IsNull.Should().BeTrue();
        df["f"].DataType.Should().Be(DataType.Float64);
        df["f"].ToArray<double>().Should().Equal(2.5, 3.0);
        df["b"].DataType.Should().Be(DataType.Boolean);
        df["b"].ToArray<bool>().Should().Equal(true, false);
    }

    [Fact]
    public void Json_RoundTrip_PreservesValuesAndTypes()
    {
        var df = new DataFrame(
            Series.FromValues("id", new[] { 1, 2, 3 }),
            Series.FromValues("big", new[] { 1L, 5_000_000_000L, -7L }),
            Series.FromValues("score", new[] { 1.0, 2.5, -3.0 }),
            Series.FromValues("name", new string?[] { "Ann", null, "Zoë 🙂" }),
            Series.FromValues("ok", new[] { true, false, true }));

        var result = JsonReader.ReadString(df.ToJson());

        result.Columns.Should().Equal("id", "big", "score", "name", "ok");
        result["id"].DataType.Should().Be(DataType.Int32);
        result["big"].DataType.Should().Be(DataType.Int64);
        result["score"].DataType.Should().Be(DataType.Float64);
        result["score"].ToArray<double>().Should().Equal(1.0, 2.5, -3.0);
        result["name"][1].IsNull.Should().BeTrue();
        result["name"][2].AsString().Should().Be("Zoë 🙂");
        result["ok"].ToArray<bool>().Should().Equal(true, false, true);
    }

    [Fact]
    public void Ndjson_RoundTrip_ThroughStream()
    {
        var df = Mixed();
        using var stream = new MemoryStream();

        df.WriteNdjson(stream);
        stream.Position = 0;
        var result = NdjsonReader.Read(stream);

        result.Height.Should().Be(2);
        result["a"].ToArray<int>().Should().Equal(1, 2);
        result["c"][0].AsString().Should().Be("x");
        result["d"].ToArray<bool>().Should().Equal(true, false);
    }

    [Fact]
    public void WriteJson_LeavesStreamOpen()
    {
        using var stream = new MemoryStream();

        Mixed().WriteJson(stream);

        stream.CanWrite.Should().BeTrue();
        stream.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void WriteJson_ToFile_ReadsBack()
    {
        var path = Path.Combine(_tempDir, "data.json");

        Mixed().WriteJson(path);
        var result = Pl.ReadJson(path);

        result.Height.Should().Be(2);
        result["a"].ToArray<int>().Should().Equal(1, 2);
    }

    [Fact]
    public void WriteNdjson_ToFile_ScansLazily()
    {
        var path = Path.Combine(_tempDir, "data.ndjson");
        var df = new DataFrame(
            Series.FromValues("id", new[] { 1, 2, 3, 4 }),
            Series.FromValues("value", new[] { 10.0, 20.0, 30.0, 40.0 }));

        df.WriteNdjson(path);
        var result = Pl.ScanNdjson(path).Filter(Col("value").Gt(15.0)).Collect();

        result["id"].ToArray<int>().Should().Equal(2, 3, 4);
    }
}
