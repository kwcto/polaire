// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using Xunit;
using FluentAssertions;
using Polaire.Sql;

namespace Polaire.Tests;

public class SqlTests
{
    #region Basic SELECT

    [Fact]
    public void Sql_SelectAll_ShouldReturnAllColumns()
    {
        var df = new DataFrame(
            Series.FromValues("name", new[] { "Alice", "Bob", "Charlie" }),
            Series.FromValues("age", new[] { 30, 25, 35 })
        );

        var result = df.Sql("SELECT * FROM self");

        result.Height.Should().Be(3);
        result.Width.Should().Be(2);
    }

    [Fact]
    public void Sql_SelectColumns_ShouldReturnSelectedColumns()
    {
        var df = new DataFrame(
            Series.FromValues("name", new[] { "Alice", "Bob", "Charlie" }),
            Series.FromValues("age", new[] { 30, 25, 35 }),
            Series.FromValues("salary", new[] { 50000.0, 60000.0, 55000.0 })
        );

        var result = df.Sql("SELECT name, age FROM self");

        result.Width.Should().Be(2);
        result.Columns.Should().Contain("name");
        result.Columns.Should().Contain("age");
        result.Columns.Should().NotContain("salary");
    }

    [Fact]
    public void Sql_SelectWithAlias_ShouldUseAlias()
    {
        var df = new DataFrame(
            Series.FromValues("name", new[] { "Alice", "Bob" }),
            Series.FromValues("salary", new[] { 50000.0, 60000.0 })
        );

        var result = df.Sql("SELECT name AS employee_name, salary AS pay FROM self");

        result.Columns.Should().Contain("employee_name");
        result.Columns.Should().Contain("pay");
    }

    #endregion

    #region WHERE clause

    [Fact]
    public void Sql_WhereEquals_ShouldFilterRows()
    {
        var df = new DataFrame(
            Series.FromValues("name", new[] { "Alice", "Bob", "Charlie" }),
            Series.FromValues("age", new[] { 30, 25, 35 })
        );

        var result = df.Sql("SELECT * FROM self WHERE age = 30");

        result.Height.Should().Be(1);
        result["name"][0].Should().Be("Alice");
    }

    [Fact]
    public void Sql_WhereGreaterThan_ShouldFilterRows()
    {
        var df = new DataFrame(
            Series.FromValues("name", new[] { "Alice", "Bob", "Charlie" }),
            Series.FromValues("age", new[] { 30, 25, 35 })
        );

        var result = df.Sql("SELECT * FROM self WHERE age > 28");

        result.Height.Should().Be(2);
    }

    [Fact]
    public void Sql_WhereLessThan_ShouldFilterRows()
    {
        var df = new DataFrame(
            Series.FromValues("name", new[] { "Alice", "Bob", "Charlie" }),
            Series.FromValues("age", new[] { 30, 25, 35 })
        );

        var result = df.Sql("SELECT * FROM self WHERE age < 30");

        result.Height.Should().Be(1);
        result["name"][0].Should().Be("Bob");
    }

    [Fact]
    public void Sql_WhereAnd_ShouldCombineConditions()
    {
        var df = new DataFrame(
            Series.FromValues("name", new[] { "Alice", "Bob", "Charlie", "Diana" }),
            Series.FromValues("age", new[] { 30, 25, 35, 28 }),
            Series.FromValues("salary", new[] { 50000.0, 60000.0, 55000.0, 70000.0 })
        );

        var result = df.Sql("SELECT * FROM self WHERE age > 25 AND salary > 55000");

        result.Height.Should().Be(1);
        result["name"][0].Should().Be("Diana");
    }

    [Fact]
    public void Sql_WhereOr_ShouldCombineConditions()
    {
        var df = new DataFrame(
            Series.FromValues("name", new[] { "Alice", "Bob", "Charlie" }),
            Series.FromValues("age", new[] { 30, 25, 35 })
        );

        var result = df.Sql("SELECT * FROM self WHERE age = 25 OR age = 35");

        result.Height.Should().Be(2);
    }

    #endregion

    #region Arithmetic expressions

    [Fact]
    public void Sql_ArithmeticAdd_ShouldCompute()
    {
        var df = new DataFrame(
            Series.FromValues("x", new[] { 10, 20, 30 })
        );

        var result = df.Sql("SELECT x + 5 AS result FROM self");

        result["result"][0].AsInt64().Should().Be(15);
        result["result"][1].AsInt64().Should().Be(25);
        result["result"][2].AsInt64().Should().Be(35);
    }

