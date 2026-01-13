// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using Polaire.Expressions;

namespace Polaire.LazyFrame;

/// <summary>
/// Optimizes logical query plans using various optimization rules.
/// Implements standard query optimization techniques like predicate pushdown,
/// projection pushdown, constant folding, and common subexpression elimination.
/// </summary>
public static class QueryOptimizer
{
    /// <summary>Optimizes a logical plan.</summary>
    public static LogicalPlan Optimize(LogicalPlan plan)
    {
        var optimized = plan;

        // Apply optimization passes
        optimized = PredicatePushdown(optimized);
        optimized = ProjectionPushdown(optimized);
        optimized = ConstantFolding(optimized);
        optimized = SimplifyExpressions(optimized);
        optimized = CombineFilters(optimized);
        optimized = CombineLimits(optimized);

        return optimized;
    }

    // ============================================================================
    // Predicate Pushdown
    // ============================================================================

    /// <summary>
    /// Pushes filter predicates as close to the data source as possible.
    /// This reduces the number of rows processed by subsequent operations.
    /// </summary>
    private static LogicalPlan PredicatePushdown(LogicalPlan plan)
    {
        return plan switch
        {
            // Push filter through select
            LogicalPlan.Filter { Input: LogicalPlan.Select select, Predicate: var pred }
                when CanPushPredicateThroughSelect(pred, select) =>
                new LogicalPlan.Select(
                    PredicatePushdown(new LogicalPlan.Filter(select.Input, pred)),
                    select.Exprs),

            // Push filter through sort (filter doesn't depend on ordering)
            LogicalPlan.Filter { Input: LogicalPlan.Sort sort, Predicate: var pred } =>
                new LogicalPlan.Sort(
                    PredicatePushdown(new LogicalPlan.Filter(sort.Input, pred)),
                    sort.Exprs,
                    sort.MaintainOrder),

            // Push filter to scan (predicate pushdown to source)
            LogicalPlan.Filter { Input: LogicalPlan.Scan scan, Predicate: var pred } =>
                new LogicalPlan.ScanWithPredicate(scan.Df, pred),

            // Combine predicate with existing scan predicate
            LogicalPlan.Filter { Input: LogicalPlan.ScanWithPredicate scanPred, Predicate: var pred } =>
                new LogicalPlan.ScanWithPredicate(scanPred.Df, CombinePredicates(scanPred.Predicate, pred)),

            // Push filter through join (if predicate only references one side)
            LogicalPlan.Filter { Input: LogicalPlan.Join join, Predicate: var pred }
                when GetPredicateSide(pred, join) is { } side =>
                side == JoinSide.Left
                    ? new LogicalPlan.Join(
                        PredicatePushdown(new LogicalPlan.Filter(join.Left, pred)),
                        join.Right, join.LeftOn, join.RightOn, join.How, join.Suffix)
                    : new LogicalPlan.Join(
                        join.Left,
                        PredicatePushdown(new LogicalPlan.Filter(join.Right, pred)),
                        join.LeftOn, join.RightOn, join.How, join.Suffix),

            // Recursively optimize children
            LogicalPlan.Select s => new LogicalPlan.Select(PredicatePushdown(s.Input), s.Exprs),
            LogicalPlan.WithColumns w => new LogicalPlan.WithColumns(PredicatePushdown(w.Input), w.Exprs),
            LogicalPlan.Filter f => new LogicalPlan.Filter(PredicatePushdown(f.Input), f.Predicate),
            LogicalPlan.Sort s => new LogicalPlan.Sort(PredicatePushdown(s.Input), s.Exprs, s.MaintainOrder),
            LogicalPlan.Limit l => new LogicalPlan.Limit(PredicatePushdown(l.Input), l.N),
            LogicalPlan.Aggregate a => new LogicalPlan.Aggregate(PredicatePushdown(a.Input), a.GroupBy, a.Aggs),
            LogicalPlan.Join j => new LogicalPlan.Join(
                PredicatePushdown(j.Left), PredicatePushdown(j.Right),
                j.LeftOn, j.RightOn, j.How, j.Suffix),
            LogicalPlan.Union u => new LogicalPlan.Union(PredicatePushdown(u.Left), PredicatePushdown(u.Right)),

            _ => plan
        };
    }

