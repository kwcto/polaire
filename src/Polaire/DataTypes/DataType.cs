// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using System.Diagnostics.CodeAnalysis;

namespace Polaire.DataTypes;

/// <summary>
/// Represents all data types supported by Polaire, matching Polars' type system.
/// Based on Apache Arrow's type system with extensions for temporal and nested types.
/// </summary>
public abstract record DataType
{
    private DataType() { } // Sealed hierarchy

    // ============================================================================
    // Numeric Types
    // ============================================================================

    /// <summary>8-bit signed integer (-128 to 127)</summary>
    public sealed record Int8Type : DataType
    {
        public static readonly Int8Type Instance = new();
        private Int8Type() { }
        public override string ToString() => "Int8";
    }

    /// <summary>16-bit signed integer (-32,768 to 32,767)</summary>
    public sealed record Int16Type : DataType
    {
        public static readonly Int16Type Instance = new();
        private Int16Type() { }
        public override string ToString() => "Int16";
    }

    /// <summary>32-bit signed integer</summary>
    public sealed record Int32Type : DataType
    {
        public static readonly Int32Type Instance = new();
        private Int32Type() { }
        public override string ToString() => "Int32";
    }

    /// <summary>64-bit signed integer</summary>
    public sealed record Int64Type : DataType
    {
        public static readonly Int64Type Instance = new();
        private Int64Type() { }
        public override string ToString() => "Int64";
    }

    /// <summary>128-bit signed integer for high-precision calculations</summary>
    public sealed record Int128Type : DataType
    {
        public static readonly Int128Type Instance = new();
        private Int128Type() { }
        public override string ToString() => "Int128";
    }

    /// <summary>8-bit unsigned integer (0 to 255)</summary>
    public sealed record UInt8Type : DataType
    {
        public static readonly UInt8Type Instance = new();
        private UInt8Type() { }
        public override string ToString() => "UInt8";
    }

    /// <summary>16-bit unsigned integer (0 to 65,535)</summary>
    public sealed record UInt16Type : DataType
    {
        public static readonly UInt16Type Instance = new();
        private UInt16Type() { }
        public override string ToString() => "UInt16";
    }

    /// <summary>32-bit unsigned integer</summary>
    public sealed record UInt32Type : DataType
    {
        public static readonly UInt32Type Instance = new();
        private UInt32Type() { }
        public override string ToString() => "UInt32";
    }

    /// <summary>64-bit unsigned integer</summary>
    public sealed record UInt64Type : DataType
    {
        public static readonly UInt64Type Instance = new();
        private UInt64Type() { }
        public override string ToString() => "UInt64";
    }

    /// <summary>128-bit unsigned integer</summary>
    public sealed record UInt128Type : DataType
    {
        public static readonly UInt128Type Instance = new();
        private UInt128Type() { }
        public override string ToString() => "UInt128";
    }

    /// <summary>32-bit IEEE 754 floating point</summary>
    public sealed record Float32Type : DataType
    {
        public static readonly Float32Type Instance = new();
        private Float32Type() { }
        public override string ToString() => "Float32";
    }

    /// <summary>64-bit IEEE 754 floating point</summary>
    public sealed record Float64Type : DataType
    {
        public static readonly Float64Type Instance = new();
        private Float64Type() { }
        public override string ToString() => "Float64";
    }

    /// <summary>
    /// Fixed-point decimal backed by 128-bit integer.
    /// Allows up to 38 significant digits with configurable precision and scale.
    /// </summary>
    public sealed record DecimalType(int Precision, int Scale) : DataType
    {
        public override string ToString() => $"Decimal({Precision}, {Scale})";
    }

    // ============================================================================
    // Boolean Type
    // ============================================================================

    /// <summary>Boolean type, bit-packed for efficient storage (1 byte per 8 values)</summary>
    public sealed record BooleanType : DataType
    {
        public static readonly BooleanType Instance = new();
        private BooleanType() { }
        public override string ToString() => "Boolean";
    }

    // ============================================================================
    // String and Binary Types
    // ============================================================================

    /// <summary>Variable-length UTF-8 encoded string</summary>
    public sealed record StringType : DataType
    {
        public static readonly StringType Instance = new();
        private StringType() { }
        public override string ToString() => "String";
    }

