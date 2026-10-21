// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Polaire.IO;

/// <summary>
/// Writes DataFrames as JSON (an array of row objects, the layout JsonReader reads) or
/// NDJSON (one row object per line), matching Polars write_json and write_ndjson.
/// </summary>
/// <remarks>
/// Numbers and booleans are written as JSON values; NaN and infinities become null because JSON
/// cannot represent them. Dates are written as "yyyy-MM-dd" and datetimes as ISO 8601 without an
/// offset (e.g. "2024-06-15T12:30:45.123"). List and struct columns become arrays and objects.
/// Non-ASCII text is written as UTF-8 without escaping.
/// </remarks>
public static class JsonWriter
{
    private static readonly JsonWriterOptions WriterOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly JsonSerializerOptions NestedValueOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
    };

    // ============================================================================
    // JSON
    // ============================================================================

    /// <summary>Writes a DataFrame to a JSON file.</summary>
    public static void Write(DataFrame df, string path)
    {
        using var stream = File.Create(path);
        Write(df, stream);
    }

    /// <summary>Writes a DataFrame to a JSON stream. The stream is left open.</summary>
    public static void Write(DataFrame df, Stream stream)
    {
        var (columns, names) = Prepare(df);
        using var writer = new Utf8JsonWriter(stream, WriterOptions);

        writer.WriteStartArray();
        for (int row = 0; row < df.Height; row++)
            WriteRow(writer, columns, names, row);
        writer.WriteEndArray();
    }

    /// <summary>Converts a DataFrame to a JSON string.</summary>
    public static string WriteString(DataFrame df)
    {
        using var stream = new MemoryStream();
        Write(df, stream);
        return Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);
    }

    // ============================================================================
    // NDJSON
    // ============================================================================

    /// <summary>Writes a DataFrame to an NDJSON file.</summary>
    public static void WriteNdjson(DataFrame df, string path)
    {
        using var stream = File.Create(path);
        WriteNdjson(df, stream);
    }

    /// <summary>Writes a DataFrame to an NDJSON stream, one object per line. The stream is left open.</summary>
    public static void WriteNdjson(DataFrame df, Stream stream)
    {
        var (columns, names) = Prepare(df);
        using var writer = new Utf8JsonWriter(stream, WriterOptions);

        for (int row = 0; row < df.Height; row++)
        {
            WriteRow(writer, columns, names, row);
            writer.Flush();
            stream.WriteByte((byte)'\n');
            // Each line is a separate JSON document
            writer.Reset();
        }
    }

    /// <summary>Converts a DataFrame to an NDJSON string.</summary>
    public static string WriteNdjsonString(DataFrame df)
    {
        using var stream = new MemoryStream();
        WriteNdjson(df, stream);
        return Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);
    }

    // ============================================================================
    // Values
    // ============================================================================

    private static (Series[] Columns, JsonEncodedText[] Names) Prepare(DataFrame df)
    {
        var columns = df.ToArray();
        var names = columns.Select(c => JsonEncodedText.Encode(c.Name, JavaScriptEncoder.UnsafeRelaxedJsonEscaping)).ToArray();
        return (columns, names);
    }

    private static void WriteRow(Utf8JsonWriter writer, Series[] columns, JsonEncodedText[] names, int row)
    {
        writer.WriteStartObject();
        for (int c = 0; c < columns.Length; c++)
        {
            writer.WritePropertyName(names[c]);
            WriteValue(writer, columns[c], row);
        }
        writer.WriteEndObject();
    }

    private static void WriteValue(Utf8JsonWriter writer, Series series, int row)
    {
        if (series.IsNull(row))
        {
            writer.WriteNullValue();
            return;
        }

        if (series.DataType is DataType.ListType or DataType.StructType)
        {
            JsonSerializer.Serialize(writer, series.Data.GetBoxedValue(row), NestedValueOptions);
            return;
        }

        var value = series[row];
        switch (value.Kind)
        {
            case AnyValueKind.Int8 or AnyValueKind.Int16 or AnyValueKind.Int32 or AnyValueKind.Int64:
                value.TryGetInt64(out var l);
                writer.WriteNumberValue(l);
                break;
            case AnyValueKind.UInt8 or AnyValueKind.UInt16 or AnyValueKind.UInt32 or AnyValueKind.UInt64:
                writer.WriteNumberValue(value.Cast(DataType.UInt64).AsUInt64());
                break;
            case AnyValueKind.Float32:
                var f = value.AsFloat32();
                if (float.IsFinite(f)) WriteFloat(writer, f.ToString("R", CultureInfo.InvariantCulture));
                else writer.WriteNullValue();
                break;
            case AnyValueKind.Float64:
                var d = value.AsFloat64();
                if (double.IsFinite(d)) WriteFloat(writer, d.ToString("R", CultureInfo.InvariantCulture));
                else writer.WriteNullValue();
                break;
            case AnyValueKind.Boolean:
                writer.WriteBooleanValue(value.AsBoolean());
                break;
            case AnyValueKind.String:
                writer.WriteStringValue(value.AsString());
                break;
            case AnyValueKind.Date:
                writer.WriteStringValue(value.AsDate().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                break;
            case AnyValueKind.DateTime:
                writer.WriteStringValue(value.AsDateTime().ToString("yyyy-MM-ddTHH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture));
                break;
            case AnyValueKind.Null:
                writer.WriteNullValue();
                break;
            default:
                writer.WriteStringValue(value.ToString());
                break;
        }
    }

    /// <summary>Writes a float so that it reads back as a float: 1.0 is written as "1.0", not "1".</summary>
    private static void WriteFloat(Utf8JsonWriter writer, string roundTrip)
    {
        writer.WriteRawValue(roundTrip.AsSpan().IndexOfAny('.', 'E') >= 0 ? roundTrip : roundTrip + ".0");
    }
}