    private static bool CanPushPredicateThroughSelect(Expr pred, LogicalPlan.Select select)
    {
        // Can push if predicate only references columns that exist before the select
        var referencedColumns = GetReferencedColumns(pred);
        var selectOutputs = select.Exprs.Select(GetExprOutputName).ToHashSet();

        // Check if all referenced columns are simple pass-through columns
        return referencedColumns.All(col =>
            select.Exprs.Any(e => e is Expr.Column c && c.Name == col));
    }

    private static string GetExprOutputName(Expr expr) => expr switch
    {
        Expr.Column c => c.Name,
        Expr.Alias a => a.Name,
        _ => "unknown"
    };

    private enum JoinSide { Left, Right }

    private static JoinSide? GetPredicateSide(Expr pred, LogicalPlan.Join join)
    {
        // Determine if predicate references only left or right side columns
        // This is a simplified check - full implementation would track column lineage
        return null;
    }

    private static Expr CombinePredicates(Expr a, Expr b) => a & b;

    // ============================================================================
    // Projection Pushdown
    // ============================================================================

    /// <summary>
    /// Pushes column projections as close to the data source as possible.
    /// This reduces memory usage by not loading unnecessary columns.
    /// </summary>
    private static LogicalPlan ProjectionPushdown(LogicalPlan plan)
    {
        var requiredColumns = CollectRequiredColumns(plan);
        return PushProjections(plan, requiredColumns);
    }

    private static HashSet<string> CollectRequiredColumns(LogicalPlan plan)
    {
        return plan switch
        {
            LogicalPlan.Scan scan => scan.Df.Columns.ToHashSet(),
            LogicalPlan.Select select =>
                select.Exprs.SelectMany(GetReferencedColumns).ToHashSet(),
            LogicalPlan.Filter filter =>
                GetReferencedColumns(filter.Predicate)
                    .Concat(CollectRequiredColumns(filter.Input)).ToHashSet(),
            LogicalPlan.Aggregate agg =>
                agg.GroupBy.Concat(agg.Aggs).SelectMany(GetReferencedColumns).ToHashSet(),
            LogicalPlan.Sort sort =>
                sort.Exprs.SelectMany(GetReferencedColumns)
                    .Concat(CollectRequiredColumns(sort.Input)).ToHashSet(),
            LogicalPlan.Join join =>
                join.LeftOn.Concat(join.RightOn).SelectMany(GetReferencedColumns)
                    .Concat(CollectRequiredColumns(join.Left))
                    .Concat(CollectRequiredColumns(join.Right)).ToHashSet(),
            _ => plan switch
            {
                LogicalPlan.WithColumns w => CollectRequiredColumns(w.Input),
                LogicalPlan.Limit l => CollectRequiredColumns(l.Input),
                LogicalPlan.Distinct d => CollectRequiredColumns(d.Input),
                _ => new HashSet<string>()
            }
        };
    }

    private static LogicalPlan PushProjections(LogicalPlan plan, HashSet<string> requiredColumns)
    {
        return plan switch
        {
            // Add projection to scan if not all columns needed
            LogicalPlan.Scan scan when requiredColumns.Count < scan.Df.Columns.Count =>
                new LogicalPlan.ScanWithProjection(scan.Df, requiredColumns.Intersect(scan.Df.Columns).ToArray()),

            LogicalPlan.ScanWithPredicate scanPred when requiredColumns.Count < scanPred.Df.Columns.Count =>
                new LogicalPlan.Filter(
                    new LogicalPlan.ScanWithProjection(scanPred.Df,
                        requiredColumns.Concat(GetReferencedColumns(scanPred.Predicate)).Distinct().ToArray()),
                    scanPred.Predicate),

            // Recursively process children
            LogicalPlan.Select s => new LogicalPlan.Select(
                PushProjections(s.Input, s.Exprs.SelectMany(GetReferencedColumns).ToHashSet()),
                s.Exprs),
            LogicalPlan.Filter f => new LogicalPlan.Filter(
                PushProjections(f.Input, requiredColumns.Concat(GetReferencedColumns(f.Predicate)).ToHashSet()),
                f.Predicate),

            _ => plan
        };
    }

    // ============================================================================
    // Constant Folding
    // ============================================================================

