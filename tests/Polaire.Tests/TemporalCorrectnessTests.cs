// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using FluentAssertions;
using Polaire.DataTypes;
using Xunit;
using static Polaire.Expressions.Expr;

namespace Polaire.Tests;

/// <summary>
/// Value-level tests for Date and DateTime columns. Arrow stores a Date as days since 1970-01-01
/// and a DateTime as units (default nanoseconds) since the Unix epoch; these tests check that values
/// read back correctly and keep their type through row operations, comparisons and output.
/// </summary>
public class TemporalCorrectnessTests
{
    private static readonly DateTime T1 = new(2024, 6, 15, 12, 30, 45, 123, DateTimeKind.Utc);
    private static readonly DateTime T2 = new(2023, 1, 2, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime T3 = new(1969, 7, 20, 20, 17, 40, DateTimeKind.Utc);

    private static readonly DateOnly D1 = new(2024, 6, 15);
    private static readonly DateOnly D2 = new(2023, 1, 2);
    private static readonly DateOnly D3 = new(1969, 7, 20);

    private static DataFrame Frame() => new(
        Series.FromValues("x", new[] { 1, 2, 3 }),
        Series.FromValues("ts", new[] { T1, T2, T3 }),
        Series.FromValues("d", new[] { D1, D2, D3 }));

    // ============================================================================
    // Storage and value access
    // ============================================================================

    [Fact]
    public void DateTimeSeries_Indexer_ReturnsStoredValues()
    {
        var s = Series.FromValues("ts", new[] { T1, T2, T3 });

        s[0].AsDateTime().Should().Be(T1);
        s[1].AsDateTime().Should().Be(T2);
        s[2].AsDateTime().Should().Be(T3);
    }

    [Fact]
    public void DateTimeSeries_StoresNanosecondsSinceUnixEpoch()
    {
        var s = Series.FromValues("ts", new[]
        {
            new DateTime(1970, 1, 1, 0, 0, 1, DateTimeKind.Utc),
            new DateTime(1969, 12, 31, 23, 59, 59, DateTimeKind.Utc)
        });

        s.DataType.Should().Be(DataType.DateTime(TimeUnit.Nanoseconds));
        s.AsTyped<long>()!.GetValue(0).Should().Be(1_000_000_000L);
        s.AsTyped<long>()!.GetValue(1).Should().Be(-1_000_000_000L);
    }

    [Fact]
    public void DateSeries_Indexer_ReturnsStoredValues()
    {
        var s = Series.FromValues("d", new[] { D1, new DateOnly(1970, 1, 1), new DateOnly(1969, 12, 31) });

        s[0].AsDate().Should().Be(D1);
        s[1].AsDate().Should().Be(new DateOnly(1970, 1, 1));
        s[2].AsDate().Should().Be(new DateOnly(1969, 12, 31));
    }

    [Fact]
    public void DateSeries_StoresDaysSinceUnixEpoch()
    {
        var s = Series.FromValues("d", new[] { new DateOnly(1970, 1, 1), new DateOnly(1970, 1, 11), new DateOnly(1969, 12, 31) });

        s.AsTyped<int>()!.GetValue(0).Should().Be(0);
        s.AsTyped<int>()!.GetValue(1).Should().Be(10);
        s.AsTyped<int>()!.GetValue(2).Should().Be(-1);
    }

    [Fact]
    public void NullableTemporalSeries_PreserveNullsAndValues()
    {
        var ts = Series.FromNullable("ts", new DateTime?[] { T1, null });
        var d = Series.FromNullable("d", new DateOnly?[] { null, D2 });

        ts[0].AsDateTime().Should().Be(T1);
        ts[1].IsNull.Should().BeTrue();
        d[0].IsNull.Should().BeTrue();
        d[1].AsDate().Should().Be(D2);
    }

    [Fact]
    public void FromValues_LocalKindDateTime_StoresWallClockTime()
    {
        var local = new DateTime(2024, 3, 10, 8, 0, 0, DateTimeKind.Local);

        var s = Series.FromValues("ts", new[] { local });

        s[0].AsDateTime().Should().Be(new DateTime(2024, 3, 10, 8, 0, 0));
    }

    [Fact]
    public void DateTimeSeries_ToString_ShowsValues()
    {
        var df = Frame();

        df.ToString().Should().Contain("2024-06-15T12:30:45").And.Contain("2024-06-15");
    }

    // ============================================================================
    // Component extraction
    // ============================================================================

    [Fact]
    public void Dt_Components_FromDateTime()
    {
        var dt = Series.FromValues("ts", new[] { T1 }).Dt;

        dt.Year()[0].AsInt32().Should().Be(2024);
        dt.Month()[0].AsInt32().Should().Be(6);
        dt.Day()[0].AsInt32().Should().Be(15);
        dt.Hour()[0].AsInt32().Should().Be(12);
        dt.Minute()[0].AsInt32().Should().Be(30);
        dt.Second()[0].AsInt32().Should().Be(45);
        dt.Millisecond()[0].AsInt32().Should().Be(123);
        dt.DayOfWeek()[0].AsInt32().Should().Be((int)DayOfWeek.Saturday);
    }

    [Fact]
    public void Dt_Components_FromPreEpochDateTime()
    {
        var dt = Series.FromValues("ts", new[] { T3 }).Dt;

        dt.Year()[0].AsInt32().Should().Be(1969);
        dt.Month()[0].AsInt32().Should().Be(7);
        dt.Day()[0].AsInt32().Should().Be(20);
        dt.Hour()[0].AsInt32().Should().Be(20);
        dt.Minute()[0].AsInt32().Should().Be(17);
        dt.Second()[0].AsInt32().Should().Be(40);
    }

    [Fact]
    public void Dt_Components_FromDate()
    {
        var dt = Series.FromValues("d", new[] { new DateOnly(2024, 2, 29) }).Dt;

        dt.Year()[0].AsInt32().Should().Be(2024);
        dt.Month()[0].AsInt32().Should().Be(2);
        dt.Day()[0].AsInt32().Should().Be(29);
        dt.DayOfYear()[0].AsInt32().Should().Be(60);
        dt.IsLeapYear()[0].AsBoolean().Should().BeTrue();
        dt.Quarter()[0].AsInt32().Should().Be(1);
    }

    [Theory]
    [InlineData(TimeUnit.Seconds)]
    [InlineData(TimeUnit.Milliseconds)]
    [InlineData(TimeUnit.Microseconds)]
    [InlineData(TimeUnit.Nanoseconds)]
    public void Dt_Components_RespectTimeUnit(TimeUnit unit)
    {
        var s = Series.FromValues("s", new[] { "2024-06-15 12:30:45" }).Str.ToDatetime(timeUnit: unit);

        s.DataType.Should().Be(DataType.DateTime(unit));
        s[0].AsDateTime().Should().Be(new DateTime(2024, 6, 15, 12, 30, 45));
        s.Dt.Hour()[0].AsInt32().Should().Be(12);
        s.Dt.EpochSeconds()[0].AsInt64().Should().Be(1_718_454_645L);
    }

    [Fact]
    public void Dt_Epoch_FromDateTime()
    {
        var dt = Series.FromValues("ts", new[] { T1 }).Dt;

        dt.EpochSeconds()[0].AsInt64().Should().Be(1_718_454_645L);
        dt.EpochMilliseconds()[0].AsInt64().Should().Be(1_718_454_645_123L);
    }

    // ============================================================================
    // Transformations
    // ============================================================================

    [Fact]
    public void Dt_AddDays_ShiftsValues()
    {
        var result = Series.FromValues("ts", new[] { T1, T3 }).Dt.AddDays(1);

        result[0].AsDateTime().Should().Be(T1.AddDays(1));
        result[1].AsDateTime().Should().Be(T3.AddDays(1));
    }

    [Fact]
    public void Dt_TruncateToMonth_ReturnsFirstOfMonth()
    {
        var result = Series.FromValues("ts", new[] { T1 }).Dt.TruncateToMonth();

        result[0].AsDateTime().Should().Be(new DateTime(2024, 6, 1));
    }

    [Fact]
    public void Dt_AddMonths_OnDate_ClampsToMonthEnd()
    {
        var result = Series.FromValues("d", new[] { new DateOnly(2024, 1, 31) }).Dt.AddMonths(1);

        result[0].AsDateTime().Should().Be(new DateTime(2024, 2, 29));
    }

    [Fact]
    public void Dt_ConvertTimeZone_ConvertsWallClockTime()
    {
        var s = Series.FromValues("ts", new[] { new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc) });

        // New York is UTC-4 during daylight saving time
        var result = s.Dt.ConvertTimeZone("UTC", "America/New_York");

        result[0].AsDateTime().Should().Be(new DateTime(2024, 6, 15, 8, 0, 0));
    }

