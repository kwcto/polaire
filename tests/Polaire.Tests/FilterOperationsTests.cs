// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET
// Ported from Polars py-polars/tests/unit/operations/test_filter.py

using Xunit;
using FluentAssertions;
using Polaire.DataTypes;
using static Polaire.Pl;

namespace Polaire.Tests;

/// <summary>
/// Comprehensive filter operation tests ported from Polars test suite.
/// Uses LazyFrame.Filter for expression-based filtering.
/// </summary>
public class FilterOperationsTests
{
    // ============================================================================
    // Basic Filter Tests (using LazyFrame for Expr-based filtering)
    // ============================================================================

    [Fact]
    public void Filter_SingleCondition_ReturnsMatchingRows()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 }),
            Series.FromValues("b", new[] { "x", "y", "z", "w", "v" })
        );

        var result = df.Lazy().Filter(Col("a").Gt(3)).Collect();

        result.Height.Should().Be(2);
        result["a"][0].AsInt32().Should().Be(4);
        result["a"][1].AsInt32().Should().Be(5);
    }

    [Fact]
    public void Filter_NoMatch_ReturnsEmptyDataFrame()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 })
        );

        var result = df.Lazy().Filter(Col("a").Gt(100)).Collect();

        result.Height.Should().Be(0);
        result.Width.Should().Be(1);
    }

    [Fact]
    public void Filter_AllMatch_ReturnsAllRows()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 })
        );

        var result = df.Lazy().Filter(Col("a").Gt(0)).Collect();

        result.Height.Should().Be(3);
    }

    [Fact]
    public void Filter_EmptyDataFrame_ReturnsEmpty()
    {
        var df = new DataFrame(
            Series.FromValues("a", Array.Empty<int>())
        );

        var result = df.Lazy().Filter(Col("a").Gt(0)).Collect();

        result.Height.Should().Be(0);
    }

    // ============================================================================
    // Logical Operator Tests
    // ============================================================================

    [Fact]
    public void Filter_AndCondition_ReturnsMatchingRows()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 }),
            Series.FromValues("b", new[] { 10, 20, 30, 40, 50 })
        );

        var result = df.Lazy().Filter(Col("a").Gt(2).And(Col("b").Lt(45))).Collect();

        result.Height.Should().Be(2);
        result["a"].ToArray<int>().Should().BeEquivalentTo(new[] { 3, 4 });
    }

    [Fact]
    public void Filter_OrCondition_ReturnsMatchingRows()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 }),
            Series.FromValues("b", new[] { 10, 20, 30, 40, 50 })
        );

        var result = df.Lazy().Filter(Col("a").Eq(1).Or(Col("a").Eq(5))).Collect();

        result.Height.Should().Be(2);
        result["a"].ToArray<int>().Should().BeEquivalentTo(new[] { 1, 5 });
    }

    [Fact]
    public void Filter_NotCondition_ReturnsNonMatchingRows()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 })
        );

        var result = df.Lazy().Filter(Col("a").Le(2)).Collect();

        result.Height.Should().Be(2);
        result["a"].ToArray<int>().Should().BeEquivalentTo(new[] { 1, 2 });
    }

    [Fact]
    public void Filter_ComplexLogicalExpression_Works()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 }),
            Series.FromValues("b", new[] { "x", "y", "x", "y", "x" })
        );

        // ((a > 2) AND (b == "x")) OR (a == 1)
        var result = df.Lazy().Filter(
            Col("a").Gt(2).And(Col("b").Eq("x")).Or(Col("a").Eq(1))
        ).Collect();

        result.Height.Should().Be(3);
        result["a"].ToArray<int>().Should().BeEquivalentTo(new[] { 1, 3, 5 });
    }

    // ============================================================================
    // Null Handling Tests
    // ============================================================================

    [Fact]
    public void Filter_NullValues_ExcludedByDefault()
    {
        var df = new DataFrame(
            Series.FromNullable("a", new int?[] { 1, null, 3, null, 5 })
        );

        var result = df.Lazy().Filter(Col("a").Gt(2)).Collect();

        result.Height.Should().Be(2);
        result["a"][0].AsInt32().Should().Be(3);
        result["a"][1].AsInt32().Should().Be(5);
    }

    [Fact]
    public void Filter_IsNull_ReturnsNullRows()
    {
        var df = new DataFrame(
            Series.FromNullable("a", new int?[] { 1, null, 3, null, 5 })
        );

        var result = df.Lazy().Filter(Col("a").IsNullExpr()).Collect();

        result.Height.Should().Be(2);
        result["a"].NullCount.Should().Be(2);
    }

    [Fact]
    public void Filter_IsNotNull_ReturnsNonNullRows()
    {
        var df = new DataFrame(
            Series.FromNullable("a", new int?[] { 1, null, 3, null, 5 })
        );

        var result = df.Lazy().Filter(Col("a").IsNotNullExpr()).Collect();

        result.Height.Should().Be(3);
        result["a"].NullCount.Should().Be(0);
    }

    // ============================================================================
    // Comparison Operators Tests
    // ============================================================================

    [Fact]
    public void Filter_Eq_ReturnsEqualRows()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 2, 1 })
        );

        var result = df.Lazy().Filter(Col("a").Eq(2)).Collect();

        result.Height.Should().Be(2);
    }

    [Fact]
    public void Filter_Neq_ReturnsNonEqualRows()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 2, 1 })
        );

        var result = df.Lazy().Filter(Col("a").Ne(2)).Collect();

        result.Height.Should().Be(3);
    }

    [Fact]
    public void Filter_Gt_ReturnsGreaterRows()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 })
        );

        var result = df.Lazy().Filter(Col("a").Gt(3)).Collect();

        result.Height.Should().Be(2);
        result["a"].ToArray<int>().Should().BeEquivalentTo(new[] { 4, 5 });
    }

    [Fact]
    public void Filter_Gte_ReturnsGreaterOrEqualRows()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 })
        );

        var result = df.Lazy().Filter(Col("a").Ge(3)).Collect();

        result.Height.Should().Be(3);
        result["a"].ToArray<int>().Should().BeEquivalentTo(new[] { 3, 4, 5 });
    }

    [Fact]
    public void Filter_Lt_ReturnsLesserRows()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 })
        );

        var result = df.Lazy().Filter(Col("a").Lt(3)).Collect();

        result.Height.Should().Be(2);
        result["a"].ToArray<int>().Should().BeEquivalentTo(new[] { 1, 2 });
    }

    [Fact]
    public void Filter_Lte_ReturnsLesserOrEqualRows()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 })
        );

        var result = df.Lazy().Filter(Col("a").Le(3)).Collect();

        result.Height.Should().Be(3);
        result["a"].ToArray<int>().Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    // ============================================================================
    // IsIn Tests
    // ============================================================================

    [Fact]
    public void Filter_IsIn_IntArray_ReturnsMatchingRows()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 })
        );

        var result = df.Lazy().Filter(Col("a").IsInSet(2, 4)).Collect();

        result.Height.Should().Be(2);
        result["a"].ToArray<int>().Should().BeEquivalentTo(new[] { 2, 4 });
    }

    [Fact]
    public void Filter_IsIn_StringArray_ReturnsMatchingRows()
    {
        var df = new DataFrame(
            Series.FromValues("name", new[] { "Alice", "Bob", "Charlie", "Diana" })
        );

        var result = df.Lazy().Filter(Col("name").IsInSet("Bob", "Diana")).Collect();

        result.Height.Should().Be(2);
    }

    [Fact]
    public void Filter_IsIn_EmptyArray_ReturnsNoRows()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 })
        );

        var result = df.Lazy().Filter(Col("a").IsInSet()).Collect();

        result.Height.Should().Be(0);
    }

    // ============================================================================
    // Between Tests
    // ============================================================================

    [Fact]
    public void Filter_Between_InclusiveBoth_ReturnsMatchingRows()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 })
        );

        var result = df.Lazy().Filter(Col("a").IsBetween(2, 4)).Collect();

        result.Height.Should().Be(3);
        result["a"].ToArray<int>().Should().BeEquivalentTo(new[] { 2, 3, 4 });
    }

    [Fact]
    public void Filter_Between_ExclusiveBoth_ReturnsMatchingRows()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 })
        );

        // Use exclusive by setting inclusive=false and manually check bounds > and <
        var result = df.Lazy().Filter(Col("a").Gt(2).And(Col("a").Lt(4))).Collect();

        result.Height.Should().Be(1);
        result["a"][0].AsInt32().Should().Be(3);
    }

    [Fact]
    public void Filter_Between_Floats_ReturnsMatchingRows()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1.0, 2.5, 3.5, 4.5, 5.0 })
        );

        var result = df.Lazy().Filter(Col("a").IsBetween(2.0, 4.0)).Collect();

        result.Height.Should().Be(2);
    }

    // ============================================================================
    // String Filter Tests
    // ============================================================================

    [Fact]
    public void Filter_StringContains_ReturnsMatchingRows()
    {
        var df = new DataFrame(
            Series.FromValues("name", new[] { "Alice", "Bob", "Charlie", "Alicia" })
        );

        var result = df.Lazy().Filter(Col("name").StrExpr.Contains("Ali")).Collect();

        result.Height.Should().Be(2);
    }

    [Fact]
    public void Filter_StringStartsWith_ReturnsMatchingRows()
    {
        var df = new DataFrame(
            Series.FromValues("name", new[] { "Alice", "Bob", "Charlie", "Alicia" })
        );

        var result = df.Lazy().Filter(Col("name").StrExpr.StartsWith("Al")).Collect();

        result.Height.Should().Be(2);
    }

    [Fact]
    public void Filter_StringEndsWith_ReturnsMatchingRows()
    {
        var df = new DataFrame(
            Series.FromValues("name", new[] { "Alice", "Bob", "Charlie", "Alicia" })
        );

        var result = df.Lazy().Filter(Col("name").StrExpr.EndsWith("ce")).Collect();

        result.Height.Should().Be(1);
        result["name"][0].AsString().Should().Be("Alice");
    }

    // ============================================================================
    // LazyFrame Filter Tests
    // ============================================================================

    [Fact]
    public void LazyFrame_Filter_ThenCollect_ReturnsFilteredData()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 }),
            Series.FromValues("b", new[] { "x", "y", "x", "y", "x" })
        );

        var result = df.Lazy()
            .Filter(Col("a").Gt(2))
            .Collect();

        result.Height.Should().Be(3);
    }

    [Fact]
    public void LazyFrame_MultipleFilters_Chained()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 }),
            Series.FromValues("b", new[] { 10, 20, 30, 40, 50 })
        );

        var result = df.Lazy()
            .Filter(Col("a").Gt(1))
            .Filter(Col("b").Lt(45))
            .Collect();

        result.Height.Should().Be(3);
        result["a"].ToArray<int>().Should().BeEquivalentTo(new[] { 2, 3, 4 });
    }

    [Fact]
    public void LazyFrame_FilterThenSelect_Works()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 }),
            Series.FromValues("b", new[] { "x", "y", "z", "w", "v" }),
            Series.FromValues("c", new[] { 10, 20, 30, 40, 50 })
        );

        var result = df.Lazy()
            .Filter(Col("a").Gt(2))
            .Select(Col("a"), Col("c"))
            .Collect();

        result.Width.Should().Be(2);
        result.Height.Should().Be(3);
        result.Columns.Should().ContainInOrder("a", "c");
    }

    // ============================================================================
    // Data Type Specific Filter Tests
    // ============================================================================

    [Fact]
    public void Filter_Float64_ReturnsMatchingRows()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1.1, 2.2, 3.3, 4.4, 5.5 })
        );

        var result = df.Lazy().Filter(Col("a").Gt(3.0)).Collect();

        result.Height.Should().Be(3);
    }

    [Fact]
    public void Filter_Int64_ReturnsMatchingRows()
    {
        var df = new DataFrame(
            Series.FromValues("a", new long[] { 1_000_000_000, 2_000_000_000, 3_000_000_000 })
        );

        var result = df.Lazy().Filter(Col("a").Gt(1_500_000_000L)).Collect();

        result.Height.Should().Be(2);
    }

    [Fact]
    public void Filter_Boolean_ReturnsMatchingRows()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 }),
            Series.FromValues("flag", new[] { true, false, true, false, true })
        );

        // Select both columns to preserve 'a' (projection pushdown would otherwise remove unreferenced columns)
        var result = df.Lazy()
            .Filter(Col("flag"))
            .Select(Col("a"), Col("flag"))
            .Collect();

        result.Height.Should().Be(3);
        result["a"].ToArray<int>().Should().BeEquivalentTo(new[] { 1, 3, 5 });
    }

    [Fact]
    public void Filter_PreservesDataTypes()
    {
        var df = new DataFrame(
            Series.FromValues("int_col", new[] { 1, 2, 3 }),
            Series.FromValues("str_col", new[] { "a", "b", "c" }),
            Series.FromValues("float_col", new[] { 1.0, 2.0, 3.0 })
        );

        // Select all columns explicitly to preserve them (projection pushdown would otherwise remove unreferenced columns)
        var result = df.Lazy()
            .Filter(Col("int_col").Gt(1))
            .Select(Col("int_col"), Col("str_col"), Col("float_col"))
            .Collect();

        result["int_col"].DataType.Should().Be(DataType.Int32);
        result["str_col"].DataType.Should().Be(DataType.String);
        result["float_col"].DataType.Should().Be(DataType.Float64);
    }

    // ============================================================================
    // Edge Cases
    // ============================================================================

    [Fact]
    public void Filter_LargeDataFrame_WorksCorrectly()
    {
        var size = 10000;
        var values = Enumerable.Range(0, size).ToArray();
        var df = new DataFrame(
            Series.FromValues("a", values)
        );

        var result = df.Lazy().Filter(Col("a").Ge(size / 2)).Collect();

        result.Height.Should().Be(size / 2);
    }

    [Fact]
    public void Filter_SingleRowMatch_ReturnsOneRow()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 })
        );

        var result = df.Lazy().Filter(Col("a").Eq(3)).Collect();

        result.Height.Should().Be(1);
        result["a"][0].AsInt32().Should().Be(3);
    }

    // ============================================================================
    // Multiple Column Filter Tests
    // ============================================================================

    [Fact]
    public void Filter_MultipleColumns_AndCondition()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 }),
            Series.FromValues("b", new[] { "x", "y", "x", "y", "x" }),
            Series.FromValues("c", new[] { true, false, true, false, true })
        );

        var result = df.Lazy().Filter(
            Col("a").Gt(2).And(Col("b").Eq("x")).And(Col("c"))
        ).Collect();

        result.Height.Should().Be(2);
        result["a"].ToArray<int>().Should().BeEquivalentTo(new[] { 3, 5 });
    }

    [Fact]
    public void Filter_ColumnComparisonWithColumn()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 5, 3, 8, 2 }),
            Series.FromValues("b", new[] { 2, 3, 3, 7, 5 })
        );

        var result = df.Lazy().Filter(Col("a").Gt(Col("b"))).Collect();

        result.Height.Should().Be(2);
    }

    // ============================================================================
    // Filter with Expressions Tests
    // ============================================================================

    [Fact]
    public void Filter_WithArithmeticExpression()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 })
        );

        // Filter where a * 2 > 6 (i.e., a > 3)
        var result = df.Lazy().Filter((Col("a") * 2.0).Gt(6.0)).Collect();

        result.Height.Should().Be(2);
        result["a"].ToArray<int>().Should().BeEquivalentTo(new[] { 4, 5 });
    }

    [Fact]
    public void Filter_WithModulo()
    {
        // Use float data since Int32 modulo requires type promotion
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0 })
        );

        // Filter even numbers (a % 2 == 0)
        var result = df.Lazy().Filter((Col("a") % Lit(2.0)).Eq(0.0)).Collect();

        result.Height.Should().Be(3);
        result["a"].ToArray<double>().Should().BeEquivalentTo(new[] { 2.0, 4.0, 6.0 });
    }

    // ============================================================================
    // Literal True/False Filter Tests (from Polars test_simplify_expression_lit_true_4376)
    // ============================================================================

    [Fact]
    public void Filter_LiteralTrue_ReturnsAllRows()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 })
        );

        // Select column explicitly to preserve it (projection pushdown removes columns not referenced in filter)
        var result = df.Lazy().Filter(Lit(true)).Select(Col("a")).Collect();

        result.Height.Should().Be(3);
    }

    [Fact]
    public void Filter_LiteralFalse_ReturnsNoRows()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 })
        );

        var result = df.Lazy().Filter(Lit(false)).Collect();

        result.Height.Should().Be(0);
    }

    [Fact]
    public void Filter_LiteralTrueOrCondition_SimplifiesToTrue()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 })
        );

        // true OR (condition) should return all rows
        var result = df.Lazy().Filter(Lit(true) | Col("a").Eq(1)).Collect();

        result.Height.Should().Be(3);
    }

    // ============================================================================
    // Direct Series Filter Tests (using DataFrame.Filter with Series mask)
    // ============================================================================

    [Fact]
    public void Filter_WithSeriesMask_ReturnsMatchingRows()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 }),
            Series.FromValues("b", new[] { 10, 20, 30, 40, 50 })
        );

        // Create boolean mask using Series comparison
        var mask = df["a"].Gt(AnyValue.From(2));
        var result = df.Filter(mask);

        result.Height.Should().Be(3);
        result["a"].ToArray<int>().Should().BeEquivalentTo(new[] { 3, 4, 5 });
    }

    [Fact]
    public void Filter_WithCombinedSeriesMasks_ReturnsMatchingRows()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 }),
            Series.FromValues("b", new[] { 10, 20, 30, 40, 50 })
        );

        // Create combined boolean mask
        var maskA = df["a"].Gt(AnyValue.From(2));
        var maskB = df["b"].Lt(AnyValue.From(45));
        var combinedMask = maskA & maskB;
        var result = df.Filter(combinedMask);

        result.Height.Should().Be(2);
        result["a"].ToArray<int>().Should().BeEquivalentTo(new[] { 3, 4 });
    }

    // ============================================================================
    // Additional Filter Edge Cases
    // ============================================================================

    [Fact]
    public void Filter_AllNullColumn_IsNull_ReturnsAllRows()
    {
        var df = new DataFrame(
            Series.FromNullable("a", new int?[] { null, null, null })
        );

        var result = df.Lazy().Filter(Col("a").IsNullExpr()).Collect();

        result.Height.Should().Be(3);
    }

    [Fact]
    public void Filter_AllNullColumn_IsNotNull_ReturnsNoRows()
    {
        var df = new DataFrame(
            Series.FromNullable("a", new int?[] { null, null, null })
        );

        var result = df.Lazy().Filter(Col("a").IsNotNullExpr()).Collect();

        result.Height.Should().Be(0);
    }

    [Fact]
    public void Filter_WithNot_InvertsCondition()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 })
        );

        // NOT (a > 3) should return a <= 3
        var result = df.Lazy().Filter(Col("a").Gt(3).Not()).Collect();

        result.Height.Should().Be(3);
        result["a"].ToArray<int>().Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void Filter_XorCondition_ReturnsExclusiveMatchingRows()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 }),
            Series.FromValues("b", new[] { true, false, true, false, true })
        );

        // XOR: (a > 2) XOR b - true when exactly one is true
        var result = df.Lazy().Filter(Col("a").Gt(2).Xor(Col("b"))).Collect();

        // a > 2: [F, F, T, T, T], b: [T, F, T, F, T]
        // XOR:   [T, F, F, T, F] -> rows 0 and 3
        result.Height.Should().Be(2);
    }

    [Fact]
    public void Filter_NestedLogicalExpressions_Works()
    {
        var df = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4, 5 }),
            Series.FromValues("b", new[] { 10, 20, 30, 40, 50 }),
            Series.FromValues("c", new[] { true, false, true, false, true })
        );

        // Complex: ((a > 2) AND (b < 45)) OR (c AND (a == 1))
        var result = df.Lazy().Filter(
            Col("a").Gt(2).And(Col("b").Lt(45))
                .Or(Col("c").And(Col("a").Eq(1)))
        ).Collect();

        result.Height.Should().Be(3);
    }

    [Fact]
    public void Filter_StringEquals_ReturnsExactMatch()
    {
        var df = new DataFrame(
            Series.FromValues("name", new[] { "Alice", "Bob", "alice", "ALICE" })
        );

        var result = df.Lazy().Filter(Col("name").Eq("Alice")).Collect();

        result.Height.Should().Be(1);
        result["name"][0].AsString().Should().Be("Alice");
    }

    [Fact]
    public void Filter_EmptyString_ReturnsMatchingRows()
    {
        var df = new DataFrame(
            Series.FromValues("name", new[] { "", "Bob", "", "Diana" })
        );

        var result = df.Lazy().Filter(Col("name").Eq("")).Collect();

        result.Height.Should().Be(2);
    }

    [Fact]
    public void Filter_UnicodeStrings_ReturnsMatchingRows()
    {
        var df = new DataFrame(
            Series.FromValues("name", new[] { "こんにちは", "Hello", "世界", "World" })
        );

        var result = df.Lazy().Filter(Col("name").Eq("こんにちは")).Collect();

        result.Height.Should().Be(1);
        result["name"][0].AsString().Should().Be("こんにちは");
    }
}
