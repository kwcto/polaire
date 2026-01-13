// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using Polaire.DataTypes;
using Polaire.Series;
using Polaire.Compute;
using Polaire.DataFrame;

namespace Polaire.Expressions;

/// <summary>
/// Evaluates expressions against a DataFrame context.
/// </summary>
public static class ExprEvaluator
{
    /// <summary>Evaluates an expression against a DataFrame.</summary>
    public static Series.Series Evaluate(Expr expr, DataFrame.DataFrame df)
    {
        return expr switch
        {
            Expr.Column col => EvaluateColumn(col, df),
            Expr.Literal lit => EvaluateLiteral(lit, df.Height),
            Expr.Alias alias => EvaluateAlias(alias, df),
            Expr.BinaryOp binop => EvaluateBinaryOp(binop, df),
            Expr.UnaryOp unop => EvaluateUnaryOp(unop, df),
            Expr.Agg agg => EvaluateAgg(agg, df),
            Expr.Cast cast => EvaluateCast(cast, df),
            Expr.IsNull isNull => EvaluateIsNull(isNull, df),
            Expr.IsNotNull isNotNull => EvaluateIsNotNull(isNotNull, df),
            Expr.FillNull fillNull => EvaluateFillNull(fillNull, df),
            Expr.When when => EvaluateWhen(when, df),
            Expr.Function func => EvaluateFunction(func, df),
            Expr.Sort sort => EvaluateSort(sort, df),
            Expr.Str str => EvaluateStr(str, df),
            Expr.Dt dt => EvaluateDt(dt, df),
            Expr.IsIn isIn => EvaluateIsIn(isIn, df),
            Expr.Between between => EvaluateBetween(between, df),
            _ => throw new NotSupportedException($"Expression type not supported: {expr.GetType().Name}")
        };
    }

    /// <summary>Evaluates multiple expressions.</summary>
    public static Series.Series[] Evaluate(IEnumerable<Expr> exprs, DataFrame.DataFrame df)
    {
        return exprs.Select(e => Evaluate(e, df)).ToArray();
    }

    // ============================================================================
    // Column
    // ============================================================================

    private static Series.Series EvaluateColumn(Expr.Column col, DataFrame.DataFrame df)
    {
        if (!df.Columns.Contains(col.Name))
            throw new KeyNotFoundException($"Column '{col.Name}' not found in DataFrame");
        return df[col.Name];
    }

    // ============================================================================
    // Literal
    // ============================================================================

    private static Series.Series EvaluateLiteral(Expr.Literal lit, int length)
    {
        return lit.Value.Kind switch
        {
            AnyValueKind.Null => CreateNullSeries("literal", length, lit.ExplicitType ?? DataType.Null),
            AnyValueKind.Int32 => Series.Series.FromValues("literal", Enumerable.Repeat(lit.Value.AsInt32(), length).ToArray()),
            AnyValueKind.Int64 => Series.Series.FromValues("literal", Enumerable.Repeat(lit.Value.AsInt64(), length).ToArray()),
            AnyValueKind.Float32 => Series.Series.FromValues("literal", Enumerable.Repeat(lit.Value.AsFloat32(), length).ToArray()),
            AnyValueKind.Float64 => Series.Series.FromValues("literal", Enumerable.Repeat(lit.Value.AsFloat64(), length).ToArray()),
            AnyValueKind.Boolean => Series.Series.FromValues("literal", Enumerable.Repeat(lit.Value.AsBoolean(), length).ToArray()),
            AnyValueKind.String => Series.Series.FromValues("literal", Enumerable.Repeat(lit.Value.AsString(), length).ToArray()),
            _ => Series.Series.FromValues("literal", Enumerable.Repeat(lit.Value.ToString(), length).ToArray())
        };
    }

    private static Series.Series CreateNullSeries(string name, int length, DataType type)
    {
        return type switch
        {
            DataType.Int32Type => Series.Series.FromNullable<int>(name, new int?[length]),
            DataType.Int64Type => Series.Series.FromNullable<long>(name, new long?[length]),
            DataType.Float64Type => Series.Series.FromNullable<double>(name, new double?[length]),
            DataType.BooleanType => Series.Series.FromNullable<bool>(name, new bool?[length]),
            _ => Series.Series.FromValues(name, new string?[length])
        };
    }

