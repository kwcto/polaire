// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using Apache.Arrow;
using Apache.Arrow.Types;
using Polaire.DataTypes;

namespace Polaire.Core;

/// <summary>
/// Conversions between the physical Arrow representation of temporal columns and .NET values.
/// Date columns store days since the Unix epoch (Arrow Date32).
/// DateTime columns store elapsed units since the Unix epoch (Arrow Timestamp), in the column's TimeUnit.
/// .NET values are treated as wall-clock time: DateTime.Kind is ignored on the way in and UTC on the way out.
/// </summary>
internal static class TemporalConversions
{
    /// <summary>DateOnly.DayNumber of 1970-01-01.</summary>
    public const int UnixEpochDayNumber = 719_162;

    public static DateOnly DateFromDays(int daysSinceEpoch) =>
        DateOnly.FromDayNumber(daysSinceEpoch + UnixEpochDayNumber);

    public static int DaysFromDate(DateOnly date) => date.DayNumber - UnixEpochDayNumber;

    /// <summary>Converts a raw timestamp value to a UTC DateTime (sub-100ns precision is truncated).</summary>
    public static DateTime DateTimeFromRaw(long raw, DataTypes.TimeUnit unit)
    {
        long ticks = unit switch
        {
            DataTypes.TimeUnit.Nanoseconds => FloorDiv(raw, 100),
            DataTypes.TimeUnit.Microseconds => checked(raw * 10),
            DataTypes.TimeUnit.Milliseconds => checked(raw * TimeSpan.TicksPerMillisecond),
            DataTypes.TimeUnit.Seconds => checked(raw * TimeSpan.TicksPerSecond),
            _ => throw new NotSupportedException($"Unsupported time unit: {unit}")
        };
        return DateTime.UnixEpoch.AddTicks(ticks);
    }

    /// <summary>Converts a DateTime to a raw timestamp value, treating it as wall-clock time.</summary>
    public static long RawFromDateTime(DateTime value, DataTypes.TimeUnit unit)
    {
        long ticks = value.Ticks - DateTime.UnixEpoch.Ticks;
        return unit switch
        {
            DataTypes.TimeUnit.Nanoseconds => checked(ticks * 100),
            DataTypes.TimeUnit.Microseconds => FloorDiv(ticks, 10),
            DataTypes.TimeUnit.Milliseconds => FloorDiv(ticks, TimeSpan.TicksPerMillisecond),
            DataTypes.TimeUnit.Seconds => FloorDiv(ticks, TimeSpan.TicksPerSecond),
            _ => throw new NotSupportedException($"Unsupported time unit: {unit}")
        };
    }

    /// <summary>Gets the TimeUnit of a DateTime data type.</summary>
    public static DataTypes.TimeUnit UnitOf(DataType dataType) =>
        dataType is DataType.DateTimeType dt
            ? dt.Unit
            : throw new ArgumentException($"Expected a DateTime type, got {dataType}");

    // ============================================================================
    // Array construction
    // ============================================================================

    /// <summary>Builds a Date column from nullable dates.</summary>
    public static Series DateSeries(string name, IReadOnlyList<DateOnly?> values)
    {
        var builder = new Int32Array.Builder();
        builder.Reserve(values.Count);
        foreach (var v in values)
        {
            if (v.HasValue) builder.Append(DaysFromDate(v.Value));
            else builder.AppendNull();
        }
        return Series.FromArrowArray(name, AsDate32(builder.Build()), DataType.Date);
    }

    /// <summary>Builds a DateTime column with the given unit from nullable wall-clock DateTimes.</summary>
    public static Series DateTimeSeries(string name, IReadOnlyList<DateTime?> values,
        DataTypes.TimeUnit unit = DataTypes.TimeUnit.Nanoseconds, string? timeZone = null)
    {
        var builder = new Int64Array.Builder();
        builder.Reserve(values.Count);
        foreach (var v in values)
        {
            if (v.HasValue) builder.Append(RawFromDateTime(v.Value, unit));
            else builder.AppendNull();
        }
        var dataType = DataType.DateTime(unit, timeZone);
        return Series.FromArrowArray(name, AsTimestamp(builder.Build(), dataType), dataType);
    }

    /// <summary>Reinterprets an Int32 array of epoch days as a Date32 array (no copy).</summary>
    public static Date32Array AsDate32(Int32Array days) =>
        new(new ArrayData(Date32Type.Default, days.Length, days.NullCount, days.Offset, days.Data.Buffers));

    /// <summary>Reinterprets an Int64 array of raw values as a Timestamp array (no copy).</summary>
    public static TimestampArray AsTimestamp(Int64Array raw, DataType.DateTimeType dataType) =>
        new(new ArrayData(ToArrowTimestampType(dataType), raw.Length, raw.NullCount, raw.Offset, raw.Data.Buffers));

    private static TimestampType ToArrowTimestampType(DataType.DateTimeType dataType)
    {
        var unit = dataType.Unit switch
        {
            DataTypes.TimeUnit.Nanoseconds => Apache.Arrow.Types.TimeUnit.Nanosecond,
            DataTypes.TimeUnit.Microseconds => Apache.Arrow.Types.TimeUnit.Microsecond,
            DataTypes.TimeUnit.Milliseconds => Apache.Arrow.Types.TimeUnit.Millisecond,
            DataTypes.TimeUnit.Seconds => Apache.Arrow.Types.TimeUnit.Second,
            _ => throw new NotSupportedException($"Unsupported time unit: {dataType.Unit}")
        };
        return new TimestampType(unit, dataType.TimeZone);
    }

    private static long FloorDiv(long a, long b)
    {
        long q = a / b;
        return (a % b != 0 && (a < 0) != (b < 0)) ? q - 1 : q;
    }
}
