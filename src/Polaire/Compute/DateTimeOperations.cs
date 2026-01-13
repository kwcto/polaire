// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using Apache.Arrow;
using Polaire.DataTypes;
using Polaire.Core;
using Polaire.Series;

namespace Polaire.Compute;

/// <summary>
/// DateTime operations namespace for Series (accessed via .Dt property).
/// </summary>
public sealed class DateTimeOperations
{
    private readonly Series _series;

    public DateTimeOperations(Series series)
    {
        if (series.DataType is not DataType.DateType and not DataType.DateTimeType and not DataType.TimeType)
            throw new ArgumentException("DateTime operations require temporal series");
        _series = series;
    }

    // ============================================================================
    // Date Component Extraction
    // ============================================================================

    public Series Year()
    {
        return ExtractComponent(dt => dt.Year);
    }

    public Series Month()
    {
        return ExtractComponent(dt => dt.Month);
    }

    public Series Day()
    {
        return ExtractComponent(dt => dt.Day);
    }

    public Series Hour()
    {
        return ExtractComponent(dt => dt.Hour);
    }

    public Series Minute()
    {
        return ExtractComponent(dt => dt.Minute);
    }

    public Series Second()
    {
        return ExtractComponent(dt => dt.Second);
    }

    public Series Millisecond()
    {
        return ExtractComponent(dt => dt.Millisecond);
    }

    public Series Microsecond()
    {
        return ExtractComponent(dt => (dt.Ticks / 10) % 1000);
    }

    public Series Nanosecond()
    {
        return ExtractComponent(dt => (dt.Ticks % 10) * 100);
    }

    // ============================================================================
    // Week/Day of Week
    // ============================================================================

    public Series DayOfWeek()
    {
        return ExtractComponent(dt => (int)dt.DayOfWeek);
    }

    public Series DayOfYear()
    {
        return ExtractComponent(dt => dt.DayOfYear);
    }

    public Series WeekOfYear()
    {
        return ExtractComponent(dt =>
            System.Globalization.ISOWeek.GetWeekOfYear(dt));
    }

    public Series IsoYear()
    {
        return ExtractComponent(dt =>
            System.Globalization.ISOWeek.GetYear(dt));
    }

    public Series Quarter()
    {
        return ExtractComponent(dt => (dt.Month - 1) / 3 + 1);
    }

    // ============================================================================
    // Boolean Properties
    // ============================================================================

    public Series IsLeapYear()
    {
        return ExtractBoolean(dt => DateTime.IsLeapYear(dt.Year));
    }

    public Series IsWeekend()
    {
        return ExtractBoolean(dt => dt.DayOfWeek is System.DayOfWeek.Saturday or System.DayOfWeek.Sunday);
    }

    public Series IsMonthStart()
    {
        return ExtractBoolean(dt => dt.Day == 1);
    }

    public Series IsMonthEnd()
    {
        return ExtractBoolean(dt => dt.Day == DateTime.DaysInMonth(dt.Year, dt.Month));
    }

    public Series IsQuarterStart()
    {
        return ExtractBoolean(dt => dt.Day == 1 && dt.Month % 3 == 1);
    }

    public Series IsQuarterEnd()
    {
        return ExtractBoolean(dt =>
        {
            var month = dt.Month;
            return dt.Day == DateTime.DaysInMonth(dt.Year, month) && month % 3 == 0;
        });
    }

    public Series IsYearStart()
    {
        return ExtractBoolean(dt => dt.Month == 1 && dt.Day == 1);
    }

    public Series IsYearEnd()
    {
        return ExtractBoolean(dt => dt.Month == 12 && dt.Day == 31);
    }

    // ============================================================================
    // Truncation
    // ============================================================================

