// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET


using Polaire.Expressions;
using Polaire.IO;

namespace Polaire.LazyFrame;

/// <summary>
/// Represents a logical query plan node.
/// The plan is a tree structure that can be optimized before execution.
/// </summary>
public abstract record LogicalPlan
{
    private LogicalPlan() { } // Sealed hierarchy

    // ============================================================================
    // Source Nodes
    // ============================================================================

    /// <summary>Scan a materialized DataFrame.</summary>
    public sealed record Scan(DataFrame Df) : LogicalPlan
    {
        public override string ToString() => $"Scan(rows={Df.Height}, cols={Df.Width})";
    }

    /// <summary>Scan with projection pushdown (only read needed columns).</summary>
    public sealed record ScanWithProjection(DataFrame Df, string[] Columns) : LogicalPlan
    {
        public override string ToString() => $"Scan(cols=[{string.Join(", ", Columns)}])";
    }

    /// <summary>Scan with predicate pushdown (filter while reading).</summary>
    public sealed record ScanWithPredicate(DataFrame Df, Expr Predicate) : LogicalPlan
    {
        public override string ToString() => $"Scan(predicate={Predicate})";
    }

    // ============================================================================
    // File Source Nodes (Lazy Scanning)
    // ============================================================================

    /// <summary>Scan a CSV file lazily.</summary>
    public sealed record ScanCsv(string Path, CsvOptions Options, string[]? Columns, Expr? Predicate) : LogicalPlan
    {
        public override string ToString() => $"ScanCsv({System.IO.Path.GetFileName(Path)}" +
            (Columns is not null ? $", cols=[{string.Join(", ", Columns)}]" : "") +
            (Predicate is not null ? $", predicate={Predicate}" : "") + ")";
    }

    /// <summary>Scan a Parquet file lazily.</summary>
    public sealed record ScanParquet(string Path, ParquetOptions Options, string[]? Columns, Expr? Predicate) : LogicalPlan
    {
        public override string ToString() => $"ScanParquet({System.IO.Path.GetFileName(Path)}" +
            (Columns is not null ? $", cols=[{string.Join(", ", Columns)}]" : "") +
            (Predicate is not null ? $", predicate={Predicate}" : "") + ")";
    }

    /// <summary>Scan an NDJSON file lazily.</summary>
    public sealed record ScanNdjson(string Path, NdjsonOptions Options, string[]? Columns, Expr? Predicate) : LogicalPlan
    {
        public override string ToString() => $"ScanNdjson({System.IO.Path.GetFileName(Path)}" +
            (Columns is not null ? $", cols=[{string.Join(", ", Columns)}]" : "") +
            (Predicate is not null ? $", predicate={Predicate}" : "") + ")";
    }

    // ============================================================================
    // Projection
    // ============================================================================

    /// <summary>Select specific expressions.</summary>
    public sealed record Select(LogicalPlan Input, Expr[] Exprs) : LogicalPlan
    {
        public override string ToString() => $"Select([{string.Join(", ", Exprs.AsEnumerable())}])";
    }

    /// <summary>Add or replace columns.</summary>
    public sealed record WithColumns(LogicalPlan Input, Expr[] Exprs) : LogicalPlan
    {
        public override string ToString() => $"WithColumns([{string.Join(", ", Exprs.AsEnumerable())}])";
    }

    /// <summary>Drop columns.</summary>
    public sealed record Drop(LogicalPlan Input, string[] Columns) : LogicalPlan
    {
        public override string ToString() => $"Drop([{string.Join(", ", Columns)}])";
    }

    /// <summary>Rename columns.</summary>
    public sealed record Rename(LogicalPlan Input, Dictionary<string, string> Mapping) : LogicalPlan
    {
        public override string ToString() => $"Rename({Mapping.Count} columns)";
    }

    // ============================================================================
    // Selection
    // ============================================================================

    /// <summary>Filter rows based on predicate.</summary>
    public sealed record Filter(LogicalPlan Input, Expr Predicate) : LogicalPlan
    {
        public override string ToString() => $"Filter({Predicate})";
    }

    /// <summary>Limit number of rows.</summary>
    public sealed record Limit(LogicalPlan Input, int N) : LogicalPlan
    {
        public override string ToString() => $"Limit({N})";
    }

    /// <summary>Get last N rows.</summary>
    public sealed record Tail(LogicalPlan Input, int N) : LogicalPlan
    {
        public override string ToString() => $"Tail({N})";
    }

