// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET


using Polaire.Expressions;
using Polaire.Compute;


namespace Polaire.LazyFrame;

/// <summary>
/// Executes optimized logical plans to produce DataFrames.
/// </summary>
public static class PlanExecutor
{
    /// <summary>Executes a logical plan.</summary>
    public static DataFrame Execute(LogicalPlan plan)
    {
        return plan switch
        {
            LogicalPlan.Scan scan => scan.Df,
            LogicalPlan.ScanWithProjection scanProj => ExecuteScanWithProjection(scanProj),
            LogicalPlan.ScanWithPredicate scanPred => ExecuteScanWithPredicate(scanPred),
            LogicalPlan.Select select => ExecuteSelect(select),
            LogicalPlan.WithColumns withCols => ExecuteWithColumns(withCols),
            LogicalPlan.Filter filter => ExecuteFilter(filter),
            LogicalPlan.Sort sort => ExecuteSort(sort),
            LogicalPlan.Limit limit => ExecuteLimit(limit),
            LogicalPlan.Tail tail => ExecuteTail(tail),
            LogicalPlan.Slice slice => ExecuteSlice(slice),
            LogicalPlan.Drop drop => ExecuteDrop(drop),
            LogicalPlan.Rename rename => ExecuteRename(rename),
            LogicalPlan.Distinct distinct => ExecuteDistinct(distinct),
            LogicalPlan.Aggregate aggregate => ExecuteAggregate(aggregate),
            LogicalPlan.Join join => ExecuteJoin(join),
            LogicalPlan.Union union => ExecuteUnion(union),
            LogicalPlan.Explode explode => ExecuteExplode(explode),
            LogicalPlan.Cache cache => Execute(cache.Input), // Simple cache - just execute
            _ => throw new NotSupportedException($"Plan node not supported: {plan.GetType().Name}")
        };
    }

    // ============================================================================
    // Scan Operations
    // ============================================================================

    private static DataFrame ExecuteScanWithProjection(LogicalPlan.ScanWithProjection plan)
    {
        return plan.Df.Select(plan.Columns);
    }

    private static DataFrame ExecuteScanWithPredicate(LogicalPlan.ScanWithPredicate plan)
    {
        var mask = ExprEvaluator.Evaluate(plan.Predicate, plan.Df);
        return plan.Df.Filter(mask);
    }

    // ============================================================================
    // Projection Operations
    // ============================================================================

    private static DataFrame ExecuteSelect(LogicalPlan.Select plan)
    {
        var input = Execute(plan.Input);
        var resultColumns = ExprEvaluator.Evaluate(plan.Exprs, input);
        return new DataFrame(resultColumns);
    }

    private static DataFrame ExecuteWithColumns(LogicalPlan.WithColumns plan)
    {
        var input = Execute(plan.Input);
        var result = input;

        foreach (var expr in plan.Exprs)
        {
            var col = ExprEvaluator.Evaluate(expr, input);
            result = result.WithColumn(col);
        }

        return result;
    }

    private static DataFrame ExecuteDrop(LogicalPlan.Drop plan)
    {
        var input = Execute(plan.Input);
        return input.Drop(plan.Columns);
    }

    private static DataFrame ExecuteRename(LogicalPlan.Rename plan)
    {
        var input = Execute(plan.Input);
        return input.Rename(plan.Mapping);
    }

    // ============================================================================
    // Selection Operations
    // ============================================================================

    private static DataFrame ExecuteFilter(LogicalPlan.Filter plan)
    {
        var input = Execute(plan.Input);
        var mask = ExprEvaluator.Evaluate(plan.Predicate, input);
        return input.Filter(mask);
    }

    private static DataFrame ExecuteLimit(LogicalPlan.Limit plan)
    {
        var input = Execute(plan.Input);
        return input.Head(plan.N);
    }

    private static DataFrame ExecuteTail(LogicalPlan.Tail plan)
    {
        var input = Execute(plan.Input);
        return input.Tail(plan.N);
    }

    private static DataFrame ExecuteSlice(LogicalPlan.Slice plan)
    {
        var input = Execute(plan.Input);
        return input.Slice(plan.Offset, plan.Length);
    }

    private static DataFrame ExecuteDistinct(LogicalPlan.Distinct plan)
    {
        var input = Execute(plan.Input);
        return plan.Subset is null ? input.Unique() : input.Unique(plan.Subset);
    }

    // ============================================================================
    // Ordering Operations
    // ============================================================================

