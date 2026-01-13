using Xunit;
// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using FluentAssertions;
using Polaire.DataTypes;


using Polaire.Expressions;
using static Polaire.Expressions.Expr;

namespace Polaire.Tests;

public class ExpressionTests
{
    [Fact]
    public void Expr_Column_ShouldCreateColumnReference()
    {
        var expr = Col("my_column");

        expr.Should().BeOfType<Expr.Column>();
        ((Expr.Column)expr).Name.Should().Be("my_column");
        expr.ToString().Should().Be("col(\"my_column\")");
    }

    [Fact]
    public void Expr_Literal_ShouldCreateLiteralValue()
    {
        var intLit = Lit(42);
        var strLit = Lit("hello");
        var floatLit = Lit(3.14);

        intLit.Should().BeOfType<Expr.Literal>();
        strLit.Should().BeOfType<Expr.Literal>();
        floatLit.Should().BeOfType<Expr.Literal>();
    }

    [Fact]
    public void Expr_Arithmetic_ShouldCreateBinaryOps()
    {
        var a = Col("a");
        var b = Col("b");

        var add = a + b;
        var sub = a - b;
        var mul = a * b;
        var div = a / b;

        add.Should().BeOfType<Expr.BinaryOp>();
        ((Expr.BinaryOp)add).Op.Should().Be(BinaryOperator.Add);
    }

    [Fact]
    public void Expr_ScalarArithmetic_ShouldWork()
    {
        var a = Col("a");

        var addScalar = a + 10.0;
        var mulScalar = a * 2.0;

        addScalar.Should().BeOfType<Expr.BinaryOp>();
        mulScalar.Should().BeOfType<Expr.BinaryOp>();
    }

    [Fact]
    public void Expr_Comparison_ShouldCreateComparisonOps()
    {
        var a = Col("a");

        var eq = a.Eq(10);
        var gt = a.Gt(5);
        var le = a.Le(100);

        eq.Should().BeOfType<Expr.BinaryOp>();
        ((Expr.BinaryOp)eq).Op.Should().Be(BinaryOperator.Equal);
        ((Expr.BinaryOp)gt).Op.Should().Be(BinaryOperator.GreaterThan);
        ((Expr.BinaryOp)le).Op.Should().Be(BinaryOperator.LessEqual);
    }

    [Fact]
    public void Expr_Boolean_ShouldCreateBooleanOps()
    {
        var a = Col("a");
        var b = Col("b");

        var and = a & b;
        var or = a | b;
        var not = !a;

        and.Should().BeOfType<Expr.BinaryOp>();
        or.Should().BeOfType<Expr.BinaryOp>();
        not.Should().BeOfType<Expr.UnaryOp>();
    }

    [Fact]
    public void Expr_Aggregation_ShouldCreateAggOps()
    {
        var a = Col("a");

        var sum = a.Sum();
        var mean = a.Mean();
        var min = a.Min();
        var max = a.Max();

        sum.Should().BeOfType<Expr.Agg>();
        ((Expr.Agg)sum).Type.Should().Be(AggregationType.Sum);
    }

    [Fact]
    public void Expr_Alias_ShouldRenameOutput()
    {
        var expr = Col("a").Sum().As("total");

        expr.Should().BeOfType<Expr.Alias>();
        ((Expr.Alias)expr).Name.Should().Be("total");
    }

    [Fact]
    public void Expr_Cast_ShouldConvertType()
    {
        var expr = Col("a").CastTo(DataType.Float64);

        expr.Should().BeOfType<Expr.Cast>();
        ((Expr.Cast)expr).TargetType.Should().Be(DataType.Float64);
    }

    [Fact]
    public void Expr_When_ThenOtherwise_ShouldCreateConditional()
    {
        var condition = Col("a").Gt(10);
        var expr = Expr.WhenExpr(condition).Then("big").Otherwise("small");

        expr.Should().BeOfType<Expr.When>();
    }

    [Fact]
    public void Expr_Sort_ShouldCreateSortExpr()
    {
        var expr = Col("a").SortBy(descending: true);

        expr.Should().BeOfType<Expr.Sort>();
        ((Expr.Sort)expr).Descending.Should().BeTrue();
    }

    [Fact]
    public void Expr_IsNull_ShouldCreateNullCheck()
    {
        var expr = Col("a").IsNullExpr();

        expr.Should().BeOfType<Expr.IsNull>();
    }

    [Fact]
    public void Expr_FillNull_ShouldCreateFillOp()
    {
        var expr = Col("a").FillNullWith(0);

        expr.Should().BeOfType<Expr.FillNull>();
    }

