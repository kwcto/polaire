// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using System.Globalization;

namespace Polaire.Compute;

/// <summary>
/// A Polars duration string such as "1y2mo3w4d5h6m7s8ms9us10ns", negated by a leading '-'.
/// Months (including quarters and years) are calendar months; days and smaller units are fixed lengths.
/// </summary>
internal readonly record struct PolarsDuration(int Months, long Days, long Nanoseconds, bool Negative)
{
    private const string ValidUnits = "ns, us, ms, s, m, h, d, w, mo, q, y";

    public static PolarsDuration Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var s = text.AsSpan();
        var negative = s.StartsWith("-");
        if (negative) s = s[1..];
        if (s.IsEmpty)
            throw new FormatException($"Invalid duration '{text}': expected values such as \"1d\", \"2h30m\" or \"-1mo\"");

        long months = 0, days = 0, nanos = 0;
        int i = 0;
        while (i < s.Length)
        {
            int numberStart = i;
            while (i < s.Length && char.IsAsciiDigit(s[i])) i++;
            if (i == numberStart)
                throw new FormatException($"Invalid duration '{text}': expected an integer before '{s[i..].ToString()}'");
            var n = long.Parse(s[numberStart..i], NumberStyles.None, CultureInfo.InvariantCulture);

            int unitStart = i;
            while (i < s.Length && !char.IsAsciiDigit(s[i])) i++;
            var unit = s[unitStart..i].ToString();

            checked
            {
                switch (unit)
                {
                    case "ns": nanos += n; break;
                    case "us" or "µs": nanos += n * 1_000; break;
                    case "ms": nanos += n * 1_000_000; break;
                    case "s": nanos += n * 1_000_000_000; break;
                    case "m": nanos += n * 60_000_000_000; break;
                    case "h": nanos += n * 3_600_000_000_000; break;
                    case "d": days += n; break;
                    case "w": days += n * 7; break;
                    case "mo": months += n; break;
                    case "q": months += n * 3; break;
                    case "y": months += n * 12; break;
                    case "":
                        throw new FormatException($"Invalid duration '{text}': missing unit after {n}. Valid units: {ValidUnits}");
                    case "i":
                        throw new FormatException($"Invalid duration '{text}': index counts ('i') are not valid for temporal offsets");
                    default:
                        throw new FormatException($"Invalid duration '{text}': unknown unit '{unit}'. Valid units: {ValidUnits}");
                }
            }
        }

        return new PolarsDuration(checked((int)months), days, nanos, negative);
    }

    /// <summary>Adds this duration: months first (day clamped to the month's end), then days, then the fixed part.</summary>
    public DateTime AddTo(DateTime value)
    {
        int sign = Negative ? -1 : 1;
        return value
            .AddMonths(sign * Months)
            .AddDays(sign * Days)
            .AddTicks(sign * (Nanoseconds / 100));
    }
}
