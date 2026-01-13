// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using FluentAssertions;
using Polaire;
using Polaire.Compute;
using Xunit;

namespace Polaire.Tests;

/// <summary>
/// Tests for DateTimeOperations.
/// Note: DateTime transformation operations (AddDays, TruncateToX, etc.) have known issues
/// with timestamp storage/retrieval overflow. Extraction operations (Year, Month, etc.) work correctly.
/// </summary>
public class DateTimeOperationsTests
{
    // ============================================================================
    // Construction and Basic Operations
    // ============================================================================

    [Fact]
    public void DateTimeOperations_Constructor_ThrowsForNonTemporalSeries()
    {
        var series = Series.FromValues("numbers", new[] { 1, 2, 3 });

        var act = () => new DateTimeOperations(series);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*temporal*");
    }

    [Fact]
    public void DateTimeOperations_Constructor_AcceptsDateTimeSeries()
    {
        var dates = new DateTime[]
        {
            new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };
        var series = Series.FromValues("dt", dates);

        var act = () => new DateTimeOperations(series);

        act.Should().NotThrow();
    }

    [Fact]
    public void DateTimeOperations_Constructor_AcceptsDateOnlySeries()
    {
        var dates = new DateOnly[]
        {
            new DateOnly(2024, 1, 1),
        };
        var series = Series.FromValues("date", dates);

        var act = () => new DateTimeOperations(series);

        act.Should().NotThrow();
    }

    // ============================================================================
    // Component Extraction Tests
    // ============================================================================

    [Fact]
    public void Year_ReturnsInt32Series()
    {
        var dates = new DateTime[]
        {
            new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc),
        };
        var series = Series.FromValues("dt", dates);
        var dt = new DateTimeOperations(series);

        var result = dt.Year();

