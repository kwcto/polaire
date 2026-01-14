// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using SqlParser.Ast;
using Polaire.Expressions;
using static Polaire.Expressions.Expr;
using SqlBinaryOp = SqlParser.Ast.BinaryOperator;

namespace Polaire.Sql;

/// <summary>
/// Converts SQL AST to Polaire LogicalPlan.
/// </summary>
public static class SqlToLogicalPlan
{
    /// <summary>
    /// Convert a SQL statement to a LogicalPlan.
    /// </summary>
    public static LazyFrame.LogicalPlan Convert(Statement stmt, IReadOnlyDictionary<string, DataFrame> tables)
    {
        if (stmt is not Statement.Select select)
            throw new NotSupportedException($"Only SELECT statements supported, got: {stmt.GetType().Name}");

        return ConvertSelect(select.Query, tables);
    }

    private static LazyFrame.LogicalPlan ConvertSelect(Query query, IReadOnlyDictionary<string, DataFrame> tables)
    {
        // Get the select body
        var body = query.Body as SetExpression.SelectExpression
            ?? throw new NotSupportedException("Complex set operations (UNION, INTERSECT, etc.) not yet supported");

        var select = body.Select;

        // 1. FROM clause -> Source plan(s)
        if (select.From == null || select.From.Count == 0)
            throw new ArgumentException("FROM clause required");
        var plan = ConvertFrom(select.From, tables);

        // 2. WHERE clause -> Filter
        if (select.Selection != null)
        {
            var predicate = SqlExprConverter.Convert(select.Selection);
            plan = new LazyFrame.LogicalPlan.Filter(plan, predicate);
        }

        // 3. GROUP BY + Aggregations
        if (select.GroupBy is GroupByExpression.Expressions groupByExprs && groupByExprs.ColumnNames.Count > 0)
        {
            plan = ConvertGroupBy(plan, groupByExprs.ColumnNames, select.Projection);
        }
        else if (ContainsAggregates(select.Projection))
        {
            // SELECT with aggregates but no GROUP BY (e.g., SELECT COUNT(*) FROM t)
            plan = ConvertGroupBy(plan, Array.Empty<Expression>(), select.Projection);
        }
        else
        {
            // Regular SELECT projection
            plan = ConvertProjection(plan, select.Projection);
        }

        // 4. HAVING clause (filter after GROUP BY)
        if (select.Having != null)
        {
            var havingPred = SqlExprConverter.Convert(select.Having);
            plan = new LazyFrame.LogicalPlan.Filter(plan, havingPred);
        }

        // 5. DISTINCT
        if (select.Distinct != null)
        {
            plan = new LazyFrame.LogicalPlan.Distinct(plan, null);
        }

        // 6. ORDER BY
        if (query.OrderBy?.Expressions != null && query.OrderBy.Expressions.Count > 0)
        {
            var sortExprs = query.OrderBy.Expressions.Select(ob =>
            {
                var expr = SqlExprConverter.Convert(ob.Expression);
                var desc = ob.Asc == false; // DESC if explicitly false
                return expr.SortBy(desc);
            }).ToArray();

            plan = new LazyFrame.LogicalPlan.Sort(plan, sortExprs, false);
        }

        // 7. LIMIT
        if (query.Limit != null)
        {
            var limitExpr = query.Limit;
            if (limitExpr is Expression.LiteralValue lit && lit.Value is Value.Number num)
            {
                if (int.TryParse(num.Value, out var n))
                {
                    plan = new LazyFrame.LogicalPlan.Limit(plan, n);
                }
            }
        }

        // 8. OFFSET (combine with LIMIT as Slice)
        if (query.Offset != null)
        {
            var offsetExpr = query.Offset.Value;
            if (offsetExpr is Expression.LiteralValue lit && lit.Value is Value.Number num)
            {
                if (int.TryParse(num.Value, out var offset) && offset > 0)
                {
                    // If we already have a Limit, convert to Slice
                    if (plan is LazyFrame.LogicalPlan.Limit limit)
                    {
                        plan = new LazyFrame.LogicalPlan.Slice(limit.Input, offset, limit.N);
                    }
                    else
                    {
                        // Just offset without limit - use Slice with int.MaxValue length
                        plan = new LazyFrame.LogicalPlan.Slice(plan, offset, int.MaxValue);
                    }
                }
            }
        }

        return plan;
    }

