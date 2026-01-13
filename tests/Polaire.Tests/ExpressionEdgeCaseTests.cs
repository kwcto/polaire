// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET
//
// Edge case tests for expression operations, inspired by Polars test suite.
// These tests cover:
// - Expression comparison operations
// - Expression arithmetic edge cases
// - Expression chaining
// - Expression alias handling

using FluentAssertions;
using Polaire;
using Polaire.DataTypes;
using Xunit;
using static Polaire.Pl;

namespace Polaire.Tests;

/// <summary>
/// Edge case tests for expression operations.
/// </summary>
public class ExpressionEdgeCaseTests
{
    // ============================================================================
    // Comparison Expression Tests
    // ============================================================================

    [Fact]
    public void Gt_WithZero_Works()
    {
        var df = new DataFrame(
            Series.FromValues("x", new[] { -1, 0, 1 })
        );

        var result = df.Lazy().Filter(Col("x").Gt(0)).Collect();

        result.Height.Should().Be(1);
        result["x"][0].AsInt32().Should().Be(1);
    }

    [Fact]
    public void Lt_WithZero_Works()
    {
        var df = new DataFrame(
            Series.FromValues("x", new[] { -1, 0, 1 })
        );

        var result = df.Lazy().Filter(Col("x").Lt(0)).Collect();

        result.Height.Should().Be(1);
        result["x"][0].AsInt32().Should().Be(-1);
    }

    [Fact]
    public void Ge_WithValue_Works()
    {
        var df = new DataFrame(
            Series.FromValues("x", new[] { 1, 2, 3, 4, 5 })
        );

        var result = df.Lazy().Filter(Col("x").Ge(3)).Collect();

        result.Height.Should().Be(3);  // 3, 4, 5
    }

    [Fact]
    public void Le_WithValue_Works()
    {
        var df = new DataFrame(
            Series.FromValues("x", new[] { 1, 2, 3, 4, 5 })
        );

        var result = df.Lazy().Filter(Col("x").Le(3)).Collect();

        result.Height.Should().Be(3);  // 1, 2, 3
    }

    [Fact]
    public void Eq_WithValue_Works()
    {
        var df = new DataFrame(
            Series.FromValues("x", new[] { 1, 2, 3, 2, 1 })
        );

        var result = df.Lazy().Filter(Col("x").Eq(2)).Collect();

        result.Height.Should().Be(2);
    }

    [Fact]
    public void Ne_WithValue_Works()
    {
        var df = new DataFrame(
            Series.FromValues("x", new[] { 1, 2, 3, 2, 1 })
        );

        var result = df.Lazy().Filter(Col("x").Ne(2)).Collect();

        result.Height.Should().Be(3);
    }

    // ============================================================================
    // Comparison with Floating Point
    // ============================================================================

    [Fact]
    public void Gt_FloatValues_Works()
    {
        var df = new DataFrame(
            Series.FromValues("x", new[] { 0.1, 0.2, 0.3 })
        );

        var result = df.Lazy().Filter(Col("x").Gt(0.15)).Collect();

        result.Height.Should().Be(2);  // 0.2, 0.3
    }

    [Fact]
    public void Eq_FloatValues_Works()
    {
        var df = new DataFrame(
            Series.FromValues("x", new[] { 1.5, 2.5, 1.5 })
        );

        var result = df.Lazy().Filter(Col("x").Eq(1.5)).Collect();

        result.Height.Should().Be(2);
    }

    // ============================================================================
    // Comparison with Strings
    // ============================================================================

    [Fact]
    public void Eq_StringValues_Works()
    {
        var df = new DataFrame(
            Series.FromValues("name", new[] { "Alice", "Bob", "Alice" })
        );

        var result = df.Lazy().Filter(Col("name").Eq("Alice")).Collect();

        result.Height.Should().Be(2);
    }

    [Fact]
    public void Ne_StringValues_Works()
    {
        var df = new DataFrame(
            Series.FromValues("name", new[] { "Alice", "Bob", "Charlie" })
        );

        var result = df.Lazy().Filter(Col("name").Ne("Bob")).Collect();

        result.Height.Should().Be(2);
    }

    // ============================================================================
    // Arithmetic Expression Tests
    // ============================================================================

