// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using System.Linq.Expressions;
using Polaire.DataTypes;
using Polaire.Series;

namespace Polaire.Expressions;

/// <summary>
/// Represents an expression in the Polaire query language.
/// Expressions form a tree structure that can be optimized and compiled to efficient code.
/// </summary>
public abstract record Expr
{
    private Expr() { } // Sealed hierarchy

    // ============================================================================
    // Expression Types
    // ============================================================================

    /// <summary>Reference to a column by name.</summary>
    public sealed record Column(string Name) : Expr
    {
        public override string ToString() => $"col(\"{Name}\")";
    }

    /// <summary>Literal/constant value.</summary>
    public sealed record Literal(AnyValue Value, DataType? ExplicitType = null) : Expr
    {
        public override string ToString() => Value.ToString();
    }

    /// <summary>Alias/rename expression.</summary>
    public sealed record Alias(Expr Inner, string Name) : Expr
    {
        public override string ToString() => $"{Inner}.alias(\"{Name}\")";
    }

    /// <summary>Binary operation (add, sub, mul, div, etc.).</summary>
    public sealed record BinaryOp(Expr Left, BinaryOperator Op, Expr Right) : Expr
    {
        public override string ToString() => $"({Left} {Op.ToSymbol()} {Right})";
    }

    /// <summary>Unary operation (negate, not, etc.).</summary>
    public sealed record UnaryOp(UnaryOperator Op, Expr Inner) : Expr
    {
        public override string ToString() => $"{Op.ToSymbol()}{Inner}";
    }

    /// <summary>Function call.</summary>
    public sealed record Function(string Name, IReadOnlyList<Expr> Args) : Expr
    {
        public override string ToString() => $"{Name}({string.Join(", ", Args)})";
    }

    /// <summary>Aggregation expression.</summary>
    public sealed record Agg(AggregationType Type, Expr Inner) : Expr
    {
        public override string ToString() => $"{Inner}.{Type.ToString().ToLower()}()";
    }

    /// <summary>Cast expression.</summary>
    public sealed record Cast(Expr Inner, DataType TargetType) : Expr
    {
        public override string ToString() => $"{Inner}.cast({TargetType})";
    }

    /// <summary>Sort expression.</summary>
    public sealed record Sort(Expr Inner, bool Descending = false, bool NullsLast = true) : Expr
    {
        public override string ToString() => $"{Inner}.sort({(Descending ? "desc" : "asc")})";
    }

    /// <summary>Filter expression (when/then/otherwise).</summary>
    public sealed record When(Expr Condition, Expr ThenExpr, Expr? OtherwiseExpr = null) : Expr
    {
        public override string ToString() =>
            OtherwiseExpr is null
                ? $"when({Condition}).then({ThenExpr})"
                : $"when({Condition}).then({ThenExpr}).otherwise({OtherwiseExpr})";
    }

    /// <summary>Window function expression.</summary>
    public sealed record Window(Expr Inner, IReadOnlyList<Expr> PartitionBy, IReadOnlyList<SortExpr>? OrderBy = null) : Expr
    {
        public override string ToString() =>
            $"{Inner}.over([{string.Join(", ", PartitionBy)}])";
    }

    /// <summary>Slice expression.</summary>
    public sealed record Slice(Expr Inner, int Offset, int? Length) : Expr
    {
        public override string ToString() =>
            Length.HasValue
                ? $"{Inner}.slice({Offset}, {Length})"
                : $"{Inner}.slice({Offset})";
    }

    /// <summary>Take expression.</summary>
    public sealed record Take(Expr Inner, Expr Indices) : Expr
    {
        public override string ToString() => $"{Inner}.take({Indices})";
    }

    /// <summary>Fill null expression.</summary>
    public sealed record FillNull(Expr Inner, Expr Fill) : Expr
    {
        public override string ToString() => $"{Inner}.fill_null({Fill})";
    }

    /// <summary>Struct field access.</summary>
    public sealed record StructField(Expr Inner, string FieldName) : Expr
    {
        public override string ToString() => $"{Inner}.struct.field(\"{FieldName}\")";
    }

    /// <summary>List element access.</summary>
    public sealed record ListGet(Expr Inner, int Index) : Expr
    {
        public override string ToString() => $"{Inner}.list.get({Index})";
    }

    /// <summary>String expression (for .str namespace access).</summary>
    public sealed record Str(Expr Inner, StringOp Operation) : Expr
    {
        public override string ToString() => $"{Inner}.str.{Operation}";
    }