    // ============================================================================
    // Alias
    // ============================================================================

    private static Series.Series EvaluateAlias(Expr.Alias alias, DataFrame.DataFrame df)
    {
        var inner = Evaluate(alias.Inner, df);
        return inner.Rename(alias.Name);
    }

    // ============================================================================
    // Binary Operations
    // ============================================================================

    private static Series.Series EvaluateBinaryOp(Expr.BinaryOp binop, DataFrame.DataFrame df)
    {
        var left = Evaluate(binop.Left, df);
        var right = Evaluate(binop.Right, df);

        return binop.Op switch
        {
            // Arithmetic
            BinaryOperator.Add => left + right,
            BinaryOperator.Subtract => left - right,
            BinaryOperator.Multiply => left * right,
            BinaryOperator.Divide => left / right,
            BinaryOperator.Modulo => left % right,

            // Comparison
            BinaryOperator.Equal => left.Eq(right),
            BinaryOperator.NotEqual => left.Ne(right),
            BinaryOperator.LessThan => left.Lt(right),
            BinaryOperator.LessEqual => left.Le(right),
            BinaryOperator.GreaterThan => left.Gt(right),
            BinaryOperator.GreaterEqual => left.Ge(right),

            // Boolean
            BinaryOperator.And => left.And(right),
            BinaryOperator.Or => left.Or(right),
            BinaryOperator.Xor => left.Xor(right),

            _ => throw new NotSupportedException($"Binary operator not supported: {binop.Op}")
        };
    }

    // ============================================================================
    // Unary Operations
    // ============================================================================

    private static Series.Series EvaluateUnaryOp(Expr.UnaryOp unop, DataFrame.DataFrame df)
    {
        var inner = Evaluate(unop.Inner, df);

        return unop.Op switch
        {
            UnaryOperator.Negate => -inner,
            UnaryOperator.Not => inner.Not(),
            _ => throw new NotSupportedException($"Unary operator not supported: {unop.Op}")
        };
    }

    // ============================================================================
    // Aggregations
    // ============================================================================

    private static Series.Series EvaluateAgg(Expr.Agg agg, DataFrame.DataFrame df)
    {
        var inner = Evaluate(agg.Inner, df);

        var result = agg.Type switch
        {
            AggregationType.Sum => inner.Sum(),
            AggregationType.Mean => inner.Mean(),
            AggregationType.Median => inner.Median(),
            AggregationType.Min => inner.Min(),
            AggregationType.Max => inner.Max(),
            AggregationType.Std => inner.Std(),
            AggregationType.Var => inner.Var(),
            AggregationType.Count => AnyValue.From(inner.Count()),
            AggregationType.First => inner.First(),
            AggregationType.Last => inner.Last(),
            AggregationType.NUnique => AnyValue.From(inner.Unique().Length),
            _ => throw new NotSupportedException($"Aggregation type not supported: {agg.Type}")
        };

        // For aggregations, return a single-element series
        return CreateSeriesFromAnyValue(inner.Name, result);
    }

    private static Series.Series CreateSeriesFromAnyValue(string name, AnyValue value)
    {
        return value.Kind switch
        {
            AnyValueKind.Int32 => Series.Series.FromValues(name, new[] { value.AsInt32() }),
            AnyValueKind.Int64 => Series.Series.FromValues(name, new[] { value.AsInt64() }),
            AnyValueKind.Float32 => Series.Series.FromValues(name, new[] { value.AsFloat32() }),
            AnyValueKind.Float64 or AnyValueKind.Null when value.TryGetDouble(out var d) =>
                Series.Series.FromValues(name, new[] { d }),
            AnyValueKind.Boolean => Series.Series.FromValues(name, new[] { value.AsBoolean() }),
            AnyValueKind.String => Series.Series.FromValues(name, new[] { value.AsString() }),
            AnyValueKind.Null => Series.Series.FromNullable<double>(name, new double?[] { null }),
            _ => Series.Series.FromValues(name, new[] { value.ToString() })
        };
    }

