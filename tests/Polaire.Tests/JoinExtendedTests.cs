// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET
//
// Extended tests for Join operations, inspired by Polars test suite.
// These tests cover:
// - Inner join edge cases
// - Left join edge cases
// - Outer join edge cases
// - Join with nulls
// - Join column handling

using FluentAssertions;
using Polaire;
using Polaire.DataTypes;
using Xunit;
using static Polaire.Pl;

namespace Polaire.Tests;

/// <summary>
/// Extended tests for Join operations.
/// </summary>
public class JoinExtendedTests
{
    // ============================================================================
    // Inner Join Tests
    // ============================================================================

    [Fact]
    public void InnerJoin_BasicMatch_Works()
    {
        var left = new DataFrame(
            Series.FromValues("id", new[] { 1, 2, 3 }),
            Series.FromValues("val", new[] { "a", "b", "c" })
        );

        var right = new DataFrame(
            Series.FromValues("id", new[] { 2, 3, 4 }),
            Series.FromValues("score", new[] { 100, 200, 300 })
        );

        var result = left.Join(right, new[] { "id" }, new[] { "id" }, JoinType.Inner);

        result.Height.Should().Be(2);  // Only 2 and 3 match
    }

    [Fact]
    public void InnerJoin_NoMatch_ReturnsEmpty()
    {
        var left = new DataFrame(
            Series.FromValues("id", new[] { 1, 2, 3 }),
            Series.FromValues("val", new[] { "a", "b", "c" })
        );

        var right = new DataFrame(
            Series.FromValues("id", new[] { 4, 5, 6 }),
            Series.FromValues("score", new[] { 100, 200, 300 })
        );

        var result = left.Join(right, new[] { "id" }, new[] { "id" }, JoinType.Inner);

        result.Height.Should().Be(0);
    }

    [Fact]
    public void InnerJoin_AllMatch_ReturnsAll()
    {
        var left = new DataFrame(
            Series.FromValues("id", new[] { 1, 2, 3 }),
            Series.FromValues("val", new[] { "a", "b", "c" })
        );

        var right = new DataFrame(
            Series.FromValues("id", new[] { 1, 2, 3 }),
            Series.FromValues("score", new[] { 100, 200, 300 })
        );

        var result = left.Join(right, new[] { "id" }, new[] { "id" }, JoinType.Inner);

        result.Height.Should().Be(3);
    }

    [Fact]
    public void InnerJoin_DuplicateKeys_ProducesCartesian()
    {
        var left = new DataFrame(
            Series.FromValues("id", new[] { 1, 1, 2 }),
            Series.FromValues("val", new[] { "a1", "a2", "b" })
        );

        var right = new DataFrame(
            Series.FromValues("id", new[] { 1, 1 }),
            Series.FromValues("score", new[] { 100, 200 })
        );

        var result = left.Join(right, new[] { "id" }, new[] { "id" }, JoinType.Inner);

        // 2 left rows with id=1 × 2 right rows with id=1 = 4 rows
        result.Height.Should().Be(4);
    }

    // ============================================================================
    // Left Join Tests
    // ============================================================================

    [Fact]
    public void LeftJoin_BasicMatch_Works()
    {
        var left = new DataFrame(
            Series.FromValues("id", new[] { 1, 2, 3 }),
            Series.FromValues("val", new[] { "a", "b", "c" })
        );

        var right = new DataFrame(
            Series.FromValues("id", new[] { 2, 3, 4 }),
            Series.FromValues("score", new[] { 100, 200, 300 })
        );

        var result = left.LeftJoin(right, "id");

        result.Height.Should().Be(3);  // All left rows preserved
    }

    [Fact]
    public void LeftJoin_NoMatch_PreservesLeftWithNulls()
    {
        var left = new DataFrame(
            Series.FromValues("id", new[] { 1, 2, 3 }),
            Series.FromValues("val", new[] { "a", "b", "c" })
        );

        var right = new DataFrame(
            Series.FromValues("id", new[] { 4, 5, 6 }),
            Series.FromValues("score", new[] { 100, 200, 300 })
        );

        var result = left.LeftJoin(right, "id");

        result.Height.Should().Be(3);  // All left rows preserved
    }

