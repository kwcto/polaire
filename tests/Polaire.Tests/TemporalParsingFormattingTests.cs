// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using System.Globalization;
using FluentAssertions;
using Polaire.DataTypes;
using Xunit;
using static Polaire.Expressions.Expr;

namespace Polaire.Tests;

/// <summary>
/// Tests for Str.Strptime / ToDate / ToDatetime, Dt.Strftime and Dt.OffsetBy, including the
/// Polars (chrono) '%' format specifiers and Polars duration strings.
/// </summary>
public class TemporalParsingFormattingTests
{
    private static readonly DateTime T1 = new(2024, 6, 15, 12, 30, 45, 123, DateTimeKind.Utc);

    private static Series Strings(params string?[] values) => Series.FromValues("s", values);

    // ============================================================================
    // Strptime: chrono formats
    // ============================================================================

    [Fact]
    public void ToDate_WithFormat_ParsesAndKeepsNulls()
    {
        var result = Strings("2024-06-15", null, "1999-12-31").Str.ToDate("%Y-%m-%d");

        result.DataType.Should().Be(DataType.Date);
        result[0].AsDate().Should().Be(new DateOnly(2024, 6, 15));
        result[1].IsNull.Should().BeTrue();
        result[2].AsDate().Should().Be(new DateOnly(1999, 12, 31));
    }

    [Theory]
    [InlineData("15/06/2024", "%d/%m/%Y")]
    [InlineData("20240615", "%Y%m%d")]
    [InlineData("Jun 15, 2024", "%b %d, %Y")]
    [InlineData("june 15 2024", "%B %d %Y")]
    [InlineData("Saturday, June 15, 2024", "%A, %B %d, %Y")]
    [InlineData("24-06-15", "%y-%m-%d")]
    [InlineData("2024-167", "%Y-%j")]
    [InlineData("06/15/24", "%D")]
    [InlineData("2024-06-15", "%F")]
    [InlineData("2024-6-5x", "%Y-%m-%dx")]
    public void ToDate_ChronoFormats(string text, string format)
    {
        var expected = text == "2024-6-5x" ? new DateOnly(2024, 6, 5) : new DateOnly(2024, 6, 15);

        Strings(text).Str.ToDate(format)[0].AsDate().Should().Be(expected);
    }

    [Theory]
    [InlineData("69-01-01", 1969)]
    [InlineData("68-01-01", 2068)]
    [InlineData("00-01-01", 2000)]
    public void ToDate_TwoDigitYear_UsesPosixPivot(string text, int expectedYear)
    {
        Strings(text).Str.ToDate("%y-%m-%d")[0].AsDate().Year.Should().Be(expectedYear);
    }

    [Theory]
    [InlineData("2024-06-15 12:30:45", "%Y-%m-%d %H:%M:%S", 0)]
    [InlineData("2024-06-15 12:30:45.123", "%Y-%m-%d %H:%M:%S%.f", 123)]
    [InlineData("2024-06-15 12:30:45", "%Y-%m-%d %H:%M:%S%.f", 0)]
    [InlineData("2024-06-15 12:30:45.123", "%Y-%m-%d %H:%M:%S%.3f", 123)]
    [InlineData("2024-06-15 12:30:45.123456", "%Y-%m-%d %H:%M:%S%.6f", 123)]
    [InlineData("2024-06-15 12:30:45.123000000", "%Y-%m-%d %H:%M:%S.%f", 123)]
    [InlineData("2024-06-15T12:30:45", "%Y-%m-%dT%H:%M:%S", 0)]
    [InlineData("2024-06-15 12:30:45", "%F %T", 0)]
    [InlineData("15 Jun 2024 12:30:45 PM", "%d %b %Y %I:%M:%S %p", 0)]
    public void ToDatetime_ChronoFormats(string text, string format, int expectedMillisecond)
    {
        var result = Strings(text).Str.ToDatetime(format);

        result.DataType.Should().Be(DataType.DateTime(TimeUnit.Nanoseconds));
        var value = result[0].AsDateTime();
        value.Should().Be(new DateTime(2024, 6, 15, 12, 30, 45).AddMilliseconds(expectedMillisecond)
            .AddTicks(text.EndsWith("123456") ? 4560 : 0));
    }