    // ============================================================================
    // Row operations keep temporal types
    // ============================================================================

    [Fact]
    public void Filter_PreservesTemporalColumns()
    {
        var result = Frame().Filter(Series.FromValues("m", new[] { false, true, true }));

        result["ts"].DataType.Should().Be(DataType.DateTime(TimeUnit.Nanoseconds));
        result["d"].DataType.Should().Be(DataType.Date);
        result["ts"][0].AsDateTime().Should().Be(T2);
        result["ts"][1].AsDateTime().Should().Be(T3);
        result["d"][0].AsDate().Should().Be(D2);
        result["d"][1].AsDate().Should().Be(D3);
    }

    [Fact]
    public void Sort_ByDateTimeColumn_OrdersChronologically()
    {
        var result = Frame().Sort("ts");

        result["x"].ToArray<int>().Should().Equal(3, 2, 1);
        result["ts"][0].AsDateTime().Should().Be(T3);
        result["d"].DataType.Should().Be(DataType.Date);
    }

    [Fact]
    public void Sort_ByDateColumnDescending_OrdersChronologically()
    {
        var result = Frame().Sort("d", descending: true);

        result["x"].ToArray<int>().Should().Equal(1, 2, 3);
    }

    [Fact]
    public void LazySort_ByDateTimeColumn_OrdersChronologically()
    {
        var result = Frame().Lazy().Sort("ts").Collect();

        result["x"].ToArray<int>().Should().Equal(3, 2, 1);
    }