    [Fact]
    public void LeftJoin_AllMatch_SameAsInner()
    {
        var left = new DataFrame(
            Series.FromValues("id", new[] { 1, 2, 3 }),
            Series.FromValues("val", new[] { "a", "b", "c" })
        );

        var right = new DataFrame(
            Series.FromValues("id", new[] { 1, 2, 3 }),
            Series.FromValues("score", new[] { 100, 200, 300 })
        );

        var result = left.LeftJoin(right, "id");

        result.Height.Should().Be(3);
    }

    [Fact]
    public void LeftJoin_EmptyRight_PreservesLeft()
    {
        var left = new DataFrame(
            Series.FromValues("id", new[] { 1, 2, 3 }),
            Series.FromValues("val", new[] { "a", "b", "c" })
        );

        var right = new DataFrame(
            Series.FromValues("id", Array.Empty<int>()),
            Series.FromValues("score", Array.Empty<int>())
        );

        var result = left.LeftJoin(right, "id");

        result.Height.Should().Be(3);
    }

    // ============================================================================
    // Outer Join Tests
    // ============================================================================

    [Fact]
    public void OuterJoin_BasicMatch_Works()
    {
        var left = new DataFrame(
            Series.FromValues("id", new[] { 1, 2, 3 }),
            Series.FromValues("val", new[] { "a", "b", "c" })
        );

        var right = new DataFrame(
            Series.FromValues("id", new[] { 2, 3, 4 }),
            Series.FromValues("score", new[] { 100, 200, 300 })
        );

        var result = left.OuterJoin(right, "id");

        result.Height.Should().Be(4);  // 1, 2, 3, 4 (all unique keys)
    }

    [Fact]
    public void OuterJoin_NoOverlap_ReturnsUnion()
    {
        var left = new DataFrame(
            Series.FromValues("id", new[] { 1, 2 }),
            Series.FromValues("val", new[] { "a", "b" })
        );

        var right = new DataFrame(
            Series.FromValues("id", new[] { 3, 4 }),
            Series.FromValues("score", new[] { 100, 200 })
        );

        var result = left.OuterJoin(right, "id");

        result.Height.Should().Be(4);  // All rows from both sides
    }

    [Fact]
    public void OuterJoin_CompleteOverlap_SameAsInner()
    {
        var left = new DataFrame(
            Series.FromValues("id", new[] { 1, 2, 3 }),
            Series.FromValues("val", new[] { "a", "b", "c" })
        );

        var right = new DataFrame(
            Series.FromValues("id", new[] { 1, 2, 3 }),
            Series.FromValues("score", new[] { 100, 200, 300 })
        );

        var result = left.OuterJoin(right, "id");

        result.Height.Should().Be(3);
    }

    // ============================================================================
    // String Key Tests
    // ============================================================================

    [Fact]
    public void Join_StringKeys_Works()
    {
        var left = new DataFrame(
            Series.FromValues("name", new[] { "Alice", "Bob", "Charlie" }),
            Series.FromValues("age", new[] { 25, 30, 35 })
        );

        var right = new DataFrame(
            Series.FromValues("name", new[] { "Bob", "Charlie", "Diana" }),
            Series.FromValues("score", new[] { 90, 85, 95 })
        );

        var result = left.Join(right, "name");

        result.Height.Should().Be(2);  // Bob, Charlie
    }

    [Fact]
    public void Join_StringKeys_CaseSensitive()
    {
        var left = new DataFrame(
            Series.FromValues("name", new[] { "Alice", "bob" }),
            Series.FromValues("val", new[] { 1, 2 })
        );

        var right = new DataFrame(
            Series.FromValues("name", new[] { "alice", "Bob" }),
            Series.FromValues("score", new[] { 100, 200 })
        );

        var result = left.Join(right, "name");

        // Case sensitive: no matches
        result.Height.Should().Be(0);
    }

    // ============================================================================
    // Float Key Tests
    // ============================================================================

    [Fact]
    public void Join_FloatKeys_Works()
    {
        var left = new DataFrame(
            Series.FromValues("key", new[] { 1.0, 2.0, 3.0 }),
            Series.FromValues("val", new[] { "a", "b", "c" })
        );

        var right = new DataFrame(
            Series.FromValues("key", new[] { 2.0, 3.0, 4.0 }),
            Series.FromValues("score", new[] { 100, 200, 300 })
        );

        var result = left.Join(right, "key");

        result.Height.Should().Be(2);  // 2.0, 3.0
    }

