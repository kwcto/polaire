// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using Polaire.DataTypes;

using Polaire.Expressions;


namespace Polaire.LazyFrame;

/// <summary>
/// A lazy representation of a DataFrame that builds up a logical plan.
/// Operations on a LazyFrame don't execute until .Collect() is called.
/// This allows for query optimization.
/// </summary>
public sealed class LazyFrame
{
    private readonly LogicalPlan _plan;

    internal LazyFrame(LogicalPlan plan)
    {
        _plan = plan;
    }

    internal LazyFrame(DataFrame df)
    {
        _plan = new LogicalPlan.Scan(df);
    }

    /// <summary>Gets the logical plan.</summary>
    public LogicalPlan Plan => _plan;

    // ============================================================================
    // Static Factory Methods
    // ============================================================================

    /// <summary>Creates a LazyFrame from a DataFrame.</summary>
    public static LazyFrame From(DataFrame df) => new(df);

    // ============================================================================
    // Transformation Operations
    // ============================================================================

    /// <summary>Selects columns using expressions.</summary>
    public LazyFrame Select(params Expr[] exprs)
    {
        return new LazyFrame(new LogicalPlan.Select(_plan, exprs));
    }

    /// <summary>Selects columns by name.</summary>
    public LazyFrame Select(params string[] columns)
    {
        return Select(columns.Select(c => Expr.Col(c)).ToArray());
    }

    /// <summary>Adds or replaces columns.</summary>
    public LazyFrame WithColumns(params Expr[] exprs)
    {
        return new LazyFrame(new LogicalPlan.WithColumns(_plan, exprs));
    }

    /// <summary>Filters rows based on a predicate.</summary>
    public LazyFrame Filter(Expr predicate)
    {
        return new LazyFrame(new LogicalPlan.Filter(_plan, predicate));
    }

    /// <summary>Groups by columns for aggregation.</summary>
    public LazyGroupBy GroupBy(params Expr[] exprs)
    {
        return new LazyGroupBy(this, exprs);
    }

    /// <summary>Groups by column names.</summary>
    public LazyGroupBy GroupBy(params string[] columns)
    {
        return GroupBy(columns.Select(c => Expr.Col(c)).ToArray());
    }

    /// <summary>Sorts by expressions.</summary>
    public LazyFrame Sort(params Expr[] exprs)
    {
        return new LazyFrame(new LogicalPlan.Sort(_plan, exprs, false));
    }

    /// <summary>Sorts by expressions with direction.</summary>
    public LazyFrame Sort(params (Expr expr, bool descending)[] exprs)
    {
        var sortExprs = exprs.Select(e => e.descending ? e.expr.SortBy(true) : e.expr).ToArray();
        return new LazyFrame(new LogicalPlan.Sort(_plan, sortExprs, false));
    }

    /// <summary>Sorts by column names.</summary>
    public LazyFrame Sort(params string[] columns)
    {
        return Sort(columns.Select(c => Expr.Col(c)).ToArray());
    }

    /// <summary>Gets the first n rows.</summary>
    public LazyFrame Head(int n)
    {
        return new LazyFrame(new LogicalPlan.Limit(_plan, n));
    }

    /// <summary>Gets the last n rows.</summary>
    public LazyFrame Tail(int n)
    {
        return new LazyFrame(new LogicalPlan.Tail(_plan, n));
    }

    /// <summary>Slices rows.</summary>
    public LazyFrame Slice(int offset, int length)
    {
        return new LazyFrame(new LogicalPlan.Slice(_plan, offset, length));
    }

    /// <summary>Drops columns.</summary>
    public LazyFrame Drop(params string[] columns)
    {
        return new LazyFrame(new LogicalPlan.Drop(_plan, columns));
    }

    /// <summary>Renames columns.</summary>
    public LazyFrame Rename(Dictionary<string, string> mapping)
    {
        return new LazyFrame(new LogicalPlan.Rename(_plan, mapping));
    }

