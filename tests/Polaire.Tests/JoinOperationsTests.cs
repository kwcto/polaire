// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using FluentAssertions;
using Polaire;
using Polaire.DataTypes;
using Xunit;

namespace Polaire.Tests;

/// <summary>
/// Tests for Join operations on DataFrame.
/// </summary>
public class JoinOperationsTests
{
    // ============================================================================
    // Inner Join Tests
    // ============================================================================

    [Fact]
    public void Join_InnerJoin_ReturnsMatchingRows()
    {
        var left = new DataFrame(
            Series.FromValues("id", new[] { 1, 2, 3, 4 }),
            Series.FromValues("name", new[] { "Alice", "Bob", "Charlie", "Diana" })
        );
        var right = new DataFrame(
            Series.FromValues("id", new[] { 2, 3, 5 }),
            Series.FromValues("score", new[] { 85, 90, 95 })
        );

        var result = left.Join(right, "id");

        result.Height.Should().Be(2); // Only ids 2 and 3 match
        result.Columns.Should().Contain("id");
        result.Columns.Should().Contain("name");
        result.Columns.Should().Contain("score");
    }

    [Fact]
    public void Join_InnerJoin_NoMatches_ReturnsEmpty()
    {
        var left = new DataFrame(
            Series.FromValues("id", new[] { 1, 2, 3 }),
            Series.FromValues("value", new[] { 10, 20, 30 })
        );
        var right = new DataFrame(
            Series.FromValues("id", new[] { 4, 5, 6 }),
            Series.FromValues("other", new[] { 40, 50, 60 })
        );

        var result = left.Join(right, "id");

        result.Height.Should().Be(0);
    }

    [Fact]
    public void Join_InnerJoin_AllMatch_ReturnsAllRows()
    {
        var left = new DataFrame(
            Series.FromValues("id", new[] { 1, 2, 3 }),
            Series.FromValues("left_val", new[] { 10, 20, 30 })
        );
        var right = new DataFrame(
            Series.FromValues("id", new[] { 1, 2, 3 }),
            Series.FromValues("right_val", new[] { 100, 200, 300 })
        );

        var result = left.Join(right, "id");

        result.Height.Should().Be(3);
    }

    // ============================================================================
    // Left Join Tests
    // ============================================================================

    [Fact]
    public void LeftJoin_KeepsAllLeftRows()
    {
        var left = new DataFrame(
            Series.FromValues("id", new[] { 1, 2, 3, 4 }),
            Series.FromValues("name", new[] { "Alice", "Bob", "Charlie", "Diana" })
        );
        var right = new DataFrame(
            Series.FromValues("id", new[] { 2, 4 }),
            Series.FromValues("score", new[] { 85, 95 })
        );

        var result = left.LeftJoin(right, "id");

        result.Height.Should().Be(4);
        result.Columns.Should().Contain("id");
        result.Columns.Should().Contain("name");
        result.Columns.Should().Contain("score");
    }

    [Fact]
    public void LeftJoin_NoMatches_KeepsLeftWithNulls()
    {
        var left = new DataFrame(
            Series.FromValues("id", new[] { 1, 2, 3 }),
            Series.FromValues("value", new[] { 10, 20, 30 })
        );
        var right = new DataFrame(
            Series.FromValues("id", new[] { 4, 5 }),
            Series.FromValues("other", new[] { 40, 50 })
        );

        var result = left.LeftJoin(right, "id");

        result.Height.Should().Be(3);
        // other column should have nulls for all rows
    }

    // ============================================================================
    // Outer Join Tests
    // ============================================================================

    [Fact]
    public void OuterJoin_KeepsAllRows()
    {
        var left = new DataFrame(
            Series.FromValues("id", new[] { 1, 2 }),
            Series.FromValues("left_val", new[] { 10, 20 })
        );
        var right = new DataFrame(
            Series.FromValues("id", new[] { 2, 3 }),
            Series.FromValues("right_val", new[] { 200, 300 })
        );

        var result = left.OuterJoin(right, "id");

        result.Height.Should().Be(3); // ids 1, 2, 3
    }

    // ============================================================================
    // Different Column Names Join Tests
    // ============================================================================