    /// <summary>
    /// Evaluates constant expressions at compile time.
    /// </summary>
    private static LogicalPlan ConstantFolding(LogicalPlan plan)
    {
        return plan switch
        {
            LogicalPlan.Select s => new LogicalPlan.Select(
                ConstantFolding(s.Input),
                s.Exprs.Select(FoldConstants).ToArray()),
            LogicalPlan.Filter f => new LogicalPlan.Filter(
                ConstantFolding(f.Input),
                FoldConstants(f.Predicate)),
            LogicalPlan.WithColumns w => new LogicalPlan.WithColumns(
                ConstantFolding(w.Input),
                w.Exprs.Select(FoldConstants).ToArray()),
            LogicalPlan.Sort s => new LogicalPlan.Sort(
                ConstantFolding(s.Input),
                s.Exprs.Select(FoldConstants).ToArray(),
                s.MaintainOrder),
            LogicalPlan.Aggregate a => new LogicalPlan.Aggregate(
                ConstantFolding(a.Input),
                a.GroupBy.Select(FoldConstants).ToArray(),
                a.Aggs.Select(FoldConstants).ToArray()),
            _ => plan
        };
    }

    private static Expr FoldConstants(Expr expr)
    {
        return expr switch
        {
            // Fold binary operations on literals
            Expr.BinaryOp { Left: Expr.Literal l1, Right: Expr.Literal l2, Op: var op }
                when TryFoldBinaryOp(l1.Value, l2.Value, op, out var result) =>
                new Expr.Literal(result),

            // Fold unary operations on literals
            Expr.UnaryOp { Inner: Expr.Literal lit, Op: UnaryOperator.Negate }
                when lit.Value.TryGetDouble(out var d) =>
                new Expr.Literal(AnyValue.From(-d)),
            Expr.UnaryOp { Inner: Expr.Literal lit, Op: UnaryOperator.Not }
                when lit.Value.Kind == AnyValueKind.Boolean =>
                new Expr.Literal(AnyValue.From(!lit.Value.AsBoolean())),

            // Simplify x + 0, x * 1, etc.
            Expr.BinaryOp { Left: var left, Right: Expr.Literal { Value: var v }, Op: BinaryOperator.Add }
                when v.TryGetDouble(out var d) && d == 0 => FoldConstants(left),
            Expr.BinaryOp { Left: var left, Right: Expr.Literal { Value: var v }, Op: BinaryOperator.Multiply }
                when v.TryGetDouble(out var d) && d == 1 => FoldConstants(left),
            Expr.BinaryOp { Left: var left, Right: Expr.Literal { Value: var v }, Op: BinaryOperator.Multiply }
                when v.TryGetDouble(out var d) && d == 0 => new Expr.Literal(AnyValue.From(0.0)),

            // Recursively fold
            Expr.BinaryOp binop => new Expr.BinaryOp(FoldConstants(binop.Left), binop.Op, FoldConstants(binop.Right)),
            Expr.UnaryOp unop => new Expr.UnaryOp(unop.Op, FoldConstants(unop.Inner)),
            Expr.Alias alias => new Expr.Alias(FoldConstants(alias.Inner), alias.Name),
            Expr.Cast cast => new Expr.Cast(FoldConstants(cast.Inner), cast.TargetType),
            Expr.Agg agg => new Expr.Agg(agg.Type, FoldConstants(agg.Inner)),

            _ => expr
        };
    }

    private static bool TryFoldBinaryOp(AnyValue left, AnyValue right, BinaryOperator op, out AnyValue result)
    {
        result = AnyValue.Null;

        if (!left.TryGetDouble(out var l) || !right.TryGetDouble(out var r))
            return false;

        result = op switch
        {
            BinaryOperator.Add => AnyValue.From(l + r),
            BinaryOperator.Subtract => AnyValue.From(l - r),
            BinaryOperator.Multiply => AnyValue.From(l * r),
            BinaryOperator.Divide when r != 0 => AnyValue.From(l / r),
            BinaryOperator.Equal => AnyValue.From(l == r),
            BinaryOperator.NotEqual => AnyValue.From(l != r),
            BinaryOperator.LessThan => AnyValue.From(l < r),
            BinaryOperator.LessEqual => AnyValue.From(l <= r),
            BinaryOperator.GreaterThan => AnyValue.From(l > r),
            BinaryOperator.GreaterEqual => AnyValue.From(l >= r),
            _ => AnyValue.Null
        };

        return !result.IsNull;
    }