    /// <summary>DateTime expression (for .dt namespace access).</summary>
    public sealed record Dt(Expr Inner, DateTimeOp Operation) : Expr
    {
        public override string ToString() => $"{Inner}.dt.{Operation}";
    }

    /// <summary>Is null check.</summary>
    public sealed record IsNull(Expr Inner) : Expr
    {
        public override string ToString() => $"{Inner}.is_null()";
    }

    /// <summary>Is not null check.</summary>
    public sealed record IsNotNull(Expr Inner) : Expr
    {
        public override string ToString() => $"{Inner}.is_not_null()";
    }

    /// <summary>Is in set check.</summary>
    public sealed record IsIn(Expr Inner, IReadOnlyList<AnyValue> Values) : Expr
    {
        public override string ToString() => $"{Inner}.is_in([{string.Join(", ", Values.Take(5))}{(Values.Count > 5 ? ", ..." : "")}])";
    }

    /// <summary>Between check.</summary>
    public sealed record Between(Expr Inner, Expr Lower, Expr Upper, bool Inclusive = true) : Expr
    {
        public override string ToString() => $"{Inner}.is_between({Lower}, {Upper})";
    }

    /// <summary>Ternary if-then-else.</summary>
    public sealed record IfThenElse(Expr Condition, Expr Then, Expr Else) : Expr
    {
        public override string ToString() => $"if({Condition}, {Then}, {Else})";
    }

    // ============================================================================
    // Static Factory Methods (Polars-like API)
    // ============================================================================

    /// <summary>Creates a column reference.</summary>
    public static Expr Col(string name) => new Column(name);

    /// <summary>Creates multiple column references.</summary>
    public static Expr[] Cols(params string[] names) => names.Select(Col).ToArray();

    /// <summary>Selects all columns.</summary>
    public static Expr All() => new Function("all", Array.Empty<Expr>());

    /// <summary>Excludes columns by name.</summary>
    public static Expr Exclude(params string[] columns) => new Function("exclude", columns.Select(c => (Expr)new Literal(AnyValue.From(c))).ToArray());

    /// <summary>Creates a literal value.</summary>
    public static Expr Lit(object? value) => value switch
    {
        null => new Literal(AnyValue.Null),
        int i => new Literal(AnyValue.From(i)),
        long l => new Literal(AnyValue.From(l)),
        float f => new Literal(AnyValue.From(f)),
        double d => new Literal(AnyValue.From(d)),
        bool b => new Literal(AnyValue.From(b)),
        string s => new Literal(AnyValue.From(s)),
        DateTime dt => new Literal(AnyValue.From(dt)),
        AnyValue av => new Literal(av),
        _ => new Literal(AnyValue.From(value.ToString()))
    };

    /// <summary>Creates a when expression (conditional).</summary>
    public static WhenBuilder WhenExpr(Expr condition) => new(condition);

    /// <summary>Counts rows.</summary>
    public static Expr Count() => new Function("count", Array.Empty<Expr>());

    /// <summary>Row number expression.</summary>
    public static Expr RowNumber() => new Function("row_number", Array.Empty<Expr>());

    /// <summary>Creates a range expression.</summary>
    public static Expr Arange(int start, int end, int step = 1) =>
        new Function("arange", new Expr[] { Lit(start), Lit(end), Lit(step) });

    /// <summary>Concatenates string columns.</summary>
    public static Expr Concat(params Expr[] exprs) => new Function("concat", exprs);

    /// <summary>Concatenates string columns with separator.</summary>
    public static Expr ConcatStr(Expr separator, params Expr[] exprs) =>
        new Function("concat_str", new[] { separator }.Concat(exprs).ToArray());

    // ============================================================================
    // Fluent Methods
    // ============================================================================

    /// <summary>Renames the expression output.</summary>
    public Expr As(string name) => new Alias(this, name);

    /// <summary>Casts to a different type.</summary>
    public Expr CastTo(DataType type) => new Cast(this, type);

    /// <summary>Sorts the expression.</summary>
    public Expr SortBy(bool descending = false) => new Sort(this, descending);

    /// <summary>Fills null values.</summary>
    public Expr FillNullWith(Expr fill) => new FillNull(this, fill);
    public Expr FillNullWith(object? value) => FillNullWith(Lit(value));