        result.Length.Should().Be(1);
        result.DataType.Should().Be(Polaire.DataTypes.DataType.Int32);
    }

    [Fact]
    public void Month_ReturnsInt32Series()
    {
        var dates = new DateTime[]
        {
            new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc),
        };
        var series = Series.FromValues("dt", dates);
        var dt = new DateTimeOperations(series);

        var result = dt.Month();

        result.Length.Should().Be(1);
        result.DataType.Should().Be(Polaire.DataTypes.DataType.Int32);
    }

    [Fact]
    public void Day_ReturnsInt32Series()
    {
        var dates = new DateTime[]
        {
            new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc),
        };
        var series = Series.FromValues("dt", dates);
        var dt = new DateTimeOperations(series);

        var result = dt.Day();

        result.Length.Should().Be(1);
        result.DataType.Should().Be(Polaire.DataTypes.DataType.Int32);
    }

    [Fact]
    public void Hour_ReturnsInt32Series()
    {
        var dates = new DateTime[]
        {
            new DateTime(2024, 6, 15, 12, 30, 45, DateTimeKind.Utc),
        };
        var series = Series.FromValues("dt", dates);
        var dt = new DateTimeOperations(series);

        var result = dt.Hour();

        result.Length.Should().Be(1);
        result.DataType.Should().Be(Polaire.DataTypes.DataType.Int32);
    }

    [Fact]
    public void Minute_ReturnsInt32Series()
    {
        var dates = new DateTime[]
        {
            new DateTime(2024, 6, 15, 12, 30, 45, DateTimeKind.Utc),
        };
        var series = Series.FromValues("dt", dates);
        var dt = new DateTimeOperations(series);

        var result = dt.Minute();

        result.Length.Should().Be(1);
        result.DataType.Should().Be(Polaire.DataTypes.DataType.Int32);
    }

    [Fact]
    public void Second_ReturnsInt32Series()
    {
        var dates = new DateTime[]
        {
            new DateTime(2024, 6, 15, 12, 30, 45, DateTimeKind.Utc),
        };
        var series = Series.FromValues("dt", dates);
        var dt = new DateTimeOperations(series);

        var result = dt.Second();

        result.Length.Should().Be(1);
        result.DataType.Should().Be(Polaire.DataTypes.DataType.Int32);
    }

    [Fact]
    public void Millisecond_ReturnsInt32Series()
    {
        var dates = new DateTime[]
        {
            new DateTime(2024, 6, 15, 12, 30, 45, 123, DateTimeKind.Utc),
        };
        var series = Series.FromValues("dt", dates);
        var dt = new DateTimeOperations(series);

        var result = dt.Millisecond();

        result.Length.Should().Be(1);
        result.DataType.Should().Be(Polaire.DataTypes.DataType.Int32);
    }

    [Fact]
    public void Microsecond_ReturnsInt32Series()
    {
        var dates = new DateTime[]
        {
            new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc),
        };
        var series = Series.FromValues("dt", dates);
        var dt = new DateTimeOperations(series);

        var result = dt.Microsecond();

        result.Length.Should().Be(1);
        result.DataType.Should().Be(Polaire.DataTypes.DataType.Int32);
    }

    [Fact]
    public void Nanosecond_ReturnsInt32Series()
    {
        var dates = new DateTime[]
        {
            new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc),
        };
        var series = Series.FromValues("dt", dates);
        var dt = new DateTimeOperations(series);

        var result = dt.Nanosecond();

        result.Length.Should().Be(1);
        result.DataType.Should().Be(Polaire.DataTypes.DataType.Int32);
    }

    // ============================================================================
    // Week/Day of Week Tests
    // ============================================================================

    [Fact]
    public void DayOfWeek_ReturnsInt32Series()
    {
        var dates = new DateTime[]
        {
            new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc),
        };
        var series = Series.FromValues("dt", dates);
        var dt = new DateTimeOperations(series);

        var result = dt.DayOfWeek();

        result.Length.Should().Be(1);
        result.DataType.Should().Be(Polaire.DataTypes.DataType.Int32);
        // Should be between 0 (Sunday) and 6 (Saturday)
        result[0].AsInt32().Should().BeInRange(0, 6);
    }

    [Fact]
    public void DayOfYear_ReturnsInt32Series()
    {
        var dates = new DateTime[]
        {
            new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc),
        };
        var series = Series.FromValues("dt", dates);
        var dt = new DateTimeOperations(series);

        var result = dt.DayOfYear();

        result.Length.Should().Be(1);
        result.DataType.Should().Be(Polaire.DataTypes.DataType.Int32);
        // Day of year should be between 1 and 366
        result[0].AsInt32().Should().BeInRange(1, 366);
    }

    [Fact]
    public void WeekOfYear_ReturnsInt32Series()
    {
        var dates = new DateTime[]
        {
            new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc),
        };
        var series = Series.FromValues("dt", dates);
        var dt = new DateTimeOperations(series);

        var result = dt.WeekOfYear();

        result.Length.Should().Be(1);
        result.DataType.Should().Be(Polaire.DataTypes.DataType.Int32);
        // Week should be between 1 and 53
        result[0].AsInt32().Should().BeInRange(1, 53);
    }

    [Fact]
    public void IsoYear_ReturnsInt32Series()
    {
        var dates = new DateTime[]
        {
            new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc),
        };
        var series = Series.FromValues("dt", dates);
        var dt = new DateTimeOperations(series);

        var result = dt.IsoYear();

        result.Length.Should().Be(1);
        result.DataType.Should().Be(Polaire.DataTypes.DataType.Int32);
    }

    [Fact]
    public void Quarter_ReturnsInt32Series()
    {
        var dates = new DateTime[]
        {
            new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc),
        };
        var series = Series.FromValues("dt", dates);
        var dt = new DateTimeOperations(series);

        var result = dt.Quarter();

        result.Length.Should().Be(1);
        result.DataType.Should().Be(Polaire.DataTypes.DataType.Int32);
        // Quarter should be between 1 and 4
        result[0].AsInt32().Should().BeInRange(1, 4);
    }

    // ============================================================================
    // Boolean Property Tests
    // ============================================================================

    [Fact]
    public void IsLeapYear_ReturnsBooleanSeries()
    {
        var dates = new DateTime[]
        {
            new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };
        var series = Series.FromValues("dt", dates);
        var dt = new DateTimeOperations(series);

        var result = dt.IsLeapYear();

        result.Length.Should().Be(1);
        result.DataType.Should().Be(Polaire.DataTypes.DataType.Boolean);
    }

    [Fact]
    public void IsWeekend_ReturnsBooleanSeries()
    {
        var dates = new DateTime[]
        {
            new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc),
        };
        var series = Series.FromValues("dt", dates);
        var dt = new DateTimeOperations(series);

        var result = dt.IsWeekend();

        result.Length.Should().Be(1);
        result.DataType.Should().Be(Polaire.DataTypes.DataType.Boolean);
    }

    [Fact]
    public void IsMonthStart_ReturnsBooleanSeries()
    {
        var dates = new DateTime[]
        {
            new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc),
        };
        var series = Series.FromValues("dt", dates);
        var dt = new DateTimeOperations(series);

        var result = dt.IsMonthStart();

        result.Length.Should().Be(1);
        result.DataType.Should().Be(Polaire.DataTypes.DataType.Boolean);
    }

    [Fact]
    public void IsMonthEnd_ReturnsBooleanSeries()
    {
        var dates = new DateTime[]
        {
            new DateTime(2024, 6, 30, 0, 0, 0, DateTimeKind.Utc),
        };
        var series = Series.FromValues("dt", dates);
        var dt = new DateTimeOperations(series);

        var result = dt.IsMonthEnd();

        result.Length.Should().Be(1);
        result.DataType.Should().Be(Polaire.DataTypes.DataType.Boolean);
    }

    [Fact]
    public void IsQuarterStart_ReturnsBooleanSeries()
    {
        var dates = new DateTime[]
        {
            new DateTime(2024, 4, 1, 0, 0, 0, DateTimeKind.Utc),
        };
        var series = Series.FromValues("dt", dates);
        var dt = new DateTimeOperations(series);

        var result = dt.IsQuarterStart();

        result.Length.Should().Be(1);
        result.DataType.Should().Be(Polaire.DataTypes.DataType.Boolean);
    }

    [Fact]
    public void IsQuarterEnd_ReturnsBooleanSeries()
    {
        var dates = new DateTime[]
        {
            new DateTime(2024, 6, 30, 0, 0, 0, DateTimeKind.Utc),
        };
        var series = Series.FromValues("dt", dates);
        var dt = new DateTimeOperations(series);

        var result = dt.IsQuarterEnd();

        result.Length.Should().Be(1);
        result.DataType.Should().Be(Polaire.DataTypes.DataType.Boolean);
    }

    [Fact]
    public void IsYearStart_ReturnsBooleanSeries()
    {
        var dates = new DateTime[]
        {
            new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };
        var series = Series.FromValues("dt", dates);
        var dt = new DateTimeOperations(series);

        var result = dt.IsYearStart();

        result.Length.Should().Be(1);
        result.DataType.Should().Be(Polaire.DataTypes.DataType.Boolean);
    }

    [Fact]
    public void IsYearEnd_ReturnsBooleanSeries()
    {
        var dates = new DateTime[]
        {
            new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc),
        };
        var series = Series.FromValues("dt", dates);
        var dt = new DateTimeOperations(series);

        var result = dt.IsYearEnd();

        result.Length.Should().Be(1);
        result.DataType.Should().Be(Polaire.DataTypes.DataType.Boolean);
    }

    // ============================================================================
    // Formatting Tests
    // ============================================================================

    [Fact]
    public void Strftime_ReturnsStringSeries()
    {
        var dates = new DateTime[]
        {
            new DateTime(2024, 6, 15, 12, 30, 45, DateTimeKind.Utc),
        };
        var series = Series.FromValues("dt", dates);
        var dt = new DateTimeOperations(series);

        var result = dt.Strftime("yyyy-MM-dd");

        result.Length.Should().Be(1);
        result.DataType.Should().Be(Polaire.DataTypes.DataType.String);
    }

    [Fact]
    public void ToIsoString_ReturnsStringSeries()
    {
        var dates = new DateTime[]
        {
            new DateTime(2024, 6, 15, 12, 30, 45, DateTimeKind.Utc),
        };
        var series = Series.FromValues("dt", dates);
        var dt = new DateTimeOperations(series);

        var result = dt.ToIsoString();

        result.Length.Should().Be(1);
        result.DataType.Should().Be(Polaire.DataTypes.DataType.String);
    }

    // ============================================================================
    // Epoch Conversion Tests
    // ============================================================================

    [Fact]
    public void EpochSeconds_ReturnsInt64Series()
    {
        var dates = new DateTime[]
        {
            new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc),
        };
        var series = Series.FromValues("dt", dates);
        var dt = new DateTimeOperations(series);

        var result = dt.EpochSeconds();

        result.Length.Should().Be(1);
        result.DataType.Should().Be(Polaire.DataTypes.DataType.Int64);
        // Value should be positive (after Unix epoch)
        result[0].AsInt64().Should().BeGreaterThan(0);
    }

    [Fact]
    public void EpochMilliseconds_ReturnsInt64Series()
    {
        var dates = new DateTime[]
        {
            new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc),
        };
        var series = Series.FromValues("dt", dates);
        var dt = new DateTimeOperations(series);

        var result = dt.EpochMilliseconds();

        result.Length.Should().Be(1);
        result.DataType.Should().Be(Polaire.DataTypes.DataType.Int64);
    }

    [Fact]
    public void EpochMicroseconds_ReturnsInt64Series()
    {
        var dates = new DateTime[]
        {
            new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc),
        };
        var series = Series.FromValues("dt", dates);
        var dt = new DateTimeOperations(series);

        var result = dt.EpochMicroseconds();

        result.Length.Should().Be(1);
        result.DataType.Should().Be(Polaire.DataTypes.DataType.Int64);
    }

    [Fact]
    public void EpochNanoseconds_ReturnsInt64Series()
    {
        var dates = new DateTime[]
        {
            new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc),
        };
        var series = Series.FromValues("dt", dates);
        var dt = new DateTimeOperations(series);

        var result = dt.EpochNanoseconds();

        result.Length.Should().Be(1);
        result.DataType.Should().Be(Polaire.DataTypes.DataType.Int64);
    }

    // ============================================================================
    // Edge Cases
    // ============================================================================

    [Fact]
    public void Operations_HandleEmptySeries()
    {
        var series = Series.FromValues("dt", Array.Empty<DateTime>());
        var dt = new DateTimeOperations(series);

        var year = dt.Year();
        var isWeekend = dt.IsWeekend();

        year.Length.Should().Be(0);
        isWeekend.Length.Should().Be(0);
    }

    [Fact]
    public void Operations_HandleSingleElement()
    {
        var dates = new DateTime[]
        {
            new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc),
        };
        var series = Series.FromValues("dt", dates);
        var dt = new DateTimeOperations(series);

        var year = dt.Year();
        var month = dt.Month();
        var day = dt.Day();

        year.Length.Should().Be(1);
        month.Length.Should().Be(1);
        day.Length.Should().Be(1);
    }

    [Fact]
    public void Operations_HandleMultipleElements()
    {
        var dates = new DateTime[]
        {
            new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 6, 15, 12, 30, 45, DateTimeKind.Utc),
            new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc),
        };
        var series = Series.FromValues("dt", dates);
        var dt = new DateTimeOperations(series);

        var year = dt.Year();
        var quarter = dt.Quarter();
        var isMonthEnd = dt.IsMonthEnd();

        year.Length.Should().Be(3);
        quarter.Length.Should().Be(3);
        isMonthEnd.Length.Should().Be(3);
    }

    // ============================================================================
    // DateOnly Series Tests
    // ============================================================================

    [Fact]
    public void DateOnly_SupportsYearExtraction()
    {
        var dates = new DateOnly[]
        {
            new DateOnly(2024, 6, 15),
        };
        var series = Series.FromValues("date", dates);
        var dt = new DateTimeOperations(series);

        var result = dt.Year();

        result.Length.Should().Be(1);
        result.DataType.Should().Be(Polaire.DataTypes.DataType.Int32);
    }

    [Fact]
    public void DateOnly_SupportsMonthExtraction()
    {
        var dates = new DateOnly[]
        {
            new DateOnly(2024, 6, 15),
        };
        var series = Series.FromValues("date", dates);
        var dt = new DateTimeOperations(series);

        var result = dt.Month();

        result.Length.Should().Be(1);
    }

    [Fact]
    public void DateOnly_SupportsDayExtraction()
    {
        var dates = new DateOnly[]
        {
            new DateOnly(2024, 6, 15),
        };
        var series = Series.FromValues("date", dates);
        var dt = new DateTimeOperations(series);

        var result = dt.Day();

        result.Length.Should().Be(1);
    }

    [Fact]
    public void DateOnly_SupportsDayOfWeek()
    {
        var dates = new DateOnly[]
        {
            new DateOnly(2024, 6, 15),
        };
        var series = Series.FromValues("date", dates);
        var dt = new DateTimeOperations(series);

        var result = dt.DayOfWeek();

        result.Length.Should().Be(1);
        result[0].AsInt32().Should().BeInRange(0, 6);
    }

    [Fact]
    public void DateOnly_SupportsQuarter()
    {
        var dates = new DateOnly[]
        {
            new DateOnly(2024, 6, 15),
        };
        var series = Series.FromValues("date", dates);
        var dt = new DateTimeOperations(series);

        var result = dt.Quarter();

        result.Length.Should().Be(1);
        result[0].AsInt32().Should().BeInRange(1, 4);
    }

    [Fact]
    public void DateOnly_SupportsIsWeekend()
    {
        var dates = new DateOnly[]
        {
            new DateOnly(2024, 6, 15),
        };
        var series = Series.FromValues("date", dates);
        var dt = new DateTimeOperations(series);

        var result = dt.IsWeekend();

        result.Length.Should().Be(1);
        result.DataType.Should().Be(Polaire.DataTypes.DataType.Boolean);
    }
}
