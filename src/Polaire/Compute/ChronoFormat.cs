// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using System.Globalization;
using System.Text;

namespace Polaire.Compute;

/// <summary>
/// strftime formatting and strptime parsing with the chrono specifiers that Polars uses
/// (e.g. "%Y-%m-%d %H:%M:%S%.f"). Values are wall-clock times; parsed UTC offsets are
/// converted to UTC.
/// </summary>
/// <remarks>
/// Supported: %Y %C %y %m %b %h %B %d %e %a %A %w %u %j %H %k %I %l %p %P %M %S
/// %f %.f %.3f %.6f %.9f %3f %6f %9f %s %z %:z %F %T %X %R %D %x %r %c %v %% %n %t,
/// with the padding flags '-', '_' and '0'. Format only: %V %G %Z.
/// Differences from chrono: %f parses its digits as a fraction of a second ("5" is 500ms);
/// %S rejects leap seconds.
/// </remarks>
internal static class ChronoFormat
{
    private static readonly DateTimeFormatInfo Names = CultureInfo.InvariantCulture.DateTimeFormat;

    // ============================================================================
    // Formatting
    // ============================================================================

    public static string Format(DateTime value, string format)
    {
        var sb = new StringBuilder(format.Length + 16);
        AppendFormatted(sb, value, format);
        return sb.ToString();
    }