    /// <summary>Window function over partitions.</summary>
    public Expr Over(params Expr[] partitionBy) => new Window(this, partitionBy);

    /// <summary>Filters with between.</summary>
    public Expr IsBetween(Expr lower, Expr upper, bool inclusive = true) => new Between(this, lower, upper, inclusive);
    public Expr IsBetween(object? lower, object? upper, bool inclusive = true) => IsBetween(Lit(lower), Lit(upper), inclusive);

    /// <summary>Checks if in set.</summary>
    public Expr IsInSet(params object?[] values) =>
        new IsIn(this, values.Select(v => v is AnyValue av ? av : Lit(v) is Literal lit ? lit.Value : AnyValue.Null).ToArray());

    // ============================================================================
    // Aggregation Methods
    // ============================================================================

    public Expr Sum() => new Agg(AggregationType.Sum, this);
    public Expr Mean() => new Agg(AggregationType.Mean, this);
    public Expr Median() => new Agg(AggregationType.Median, this);
    public Expr Min() => new Agg(AggregationType.Min, this);
    public Expr Max() => new Agg(AggregationType.Max, this);
    public Expr Std() => new Agg(AggregationType.Std, this);
    public Expr Var() => new Agg(AggregationType.Var, this);
    public Expr CountExpr() => new Agg(AggregationType.Count, this);
    public Expr First() => new Agg(AggregationType.First, this);
    public Expr Last() => new Agg(AggregationType.Last, this);
    public Expr NUnique() => new Agg(AggregationType.NUnique, this);
    public Expr ArgMin() => new Agg(AggregationType.ArgMin, this);
    public Expr ArgMax() => new Agg(AggregationType.ArgMax, this);

    // ============================================================================
    // Arithmetic Operators
    // ============================================================================

    public static Expr operator +(Expr left, Expr right) => new BinaryOp(left, BinaryOperator.Add, right);
    public static Expr operator -(Expr left, Expr right) => new BinaryOp(left, BinaryOperator.Subtract, right);
    public static Expr operator *(Expr left, Expr right) => new BinaryOp(left, BinaryOperator.Multiply, right);
    public static Expr operator /(Expr left, Expr right) => new BinaryOp(left, BinaryOperator.Divide, right);
    public static Expr operator %(Expr left, Expr right) => new BinaryOp(left, BinaryOperator.Modulo, right);
    public static Expr operator -(Expr expr) => new UnaryOp(UnaryOperator.Negate, expr);

    // With scalars
    public static Expr operator +(Expr left, double right) => new BinaryOp(left, BinaryOperator.Add, Lit(right));
    public static Expr operator -(Expr left, double right) => new BinaryOp(left, BinaryOperator.Subtract, Lit(right));
    public static Expr operator *(Expr left, double right) => new BinaryOp(left, BinaryOperator.Multiply, Lit(right));
    public static Expr operator /(Expr left, double right) => new BinaryOp(left, BinaryOperator.Divide, Lit(right));

    public static Expr operator +(double left, Expr right) => new BinaryOp(Lit(left), BinaryOperator.Add, right);
    public static Expr operator -(double left, Expr right) => new BinaryOp(Lit(left), BinaryOperator.Subtract, right);
    public static Expr operator *(double left, Expr right) => new BinaryOp(Lit(left), BinaryOperator.Multiply, right);
    public static Expr operator /(double left, Expr right) => new BinaryOp(Lit(left), BinaryOperator.Divide, right);

    // ============================================================================
    // Comparison Operators
    // ============================================================================

    public Expr Eq(Expr other) => new BinaryOp(this, BinaryOperator.Equal, other);
    public Expr Ne(Expr other) => new BinaryOp(this, BinaryOperator.NotEqual, other);
    public Expr Lt(Expr other) => new BinaryOp(this, BinaryOperator.LessThan, other);
    public Expr Le(Expr other) => new BinaryOp(this, BinaryOperator.LessEqual, other);
    public Expr Gt(Expr other) => new BinaryOp(this, BinaryOperator.GreaterThan, other);
    public Expr Ge(Expr other) => new BinaryOp(this, BinaryOperator.GreaterEqual, other);

    public Expr Eq(object? value) => Eq(Lit(value));
    public Expr Ne(object? value) => Ne(Lit(value));
    public Expr Lt(object? value) => Lt(Lit(value));
    public Expr Le(object? value) => Le(Lit(value));
    public Expr Gt(object? value) => Gt(Lit(value));
    public Expr Ge(object? value) => Ge(Lit(value));