    // ============================================================================
    // Cast
    // ============================================================================

    private static Series.Series EvaluateCast(Expr.Cast cast, DataFrame.DataFrame df)
    {
        var inner = Evaluate(cast.Inner, df);
        return inner.Cast(cast.TargetType);
    }

    // ============================================================================
    // Null Checks
    // ============================================================================

    private static Series.Series EvaluateIsNull(Expr.IsNull isNull, DataFrame.DataFrame df)
    {
        var inner = Evaluate(isNull.Inner, df);
        return inner.IsNull();
    }

    private static Series.Series EvaluateIsNotNull(Expr.IsNotNull isNotNull, DataFrame.DataFrame df)
    {
        var inner = Evaluate(isNotNull.Inner, df);
        return inner.IsNotNull();
    }

    // ============================================================================
    // FillNull
    // ============================================================================

    private static Series.Series EvaluateFillNull(Expr.FillNull fillNull, DataFrame.DataFrame df)
    {
        var inner = Evaluate(fillNull.Inner, df);
        var fill = Evaluate(fillNull.Fill, df);

        // If fill is literal (single value), use FillNull
        if (fill.Length == df.Height && !fill.HasNulls)
        {
            // For now, use first value of fill series
            return inner.FillNull(fill[0]);
        }

        return inner.FillNull(fill[0]);
    }

    // ============================================================================
    // When/Then/Otherwise
    // ============================================================================

    private static Series.Series EvaluateWhen(Expr.When when, DataFrame.DataFrame df)
    {
        var condition = Evaluate(when.Condition, df);
        var thenResult = Evaluate(when.ThenExpr, df);
        var otherwiseResult = when.OtherwiseExpr is not null
            ? Evaluate(when.OtherwiseExpr, df)
            : null;

        // Build result based on condition
        var values = new List<AnyValue>();
        for (int i = 0; i < df.Height; i++)
        {
            if (!condition.IsNull(i) && condition[i].AsBoolean())
            {
                values.Add(thenResult[i]);
            }
            else if (otherwiseResult is not null)
            {
                values.Add(otherwiseResult[i]);
            }
            else
            {
                values.Add(AnyValue.Null);
            }
        }

        return BuildSeriesFromAnyValues(thenResult.Name, values, thenResult.DataType);
    }

    // ============================================================================
    // Function
    // ============================================================================

    private static Series.Series EvaluateFunction(Expr.Function func, DataFrame.DataFrame df)
    {
        return func.Name.ToLower() switch
        {
            "count" => Series.Series.FromValues("count", new[] { df.Height }),
            "row_number" => Series.Series.FromValues("row_number", Enumerable.Range(0, df.Height).ToArray()),
            "arange" when func.Args.Count >= 2 =>
                EvaluateArange(func.Args[0], func.Args[1], func.Args.Count > 2 ? func.Args[2] : null, df),
            _ => throw new NotSupportedException($"Function not supported: {func.Name}")
        };
    }

    private static Series.Series EvaluateArange(Expr start, Expr end, Expr? step, DataFrame.DataFrame df)
    {
        var startVal = Evaluate(start, df)[0].AsInt32();
        var endVal = Evaluate(end, df)[0].AsInt32();
        var stepVal = step is not null ? Evaluate(step, df)[0].AsInt32() : 1;

        var values = new List<int>();
        for (int i = startVal; stepVal > 0 ? i < endVal : i > endVal; i += stepVal)
        {
            values.Add(i);
        }

        return Series.Series.FromValues("arange", values.ToArray());
    }

    // ============================================================================
    // Sort
    // ============================================================================

    private static Series.Series EvaluateSort(Expr.Sort sort, DataFrame.DataFrame df)
    {
        var inner = Evaluate(sort.Inner, df);
        return inner.Sort(sort.Descending, sort.NullsLast);
    }

    // ============================================================================
    // String Operations
    // ============================================================================