    /// <summary>Variable-length binary data</summary>
    public sealed record BinaryType : DataType
    {
        public static readonly BinaryType Instance = new();
        private BinaryType() { }
        public override string ToString() => "Binary";
    }

    /// <summary>Binary data with 64-bit offsets for large binary values</summary>
    public sealed record LargeBinaryType : DataType
    {
        public static readonly LargeBinaryType Instance = new();
        private LargeBinaryType() { }
        public override string ToString() => "LargeBinary";
    }

    /// <summary>UTF-8 string with 64-bit offsets for large strings</summary>
    public sealed record LargeStringType : DataType
    {
        public static readonly LargeStringType Instance = new();
        private LargeStringType() { }
        public override string ToString() => "LargeString";
    }

    // ============================================================================
    // Temporal Types
    // ============================================================================

    /// <summary>32-bit date representing days since Unix epoch (1970-01-01)</summary>
    public sealed record DateType : DataType
    {
        public static readonly DateType Instance = new();
        private DateType() { }
        public override string ToString() => "Date";
    }

    /// <summary>64-bit time representing nanoseconds since midnight</summary>
    public sealed record TimeType : DataType
    {
        public static readonly TimeType Instance = new();
        private TimeType() { }
        public override string ToString() => "Time";
    }

    /// <summary>
    /// 64-bit datetime representing elapsed time since Unix epoch.
    /// </summary>
    public sealed record DateTimeType(TimeUnit Unit, string? TimeZone = null) : DataType
    {
        public override string ToString() =>
            TimeZone is null ? $"DateTime({Unit})" : $"DateTime({Unit}, {TimeZone})";
    }

    /// <summary>64-bit duration representing a time span</summary>
    public sealed record DurationType(TimeUnit Unit) : DataType
    {
        public override string ToString() => $"Duration({Unit})";
    }

    // ============================================================================
    // Nested Types
    // ============================================================================

    /// <summary>Variable-length list of elements of the same type</summary>
    public sealed record ListType(DataType Inner) : DataType
    {
        public override string ToString() => $"List({Inner})";
    }

    /// <summary>Fixed-size array with known dimensions</summary>
    public sealed record ArrayType(DataType Inner, int Size) : DataType
    {
        public override string ToString() => $"Array({Inner}, {Size})";
    }

    /// <summary>Composite type with named fields</summary>
    public sealed record StructType(IReadOnlyList<Field> Fields) : DataType
    {
        public override string ToString() =>
            $"Struct({{{string.Join(", ", Fields.Select(f => $"{f.Name}: {f.Type}"))}}})";
    }

    // ============================================================================
    // Categorical Types
    // ============================================================================

    /// <summary>
    /// Categorical type with runtime-inferred categories.
    /// </summary>
    public sealed record CategoricalType(CategoricalOrdering Ordering = CategoricalOrdering.Physical) : DataType
    {
        public override string ToString() => $"Categorical({Ordering})";
    }

    /// <summary>
    /// Enum type with predefined categories for better performance and validation.
    /// </summary>
    public sealed record EnumType(IReadOnlyList<string> Categories) : DataType
    {
        public override string ToString() =>
            $"Enum([{string.Join(", ", Categories.Take(5))}{(Categories.Count > 5 ? ", ..." : "")}])";
    }

    // ============================================================================
    // Special Types
    // ============================================================================

    /// <summary>Represents null/missing values</summary>
    public sealed record NullType : DataType
    {
        public static readonly NullType Instance = new();
        private NullType() { }
        public override string ToString() => "Null";
    }

    /// <summary>Object type for arbitrary data (escape hatch)</summary>
    public sealed record ObjectType(string Identifier) : DataType
    {
        public override string ToString() => $"Object({Identifier})";
    }

    /// <summary>Unknown type placeholder (for lazy schema resolution)</summary>
    public sealed record UnknownType : DataType
    {
        public static readonly UnknownType Instance = new();
        private UnknownType() { }
        public override string ToString() => "Unknown";
    }

    // ============================================================================
    // Helper Methods
    // ============================================================================

    /// <summary>Returns true if this is a numeric type (integer or floating point)</summary>
    public bool IsNumeric => this is Int8Type or Int16Type or Int32Type or Int64Type or Int128Type
        or UInt8Type or UInt16Type or UInt32Type or UInt64Type or UInt128Type
        or Float32Type or Float64Type or DecimalType;

