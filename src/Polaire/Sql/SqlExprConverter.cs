// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using SqlParser.Ast;
using Polaire.Expressions;
using static Polaire.Expressions.Expr;
using SqlBinaryOp = SqlParser.Ast.BinaryOperator;
using SqlUnaryOp = SqlParser.Ast.UnaryOperator;
using PolaireBinaryOp = Polaire.Expressions.BinaryOperator;
using PolaireUnaryOp = Polaire.Expressions.UnaryOperator;

namespace Polaire.Sql;

/// <summary>
/// Converts SQL AST expressions to Polaire expressions.
/// </summary>
public static class SqlExprConverter
{
    /// <summary>
    /// Convert a SQL expression to a Polaire expression.
    /// </summary>
    public static Expr Convert(Expression sqlExpr)
    {
        return sqlExpr switch
        {
            // Column reference
            Expression.Identifier ident => Col(ident.Ident.Value),
            Expression.CompoundIdentifier compound => Col(compound.Idents[^1].Value),

            // Literals
            Expression.LiteralValue lit => ConvertLiteral(lit),

            // Binary operations
            Expression.BinaryOp binary => ConvertBinaryOp(binary),

            // Unary operations
            Expression.UnaryOp unary => ConvertUnaryOp(unary),

            // Function calls
            Expression.Function func => ConvertFunction(func),

            // Nested expression in parentheses
            Expression.Nested nested => Convert(nested.Expression),

            // IS NULL / IS NOT NULL
            Expression.IsNull isNull => Convert(isNull.Expression).IsNullExpr(),
            Expression.IsNotNull isNotNull => Convert(isNotNull.Expression).IsNotNullExpr(),

            // LIKE (separate expression type in SqlParser-cs)
            Expression.Like like when like.Expression != null && like.Pattern != null
                => ConvertLike(Convert(like.Expression), like.Pattern, like.Negated),

            // BETWEEN
            Expression.Between between => ConvertBetween(between),

            // IN list
            Expression.InList inList => ConvertInList(inList),

            // CASE WHEN
            Expression.Case caseExpr => ConvertCase(caseExpr),

            _ => throw new NotSupportedException($"SQL expression type not supported: {sqlExpr.GetType().Name}")
        };
    }

    private static Expr ConvertLiteral(Expression.LiteralValue lit)
    {
        return lit.Value switch
        {
            Value.Number num => ConvertNumber(num.Value),
            Value.SingleQuotedString str => Lit(str.Value),
            Value.DoubleQuotedString str => Lit(str.Value),
            Value.Boolean b => Lit(b.Value),
            Value.Null => Lit(null),
            _ => throw new NotSupportedException($"Literal type not supported: {lit.Value.GetType().Name}")
        };
    }

    private static Expr ConvertNumber(string numStr)
    {
        if (long.TryParse(numStr, out var longVal))
            return Lit(longVal);
        if (double.TryParse(numStr, out var doubleVal))
            return Lit(doubleVal);
        throw new NotSupportedException($"Cannot parse number: {numStr}");
    }

    private static Expr ConvertBinaryOp(Expression.BinaryOp binary)
    {
        var left = Convert(binary.Left);
        var right = Convert(binary.Right);

        return binary.Op switch
        {
            // Arithmetic
            SqlBinaryOp.Plus => left + right,
            SqlBinaryOp.Minus => left - right,
            SqlBinaryOp.Multiply => left * right,
            SqlBinaryOp.Divide => left / right,
            SqlBinaryOp.Modulo => left % right,

            // Comparison
            SqlBinaryOp.Eq => new Expr.BinaryOp(left, PolaireBinaryOp.Equal, right),
            SqlBinaryOp.NotEq => new Expr.BinaryOp(left, PolaireBinaryOp.NotEqual, right),
            SqlBinaryOp.Lt => new Expr.BinaryOp(left, PolaireBinaryOp.LessThan, right),
            SqlBinaryOp.LtEq => new Expr.BinaryOp(left, PolaireBinaryOp.LessEqual, right),
            SqlBinaryOp.Gt => new Expr.BinaryOp(left, PolaireBinaryOp.GreaterThan, right),
            SqlBinaryOp.GtEq => new Expr.BinaryOp(left, PolaireBinaryOp.GreaterEqual, right),

            // Logical
            SqlBinaryOp.And => new Expr.BinaryOp(left, PolaireBinaryOp.And, right),
            SqlBinaryOp.Or => new Expr.BinaryOp(left, PolaireBinaryOp.Or, right),
            SqlBinaryOp.Xor => new Expr.BinaryOp(left, PolaireBinaryOp.Xor, right),

            _ => throw new NotSupportedException($"Binary operator not supported: {binary.Op}")
        };
    }