    private static LazyFrame.LogicalPlan ConvertFrom(IReadOnlyList<TableWithJoins> fromClause, IReadOnlyDictionary<string, DataFrame> tables)
    {
        if (fromClause.Count == 0)
            throw new ArgumentException("FROM clause required");

        if (fromClause.Count > 1)
            throw new NotSupportedException("Multiple tables in FROM not yet supported; use JOIN instead");

        var tableWithJoins = fromClause[0];
        if (tableWithJoins.Relation == null)
            throw new ArgumentException("Table relation is required");

        var basePlan = ConvertTableFactor(tableWithJoins.Relation, tables);

        // Process JOINs
        if (tableWithJoins.Joins != null)
        {
            foreach (var join in tableWithJoins.Joins)
            {
                basePlan = ConvertJoin(basePlan, join, tables);
            }
        }

        return basePlan;
    }

    private static LazyFrame.LogicalPlan ConvertTableFactor(TableFactor factor, IReadOnlyDictionary<string, DataFrame> tables)
    {
        return factor switch
        {
            TableFactor.Table table => ConvertTable(table, tables),
            TableFactor.Derived derived => ConvertSelect(derived.SubQuery, tables),
            TableFactor.NestedJoin nested when nested.TableWithJoins?.Relation != null
                => ConvertTableFactor(nested.TableWithJoins.Relation, tables),
            _ => throw new NotSupportedException($"Table factor type not supported: {factor.GetType().Name}")
        };
    }

    private static LazyFrame.LogicalPlan ConvertTable(TableFactor.Table table, IReadOnlyDictionary<string, DataFrame> tables)
    {
        var tableName = table.Name.ToString();

        // Look up table in context (case-insensitive)
        if (!tables.TryGetValue(tableName, out var df))
        {
            // Try to find case-insensitively
            var match = tables.FirstOrDefault(kvp =>
                string.Equals(kvp.Key, tableName, StringComparison.OrdinalIgnoreCase));

            if (match.Value == null)
                throw new ArgumentException($"Table not found: {tableName}. Available tables: {string.Join(", ", tables.Keys)}");

            df = match.Value;
        }

        return new LazyFrame.LogicalPlan.Scan(df);
    }

    private static LazyFrame.LogicalPlan ConvertJoin(LazyFrame.LogicalPlan left, Join join, IReadOnlyDictionary<string, DataFrame> tables)
    {
        if (join.Relation == null)
            throw new ArgumentException("Join relation is required");

        var right = ConvertTableFactor(join.Relation, tables);

        // Determine join type
        if (join.JoinOperator == null)
            throw new ArgumentException("Join operator is required");

        var joinType = join.JoinOperator switch
        {
            JoinOperator.Inner => JoinType.Inner,
            JoinOperator.LeftOuter => JoinType.Left,
            JoinOperator.RightOuter => JoinType.Right,
            JoinOperator.FullOuter => JoinType.Outer,
            JoinOperator.CrossJoin => JoinType.Cross,
            _ => throw new NotSupportedException($"Join type not supported: {join.JoinOperator.GetType().Name}")
        };

        // Extract join condition
        JoinConstraint? constraint = join.JoinOperator switch
        {
            JoinOperator.Inner inner => inner.JoinConstraint,
            JoinOperator.LeftOuter lo => lo.JoinConstraint,
            JoinOperator.RightOuter ro => ro.JoinConstraint,
            JoinOperator.FullOuter fo => fo.JoinConstraint,
            _ => null
        };

        if (joinType == JoinType.Cross)
        {
            // Cross join - no condition needed
            return new LazyFrame.LogicalPlan.Join(left, right, Array.Empty<Expr>(), Array.Empty<Expr>(), joinType, string.Empty);
        }

        if (constraint == null)
            throw new ArgumentException("JOIN requires ON clause");

        // Parse ON condition to extract left/right join keys
        var (leftOn, rightOn) = ExtractJoinKeys(constraint);

        return new LazyFrame.LogicalPlan.Join(left, right, leftOn, rightOn, joinType, string.Empty);
    }

    private static (Expr[] LeftOn, Expr[] RightOn) ExtractJoinKeys(JoinConstraint constraint)
    {
        if (constraint is JoinConstraint.On on)
        {
            // For now, support simple equality conditions: a.x = b.y
            // More complex conditions would need additional handling
            return ExtractEqualityKeys(on.Expression);
        }

        if (constraint is JoinConstraint.Using usingClause)
        {
            var cols = usingClause.Idents.Select(i => Col(i.Value)).ToArray();
            return (cols, cols);
        }

        throw new NotSupportedException($"Join constraint type not supported: {constraint.GetType().Name}");
    }

    private static (Expr[] LeftOn, Expr[] RightOn) ExtractEqualityKeys(Expression expr)
    {
        var leftKeys = new List<Expr>();
        var rightKeys = new List<Expr>();

        ExtractEqualityKeysRecursive(expr, leftKeys, rightKeys);

        if (leftKeys.Count == 0)
            throw new ArgumentException("JOIN ON clause must contain equality conditions (e.g., a.id = b.id)");

        return (leftKeys.ToArray(), rightKeys.ToArray());
    }

