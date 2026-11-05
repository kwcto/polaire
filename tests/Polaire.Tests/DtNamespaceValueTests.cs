// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using FluentAssertions;
using Polaire.DataTypes;
using Xunit;

namespace Polaire.Tests;

/// <summary>
/// Value-level tests for the Series.Dt namespace. The earlier DateTimeOperationsTests mostly check
/// result types, lengths and value ranges; these pin exact values: ISO-week and leap-year boundaries,
/// pre-epoch timestamps, calendar clamping, rounding at interval boundaries, null propagation, and
/// Date (not DateTime) inputs.
/// </summary>
public class DtNamespaceValueTests
{
    // 2024-06-15 is a Saturday, 2023-01-02 a Monday, 1969-07-20 (pre-epoch) a Sunday.
    private static readonly DateTime T1 = new(2024, 6, 15, 12, 30, 45, 123, DateTimeKind.Utc);
    private static readonly DateTime T2 = new(2023, 1, 2, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime T3 = new(1969, 7, 20, 20, 17, 40, DateTimeKind.Utc);

    private static Series Times3() => Series.FromValues("ts", new[] { T1, T2, T3 });
    private static Series Dates(params DateOnly[] dates) => Series.FromValues("d", dates);
    private static DateOnly D(int y, int m, int d) => new(y, m, d);
    private static DateTime Dt(int y, int mo, int d, int h = 0, int mi = 0, int s = 0, int ms = 0) =>
        new(y, mo, d, h, mi, s, ms, DateTimeKind.Utc);

    private static int[] Ints(Series s) => s.ToArray<int>();
    private static long[] Longs(Series s) => s.ToArray<long>();
    private static bool[] Bools(Series s) => Enumerable.Range(0, s.Length).Select(i => s[i].AsBoolean()).ToArray();
    private static DateTime[] Times(Series s) => Enumerable.Range(0, s.Length).Select(i => s[i].AsDateTime()).ToArray();

    // ============================================================================
    // Component extraction
    // ============================================================================

    [Fact]
    public void YearMonthDay_ExactValues()
    {
        var s = Times3();

        Ints(s.Dt.Year()).Should().Equal(2024, 2023, 1969);
        Ints(s.Dt.Month()).Should().Equal(6, 1, 7);
        Ints(s.Dt.Day()).Should().Equal(15, 2, 20);
    }

    [Fact]
    public void TimeComponents_ExactValues()
    {
        var s = Times3();

        Ints(s.Dt.Hour()).Should().Equal(12, 0, 20);
        Ints(s.Dt.Minute()).Should().Equal(30, 0, 17);
        Ints(s.Dt.Second()).Should().Equal(45, 0, 40);
        Ints(s.Dt.Millisecond()).Should().Equal(123, 0, 0);
    }

    [Fact]
    public void SubMillisecondComponents_FollowDotNetSemantics()
    {
        // 12:30:45.1234567 -> Microsecond/Nanosecond are the components within the millisecond/microsecond,
        // as in .NET's DateTime.Microsecond and DateTime.Nanosecond. (Polars returns the whole sub-second
        // part instead: 123456 and 123456700.)
        var s = Series.FromValues("ts", new[] { new DateTime(2024, 6, 15, 12, 30, 45, DateTimeKind.Utc).AddTicks(1234567) });

        Ints(s.Dt.Millisecond()).Should().Equal(123);
        Ints(s.Dt.Microsecond()).Should().Equal(456);
        Ints(s.Dt.Nanosecond()).Should().Equal(700);
    }

    [Fact]
    public void Components_OnDateColumn_TimePartsAreZero()
    {
        var s = Dates(D(2024, 6, 15), D(1969, 7, 20));

        Ints(s.Dt.Year()).Should().Equal(2024, 1969);
        Ints(s.Dt.Month()).Should().Equal(6, 7);
        Ints(s.Dt.Day()).Should().Equal(15, 20);
        Ints(s.Dt.Hour()).Should().Equal(0, 0);
        Ints(s.Dt.Minute()).Should().Equal(0, 0);
        Ints(s.Dt.Second()).Should().Equal(0, 0);
        Ints(s.Dt.Millisecond()).Should().Equal(0, 0);
    }

    [Fact]
    public void Components_AreInt32AndKeepTheSeriesName()
    {
        var year = Times3().Dt.Year();

        year.Name.Should().Be("ts");
        year.DataType.Should().Be(DataType.Int32);
        year.NullCount.Should().Be(0);
    }

    // ============================================================================
    // Day of week, day of year, ISO weeks, quarters
    // ============================================================================

    [Fact]
    public void DayOfWeek_FollowsDotNet_SundayIsZero()
    {
        // .NET DayOfWeek numbering (Sunday = 0 ... Saturday = 6); Polars' weekday() uses Monday = 1 ... Sunday = 7.
        Ints(Times3().Dt.DayOfWeek()).Should().Equal(6, 1, 0);

        var week = Dates(D(2024, 6, 10), D(2024, 6, 11), D(2024, 6, 12), D(2024, 6, 13), D(2024, 6, 14), D(2024, 6, 15), D(2024, 6, 16));
        Ints(week.Dt.DayOfWeek()).Should().Equal(1, 2, 3, 4, 5, 6, 0);
    }

    [Fact]
    public void DayOfYear_ExactValues_IncludingLeapYears()
    {
        Ints(Times3().Dt.DayOfYear()).Should().Equal(167, 2, 201);

        var s = Dates(D(2024, 12, 31), D(2023, 12, 31), D(2024, 3, 1), D(2023, 3, 1), D(2024, 1, 1));
        Ints(s.Dt.DayOfYear()).Should().Equal(366, 365, 61, 60, 1);
    }

    [Fact]
    public void WeekOfYear_And_IsoYear_ExactValues()
    {
        var s = Times3();

        Ints(s.Dt.WeekOfYear()).Should().Equal(24, 1, 29);
        Ints(s.Dt.IsoYear()).Should().Equal(2024, 2023, 1969);
    }

    [Fact]
    public void WeekOfYear_And_IsoYear_AtYearBoundaries()
    {
        // 2021-01-01 (Fri) belongs to ISO week 53 of 2020; 2024-12-30 (Mon) starts week 1 of 2025;
        // 2019-12-30 (Mon) starts week 1 of 2020; 2027-01-03 (Sun) ends week 53 of 2026; 2024-12-29 (Sun) ends week 52 of 2024.
        var s = Dates(D(2021, 1, 1), D(2024, 12, 30), D(2019, 12, 30), D(2027, 1, 3), D(2024, 12, 29));

        Ints(s.Dt.WeekOfYear()).Should().Equal(53, 1, 1, 53, 52);
        Ints(s.Dt.IsoYear()).Should().Equal(2020, 2025, 2020, 2026, 2024);
    }

    [Fact]
    public void Quarter_ExactValues_AtQuarterBoundaries()
    {
        Ints(Times3().Dt.Quarter()).Should().Equal(2, 1, 3);

        var s = Dates(D(2024, 3, 31), D(2024, 4, 1), D(2024, 9, 30), D(2024, 10, 1), D(2024, 12, 31));
        Ints(s.Dt.Quarter()).Should().Equal(1, 2, 3, 4, 4);
    }

    // ============================================================================
    // Boolean calendar predicates
    // ============================================================================

    [Fact]
    public void IsLeapYear_UsesGregorianRules()
    {
        // 1900 is not a leap year (divisible by 100), 2000 is (divisible by 400).
        var s = Dates(D(2024, 2, 29), D(2023, 6, 1), D(1900, 1, 1), D(2000, 1, 1), D(1969, 7, 20));

        Bools(s.Dt.IsLeapYear()).Should().Equal(true, false, false, true, false);
    }

    [Fact]
    public void IsWeekend_FridayThroughMonday()
    {
        var s = Dates(D(2024, 6, 14), D(2024, 6, 15), D(2024, 6, 16), D(2024, 6, 17));

        Bools(s.Dt.IsWeekend()).Should().Equal(false, true, true, false);
    }

    [Fact]
    public void IsMonthStart_And_IsMonthEnd_HandleFebruary()
    {
        var s = Dates(D(2024, 2, 1), D(2024, 2, 28), D(2024, 2, 29), D(2023, 2, 28), D(2024, 3, 1), D(2024, 4, 30));

        Bools(s.Dt.IsMonthStart()).Should().Equal(true, false, false, false, true, false);
        Bools(s.Dt.IsMonthEnd()).Should().Equal(false, false, true, true, false, true);
    }

    [Fact]
    public void IsQuarterStart_OnlyOnFirstDayOfQuarterMonths()
    {
        var s = Dates(D(2024, 1, 1), D(2024, 4, 1), D(2024, 7, 1), D(2024, 10, 1), D(2024, 2, 1), D(2024, 4, 2));

        Bools(s.Dt.IsQuarterStart()).Should().Equal(true, true, true, true, false, false);
    }

    [Fact]
    public void IsQuarterEnd_OnlyOnLastDayOfQuarterMonths()
    {
        var s = Dates(D(2024, 3, 31), D(2024, 6, 30), D(2024, 9, 30), D(2024, 12, 31), D(2024, 1, 31), D(2024, 6, 29));

        Bools(s.Dt.IsQuarterEnd()).Should().Equal(true, true, true, true, false, false);
    }

    [Fact]
    public void IsYearStart_And_IsYearEnd()
    {
        var s = Dates(D(2024, 1, 1), D(2024, 1, 2), D(2024, 12, 31), D(2024, 12, 30));

        Bools(s.Dt.IsYearStart()).Should().Equal(true, false, false, false);
        Bools(s.Dt.IsYearEnd()).Should().Equal(false, false, true, false);
    }

    [Fact]
    public void Predicates_AreBooleanAndKeepTheSeriesName()
    {
        var result = Dates(D(2024, 6, 15)).Dt.IsWeekend();

        result.Name.Should().Be("d");
        result.DataType.Should().Be(DataType.Boolean);
    }

    // ============================================================================
    // Truncation
    // ============================================================================

    [Fact]
    public void TruncateToYearMonthDay_ExactValues()
    {
        var s = Series.FromValues("ts", new[] { T1 });

        Times(s.Dt.TruncateToYear()).Should().Equal(Dt(2024, 1, 1));
        Times(s.Dt.TruncateToMonth()).Should().Equal(Dt(2024, 6, 1));
        Times(s.Dt.TruncateToDay()).Should().Equal(Dt(2024, 6, 15));
        s.Dt.TruncateToDay().DataType.Should().Be(DataType.DateTime(TimeUnit.Nanoseconds));
    }

    [Fact]
    public void TruncateToHourMinuteSecond_ExactValues()
    {
        var s = Series.FromValues("ts", new[] { T1 });

        Times(s.Dt.TruncateToHour()).Should().Equal(Dt(2024, 6, 15, 12));
        Times(s.Dt.TruncateToMinute()).Should().Equal(Dt(2024, 6, 15, 12, 30));
        Times(s.Dt.TruncateToSecond()).Should().Equal(Dt(2024, 6, 15, 12, 30, 45));
    }

    [Fact]
    public void Truncate_PreEpochValues()
    {
        var s = Series.FromValues("ts", new[] { T3 });

        Times(s.Dt.TruncateToYear()).Should().Equal(Dt(1969, 1, 1));
        Times(s.Dt.TruncateToDay()).Should().Equal(Dt(1969, 7, 20));
        Times(s.Dt.TruncateToHour()).Should().Equal(Dt(1969, 7, 20, 20));
    }

    [Fact]
    public void Truncate_OnDateColumn_ReturnsDateTimeAtMidnight()
    {
        // Polars keeps the Date type here; Polaire's truncation helpers always produce a DateTime column.
        var result = Dates(D(2024, 6, 15), D(1969, 7, 20)).Dt.TruncateToMonth();

        result.DataType.Should().Be(DataType.DateTime(TimeUnit.Nanoseconds));
        Times(result).Should().Equal(Dt(2024, 6, 1), Dt(1969, 7, 1));
    }

    // ============================================================================
    // Rounding to an interval
    // ============================================================================

    [Fact]
    public void RoundFloorCeiling_ToHour()
    {
        var s = Times3();
        var hour = TimeSpan.FromHours(1);

        Times(s.Dt.Round(hour)).Should().Equal(Dt(2024, 6, 15, 13), T2, Dt(1969, 7, 20, 20));
        Times(s.Dt.Floor(hour)).Should().Equal(Dt(2024, 6, 15, 12), T2, Dt(1969, 7, 20, 20));
        Times(s.Dt.Ceiling(hour)).Should().Equal(Dt(2024, 6, 15, 13), T2, Dt(1969, 7, 20, 21));
    }

    [Fact]
    public void RoundFloorCeiling_ToFifteenMinutes()
    {
        var s = Series.FromValues("ts", new[] { T1 });
        var quarter = TimeSpan.FromMinutes(15);

        Times(s.Dt.Round(quarter)).Should().Equal(Dt(2024, 6, 15, 12, 30));
        Times(s.Dt.Floor(quarter)).Should().Equal(Dt(2024, 6, 15, 12, 30));
        Times(s.Dt.Ceiling(quarter)).Should().Equal(Dt(2024, 6, 15, 12, 45));
    }

    [Fact]
    public void RoundFloorCeiling_ToSecond_HalfRoundsUp()
    {
        var s = Series.FromValues("ts", new[] { T1, Dt(2024, 6, 15, 12, 30, 45, 500) });
        var second = TimeSpan.FromSeconds(1);

        Times(s.Dt.Round(second)).Should().Equal(Dt(2024, 6, 15, 12, 30, 45), Dt(2024, 6, 15, 12, 30, 46));
        Times(s.Dt.Floor(second)).Should().Equal(Dt(2024, 6, 15, 12, 30, 45), Dt(2024, 6, 15, 12, 30, 45));
        Times(s.Dt.Ceiling(second)).Should().Equal(Dt(2024, 6, 15, 12, 30, 46), Dt(2024, 6, 15, 12, 30, 46));
    }

    [Fact]
    public void RoundFloorCeiling_ToDay()
    {
        var s = Times3();
        var day = TimeSpan.FromDays(1);

        Times(s.Dt.Round(day)).Should().Equal(Dt(2024, 6, 16), T2, Dt(1969, 7, 21));
        Times(s.Dt.Floor(day)).Should().Equal(Dt(2024, 6, 15), T2, Dt(1969, 7, 20));
        Times(s.Dt.Ceiling(day)).Should().Equal(Dt(2024, 6, 16), T2, Dt(1969, 7, 21));
    }

    [Fact]
    public void Rounding_KeepsDateTimeTypeAndName()
    {
        var result = Times3().Dt.Round(TimeSpan.FromHours(1));

        result.Name.Should().Be("ts");
        result.DataType.Should().Be(DataType.DateTime(TimeUnit.Nanoseconds));
    }

    // ============================================================================
    // Offsets
    // ============================================================================

    [Fact]
    public void AddDays_ExactValues_AcrossTheEpoch()
    {
        var s = Times3();

        Times(s.Dt.AddDays(-20)).Should().Equal(Dt(2024, 5, 26, 12, 30, 45, 123), Dt(2022, 12, 13), Dt(1969, 6, 30, 20, 17, 40));
        Times(s.Dt.AddDays(165)).Should().Equal(Dt(2024, 11, 27, 12, 30, 45, 123), Dt(2023, 6, 16), Dt(1970, 1, 1, 20, 17, 40));
        Times(s.Dt.AddDays(0)).Should().Equal(T1, T2, T3);
    }

    [Fact]
    public void AddMonths_ClampsToEndOfTargetMonth()
    {
        var s = Dates(D(2024, 1, 31), D(2024, 3, 31), D(2023, 1, 31), D(2024, 6, 15));

        Times(s.Dt.AddMonths(1)).Should().Equal(Dt(2024, 2, 29), Dt(2024, 4, 30), Dt(2023, 2, 28), Dt(2024, 7, 15));
        Times(s.Dt.AddMonths(-1)).Should().Equal(Dt(2023, 12, 31), Dt(2024, 2, 29), Dt(2022, 12, 31), Dt(2024, 5, 15));
        Times(Series.FromValues("ts", new[] { T1 }).Dt.AddMonths(8)).Should().Equal(Dt(2025, 2, 15, 12, 30, 45, 123));
    }

    [Fact]
    public void AddYears_ClampsLeapDay()
    {
        var leap = Dates(D(2024, 2, 29));

        Times(leap.Dt.AddYears(1)).Should().Equal(Dt(2025, 2, 28));
        Times(leap.Dt.AddYears(4)).Should().Equal(Dt(2028, 2, 29));
        Times(Series.FromValues("ts", new[] { T3 }).Dt.AddYears(55)).Should().Equal(Dt(2024, 7, 20, 20, 17, 40));
    }

    [Fact]
    public void Add_And_Subtract_TimeSpan()
    {
        var s = Series.FromValues("ts", new[] { T1, T3 });

        Times(s.Dt.Add(TimeSpan.FromMinutes(90))).Should().Equal(Dt(2024, 6, 15, 14, 0, 45, 123), Dt(1969, 7, 20, 21, 47, 40));
        Times(s.Dt.Subtract(TimeSpan.FromDays(1.5))).Should().Equal(Dt(2024, 6, 14, 0, 30, 45, 123), Dt(1969, 7, 19, 8, 17, 40));
        // 20:17:40 + 3h42m20s crosses midnight exactly.
        Times(s.Dt.Add(TimeSpan.FromSeconds(13_340))).Should().Equal(Dt(2024, 6, 15, 16, 13, 5, 123), Dt(1969, 7, 21));
    }

    [Fact]
    public void AddDays_OnDateColumn_ReturnsDateTimeAtMidnight()
    {
        var result = Dates(D(2024, 6, 15), D(1969, 12, 31)).Dt.AddDays(1);

        result.DataType.Should().Be(DataType.DateTime(TimeUnit.Nanoseconds));
        Times(result).Should().Equal(Dt(2024, 6, 16), Dt(1970, 1, 1));
    }

    // ============================================================================
    // Calendar boundaries
    // ============================================================================

    [Fact]
    public void MonthStart_And_MonthEnd()
    {
        var s = Times3();

        Times(s.Dt.MonthStart()).Should().Equal(Dt(2024, 6, 1), Dt(2023, 1, 1), Dt(1969, 7, 1));
        Times(s.Dt.MonthEnd()).Should().Equal(
            Dt(2024, 6, 30, 23, 59, 59, 999), Dt(2023, 1, 31, 23, 59, 59, 999), Dt(1969, 7, 31, 23, 59, 59, 999));
    }

    [Fact]
    public void MonthEnd_InFebruary_DependsOnLeapYear()
    {
        var s = Dates(D(2024, 2, 10), D(2023, 2, 10));

        Times(s.Dt.MonthEnd()).Should().Equal(Dt(2024, 2, 29, 23, 59, 59, 999), Dt(2023, 2, 28, 23, 59, 59, 999));
    }

    [Fact]
    public void QuarterStart_And_QuarterEnd()
    {
        var s = Times3();

        Times(s.Dt.QuarterStart()).Should().Equal(Dt(2024, 4, 1), Dt(2023, 1, 1), Dt(1969, 7, 1));
        Times(s.Dt.QuarterEnd()).Should().Equal(
            Dt(2024, 6, 30, 23, 59, 59, 999), Dt(2023, 3, 31, 23, 59, 59, 999), Dt(1969, 9, 30, 23, 59, 59, 999));

        var q4 = Dates(D(2024, 11, 5));
        Times(q4.Dt.QuarterStart()).Should().Equal(Dt(2024, 10, 1));
        Times(q4.Dt.QuarterEnd()).Should().Equal(Dt(2024, 12, 31, 23, 59, 59, 999));
    }

    [Fact]
    public void YearStart_And_YearEnd()
    {
        var s = Times3();

        Times(s.Dt.YearStart()).Should().Equal(Dt(2024, 1, 1), Dt(2023, 1, 1), Dt(1969, 1, 1));
        Times(s.Dt.YearEnd()).Should().Equal(
            Dt(2024, 12, 31, 23, 59, 59, 999), Dt(2023, 12, 31, 23, 59, 59, 999), Dt(1969, 12, 31, 23, 59, 59, 999));
    }

    // ============================================================================
    // Epoch conversions
    // ============================================================================

    [Fact]
    public void EpochSeconds_And_Milliseconds_ExactValues()
    {
        var s = Times3();

        Longs(s.Dt.EpochSeconds()).Should().Equal(1_718_454_645L, 1_672_617_600L, -14_182_940L);
        Longs(s.Dt.EpochMilliseconds()).Should().Equal(1_718_454_645_123L, 1_672_617_600_000L, -14_182_940_000L);
    }

    [Fact]
    public void EpochMicroseconds_And_Nanoseconds_ExactValues()
    {
        var s = Series.FromValues("ts", new[]
        {
            new DateTime(2024, 6, 15, 12, 30, 45, DateTimeKind.Utc).AddTicks(1234567),
            T3
        });

        Longs(s.Dt.EpochMicroseconds()).Should().Equal(1_718_454_645_123_456L, -14_182_940_000_000L);
        Longs(s.Dt.EpochNanoseconds()).Should().Equal(1_718_454_645_123_456_700L, -14_182_940_000_000_000L);
    }

    [Fact]
    public void Epoch_SubMillisecondFraction_OnEitherSideOfTheEpoch()
    {
        // 500 microseconds after and before 1970-01-01T00:00:00.
        var s = Series.FromValues("ts", new[] { DateTime.UnixEpoch.AddTicks(5000), DateTime.UnixEpoch.AddTicks(-5000) });

        Longs(s.Dt.EpochMicroseconds()).Should().Equal(500L, -500L);
        Longs(s.Dt.EpochNanoseconds()).Should().Equal(500_000L, -500_000L);
    }

    [Fact]
    public void Epoch_OnDateColumn_CountsFromMidnight()
    {
        var s = Dates(D(1970, 1, 2), D(1969, 12, 31), D(2024, 6, 15));

        Longs(s.Dt.EpochSeconds()).Should().Equal(86_400L, -86_400L, 1_718_409_600L);
        Longs(s.Dt.EpochMilliseconds()).Should().Equal(86_400_000L, -86_400_000L, 1_718_409_600_000L);
    }

    [Fact]
    public void Epoch_ResultsAreInt64AndKeepTheSeriesName()
    {
        var result = Times3().Dt.EpochSeconds();

        result.Name.Should().Be("ts");
        result.DataType.Should().Be(DataType.Int64);
    }

    // ============================================================================
    // Formatting
    // ============================================================================

    [Fact]
    public void ToIsoString_ExactValues()
    {
        var s = Times3();

        s.Dt.ToIsoString().ToArray<string>().Should().Equal(
            "2024-06-15T12:30:45.1230000Z", "2023-01-02T00:00:00.0000000Z", "1969-07-20T20:17:40.0000000Z");
    }

    [Fact]
    public void ToIsoString_OnDateColumn_IsMidnightUtc()
    {
        Dates(D(2024, 6, 15)).Dt.ToIsoString().ToArray<string>().Should().Equal("2024-06-15T00:00:00.0000000Z");
    }

    [Fact]
    public void ToUtc_IsIdentityForStoredValues()
    {
        // Stored values are wall-clock times read back as UTC, so ToUtc changes nothing.
        Times(Times3().Dt.ToUtc()).Should().Equal(T1, T2, T3);
    }

    // ============================================================================
    // Null propagation
    // ============================================================================

    [Fact]
    public void Components_PropagateNulls()
    {
        var s = Series.FromNullable("ts", new DateTime?[] { T1, null, T3 });
        var year = s.Dt.Year();

        year.Name.Should().Be("ts");
        year.NullCount.Should().Be(1);
        year[0].AsInt32().Should().Be(2024);
        year[1].IsNull.Should().BeTrue();
        year[2].AsInt32().Should().Be(1969);
    }

    [Fact]
    public void Predicates_PropagateNulls()
    {
        var s = Series.FromNullable("ts", new DateTime?[] { T1, null, T3 });
        var weekend = s.Dt.IsWeekend();

        weekend.NullCount.Should().Be(1);
        weekend[0].AsBoolean().Should().BeTrue();
        weekend[1].IsNull.Should().BeTrue();
        weekend[2].AsBoolean().Should().BeTrue();
    }

    [Fact]
    public void Transforms_PropagateNulls()
    {
        var s = Series.FromNullable("ts", new DateTime?[] { T1, null, T3 });

        var day = s.Dt.TruncateToDay();
        day.DataType.Should().Be(DataType.DateTime(TimeUnit.Nanoseconds));
        day.NullCount.Should().Be(1);
        day[0].AsDateTime().Should().Be(Dt(2024, 6, 15));
        day[1].IsNull.Should().BeTrue();
        day[2].AsDateTime().Should().Be(Dt(1969, 7, 20));

        var shifted = s.Dt.AddDays(1);
        shifted.NullCount.Should().Be(1);
        shifted[1].IsNull.Should().BeTrue();

        var monthEnd = s.Dt.MonthEnd();
        monthEnd.NullCount.Should().Be(1);
        monthEnd[2].AsDateTime().Should().Be(Dt(1969, 7, 31, 23, 59, 59, 999));
    }

    [Fact]
    public void Epoch_PropagatesNulls()
    {
        var s = Series.FromNullable("ts", new DateTime?[] { T1, null, T3 });
        var epoch = s.Dt.EpochSeconds();

        epoch.NullCount.Should().Be(1);
        epoch[0].AsInt64().Should().Be(1_718_454_645L);
        epoch[1].IsNull.Should().BeTrue();
        epoch[2].AsInt64().Should().Be(-14_182_940L);
    }

    [Fact]
    public void Formatting_PropagatesNulls()
    {
        var s = Series.FromNullable("ts", new DateTime?[] { T1, null, T3 });

        var iso = s.Dt.ToIsoString();
        iso.DataType.Should().Be(DataType.String);
        iso.NullCount.Should().Be(1);
        iso[0].AsString().Should().Be("2024-06-15T12:30:45.1230000Z");
        iso[1].IsNull.Should().BeTrue();

        var year = s.Dt.Strftime("%Y");
        year[0].AsString().Should().Be("2024");
        year[1].IsNull.Should().BeTrue();
        year[2].AsString().Should().Be("1969");
    }

    [Fact]
    public void DateColumnWithNulls_PropagatesNulls()
    {
        var s = Series.FromNullable("d", new DateOnly?[] { D(2024, 2, 29), null });

        var leap = s.Dt.IsLeapYear();
        leap[0].AsBoolean().Should().BeTrue();
        leap[1].IsNull.Should().BeTrue();

        var month = s.Dt.Month();
        month[0].AsInt32().Should().Be(2);
        month[1].IsNull.Should().BeTrue();
    }

    [Fact]
    public void EmptySeries_ProduceEmptyResultsOfTheRightType()
    {
        var s = Series.FromValues("ts", Array.Empty<DateTime>());

        s.Dt.Year().Length.Should().Be(0);
        s.Dt.Year().DataType.Should().Be(DataType.Int32);
        s.Dt.IsWeekend().Length.Should().Be(0);
        s.Dt.TruncateToDay().Length.Should().Be(0);
        s.Dt.TruncateToDay().DataType.Should().Be(DataType.DateTime(TimeUnit.Nanoseconds));
        s.Dt.EpochSeconds().Length.Should().Be(0);
        s.Dt.EpochSeconds().DataType.Should().Be(DataType.Int64);
    }
}