    // ============================================================================
    // Boolean Operators
    // ============================================================================

    public static Expr operator &(Expr left, Expr right) => new BinaryOp(left, BinaryOperator.And, right);
    public static Expr operator |(Expr left, Expr right) => new BinaryOp(left, BinaryOperator.Or, right);
    public static Expr operator ^(Expr left, Expr right) => new BinaryOp(left, BinaryOperator.Xor, right);
    public static Expr operator !(Expr expr) => new UnaryOp(UnaryOperator.Not, expr);

    public Expr And(Expr other) => this & other;
    public Expr Or(Expr other) => this | other;
    public Expr Not() => !this;
    public Expr Xor(Expr other) => this ^ other;

    // ============================================================================
    // Null Checks
    // ============================================================================

    public Expr IsNullExpr() => new IsNull(this);
    public Expr IsNotNullExpr() => new IsNotNull(this);

    // ============================================================================
    // String Access
    // ============================================================================

    public StringExprBuilder StrExpr => new(this);

    // ============================================================================
    // DateTime Access
    // ============================================================================

    public DateTimeExprBuilder DtExpr => new(this);
}

/// <summary>Builder for when-then-otherwise expressions.</summary>
public class WhenBuilder
{
    private readonly Expr _condition;

    public WhenBuilder(Expr condition)
    {
        _condition = condition;
    }

    public ThenBuilder Then(Expr value) => new(_condition, value);
    public ThenBuilder Then(object? value) => Then(Expr.Lit(value));
}

/// <summary>Builder for then-otherwise expressions.</summary>
public class ThenBuilder
{
    private readonly Expr _condition;
    private readonly Expr _then;

    public ThenBuilder(Expr condition, Expr then)
    {
        _condition = condition;
        _then = then;
    }

    public Expr Otherwise(Expr value) => new Expr.When(_condition, _then, value);
    public Expr Otherwise(object? value) => Otherwise(Expr.Lit(value));

    /// <summary>Implicit conversion to Expr (defaults otherwise to null).</summary>
    public static implicit operator Expr(ThenBuilder builder) => new Expr.When(builder._condition, builder._then, null);
}

/// <summary>Builder for string expressions.</summary>
public class StringExprBuilder
{
    private readonly Expr _inner;

    public StringExprBuilder(Expr inner)
    {
        _inner = inner;
    }

    public Expr ToLowerCase() => new Expr.Str(_inner, new StringOp.ToLowerCase());
    public Expr ToUpperCase() => new Expr.Str(_inner, new StringOp.ToUpperCase());
    public Expr Strip() => new Expr.Str(_inner, new StringOp.Strip());
    public Expr Contains(string pattern, bool literal = true) => new Expr.Str(_inner, new StringOp.Contains(pattern, literal));
    public Expr StartsWith(string prefix) => new Expr.Str(_inner, new StringOp.StartsWith(prefix));
    public Expr EndsWith(string suffix) => new Expr.Str(_inner, new StringOp.EndsWith(suffix));
    public Expr Replace(string pattern, string replacement) => new Expr.Str(_inner, new StringOp.Replace(pattern, replacement));
    public Expr Lengths() => new Expr.Str(_inner, new StringOp.Lengths());
    public Expr Substring(int start, int? length = null) => new Expr.Str(_inner, new StringOp.Substring(start, length));
    public Expr Extract(string pattern, int group = 0) => new Expr.Str(_inner, new StringOp.Extract(pattern, group));
}

/// <summary>Builder for datetime expressions.</summary>
public class DateTimeExprBuilder
{
    private readonly Expr _inner;

    public DateTimeExprBuilder(Expr inner)
    {
        _inner = inner;
    }

    public Expr Year() => new Expr.Dt(_inner, new DateTimeOp.Year());
    public Expr Month() => new Expr.Dt(_inner, new DateTimeOp.Month());
    public Expr Day() => new Expr.Dt(_inner, new DateTimeOp.Day());
    public Expr Hour() => new Expr.Dt(_inner, new DateTimeOp.Hour());
    public Expr Minute() => new Expr.Dt(_inner, new DateTimeOp.Minute());
    public Expr Second() => new Expr.Dt(_inner, new DateTimeOp.Second());
    public Expr DayOfWeek() => new Expr.Dt(_inner, new DateTimeOp.DayOfWeek());
    public Expr DayOfYear() => new Expr.Dt(_inner, new DateTimeOp.DayOfYear());
    public Expr WeekOfYear() => new Expr.Dt(_inner, new DateTimeOp.WeekOfYear());
    public Expr Quarter() => new Expr.Dt(_inner, new DateTimeOp.Quarter());
}

