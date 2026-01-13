// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Polaire.DataTypes;

/// <summary>
/// A type-erased value that can hold any Polaire data type.
/// Used for heterogeneous operations and runtime value handling.
/// Optimized for minimal boxing and efficient memory layout.
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public readonly struct AnyValue : IEquatable<AnyValue>, IComparable<AnyValue>
{
    // Union-style storage for primitive types (avoid boxing)
    [FieldOffset(0)] private readonly long _intValue;
    [FieldOffset(0)] private readonly ulong _uintValue;
    [FieldOffset(0)] private readonly double _floatValue;
    [FieldOffset(0)] private readonly Int128 _int128Value;
    [FieldOffset(0)] private readonly UInt128 _uint128Value;

    // Reference type storage (strings, lists, etc.)
    [FieldOffset(16)] private readonly object? _objectValue;

    // Type discriminator
    [FieldOffset(24)] private readonly AnyValueKind _kind;

    // Null flag
    [FieldOffset(25)] private readonly bool _isNull;

    private AnyValue(AnyValueKind kind, bool isNull = false)
    {
        _kind = kind;
        _isNull = isNull;
        _intValue = 0;
        _objectValue = null;
    }

    private AnyValue(long value, AnyValueKind kind)
    {
        _kind = kind;
        _isNull = false;
        _intValue = value;
        _objectValue = null;
    }

    private AnyValue(ulong value, AnyValueKind kind)
    {
        _kind = kind;
        _isNull = false;
        _uintValue = value;
        _objectValue = null;
    }

    private AnyValue(double value, AnyValueKind kind)
    {
        _kind = kind;
        _isNull = false;
        _floatValue = value;
        _objectValue = null;
    }

    private AnyValue(Int128 value)
    {
        _kind = AnyValueKind.Int128;
        _isNull = false;
        _int128Value = value;
        _objectValue = null;
    }

    private AnyValue(UInt128 value)
    {
        _kind = AnyValueKind.UInt128;
        _isNull = false;
        _uint128Value = value;
        _objectValue = null;
    }

    private AnyValue(object? value, AnyValueKind kind)
    {
        _kind = kind;
        _isNull = value is null;
        _intValue = 0;
        _objectValue = value;
    }

    // ============================================================================
    // Static Constructors
    // ============================================================================

    public static AnyValue Null => new(AnyValueKind.Null, isNull: true);

    public static AnyValue From(sbyte value) => new(value, AnyValueKind.Int8);
    public static AnyValue From(short value) => new(value, AnyValueKind.Int16);
    public static AnyValue From(int value) => new(value, AnyValueKind.Int32);
    public static AnyValue From(long value) => new(value, AnyValueKind.Int64);
    public static AnyValue From(Int128 value) => new(value);

    public static AnyValue From(byte value) => new(value, AnyValueKind.UInt8);
    public static AnyValue From(ushort value) => new(value, AnyValueKind.UInt16);
    public static AnyValue From(uint value) => new(value, AnyValueKind.UInt32);
    public static AnyValue From(ulong value) => new(value, AnyValueKind.UInt64);
    public static AnyValue From(UInt128 value) => new(value);

    public static AnyValue From(float value) => new(value, AnyValueKind.Float32);
    public static AnyValue From(double value) => new(value, AnyValueKind.Float64);

    public static AnyValue From(bool value) => new(value ? 1L : 0L, AnyValueKind.Boolean);

    public static AnyValue From(string? value) => value is null
        ? Null
        : new(value, AnyValueKind.String);

    public static AnyValue From(ReadOnlyMemory<byte> value) => new(value, AnyValueKind.Binary);

    public static AnyValue From(DateOnly value) => new(value.DayNumber, AnyValueKind.Date);
    public static AnyValue From(TimeOnly value) => new(value.ToTimeSpan().Ticks * 100, AnyValueKind.Time); // Convert to nanos
    public static AnyValue From(DateTime value) => new(value.Ticks, AnyValueKind.DateTime);
    public static AnyValue From(DateTimeOffset value) => new(value.UtcTicks, AnyValueKind.DateTime);
    public static AnyValue From(TimeSpan value) => new(value.Ticks * 100, AnyValueKind.Duration); // Convert to nanos

    public static AnyValue From(decimal value, int precision = 38, int scale = 9) =>
        new((decimal.IsInteger(value) ? (Int128)value : (Int128)(value * (decimal)Math.Pow(10, scale))), AnyValueKind.Decimal);

    public static AnyValue FromList(IReadOnlyList<AnyValue> values) => new(values, AnyValueKind.List);
    public static AnyValue FromStruct(IReadOnlyDictionary<string, AnyValue> fields) => new(fields, AnyValueKind.Struct);

    // ============================================================================
    // Properties
    // ============================================================================

    public AnyValueKind Kind => _kind;
    public bool IsNull => _isNull || _kind == AnyValueKind.Null;

    // ============================================================================
    // Value Accessors
    // ============================================================================

    public sbyte AsInt8() => _kind == AnyValueKind.Int8 ? (sbyte)_intValue : throw InvalidCast(AnyValueKind.Int8);
    public short AsInt16() => _kind == AnyValueKind.Int16 ? (short)_intValue : throw InvalidCast(AnyValueKind.Int16);
    public int AsInt32() => _kind == AnyValueKind.Int32 ? (int)_intValue : throw InvalidCast(AnyValueKind.Int32);
    public long AsInt64() => _kind == AnyValueKind.Int64 ? _intValue : throw InvalidCast(AnyValueKind.Int64);
    public Int128 AsInt128() => _kind == AnyValueKind.Int128 ? _int128Value : throw InvalidCast(AnyValueKind.Int128);

    public byte AsUInt8() => _kind == AnyValueKind.UInt8 ? (byte)_uintValue : throw InvalidCast(AnyValueKind.UInt8);
    public ushort AsUInt16() => _kind == AnyValueKind.UInt16 ? (ushort)_uintValue : throw InvalidCast(AnyValueKind.UInt16);
    public uint AsUInt32() => _kind == AnyValueKind.UInt32 ? (uint)_uintValue : throw InvalidCast(AnyValueKind.UInt32);
    public ulong AsUInt64() => _kind == AnyValueKind.UInt64 ? _uintValue : throw InvalidCast(AnyValueKind.UInt64);
    public UInt128 AsUInt128() => _kind == AnyValueKind.UInt128 ? _uint128Value : throw InvalidCast(AnyValueKind.UInt128);

    public float AsFloat32() => _kind == AnyValueKind.Float32 ? (float)_floatValue : throw InvalidCast(AnyValueKind.Float32);
    public double AsFloat64() => _kind == AnyValueKind.Float64 ? _floatValue : throw InvalidCast(AnyValueKind.Float64);

    public bool AsBoolean() => _kind == AnyValueKind.Boolean ? _intValue != 0 : throw InvalidCast(AnyValueKind.Boolean);

    public string AsString() => _kind == AnyValueKind.String
        ? (_objectValue as string) ?? throw new InvalidOperationException("Null string")
        : throw InvalidCast(AnyValueKind.String);

    public ReadOnlyMemory<byte> AsBinary() => _kind == AnyValueKind.Binary
        ? (ReadOnlyMemory<byte>)_objectValue!
        : throw InvalidCast(AnyValueKind.Binary);

    public DateOnly AsDate() => _kind == AnyValueKind.Date
        ? DateOnly.FromDayNumber((int)_intValue)
        : throw InvalidCast(AnyValueKind.Date);

    public TimeOnly AsTime() => _kind == AnyValueKind.Time
        ? new TimeOnly(_intValue / 100) // Convert nanos to ticks
        : throw InvalidCast(AnyValueKind.Time);

    public DateTime AsDateTime() => _kind == AnyValueKind.DateTime
        ? new DateTime(_intValue, DateTimeKind.Utc)
        : throw InvalidCast(AnyValueKind.DateTime);

    public TimeSpan AsDuration() => _kind == AnyValueKind.Duration
        ? new TimeSpan(_intValue / 100) // Convert nanos to ticks
        : throw InvalidCast(AnyValueKind.Duration);

    public IReadOnlyList<AnyValue> AsList() => _kind == AnyValueKind.List
        ? (_objectValue as IReadOnlyList<AnyValue>) ?? throw new InvalidOperationException("Null list")
        : throw InvalidCast(AnyValueKind.List);

    public IReadOnlyDictionary<string, AnyValue> AsStruct() => _kind == AnyValueKind.Struct
        ? (_objectValue as IReadOnlyDictionary<string, AnyValue>) ?? throw new InvalidOperationException("Null struct")
        : throw InvalidCast(AnyValueKind.Struct);

    // ============================================================================
    // Safe Accessors (Try pattern)
    // ============================================================================

    public bool TryGetInt64(out long value)
    {
        if (_kind is AnyValueKind.Int8 or AnyValueKind.Int16 or AnyValueKind.Int32 or AnyValueKind.Int64)
        {
            value = _intValue;
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGetDouble(out double value)
    {
        if (_kind is AnyValueKind.Float32 or AnyValueKind.Float64)
        {
            value = _floatValue;
            return true;
        }
        if (_kind is AnyValueKind.Int8 or AnyValueKind.Int16 or AnyValueKind.Int32 or AnyValueKind.Int64)
        {
            value = _intValue;
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGetString([NotNullWhen(true)] out string? value)
    {
        if (_kind == AnyValueKind.String && _objectValue is string s)
        {
            value = s;
            return true;
        }
        value = null;
        return false;
    }

    // ============================================================================
    // Type Conversion
    // ============================================================================

    public AnyValue Cast(DataType targetType) => targetType switch
    {
        DataType.Int8Type => From((sbyte)ToInt64()),
        DataType.Int16Type => From((short)ToInt64()),
        DataType.Int32Type => From((int)ToInt64()),
        DataType.Int64Type => From(ToInt64()),
        DataType.UInt8Type => From((byte)ToUInt64()),
        DataType.UInt16Type => From((ushort)ToUInt64()),
        DataType.UInt32Type => From((uint)ToUInt64()),
        DataType.UInt64Type => From(ToUInt64()),
        DataType.Float32Type => From((float)ToDouble()),
        DataType.Float64Type => From(ToDouble()),
        DataType.BooleanType => From(ToBoolean()),
        DataType.StringType => From(ToString()),
        _ => throw new InvalidCastException($"Cannot cast {_kind} to {targetType}")
    };

    private long ToInt64() => _kind switch
    {
        AnyValueKind.Int8 or AnyValueKind.Int16 or AnyValueKind.Int32 or AnyValueKind.Int64 => _intValue,
        AnyValueKind.UInt8 or AnyValueKind.UInt16 or AnyValueKind.UInt32 or AnyValueKind.UInt64 => (long)_uintValue,
        AnyValueKind.Float32 or AnyValueKind.Float64 => (long)_floatValue,
        AnyValueKind.Boolean => _intValue,
        _ => throw new InvalidCastException($"Cannot convert {_kind} to Int64")
    };

    private ulong ToUInt64() => _kind switch
    {
        AnyValueKind.Int8 or AnyValueKind.Int16 or AnyValueKind.Int32 or AnyValueKind.Int64 => (ulong)_intValue,
        AnyValueKind.UInt8 or AnyValueKind.UInt16 or AnyValueKind.UInt32 or AnyValueKind.UInt64 => _uintValue,
        AnyValueKind.Float32 or AnyValueKind.Float64 => (ulong)_floatValue,
        AnyValueKind.Boolean => (ulong)_intValue,
        _ => throw new InvalidCastException($"Cannot convert {_kind} to UInt64")
    };

    private double ToDouble() => _kind switch
    {
        AnyValueKind.Int8 or AnyValueKind.Int16 or AnyValueKind.Int32 or AnyValueKind.Int64 => _intValue,
        AnyValueKind.UInt8 or AnyValueKind.UInt16 or AnyValueKind.UInt32 or AnyValueKind.UInt64 => _uintValue,
        AnyValueKind.Float32 or AnyValueKind.Float64 => _floatValue,
        _ => throw new InvalidCastException($"Cannot convert {_kind} to Double")
    };

    private bool ToBoolean() => _kind switch
    {
        AnyValueKind.Boolean => _intValue != 0,
        AnyValueKind.Int8 or AnyValueKind.Int16 or AnyValueKind.Int32 or AnyValueKind.Int64 => _intValue != 0,
        AnyValueKind.UInt8 or AnyValueKind.UInt16 or AnyValueKind.UInt32 or AnyValueKind.UInt64 => _uintValue != 0,
        AnyValueKind.Float32 or AnyValueKind.Float64 => _floatValue != 0,
        AnyValueKind.String => !string.IsNullOrEmpty(_objectValue as string),
        _ => throw new InvalidCastException($"Cannot convert {_kind} to Boolean")
    };

    // ============================================================================
    // Equality and Comparison
    // ============================================================================

    public bool Equals(AnyValue other)
    {
        if (_isNull && other._isNull) return true;
        if (_isNull || other._isNull) return false;
        if (_kind != other._kind) return false;

        return _kind switch
        {
            AnyValueKind.Int8 or AnyValueKind.Int16 or AnyValueKind.Int32 or AnyValueKind.Int64
                or AnyValueKind.Boolean or AnyValueKind.Date or AnyValueKind.Time
                or AnyValueKind.DateTime or AnyValueKind.Duration => _intValue == other._intValue,
            AnyValueKind.UInt8 or AnyValueKind.UInt16 or AnyValueKind.UInt32 or AnyValueKind.UInt64 => _uintValue == other._uintValue,
            AnyValueKind.Float32 or AnyValueKind.Float64 => _floatValue == other._floatValue || (double.IsNaN(_floatValue) && double.IsNaN(other._floatValue)),
            AnyValueKind.Int128 => _int128Value == other._int128Value,
            AnyValueKind.UInt128 => _uint128Value == other._uint128Value,
            AnyValueKind.String => string.Equals(_objectValue as string, other._objectValue as string, StringComparison.Ordinal),
            AnyValueKind.Null => true,
            _ => Equals(_objectValue, other._objectValue)
        };
    }

    public int CompareTo(AnyValue other)
    {
        // Nulls sort last
        if (_isNull && other._isNull) return 0;
        if (_isNull) return 1;
        if (other._isNull) return -1;

        // Different types - compare by kind ordinal
        if (_kind != other._kind) return _kind.CompareTo(other._kind);

        return _kind switch
        {
            AnyValueKind.Int8 or AnyValueKind.Int16 or AnyValueKind.Int32 or AnyValueKind.Int64
                or AnyValueKind.Date or AnyValueKind.Time or AnyValueKind.DateTime or AnyValueKind.Duration
                => _intValue.CompareTo(other._intValue),
            AnyValueKind.UInt8 or AnyValueKind.UInt16 or AnyValueKind.UInt32 or AnyValueKind.UInt64 => _uintValue.CompareTo(other._uintValue),
            AnyValueKind.Float32 or AnyValueKind.Float64 => CompareFloats(_floatValue, other._floatValue),
            AnyValueKind.Int128 => _int128Value.CompareTo(other._int128Value),
            AnyValueKind.UInt128 => _uint128Value.CompareTo(other._uint128Value),
            AnyValueKind.Boolean => _intValue.CompareTo(other._intValue),
            AnyValueKind.String => string.Compare(_objectValue as string, other._objectValue as string, StringComparison.Ordinal),
            _ => 0
        };
    }

    private static int CompareFloats(double a, double b)
    {
        // Polars semantics: NaN > all non-NaN values, NaN == NaN
        if (double.IsNaN(a) && double.IsNaN(b)) return 0;
        if (double.IsNaN(a)) return 1;
        if (double.IsNaN(b)) return -1;
        return a.CompareTo(b);
    }

    public override bool Equals(object? obj) => obj is AnyValue other && Equals(other);

    public override int GetHashCode()
    {
        if (_isNull) return 0;
        return _kind switch
        {
            AnyValueKind.Int8 or AnyValueKind.Int16 or AnyValueKind.Int32 or AnyValueKind.Int64
                or AnyValueKind.Boolean or AnyValueKind.Date or AnyValueKind.Time
                or AnyValueKind.DateTime or AnyValueKind.Duration => HashCode.Combine(_kind, _intValue),
            AnyValueKind.UInt8 or AnyValueKind.UInt16 or AnyValueKind.UInt32 or AnyValueKind.UInt64 => HashCode.Combine(_kind, _uintValue),
            AnyValueKind.Float32 or AnyValueKind.Float64 => double.IsNaN(_floatValue) ? HashCode.Combine(_kind, 0) : HashCode.Combine(_kind, _floatValue),
            AnyValueKind.Int128 => HashCode.Combine(_kind, _int128Value),
            AnyValueKind.UInt128 => HashCode.Combine(_kind, _uint128Value),
            AnyValueKind.String => HashCode.Combine(_kind, _objectValue?.GetHashCode() ?? 0),
            _ => HashCode.Combine(_kind, _objectValue?.GetHashCode() ?? 0)
        };
    }

    public override string ToString()
    {
        if (_isNull) return "null";
        return _kind switch
        {
            AnyValueKind.Int8 or AnyValueKind.Int16 or AnyValueKind.Int32 or AnyValueKind.Int64 => _intValue.ToString(),
            AnyValueKind.UInt8 or AnyValueKind.UInt16 or AnyValueKind.UInt32 or AnyValueKind.UInt64 => _uintValue.ToString(),
            AnyValueKind.Float32 => ((float)_floatValue).ToString("G"),
            AnyValueKind.Float64 => _floatValue.ToString("G"),
            AnyValueKind.Boolean => _intValue != 0 ? "true" : "false",
            AnyValueKind.String => $"\"{_objectValue}\"",
            AnyValueKind.Date => AsDate().ToString("yyyy-MM-dd"),
            AnyValueKind.Time => AsTime().ToString("HH:mm:ss.fffffff"),
            AnyValueKind.DateTime => AsDateTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"),
            AnyValueKind.Duration => AsDuration().ToString(),
            AnyValueKind.Int128 => _int128Value.ToString(),
            AnyValueKind.UInt128 => _uint128Value.ToString(),
            AnyValueKind.List => $"[{string.Join(", ", AsList().Take(5))}{(AsList().Count > 5 ? ", ..." : "")}]",
            AnyValueKind.Struct => $"{{{string.Join(", ", AsStruct().Take(3).Select(kv => $"{kv.Key}: {kv.Value}"))}}}",
            AnyValueKind.Null => "null",
            _ => _objectValue?.ToString() ?? "null"
        };
    }

    // ============================================================================
    // Operators
    // ============================================================================

    public static bool operator ==(AnyValue left, AnyValue right) => left.Equals(right);
    public static bool operator !=(AnyValue left, AnyValue right) => !left.Equals(right);
    public static bool operator <(AnyValue left, AnyValue right) => left.CompareTo(right) < 0;
    public static bool operator >(AnyValue left, AnyValue right) => left.CompareTo(right) > 0;
    public static bool operator <=(AnyValue left, AnyValue right) => left.CompareTo(right) <= 0;
    public static bool operator >=(AnyValue left, AnyValue right) => left.CompareTo(right) >= 0;

    // Implicit conversions for convenience
    public static implicit operator AnyValue(sbyte value) => From(value);
    public static implicit operator AnyValue(short value) => From(value);
    public static implicit operator AnyValue(int value) => From(value);
    public static implicit operator AnyValue(long value) => From(value);
    public static implicit operator AnyValue(byte value) => From(value);
    public static implicit operator AnyValue(ushort value) => From(value);
    public static implicit operator AnyValue(uint value) => From(value);
    public static implicit operator AnyValue(ulong value) => From(value);
    public static implicit operator AnyValue(float value) => From(value);
    public static implicit operator AnyValue(double value) => From(value);
    public static implicit operator AnyValue(bool value) => From(value);
    public static implicit operator AnyValue(string? value) => From(value);
    public static implicit operator AnyValue(DateOnly value) => From(value);
    public static implicit operator AnyValue(TimeOnly value) => From(value);
    public static implicit operator AnyValue(DateTime value) => From(value);
    public static implicit operator AnyValue(TimeSpan value) => From(value);

    private static InvalidCastException InvalidCast(AnyValueKind expected) =>
        new($"Expected {expected} but got {expected}");
}

/// <summary>
/// Discriminator for AnyValue union type.
/// </summary>
public enum AnyValueKind : byte
{
    Null = 0,
    Int8,
    Int16,
    Int32,
    Int64,
    Int128,
    UInt8,
    UInt16,
    UInt32,
    UInt64,
    UInt128,
    Float32,
    Float64,
    Decimal,
    Boolean,
    String,
    Binary,
    Date,
    Time,
    DateTime,
    Duration,
    List,
    Array,
    Struct,
    Categorical,
    Enum,
    Object
}
