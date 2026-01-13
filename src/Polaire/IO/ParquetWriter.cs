// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using ParquetSchema = Parquet.Schema;
using ParquetData = Parquet.Data;
using PolaireDataType = Polaire.DataTypes.DataType;

namespace Polaire.IO;

/// <summary>
/// High-performance Parquet writer.
/// </summary>
public static class ParquetWriter
{
    /// <summary>
    /// Writes a DataFrame to a Parquet file.
    /// </summary>
    public static void Write(DataFrame df, string path, ParquetWriteOptions? options = null)
    {
        options ??= ParquetWriteOptions.Default;

        using var stream = File.Create(path);
        Write(df, stream, options);
    }

    /// <summary>
    /// Writes a DataFrame to a Parquet stream.
    /// </summary>
    public static void Write(DataFrame df, Stream stream, ParquetWriteOptions? options = null)
    {
        options ??= ParquetWriteOptions.Default;

        // Build schema
        var fields = new List<ParquetSchema.DataField>();
        foreach (var colName in df.Columns)
        {
            var series = df[colName];
            var field = CreateField(colName, series.DataType, series.HasNulls);
            fields.Add(field);
        }

        var schema = new ParquetSchema.ParquetSchema(fields.ToArray());

        // Write data
        using var writer = global::Parquet.ParquetWriter.CreateAsync(schema, stream).GetAwaiter().GetResult();
        writer.CompressionMethod = options.Compression;

        // Calculate row groups
        var rowsPerGroup = options.RowGroupSize;
        var totalRows = df.Height;
        var rowGroupCount = (totalRows + rowsPerGroup - 1) / rowsPerGroup;

        for (int rg = 0; rg < rowGroupCount; rg++)
        {
            var startRow = rg * rowsPerGroup;
            var endRow = Math.Min(startRow + rowsPerGroup, totalRows);
            var rowCount = endRow - startRow;

            using var rowGroupWriter = writer.CreateRowGroup();

            for (int colIdx = 0; colIdx < df.Columns.Count; colIdx++)
            {
                var colName = df.Columns.ElementAt(colIdx);
                var series = df[colName];
                var field = fields[colIdx];
                var column = CreateDataColumn(field, series, startRow, rowCount);
                rowGroupWriter.WriteColumnAsync(column).GetAwaiter().GetResult();
            }
        }
    }

    private static ParquetSchema.DataField CreateField(string name, PolaireDataType dtype, bool nullable)
    {
        return dtype switch
        {
            PolaireDataType.Int8Type or PolaireDataType.Int16Type or PolaireDataType.Int32Type =>
                new ParquetSchema.DataField<int?>(name),
            PolaireDataType.Int64Type =>
                new ParquetSchema.DataField<long?>(name),
            PolaireDataType.UInt8Type or PolaireDataType.UInt16Type or PolaireDataType.UInt32Type =>
                new ParquetSchema.DataField<int?>(name),
            PolaireDataType.UInt64Type =>
                new ParquetSchema.DataField<long?>(name),
            PolaireDataType.Float32Type =>
                new ParquetSchema.DataField<float?>(name),
            PolaireDataType.Float64Type =>
                new ParquetSchema.DataField<double?>(name),
            PolaireDataType.BooleanType =>
                new ParquetSchema.DataField<bool?>(name),
            PolaireDataType.StringType =>
                new ParquetSchema.DataField<string>(name),
            _ => new ParquetSchema.DataField<string>(name)
        };
    }

    private static ParquetData.DataColumn CreateDataColumn(ParquetSchema.DataField field, Series series, int startRow, int rowCount)
    {
        // Route based on series data type for reliability
        return series.DataType switch
        {
            PolaireDataType.Int8Type or PolaireDataType.Int16Type or PolaireDataType.Int32Type or
            PolaireDataType.UInt8Type or PolaireDataType.UInt16Type or PolaireDataType.UInt32Type =>
                CreateInt32Column(field, series, startRow, rowCount),
            PolaireDataType.Int64Type or PolaireDataType.UInt64Type =>
                CreateInt64Column(field, series, startRow, rowCount),
            PolaireDataType.Float32Type =>
                CreateFloatColumn(field, series, startRow, rowCount),
            PolaireDataType.Float64Type =>
                CreateDoubleColumn(field, series, startRow, rowCount),
            PolaireDataType.BooleanType =>
                CreateBoolColumn(field, series, startRow, rowCount),
            _ => CreateStringColumn(field, series, startRow, rowCount)
        };
    }

    private static ParquetData.DataColumn CreateInt32Column(ParquetSchema.DataField field, Series series, int startRow, int rowCount)
    {
        var values = new int?[rowCount];
        for (int i = 0; i < rowCount; i++)
        {
            var val = series[startRow + i];
            values[i] = val.IsNull ? null : val.AsInt32();
        }
        return new ParquetData.DataColumn(field, values);
    }

    private static ParquetData.DataColumn CreateInt64Column(ParquetSchema.DataField field, Series series, int startRow, int rowCount)
    {
        var values = new long?[rowCount];
        for (int i = 0; i < rowCount; i++)
        {
            var val = series[startRow + i];
            values[i] = val.IsNull ? null : val.AsInt64();
        }
        return new ParquetData.DataColumn(field, values);
    }

    private static ParquetData.DataColumn CreateFloatColumn(ParquetSchema.DataField field, Series series, int startRow, int rowCount)
    {
        var values = new float?[rowCount];
        for (int i = 0; i < rowCount; i++)
        {
            var val = series[startRow + i];
            values[i] = val.IsNull ? null : val.AsFloat32();
        }
        return new ParquetData.DataColumn(field, values);
    }

    private static ParquetData.DataColumn CreateDoubleColumn(ParquetSchema.DataField field, Series series, int startRow, int rowCount)
    {
        var values = new double?[rowCount];
        for (int i = 0; i < rowCount; i++)
        {
            var val = series[startRow + i];
            values[i] = val.IsNull ? null : val.TryGetDouble(out var d) ? d : null;
        }
        return new ParquetData.DataColumn(field, values);
    }

    private static ParquetData.DataColumn CreateBoolColumn(ParquetSchema.DataField field, Series series, int startRow, int rowCount)
    {
        var values = new bool?[rowCount];
        for (int i = 0; i < rowCount; i++)
        {
            var val = series[startRow + i];
            values[i] = val.IsNull ? null : val.AsBoolean();
        }
        return new ParquetData.DataColumn(field, values);
    }

    private static ParquetData.DataColumn CreateStringColumn(ParquetSchema.DataField field, Series series, int startRow, int rowCount)
    {
        var values = new string?[rowCount];
        var isStringType = series.DataType == PolaireDataType.String || series.DataType == PolaireDataType.LargeString;
        for (int i = 0; i < rowCount; i++)
        {
            var val = series[startRow + i];
            if (val.IsNull)
                values[i] = null;
            else if (isStringType)
                values[i] = val.AsString();
            else
                values[i] = val.ToString();
        }
        return new ParquetData.DataColumn(field, values);
    }
}

/// <summary>
/// Configuration options for writing Parquet files.
/// </summary>
public sealed class ParquetWriteOptions
{
    /// <summary>Number of rows per row group.</summary>
    public int RowGroupSize { get; init; } = 250_000;  // ~512^2, Polars default

    /// <summary>Compression codec.</summary>
    public Parquet.CompressionMethod Compression { get; init; } = Parquet.CompressionMethod.Snappy;

    /// <summary>Default options.</summary>
    public static ParquetWriteOptions Default { get; } = new();
}