    /// <summary>Gets distinct rows.</summary>
    public LazyFrame Distinct()
    {
        return new LazyFrame(new LogicalPlan.Distinct(_plan, null));
    }

    /// <summary>Gets distinct rows based on subset of columns.</summary>
    public LazyFrame Distinct(params string[] subset)
    {
        return new LazyFrame(new LogicalPlan.Distinct(_plan, subset));
    }

    /// <summary>Explodes a list column into multiple rows.</summary>
    public LazyFrame Explode(params string[] columns)
    {
        return new LazyFrame(new LogicalPlan.Explode(_plan, columns));
    }

    // ============================================================================
    // Join Operations
    // ============================================================================

    /// <summary>Inner join with another LazyFrame.</summary>
    public LazyFrame Join(LazyFrame other, Expr leftOn, Expr rightOn, string suffix = "_right")
    {
        return new LazyFrame(new LogicalPlan.Join(_plan, other._plan, new[] { leftOn }, new[] { rightOn }, JoinType.Inner, suffix));
    }

    /// <summary>Inner join on same column name.</summary>
    public LazyFrame Join(LazyFrame other, string on, string suffix = "_right")
    {
        return Join(other, Expr.Col(on), Expr.Col(on), suffix);
    }

    /// <summary>Left join.</summary>
    public LazyFrame LeftJoin(LazyFrame other, string on, string suffix = "_right")
    {
        return new LazyFrame(new LogicalPlan.Join(_plan, other._plan, new[] { Expr.Col(on) }, new[] { Expr.Col(on) }, JoinType.Left, suffix));
    }

    /// <summary>Outer join.</summary>
    public LazyFrame OuterJoin(LazyFrame other, string on, string suffix = "_right")
    {
        return new LazyFrame(new LogicalPlan.Join(_plan, other._plan, new[] { Expr.Col(on) }, new[] { Expr.Col(on) }, JoinType.Outer, suffix));
    }

    // ============================================================================
    // Set Operations
    // ============================================================================

    /// <summary>Union with another LazyFrame.</summary>
    public LazyFrame Union(LazyFrame other)
    {
        return new LazyFrame(new LogicalPlan.Union(_plan, other._plan));
    }

    /// <summary>Concatenates LazyFrames vertically.</summary>
    public static LazyFrame Concat(params LazyFrame[] frames)
    {
        if (frames.Length == 0)
            throw new ArgumentException("At least one LazyFrame required");

        var result = frames[0];
        for (int i = 1; i < frames.Length; i++)
        {
            result = result.Union(frames[i]);
        }
        return result;
    }

    // ============================================================================
    // Execution
    // ============================================================================

    /// <summary>Executes the query and returns a DataFrame.</summary>
    public DataFrame Collect()
    {
        // Optimize the plan first
        var optimizedPlan = QueryOptimizer.Optimize(_plan);

        // Execute the optimized plan
        return PlanExecutor.Execute(optimizedPlan);
    }

    /// <summary>Executes the query and returns first n rows.</summary>
    public DataFrame Fetch(int n = 500)
    {
        return Head(n).Collect();
    }

    /// <summary>Returns the schema without executing.</summary>
    public IReadOnlyList<(string Name, DataType Type)> Schema()
    {
        return GetSchema(_plan);
    }

    private static IReadOnlyList<(string Name, DataType Type)> GetSchema(LogicalPlan plan)
    {
        return plan switch
        {
            LogicalPlan.Scan scan => scan.Df.Schema,
            LogicalPlan.Select select => InferSelectSchema(select),
            LogicalPlan.Filter filter => GetSchema(filter.Input),
            LogicalPlan.Sort sort => GetSchema(sort.Input),
            LogicalPlan.Limit limit => GetSchema(limit.Input),
            LogicalPlan.Drop drop => GetSchema(drop.Input)
                .Where(c => !drop.Columns.Contains(c.Name)).ToList(),
            _ => Array.Empty<(string, DataType)>()
        };
    }