    [Fact]
    public void Join_DifferentColumnNames_WorksCorrectly()
    {
        var left = new DataFrame(
            Series.FromValues("left_id", new[] { 1, 2, 3 }),
            Series.FromValues("name", new[] { "A", "B", "C" })
        );
        var right = new DataFrame(
            Series.FromValues("right_id", new[] { 2, 3, 4 }),
            Series.FromValues("value", new[] { 20, 30, 40 })
        );

        var result = left.Join(right, new[] { "left_id" }, new[] { "right_id" });

        result.Height.Should().Be(2);
        result.Columns.Should().Contain("left_id");
    }

    // ============================================================================
    // Suffix Tests
    // ============================================================================

    [Fact]
    public void Join_WithDuplicateColumnNames_AddsSuffix()
    {
        var left = new DataFrame(
            Series.FromValues("id", new[] { 1, 2 }),
            Series.FromValues("value", new[] { 10, 20 })
        );
        var right = new DataFrame(
            Series.FromValues("id", new[] { 1, 2 }),
            Series.FromValues("value", new[] { 100, 200 })
        );

        var result = left.Join(right, "id");

        result.Columns.Should().Contain("value");
        result.Columns.Should().Contain("value_right");
    }

    [Fact]
    public void Join_CustomSuffix_UsesCustomSuffix()
    {
        var left = new DataFrame(
            Series.FromValues("id", new[] { 1, 2 }),
            Series.FromValues("value", new[] { 10, 20 })
        );
        var right = new DataFrame(
            Series.FromValues("id", new[] { 1, 2 }),
            Series.FromValues("value", new[] { 100, 200 })
        );

        var result = left.Join(right, "id", suffix: "_other");

        result.Columns.Should().Contain("value");
        result.Columns.Should().Contain("value_other");
    }

    // ============================================================================
    // Multiple Keys Join Tests
    // ============================================================================

    [Fact]
    public void Join_MultipleKeys_MatchesOnAllKeys()
    {
        var left = new DataFrame(
            Series.FromValues("year", new[] { 2020, 2020, 2021, 2021 }),
            Series.FromValues("month", new[] { 1, 2, 1, 2 }),
            Series.FromValues("sales", new[] { 100, 200, 150, 250 })
        );
        var right = new DataFrame(
            Series.FromValues("year", new[] { 2020, 2021 }),
            Series.FromValues("month", new[] { 1, 2 }),
            Series.FromValues("target", new[] { 120, 230 })
        );

        var result = left.Join(right, new[] { "year", "month" }, new[] { "year", "month" });

        result.Height.Should().Be(2);
    }

    // ============================================================================
    // Edge Cases
    // ============================================================================

    [Fact]
    public void Join_EmptyLeftDataFrame_ReturnsEmpty()
    {
        var left = new DataFrame(
            Series.FromValues("id", Array.Empty<int>()),
            Series.FromValues("value", Array.Empty<int>())
        );
        var right = new DataFrame(
            Series.FromValues("id", new[] { 1, 2 }),
            Series.FromValues("other", new[] { 10, 20 })
        );

        var result = left.Join(right, "id");

        result.Height.Should().Be(0);
    }

    [Fact]
    public void Join_EmptyRightDataFrame_ReturnsEmpty()
    {
        var left = new DataFrame(
            Series.FromValues("id", new[] { 1, 2 }),
            Series.FromValues("value", new[] { 10, 20 })
        );
        var right = new DataFrame(
            Series.FromValues("id", Array.Empty<int>()),
            Series.FromValues("other", Array.Empty<int>())
        );

        var result = left.Join(right, "id");

        result.Height.Should().Be(0);
    }

    [Fact]
    public void Join_SingleRow_WorksCorrectly()
    {
        var left = new DataFrame(
            Series.FromValues("id", new[] { 1 }),
            Series.FromValues("name", new[] { "Alice" })
        );
        var right = new DataFrame(
            Series.FromValues("id", new[] { 1 }),
            Series.FromValues("score", new[] { 100 })
        );

        var result = left.Join(right, "id");

        result.Height.Should().Be(1);
    }