    [Fact]
    public void Sql_ArithmeticMultiply_ShouldCompute()
    {
        var df = new DataFrame(
            Series.FromValues("salary", new[] { 50000.0, 60000.0 })
        );

        var result = df.Sql("SELECT salary * 1.1 AS adjusted FROM self");

        result["adjusted"][0].AsFloat64().Should().BeApproximately(55000.0, 0.01);
        result["adjusted"][1].AsFloat64().Should().BeApproximately(66000.0, 0.01);
    }

    [Fact]
    public void Sql_ArithmeticDivide_ShouldCompute()
    {
        var df = new DataFrame(
            Series.FromValues("total", new[] { 100.0, 200.0, 150.0 })
        );

        var result = df.Sql("SELECT total / 2 AS half FROM self");

        result["half"][0].AsFloat64().Should().Be(50.0);
        result["half"][1].AsFloat64().Should().Be(100.0);
        result["half"][2].AsFloat64().Should().Be(75.0);
    }

    #endregion

    #region Aggregations

    [Fact]
    public void Sql_Sum_ShouldComputeSum()
    {
        var df = new DataFrame(
            Series.FromValues("amount", new[] { 10, 20, 30 })
        );

        var result = df.Sql("SELECT SUM(amount) AS total FROM self");

        result.Height.Should().Be(1);
        result["total"][0].AsInt64().Should().Be(60);
    }