    private static Expr ConvertLike(Expr left, Expression patternExpr, bool negated)
    {
        // Convert pattern expression to get the literal string
        var patternConverted = Convert(patternExpr);
        if (patternConverted is not Literal lit || !lit.Value.TryGetString(out var pattern))
            throw new NotSupportedException("LIKE requires a literal pattern");

        Expr result;
        if (pattern.StartsWith('%') && pattern.EndsWith('%'))
        {
            var search = pattern.Trim('%');
            result = new Str(left, new StringOp.Contains(search, true));
        }
        else if (pattern.StartsWith('%'))
        {
            var search = pattern.TrimStart('%');
            result = new Str(left, new StringOp.EndsWith(search));
        }
        else if (pattern.EndsWith('%'))
        {
            var search = pattern.TrimEnd('%');
            result = new Str(left, new StringOp.StartsWith(search));
        }
        else
        {
            result = new Expr.BinaryOp(left, PolaireBinaryOp.Equal, patternConverted);
        }

        if (negated)
            return new Expr.UnaryOp(PolaireUnaryOp.Not, result);
        return result;
    }

    private static Expr ConvertUnaryOp(Expression.UnaryOp unary)
    {
        var inner = Convert(unary.Expression);

        return unary.Op switch
        {
            SqlUnaryOp.Not => new Expr.UnaryOp(PolaireUnaryOp.Not, inner),
            SqlUnaryOp.Minus => new Expr.UnaryOp(PolaireUnaryOp.Negate, inner),
            SqlUnaryOp.Plus => inner,
            _ => throw new NotSupportedException($"Unary operator not supported: {unary.Op}")
        };
    }

    private static Expr ConvertFunction(Expression.Function func)
    {
        var name = GetFunctionName(func.Name);
        var args = GetFunctionArgs(func);

        return name switch
        {
            // Aggregations
            "SUM" => args.Length == 1 ? args[0].Sum() : throw new ArgumentException("SUM requires 1 argument"),
            "AVG" or "MEAN" => args.Length == 1 ? args[0].Mean() : throw new ArgumentException("AVG requires 1 argument"),
            "MIN" => args.Length == 1 ? args[0].Min() : throw new ArgumentException("MIN requires 1 argument"),
            "MAX" => args.Length == 1 ? args[0].Max() : throw new ArgumentException("MAX requires 1 argument"),
            "COUNT" => ConvertCount(func),
            "STD" or "STDDEV" => args.Length == 1 ? args[0].Std() : throw new ArgumentException("STD requires 1 argument"),
            "VAR" or "VARIANCE" => args.Length == 1 ? args[0].Var() : throw new ArgumentException("VAR requires 1 argument"),
            "MEDIAN" => args.Length == 1 ? args[0].Median() : throw new ArgumentException("MEDIAN requires 1 argument"),

            // String functions
            "UPPER" => args.Length == 1 ? new Str(args[0], new StringOp.ToUpperCase())
                : throw new ArgumentException("UPPER requires 1 argument"),
            "LOWER" => args.Length == 1 ? new Str(args[0], new StringOp.ToLowerCase())
                : throw new ArgumentException("LOWER requires 1 argument"),
            "LENGTH" or "LEN" => args.Length == 1 ? new Str(args[0], new StringOp.Lengths())
                : throw new ArgumentException("LENGTH requires 1 argument"),
            "TRIM" => args.Length == 1 ? new Str(args[0], new StringOp.Strip())
                : throw new ArgumentException("TRIM requires 1 argument"),

            // Math functions
            "ABS" => args.Length == 1 ? new Expr.UnaryOp(PolaireUnaryOp.Abs, args[0])
                : throw new ArgumentException("ABS requires 1 argument"),
            "SQRT" => args.Length == 1 ? new Expr.UnaryOp(PolaireUnaryOp.Sqrt, args[0])
                : throw new ArgumentException("SQRT requires 1 argument"),
            "LOG" or "LN" => args.Length == 1 ? new Expr.UnaryOp(PolaireUnaryOp.Log, args[0])
                : throw new ArgumentException("LOG requires 1 argument"),
            "EXP" => args.Length == 1 ? new Expr.UnaryOp(PolaireUnaryOp.Exp, args[0])
                : throw new ArgumentException("EXP requires 1 argument"),

            _ => throw new NotSupportedException($"Function not supported: {name}")
        };
    }