    [Fact]
    public void Expr_Evaluate_Column_ShouldReturnSeries()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 }),
            Series.FromValues("b", new[] { 4, 5, 6 })
        );

        var result = ExprEvaluator.Evaluate(Col("a"), df);

        result.Name.Should().Be("a");
        result.Length.Should().Be(3);
        result[0].AsInt32().Should().Be(1);
    }

    [Fact]
    public void Expr_Evaluate_Literal_ShouldBroadcast()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 })
        );

        var result = ExprEvaluator.Evaluate(Lit(42), df);

        result.Length.Should().Be(3);
        result[0].AsInt32().Should().Be(42);
        result[1].AsInt32().Should().Be(42);
        result[2].AsInt32().Should().Be(42);
    }

    [Fact]
    public void Expr_Evaluate_BinaryOp_ShouldCompute()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1.0, 2.0, 3.0 }),
            Series.FromValues("b", new[] { 10.0, 20.0, 30.0 })
        );

        var result = ExprEvaluator.Evaluate(Col("a") + Col("b"), df);

        result[0].AsFloat64().Should().Be(11.0);
        result[1].AsFloat64().Should().Be(22.0);
        result[2].AsFloat64().Should().Be(33.0);
    }

    [Fact]
    public void Expr_Evaluate_Comparison_ShouldReturnBoolean()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 })
        );

        var result = ExprEvaluator.Evaluate(Col("a").Gt(3), df);

        result.DataType.Should().Be(DataType.Boolean);
        result[2].AsBoolean().Should().BeFalse(); // 3 > 3 is false
        result[3].AsBoolean().Should().BeTrue();  // 4 > 3 is true
    }

    [Fact]
    public void Expr_Evaluate_Aggregation_ShouldComputeScalar()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 })
        );

        var result = ExprEvaluator.Evaluate(Col("a").Sum(), df);

        result.Length.Should().Be(1);
        result[0].AsInt64().Should().Be(15);
    }

    [Fact]
    public void Expr_Evaluate_Alias_ShouldRename()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 })
        );

        var result = ExprEvaluator.Evaluate(Col("a").As("x"), df);

        result.Name.Should().Be("x");
    }

    [Fact]
    public void Expr_Evaluate_Cast_ShouldConvert()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 })
        );

        var result = ExprEvaluator.Evaluate(Col("a").CastTo(DataType.Float64), df);

        result.DataType.Should().Be(DataType.Float64);
        result[0].AsFloat64().Should().Be(1.0);
    }

    [Fact]
    public void Expr_Evaluate_When_ShouldApplyConditional()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 })
        );

        var expr = Expr.WhenExpr(Col("a").Gt(3)).Then(1).Otherwise(0);
        var result = ExprEvaluator.Evaluate(expr, df);

        result[2].AsInt32().Should().Be(0); // 3 <= 3
        result[3].AsInt32().Should().Be(1); // 4 > 3
        result[4].AsInt32().Should().Be(1); // 5 > 3
    }

    [Fact]
    public void Expr_Evaluate_NullCheck_ShouldDetectNulls()
    {
        var df = new DataFrame(
            Series.FromNullable("a", new int?[] { 1, null, 3, null, 5 })
        );

        var result = ExprEvaluator.Evaluate(Col("a").IsNullExpr(), df);

        result[0].AsBoolean().Should().BeFalse();
        result[1].AsBoolean().Should().BeTrue();
        result[2].AsBoolean().Should().BeFalse();
        result[3].AsBoolean().Should().BeTrue();
    }

    [Fact]
    public void Expr_Chaining_ShouldWork()
    {
        // Test complex expression chaining like in Polars
        var expr = (Col("price") * Col("quantity"))
            .As("total")
            .CastTo(DataType.Float64);

        expr.Should().BeOfType<Expr.Cast>();

        // Extract inner alias
        var cast = (Expr.Cast)expr;
        cast.Inner.Should().BeOfType<Expr.Alias>();

        var alias = (Expr.Alias)cast.Inner;
        alias.Name.Should().Be("total");
        alias.Inner.Should().BeOfType<Expr.BinaryOp>();
    }

    [Fact]
    public void Expr_ToString_ShouldBeReadable()
    {
        var expr = (Col("a") + Col("b")).As("sum");

        var str = expr.ToString();

        str.Should().Contain("col(\"a\")");
        str.Should().Contain("+");
        str.Should().Contain("col(\"b\")");
        str.Should().Contain("alias");
    }
}