    /// <summary>Slice rows.</summary>
    public sealed record Slice(LogicalPlan Input, int Offset, int Length) : LogicalPlan
    {
        public override string ToString() => $"Slice({Offset}, {Length})";
    }

    /// <summary>Distinct rows.</summary>
    public sealed record Distinct(LogicalPlan Input, string[]? Subset) : LogicalPlan
    {
        public override string ToString() => Subset is null ? "Distinct()" : $"Distinct([{string.Join(", ", Subset)}])";
    }

    // ============================================================================
    // Ordering
    // ============================================================================

    /// <summary>Sort by expressions.</summary>
    public sealed record Sort(LogicalPlan Input, Expr[] Exprs, bool MaintainOrder) : LogicalPlan
    {
        public override string ToString() => $"Sort([{string.Join(", ", Exprs.AsEnumerable())}])";
    }

    // ============================================================================
    // Aggregation
    // ============================================================================

    /// <summary>Group by and aggregate.</summary>
    public sealed record Aggregate(LogicalPlan Input, Expr[] GroupBy, Expr[] Aggs) : LogicalPlan
    {
        public override string ToString() =>
            $"Aggregate(by=[{string.Join(", ", GroupBy.AsEnumerable())}], aggs=[{string.Join(", ", Aggs.AsEnumerable())}])";
    }

    // ============================================================================
    // Joins
    // ============================================================================

    /// <summary>Join two plans.</summary>
    public sealed record Join(
        LogicalPlan Left,
        LogicalPlan Right,
        Expr[] LeftOn,
        Expr[] RightOn,
        JoinType How,
        string Suffix) : LogicalPlan
    {
        public override string ToString() => $"Join({How}, left=[{string.Join(", ", LeftOn.AsEnumerable())}])";
    }

    // ============================================================================
    // Set Operations
    // ============================================================================

    /// <summary>Union of two plans.</summary>
    public sealed record Union(LogicalPlan Left, LogicalPlan Right) : LogicalPlan
    {
        public override string ToString() => "Union()";
    }

    // ============================================================================
    // Reshaping
    // ============================================================================

    /// <summary>Explode list column.</summary>
    public sealed record Explode(LogicalPlan Input, string[] Columns) : LogicalPlan
    {
        public override string ToString() => $"Explode([{string.Join(", ", Columns)}])";
    }

    // ============================================================================
    // Caching
    // ============================================================================

    /// <summary>Cache intermediate results.</summary>
    public sealed record Cache(LogicalPlan Input) : LogicalPlan
    {
        public override string ToString() => "Cache()";
    }
}

/// <summary>Prints a logical plan in a readable format.</summary>
public static class PlanPrinter
{
    public static string Print(LogicalPlan plan, int indent = 0)
    {
        var prefix = new string(' ', indent * 2);
        var result = prefix + plan.ToString();

        var children = GetChildren(plan);
        foreach (var child in children)
        {
            result += "\n" + Print(child, indent + 1);
        }

        return result;
    }

    private static LogicalPlan[] GetChildren(LogicalPlan plan) => plan switch
    {
        LogicalPlan.Scan => Array.Empty<LogicalPlan>(),
        LogicalPlan.ScanWithProjection => Array.Empty<LogicalPlan>(),
        LogicalPlan.ScanWithPredicate => Array.Empty<LogicalPlan>(),
        LogicalPlan.ScanCsv => Array.Empty<LogicalPlan>(),
        LogicalPlan.ScanParquet => Array.Empty<LogicalPlan>(),
        LogicalPlan.ScanNdjson => Array.Empty<LogicalPlan>(),
        LogicalPlan.Select s => new[] { s.Input },
        LogicalPlan.WithColumns w => new[] { w.Input },
        LogicalPlan.Filter f => new[] { f.Input },
        LogicalPlan.Sort s => new[] { s.Input },
        LogicalPlan.Limit l => new[] { l.Input },
        LogicalPlan.Tail t => new[] { t.Input },
        LogicalPlan.Slice s => new[] { s.Input },
        LogicalPlan.Drop d => new[] { d.Input },
        LogicalPlan.Rename r => new[] { r.Input },
        LogicalPlan.Distinct d => new[] { d.Input },
        LogicalPlan.Aggregate a => new[] { a.Input },
        LogicalPlan.Explode e => new[] { e.Input },
        LogicalPlan.Cache c => new[] { c.Input },
        LogicalPlan.Join j => new[] { j.Left, j.Right },
        LogicalPlan.Union u => new[] { u.Left, u.Right },
        _ => Array.Empty<LogicalPlan>()
    };
}