    private static void AppendFormatted(StringBuilder sb, DateTime v, string format)
    {
        for (int i = 0; i < format.Length; i++)
        {
            if (format[i] != '%')
            {
                sb.Append(format[i]);
                continue;
            }

            var spec = ReadSpecifier(format, ref i);
            switch (spec.Name)
            {
                case "Y": AppendNumber(sb, v.Year, 4, spec.Pad, '0'); break;
                case "C": AppendNumber(sb, v.Year / 100, 2, spec.Pad, '0'); break;
                case "y": AppendNumber(sb, v.Year % 100, 2, spec.Pad, '0'); break;
                case "m": AppendNumber(sb, v.Month, 2, spec.Pad, '0'); break;
                case "b" or "h": sb.Append(Names.AbbreviatedMonthNames[v.Month - 1]); break;
                case "B": sb.Append(Names.MonthNames[v.Month - 1]); break;
                case "d": AppendNumber(sb, v.Day, 2, spec.Pad, '0'); break;
                case "e": AppendNumber(sb, v.Day, 2, spec.Pad, ' '); break;
                case "a": sb.Append(Names.AbbreviatedDayNames[(int)v.DayOfWeek]); break;
                case "A": sb.Append(Names.DayNames[(int)v.DayOfWeek]); break;
                case "w": sb.Append((int)v.DayOfWeek); break;
                case "u": sb.Append(v.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)v.DayOfWeek); break;
                case "j": AppendNumber(sb, v.DayOfYear, 3, spec.Pad, '0'); break;
                case "V": AppendNumber(sb, ISOWeek.GetWeekOfYear(v), 2, spec.Pad, '0'); break;
                case "G": AppendNumber(sb, ISOWeek.GetYear(v), 4, spec.Pad, '0'); break;
                case "H": AppendNumber(sb, v.Hour, 2, spec.Pad, '0'); break;
                case "k": AppendNumber(sb, v.Hour, 2, spec.Pad, ' '); break;
                case "I": AppendNumber(sb, Hour12(v), 2, spec.Pad, '0'); break;
                case "l": AppendNumber(sb, Hour12(v), 2, spec.Pad, ' '); break;
                case "p": sb.Append(v.Hour < 12 ? "AM" : "PM"); break;
                case "P": sb.Append(v.Hour < 12 ? "am" : "pm"); break;
                case "M": AppendNumber(sb, v.Minute, 2, spec.Pad, '0'); break;
                case "S": AppendNumber(sb, v.Second, 2, spec.Pad, '0'); break;
                case "f" or "9f": AppendNumber(sb, Nanos(v), 9, '0', '0'); break;
                case "3f": AppendNumber(sb, Nanos(v) / 1_000_000, 3, '0', '0'); break;
                case "6f": AppendNumber(sb, Nanos(v) / 1_000, 6, '0', '0'); break;
                case ".3f": sb.Append('.'); AppendNumber(sb, Nanos(v) / 1_000_000, 3, '0', '0'); break;
                case ".6f": sb.Append('.'); AppendNumber(sb, Nanos(v) / 1_000, 6, '0', '0'); break;
                case ".9f": sb.Append('.'); AppendNumber(sb, Nanos(v), 9, '0', '0'); break;
                case ".f": AppendAutoFraction(sb, Nanos(v)); break;
                case "s": sb.Append(UnixSeconds(v)); break;
                case "z": sb.Append("+0000"); break;
                case ":z": sb.Append("+00:00"); break;
                case "Z": sb.Append("UTC"); break;
                case "%": sb.Append('%'); break;
                case "n": sb.Append('\n'); break;
                case "t": sb.Append('\t'); break;
                default:
                    var expansion = Expand(spec.Name)
                        ?? throw new FormatException($"Unsupported format specifier '%{spec.Name}' in \"{format}\"");
                    AppendFormatted(sb, v, expansion);
                    break;
            }
        }
    }

    /// <summary>Appends a fraction with 0, 3, 6 or 9 digits, whichever represents it exactly (chrono %.f).</summary>
    private static void AppendAutoFraction(StringBuilder sb, long nanos)
    {
        if (nanos == 0) return;
        sb.Append('.');
        if (nanos % 1_000_000 == 0) AppendNumber(sb, nanos / 1_000_000, 3, '0', '0');
        else if (nanos % 1_000 == 0) AppendNumber(sb, nanos / 1_000, 6, '0', '0');
        else AppendNumber(sb, nanos, 9, '0', '0');
    }

    private static void AppendNumber(StringBuilder sb, long value, int width, char padFlag, char defaultPad)
    {
        var digits = value.ToString(CultureInfo.InvariantCulture);
        char pad = padFlag switch
        {
            '-' => '\0',
            '_' => ' ',
            '0' => '0',
            _ => defaultPad
        };
        if (pad != '\0')
            sb.Append(pad, Math.Max(0, width - digits.Length));
        sb.Append(digits);
    }

    private static int Hour12(DateTime v) => v.Hour % 12 == 0 ? 12 : v.Hour % 12;

    private static long Nanos(DateTime v) => v.Ticks % TimeSpan.TicksPerSecond * 100;

    private static long UnixSeconds(DateTime v)
    {
        long ticks = v.Ticks - DateTime.UnixEpoch.Ticks;
        long seconds = ticks / TimeSpan.TicksPerSecond;
        return ticks % TimeSpan.TicksPerSecond < 0 ? seconds - 1 : seconds;
    }

    // ============================================================================
    // Parsing
    // ============================================================================

    /// <summary>Parses <paramref name="input"/>, which must match <paramref name="format"/> completely.</summary>
    public static bool TryParse(string input, string format, out DateTime result)
    {
        result = default;
        var fields = new ParsedFields();
        int pos = 0;
        return ParseInto(input, ref pos, format, fields)
            && pos == input.Length
            && fields.TryResolve(out result);
    }

    private static bool ParseInto(string input, ref int pos, string format, ParsedFields f)
    {
        for (int i = 0; i < format.Length; i++)
        {
            char c = format[i];
            if (char.IsWhiteSpace(c))
            {
                // Whitespace in the format matches any amount of whitespace, including none
                SkipWhitespace(input, ref pos);
                continue;
            }
            if (c != '%')
            {
                if (pos >= input.Length || input[pos] != c) return false;
                pos++;
                continue;
            }

            var spec = ReadSpecifier(format, ref i);
            bool ok = spec.Name switch
            {
                "Y" => ReadSigned(input, ref pos, 4, out var year) && Set(ref f.Year, (int)year),
                "y" => ReadNumber(input, ref pos, 2, out var yy) && Set(ref f.YearMod100, (int)yy),
                "C" => ReadNumber(input, ref pos, 2, out var century) && Set(ref f.Century, (int)century),
                "m" => ReadNumber(input, ref pos, 2, out var month) && Set(ref f.Month, (int)month),
                "b" or "h" or "B" => ReadName(input, ref pos, Names.MonthNames, Names.AbbreviatedMonthNames, out var m)
                    && Set(ref f.Month, m + 1),
                "d" or "e" => ReadNumber(input, ref pos, 2, out var day) && Set(ref f.Day, (int)day),
                "a" or "A" => ReadName(input, ref pos, Names.DayNames, Names.AbbreviatedDayNames, out _),
                "w" or "u" => ReadNumber(input, ref pos, 1, out _),
                "j" => ReadNumber(input, ref pos, 3, out var ordinal) && Set(ref f.Ordinal, (int)ordinal),
                "H" or "k" => ReadNumber(input, ref pos, 2, out var hour) && Set(ref f.Hour, (int)hour),
                "I" or "l" => ReadNumber(input, ref pos, 2, out var hour12) && Set(ref f.Hour12, (int)hour12),
                "p" or "P" => ReadAmPm(input, ref pos, out var pm) && Set(ref f.Pm, pm),
                "M" => ReadNumber(input, ref pos, 2, out var minute) && Set(ref f.Minute, (int)minute),
                "S" => ReadNumber(input, ref pos, 2, out var second) && Set(ref f.Second, (int)second),
                "f" => ReadFraction(input, ref pos, 1, 9, out f.Nanos),
                "3f" => ReadFraction(input, ref pos, 3, 3, out f.Nanos),
                "6f" => ReadFraction(input, ref pos, 6, 6, out f.Nanos),
                "9f" => ReadFraction(input, ref pos, 9, 9, out f.Nanos),
                ".f" => !Peek(input, pos, '.') || (++pos > 0 && ReadFraction(input, ref pos, 1, 9, out f.Nanos)),
                ".3f" => Peek(input, pos, '.') && ++pos > 0 && ReadFraction(input, ref pos, 3, 3, out f.Nanos),
                ".6f" => Peek(input, pos, '.') && ++pos > 0 && ReadFraction(input, ref pos, 6, 6, out f.Nanos),
                ".9f" => Peek(input, pos, '.') && ++pos > 0 && ReadFraction(input, ref pos, 9, 9, out f.Nanos),
                "s" => ReadSigned(input, ref pos, 19, out var timestamp) && Set(ref f.Timestamp, timestamp),
                "z" or ":z" => ReadOffset(input, ref pos, out var offset) && Set(ref f.OffsetMinutes, offset),
                "%" => Peek(input, pos, '%') && ++pos > 0,
                "n" or "t" => SkipWhitespace(input, ref pos),
                "V" or "G" or "Z" => throw new FormatException($"Format specifier '%{spec.Name}' is not supported for parsing"),
                _ => ParseInto(input, ref pos,
                    Expand(spec.Name) ?? throw new FormatException($"Unsupported format specifier '%{spec.Name}' in \"{format}\""),
                    f)
            };
            if (!ok) return false;
        }
        return true;
    }

    private static bool Set<T>(ref T? field, T value) where T : struct
    {
        field = value;
        return true;
    }

    private static bool Peek(string input, int pos, char c) => pos < input.Length && input[pos] == c;

    private static bool SkipWhitespace(string input, ref int pos)
    {
        while (pos < input.Length && char.IsWhiteSpace(input[pos])) pos++;
        return true;
    }

    /// <summary>Reads 1 to maxDigits ASCII digits after any leading whitespace.</summary>
    private static bool ReadNumber(string input, ref int pos, int maxDigits, out long value)
    {
        SkipWhitespace(input, ref pos);
        value = 0;
        int start = pos;
        while (pos < input.Length && pos - start < maxDigits && char.IsAsciiDigit(input[pos]))
        {
            value = value * 10 + (input[pos] - '0');
            pos++;
        }
        return pos > start;
    }

    /// <summary>Like ReadNumber, but a leading sign allows any number of digits (chrono's %Y and %s).</summary>
    private static bool ReadSigned(string input, ref int pos, int maxDigits, out long value)
    {
        SkipWhitespace(input, ref pos);
        if (Peek(input, pos, '-') || Peek(input, pos, '+'))
        {
            bool negative = input[pos] == '-';
            pos++;
            if (!ReadNumber(input, ref pos, 19, out value)) return false;
            if (negative) value = -value;
            return true;
        }
        return ReadNumber(input, ref pos, maxDigits, out value);
    }

    /// <summary>Reads digits as a fraction of a second, returning nanoseconds ("5" is 500,000,000).</summary>
    private static bool ReadFraction(string input, ref int pos, int minDigits, int maxDigits, out long nanos)
    {
        nanos = 0;
        int start = pos;
        while (pos < input.Length && pos - start < maxDigits && char.IsAsciiDigit(input[pos]))
        {
            nanos = nanos * 10 + (input[pos] - '0');
            pos++;
        }
        int digits = pos - start;
        if (digits < minDigits) return false;
        for (int d = digits; d < 9; d++) nanos *= 10;
        return true;
    }

    /// <summary>Case-insensitive match of a full or abbreviated name; returns its index.</summary>
    private static bool ReadName(string input, ref int pos, string[] full, string[] abbreviated, out int index)
    {
        foreach (var names in new[] { full, abbreviated })
        {
            for (int k = 0; k < names.Length; k++)
            {
                var name = names[k];
                if (name.Length > 0 && pos + name.Length <= input.Length
                    && string.Compare(input, pos, name, 0, name.Length, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    pos += name.Length;
                    index = k;
                    return true;
                }
            }
        }
        index = -1;
        return false;
    }

    private static bool ReadAmPm(string input, ref int pos, out bool pm)
    {
        pm = false;
        if (pos + 2 > input.Length) return false;
        var token = input.Substring(pos, 2);
        if (token.Equals("AM", StringComparison.OrdinalIgnoreCase)) pm = false;
        else if (token.Equals("PM", StringComparison.OrdinalIgnoreCase)) pm = true;
        else return false;
        pos += 2;
        return true;
    }

    /// <summary>Reads "Z", "+hh", "+hhmm" or "+hh:mm" (or '-'); returns the offset in minutes.</summary>
    private static bool ReadOffset(string input, ref int pos, out int minutes)
    {
        minutes = 0;
        SkipWhitespace(input, ref pos);
        if (Peek(input, pos, 'Z') || Peek(input, pos, 'z'))
        {
            pos++;
            return true;
        }
        if (!Peek(input, pos, '+') && !Peek(input, pos, '-')) return false;
        int sign = input[pos] == '-' ? -1 : 1;
        pos++;

        if (!ReadExactDigits(input, ref pos, 2, out var hours)) return false;
        int mins = 0;
        if (Peek(input, pos, ':')) pos++;
        if (pos < input.Length && char.IsAsciiDigit(input[pos]) && !ReadExactDigits(input, ref pos, 2, out mins)) return false;

        minutes = sign * (hours * 60 + mins);
        return true;
    }

    private static bool ReadExactDigits(string input, ref int pos, int count, out int value)
    {
        value = 0;
        if (pos + count > input.Length) return false;
        for (int k = 0; k < count; k++)
        {
            if (!char.IsAsciiDigit(input[pos + k])) return false;
            value = value * 10 + (input[pos + k] - '0');
        }
        pos += count;
        return true;
    }

    // ============================================================================
    // Specifiers
    // ============================================================================

    private readonly record struct Specifier(string Name, char Pad);

    /// <summary>Reads the specifier after the '%' at format[i], leaving i on its last character.</summary>
    private static Specifier ReadSpecifier(string format, ref int i)
    {
        int start = i;
        if (++i >= format.Length)
            throw new FormatException($"Format string \"{format}\" ends with a lone '%'");

        char pad = '\0';
        if (format[i] is '-' or '_' or '0')
        {
            pad = format[i];
            if (++i >= format.Length)
                throw new FormatException($"Incomplete format specifier at position {start} in \"{format}\"");
        }

        // Multi-character specifiers: %.f %.3f %3f %:z
        if (format[i] is '.' or '3' or '6' or '9' or ':')
        {
            int end = i;
            while (end < format.Length && format[end] is '.' or '3' or '6' or '9' or ':') end++;
            if (end >= format.Length)
                throw new FormatException($"Incomplete format specifier at position {start} in \"{format}\"");
            var name = format.Substring(i, end - i + 1);
            i = end;
            return new Specifier(name, pad);
        }

        return new Specifier(format[i].ToString(), pad);
    }

    private static string? Expand(string name) => name switch
    {
        "F" => "%Y-%m-%d",
        "T" or "X" => "%H:%M:%S",
        "R" => "%H:%M",
        "D" or "x" => "%m/%d/%y",
        "r" => "%I:%M:%S %p",
        "c" => "%a %b %e %H:%M:%S %Y",
        "v" => "%e-%b-%Y",
        _ => null
    };

    // ============================================================================
    // Field resolution
    // ============================================================================

    private sealed class ParsedFields
    {
        public int? Year, YearMod100, Century, Month, Day, Ordinal, Hour, Hour12, Minute, Second, OffsetMinutes;
        public bool? Pm;
        public long? Timestamp;
        public long Nanos;

        public bool TryResolve(out DateTime result)
        {
            result = default;
            try
            {
                if (Timestamp.HasValue)
                {
                    result = DateTime.UnixEpoch.AddSeconds(Timestamp.Value).AddTicks(Nanos / 100);
                    return true;
                }

                int? year = Year;
                if (year is null && YearMod100.HasValue)
                {
                    // POSIX: without a century, 69-99 are 1969-1999 and 00-68 are 2000-2068
                    year = Century.HasValue
                        ? Century.Value * 100 + YearMod100.Value
                        : YearMod100.Value + (YearMod100.Value >= 69 ? 1900 : 2000);
                }
                if (year is null) return false;

                DateTime date;
                if (Month.HasValue && Day.HasValue)
                    date = new DateTime(year.Value, Month.Value, Day.Value, 0, 0, 0, DateTimeKind.Utc);
                else if (Ordinal.HasValue && Month is null && Day is null)
                {
                    if (Ordinal.Value < 1 || Ordinal.Value > (DateTime.IsLeapYear(year.Value) ? 366 : 365)) return false;
                    date = new DateTime(year.Value, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(Ordinal.Value - 1);
                }
                else return false;

                int hour;
                if (Hour12.HasValue)
                {
                    if (Pm is null || Hour12.Value < 1 || Hour12.Value > 12) return false;
                    hour = Hour12.Value % 12 + (Pm.Value ? 12 : 0);
                }
                else hour = Hour ?? 0;

                int minute = Minute ?? 0, second = Second ?? 0;
                if (hour > 23 || minute > 59 || second > 59) return false;

                result = date.AddHours(hour).AddMinutes(minute).AddSeconds(second).AddTicks(Nanos / 100);
                if (OffsetMinutes.HasValue)
                    result = result.AddMinutes(-OffsetMinutes.Value);
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }
    }
}
