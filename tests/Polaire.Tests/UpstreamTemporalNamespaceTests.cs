// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using FluentAssertions;
using Polaire.DataTypes;
using Xunit;

namespace Polaire.Tests;

/// <summary>
/// Ports of upstream Polars tests from py-polars/tests/unit/operations/namespaces/temporal/test_datetime.py
/// (tag py-2.0.0). Inputs and expected values are the upstream ones; each test names its source and records
/// the .NET adaptation. Not applicable here and therefore not ported: the time-zone parametrizations
/// (replace_time_zone / tz-aware ranges), millennium/century extraction, days_in_month (no direct method;
/// see DaysInMonth below for the MonthEnd-based equivalent), truncate/round with negative or per-row
/// duration strings (Polaire's Round/Floor take a TimeSpan), dt.date()/dt.time(), and Duration/Time columns.
/// </summary>
public class UpstreamTemporalNamespaceTests
{
    // Fixture series_of_int_dates: pl.Series([8401, 10000, 20000, 30000], dtype=pl.Date)
    private static readonly DateOnly[] IntDates =
    {
        new(1993, 1, 1), new(1997, 5, 19), new(2024, 10, 4), new(2052, 2, 20)
    };

    private static Series SeriesOfIntDates() => Series.FromValues("dates", IntDates);