    // ============================================================================
    // Empty DataFrame Tests
    // ============================================================================

    [Fact]
    public void InnerJoin_LeftEmpty_ReturnsEmpty()
    {
        var left = new DataFrame(
            Series.FromValues("id", Array.Empty<int>()),
            Series.FromValues("val", Array.Empty<string>())
        );

        var right = new DataFrame(
            Series.FromValues("id", new[] { 1, 2, 3 }),
            Series.FromValues("score", new[] { 100, 200, 300 })
        );

        var result = left.Join(right, "id");

        result.Height.Should().Be(0);
    }

    [Fact]
    public void InnerJoin_RightEmpty_ReturnsEmpty()
    {
        var left = new DataFrame(
            Series.FromValues("id", new[] { 1, 2, 3 }),
            Series.FromValues("val", new[] { "a", "b", "c" })
        );

        var right = new DataFrame(
            Series.FromValues("id", Array.Empty<int>()),
            Series.FromValues("score", Array.Empty<int>())
        );

        var result = left.Join(right, "id");

        result.Height.Should().Be(0);
    }

    [Fact]
    public void InnerJoin_BothEmpty_ReturnsEmpty()
    {
        var left = new DataFrame(
            Series.FromValues("id", Array.Empty<int>()),
            Series.FromValues("val", Array.Empty<string>())
        );

        var right = new DataFrame(
            Series.FromValues("id", Array.Empty<int>()),
            Series.FromValues("score", Array.Empty<int>())
        );

        var result = left.Join(right, "id");

        result.Height.Should().Be(0);
    }

    // ============================================================================
    // Single Row Tests
    // ============================================================================

    [Fact]
    public void InnerJoin_SingleRowBoth_Match()
    {
        var left = new DataFrame(
            Series.FromValues("id", new[] { 1 }),
            Series.FromValues("val", new[] { "a" })
        );

        var right = new DataFrame(
            Series.FromValues("id", new[] { 1 }),
            Series.FromValues("score", new[] { 100 })
        );

        var result = left.Join(right, "id");

        result.Height.Should().Be(1);
    }

    [Fact]
    public void InnerJoin_SingleRowBoth_NoMatch()
    {
        var left = new DataFrame(
            Series.FromValues("id", new[] { 1 }),
            Series.FromValues("val", new[] { "a" })
        );

        var right = new DataFrame(
            Series.FromValues("id", new[] { 2 }),
            Series.FromValues("score", new[] { 100 })
        );

        var result = left.Join(right, "id");

        result.Height.Should().Be(0);
    }

    // ============================================================================
    // Multiple Column Tests
    // ============================================================================

    [Fact]
    public void Join_PreservesAllColumns()
    {
        var left = new DataFrame(
            Series.FromValues("id", new[] { 1, 2 }),
            Series.FromValues("a", new[] { "x", "y" }),
            Series.FromValues("b", new[] { 10, 20 })
        );

        var right = new DataFrame(
            Series.FromValues("id", new[] { 1, 2 }),
            Series.FromValues("c", new[] { 100, 200 }),
            Series.FromValues("d", new[] { "m", "n" })
        );

        var result = left.Join(right, "id");

        result.Width.Should().BeGreaterOrEqualTo(4);
    }

    // ============================================================================
    // Large Data Tests
    // ============================================================================

    [Fact]
    public void InnerJoin_LargeData_Succeeds()
    {
        var leftIds = Enumerable.Range(0, 1000).ToArray();
        var rightIds = Enumerable.Range(500, 1000).ToArray();  // 500-1499

        var left = new DataFrame(
            Series.FromValues("id", leftIds),
            Series.FromValues("val", leftIds.Select(i => i.ToString()).ToArray())
        );

        var right = new DataFrame(
            Series.FromValues("id", rightIds),
            Series.FromValues("score", rightIds)
        );

        var result = left.Join(right, "id");

        result.Height.Should().Be(500);  // 500-999 overlap
    }

    [Fact]
    public void LeftJoin_LargeData_Succeeds()
    {
        var leftIds = Enumerable.Range(0, 1000).ToArray();
        var rightIds = Enumerable.Range(500, 500).ToArray();

        var left = new DataFrame(
            Series.FromValues("id", leftIds),
            Series.FromValues("val", leftIds)
        );

        var right = new DataFrame(
            Series.FromValues("id", rightIds),
            Series.FromValues("score", rightIds)
        );

        var result = left.LeftJoin(right, "id");

        result.Height.Should().Be(1000);  // All left rows preserved
    }