    [Fact]
    public void ToDatetime_TwelveHourClock()
    {
        var result = Strings("2024-06-15 12:05 AM", "2024-06-15 01:30 pm").Str.ToDatetime("%Y-%m-%d %I:%M %p");

        result[0].AsDateTime().Should().Be(new DateTime(2024, 6, 15, 0, 5, 0));
        result[1].AsDateTime().Should().Be(new DateTime(2024, 6, 15, 13, 30, 0));
    }

    [Theory]
    [InlineData("2024-06-15T12:30:45+02:00", "2024-06-15 10:30:45")]
    [InlineData("2024-06-15T12:30:45-0530", "2024-06-15 18:00:45")]
    [InlineData("2024-06-15T12:30:45+05", "2024-06-15 07:30:45")]
    [InlineData("2024-06-15T12:30:45Z", "2024-06-15 12:30:45")]
    public void ToDatetime_WithOffset_ConvertsToUtc(string text, string expectedUtc)
    {
        var result = Strings(text).Str.ToDatetime("%Y-%m-%dT%H:%M:%S%z");

        result[0].AsDateTime().Should().Be(DateTime.Parse(expectedUtc, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void ToDatetime_UnixTimestamp()
    {
        var result = Strings("1718454645", "-1").Str.ToDatetime("%s");

        result[0].AsDateTime().Should().Be(new DateTime(2024, 6, 15, 12, 30, 45));
        result[1].AsDateTime().Should().Be(new DateTime(1969, 12, 31, 23, 59, 59));
    }

    // ============================================================================
    // Strptime: other format kinds, strictness, units
    // ============================================================================

    [Fact]
    public void ToDate_WithoutFormat_ParsesIsoDates()
    {
        var result = Strings("2024-02-29", "2024-06-15T23:59:59").Str.ToDate();

        result[0].AsDate().Should().Be(new DateOnly(2024, 2, 29));
        result[1].AsDate().Should().Be(new DateOnly(2024, 6, 15));
    }

    [Fact]
    public void ToDatetime_WithoutFormat_HandlesOffsetsAndNaiveValues()
    {
        var result = Strings("2024-06-15T12:30:45Z", "2024-06-15T12:30:45+02:00", "2024-06-15 12:30:45").Str.ToDatetime();

        result[0].AsDateTime().Should().Be(new DateTime(2024, 6, 15, 12, 30, 45));
        result[1].AsDateTime().Should().Be(new DateTime(2024, 6, 15, 10, 30, 45));
        result[2].AsDateTime().Should().Be(new DateTime(2024, 6, 15, 12, 30, 45));
    }

    [Fact]
    public void ToDate_WithDotNetFormat()
    {
        Strings("15.06.2024").Str.ToDate("dd.MM.yyyy")[0].AsDate().Should().Be(new DateOnly(2024, 6, 15));
    }

    [Theory]
    [InlineData("2023-02-29")]
    [InlineData("2024-13-01")]
    [InlineData("2024-06-15 extra")]
    [InlineData("not a date")]
    [InlineData("")]
    public void ToDate_Strict_ThrowsForInvalidValues(string text)
    {
        var act = () => Strings("2024-06-15", text).Str.ToDate("%Y-%m-%d");

        act.Should().Throw<FormatException>()
            .WithMessage("*column 's'*1 of 2 values*strict: false*");
    }

    [Fact]
    public void ToDate_NonStrict_ReturnsNullForInvalidValues()
    {
        var result = Strings("2024-06-15", "garbage", "2023-02-29").Str.ToDate("%Y-%m-%d", strict: false);

        result[0].AsDate().Should().Be(new DateOnly(2024, 6, 15));
        result[1].IsNull.Should().BeTrue();
        result[2].IsNull.Should().BeTrue();
    }

    [Fact]
    public void ToDatetime_TimeOnlyFormat_FailsWithoutDate()
    {
        var act = () => Strings("12:30").Str.ToDatetime("%H:%M");

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void ToDatetime_WithTimeUnit_StoresRequestedUnit()
    {
        var result = Strings("1970-01-01 00:00:01.5").Str.ToDatetime("%Y-%m-%d %H:%M:%S%.f", timeUnit: TimeUnit.Milliseconds);

        result.DataType.Should().Be(DataType.DateTime(TimeUnit.Milliseconds));
        result.AsTyped<long>()!.GetValue(0).Should().Be(1500L);
    }

    [Fact]
    public void Strptime_NonTemporalTarget_Throws()
    {
        var act = () => Strings("1").Str.Strptime(DataType.Int32);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Strptime_UnsupportedSpecifier_Throws()
    {
        var act = () => Strings("2024").Str.ToDate("%Y%Q");

        act.Should().Throw<FormatException>().WithMessage("*%Q*");
    }

    [Fact]
    public void StrExpr_ToDate_InLazyFrame()
    {
        var df = new DataFrame(Strings("2024-06-15", "2023-01-02"));

        var result = df.Lazy()
            .WithColumns(Col("s").StrExpr.ToDate("%Y-%m-%d").As("d"))
            .Filter(Col("d").Gt(new DateOnly(2024, 1, 1)))
            .Collect();

        result.Height.Should().Be(1);
        result["d"][0].AsDate().Should().Be(new DateOnly(2024, 6, 15));
    }

    // ============================================================================
    // Strftime
    // ============================================================================

    [Theory]
    [InlineData("%Y-%m-%d %H:%M:%S", "2024-06-15 12:30:45")]
    [InlineData("%F %T", "2024-06-15 12:30:45")]
    [InlineData("%Y-%m-%dT%H:%M:%S%.f", "2024-06-15T12:30:45.123")]
    [InlineData("%H:%M:%S%.3f", "12:30:45.123")]
    [InlineData("%H:%M:%S%.6f", "12:30:45.123000")]
    [InlineData("%H:%M:%S%.9f", "12:30:45.123000000")]
    [InlineData("%f", "123000000")]
    [InlineData("%3f", "123")]
    [InlineData("%a %A %b %B", "Sat Saturday Jun June")]
    [InlineData("%d/%m/%y", "15/06/24")]
    [InlineData("%D", "06/15/24")]
    [InlineData("%j %u %w", "167 6 6")]
    [InlineData("%I:%M %p %P", "12:30 PM pm")]
    [InlineData("%-m/%-d/%Y", "6/15/2024")]
    [InlineData("%e|%_m|%0e", "15| 6|15")]
    [InlineData("%C %G-W%V", "20 2024-W24")]
    [InlineData("%s", "1718454645")]
    [InlineData("100%% %z %:z", "100% +0000 +00:00")]
    [InlineData("%c", "Sat Jun 15 12:30:45 2024")]
    public void Strftime_ChronoSpecifiers(string format, string expected)
    {
        Series.FromValues("ts", new[] { T1 }).Dt.Strftime(format)[0].AsString().Should().Be(expected);
    }

    [Fact]
    public void Strftime_AutoFraction_OmitsZeroFraction()
    {
        var s = Series.FromValues("ts", new[] { new DateTime(2024, 1, 2, 3, 4, 5), new DateTime(2024, 1, 2, 3, 4, 5).AddTicks(1234) });

        var result = s.Dt.Strftime("%T%.f");

        result[0].AsString().Should().Be("03:04:05");
        result[1].AsString().Should().Be("03:04:05.000123400");
    }

    [Fact]
    public void Strftime_DotNetFormat_StillSupported()
    {
        Series.FromValues("ts", new[] { T1 }).Dt.Strftime("yyyy/MM/dd HH:mm")[0].AsString().Should().Be("2024/06/15 12:30");
    }

    [Fact]
    public void Strftime_OnDateColumn()
    {
        var s = Series.FromNullable("d", new DateOnly?[] { new DateOnly(2024, 2, 29), null });

        var result = s.Dt.Strftime("%d %B %Y");

        result[0].AsString().Should().Be("29 February 2024");
        result[1].IsNull.Should().BeTrue();
    }

    [Fact]
    public void Strftime_ThenStrptime_RoundTrips()
    {
        const string format = "%Y-%m-%d %H:%M:%S%.f";
        var original = Series.FromValues("ts", new[] { T1, new DateTime(1969, 7, 20, 20, 17, 40) });

        var roundTripped = original.Dt.Strftime(format).Str.ToDatetime(format);

        roundTripped[0].AsDateTime().Should().Be(T1);
        roundTripped[1].AsDateTime().Should().Be(new DateTime(1969, 7, 20, 20, 17, 40));
    }

    [Fact]
    public void DtExpr_Strftime_InLazyFrame()
    {
        var df = new DataFrame(Series.FromValues("ts", new[] { T1 }));

        var result = df.Lazy().Select(Col("ts").DtExpr.Strftime("%Y%m%d").As("key")).Collect();

        result["key"][0].AsString().Should().Be("20240615");
    }

    // ============================================================================
    // OffsetBy
    // ============================================================================

    [Theory]
    [InlineData("2024-01-31", "1mo", "2024-02-29")]
    [InlineData("2023-01-31", "1mo", "2023-02-28")]
    [InlineData("2024-03-31", "-1mo", "2024-02-29")]
    [InlineData("2024-02-29", "1y", "2025-02-28")]
    [InlineData("2024-01-15", "1q", "2024-04-15")]
    [InlineData("2024-01-15", "2w", "2024-01-29")]
    [InlineData("2024-01-15", "-3d", "2024-01-12")]
    [InlineData("2024-01-15", "1y2mo3d", "2025-03-18")]
    [InlineData("2024-01-15", "12h", "2024-01-15")]
    [InlineData("2024-01-15", "-12h", "2024-01-14")]
    [InlineData("2024-01-15", "36h", "2024-01-16")]
    public void OffsetBy_OnDate_IsCalendarAwareAndKeepsDateType(string start, string by, string expected)
    {
        var s = Series.FromValues("d", new[] { DateOnly.Parse(start, CultureInfo.InvariantCulture) });

        var result = s.Dt.OffsetBy(by);

        result.DataType.Should().Be(DataType.Date);
        result[0].AsDate().Should().Be(DateOnly.Parse(expected, CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("1h30m", "2024-06-15 14:00:45.123")]
    [InlineData("-1d", "2024-06-14 12:30:45.123")]
    [InlineData("90s", "2024-06-15 12:32:15.123")]
    [InlineData("877ms", "2024-06-15 12:30:46")]
    [InlineData("1us", "2024-06-15 12:30:45.123001")]
    [InlineData("1mo1h", "2024-07-15 13:30:45.123")]
    public void OffsetBy_OnDateTime(string by, string expected)
    {
        var result = Series.FromValues("ts", new[] { T1 }).Dt.OffsetBy(by);

        result.DataType.Should().Be(DataType.DateTime(TimeUnit.Nanoseconds));
        result[0].AsDateTime().Should().Be(DateTime.Parse(expected, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void OffsetBy_KeepsTimeUnitAndNulls()
    {
        var s = Strings("2024-01-31 08:00:00", null).Str.ToDatetime(timeUnit: TimeUnit.Milliseconds);

        var result = s.Dt.OffsetBy("1mo");

        result.DataType.Should().Be(DataType.DateTime(TimeUnit.Milliseconds));
        result[0].AsDateTime().Should().Be(new DateTime(2024, 2, 29, 8, 0, 0));
        result[1].IsNull.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("-")]
    [InlineData("1")]
    [InlineData("d")]
    [InlineData("1x")]
    [InlineData("1.5h")]
    [InlineData("1d-2h")]
    [InlineData("1D")]
    [InlineData("3i")]
    public void OffsetBy_InvalidDuration_Throws(string by)
    {
        var act = () => Series.FromValues("ts", new[] { T1 }).Dt.OffsetBy(by);

        act.Should().Throw<FormatException>().WithMessage("Invalid duration*");
    }

    [Fact]
    public void DtExpr_OffsetBy_InLazyFrame()
    {
        var df = new DataFrame(Series.FromValues("d", new[] { new DateOnly(2024, 1, 31) }));

        var result = df.Lazy().WithColumns(Col("d").DtExpr.OffsetBy("1mo").As("next")).Collect();

        result["next"].DataType.Should().Be(DataType.Date);
        result["next"][0].AsDate().Should().Be(new DateOnly(2024, 2, 29));
    }
}