    // ============================================================================
    // Expression Simplification
    // ============================================================================

    private static LogicalPlan SimplifyExpressions(LogicalPlan plan)
    {
        // Remove redundant operations, simplify boolean expressions, etc.
        return plan switch
        {
            // Filter with constant true - remove filter
            LogicalPlan.Filter { Input: var input, Predicate: Expr.Literal { Value: var v } }
                when v.Kind == AnyValueKind.Boolean && v.AsBoolean() =>
                SimplifyExpressions(input),

            // Filter with constant false - return empty
            // (This would need schema information to implement properly)

            _ => plan
        };
    }

    // ============================================================================
    // Combine Filters
    // ============================================================================

    private static LogicalPlan CombineFilters(LogicalPlan plan)
    {
        return plan switch
        {
            // Combine consecutive filters into single AND predicate
            LogicalPlan.Filter { Input: LogicalPlan.Filter { Input: var inner, Predicate: var p1 }, Predicate: var p2 } =>
                CombineFilters(new LogicalPlan.Filter(inner, p1 & p2)),

            // Recursively process
            LogicalPlan.Select s => new LogicalPlan.Select(CombineFilters(s.Input), s.Exprs),
            LogicalPlan.Filter f => new LogicalPlan.Filter(CombineFilters(f.Input), f.Predicate),
            LogicalPlan.Sort s => new LogicalPlan.Sort(CombineFilters(s.Input), s.Exprs, s.MaintainOrder),

            _ => plan
        };
    }

    // ============================================================================
    // Combine Limits
    // ============================================================================

    private static LogicalPlan CombineLimits(LogicalPlan plan)
    {
        return plan switch
        {
            // Take minimum of consecutive limits
            LogicalPlan.Limit { Input: LogicalPlan.Limit { Input: var inner, N: var n1 }, N: var n2 } =>
                CombineLimits(new LogicalPlan.Limit(inner, Math.Min(n1, n2))),

            // Push limit through sort (can't, need all rows to sort)
            // Push limit through filter (can't, need to filter first)

            // Recursively process
            LogicalPlan.Select s => new LogicalPlan.Select(CombineLimits(s.Input), s.Exprs),
            LogicalPlan.Filter f => new LogicalPlan.Filter(CombineLimits(f.Input), f.Predicate),
            LogicalPlan.Limit l => new LogicalPlan.Limit(CombineLimits(l.Input), l.N),

            _ => plan
        };
    }

    // ============================================================================
    // Helpers
    // ============================================================================

    private static HashSet<string> GetReferencedColumns(Expr expr)
    {
        var columns = new HashSet<string>();
        CollectColumns(expr, columns);
        return columns;
    }

    private static void CollectColumns(Expr expr, HashSet<string> columns)
    {
        switch (expr)
        {
            case Expr.Column col:
                columns.Add(col.Name);
                break;
            case Expr.Alias alias:
                CollectColumns(alias.Inner, columns);
                break;
            case Expr.BinaryOp binop:
                CollectColumns(binop.Left, columns);
                CollectColumns(binop.Right, columns);
                break;
            case Expr.UnaryOp unop:
                CollectColumns(unop.Inner, columns);
                break;
            case Expr.Agg agg:
                CollectColumns(agg.Inner, columns);
                break;
            case Expr.Cast cast:
                CollectColumns(cast.Inner, columns);
                break;
            case Expr.Function func:
                foreach (var arg in func.Args)
                    CollectColumns(arg, columns);
                break;
            case Expr.When whenExpr:
                CollectColumns(whenExpr.Condition, columns);
                CollectColumns(whenExpr.ThenExpr, columns);
                if (whenExpr.OtherwiseExpr is not null)
                    CollectColumns(whenExpr.OtherwiseExpr, columns);
                break;
            case Expr.Sort sort:
                CollectColumns(sort.Inner, columns);
                break;
            case Expr.IsNull isNull:
                CollectColumns(isNull.Inner, columns);
                break;
            case Expr.IsNotNull isNotNull:
                CollectColumns(isNotNull.Inner, columns);
                break;
            case Expr.FillNull fillNull:
                CollectColumns(fillNull.Inner, columns);
                CollectColumns(fillNull.Fill, columns);
                break;
        }
    }
}