    [Fact]
    public void Add_TwoColumns_Works()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1.0, 2.0, 3.0 }),
            Series.FromValues("b", new[] { 10.0, 20.0, 30.0 })
        );

        var result = df.Lazy()
            .WithColumns((Col("a") + Col("b")).As("sum"))
            .Collect();

        result["sum"][0].AsFloat64().Should().Be(11);
        result["sum"][2].AsFloat64().Should().Be(33);
    }

    [Fact]
    public void Sub_TwoColumns_Works()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 10.0, 20.0, 30.0 }),
            Series.FromValues("b", new[] { 1.0, 2.0, 3.0 })
        );

        var result = df.Lazy()
            .WithColumns((Col("a") - Col("b")).As("diff"))
            .Collect();

        result["diff"][0].AsFloat64().Should().Be(9);
        result["diff"][2].AsFloat64().Should().Be(27);
    }

    [Fact]
    public void Mul_TwoColumns_Works()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 2.0, 3.0, 4.0 }),
            Series.FromValues("b", new[] { 10.0, 20.0, 30.0 })
        );

        var result = df.Lazy()
            .WithColumns((Col("a") * Col("b")).As("product"))
            .Collect();

        result["product"][0].AsFloat64().Should().Be(20);
        result["product"][2].AsFloat64().Should().Be(120);
    }

    [Fact]
    public void Div_TwoColumns_Works()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 10.0, 20.0, 30.0 }),
            Series.FromValues("b", new[] { 2.0, 4.0, 5.0 })
        );

        var result = df.Lazy()
            .WithColumns((Col("a") / Col("b")).As("quotient"))
            .Collect();

        result["quotient"][0].AsFloat64().Should().Be(5);
        result["quotient"][2].AsFloat64().Should().Be(6);
    }

    // ============================================================================
    // Arithmetic with Scalars
    // ============================================================================

    [Fact]
    public void Add_ScalarToColumn_Works()
    {
        var df = new DataFrame(
            Series.FromValues("x", new[] { 1, 2, 3 })
        );

        var result = df.Lazy()
            .WithColumns((Col("x") + 10).As("plus10"))
            .Collect();

        result["plus10"][0].AsFloat64().Should().Be(11);
        result["plus10"][2].AsFloat64().Should().Be(13);
    }

    [Fact]
    public void Mul_ScalarToColumn_Works()
    {
        var df = new DataFrame(
            Series.FromValues("x", new[] { 1, 2, 3 })
        );

        var result = df.Lazy()
            .WithColumns((Col("x") * 2).As("doubled"))
            .Collect();

        result["doubled"][0].AsFloat64().Should().Be(2);
        result["doubled"][2].AsFloat64().Should().Be(6);
    }

    // ============================================================================
    // Expression Chaining Tests
    // ============================================================================

    [Fact]
    public void MultipleArithmetic_Chains_Work()
    {
        var df = new DataFrame(
            Series.FromValues("x", new[] { 1, 2, 3 })
        );

        var result = df.Lazy()
            .WithColumns(((Col("x") + 1) * 2).As("result"))
            .Collect();

        // (1+1)*2=4, (2+1)*2=6, (3+1)*2=8
        result["result"][0].AsFloat64().Should().Be(4);
        result["result"][1].AsFloat64().Should().Be(6);
        result["result"][2].AsFloat64().Should().Be(8);
    }

    // ============================================================================
    // Aggregation Expression Tests
    // ============================================================================

    [Fact]
    public void Sum_Expression_Works()
    {
        var df = new DataFrame(
            Series.FromValues("x", new[] { 1, 2, 3, 4, 5 })
        );

        var result = df.Lazy()
            .Select(Col("x").Sum().As("total"))
            .Collect();

        result["total"][0].AsInt64().Should().Be(15);
    }

    [Fact]
    public void Mean_Expression_Works()
    {
        var df = new DataFrame(
            Series.FromValues("x", new[] { 10.0, 20.0, 30.0 })
        );

        var result = df.Lazy()
            .Select(Col("x").Mean().As("avg"))
            .Collect();

        result["avg"][0].AsFloat64().Should().Be(20.0);
    }

    [Fact]
    public void Min_Expression_Works()
    {
        var df = new DataFrame(
            Series.FromValues("x", new[] { 5, 3, 8, 1, 9 })
        );

        var result = df.Lazy()
            .Select(Col("x").Min().As("minimum"))
            .Collect();

        result["minimum"][0].AsInt32().Should().Be(1);
    }

    [Fact]
    public void Max_Expression_Works()
    {
        var df = new DataFrame(
            Series.FromValues("x", new[] { 5, 3, 8, 1, 9 })
        );

        var result = df.Lazy()
            .Select(Col("x").Max().As("maximum"))
            .Collect();

        result["maximum"][0].AsInt32().Should().Be(9);
    }

    // ============================================================================
    // Alias Tests
    // ============================================================================

    [Fact]
    public void As_SetsColumnName()
    {
        var df = new DataFrame(
            Series.FromValues("x", new[] { 1, 2, 3 })
        );

        var result = df.Lazy()
            .Select(Col("x").As("renamed"))
            .Collect();

        result.Columns.Should().Contain("renamed");
        result.Columns.Should().NotContain("x");
    }

    [Fact]
    public void As_OnAggregation_SetsName()
    {
        var df = new DataFrame(
            Series.FromValues("x", new[] { 1, 2, 3 })
        );

        var result = df.Lazy()
            .Select(Col("x").Sum().As("total_x"))
            .Collect();

        result.Columns.Should().Contain("total_x");
    }

    // ============================================================================
    // Multiple Expressions Tests
    // ============================================================================

    [Fact]
    public void Select_MultipleExpressions_Works()
    {
        var df = new DataFrame(
            Series.FromValues("x", new[] { 1, 2, 3 }),
            Series.FromValues("y", new[] { 10, 20, 30 })
        );

        var result = df.Lazy()
            .Select(
                Col("x").Sum().As("sum_x"),
                Col("y").Mean().As("avg_y")
            )
            .Collect();

        result.Columns.Should().Contain("sum_x");
        result.Columns.Should().Contain("avg_y");
    }

    [Fact]
    public void WithColumns_MultipleExpressions_AddsAll()
    {
        var df = new DataFrame(
            Series.FromValues("x", new[] { 1, 2, 3 })
        );

        var result = df.Lazy()
            .WithColumns(
                (Col("x") + 1).As("x_plus_1"),
                (Col("x") * 2).As("x_times_2")
            )
            .Collect();

        result.Columns.Should().Contain("x_plus_1");
        result.Columns.Should().Contain("x_times_2");
        result.Width.Should().Be(3);  // x, x_plus_1, x_times_2
    }

    // ============================================================================
    // Empty DataFrame Tests
    // ============================================================================

    [Fact]
    public void Filter_EmptyDataFrame_ReturnsEmpty()
    {
        var df = new DataFrame(
            Series.FromValues("x", Array.Empty<int>())
        );

        var result = df.Lazy().Filter(Col("x").Gt(0)).Collect();

        result.Height.Should().Be(0);
    }

    [Fact]
    public void WithColumns_EmptyDataFrame_ReturnsEmpty()
    {
        var df = new DataFrame(
            Series.FromValues("x", Array.Empty<int>())
        );

        var result = df.Lazy()
            .WithColumns((Col("x") + 1).As("y"))
            .Collect();

        result.Height.Should().Be(0);
        result.Width.Should().Be(2);
    }

    // ============================================================================
    // Single Row Tests
    // ============================================================================

    [Fact]
    public void Filter_SingleRow_Match_ReturnsRow()
    {
        var df = new DataFrame(
            Series.FromValues("x", new[] { 5 })
        );

        var result = df.Lazy().Filter(Col("x").Eq(5)).Collect();

        result.Height.Should().Be(1);
    }

    [Fact]
    public void Filter_SingleRow_NoMatch_ReturnsEmpty()
    {
        var df = new DataFrame(
            Series.FromValues("x", new[] { 5 })
        );

        var result = df.Lazy().Filter(Col("x").Eq(10)).Collect();

        result.Height.Should().Be(0);
    }

    // ============================================================================
    // Boolean Logic Tests
    // ============================================================================

    [Fact]
    public void And_TwoConditions_Works()
    {
        var df = new DataFrame(
            Series.FromValues("x", new[] { 1, 2, 3, 4, 5 })
        );

        var result = df.Lazy()
            .Filter(Col("x").Gt(1).And(Col("x").Lt(5)))
            .Collect();

        result.Height.Should().Be(3);  // 2, 3, 4
    }

    [Fact]
    public void Or_TwoConditions_Works()
    {
        var df = new DataFrame(
            Series.FromValues("x", new[] { 1, 2, 3, 4, 5 })
        );

        var result = df.Lazy()
            .Filter(Col("x").Eq(1).Or(Col("x").Eq(5)))
            .Collect();

        result.Height.Should().Be(2);  // 1, 5
    }

    // ============================================================================
    // Null Handling in Expressions
    // ============================================================================

    [Fact]
    public void Filter_WithNulls_SkipsNulls()
    {
        var df = new DataFrame(
            Series.FromNullable("x", new int?[] { 1, null, 3, null, 5 })
        );

        var result = df.Lazy().Filter(Col("x").Gt(2)).Collect();

        result.Height.Should().Be(2);  // 3, 5
    }

    [Fact]
    public void Arithmetic_WithNulls_PropagatesNull()
    {
        var df = new DataFrame(
            Series.FromNullable("x", new int?[] { 1, null, 3 })
        );

        var result = df.Lazy()
            .WithColumns((Col("x") + 10).As("y"))
            .Collect();

        result.Height.Should().Be(3);
        result["y"].IsNull(1).Should().BeTrue();
    }

    // ============================================================================
    // Large Data Tests
    // ============================================================================

    [Fact]
    public void Filter_LargeData_Succeeds()
    {
        var values = Enumerable.Range(0, 10000).ToArray();

        var df = new DataFrame(
            Series.FromValues("x", values)
        );

        var result = df.Lazy().Filter(Col("x").Gt(5000)).Collect();

        result.Height.Should().Be(4999);  // 5001-9999
    }

    [Fact]
    public void WithColumns_LargeData_Succeeds()
    {
        var values = Enumerable.Range(0, 10000).ToArray();

        var df = new DataFrame(
            Series.FromValues("x", values)
        );

        var result = df.Lazy()
            .WithColumns((Col("x") * 2).As("doubled"))
            .Collect();

        result.Height.Should().Be(10000);
        result["doubled"][9999].AsFloat64().Should().Be(19998);
    }

    // ============================================================================
    // Literal Expression Tests
    // ============================================================================

    [Fact]
    public void Lit_IntValue_Works()
    {
        var df = new DataFrame(
            Series.FromValues("x", new[] { 1, 2, 3 })
        );

        var result = df.Lazy()
            .WithColumns(Lit(100).As("constant"))
            .Collect();

        result["constant"][0].AsInt32().Should().Be(100);
        result["constant"][2].AsInt32().Should().Be(100);
    }

    [Fact]
    public void Lit_FloatValue_Works()
    {
        var df = new DataFrame(
            Series.FromValues("x", new[] { 1, 2, 3 })
        );

        var result = df.Lazy()
            .WithColumns(Lit(3.14).As("pi"))
            .Collect();

        result["pi"][0].AsFloat64().Should().BeApproximately(3.14, 0.001);
    }

    [Fact]
    public void Lit_StringValue_Works()
    {
        var df = new DataFrame(
            Series.FromValues("x", new[] { 1, 2, 3 })
        );

        var result = df.Lazy()
            .WithColumns(Lit("hello").As("greeting"))
            .Collect();

        result["greeting"][0].AsString().Should().Be("hello");
    }

    // ============================================================================
    // Edge Cases
    // ============================================================================

    [Fact]
    public void Eq_WithBooleanValues_Works()
    {
        var df = new DataFrame(
            Series.FromValues("flag", new[] { true, false, true })
        );

        var result = df.Lazy().Filter(Col("flag").Eq(true)).Collect();

        result.Height.Should().Be(2);
    }

    [Fact]
    public void Filter_NoMatches_ReturnsEmpty()
    {
        var df = new DataFrame(
            Series.FromValues("x", new[] { 1, 2, 3 })
        );

        var result = df.Lazy().Filter(Col("x").Gt(100)).Collect();

        result.Height.Should().Be(0);
    }

    [Fact]
    public void Filter_AllMatch_ReturnsAll()
    {
        var df = new DataFrame(
            Series.FromValues("x", new[] { 10, 20, 30 })
        );

        var result = df.Lazy().Filter(Col("x").Gt(0)).Collect();

        result.Height.Should().Be(3);
    }
}