    [Fact]
    public void Unique_OnDateTimeColumn_KeepsDistinctValues()
    {
        var result = Series.FromValues("ts", new[] { T1, T2, T1, T3, T2 }).Unique();

        result.DataType.Should().Be(DataType.DateTime(TimeUnit.Nanoseconds));
        result.Length.Should().Be(3);
        result[0].AsDateTime().Should().Be(T1);
    }

    [Fact]
    public void DropNulls_OnDateColumn_KeepsDateType()
    {
        var result = Series.FromNullable("d", new DateOnly?[] { D1, null, D3 }).DropNulls();

        result.DataType.Should().Be(DataType.Date);
        result.Length.Should().Be(2);
        result[1].AsDate().Should().Be(D3);
    }

    [Fact]
    public void Filter_PreservesSmallIntegerAndUnsignedColumns()
    {
        var df = new DataFrame(
            Series.FromValues("i8", new sbyte[] { -1, 2, 3 }),
            Series.FromValues("u16", new ushort[] { 10, 20, 30 }));

        var result = df.Filter(Series.FromValues("m", new[] { true, false, true }));

        result["i8"].DataType.Should().Be(DataType.Int8);
        result["u16"].DataType.Should().Be(DataType.UInt16);
        result["i8"][0].AsInt8().Should().Be(-1);
        result["u16"][1].AsUInt16().Should().Be(30);
    }

    [Fact]
    public void VConcat_PreservesTemporalColumns()
    {
        var df = Frame();

        var result = Pl.Concat(df, df);

        result.Height.Should().Be(6);
        result["ts"].DataType.Should().Be(DataType.DateTime(TimeUnit.Nanoseconds));
        result["d"].DataType.Should().Be(DataType.Date);
        result["ts"][3].AsDateTime().Should().Be(T1);
        result["d"][5].AsDate().Should().Be(D3);
    }

    // ============================================================================
    // Comparisons
    // ============================================================================

    [Fact]
    public void LazyFilter_DateTimeColumnAgainstDateTimeLiteral()
    {
        var result = Frame().Lazy().Filter(Col("ts").Gt(new DateTime(2023, 6, 1))).Collect();

        result["x"].ToArray<int>().Should().Equal(1);
    }

    [Fact]
    public void LazyFilter_DateColumnAgainstDateOnlyLiteral()
    {
        var result = Frame().Lazy().Filter(Col("d").Le(new DateOnly(2023, 1, 2))).Collect();

        result["x"].ToArray<int>().Should().Equal(2, 3);
    }