    private static string GetFunctionName(ObjectName name)
    {
        return name.ToString().ToUpperInvariant();
    }

    private static Expr[] GetFunctionArgs(Expression.Function func)
    {
        if (func.Args is not FunctionArguments.List argList)
            return Array.Empty<Expr>();

        var args = argList.ArgumentList?.Args;
        if (args == null)
            return Array.Empty<Expr>();

        return args
            .Select(ConvertFunctionArg)
            .Where(e => e != null)
            .Cast<Expr>()
            .ToArray();
    }

    private static Expr? ConvertFunctionArg(FunctionArg arg)
    {
        return arg switch
        {
            FunctionArg.Unnamed unnamed => unnamed.FunctionArgExpression switch
            {
                FunctionArgExpression.FunctionExpression fe => Convert(fe.Expression),
                FunctionArgExpression.Wildcard => null, // Return null for COUNT(*)
                _ => throw new NotSupportedException($"Function arg type: {unnamed.FunctionArgExpression.GetType().Name}")
            },
            _ => throw new NotSupportedException("Named function arguments not supported")
        };
    }

    private static Expr ConvertCount(Expression.Function func)
    {
        // Check for COUNT(*)
        if (func.Args is FunctionArguments.List argList)
        {
            var args = argList.ArgumentList?.Args;
            if (args != null && args.Count > 0)
            {
                var firstArg = args[0];
                if (firstArg is FunctionArg.Unnamed unnamed &&
                    unnamed.FunctionArgExpression is FunctionArgExpression.Wildcard)
                {
                    return Expr.Count();
                }
            }
        }

        var funcArgs = GetFunctionArgs(func);
        if (funcArgs.Length == 1)
            return funcArgs[0].CountExpr();

        return Expr.Count();
    }

    private static Expr ConvertBetween(Expression.Between between)
    {
        var expr = Convert(between.Expression);
        var low = Convert(between.Low);
        var high = Convert(between.High);

        var result = expr.IsBetween(low, high);
        if (between.Negated)
            return new Expr.UnaryOp(PolaireUnaryOp.Not, result);
        return result;
    }

    private static Expr ConvertInList(Expression.InList inList)
    {
        var expr = Convert(inList.Expression);
        var values = inList.List.Select(v =>
        {
            var converted = Convert(v);
            if (converted is Literal lit)
                return lit.Value;
            throw new NotSupportedException("IN list must contain literals");
        }).ToArray();

        var result = new IsIn(expr, values);
        if (inList.Negated)
            return new Expr.UnaryOp(PolaireUnaryOp.Not, result);
        return result;
    }

    private static Expr ConvertCase(Expression.Case caseExpr)
    {
        if (caseExpr.Conditions.Count == 0)
            throw new ArgumentException("CASE requires at least one WHEN clause");

        Expr result = caseExpr.ElseResult != null
            ? Convert(caseExpr.ElseResult)
            : Lit(null);

        for (int i = caseExpr.Conditions.Count - 1; i >= 0; i--)
        {
            var cond = Convert(caseExpr.Conditions[i]);
            var then = Convert(caseExpr.Results[i]);
            result = new When(cond, then, result);
        }

        return result;
    }
}