    [Fact]
    public void Join_DuplicateKeysInRight_CreatesDuplicateRows()
    {
        var left = new DataFrame(
            Series.FromValues("id", new[] { 1, 2 }),
            Series.FromValues("name", new[] { "Alice", "Bob" })
        );
        var right = new DataFrame(
            Series.FromValues("id", new[] { 1, 1, 2 }),
            Series.FromValues("score", new[] { 85, 90, 95 })
        );

        var result = left.Join(right, "id");

        // Alice matches twice (scores 85, 90), Bob matches once (score 95)
        result.Height.Should().Be(3);
    }

    [Fact]
    public void Join_DuplicateKeysInBoth_CreatesCartesianProduct()
    {
        var left = new DataFrame(
            Series.FromValues("id", new[] { 1, 1 }),
            Series.FromValues("left_val", new[] { "a", "b" })
        );
        var right = new DataFrame(
            Series.FromValues("id", new[] { 1, 1 }),
            Series.FromValues("right_val", new[] { "x", "y" })
        );

        var result = left.Join(right, "id");

        // Cartesian product: 2 x 2 = 4 rows
        result.Height.Should().Be(4);
    }

    [Fact]
    public void Join_StringKeys_Basic_WorksCorrectly()
    {
        var left = new DataFrame(
            Series.FromValues("key", new[] { "A", "B", "C" }),
            Series.FromValues("left_val", new[] { 1, 2, 3 })
        );
        var right = new DataFrame(
            Series.FromValues("key", new[] { "B", "C", "D" }),
            Series.FromValues("right_val", new[] { 20, 30, 40 })
        );

        var result = left.Join(right, "key");

        result.Height.Should().Be(2); // B and C match
    }

    [Fact]
    public void LeftJoin_WithNullsInKey_HandlesCorrectly()
    {
        var left = new DataFrame(
            Series.FromNullable("id", new int?[] { 1, null, 3 }),
            Series.FromValues("name", new[] { "A", "B", "C" })
        );
        var right = new DataFrame(
            Series.FromValues("id", new[] { 1, 3 }),
            Series.FromValues("value", new[] { 10, 30 })
        );

        var result = left.LeftJoin(right, "id");

        result.Height.Should().Be(3);
    }

    [Fact]
    public void Join_LargeDataFrames_WorksCorrectly()
    {
        var size = 1000;
        var left = new DataFrame(
            Series.FromValues("id", Enumerable.Range(0, size).ToArray()),
            Series.FromValues("left_val", Enumerable.Range(0, size).Select(i => (double)i).ToArray())
        );
        var right = new DataFrame(
            Series.FromValues("id", Enumerable.Range(500, size).ToArray()),
            Series.FromValues("right_val", Enumerable.Range(500, size).Select(i => (double)i * 10).ToArray())
        );

        var result = left.Join(right, "id");

        result.Height.Should().Be(500); // ids 500-999 match
    }

    // ============================================================================
    // Column Order Tests
    // ============================================================================

    [Fact]
    public void Join_PreservesLeftColumnOrder()
    {
        var left = new DataFrame(
            Series.FromValues("id", new[] { 1, 2 }),
            Series.FromValues("a", new[] { "x", "y" }),
            Series.FromValues("b", new[] { 10, 20 })
        );
        var right = new DataFrame(
            Series.FromValues("id", new[] { 1, 2 }),
            Series.FromValues("c", new[] { 100, 200 })
        );

        var result = left.Join(right, "id");

        result.Columns[0].Should().Be("id");
        result.Columns[1].Should().Be("a");
        result.Columns[2].Should().Be("b");
        result.Columns[3].Should().Be("c");
    }

    // ============================================================================
    // Many Columns Tests
    // ============================================================================

    [Fact]
    public void Join_ManyColumns_WorksCorrectly()
    {
        var left = new DataFrame(
            Series.FromValues("id", new[] { 1, 2 }),
            Series.FromValues("col1", new[] { "a", "b" }),
            Series.FromValues("col2", new[] { 1.0, 2.0 }),
            Series.FromValues("col3", new[] { true, false })
        );
        var right = new DataFrame(
            Series.FromValues("id", new[] { 1, 2 }),
            Series.FromValues("col4", new[] { "x", "y" }),
            Series.FromValues("col5", new[] { 10.0, 20.0 }),
            Series.FromValues("col6", new[] { false, true })
        );

        var result = left.Join(right, "id");

        result.Width.Should().Be(7); // id + 3 left + 3 right
    }