    private static Series.Series EvaluateStr(Expr.Str str, DataFrame.DataFrame df)
    {
        var inner = Evaluate(str.Inner, df);
        var strOps = inner.Str;

        return str.Operation switch
        {
            StringOp.ToLowerCase => strOps.ToLowerCase(),
            StringOp.ToUpperCase => strOps.ToUpperCase(),
            StringOp.Strip => strOps.Strip(),
            StringOp.Contains c => strOps.Contains(c.Pattern, c.Literal),
            StringOp.StartsWith s => strOps.StartsWith(s.Prefix),
            StringOp.EndsWith e => strOps.EndsWith(e.Suffix),
            StringOp.Replace r => strOps.Replace(r.Pattern, r.Replacement),
            StringOp.Lengths => strOps.Lengths(),
            StringOp.Substring s => strOps.Substring(s.Start, s.Length),
            StringOp.Extract e => strOps.Extract(e.Pattern, e.Group),
            _ => throw new NotSupportedException($"String operation not supported: {str.Operation}")
        };
    }

    // ============================================================================
    // DateTime Operations
    // ============================================================================

    private static Series.Series EvaluateDt(Expr.Dt dt, DataFrame.DataFrame df)
    {
        var inner = Evaluate(dt.Inner, df);
        var dtOps = inner.Dt;

        return dt.Operation switch
        {
            DateTimeOp.Year => dtOps.Year(),
            DateTimeOp.Month => dtOps.Month(),
            DateTimeOp.Day => dtOps.Day(),
            DateTimeOp.Hour => dtOps.Hour(),
            DateTimeOp.Minute => dtOps.Minute(),
            DateTimeOp.Second => dtOps.Second(),
            DateTimeOp.DayOfWeek => dtOps.DayOfWeek(),
            DateTimeOp.DayOfYear => dtOps.DayOfYear(),
            DateTimeOp.WeekOfYear => dtOps.WeekOfYear(),
            DateTimeOp.Quarter => dtOps.Quarter(),
            _ => throw new NotSupportedException($"DateTime operation not supported: {dt.Operation}")
        };
    }

    // ============================================================================
    // IsIn
    // ============================================================================

    private static Series.Series EvaluateIsIn(Expr.IsIn isIn, DataFrame.DataFrame df)
    {
        var inner = Evaluate(isIn.Inner, df);
        return inner.IsIn(isIn.Values.ToArray());
    }

    // ============================================================================
    // Between
    // ============================================================================

    private static Series.Series EvaluateBetween(Expr.Between between, DataFrame.DataFrame df)
    {
        var inner = Evaluate(between.Inner, df);
        var lower = Evaluate(between.Lower, df)[0];
        var upper = Evaluate(between.Upper, df)[0];
        return inner.Between(lower, upper, between.Inclusive);
    }

    // ============================================================================
    // Helpers
    // ============================================================================

    private static Series.Series BuildSeriesFromAnyValues(string name, List<AnyValue> values, DataType dtype)
    {
        return dtype switch
        {
            DataType.Int32Type => Series.Series.FromNullable(name, values.Select(v => v.IsNull ? null : (int?)v.AsInt32()).ToArray()),
            DataType.Int64Type => Series.Series.FromNullable(name, values.Select(v => v.IsNull ? null : (long?)v.AsInt64()).ToArray()),
            DataType.Float32Type => Series.Series.FromNullable(name, values.Select(v => v.IsNull ? null : (float?)v.AsFloat32()).ToArray()),
            DataType.Float64Type => Series.Series.FromNullable(name, values.Select(v =>
            {
                if (v.IsNull) return null;
                if (v.TryGetDouble(out var d)) return (double?)d;
                if (v.TryGetInt64(out var l)) return (double?)l;
                return null;
            }).ToArray()),
            DataType.BooleanType => Series.Series.FromNullable(name, values.Select(v => v.IsNull ? null : (bool?)v.AsBoolean()).ToArray()),
            DataType.StringType => Series.Series.FromValues(name, values.Select(v => v.IsNull ? null : v.AsString()).ToArray()),
            _ => Series.Series.FromValues(name, values.Select(v => v.ToString()).ToArray())
        };
    }
}
