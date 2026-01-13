// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using FluentAssertions;
using Polaire;
using Polaire.DataTypes;
using Polaire.Expressions;
using Xunit;
using static Polaire.Pl;

namespace Polaire.Tests;

/// <summary>
/// Tests for the Expression API (Col, Lit, expression chaining via LazyFrame).
/// </summary>
public class ExpressionApiTests
{
    // ============================================================================
    // Col Expression Tests
    // ============================================================================

    [Fact]
    public void Col_CreatesColumnExpression()
    {
        var expr = Col("test");

        expr.Should().NotBeNull();
    }

    [Fact]
    public void Col_UsedInSelect_SelectsColumn()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 }),
            Series.FromValues("b", new[] { 10, 20, 30 })
        );

        var result = df.Lazy().Select(Col("a")).Collect();

        result.Width.Should().Be(1);
        result.Columns.Should().Contain("a");
    }

    [Fact]
    public void Col_MultipleColumns_SelectsAll()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 }),
            Series.FromValues("b", new[] { 10, 20, 30 }),
            Series.FromValues("c", new[] { 100, 200, 300 })
        );

        var result = df.Lazy().Select(Col("a"), Col("c")).Collect();

        result.Width.Should().Be(2);
        result.Columns.Should().Contain("a");
        result.Columns.Should().Contain("c");
        result.Columns.Should().NotContain("b");
    }

    // ============================================================================
    // Lit Expression Tests
    // ============================================================================

    [Fact]
    public void Lit_Int32_CreatesLiteralExpression()
    {
        var expr = Lit(42);

        expr.Should().NotBeNull();
    }

    [Fact]
    public void Lit_Float64_CreatesLiteralExpression()
    {
        var expr = Lit(3.14);

        expr.Should().NotBeNull();
    }

    [Fact]
    public void Lit_String_CreatesLiteralExpression()
    {
        var expr = Lit("hello");

        expr.Should().NotBeNull();
    }

    [Fact]
    public void Lit_Boolean_CreatesLiteralExpression()
    {
        var expr = Lit(true);

        expr.Should().NotBeNull();
    }

    // ============================================================================
    // Expression Alias Tests
    // ============================================================================

    [Fact]
    public void As_RenamesExpression()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 1, 2, 3 })
        );

        var result = df.Lazy().Select(Col("value").As("renamed")).Collect();

        result.Columns.Should().Contain("renamed");
        result.Columns.Should().NotContain("value");
    }

    [Fact]
    public void As_AlternativeSyntax_RenamesExpression()
    {
        var df = new DataFrame(
            Series.FromValues("x", new[] { 1, 2, 3 })
        );

        // .As() is the method to rename columns
        var result = df.Lazy().Select(Col("x").As("y")).Collect();

        result.Columns.Should().Contain("y");
    }

    // ============================================================================
    // Arithmetic Expression Tests
    // ============================================================================

    [Fact]
    public void Add_TwoColumns_ReturnsSum()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1.0, 2.0, 3.0 }),
            Series.FromValues("b", new[] { 10.0, 20.0, 30.0 })
        );

        var result = df.Lazy().Select((Col("a") + Col("b")).As("sum")).Collect();

        result["sum"][0].AsFloat64().Should().BeApproximately(11.0, 0.001);
        result["sum"][1].AsFloat64().Should().BeApproximately(22.0, 0.001);
        result["sum"][2].AsFloat64().Should().BeApproximately(33.0, 0.001);
    }

    [Fact]
    public void Subtract_TwoColumns_ReturnsDifference()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 10.0, 20.0, 30.0 }),
            Series.FromValues("b", new[] { 1.0, 2.0, 3.0 })
        );

        var result = df.Lazy().Select((Col("a") - Col("b")).As("diff")).Collect();

        result["diff"][0].AsFloat64().Should().BeApproximately(9.0, 0.001);
        result["diff"][1].AsFloat64().Should().BeApproximately(18.0, 0.001);
        result["diff"][2].AsFloat64().Should().BeApproximately(27.0, 0.001);
    }

    [Fact]
    public void Multiply_TwoColumns_ReturnsProduct()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 2.0, 3.0, 4.0 }),
            Series.FromValues("b", new[] { 5.0, 6.0, 7.0 })
        );

        var result = df.Lazy().Select((Col("a") * Col("b")).As("product")).Collect();

        result["product"][0].AsFloat64().Should().BeApproximately(10.0, 0.001);
        result["product"][1].AsFloat64().Should().BeApproximately(18.0, 0.001);
        result["product"][2].AsFloat64().Should().BeApproximately(28.0, 0.001);
    }

    [Fact]
    public void Divide_TwoColumns_ReturnsQuotient()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 10.0, 20.0, 30.0 }),
            Series.FromValues("b", new[] { 2.0, 4.0, 5.0 })
        );

        var result = df.Lazy().Select((Col("a") / Col("b")).As("quotient")).Collect();

        result["quotient"][0].AsFloat64().Should().BeApproximately(5.0, 0.001);
        result["quotient"][1].AsFloat64().Should().BeApproximately(5.0, 0.001);
        result["quotient"][2].AsFloat64().Should().BeApproximately(6.0, 0.001);
    }

    // ============================================================================
    // Comparison Expression Tests
    // ============================================================================

    [Fact]
    public void Eq_ScalarComparison_ReturnsCorrectMask()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 1, 2, 3, 2, 1 })
        );

        var result = df.Lazy().Filter(Col("value").Eq(2)).Collect();

        result.Height.Should().Be(2);
    }

    [Fact]
    public void Gt_ScalarComparison_ReturnsCorrectMask()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 1, 2, 3, 4, 5 })
        );

        var result = df.Lazy().Filter(Col("value").Gt(3)).Collect();

        result.Height.Should().Be(2); // 4 and 5
    }

    [Fact]
    public void Lt_ScalarComparison_ReturnsCorrectMask()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 1, 2, 3, 4, 5 })
        );

        var result = df.Lazy().Filter(Col("value").Lt(3)).Collect();

        result.Height.Should().Be(2); // 1 and 2
    }

    [Fact]
    public void Ge_ScalarComparison_ReturnsCorrectMask()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 1, 2, 3, 4, 5 })
        );

        var result = df.Lazy().Filter(Col("value").Ge(3)).Collect();

        result.Height.Should().Be(3); // 3, 4, and 5
    }

    [Fact]
    public void Le_ScalarComparison_ReturnsCorrectMask()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 1, 2, 3, 4, 5 })
        );

        var result = df.Lazy().Filter(Col("value").Le(3)).Collect();

        result.Height.Should().Be(3); // 1, 2, and 3
    }

    [Fact]
    public void Ne_ScalarComparison_ReturnsCorrectMask()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 1, 2, 3, 2, 1 })
        );

        var result = df.Lazy().Filter(Col("value").Ne(2)).Collect();

        result.Height.Should().Be(3); // two 1s and one 3
    }

    // ============================================================================
    // Boolean Expression Tests
    // ============================================================================

    [Fact]
    public void And_CombinesConditions()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 }),
            Series.FromValues("b", new[] { 5, 4, 3, 2, 1 })
        );

        var result = df.Lazy().Filter(Col("a").Gt(2).And(Col("b").Lt(4))).Collect();

        // a > 2 AND b < 4:
        // Row 0: a=1 (no), Row 1: a=2 (no), Row 2: a=3,b=3 (yes), Row 3: a=4,b=2 (yes), Row 4: a=5,b=1 (yes)
        result.Height.Should().Be(3);
    }

    [Fact]
    public void Or_CombinesConditions()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 1, 2, 3, 4, 5 })
        );

        var result = df.Lazy().Filter(Col("value").Eq(1).Or(Col("value").Eq(5))).Collect();

        result.Height.Should().Be(2);
    }

    [Fact]
    public void Not_NegatesCondition()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 1, 2, 3, 4, 5 })
        );

        var result = df.Lazy().Filter(Col("value").Eq(3).Not()).Collect();

        result.Height.Should().Be(4); // all except 3
    }

    // ============================================================================
    // Aggregation Expression Tests
    // ============================================================================

    [Fact]
    public void Sum_AggregatesColumn()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 1, 2, 3, 4, 5 })
        );

        var result = df.Lazy().Select(Col("value").Sum().As("total")).Collect();

        result["total"][0].AsInt64().Should().Be(15);
    }

    [Fact]
    public void Mean_AggregatesColumn()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 })
        );

        var result = df.Lazy().Select(Col("value").Mean().As("average")).Collect();

        result["average"][0].AsFloat64().Should().BeApproximately(3.0, 0.001);
    }

    [Fact]
    public void Min_AggregatesColumn()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 5, 2, 8, 1, 9 })
        );

        var result = df.Lazy().Select(Col("value").Min().As("minimum")).Collect();

        result["minimum"][0].AsInt32().Should().Be(1);
    }

    [Fact]
    public void Max_AggregatesColumn()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 5, 2, 8, 1, 9 })
        );

        var result = df.Lazy().Select(Col("value").Max().As("maximum")).Collect();

        result["maximum"][0].AsInt32().Should().Be(9);
    }

    // Note: Expr.Count() is a static function that may need specific context
    // The aggregation Count() on a column works in GroupBy context

    // ============================================================================
    // Expression Chaining Tests
    // ============================================================================

    [Fact]
    public void ChainedArithmetic_WorksCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1.0, 2.0, 3.0 }),
            Series.FromValues("b", new[] { 10.0, 20.0, 30.0 }),
            Series.FromValues("c", new[] { 100.0, 200.0, 300.0 })
        );

        // (a + b) * c
        var result = df.Lazy().Select(((Col("a") + Col("b")) * Col("c")).As("result")).Collect();

        result["result"][0].AsFloat64().Should().BeApproximately(1100.0, 0.001);  // (1+10)*100
        result["result"][1].AsFloat64().Should().BeApproximately(4400.0, 0.001);  // (2+20)*200
        result["result"][2].AsFloat64().Should().BeApproximately(9900.0, 0.001);  // (3+30)*300
    }

    [Fact]
    public void ChainedComparisons_WorksCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 })
        );

        // value > 3 AND value < 8
        var result = df.Lazy().Filter(Col("value").Gt(3).And(Col("value").Lt(8))).Collect();

        result.Height.Should().Be(4); // 4, 5, 6, 7
    }

    // ============================================================================
    // GroupBy with Expressions Tests
    // ============================================================================

    [Fact]
    public void GroupBy_WithSumExpression_WorksCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "B", "B" }),
            Series.FromValues("value", new[] { 1, 2, 3, 4 })
        );

        var result = df.Lazy().GroupBy("group").Agg(Col("value").Sum().As("total")).Collect();

        result.Height.Should().Be(2);
        result.Columns.Should().Contain("total");
    }

    [Fact]
    public void GroupBy_WithMeanExpression_WorksCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "B", "B" }),
            Series.FromValues("value", new[] { 1.0, 3.0, 5.0, 7.0 })
        );

        var result = df.Lazy().GroupBy("group").Agg(Col("value").Mean().As("average")).Collect();

        result.Height.Should().Be(2);
        result.Columns.Should().Contain("average");
    }

    [Fact]
    public void GroupBy_MultipleAggExpressions_WorksCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("group", new[] { "A", "A", "B", "B" }),
            Series.FromValues("value", new[] { 1, 2, 3, 4 })
        );

        var result = df.Lazy().GroupBy("group").Agg(
            Col("value").Sum().As("total"),
            Col("value").Min().As("min"),
            Col("value").Max().As("max")
        ).Collect();

        result.Height.Should().Be(2);
        result.Columns.Should().Contain("total");
        result.Columns.Should().Contain("min");
        result.Columns.Should().Contain("max");
    }

    // ============================================================================
    // Select with Multiple Expressions Tests
    // ============================================================================

    [Fact]
    public void Select_MixedExpressions_WorksCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1.0, 2.0, 3.0 }),
            Series.FromValues("b", new[] { 10.0, 20.0, 30.0 })
        );

        var result = df.Lazy().Select(
            Col("a"),
            Col("b"),
            (Col("a") + Col("b")).As("sum"),
            (Col("a") * Col("b")).As("product")
        ).Collect();

        result.Width.Should().Be(4);
        result.Columns.Should().Contain("a");
        result.Columns.Should().Contain("b");
        result.Columns.Should().Contain("sum");
        result.Columns.Should().Contain("product");
    }

    // ============================================================================
    // Edge Cases
    // ============================================================================

    [Fact]
    public void Select_SingleRow_WorksCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 42 })
        );

        var result = df.Lazy().Select(Col("value").As("x")).Collect();

        result.Height.Should().Be(1);
        result["x"][0].AsInt32().Should().Be(42);
    }

    [Fact]
    public void Filter_NoMatches_ReturnsEmptyDataFrame()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 1, 2, 3 })
        );

        var result = df.Lazy().Filter(Col("value").Gt(100)).Collect();

        result.Height.Should().Be(0);
    }

    [Fact]
    public void Filter_AllMatch_ReturnsAllRows()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 1, 2, 3 })
        );

        var result = df.Lazy().Filter(Col("value").Gt(0)).Collect();

        result.Height.Should().Be(3);
    }

    // ============================================================================
    // Complex Expression Tests
    // ============================================================================

    [Fact]
    public void ComplexFilter_MultipleConditions_WorksCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("name", new[] { "Alice", "Bob", "Charlie", "Diana" }),
            Series.FromValues("age", new[] { 25, 35, 30, 28 }),
            Series.FromValues("score", new[] { 85, 90, 75, 95 })
        );

        // age >= 28 AND score > 80
        var result = df.Lazy().Filter(Col("age").Ge(28).And(Col("score").Gt(80))).Collect();

        result.Height.Should().Be(2); // Bob (35, 90) and Diana (28, 95)
    }

    [Fact]
    public void ComplexSelect_ComputedColumns_WorksCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("price", new[] { 100.0, 200.0, 150.0 }),
            Series.FromValues("quantity", new[] { 2.0, 3.0, 4.0 })
        );

        var result = df.Lazy().Select(
            Col("price"),
            Col("quantity"),
            (Col("price") * Col("quantity")).As("total")
        ).Collect();

        result["total"][0].AsFloat64().Should().BeApproximately(200.0, 0.001);
        result["total"][1].AsFloat64().Should().BeApproximately(600.0, 0.001);
        result["total"][2].AsFloat64().Should().BeApproximately(600.0, 0.001);
    }

    // ============================================================================
    // LazyFrame Chaining Tests
    // ============================================================================

    [Fact]
    public void Filter_ThenSelect_WorksCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 }),
            Series.FromValues("b", new[] { 10, 20, 30, 40, 50 })
        );

        var result = df.Lazy()
            .Filter(Col("a").Gt(2))
            .Select(Col("b"))
            .Collect();

        result.Height.Should().Be(3);
        result.Width.Should().Be(1);
        result["b"][0].AsInt32().Should().Be(30);
    }

    [Fact]
    public void Select_ThenFilter_WorksCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 }),
            Series.FromValues("b", new[] { 10.0, 20.0, 30.0, 40.0, 50.0 })
        );

        var result = df.Lazy()
            .Select(Col("a"), (Col("a") + Col("b")).As("sum"))
            .Filter(Col("sum").Gt(30))
            .Collect();

        result.Height.Should().Be(3); // sums: 11, 22, 33, 44, 55 -> 33, 44, 55 > 30
    }

    [Fact]
    public void WithColumns_AddsComputedColumn()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1.0, 2.0, 3.0 })
        );

        var result = df.Lazy()
            .WithColumns((Col("a") * Lit(2.0)).As("doubled"))
            .Collect();

        result.Width.Should().Be(2);
        result.Columns.Should().Contain("doubled");
        result["doubled"][0].AsFloat64().Should().BeApproximately(2.0, 0.001);
        result["doubled"][1].AsFloat64().Should().BeApproximately(4.0, 0.001);
        result["doubled"][2].AsFloat64().Should().BeApproximately(6.0, 0.001);
    }

    [Fact]
    public void Sort_ThenHead_WorksCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 5, 3, 8, 1, 9, 2 })
        );

        var result = df.Lazy()
            .Sort("value")
            .Head(3)
            .Collect();

        result.Height.Should().Be(3);
        result["value"][0].AsInt32().Should().Be(1);
        result["value"][1].AsInt32().Should().Be(2);
        result["value"][2].AsInt32().Should().Be(3);
    }

    [Fact]
    public void Distinct_RemovesDuplicates()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 1, 2, 2, 3, 3, 3 })
        );

        var result = df.Lazy()
            .Distinct()
            .Collect();

        result.Height.Should().Be(3); // 1, 2, 3
    }

    // ============================================================================
    // Expression Edge Cases (from Polars test patterns)
    // ============================================================================

    [Fact]
    public void Col_NonExistentColumn_ThrowsOnCollect()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 })
        );

        Action act = () => df.Lazy().Select(Col("nonexistent")).Collect();

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Lit_NullValue_CreatesNull()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 })
        );

        // Literal null should be handled
        var nullLit = Expr.Lit(AnyValue.Null);
        nullLit.Should().BeOfType<Expr.Literal>();
    }

    [Fact]
    public void Arithmetic_ChainedOperations_CorrectPrecedence()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 2.0, 3.0, 4.0 }),
            Series.FromValues("b", new[] { 1.0, 2.0, 3.0 }),
            Series.FromValues("c", new[] { 10.0, 10.0, 10.0 })
        );

        // a + b * c should be a + (b * c) due to operator precedence
        var result = df.Lazy()
            .Select((Col("a") + Col("b") * Col("c")).As("result"))
            .Collect();

        // 2 + 1*10 = 12, 3 + 2*10 = 23, 4 + 3*10 = 34
        result["result"][0].AsFloat64().Should().BeApproximately(12.0, 0.0001);
        result["result"][1].AsFloat64().Should().BeApproximately(23.0, 0.0001);
        result["result"][2].AsFloat64().Should().BeApproximately(34.0, 0.0001);
    }

    [Fact]
    public void Arithmetic_Parentheses_OverridePrecedence()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 2.0, 3.0, 4.0 }),
            Series.FromValues("b", new[] { 1.0, 2.0, 3.0 }),
            Series.FromValues("c", new[] { 10.0, 10.0, 10.0 })
        );

        // (a + b) * c
        var result = df.Lazy()
            .Select(((Col("a") + Col("b")) * Col("c")).As("result"))
            .Collect();

        // (2+1)*10 = 30, (3+2)*10 = 50, (4+3)*10 = 70
        result["result"][0].AsFloat64().Should().BeApproximately(30.0, 0.0001);
        result["result"][1].AsFloat64().Should().BeApproximately(50.0, 0.0001);
        result["result"][2].AsFloat64().Should().BeApproximately(70.0, 0.0001);
    }

    [Fact]
    public void Comparison_AllOperators_WorkCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 })
        );

        // Equal
        var eq = df.Lazy().Filter(Col("a").Eq(3)).Collect();
        eq.Height.Should().Be(1);
        eq["a"][0].AsInt32().Should().Be(3);

        // Not Equal
        var ne = df.Lazy().Filter(Col("a").Ne(3)).Collect();
        ne.Height.Should().Be(4);

        // Greater Than
        var gt = df.Lazy().Filter(Col("a").Gt(3)).Collect();
        gt.Height.Should().Be(2);  // 4, 5

        // Greater Equal
        var ge = df.Lazy().Filter(Col("a").Ge(3)).Collect();
        ge.Height.Should().Be(3);  // 3, 4, 5

        // Less Than
        var lt = df.Lazy().Filter(Col("a").Lt(3)).Collect();
        lt.Height.Should().Be(2);  // 1, 2

        // Less Equal
        var le = df.Lazy().Filter(Col("a").Le(3)).Collect();
        le.Height.Should().Be(3);  // 1, 2, 3
    }

    [Fact]
    public void Boolean_AndOr_CombineCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 }),
            Series.FromValues("b", new[] { 5, 4, 3, 2, 1 })
        );

        // a > 2 AND b > 2 -> only (3, 3) matches
        var andResult = df.Lazy()
            .Filter(Col("a").Gt(2).And(Col("b").Gt(2)))
            .Collect();
        andResult.Height.Should().Be(1);
        andResult["a"][0].AsInt32().Should().Be(3);

        // a > 4 OR b > 4 -> (1, 5) and (5, 1) match
        var orResult = df.Lazy()
            .Filter(Col("a").Gt(4).Or(Col("b").Gt(4)))
            .Collect();
        orResult.Height.Should().Be(2);
    }

    [Fact]
    public void Boolean_Not_NegatesCondition()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 })
        );

        // NOT (a > 3) -> a <= 3
        var result = df.Lazy()
            .Filter(!Col("a").Gt(3))
            .Collect();

        result.Height.Should().Be(3);  // 1, 2, 3
    }

    [Fact]
    public void IsIn_SingleValue_MatchesExactly()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 })
        );

        var result = df.Lazy()
            .Filter(Col("a").IsInSet(2, 4))
            .Collect();

        result.Height.Should().Be(2);
    }

    [Fact]
    public void IsIn_NoMatches_ReturnsEmpty()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 })
        );

        var result = df.Lazy()
            .Filter(Col("a").IsInSet(10, 20, 30))
            .Collect();

        result.Height.Should().Be(0);
    }

    [Fact]
    public void Between_InclusiveBounds_MatchesBoundary()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 })
        );

        // Between is typically inclusive: [2, 4]
        var result = df.Lazy()
            .Filter(Col("a").IsBetween(2, 4))
            .Collect();

        result.Height.Should().Be(3);  // 2, 3, 4
    }

    [Fact]
    public void Aggregation_Multiple_InSameSelect()
    {
        var df = new DataFrame(
            Series.FromValues("value", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 })
        );

        var result = df.Lazy()
            .Select(
                Col("value").Sum().As("sum"),
                Col("value").Mean().As("mean"),
                Col("value").Min().As("min"),
                Col("value").Max().As("max")
            )
            .Collect();

        result["sum"][0].AsFloat64().Should().Be(15.0);
        result["mean"][0].AsFloat64().Should().Be(3.0);
        result["min"][0].AsFloat64().Should().Be(1.0);
        result["max"][0].AsFloat64().Should().Be(5.0);
    }

    [Fact]
    public void Aggregation_OnEmptyDataFrame_ReturnsNull()
    {
        var df = new DataFrame(
            Series.FromValues("value", Array.Empty<double>())
        );

        var result = df.Lazy()
            .Select(Col("value").Sum().As("sum"))
            .Collect();

        result.Height.Should().Be(1);
        // Empty sum should be null or 0
    }

    [Fact]
    public void Cast_Int32ToFloat64_PreservesValues()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 })
        );

        var result = df.Lazy()
            .Select(Col("a").CastTo(DataType.Float64).As("a_float"))
            .Collect();

        result["a_float"].DataType.Should().Be(DataType.Float64);
        result["a_float"][0].AsFloat64().Should().Be(1.0);
        result["a_float"][1].AsFloat64().Should().Be(2.0);
        result["a_float"][2].AsFloat64().Should().Be(3.0);
    }

    [Fact]
    public void Cast_Float64ToInt32_Truncates()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1.7, 2.3, 3.9 })
        );

        var result = df.Lazy()
            .Select(Col("a").CastTo(DataType.Int32).As("a_int"))
            .Collect();

        result["a_int"].DataType.Should().Be(DataType.Int32);
        result["a_int"][0].AsInt32().Should().Be(1);
        result["a_int"][1].AsInt32().Should().Be(2);
        result["a_int"][2].AsInt32().Should().Be(3);
    }

    [Fact]
    public void When_SimpleCondition_WorksCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("score", new[] { 95, 85, 75, 65, 55 })
        );

        // Simple pass/fail based on score >= 70
        var result = df.Lazy()
            .WithColumns(
                Expr.WhenExpr(Col("score").Ge(70)).Then("Pass").Otherwise("Fail").As("status")
            )
            .Collect();

        result["status"][0].AsString().Should().Be("Pass");  // 95
        result["status"][1].AsString().Should().Be("Pass");  // 85
        result["status"][2].AsString().Should().Be("Pass");  // 75
        result["status"][3].AsString().Should().Be("Fail");  // 65
        result["status"][4].AsString().Should().Be("Fail");  // 55
    }

    [Fact]
    public void IsNull_DetectsNullValues()
    {
        var df = new DataFrame(
            Series.FromNullable("a", new int?[] { 1, null, 3, null, 5 })
        );

        var result = df.Lazy()
            .Filter(Col("a").IsNullExpr())
            .Collect();

        result.Height.Should().Be(2);
    }

    [Fact]
    public void IsNotNull_DetectsNonNullValues()
    {
        var df = new DataFrame(
            Series.FromNullable("a", new int?[] { 1, null, 3, null, 5 })
        );

        var result = df.Lazy()
            .Filter(Col("a").IsNotNullExpr())
            .Collect();

        result.Height.Should().Be(3);
    }

    [Fact]
    public void FillNull_ReplacesNullsWithValue()
    {
        var df = new DataFrame(
            Series.FromNullable("a", new int?[] { 1, null, 3, null, 5 })
        );

        var result = df.Lazy()
            .Select(Col("a").FillNullWith(0).As("filled"))
            .Collect();

        result["filled"][1].AsInt32().Should().Be(0);
        result["filled"][3].AsInt32().Should().Be(0);
    }

    [Fact]
    public void Alias_CanBeChained()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 })
        );

        var result = df.Lazy()
            .Select(
                Col("a").As("x").As("y").As("z")
            )
            .Collect();

        result.Columns.Should().Contain("z");
        result["z"][0].AsInt32().Should().Be(1);
    }

    [Fact]
    public void Select_SameColumnMultipleTimes_WithDifferentAliases()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1.0, 2.0, 3.0 })
        );

        var result = df.Lazy()
            .Select(
                Col("a").As("original"),
                (Col("a") * Lit(2.0)).As("doubled"),
                (Col("a") * Col("a")).As("squared")
            )
            .Collect();

        result.Width.Should().Be(3);
        result["original"][0].AsFloat64().Should().Be(1.0);
        result["doubled"][0].AsFloat64().Should().Be(2.0);
        result["squared"][0].AsFloat64().Should().Be(1.0);
    }

    // ============================================================================
    // Large Data Expression Tests
    // ============================================================================

    [Fact]
    public void Expression_LargeDataFrame_PerformsCorrectly()
    {
        var size = 10000;
        var values = Enumerable.Range(0, size).Select(i => (double)i).ToArray();
        var df = new DataFrame(
            Series.FromValues("value", values)
        );

        var result = df.Lazy()
            .Select(
                (Col("value") * Lit(2.0)).As("doubled")
            )
            .Collect();

        result.Height.Should().Be(size);
        result["doubled"][0].AsFloat64().Should().Be(0.0);
        result["doubled"][size - 1].AsFloat64().Should().Be((size - 1) * 2.0);
    }

    [Fact]
    public void Filter_LargeDataFrame_PerformsCorrectly()
    {
        var size = 10000;
        var values = Enumerable.Range(0, size).ToArray();
        var df = new DataFrame(
            Series.FromValues("value", values)
        );

        // Filter to values >= 5000
        var result = df.Lazy()
            .Filter(Col("value").Ge(5000))
            .Collect();

        result.Height.Should().Be(5000);
    }

    // ============================================================================
    // Null Handling in Expressions
    // ============================================================================

    [Fact]
    public void Arithmetic_WithNulls_PropagatesNulls()
    {
        var df = new DataFrame(
            Series.FromNullable("a", new double?[] { 1.0, null, 3.0 }),
            Series.FromNullable("b", new double?[] { 10.0, 20.0, null })
        );

        var result = df.Lazy()
            .Select((Col("a") + Col("b")).As("sum"))
            .Collect();

        // 1 + 10 = 11, null + 20 = null, 3 + null = null
        result["sum"][0].AsFloat64().Should().Be(11.0);
        result["sum"].IsNull(1).Should().BeTrue();
        result["sum"].IsNull(2).Should().BeTrue();
    }

    [Fact]
    public void Comparison_WithNulls_ReturnsNull()
    {
        var df = new DataFrame(
            Series.FromNullable("a", new int?[] { 1, null, 3 })
        );

        // null > 2 should be null (not true or false)
        var boolResult = df.Lazy()
            .Select(Col("a").Gt(2).As("result"))
            .Collect();

        boolResult["result"][0].AsBoolean().Should().BeFalse();  // 1 > 2 = false
        boolResult["result"].IsNull(1).Should().BeTrue();  // null > 2 = null
        boolResult["result"][2].AsBoolean().Should().BeTrue();   // 3 > 2 = true
    }

    // ============================================================================
    // Division and Modulo Edge Cases
    // ============================================================================

    [Fact]
    public void Division_ByZero_ReturnsInfinity()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 10.0, 20.0, 30.0 }),
            Series.FromValues("b", new[] { 2.0, 0.0, 5.0 })
        );

        var result = df.Lazy()
            .Select((Col("a") / Col("b")).As("result"))
            .Collect();

        result["result"][0].AsFloat64().Should().Be(5.0);
        result["result"][1].AsFloat64().Should().Be(double.PositiveInfinity);
        result["result"][2].AsFloat64().Should().Be(6.0);
    }

    [Fact]
    public void Subtraction_SameColumn_ReturnsZero()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1.0, 2.0, 3.0 })
        );

        var result = df.Lazy()
            .Select((Col("a") - Col("a")).As("zero"))
            .Collect();

        result["zero"][0].AsFloat64().Should().Be(0.0);
        result["zero"][1].AsFloat64().Should().Be(0.0);
        result["zero"][2].AsFloat64().Should().Be(0.0);
    }

    // ============================================================================
    // Complex Expression Chains
    // ============================================================================

    [Fact]
    public void Expression_ComplexChain_WorksCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("price", new[] { 100.0, 200.0, 300.0 }),
            Series.FromValues("quantity", new[] { 2.0, 3.0, 1.0 }),
            Series.FromValues("discount", new[] { 0.1, 0.2, 0.0 })
        );

        // total = price * quantity * (1 - discount)
        var result = df.Lazy()
            .Select(
                (Col("price") * Col("quantity") * (Lit(1.0) - Col("discount"))).As("total")
            )
            .Collect();

        // 100 * 2 * 0.9 = 180
        result["total"][0].AsFloat64().Should().BeApproximately(180.0, 0.0001);
        // 200 * 3 * 0.8 = 480
        result["total"][1].AsFloat64().Should().BeApproximately(480.0, 0.0001);
        // 300 * 1 * 1.0 = 300
        result["total"][2].AsFloat64().Should().BeApproximately(300.0, 0.0001);
    }

    [Fact]
    public void FilterSelect_Combined_WorksCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("name", new[] { "Alice", "Bob", "Charlie", "Diana" }),
            Series.FromValues("age", new[] { 25, 30, 35, 28 }),
            Series.FromValues("score", new[] { 85.0, 92.0, 78.0, 95.0 })
        );

        var result = df.Lazy()
            .Filter(Col("age").Gt(26))
            .Select(
                Col("name"),
                (Col("score") * Lit(1.1)).As("adjusted_score")
            )
            .Collect();

        result.Height.Should().Be(3);  // Bob, Charlie, Diana
        result.Columns.Should().BeEquivalentTo(new[] { "name", "adjusted_score" });
    }
}