    // ============================================================================
    // Semi Join Tests (ported from Polars test_semi_anti_join)
    // ============================================================================

    [Fact]
    public void SemiJoin_ReturnsMatchingLeftRows()
    {
        var left = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 }),
            Series.FromValues("b", new[] { "one", "two", "three" })
        );
        var right = new DataFrame(
            Series.FromValues("a", new[] { 1, 2 }),
            Series.FromValues("c", new[] { 100, 200 })
        );

        var result = left.Join(right, new[] { "a" }, new[] { "a" }, JoinType.Semi);

        result.Height.Should().Be(2);
        result.Columns.Should().BeEquivalentTo(new[] { "a", "b" }); // Only left columns
        result["a"].ToArray<int>().Should().BeEquivalentTo(new[] { 1, 2 });
    }

    [Fact]
    public void SemiJoin_NoMatches_ReturnsEmpty()
    {
        var left = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 }),
            Series.FromValues("b", new[] { "one", "two", "three" })
        );
        var right = new DataFrame(
            Series.FromValues("a", new[] { 4, 5, 6 }),
            Series.FromValues("c", new[] { 400, 500, 600 })
        );

        var result = left.Join(right, new[] { "a" }, new[] { "a" }, JoinType.Semi);

        result.Height.Should().Be(0);
    }

    [Fact]
    public void SemiJoin_AllMatch_ReturnsAllLeft()
    {
        var left = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 }),
            Series.FromValues("b", new[] { "one", "two", "three" })
        );
        var right = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3, 4 }),
            Series.FromValues("c", new[] { 100, 200, 300, 400 })
        );

        var result = left.Join(right, new[] { "a" }, new[] { "a" }, JoinType.Semi);

        result.Height.Should().Be(3);
    }

    // ============================================================================
    // Anti Join Tests (ported from Polars test_semi_anti_join)
    // ============================================================================

    [Fact]
    public void AntiJoin_ReturnsNonMatchingLeftRows()
    {
        var left = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 }),
            Series.FromValues("b", new[] { "one", "two", "three" })
        );
        var right = new DataFrame(
            Series.FromValues("a", new[] { 1, 2 }),
            Series.FromValues("c", new[] { 100, 200 })
        );

        var result = left.Join(right, new[] { "a" }, new[] { "a" }, JoinType.Anti);

        result.Height.Should().Be(1);
        result.Columns.Should().BeEquivalentTo(new[] { "a", "b" }); // Only left columns
        result["a"][0].AsInt32().Should().Be(3);
    }

    [Fact]
    public void AntiJoin_NoMatches_ReturnsAllLeft()
    {
        var left = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 }),
            Series.FromValues("b", new[] { "one", "two", "three" })
        );
        var right = new DataFrame(
            Series.FromValues("a", new[] { 4, 5, 6 }),
            Series.FromValues("c", new[] { 400, 500, 600 })
        );

        var result = left.Join(right, new[] { "a" }, new[] { "a" }, JoinType.Anti);

        result.Height.Should().Be(3);
    }

    [Fact]
    public void AntiJoin_AllMatch_ReturnsEmpty()
    {
        var left = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 }),
            Series.FromValues("b", new[] { "one", "two", "three" })
        );
        var right = new DataFrame(
            Series.FromValues("a", new[] { 1, 2, 3 }),
            Series.FromValues("c", new[] { 100, 200, 300 })
        );

        var result = left.Join(right, new[] { "a" }, new[] { "a" }, JoinType.Anti);

        result.Height.Should().Be(0);
    }

    // ============================================================================
    // Cross Join Tests (ported from Polars test_cross_join)
    // ============================================================================

    [Fact]
    public void CrossJoin_ReturnsCartesianProduct()
    {
        var left = new DataFrame(
            Series.FromValues("a", new[] { 1, 2 })
        );
        var right = new DataFrame(
            Series.FromValues("b", new[] { "x", "y", "z" })
        );

        var result = left.Join(right, Array.Empty<string>(), Array.Empty<string>(), JoinType.Cross);

        result.Height.Should().Be(6); // 2 * 3 = 6
        result.Columns.Should().Contain("a");
        result.Columns.Should().Contain("b");
    }

    [Fact]
    public void CrossJoin_EmptyLeft_ReturnsEmpty()
    {
        var left = new DataFrame(
            Series.FromValues("a", Array.Empty<int>())
        );
        var right = new DataFrame(
            Series.FromValues("b", new[] { "x", "y", "z" })
        );

        var result = left.Join(right, Array.Empty<string>(), Array.Empty<string>(), JoinType.Cross);

        result.Height.Should().Be(0);
    }

    [Fact]
    public void CrossJoin_EmptyRight_ReturnsEmpty()
    {
        var left = new DataFrame(
            Series.FromValues("a", new[] { 1, 2 })
        );
        var right = new DataFrame(
            Series.FromValues("b", Array.Empty<string>())
        );

        var result = left.Join(right, Array.Empty<string>(), Array.Empty<string>(), JoinType.Cross);

        result.Height.Should().Be(0);
    }

    [Fact]
    public void CrossJoin_SingleRowEach_ReturnsSingleRow()
    {
        var left = new DataFrame(
            Series.FromValues("a", new[] { 1 })
        );
        var right = new DataFrame(
            Series.FromValues("b", new[] { "x" })
        );

        var result = left.Join(right, Array.Empty<string>(), Array.Empty<string>(), JoinType.Cross);

        result.Height.Should().Be(1);
        result["a"][0].AsInt32().Should().Be(1);
        result["b"][0].AsString().Should().Be("x");
    }

    // ============================================================================
    // Negative Integer Join Tests (ported from Polars test_join_negative_integers)
    // ============================================================================

    [Fact]
    public void Join_NegativeIntegers_WorksCorrectly()
    {
        var left = new DataFrame(
            Series.FromValues("a", new[] { -2, -1, 0, 1, 2 }),
            Series.FromValues("b", new[] { "neg2", "neg1", "zero", "one", "two" })
        );
        var right = new DataFrame(
            Series.FromValues("a", new[] { -1, 1 }),
            Series.FromValues("c", new[] { 100, 200 })
        );

        var result = left.Join(right, "a");

        result.Height.Should().Be(2);
        result["b"].ToArray<string>().Should().BeEquivalentTo(new[] { "neg1", "one" });
    }

    [Fact]
    public void Join_Int64NegativeIntegers_WorksCorrectly()
    {
        var left = new DataFrame(
            Series.FromValues("a", new long[] { -9_000_000_000L, -1, 0, 1, 9_000_000_000L }),
            Series.FromValues("b", new[] { "min", "neg1", "zero", "one", "max" })
        );
        var right = new DataFrame(
            Series.FromValues("a", new long[] { -9_000_000_000L, 9_000_000_000L }),
            Series.FromValues("c", new[] { "left", "right" })
        );

        var result = left.Join(right, "a");

        result.Height.Should().Be(2);
    }

    // ============================================================================
    // Join with String Keys Tests
    // ============================================================================

    [Fact]
    public void Join_StringKeys_WorksCorrectly()
    {
        var left = new DataFrame(
            Series.FromValues("key", new[] { "apple", "banana", "cherry" }),
            Series.FromValues("value", new[] { 1, 2, 3 })
        );
        var right = new DataFrame(
            Series.FromValues("key", new[] { "banana", "cherry", "date" }),
            Series.FromValues("score", new[] { 90, 80, 70 })
        );

        var result = left.Join(right, "key");

        result.Height.Should().Be(2);
        result["key"].ToArray<string>().Should().BeEquivalentTo(new[] { "banana", "cherry" });
    }

    [Fact]
    public void Join_CaseSensitiveStringKeys_RespectsCase()
    {
        var left = new DataFrame(
            Series.FromValues("key", new[] { "Apple", "BANANA", "cherry" }),
            Series.FromValues("value", new[] { 1, 2, 3 })
        );
        var right = new DataFrame(
            Series.FromValues("key", new[] { "apple", "banana", "cherry" }),
            Series.FromValues("score", new[] { 90, 80, 70 })
        );

        var result = left.Join(right, "key");

        result.Height.Should().Be(1); // Only "cherry" matches
        result["key"][0].AsString().Should().Be("cherry");
    }

    // ============================================================================
    // Join with Float Keys Tests
    // ============================================================================

    [Fact]
    public void Join_Float64Keys_WorksCorrectly()
    {
        var left = new DataFrame(
            Series.FromValues("key", new[] { 1.0, 2.0, 3.0 }),
            Series.FromValues("value", new[] { "a", "b", "c" })
        );
        var right = new DataFrame(
            Series.FromValues("key", new[] { 2.0, 3.0, 4.0 }),
            Series.FromValues("score", new[] { 100, 200, 300 })
        );

        var result = left.Join(right, "key");

        result.Height.Should().Be(2);
    }

    // ============================================================================
    // Multiple Key Join Tests (Additional)
    // ============================================================================

    [Fact]
    public void Join_ThreeKeys_WorksCorrectly()
    {
        var left = new DataFrame(
            Series.FromValues("k1", new[] { 1, 1, 2, 2 }),
            Series.FromValues("k2", new[] { "a", "b", "a", "b" }),
            Series.FromValues("k3", new[] { true, true, false, false }),
            Series.FromValues("value", new[] { 10, 20, 30, 40 })
        );
        var right = new DataFrame(
            Series.FromValues("k1", new[] { 1, 2 }),
            Series.FromValues("k2", new[] { "a", "b" }),
            Series.FromValues("k3", new[] { true, false }),
            Series.FromValues("score", new[] { 100, 200 })
        );

        var result = left.Join(right, new[] { "k1", "k2", "k3" }, new[] { "k1", "k2", "k3" });

        result.Height.Should().Be(2);
    }

    // ============================================================================
    // Self Join Tests
    // ============================================================================

    [Fact]
    public void SelfJoin_WorksCorrectly()
    {
        var df = new DataFrame(
            Series.FromValues("id", new[] { 1, 2, 3 }),
            Series.FromValues("parent_id", new[] { 0, 1, 1 }),
            Series.FromValues("name", new[] { "root", "child1", "child2" })
        );

        // Join to find parent names
        var result = df.Join(
            df.Select("id", "name").Rename(new Dictionary<string, string> { { "name", "parent_name" } }),
            new[] { "parent_id" },
            new[] { "id" }
        );

        // Should match parent_id=1 to id=1
        result.Height.Should().BeGreaterOrEqualTo(2);
    }

    // ============================================================================
    // Large Data Join Tests
    // ============================================================================

    [Fact]
    public void Join_LargeDataFrame_WorksCorrectly()
    {
        var size = 10000;
        var left = new DataFrame(
            Series.FromValues("id", Enumerable.Range(0, size).ToArray()),
            Series.FromValues("value", Enumerable.Range(0, size).Select(x => (double)x).ToArray())
        );
        var right = new DataFrame(
            Series.FromValues("id", Enumerable.Range(size / 2, size / 2).ToArray()), // Half overlap
            Series.FromValues("score", Enumerable.Range(0, size / 2).Select(x => x * 10).ToArray())
        );

        var result = left.Join(right, "id");

        result.Height.Should().Be(size / 2);
    }

    // ============================================================================
    // Join Order Preservation Tests
    // ============================================================================

    [Fact]
    public void InnerJoin_PreservesLeftOrder()
    {
        var left = new DataFrame(
            Series.FromValues("id", new[] { 3, 1, 2 }),
            Series.FromValues("name", new[] { "three", "one", "two" })
        );
        var right = new DataFrame(
            Series.FromValues("id", new[] { 1, 2, 3 }),
            Series.FromValues("value", new[] { 100, 200, 300 })
        );

        var result = left.Join(right, "id");

        // Result should maintain left table order: 3, 1, 2
        result["id"][0].AsInt32().Should().Be(3);
        result["id"][1].AsInt32().Should().Be(1);
        result["id"][2].AsInt32().Should().Be(2);
    }
}