    [Fact]
    public void Sql_Avg_ShouldComputeAverage()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 10.0, 20.0, 30.0 })
        );

        var result = df.Sql("SELECT AVG(value) AS avg_value FROM self");

        result["avg_value"][0].AsFloat64().Should().Be(20.0);
    }

    [Fact]
    public void Sql_Min_ShouldComputeMin()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 30, 10, 20 })
        );

        var result = df.Sql("SELECT MIN(value) AS min_value FROM self");

        result["min_value"][0].AsInt32().Should().Be(10);
    }

    [Fact]
    public void Sql_Max_ShouldComputeMax()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 30, 10, 20 })
        );

        var result = df.Sql("SELECT MAX(value) AS max_value FROM self");

        result["max_value"][0].AsInt32().Should().Be(30);
    }

    [Fact]
    public void Sql_Count_ShouldCountRows()
    {
        var df = new DataFrame(
            Series.FromValues("name", new[] { "Alice", "Bob", "Charlie" })
        );

        var result = df.Sql("SELECT COUNT(*) AS total FROM self");

        result["total"][0].AsInt32().Should().Be(3);
    }

    #endregion

    #region GROUP BY

    [Fact]
    public void Sql_GroupBy_ShouldGroupAndAggregate()
    {
        var df = new DataFrame(
            Series.FromValues("category", new[] { "A", "B", "A", "B", "A" }),
            Series.FromValues("amount", new[] { 10, 20, 30, 40, 50 })
        );

        var result = df.Sql("SELECT category, SUM(amount) AS total FROM self GROUP BY category");

        result.Height.Should().Be(2);
    }

    [Fact]
    public void Sql_GroupByMultipleAggregations_ShouldWork()
    {
        var df = new DataFrame(
            Series.FromValues("dept", new[] { "Sales", "IT", "Sales", "IT" }),
            Series.FromValues("salary", new[] { 50000.0, 60000.0, 55000.0, 70000.0 })
        );

        var result = df.Sql("SELECT dept, AVG(salary) AS avg_sal, MAX(salary) AS max_sal FROM self GROUP BY dept");

        result.Height.Should().Be(2);
        result.Columns.Should().Contain("avg_sal");
        result.Columns.Should().Contain("max_sal");
    }

    #endregion

    #region ORDER BY

    [Fact]
    public void Sql_OrderByAsc_ShouldSortAscending()
    {
        var df = new DataFrame(
            Series.FromValues("name", new[] { "Charlie", "Alice", "Bob" }),
            Series.FromValues("age", new[] { 35, 30, 25 })
        );

        var result = df.Sql("SELECT * FROM self ORDER BY age ASC");

        result["age"][0].AsInt32().Should().Be(25);
        result["age"][1].AsInt32().Should().Be(30);
        result["age"][2].AsInt32().Should().Be(35);
    }

    [Fact]
    public void Sql_OrderByDesc_ShouldSortDescending()
    {
        var df = new DataFrame(
            Series.FromValues("name", new[] { "Alice", "Bob", "Charlie" }),
            Series.FromValues("age", new[] { 30, 25, 35 })
        );

        var result = df.Sql("SELECT * FROM self ORDER BY age DESC");

        result["age"][0].AsInt32().Should().Be(35);
        result["age"][1].AsInt32().Should().Be(30);
        result["age"][2].AsInt32().Should().Be(25);
    }

    #endregion

    #region LIMIT

    [Fact]
    public void Sql_Limit_ShouldLimitRows()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 1, 2, 3, 4, 5 })
        );

        var result = df.Sql("SELECT * FROM self LIMIT 3");

        result.Height.Should().Be(3);
    }

    [Fact]
    public void Sql_LimitWithOrderBy_ShouldWork()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 5, 3, 1, 4, 2 })
        );

        var result = df.Sql("SELECT * FROM self ORDER BY value DESC LIMIT 2");

        result.Height.Should().Be(2);
        result["value"][0].AsInt32().Should().Be(5);
        result["value"][1].AsInt32().Should().Be(4);
    }

    #endregion

    #region NULL handling

    [Fact]
    public void Sql_IsNull_ShouldFilterNulls()
    {
        var df = new DataFrame(
            Series.FromNullable("value", new int?[] { 1, null, 3, null, 5 })
        );

        var result = df.Sql("SELECT * FROM self WHERE value IS NULL");

        result.Height.Should().Be(2);
    }

    [Fact]
    public void Sql_IsNotNull_ShouldFilterNonNulls()
    {
        var df = new DataFrame(
            Series.FromNullable("value", new int?[] { 1, null, 3, null, 5 })
        );

        var result = df.Sql("SELECT * FROM self WHERE value IS NOT NULL");

        result.Height.Should().Be(3);
    }

    #endregion

    #region DISTINCT

    [Fact]
    public void Sql_Distinct_ShouldRemoveDuplicates()
    {
        var df = new DataFrame(
            Series.FromValues("category", new[] { "A", "B", "A", "C", "B", "A" })
        );

        var result = df.Sql("SELECT DISTINCT category FROM self");

        result.Height.Should().Be(3);
    }

    #endregion

    #region BETWEEN and IN

    [Fact]
    public void Sql_Between_ShouldFilterRange()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 1, 5, 10, 15, 20 })
        );

        var result = df.Sql("SELECT * FROM self WHERE value BETWEEN 5 AND 15");

        result.Height.Should().Be(3);
    }

    [Fact]
    public void Sql_In_ShouldFilterSet()
    {
        var df = new DataFrame(
            Series.FromValues("name", new[] { "Alice", "Bob", "Charlie", "Diana" })
        );

        var result = df.Sql("SELECT * FROM self WHERE name IN ('Alice', 'Charlie')");

        result.Height.Should().Be(2);
    }

    #endregion

    #region LIKE

    [Fact]
    public void Sql_LikeStartsWith_ShouldMatch()
    {
        var df = new DataFrame(
            Series.FromValues("name", new[] { "Alice", "Bob", "Alexandra", "Charlie" })
        );

        var result = df.Sql("SELECT * FROM self WHERE name LIKE 'Al%'");

        result.Height.Should().Be(2);
    }

    [Fact]
    public void Sql_LikeEndsWith_ShouldMatch()
    {
        var df = new DataFrame(
            Series.FromValues("name", new[] { "alice", "Bob", "prince", "nice" })
        );

        var result = df.Sql("SELECT * FROM self WHERE name LIKE '%ice'");

        // alice, nice end with 'ice' (prince ends with 'nce')
        result.Height.Should().Be(2);
    }

    [Fact]
    public void Sql_LikeContains_ShouldMatch()
    {
        var df = new DataFrame(
            Series.FromValues("name", new[] { "Alice", "Bob", "Alexandra", "Charlie" })
        );

        var result = df.Sql("SELECT * FROM self WHERE name LIKE '%ob%'");

        result.Height.Should().Be(1);
        result["name"][0].Should().Be("Bob");
    }

    #endregion

    #region SqlContext

    [Fact]
    public void SqlContext_RegisterAndQuery_ShouldWork()
    {
        var users = new DataFrame(
            Series.FromValues("id", new[] { 1, 2, 3 }),
            Series.FromValues("name", new[] { "Alice", "Bob", "Charlie" })
        );

        var orders = new DataFrame(
            Series.FromValues("order_id", new[] { 101, 102, 103 }),
            Series.FromValues("user_id", new[] { 1, 2, 1 }),
            Series.FromValues("amount", new[] { 100.0, 200.0, 150.0 })
        );

        var ctx = new SqlContext();
        ctx.Register("users", users);
        ctx.Register("orders", orders);

        var result = ctx.Sql("SELECT * FROM orders WHERE amount > 100");

        result.Height.Should().Be(2);
    }

    [Fact]
    public void SqlContext_CaseInsensitiveTableNames_ShouldWork()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 1, 2, 3 })
        );

        var ctx = new SqlContext();
        ctx.Register("MyTable", df);

        var result = ctx.Sql("SELECT * FROM mytable");

        result.Height.Should().Be(3);
    }

    [Fact]
    public void SqlContext_TableNames_ShouldListRegisteredTables()
    {
        var ctx = new SqlContext();
        ctx.Register("users", new DataFrame(Series.FromValues("id", new[] { 1 })));
        ctx.Register("orders", new DataFrame(Series.FromValues("id", new[] { 1 })));

        ctx.TableNames.Should().Contain("users");
        ctx.TableNames.Should().Contain("orders");
        ctx.TableNames.Count.Should().Be(2);
    }

    [Fact]
    public void SqlContext_Unregister_ShouldRemoveTable()
    {
        var ctx = new SqlContext();
        ctx.Register("temp", new DataFrame(Series.FromValues("id", new[] { 1 })));

        ctx.TableNames.Should().Contain("temp");

        ctx.Unregister("temp");

        ctx.TableNames.Should().NotContain("temp");
    }

    #endregion

    #region JOIN

    [Fact]
    public void SqlContext_InnerJoin_ShouldJoinTables()
    {
        var users = new DataFrame(
            Series.FromValues("id", new[] { 1, 2, 3 }),
            Series.FromValues("name", new[] { "Alice", "Bob", "Charlie" })
        );

        var orders = new DataFrame(
            Series.FromValues("user_id", new[] { 1, 2, 1 }),
            Series.FromValues("amount", new[] { 100.0, 200.0, 150.0 })
        );

        var ctx = new SqlContext();
        ctx.Register("users", users);
        ctx.Register("orders", orders);

        var result = ctx.Sql("SELECT users.name, orders.amount FROM users JOIN orders ON users.id = orders.user_id");

        result.Height.Should().Be(3);
    }

    #endregion

    #region CASE WHEN

    [Fact]
    public void Sql_CaseWhen_ShouldEvaluateConditions()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 10, 50, 100 })
        );

        var result = df.Sql(@"
            SELECT value,
                   CASE
                       WHEN value < 25 THEN 'low'
                       WHEN value < 75 THEN 'medium'
                       ELSE 'high'
                   END AS category
            FROM self
        ");

        result["category"][0].Should().Be("low");
        result["category"][1].Should().Be("medium");
        result["category"][2].Should().Be("high");
    }

    #endregion

    #region String Functions

    [Fact]
    public void Sql_Upper_ShouldConvertToUppercase()
    {
        var df = new DataFrame(
            Series.FromValues("name", new[] { "alice", "Bob", "CHARLIE" })
        );

        var result = df.Sql("SELECT UPPER(name) AS upper_name FROM self");

        result["upper_name"][0].Should().Be("ALICE");
        result["upper_name"][1].Should().Be("BOB");
        result["upper_name"][2].Should().Be("CHARLIE");
    }

    [Fact]
    public void Sql_Lower_ShouldConvertToLowercase()
    {
        var df = new DataFrame(
            Series.FromValues("name", new[] { "ALICE", "Bob", "charlie" })
        );

        var result = df.Sql("SELECT LOWER(name) AS lower_name FROM self");

        result["lower_name"][0].Should().Be("alice");
        result["lower_name"][1].Should().Be("bob");
        result["lower_name"][2].Should().Be("charlie");
    }

    #endregion

    #region Math Functions

    [Fact]
    public void Sql_Abs_ShouldComputeAbsoluteValue()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { -10, 20, -30 })
        );

        var result = df.Sql("SELECT ABS(value) AS abs_value FROM self");

        result["abs_value"][0].AsInt32().Should().Be(10);
        result["abs_value"][1].AsInt32().Should().Be(20);
        result["abs_value"][2].AsInt32().Should().Be(30);
    }

    [Fact]
    public void Sql_Sqrt_ShouldComputeSquareRoot()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 4.0, 9.0, 16.0 })
        );

        var result = df.Sql("SELECT SQRT(value) AS sqrt_value FROM self");

        result["sqrt_value"][0].AsFloat64().Should().BeApproximately(2.0, 0.001);
        result["sqrt_value"][1].AsFloat64().Should().BeApproximately(3.0, 0.001);
        result["sqrt_value"][2].AsFloat64().Should().BeApproximately(4.0, 0.001);
    }

    #endregion

    #region Error handling

    [Fact]
    public void Sql_EmptyQuery_ShouldThrow()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 1, 2, 3 })
        );

        var act = () => df.Sql("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Sql_InvalidTableName_ShouldThrow()
    {
        var ctx = new SqlContext();
        ctx.Register("users", new DataFrame(Series.FromValues("id", new[] { 1 })));

        var act = () => ctx.Sql("SELECT * FROM nonexistent");

        act.Should().Throw<ArgumentException>().WithMessage("*Table not found*");
    }

    #endregion
}