    /// <summary>Returns true if this is an integer type</summary>
    public bool IsInteger => this is Int8Type or Int16Type or Int32Type or Int64Type or Int128Type
        or UInt8Type or UInt16Type or UInt32Type or UInt64Type or UInt128Type;

    /// <summary>Returns true if this is a floating point type</summary>
    public bool IsFloat => this is Float32Type or Float64Type;

    /// <summary>Returns true if this is a temporal type</summary>
    public bool IsTemporal => this is DateType or TimeType or DateTimeType or DurationType;

    /// <summary>Returns true if this is a nested type (list, array, or struct)</summary>
    public bool IsNested => this is ListType or ArrayType or StructType;

    /// <summary>Returns true if this is a signed numeric type</summary>
    public bool IsSigned => this is Int8Type or Int16Type or Int32Type or Int64Type or Int128Type
        or Float32Type or Float64Type or DecimalType;

    /// <summary>Gets the physical size in bytes for fixed-size types, or -1 for variable-size types</summary>
    public int PhysicalSize => this switch
    {
        Int8Type or UInt8Type or BooleanType => 1,
        Int16Type or UInt16Type => 2,
        Int32Type or UInt32Type or Float32Type or DateType => 4,
        Int64Type or UInt64Type or Float64Type or TimeType or DateTimeType or DurationType => 8,
        Int128Type or UInt128Type or DecimalType => 16,
        _ => -1
    };

    // ============================================================================
    // Static Factory Methods
    // ============================================================================

    public static readonly Int8Type Int8 = Int8Type.Instance;
    public static readonly Int16Type Int16 = Int16Type.Instance;
    public static readonly Int32Type Int32 = Int32Type.Instance;
    public static readonly Int64Type Int64 = Int64Type.Instance;
    public static readonly Int128Type Int128 = Int128Type.Instance;
    public static readonly UInt8Type UInt8 = UInt8Type.Instance;
    public static readonly UInt16Type UInt16 = UInt16Type.Instance;
    public static readonly UInt32Type UInt32 = UInt32Type.Instance;
    public static readonly UInt64Type UInt64 = UInt64Type.Instance;
    public static readonly UInt128Type UInt128 = UInt128Type.Instance;
    public static readonly Float32Type Float32 = Float32Type.Instance;
    public static readonly Float64Type Float64 = Float64Type.Instance;
    public static readonly BooleanType Boolean = BooleanType.Instance;
    public static readonly StringType String = StringType.Instance;
    public static readonly BinaryType Binary = BinaryType.Instance;
    public static readonly LargeBinaryType LargeBinary = LargeBinaryType.Instance;
    public static readonly LargeStringType LargeString = LargeStringType.Instance;
    public static readonly DateType Date = DateType.Instance;
    public static readonly TimeType Time = TimeType.Instance;
    public static readonly NullType Null = NullType.Instance;
    public static readonly UnknownType Unknown = UnknownType.Instance;

    public static DateTimeType DateTime(TimeUnit unit, string? timeZone = null) => new(unit, timeZone);
    public static DurationType Duration(TimeUnit unit) => new(unit);
    public static DecimalType Decimal(int precision, int scale) => new(precision, scale);
    public static ListType List(DataType inner) => new(inner);
    public static ArrayType Array(DataType inner, int size) => new(inner, size);
    public static StructType Struct(params Field[] fields) => new(fields);
    public static CategoricalType Categorical(CategoricalOrdering ordering = CategoricalOrdering.Physical) => new(ordering);
    public static EnumType Enum(params string[] categories) => new(categories);
    public static ObjectType Object(string identifier) => new(identifier);
}

/// <summary>Time unit for temporal types</summary>
public enum TimeUnit
{
    Nanoseconds,
    Microseconds,
    Milliseconds,
    Seconds
}

/// <summary>Ordering for categorical types</summary>
public enum CategoricalOrdering
{
    /// <summary>Order by physical representation (integer index)</summary>
    Physical,
    /// <summary>Order lexicographically by category string</summary>
    Lexical
}

/// <summary>Represents a field in a struct type</summary>
public readonly record struct Field(string Name, DataType Type, bool Nullable = true)
{
    public override string ToString() => $"{Name}: {Type}{(Nullable ? "?" : "")}";
}