    // Upstream casts the Date fixture to Datetime; here the same dates are built as midnight DateTimes.
    private static Series SeriesOfIntDatesAsDateTime() =>
        Series.FromValues("dates", IntDates.Select(d => d.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)).ToArray());

    private static DateTime Dt(int y, int mo, int d, int h = 0, int mi = 0, int s = 0) =>
        new(y, mo, d, h, mi, s, DateTimeKind.Utc);

    private static int[] Ints(Series s) => s.ToArray<int>();
    private static long[] Longs(Series s) => s.ToArray<long>();
    private static bool[] Bools(Series s) => Enumerable.Range(0, s.Length).Select(i => s[i].AsBoolean()).ToArray();
    private static DateTime[] Times(Series s) => Enumerable.Range(0, s.Length).Select(i => s[i].AsDateTime()).ToArray();

    private static Series Component(Series s, string unit) => unit switch
    {
        "year" => s.Dt.Year(),
        "iso_year" => s.Dt.IsoYear(),
        "quarter" => s.Dt.Quarter(),
        "month" => s.Dt.Month(),
        "week" => s.Dt.WeekOfYear(),
        "day" => s.Dt.Day(),
        "weekday" => s.Dt.DayOfWeek(),
        "ordinal_day" => s.Dt.DayOfYear(),
        "hour" => s.Dt.Hour(),
        "minute" => s.Dt.Minute(),
        "second" => s.Dt.Second(),
        "millisecond" => s.Dt.Millisecond(),
        "microsecond" => s.Dt.Microsecond(),
        "nanosecond" => s.Dt.Nanosecond(),
        _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unknown component")
    };

    // test_dt_to_string
    [Fact]
    public void DtToString()
    {
        var s = SeriesOfIntDates();

        // The fixture is defined by its day numbers; make sure the port stores exactly those.
        s.DataType.Should().Be(DataType.Date);
        Enumerable.Range(0, 4).Select(i => s.AsTyped<int>()!.GetValue(i)).Should().Equal(8401, 10000, 20000, 30000);

        s.Dt.Strftime("%F").ToArray<string>().Should().Equal("1993-01-01", "1997-05-19", "2024-10-04", "2052-02-20");
    }

    // test_dt_extract_datetime_component (time_zone=None). Upstream dtypes are Int8/Int16/Int32 per component;
    // Polaire returns Int32 for all of them. weekday: upstream is Monday=1..Sunday=7 and Polaire follows .NET
    // (Sunday=0); the fixture holds a Friday, a Monday, a Friday and a Tuesday, so the values coincide.
    [Theory]
    [InlineData("year", new[] { 1993, 1997, 2024, 2052 })]
    [InlineData("iso_year", new[] { 1992, 1997, 2024, 2052 })]
    [InlineData("quarter", new[] { 1, 2, 4, 1 })]
    [InlineData("month", new[] { 1, 5, 10, 2 })]
    [InlineData("week", new[] { 53, 21, 40, 8 })]
    [InlineData("day", new[] { 1, 19, 4, 20 })]
    [InlineData("weekday", new[] { 5, 1, 5, 2 })]
    [InlineData("ordinal_day", new[] { 1, 139, 278, 51 })]
    public void DtExtractDatetimeComponent(string unit, int[] expected)
    {
        var fromDate = Component(SeriesOfIntDates(), unit);
        fromDate.DataType.Should().Be(DataType.Int32);
        Ints(fromDate).Should().Equal(expected);

        var fromDateTime = Component(SeriesOfIntDatesAsDateTime(), unit);
        fromDateTime.DataType.Should().Be(DataType.Int32);
        Ints(fromDateTime).Should().Equal(expected);
    }

    // Fixture series_of_str_dates parsed with "%Y-%m-%d %H:%M:%S.%9f":
    // ["2020-01-01 00:00:00.000000000", "2020-02-02 03:20:10.987654321"].
    // Adaptation: the values are constructed directly; .NET ticks keep 987654300 of the 987654321 nanoseconds.
    private static Series StrptimeFixture() => Series.FromValues("dates", new[]
    {
        Dt(2020, 1, 1),
        Dt(2020, 2, 2, 3, 20, 10).AddTicks(9_876_543)
    });

    // test_strptime_extract_times
    [Theory]
    [InlineData("hour", new[] { 0, 3 })]
    [InlineData("minute", new[] { 0, 20 })]
    [InlineData("second", new[] { 0, 10 })]
    [InlineData("millisecond", new[] { 0, 987 })]
    public void StrptimeExtractTimes(string unit, int[] expected)
    {
        Ints(Component(StrptimeFixture(), unit)).Should().Equal(expected);
    }

    // test_strptime_extract_times, microsecond case. Upstream expects the whole sub-second part in
    // microseconds; Polaire's Microsecond() returns the .NET component within the millisecond (654).
    [Fact(Skip = "Polaire Dt.Microsecond() returns the microsecond component (0-999), Polars returns microseconds within the second")]
    public void StrptimeExtractTimes_Microsecond()
    {
        Ints(StrptimeFixture().Dt.Microsecond()).Should().Equal(0, 987654);
    }

    // test_strptime_extract_times, nanosecond case. Same divergence, and .NET ticks cannot hold the last 21 ns.
    [Fact(Skip = "Polaire Dt.Nanosecond() returns the nanosecond component (0-900), Polars returns nanoseconds within the second")]
    public void StrptimeExtractTimes_Nanosecond()
    {
        Ints(StrptimeFixture().Dt.Nanosecond()).Should().Equal(0, 987654321);
    }

    // Upstream: pl.datetime_range(start, stop, timedelta(minutes=30), eager=True), which includes stop.
    // Pl.DateTimeRange excludes its end, so the end is pushed one step past stop.
    private static Series HalfHourRange(DateTime start, DateTime stop) =>
        Pl.DateTimeRange("dates", start, stop.AddMinutes(30), TimeSpan.FromMinutes(30));

    // test_truncate (every="1h"). Adaptation: Polaire has no duration-string truncate; Floor(1h) and
    // TruncateToHour are both checked. The ms/us time-unit parametrizations are not reproduced.
    [Fact]
    public void Truncate()
    {
        var start = Dt(2022, 1, 1);
        var stop = Dt(2022, 1, 2);
        var s = HalfHourRange(start, stop);
        s.Length.Should().Be(49);

        foreach (var output in new[] { s.Dt.Floor(TimeSpan.FromHours(1)), s.Dt.TruncateToHour() })
        {
            var o = Times(output);
            o[0].Should().Be(start);
            o[1].Should().Be(start);
            o[2].Should().Be(start.AddHours(1));
            o[3].Should().Be(start.AddHours(1));
            o[^3].Should().Be(stop.AddHours(-1));
            o[^2].Should().Be(stop.AddHours(-1));
            o[^1].Should().Be(stop);
        }
    }

    // test_round (every="1h")
    [Fact]
    public void Round()
    {
        var start = Dt(2022, 1, 1);
        var stop = Dt(2022, 1, 2);
        var o = Times(HalfHourRange(start, stop).Dt.Round(TimeSpan.FromHours(1)));

        o[0].Should().Be(start);
        o[1].Should().Be(start.AddHours(1));
        o[2].Should().Be(start.AddHours(1));
        o[3].Should().Be(start.AddHours(2));
        o[^3].Should().Be(stop.AddHours(-1));
        o[^2].Should().Be(stop);
        o[^1].Should().Be(stop);
    }

    // test_round_expr, the every_lit column (round "1d") and the all_lit check (round "1h").
    // The all_expr/date_lit columns round by a per-row "every" column with calendar units ("1y", "1mo"),
    // which Polaire's TimeSpan-based Round cannot express.
    [Fact]
    public void RoundExpr_EveryLit()
    {
        var date = Series.FromNullable("date", new DateTime?[]
        {
            Dt(2022, 11, 14), Dt(2023, 10, 11), Dt(2022, 3, 20, 5, 7, 18), Dt(2022, 4, 3, 13, 30, 32), null, Dt(2022, 12, 1)
        });

        var everyLit = date.Dt.Round(TimeSpan.FromDays(1));
        everyLit.NullCount.Should().Be(1);
        everyLit[0].AsDateTime().Should().Be(Dt(2022, 11, 14));
        everyLit[1].AsDateTime().Should().Be(Dt(2023, 10, 11));
        everyLit[2].AsDateTime().Should().Be(Dt(2022, 3, 20));
        everyLit[3].AsDateTime().Should().Be(Dt(2022, 4, 4));
        everyLit[4].IsNull.Should().BeTrue();
        everyLit[5].AsDateTime().Should().Be(Dt(2022, 12, 1));

        var allLit = Series.FromValues("all_lit", new[] { Dt(2022, 3, 20, 5, 7) }).Dt.Round(TimeSpan.FromHours(1));
        Times(allLit).Should().Equal(Dt(2022, 3, 20, 5));
    }

    // test_epoch_matches_timestamp. Polaire has no dt.timestamp(); the units are checked against each other
    // and against the static Unix values of the upstream dates. epoch("d") has no Polaire counterpart.
    [Fact]
    public void EpochMatchesTimestamp()
    {
        var dates = Series.FromValues("dates", new[] { Dt(2001, 1, 1), Dt(2001, 2, 1, 10, 8, 9) });

        var seconds = Longs(dates.Dt.EpochSeconds());
        var ms = Longs(dates.Dt.EpochMilliseconds());
        var us = Longs(dates.Dt.EpochMicroseconds());
        var ns = Longs(dates.Dt.EpochNanoseconds());

        seconds.Should().Equal(978_307_200L, 981_022_089L);
        ms.Should().Equal(seconds.Select(v => v * 1000).ToArray());
        us.Should().Equal(ms.Select(v => v * 1000).ToArray());
        ns.Should().Equal(us.Select(v => v * 1000).ToArray());
        seconds.Should().Equal(ms.Select(v => v / 1000).ToArray());
    }

    // Upstream builds pl.date_range / pl.datetime_range(1990-01-01, 2004-01-01, "1y"); the same 15 values are listed here.
    private static DateOnly[] YearlyJan1() => Enumerable.Range(1990, 15).Select(y => new DateOnly(y, 1, 1)).ToArray();

    // test_iso_year (date_range and the ns datetime_range without time zone)
    [Fact]
    public void IsoYear()
    {
        var expected = new[] { 1990, 1991, 1992, 1992, 1993, 1994, 1996, 1997, 1998, 1998, 1999, 2001, 2002, 2003, 2004 };

        Ints(Series.FromValues("dates", YearlyJan1()).Dt.IsoYear()).Should().Equal(expected);
        Ints(Series.FromValues("dates", YearlyJan1().Select(d => d.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)).ToArray())
            .Dt.IsoYear()).Should().Equal(expected);
    }

    // test_is_leap_year (date_range and the ns datetime_range without time zone)
    [Fact]
    public void IsLeapYear()
    {
        var expected = new[]
        {
            false, false, true, false, false, false, true, false, false, false, true, false, false, false, true
        };

        Bools(Series.FromValues("dates", YearlyJan1()).Dt.IsLeapYear()).Should().Equal(expected);
        Bools(Series.FromValues("dates", YearlyJan1().Select(d => d.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)).ToArray())
            .Dt.IsLeapYear()).Should().Equal(expected);
    }

    // test_days_in_month (date case). Polaire has no days_in_month; MonthEnd().Dt.Day() is the equivalent.
    [Theory]
    [InlineData(1900, 28)] // divisible by 100 but not by 400
    [InlineData(2025, 28)] // not divisible by 4
    [InlineData(2000, 29)] // divisible by 400
    [InlineData(2004, 29)] // divisible by 4 but not by 100
    public void DaysInMonth(int year, int febDays)
    {
        var firstOfEachMonth = Enumerable.Range(1, 12).Select(m => new DateOnly(year, m, 1)).ToArray();

        Ints(Series.FromValues("dates", firstOfEachMonth).Dt.MonthEnd().Dt.Day())
            .Should().Equal(31, febDays, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31);
    }

    // test_quarter: datetime_range(2022-01-01, 2022-12-01, "1mo")
    [Fact]
    public void Quarter()
    {
        var months = Enumerable.Range(1, 12).Select(m => Dt(2022, m, 1)).ToArray();

        Ints(Series.FromValues("dates", months).Dt.Quarter()).Should().Equal(1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 4);
    }

    // test_year_empty_df
    [Fact]
    public void YearEmptyDf()
    {
        var df = new DataFrame(Series.FromValues("date", Array.Empty<DateOnly>()));

        df["date"].DataType.Should().Be(DataType.Date);
        df["date"].Dt.Year().DataType.Should().Be(DataType.Int32);
        df["date"].Dt.Year().Length.Should().Be(0);
    }

    // test_weekday for ms/us/ns time units. Upstream cast_time_unit is replaced by parsing into the unit.
    // Friday is 5 in both numberings.
    [Theory]
    [InlineData(TimeUnit.Milliseconds)]
    [InlineData(TimeUnit.Microseconds)]
    [InlineData(TimeUnit.Nanoseconds)]
    public void Weekday(TimeUnit timeUnit)
    {
        var friday = Series.FromValues("friday", new[] { "2023-02-17 00:00:00" }).Str.ToDatetime(timeUnit: timeUnit);

        friday.DataType.Should().Be(DataType.DateTime(timeUnit));
        friday.Dt.DayOfWeek()[0].AsInt32().Should().Be(5);
    }

    // test_weekday, Date case
    [Fact]
    public void Weekday_Date()
    {
        Series.FromValues("friday", new[] { new DateOnly(2023, 2, 17) }).Dt.DayOfWeek()[0].AsInt32().Should().Be(5);
    }

    // test_offset_by: datetime_range(2000-01-01, 2020-01-01, "1y") offset by "1y" and "-1y2mo".
    [Fact]
    public void OffsetBy()
    {
        var dates = Series.FromValues("dates", Enumerable.Range(2000, 21).Select(y => Dt(y, 1, 1)).ToArray());

        var datePlus1Y = dates.Dt.OffsetBy("1y");
        var dateMin = dates.Dt.OffsetBy("-1y2mo");

        Ints(datePlus1Y.Dt.Day()).Should().OnlyContain(d => d == 1);
        Ints(dateMin.Dt.Day()).Should().OnlyContain(d => d == 1);

        Times(dateMin).Should().Equal(Enumerable.Range(1998, 21).Select(y => Dt(y, 11, 1)).ToArray());
        Times(datePlus1Y).Should().Equal(Enumerable.Range(2001, 21).Select(y => Dt(y, 1, 1)).ToArray());
    }
}
