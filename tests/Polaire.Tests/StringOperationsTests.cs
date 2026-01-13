// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using FluentAssertions;
using Polaire.DataTypes;
using Polaire.Series;

namespace Polaire.Tests;

public class StringOperationsTests
{
    [Fact]
    public void Str_ToLowerCase_ShouldWork()
    {
        var series = Series.FromValues("s", new[] { "HELLO", "World", "TEST" });
        var result = series.Str.ToLowerCase();

        result[0].AsString().Should().Be("hello");
        result[1].AsString().Should().Be("world");
        result[2].AsString().Should().Be("test");
    }

    [Fact]
    public void Str_ToUpperCase_ShouldWork()
    {
        var series = Series.FromValues("s", new[] { "hello", "World", "test" });
        var result = series.Str.ToUpperCase();

        result[0].AsString().Should().Be("HELLO");
        result[1].AsString().Should().Be("WORLD");
        result[2].AsString().Should().Be("TEST");
    }

    [Fact]
    public void Str_Strip_ShouldTrimWhitespace()
    {
        var series = Series.FromValues("s", new[] { "  hello  ", " world", "test " });
        var result = series.Str.Strip();

        result[0].AsString().Should().Be("hello");
        result[1].AsString().Should().Be("world");
        result[2].AsString().Should().Be("test");
    }

    [Fact]
    public void Str_Contains_ShouldCheckSubstring()
    {
        var series = Series.FromValues("s", new[] { "hello world", "goodbye", "hello there" });
        var result = series.Str.Contains("hello");

        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeFalse();
        result[2].AsBoolean().Should().BeTrue();
    }

    [Fact]
    public void Str_StartsWith_ShouldCheckPrefix()
    {
        var series = Series.FromValues("s", new[] { "hello world", "goodbye", "hello there" });
        var result = series.Str.StartsWith("hello");

        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeFalse();
        result[2].AsBoolean().Should().BeTrue();
    }

    [Fact]
    public void Str_EndsWith_ShouldCheckSuffix()
    {
        var series = Series.FromValues("s", new[] { "hello world", "goodbye world", "hello" });
        var result = series.Str.EndsWith("world");

        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeTrue();
        result[2].AsBoolean().Should().BeFalse();
    }

    [Fact]
    public void Str_Replace_ShouldReplacePattern()
    {
        var series = Series.FromValues("s", new[] { "hello world", "hello there", "hi world" });
        var result = series.Str.Replace("hello", "hi");

        result[0].AsString().Should().Be("hi world");
        result[1].AsString().Should().Be("hi there");
        result[2].AsString().Should().Be("hi world");
    }

    [Fact]
    public void Str_Lengths_ShouldReturnStringLengths()
    {
        var series = Series.FromValues("s", new[] { "a", "ab", "abc", "abcd" });
        var result = series.Str.Lengths();

        result[0].AsInt32().Should().Be(1);
        result[1].AsInt32().Should().Be(2);
        result[2].AsInt32().Should().Be(3);
        result[3].AsInt32().Should().Be(4);
    }

    [Fact]
    public void Str_Substring_ShouldExtractPortion()
    {
        var series = Series.FromValues("s", new[] { "hello world", "testing", "abcdef" });
        var result = series.Str.Substring(0, 5);

        result[0].AsString().Should().Be("hello");
        result[1].AsString().Should().Be("testi");
        result[2].AsString().Should().Be("abcde");
    }

    [Fact]
    public void Str_PadStart_ShouldPadLeft()
    {
        var series = Series.FromValues("s", new[] { "1", "12", "123" });
        var result = series.Str.PadStart(5, '0');

        result[0].AsString().Should().Be("00001");
        result[1].AsString().Should().Be("00012");
        result[2].AsString().Should().Be("00123");
    }

    [Fact]
    public void Str_PadEnd_ShouldPadRight()
    {
        var series = Series.FromValues("s", new[] { "1", "12", "123" });
        var result = series.Str.PadEnd(5, '0');

        result[0].AsString().Should().Be("10000");
        result[1].AsString().Should().Be("12000");
        result[2].AsString().Should().Be("12300");
    }

    [Fact]
    public void Str_ZFill_ShouldZeroPad()
    {
        var series = Series.FromValues("s", new[] { "5", "50", "-5", "500" });
        var result = series.Str.ZFill(4);

        result[0].AsString().Should().Be("0005");
        result[1].AsString().Should().Be("0050");
        result[2].AsString().Should().Be("-005");
        result[3].AsString().Should().Be("0500");
    }

    [Fact]
    public void Str_Extract_ShouldExtractRegexMatch()
    {
        var series = Series.FromValues("s", new[] { "user123", "admin456", "guest" });
        var result = series.Str.Extract(@"\d+");

        result[0].AsString().Should().Be("123");
        result[1].AsString().Should().Be("456");
        result[2].IsNull.Should().BeTrue();
    }

    [Fact]
    public void Str_Matches_ShouldCheckRegex()
    {
        var series = Series.FromValues("s", new[] { "abc123", "xyz", "test456" });
        var result = series.Str.Matches(@"\d+");

        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeFalse();
        result[2].AsBoolean().Should().BeTrue();
    }

    [Fact]
    public void Str_CountMatches_ShouldCountOccurrences()
    {
        var series = Series.FromValues("s", new[] { "ababa", "aaa", "xyz" });
        var result = series.Str.CountMatches("a");

        result[0].AsInt32().Should().Be(3);
        result[1].AsInt32().Should().Be(3);
        result[2].AsInt32().Should().Be(0);
    }

    [Fact]
    public void Str_WithNulls_ShouldHandleGracefully()
    {
        var series = Series.FromValues("s", new[] { "hello", null, "world" });
        var result = series.Str.ToUpperCase();

        result[0].AsString().Should().Be("HELLO");
        result[1].IsNull.Should().BeTrue();
        result[2].AsString().Should().Be("WORLD");
    }

    [Fact]
    public void Str_ToTitleCase_ShouldWork()
    {
        var series = Series.FromValues("s", new[] { "hello world", "HELLO WORLD", "hELLO wORLD" });
        var result = series.Str.ToTitleCase();

        result[0].AsString().Should().Be("Hello World");
        result[1].AsString().Should().Be("Hello World");
        result[2].AsString().Should().Be("Hello World");
    }
}