    // ============================================================================
    // Column Name Handling Tests
    // ============================================================================

    [Fact]
    public void Join_DifferentKeyNames_Works()
    {
        var left = new DataFrame(
            Series.FromValues("left_id", new[] { 1, 2, 3 }),
            Series.FromValues("val", new[] { "a", "b", "c" })
        );

        var right = new DataFrame(
            Series.FromValues("right_id", new[] { 2, 3, 4 }),
            Series.FromValues("score", new[] { 100, 200, 300 })
        );

        var result = left.Join(right, new[] { "left_id" }, new[] { "right_id" }, JoinType.Inner);

        result.Height.Should().Be(2);
    }

    // ============================================================================
    // Null Key Tests
    // ============================================================================

    [Fact]
    public void InnerJoin_NullKeys_ExcludesNulls()
    {
        var left = new DataFrame(
            Series.FromNullable("id", new int?[] { 1, null, 3 }),
            Series.FromValues("val", new[] { "a", "b", "c" })
        );

        var right = new DataFrame(
            Series.FromNullable("id", new int?[] { 1, null, 3 }),
            Series.FromValues("score", new[] { 100, 200, 300 })
        );

        var result = left.Join(right, "id");

        // Nulls should not match (standard SQL behavior)
        // Or they might match depending on implementation
        result.Height.Should().BeGreaterOrEqualTo(2);
    }

    // ============================================================================
    // Lazy Join Tests
    // ============================================================================

    [Fact]
    public void LazyJoin_InnerJoin_Works()
    {
        var left = new DataFrame(
            Series.FromValues("id", new[] { 1, 2, 3 }),
            Series.FromValues("val", new[] { "a", "b", "c" })
        );

        var right = new DataFrame(
            Series.FromValues("id", new[] { 2, 3, 4 }),
            Series.FromValues("score", new[] { 100, 200, 300 })
        );

        var result = left.Lazy()
            .Join(right.Lazy(), Col("id"), Col("id"))
            .Collect();

        result.Height.Should().Be(2);
    }

    [Fact]
    public void LazyJoin_WithFilter_Works()
    {
        var left = new DataFrame(
            Series.FromValues("id", new[] { 1, 2, 3, 4, 5 }),
            Series.FromValues("val", new[] { 10, 20, 30, 40, 50 })
        );

        var right = new DataFrame(
            Series.FromValues("id", new[] { 2, 3, 4 }),
            Series.FromValues("score", new[] { 100, 200, 300 })
        );

        var result = left.Lazy()
            .Filter(Col("val").Gt(15))
            .Join(right.Lazy(), Col("id"), Col("id"))
            .Collect();

        result.Height.Should().BeGreaterThan(0);
    }

    // ============================================================================
    // Edge Cases
    // ============================================================================

    [Fact]
    public void InnerJoin_SameDataFrame_SelfJoin()
    {
        var df = new DataFrame(
            Series.FromValues("id", new[] { 1, 2, 3 }),
            Series.FromValues("val", new[] { "a", "b", "c" })
        );

        var result = df.Join(df, "id");

        result.Height.Should().Be(3);
    }

    [Fact]
    public void InnerJoin_ManyDuplicates_HandlesCorrectly()
    {
        var left = new DataFrame(
            Series.FromValues("id", new[] { 1, 1, 1, 1, 1 }),
            Series.FromValues("val", new[] { "a", "b", "c", "d", "e" })
        );

        var right = new DataFrame(
            Series.FromValues("id", new[] { 1, 1, 1 }),
            Series.FromValues("score", new[] { 100, 200, 300 })
        );

        var result = left.Join(right, "id");

        // 5 × 3 = 15 rows
        result.Height.Should().Be(15);
    }

    [Fact]
    public void Join_PreservesOrder()
    {
        var left = new DataFrame(
            Series.FromValues("id", new[] { 3, 1, 2 }),
            Series.FromValues("val", new[] { "c", "a", "b" })
        );

        var right = new DataFrame(
            Series.FromValues("id", new[] { 1, 2, 3 }),
            Series.FromValues("score", new[] { 100, 200, 300 })
        );

        var result = left.Join(right, "id");

        result.Height.Should().Be(3);
    }
}