/// <summary>Sort expression with direction.</summary>
public record SortExpr(Expr Inner, bool Descending = false);

/// <summary>Binary operators.</summary>
public enum BinaryOperator
{
    Add, Subtract, Multiply, Divide, Modulo, FloorDivide, Power,
    Equal, NotEqual, LessThan, LessEqual, GreaterThan, GreaterEqual,
    And, Or, Xor
}

public static class BinaryOperatorExtensions
{
    public static string ToSymbol(this BinaryOperator op) => op switch
    {
        BinaryOperator.Add => "+",
        BinaryOperator.Subtract => "-",
        BinaryOperator.Multiply => "*",
        BinaryOperator.Divide => "/",
        BinaryOperator.Modulo => "%",
        BinaryOperator.FloorDivide => "//",
        BinaryOperator.Power => "**",
        BinaryOperator.Equal => "==",
        BinaryOperator.NotEqual => "!=",
        BinaryOperator.LessThan => "<",
        BinaryOperator.LessEqual => "<=",
        BinaryOperator.GreaterThan => ">",
        BinaryOperator.GreaterEqual => ">=",
        BinaryOperator.And => "&",
        BinaryOperator.Or => "|",
        BinaryOperator.Xor => "^",
        _ => op.ToString()
    };
}

/// <summary>Unary operators.</summary>
public enum UnaryOperator
{
    Negate, Not, Abs, Sqrt, Log, Exp, Sin, Cos, Tan
}

public static class UnaryOperatorExtensions
{
    public static string ToSymbol(this UnaryOperator op) => op switch
    {
        UnaryOperator.Negate => "-",
        UnaryOperator.Not => "!",
        _ => op.ToString().ToLower() + "()"
    };
}

/// <summary>Aggregation types.</summary>
public enum AggregationType
{
    Sum, Mean, Median, Min, Max, Std, Var, Count, First, Last, NUnique, ArgMin, ArgMax
}

/// <summary>String operations.</summary>
public abstract record StringOp
{
    public sealed record ToLowerCase : StringOp { public override string ToString() => "to_lowercase()"; }
    public sealed record ToUpperCase : StringOp { public override string ToString() => "to_uppercase()"; }
    public sealed record Strip : StringOp { public override string ToString() => "strip()"; }
    public sealed record Contains(string Pattern, bool Literal) : StringOp { public override string ToString() => $"contains(\"{Pattern}\")"; }
    public sealed record StartsWith(string Prefix) : StringOp { public override string ToString() => $"starts_with(\"{Prefix}\")"; }
    public sealed record EndsWith(string Suffix) : StringOp { public override string ToString() => $"ends_with(\"{Suffix}\")"; }
    public sealed record Replace(string Pattern, string Replacement) : StringOp { public override string ToString() => $"replace(\"{Pattern}\", \"{Replacement}\")"; }
    public sealed record Lengths : StringOp { public override string ToString() => "lengths()"; }
    public sealed record Substring(int Start, int? Length) : StringOp { public override string ToString() => $"slice({Start}, {Length})"; }
    public sealed record Extract(string Pattern, int Group) : StringOp { public override string ToString() => $"extract(\"{Pattern}\", {Group})"; }
}

/// <summary>DateTime operations.</summary>
public abstract record DateTimeOp
{
    public sealed record Year : DateTimeOp { public override string ToString() => "year()"; }
    public sealed record Month : DateTimeOp { public override string ToString() => "month()"; }
    public sealed record Day : DateTimeOp { public override string ToString() => "day()"; }
    public sealed record Hour : DateTimeOp { public override string ToString() => "hour()"; }
    public sealed record Minute : DateTimeOp { public override string ToString() => "minute()"; }
    public sealed record Second : DateTimeOp { public override string ToString() => "second()"; }
    public sealed record DayOfWeek : DateTimeOp { public override string ToString() => "weekday()"; }
    public sealed record DayOfYear : DateTimeOp { public override string ToString() => "ordinal_day()"; }
    public sealed record WeekOfYear : DateTimeOp { public override string ToString() => "week()"; }
    public sealed record Quarter : DateTimeOp { public override string ToString() => "quarter()"; }
}
