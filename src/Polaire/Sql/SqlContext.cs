// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using SqlParser;
using LazyFrameType = Polaire.LazyFrame.LazyFrame;

namespace Polaire.Sql;

/// <summary>
/// SQL execution context for querying DataFrames.
/// Register DataFrames as tables, then execute SQL queries against them.
/// </summary>
public class SqlContext
{
    private readonly Dictionary<string, DataFrame> _tables = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Register a DataFrame as a named table for SQL queries.
    /// </summary>
    /// <param name="name">Table name to use in SQL queries</param>
    /// <param name="df">DataFrame to register</param>
    public void Register(string name, DataFrame df)
    {
        _tables[name] = df;
    }

    /// <summary>
    /// Unregister a table from the context.
    /// </summary>
    /// <param name="name">Table name to unregister</param>
    public bool Unregister(string name)
    {
        return _tables.Remove(name);
    }

    /// <summary>
    /// Get all registered table names.
    /// </summary>
    public IReadOnlyCollection<string> TableNames => _tables.Keys;

    /// <summary>
    /// Execute a SQL query and return the result as a DataFrame.
    /// </summary>
    /// <param name="query">SQL query string</param>
    /// <returns>DataFrame with query results</returns>
    /// <exception cref="SqlParseException">If the SQL cannot be parsed</exception>
    /// <exception cref="NotSupportedException">If the SQL uses unsupported features</exception>
    public DataFrame Sql(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query cannot be empty", nameof(query));

        // Parse SQL using SqlParser-cs
        var statements = new Parser().ParseSql(query);

        if (statements.Count == 0)
            throw new ArgumentException("No SQL statements found in query");

        if (statements.Count > 1)
            throw new NotSupportedException("Multiple statements not supported; use single SELECT query");

        var statement = statements[0];

        // Convert SQL AST to LogicalPlan
        var plan = SqlToLogicalPlan.Convert(statement, _tables);

        // Execute via LazyFrame
        return new LazyFrameType(plan).Collect();
    }
}

/// <summary>
/// Exception thrown when SQL parsing fails.
/// </summary>
public class SqlParseException : Exception
{
    public SqlParseException(string message) : base(message) { }
    public SqlParseException(string message, Exception inner) : base(message, inner) { }
}