    public Series TruncateToYear()
    {
        return TransformDateTime(dt => new DateTime(dt.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    public Series TruncateToMonth()
    {
        return TransformDateTime(dt => new DateTime(dt.Year, dt.Month, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    public Series TruncateToDay()
    {
        return TransformDateTime(dt => new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0, DateTimeKind.Utc));
    }

    public Series TruncateToHour()
    {
        return TransformDateTime(dt => new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0, DateTimeKind.Utc));
    }

    public Series TruncateToMinute()
    {
        return TransformDateTime(dt => new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0, DateTimeKind.Utc));
    }

    public Series TruncateToSecond()
    {
        return TransformDateTime(dt => new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, DateTimeKind.Utc));
    }

    // ============================================================================
    // Rounding
    // ============================================================================

    public Series Round(TimeSpan interval)
    {
        return TransformDateTime(dt =>
        {
            var ticks = dt.Ticks;
            var intervalTicks = interval.Ticks;
            var rounded = ((ticks + intervalTicks / 2) / intervalTicks) * intervalTicks;
            return new DateTime(rounded, DateTimeKind.Utc);
        });
    }

    public Series Floor(TimeSpan interval)
    {
        return TransformDateTime(dt =>
        {
            var ticks = dt.Ticks;
            var intervalTicks = interval.Ticks;
            var floored = (ticks / intervalTicks) * intervalTicks;
            return new DateTime(floored, DateTimeKind.Utc);
        });
    }

    public Series Ceiling(TimeSpan interval)
    {
        return TransformDateTime(dt =>
        {
            var ticks = dt.Ticks;
            var intervalTicks = interval.Ticks;
            var ceiled = ((ticks + intervalTicks - 1) / intervalTicks) * intervalTicks;
            return new DateTime(ceiled, DateTimeKind.Utc);
        });
    }

    // ============================================================================
    // Offset Operations
    // ============================================================================

    public Series AddDays(int days)
    {
        return TransformDateTime(dt => dt.AddDays(days));
    }

    public Series AddMonths(int months)
    {
        return TransformDateTime(dt => dt.AddMonths(months));
    }

    public Series AddYears(int years)
    {
        return TransformDateTime(dt => dt.AddYears(years));
    }

    public Series Add(TimeSpan timeSpan)
    {
        return TransformDateTime(dt => dt.Add(timeSpan));
    }

    public Series Subtract(TimeSpan timeSpan)
    {
        return TransformDateTime(dt => dt.Subtract(timeSpan));
    }

    // ============================================================================
    // Timezone Operations
    // ============================================================================

    public Series ConvertTimeZone(string fromTz, string toTz)
    {
        var sourceZone = TimeZoneInfo.FindSystemTimeZoneById(fromTz);
        var destZone = TimeZoneInfo.FindSystemTimeZoneById(toTz);

        return TransformDateTime(dt =>
        {
            var utc = TimeZoneInfo.ConvertTimeToUtc(dt, sourceZone);
            return TimeZoneInfo.ConvertTimeFromUtc(utc, destZone);
        });
    }

    public Series ToUtc()
    {
        return TransformDateTime(dt => dt.ToUniversalTime());
    }

    // ============================================================================
    // Formatting
    // ============================================================================

    public Series Strftime(string format)
    {
        return TransformToString(dt => dt.ToString(format));
    }

    public Series ToIsoString()
    {
        return TransformToString(dt => dt.ToString("o"));
    }

    // ============================================================================
    // Date Calculations
    // ============================================================================

    public Series MonthStart()
    {
        return TransformDateTime(dt => new DateTime(dt.Year, dt.Month, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    public Series MonthEnd()
    {
        return TransformDateTime(dt =>
            new DateTime(dt.Year, dt.Month, DateTime.DaysInMonth(dt.Year, dt.Month), 23, 59, 59, 999, DateTimeKind.Utc));
    }

    public Series QuarterStart()
    {
        return TransformDateTime(dt =>
        {
            var quarterMonth = ((dt.Month - 1) / 3) * 3 + 1;
            return new DateTime(dt.Year, quarterMonth, 1, 0, 0, 0, DateTimeKind.Utc);
        });
    }

    public Series QuarterEnd()
    {
        return TransformDateTime(dt =>
        {
            var quarterMonth = ((dt.Month - 1) / 3) * 3 + 3;
            return new DateTime(dt.Year, quarterMonth, DateTime.DaysInMonth(dt.Year, quarterMonth), 23, 59, 59, 999, DateTimeKind.Utc);
        });
    }

    public Series YearStart()
    {
        return TransformDateTime(dt => new DateTime(dt.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    public Series YearEnd()
    {
        return TransformDateTime(dt => new DateTime(dt.Year, 12, 31, 23, 59, 59, 999, DateTimeKind.Utc));
    }

    // ============================================================================
    // Epoch Conversions
    // ============================================================================

    public Series EpochSeconds()
    {
        return ExtractLong(dt => new DateTimeOffset(dt, TimeSpan.Zero).ToUnixTimeSeconds());
    }

    public Series EpochMilliseconds()
    {
        return ExtractLong(dt => new DateTimeOffset(dt, TimeSpan.Zero).ToUnixTimeMilliseconds());
    }

    public Series EpochMicroseconds()
    {
        return ExtractLong(dt => new DateTimeOffset(dt, TimeSpan.Zero).ToUnixTimeMilliseconds() * 1000 +
            (dt.Ticks % TimeSpan.TicksPerMillisecond) / 10);
    }

    public Series EpochNanoseconds()
    {
        return ExtractLong(dt => new DateTimeOffset(dt, TimeSpan.Zero).ToUnixTimeMilliseconds() * 1_000_000 +
            (dt.Ticks % TimeSpan.TicksPerMillisecond) * 100);
    }

    // ============================================================================
    // Helpers
    // ============================================================================

    private DateTime GetDateTime(int index)
    {
        if (_series.DataType is DataType.DateType)
        {
            var data = _series.Data as ChunkedArray<int>;
            return DateOnly.FromDayNumber(data!.GetValue(index)).ToDateTime(TimeOnly.MinValue);
        }
        else if (_series.DataType is DataType.DateTimeType)
        {
            var data = _series.Data as ChunkedArray<long>;
            return new DateTime(data!.GetValue(index), DateTimeKind.Utc);
        }
        else
        {
            var data = _series.Data as ChunkedArray<long>;
            return DateTime.MinValue.Add(TimeSpan.FromTicks(data!.GetValue(index) / 100));
        }
    }

    private Series ExtractComponent(Func<DateTime, int> extractor)
    {
        var builder = new Int32Array.Builder();

        for (int i = 0; i < _series.Length; i++)
        {
            if (_series.IsNull(i))
            {
                builder.AppendNull();
            }
            else
            {
                builder.Append(extractor(GetDateTime(i)));
            }
        }

        return Series.FromArrowArray(_series.Name, builder.Build(), DataType.Int32);
    }

    private Series ExtractLong(Func<DateTime, long> extractor)
    {
        var builder = new Int64Array.Builder();

        for (int i = 0; i < _series.Length; i++)
        {
            if (_series.IsNull(i))
            {
                builder.AppendNull();
            }
            else
            {
                builder.Append(extractor(GetDateTime(i)));
            }
        }

        return Series.FromArrowArray(_series.Name, builder.Build(), DataType.Int64);
    }

    private Series ExtractBoolean(Func<DateTime, bool> predicate)
    {
        var builder = new BooleanArray.Builder();

        for (int i = 0; i < _series.Length; i++)
        {
            if (_series.IsNull(i))
            {
                builder.AppendNull();
            }
            else
            {
                builder.Append(predicate(GetDateTime(i)));
            }
        }

        return Series.FromArrowArray(_series.Name, builder.Build(), DataType.Boolean);
    }

    private Series TransformDateTime(Func<DateTime, DateTime> transform)
    {
        var builder = new TimestampArray.Builder(Apache.Arrow.Types.TimeUnit.Nanosecond);

        for (int i = 0; i < _series.Length; i++)
        {
            if (_series.IsNull(i))
            {
                builder.AppendNull();
            }
            else
            {
                var transformed = transform(GetDateTime(i));
                builder.Append(new DateTimeOffset(transformed, TimeSpan.Zero));
            }
        }

        return Series.FromArrowArray(_series.Name, builder.Build(), DataType.DateTime(TimeUnit.Nanoseconds));
    }

    private Series TransformToString(Func<DateTime, string> transform)
    {
        var builder = new StringArray.Builder();

        for (int i = 0; i < _series.Length; i++)
        {
            if (_series.IsNull(i))
            {
                builder.AppendNull();
            }
            else
            {
                builder.Append(transform(GetDateTime(i)));
            }
        }

        return Series.FromArrowArray(_series.Name, builder.Build(), DataType.String);
    }
}
