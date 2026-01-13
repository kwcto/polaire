// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using System.Globalization;
using System.Text;

namespace Polaire.IO;

/// <summary>
/// High-performance CSV writer.
/// </summary>
public static class CsvWriter
{
    /// <summary>
    /// Writes a DataFrame to a CSV file.
    /// </summary>
    public static void Write(DataFrame df, string path, CsvWriteOptions? options = null)
    {
        options ??= CsvWriteOptions.Default;

        using var stream = File.Create(path);
        Write(df, stream, options);
    }

    /// <summary>
    /// Writes a DataFrame to a CSV stream.
    /// </summary>
    public static void Write(DataFrame df, Stream stream, CsvWriteOptions? options = null)
    {
        options ??= CsvWriteOptions.Default;

        using var writer = new StreamWriter(stream, options.Encoding, leaveOpen: true);
        WriteInternal(df, writer, options);
    }

    /// <summary>
    /// Converts a DataFrame to a CSV string.
    /// </summary>
    public static string WriteString(DataFrame df, CsvWriteOptions? options = null)
    {
        options ??= CsvWriteOptions.Default;

        var sb = new StringBuilder();
        using var writer = new StringWriter(sb);
        WriteInternal(df, writer, options);
        return sb.ToString();
    }

    private static void WriteInternal(DataFrame df, TextWriter writer, CsvWriteOptions options)
    {
        var sep = options.Separator;
        var quote = options.QuoteChar;
        var lineEnding = options.LineEnding;
        var columns = df.Columns.ToArray();

        // Write header
        if (options.IncludeHeader)
        {
            for (int i = 0; i < columns.Length; i++)
            {
                if (i > 0) writer.Write(sep);
                WriteField(writer, columns[i], sep, quote);
            }
            writer.Write(lineEnding);
        }

        // Write data rows
        for (int row = 0; row < df.Height; row++)
        {
            for (int col = 0; col < columns.Length; col++)
            {
                if (col > 0) writer.Write(sep);

                var series = df[columns[col]];
                var value = series[row];

                WriteValue(writer, value, series.DataType, sep, quote, options);
            }
            writer.Write(lineEnding);
        }
    }

    private static void WriteField(TextWriter writer, string field, char sep, char quote)
    {
        if (NeedsQuoting(field, sep, quote))
        {
            writer.Write(quote);
            writer.Write(field.Replace(quote.ToString(), $"{quote}{quote}"));
            writer.Write(quote);
        }
        else
        {
            writer.Write(field);
        }
    }

    private static void WriteValue(TextWriter writer, AnyValue value, DataType dtype, char sep, char quote, CsvWriteOptions options)
    {
        if (value.IsNull)
        {
            writer.Write(options.NullValue);
            return;
        }

        var str = FormatValue(value, dtype, options);

        if (NeedsQuoting(str, sep, quote))
        {
            writer.Write(quote);
            writer.Write(str.Replace(quote.ToString(), $"{quote}{quote}"));
            writer.Write(quote);
        }
        else
        {
            writer.Write(str);
        }
    }

    private static string FormatValue(AnyValue value, DataType dtype, CsvWriteOptions options)
    {
        return dtype switch
        {
            DataType.Float32Type or DataType.Float64Type => FormatFloat(value, options),
            DataType.DateType => FormatDate(value, options),
            DataType.DateTimeType => FormatDateTime(value, options),
            DataType.BooleanType => value.AsBoolean() ? "true" : "false",
            DataType.StringType or DataType.LargeStringType => value.AsString(),
            _ => value.ToString() ?? ""
        };
    }

    private static string FormatFloat(AnyValue value, CsvWriteOptions options)
    {
        var d = value.TryGetDouble(out var dv) ? dv : value.AsFloat32();

        if (double.IsNaN(d) || double.IsInfinity(d))
            return options.NullValue;

        if (options.FloatPrecision >= 0)
            return d.ToString($"F{options.FloatPrecision}", CultureInfo.InvariantCulture);

        return d.ToString("G17", CultureInfo.InvariantCulture);
    }

    private static string FormatDate(AnyValue value, CsvWriteOptions options)
    {
        // AnyValue stores dates as long (ticks or days from epoch)
        // Try to extract as string and format
        var str = value.ToString();
        if (str != null && DateTime.TryParse(str, out var dt))
        {
            var dateOnly = DateOnly.FromDateTime(dt);
            return options.DateFormat != null
                ? dateOnly.ToString(options.DateFormat, CultureInfo.InvariantCulture)
                : dateOnly.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
        return str ?? "";
    }

    private static string FormatDateTime(AnyValue value, CsvWriteOptions options)
    {
        var str = value.ToString();
        if (str != null && DateTime.TryParse(str, out var dt))
        {
            return options.DateTimeFormat != null
                ? dt.ToString(options.DateTimeFormat, CultureInfo.InvariantCulture)
                : dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }
        return str ?? "";
    }

    private static bool NeedsQuoting(string value, char sep, char quote)
    {
        return value.Contains(sep) ||
               value.Contains(quote) ||
               value.Contains('\n') ||
               value.Contains('\r');
    }
}