    private static void ExtractEqualityKeysRecursive(Expression expr, List<Expr> leftKeys, List<Expr> rightKeys)
    {
        if (expr is Expression.BinaryOp binary)
        {
            if (binary.Op == SqlBinaryOp.Eq)
            {
                // Found equality: left = right
                leftKeys.Add(SqlExprConverter.Convert(binary.Left));
                rightKeys.Add(SqlExprConverter.Convert(binary.Right));
            }
            else if (binary.Op == SqlBinaryOp.And)
            {
                // AND: recurse into both sides
                ExtractEqualityKeysRecursive(binary.Left, leftKeys, rightKeys);
                ExtractEqualityKeysRecursive(binary.Right, leftKeys, rightKeys);
            }
        }
        else if (expr is Expression.Nested nested)
        {
            ExtractEqualityKeysRecursive(nested.Expression, leftKeys, rightKeys);
        }
    }

    private static LazyFrame.LogicalPlan ConvertProjection(LazyFrame.LogicalPlan input, IReadOnlyList<SelectItem> projection)
    {
        // Handle SELECT *
        if (projection.Count == 1 && projection[0] is SelectItem.Wildcard)
        {
            return input; // Return all columns
        }

        var exprs = new List<Expr>();

        foreach (var item in projection)
        {
            switch (item)
            {
                case SelectItem.UnnamedExpression unnamed:
                    exprs.Add(SqlExprConverter.Convert(unnamed.Expression));
                    break;

                case SelectItem.ExpressionWithAlias aliased:
                    var expr = SqlExprConverter.Convert(aliased.Expression);
                    exprs.Add(expr.As(aliased.Alias.Value));
                    break;

                case SelectItem.Wildcard:
                    // SELECT * with other columns - need to expand
                    throw new NotSupportedException("Wildcard (*) mixed with other columns not yet supported");

                case SelectItem.QualifiedWildcard qw:
                    throw new NotSupportedException($"Qualified wildcard ({qw.Name}.*) not yet supported");

                default:
                    throw new NotSupportedException($"Select item type not supported: {item.GetType().Name}");
            }
        }

        return new LazyFrame.LogicalPlan.Select(input, exprs.ToArray());
    }

    private static LazyFrame.LogicalPlan ConvertGroupBy(LazyFrame.LogicalPlan input, IReadOnlyList<Expression> groupByExprs, IReadOnlyList<SelectItem> projection)
    {
        // Convert GROUP BY expressions
        var groupBy = groupByExprs.Select(SqlExprConverter.Convert).ToArray();

        // Convert SELECT expressions - separate into group keys and aggregations
        var aggs = new List<Expr>();

        foreach (var item in projection)
        {
            Expr expr;
            string? alias = null;

            switch (item)
            {
                case SelectItem.UnnamedExpression unnamed:
                    expr = SqlExprConverter.Convert(unnamed.Expression);
                    break;

                case SelectItem.ExpressionWithAlias aliased:
                    expr = SqlExprConverter.Convert(aliased.Expression);
                    alias = aliased.Alias.Value;
                    break;

                default:
                    throw new NotSupportedException($"Select item in GROUP BY not supported: {item.GetType().Name}");
            }

            // Add alias if present
            if (alias != null)
                expr = expr.As(alias);

            aggs.Add(expr);
        }

        return new LazyFrame.LogicalPlan.Aggregate(input, groupBy, aggs.ToArray());
    }

    private static bool ContainsAggregates(IReadOnlyList<SelectItem> projection)
    {
        foreach (var item in projection)
        {
            Expression? expr = item switch
            {
                SelectItem.UnnamedExpression unnamed => unnamed.Expression,
                SelectItem.ExpressionWithAlias aliased => aliased.Expression,
                _ => null
            };

            if (expr != null && IsAggregateExpression(expr))
                return true;
        }
        return false;
    }

    private static bool IsAggregateExpression(Expression expr)
    {
        return expr switch
        {
            Expression.Function func => IsAggregateFunction(GetFunctionName(func.Name)),
            Expression.Nested nested => IsAggregateExpression(nested.Expression),
            _ => false
        };
    }

    private static string GetFunctionName(ObjectName name) => name.ToString().ToUpperInvariant();

    private static bool IsAggregateFunction(string name)
    {
        return name switch
        {
            "SUM" or "AVG" or "MEAN" or "MIN" or "MAX" or "COUNT" or
            "STD" or "STDDEV" or "VAR" or "VARIANCE" or "MEDIAN" => true,
            _ => false
        };
    }
}