    [Fact]
    public void LazyFilter_DateColumnAgainstDateTimeLiteral_ComparesAtMidnight()
    {
        var result = Frame().Lazy().Filter(Col("d").Ge(new DateTime(2023, 1, 2))).Collect();

        result["x"].ToArray<int>().Should().Equal(1, 2);
    }

    [Fact]
    public void LazyFilter_DateTimeEquality()
    {
        var result = Frame().Lazy().Filter(Col("ts").Eq(T2)).Collect();

        result["x"].ToArray<int>().Should().Equal(2);
    }

    [Fact]
    public void ScalarComparison_DateTimeSeries()
    {
        var s = Series.FromValues("ts", new[] { T1, T2, T3 });

        s.Lt(AnyValue.From(T1)).ToArray<bool>().Should().Equal(false, true, true);
        s.Ge(AnyValue.From(T2)).ToArray<bool>().Should().Equal(true, true, false);
    }

    [Fact]
    public void ScalarComparison_UnsignedIntegers()
    {
        var s = Series.FromValues("u", new byte[] { 1, 5, 10 });

        s.Gt(AnyValue.From((byte)4)).ToArray<bool>().Should().Equal(false, true, true);
        s.Gt(AnyValue.From(4)).ToArray<bool>().Should().Equal(false, true, true);
        s.Eq(AnyValue.From(5)).ToArray<bool>().Should().Equal(false, true, false);
    }

    // ============================================================================
    // Aggregations and expressions
    // ============================================================================

    [Fact]
    public void MinMax_OnDateColumn()
    {
        var s = Series.FromValues("d", new[] { D1, D2, D3 });

        s.Min().AsDate().Should().Be(D3);
        s.Max().AsDate().Should().Be(D1);
    }

    [Fact]
    public void MinMax_OnDateTimeColumn()
    {
        var s = Series.FromValues("ts", new[] { T1, T2, T3 });

        s.Min().AsDateTime().Should().Be(T3);
        s.Max().AsDateTime().Should().Be(T1);
    }

    [Fact]
    public void ValueCounts_OnDateColumn_KeepsDateType()
    {
        var counts = Series.FromValues("d", new[] { D1, D1, D2 }).ValueCounts();

        counts["d"].DataType.Should().Be(DataType.Date);
        counts["d"][0].AsDate().Should().Be(D1);
        counts["count"][0].AsInt32().Should().Be(2);
    }

    [Fact]
    public void WhenThenOtherwise_WithDateTimeBranches_KeepsDateTimeType()
    {
        var result = Frame().Lazy()
            .WithColumns(WhenExpr(Col("x").Gt(1)).Then(Col("ts")).Otherwise(Lit(T2)).As("pick"))
            .Collect();

        result["pick"].DataType.Should().Be(DataType.DateTime(TimeUnit.Nanoseconds));
        result["pick"][0].AsDateTime().Should().Be(T2);
        result["pick"][2].AsDateTime().Should().Be(T3);
    }

    [Fact]
    public void DtExpr_Year_InLazyFrame()
    {
        var result = Frame().Lazy().Select(Col("ts").DtExpr.Year().As("year")).Collect();

        result["year"].ToArray<int>().Should().Equal(2024, 2023, 1969);
    }

    // ============================================================================
    // CSV output
    // ============================================================================

    [Fact]
    public void ToCsv_FormatsDatesAndDateTimes()
    {
        var csv = Frame().Head(2).ToCsv(new Polaire.IO.CsvWriteOptions { LineEnding = "\n" });

        csv.Should().Be(
            "x,ts,d\n" +
            "1,2024-06-15 12:30:45,2024-06-15\n" +
            "2,2023-01-02 00:00:00,2023-01-02\n");
    }

    [Fact]
    public void Csv_RoundTrip_PreservesDates()
    {
        var df = new DataFrame(Series.FromValues("d", new[] { D1, D2, D3 }));

        var result = Pl.ReadCsv(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(df.ToCsv())));

        result["d"].DataType.Should().Be(DataType.Date);
        result["d"][0].AsDate().Should().Be(D1);
        result["d"][2].AsDate().Should().Be(D3);
    }
}