    private static IReadOnlyList<(string Name, DataType Type)> InferSelectSchema(LogicalPlan.Select select)
    {
        // Simplified schema inference - would need full type inference in production
        var inputSchema = GetSchema(select.Input);
        return select.Exprs.Select(e => InferExprSchema(e, inputSchema)).ToList();
    }

    private static (string Name, DataType Type) InferExprSchema(Expr expr, IReadOnlyList<(string Name, DataType Type)> inputSchema)
    {
        return expr switch
        {
            Expr.Column col => inputSchema.FirstOrDefault(c => c.Name == col.Name),
            Expr.Alias alias => (alias.Name, InferExprSchema(alias.Inner, inputSchema).Type),
            Expr.Literal lit => ("literal", InferLiteralType(lit.Value)),
            Expr.BinaryOp binop => (InferExprSchema(binop.Left, inputSchema).Name, InferBinaryOpType(binop, inputSchema)),
            Expr.Agg agg => (InferExprSchema(agg.Inner, inputSchema).Name, InferAggType(agg.Type)),
            Expr.Cast cast => (InferExprSchema(cast.Inner, inputSchema).Name, cast.TargetType),
            _ => ("unknown", DataType.Unknown)
        };
    }

    private static DataType InferLiteralType(AnyValue value) => value.Kind switch
    {
        AnyValueKind.Int32 => DataType.Int32,
        AnyValueKind.Int64 => DataType.Int64,
        AnyValueKind.Float32 => DataType.Float32,
        AnyValueKind.Float64 => DataType.Float64,
        AnyValueKind.Boolean => DataType.Boolean,
        AnyValueKind.String => DataType.String,
        _ => DataType.Unknown
    };

    private static DataType InferBinaryOpType(Expr.BinaryOp binop, IReadOnlyList<(string Name, DataType Type)> inputSchema)
    {
        var leftType = InferExprSchema(binop.Left, inputSchema).Type;
        var rightType = InferExprSchema(binop.Right, inputSchema).Type;

        return binop.Op switch
        {
            BinaryOperator.Equal or BinaryOperator.NotEqual or BinaryOperator.LessThan
                or BinaryOperator.LessEqual or BinaryOperator.GreaterThan or BinaryOperator.GreaterEqual
                or BinaryOperator.And or BinaryOperator.Or => DataType.Boolean,
            _ when leftType is DataType.Float64Type || rightType is DataType.Float64Type => DataType.Float64,
            _ when leftType is DataType.Int64Type || rightType is DataType.Int64Type => DataType.Int64,
            _ => leftType
        };
    }

    private static DataType InferAggType(AggregationType aggType) => aggType switch
    {
        AggregationType.Count or AggregationType.NUnique => DataType.Int64,
        AggregationType.Mean or AggregationType.Std or AggregationType.Var or AggregationType.Median => DataType.Float64,
        _ => DataType.Unknown
    };

    // ============================================================================
    // Debugging
    // ============================================================================

    /// <summary>Explains the logical plan.</summary>
    public string Explain(bool optimized = true)
    {
        var plan = optimized ? QueryOptimizer.Optimize(_plan) : _plan;
        return PlanPrinter.Print(plan);
    }

    public override string ToString() => Explain(false);
}

/// <summary>Lazy group by operation.</summary>
public sealed class LazyGroupBy
{
    private readonly LazyFrame _lf;
    private readonly Expr[] _groupExprs;

    internal LazyGroupBy(LazyFrame lf, Expr[] groupExprs)
    {
        _lf = lf;
        _groupExprs = groupExprs;
    }

    /// <summary>Aggregates the grouped data.</summary>
    public LazyFrame Agg(params Expr[] exprs)
    {
        return new LazyFrame(new LogicalPlan.Aggregate(_lf.Plan, _groupExprs, exprs));
    }

    /// <summary>Counts rows per group.</summary>
    public LazyFrame Count()
    {
        return Agg(Expr.Count().As("count"));
    }

    /// <summary>Gets first row per group.</summary>
    public LazyFrame First()
    {
        // Select all columns with first aggregation
        return Agg();
    }
}