    private static DataFrame ExecuteSort(LogicalPlan.Sort plan)
    {
        var input = Execute(plan.Input);

        // Extract sort specifications from expressions
        var sortSpecs = new List<(string column, bool descending)>();
        foreach (var expr in plan.Exprs)
        {
            var (colName, descending) = ExtractSortSpec(expr);
            sortSpecs.Add((colName, descending));
        }

        if (sortSpecs.Count == 0)
            return input;

        if (sortSpecs.Count == 1)
            return input.Sort(sortSpecs[0].column, sortSpecs[0].descending);

        return input.Sort(sortSpecs.ToArray());
    }

    private static (string column, bool descending) ExtractSortSpec(Expr expr)
    {
        return expr switch
        {
            Expr.Column col => (col.Name, false),
            Expr.Sort sort when sort.Inner is Expr.Column col => (col.Name, sort.Descending),
            Expr.Sort sort => (ExtractSortSpec(sort.Inner).column, sort.Descending),
            _ => throw new NotSupportedException($"Cannot extract sort spec from {expr}")
        };
    }

    // ============================================================================
    // Aggregation Operations
    // ============================================================================

    private static DataFrame ExecuteAggregate(LogicalPlan.Aggregate plan)
    {
        var input = Execute(plan.Input);

        // Extract group by column names
        var groupCols = plan.GroupBy.Select(e => e switch
        {
            Expr.Column col => col.Name,
            _ => throw new NotSupportedException("Group by expression must be a column reference")
        }).ToArray();

        var groupBy = input.GroupBy(groupCols);

        if (plan.Aggs.Length == 0)
        {
            // Just group by without aggregations
            return groupBy.First();
        }

        // Build aggregation specifications
        var aggSpecs = new List<(string column, string aggName, Func<Series, AnyValue> agg, string? alias)>();

        foreach (var expr in plan.Aggs)
        {
            var (colName, aggName, aggFunc, alias) = ExtractAggSpec(expr);
            aggSpecs.Add((colName, aggName, aggFunc, alias));
        }

        return groupBy.Agg(aggSpecs.ToArray());
    }

    private static (string column, string aggName, Func<Series, AnyValue> agg, string? alias) ExtractAggSpec(Expr expr)
    {
        return expr switch
        {
            Expr.Agg { Type: var aggType, Inner: Expr.Column col } =>
                (col.Name, aggType.ToString().ToLower(), GetAggFunction(aggType), null),
            Expr.Alias { Inner: Expr.Agg { Type: var aggType, Inner: Expr.Column col }, Name: var name } =>
                (col.Name, aggType.ToString().ToLower(), GetAggFunction(aggType), name),
            Expr.Function { Name: "count" } => ("*", "count", s => AnyValue.From(s.Count()), null),
            _ => throw new NotSupportedException($"Cannot extract aggregation from {expr}")
        };
    }

    private static Func<Series, AnyValue> GetAggFunction(AggregationType aggType) => aggType switch
    {
        AggregationType.Sum => s => s.Sum(),
        AggregationType.Mean => s => s.Mean(),
        AggregationType.Median => s => s.Median(),
        AggregationType.Min => s => s.Min(),
        AggregationType.Max => s => s.Max(),
        AggregationType.Std => s => s.Std(),
        AggregationType.Var => s => s.Var(),
        AggregationType.Count => s => AnyValue.From(s.Count()),
        AggregationType.First => s => s.First(),
        AggregationType.Last => s => s.Last(),
        AggregationType.NUnique => s => AnyValue.From(s.Unique().Length),
        _ => throw new NotSupportedException($"Aggregation type not supported: {aggType}")
    };

    // ============================================================================
    // Join Operations
    // ============================================================================

    private static DataFrame ExecuteJoin(LogicalPlan.Join plan)
    {
        var left = Execute(plan.Left);
        var right = Execute(plan.Right);

        var leftOn = plan.LeftOn.Select(e => e switch
        {
            Expr.Column col => col.Name,
            _ => throw new NotSupportedException("Join key must be a column reference")
        }).ToArray();

        var rightOn = plan.RightOn.Select(e => e switch
        {
            Expr.Column col => col.Name,
            _ => throw new NotSupportedException("Join key must be a column reference")
        }).ToArray();

        return left.Join(right, leftOn, rightOn, plan.How, plan.Suffix);
    }

    // ============================================================================
    // Set Operations
    // ============================================================================

    private static DataFrame ExecuteUnion(LogicalPlan.Union plan)
    {
        var left = Execute(plan.Left);
        var right = Execute(plan.Right);
        return DataFrame.VConcat(left, right);
    }

    // ============================================================================
    // Reshaping Operations
    // ============================================================================

    private static DataFrame ExecuteExplode(LogicalPlan.Explode plan)
    {
        var input = Execute(plan.Input);
        // TODO: Implement list explosion
        throw new NotImplementedException("Explode not yet implemented");
    }
}
