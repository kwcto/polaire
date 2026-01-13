using Xunit;
// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using FluentAssertions;
using Polaire.DataTypes;


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

    // ============================================================================
    // New String Operations Tests (reverse, find, encode/decode, repeat, slice)
    // ============================================================================

    [Fact]
    public void Str_Reverse_BasicStrings_ReversesCorrectly()
    {
        var series = Series.FromValues("s", new[] { "hello", "world", "a" });
        var result = series.Str.Reverse();

        result[0].AsString().Should().Be("olleh");
        result[1].AsString().Should().Be("dlrow");
        result[2].AsString().Should().Be("a");
    }

    [Fact]
    public void Str_Reverse_EmptyString_ReturnsEmpty()
    {
        var series = Series.FromValues("s", new[] { "", "abc" });
        var result = series.Str.Reverse();

        result[0].AsString().Should().Be("");
        result[1].AsString().Should().Be("cba");
    }

    [Fact]
    public void Str_Reverse_WithNulls_PreservesNulls()
    {
        var series = Series.FromValues("s", new string?[] { "hello", null, "world" });
        var result = series.Str.Reverse();

        result[0].AsString().Should().Be("olleh");
        result[1].IsNull.Should().BeTrue();
        result[2].AsString().Should().Be("dlrow");
    }

    [Fact]
    public void Str_Find_PatternExists_ReturnsIndex()
    {
        var series = Series.FromValues("s", new[] { "hello world", "test string" });
        var result = series.Str.Find("world");

        result[0].AsInt32().Should().Be(6);
        result[1].AsInt32().Should().Be(-1);  // Not found
    }

    [Fact]
    public void Str_Find_WithStartIndex_SearchesFromStart()
    {
        var series = Series.FromValues("s", new[] { "hello hello" });

        var fromStart = series.Str.Find("hello", start: 0);
        var fromMiddle = series.Str.Find("hello", start: 3);

        fromStart[0].AsInt32().Should().Be(0);
        fromMiddle[0].AsInt32().Should().Be(6);
    }

    [Fact]
    public void Str_FindLast_MultipleOccurrences_ReturnsLast()
    {
        var series = Series.FromValues("s", new[] { "hello hello hello" });
        var result = series.Str.FindLast("hello");

        result[0].AsInt32().Should().Be(12);
    }

    [Fact]
    public void Str_FindLast_NotFound_ReturnsMinusOne()
    {
        var series = Series.FromValues("s", new[] { "hello world" });
        var result = series.Str.FindLast("xyz");

        result[0].AsInt32().Should().Be(-1);
    }

    [Fact]
    public void Str_EncodeBase64_BasicStrings_EncodesCorrectly()
    {
        var series = Series.FromValues("s", new[] { "hello", "world" });
        var result = series.Str.EncodeBase64();

        result[0].AsString().Should().Be("aGVsbG8=");
        result[1].AsString().Should().Be("d29ybGQ=");
    }

    [Fact]
    public void Str_DecodeBase64_ValidBase64_DecodesCorrectly()
    {
        var series = Series.FromValues("s", new[] { "aGVsbG8=", "d29ybGQ=" });
        var result = series.Str.DecodeBase64();

        result[0].AsString().Should().Be("hello");
        result[1].AsString().Should().Be("world");
    }

    [Fact]
    public void Str_EncodeDecodeBase64_RoundTrip_Succeeds()
    {
        var series = Series.FromValues("s", new[] { "hello world", "test 123", "special !@#" });
        var encoded = series.Str.EncodeBase64();
        var decoded = encoded.Str.DecodeBase64();

        for (int i = 0; i < 3; i++)
        {
            decoded[i].AsString().Should().Be(series[i].AsString());
        }
    }

    [Fact]
    public void Str_DecodeBase64_InvalidBase64_ReturnsNull()
    {
        var series = Series.FromValues("s", new[] { "not-valid-base64!", "aGVsbG8=" });
        var result = series.Str.DecodeBase64();

        result[0].IsNull.Should().BeTrue();
        result[1].AsString().Should().Be("hello");
    }

    [Fact]
    public void Str_EncodeHex_BasicStrings_EncodesCorrectly()
    {
        var series = Series.FromValues("s", new[] { "hello" });
        var result = series.Str.EncodeHex();

        result[0].AsString().Should().Be("68656c6c6f");  // hex for "hello"
    }

    [Fact]
    public void Str_DecodeHex_ValidHex_DecodesCorrectly()
    {
        var series = Series.FromValues("s", new[] { "68656c6c6f" });
        var result = series.Str.DecodeHex();

        result[0].AsString().Should().Be("hello");
    }

    [Fact]
    public void Str_EncodeDecodeHex_RoundTrip_Succeeds()
    {
        var series = Series.FromValues("s", new[] { "hello", "world", "test" });
        var encoded = series.Str.EncodeHex();
        var decoded = encoded.Str.DecodeHex();

        for (int i = 0; i < 3; i++)
        {
            decoded[i].AsString().Should().Be(series[i].AsString());
        }
    }

    [Fact]
    public void Str_DecodeHex_InvalidHex_ReturnsNull()
    {
        var series = Series.FromValues("s", new[] { "not-valid-hex!", "68656c6c6f" });
        var result = series.Str.DecodeHex();

        result[0].IsNull.Should().BeTrue();
        result[1].AsString().Should().Be("hello");
    }

    [Fact]
    public void Str_Repeat_BasicStrings_RepeatsCorrectly()
    {
        var series = Series.FromValues("s", new[] { "ab", "cd" });
        var result = series.Str.Repeat(3);

        result[0].AsString().Should().Be("ababab");
        result[1].AsString().Should().Be("cdcdcd");
    }

    [Fact]
    public void Str_Repeat_ZeroTimes_ReturnsEmpty()
    {
        var series = Series.FromValues("s", new[] { "hello" });
        var result = series.Str.Repeat(0);

        result[0].AsString().Should().Be("");
    }

    [Fact]
    public void Str_Repeat_OneTIme_ReturnsSame()
    {
        var series = Series.FromValues("s", new[] { "hello" });
        var result = series.Str.Repeat(1);

        result[0].AsString().Should().Be("hello");
    }

    [Fact]
    public void Str_Repeat_WithNulls_PreservesNulls()
    {
        var series = Series.FromValues("s", new string?[] { "abc", null });
        var result = series.Str.Repeat(2);

        result[0].AsString().Should().Be("abcabc");
        result[1].IsNull.Should().BeTrue();
    }

    [Fact]
    public void Str_Slice_PositiveIndices_SlicesCorrectly()
    {
        var series = Series.FromValues("s", new[] { "hello world" });

        series.Str.Slice(0, 5)[0].AsString().Should().Be("hello");
        series.Str.Slice(6, 11)[0].AsString().Should().Be("world");
        series.Str.Slice(6)[0].AsString().Should().Be("world");
    }

    [Fact]
    public void Str_Slice_NegativeIndices_CountsFromEnd()
    {
        var series = Series.FromValues("s", new[] { "hello world" });

        series.Str.Slice(-5)[0].AsString().Should().Be("world");
        series.Str.Slice(-5, -2)[0].AsString().Should().Be("wor");
        series.Str.Slice(0, -6)[0].AsString().Should().Be("hello");
    }

    [Fact]
    public void Str_Slice_OutOfBounds_ClampsSafely()
    {
        var series = Series.FromValues("s", new[] { "hello" });

        series.Str.Slice(0, 100)[0].AsString().Should().Be("hello");
        series.Str.Slice(100)[0].AsString().Should().Be("");
        series.Str.Slice(-100)[0].AsString().Should().Be("hello");
    }

    [Fact]
    public void Str_Slice_PythonStyleSlicing_MatchesBehavior()
    {
        // Python: s[1:3] gives characters at index 1 and 2
        var series = Series.FromValues("s", new[] { "hello" });

        series.Str.Slice(1, 3)[0].AsString().Should().Be("el");  // Matches Python s[1:3]
        series.Str.Slice(0, -1)[0].AsString().Should().Be("hell");  // Matches Python s[:-1]
        series.Str.Slice(-3)[0].AsString().Should().Be("llo");  // Matches Python s[-3:]
    }

    // ============================================================================
    // Additional Edge Case Tests
    // ============================================================================

    [Fact]
    public void Str_StripStart_WhitespaceStrings_TrimsStart()
    {
        var series = Series.FromValues("s", new[] { "  hello  ", "\tworld\n" });
        var result = series.Str.StripStart();

        result[0].AsString().Should().Be("hello  ");
        result[1].AsString().Should().Be("world\n");
    }

    [Fact]
    public void Str_StripEnd_WhitespaceStrings_TrimsEnd()
    {
        var series = Series.FromValues("s", new[] { "  hello  ", "\tworld\n" });
        var result = series.Str.StripEnd();

        result[0].AsString().Should().Be("  hello");
        result[1].AsString().Should().Be("\tworld");
    }

    [Fact]
    public void Str_Strip_CustomChars_TrimsCustom()
    {
        var series = Series.FromValues("s", new[] { "xxxhelloxxx", "yyhelloyyy" });
        var result = series.Str.Strip("xy");

        result[0].AsString().Should().Be("hello");
        result[1].AsString().Should().Be("hello");
    }

    [Fact]
    public void Str_Head_BasicStrings_ExtractsHead()
    {
        var series = Series.FromValues("s", new[] { "hello", "world" });
        var result = series.Str.Head(3);

        result[0].AsString().Should().Be("hel");
        result[1].AsString().Should().Be("wor");
    }

    [Fact]
    public void Str_Tail_BasicStrings_ExtractsTail()
    {
        var series = Series.FromValues("s", new[] { "hello", "world" });
        var result = series.Str.Tail(3);

        result[0].AsString().Should().Be("llo");
        result[1].AsString().Should().Be("rld");
    }

    [Fact]
    public void Str_Contains_RegexPattern_FindsMatches()
    {
        var series = Series.FromValues("s", new[] { "hello123", "world456", "test" });
        var result = series.Str.Contains(@"\d+", literal: false);

        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeTrue();
        result[2].AsBoolean().Should().BeFalse();
    }

    [Fact]
    public void Str_Replace_RegexPattern_ReplacesAll()
    {
        var series = Series.FromValues("s", new[] { "hello123world456" });
        var result = series.Str.Replace(@"\d+", "X", literal: false);

        result[0].AsString().Should().Be("helloXworldX");
    }

    [Fact]
    public void Str_ReplaceFirst_OnlyReplacesFirst()
    {
        var series = Series.FromValues("s", new[] { "aaa bbb aaa" });
        var result = series.Str.ReplaceFirst("aaa", "ccc");

        result[0].AsString().Should().Be("ccc bbb aaa");
    }

    [Fact]
    public void Str_CountMatches_RegexPattern_CountsCorrectly()
    {
        var series = Series.FromValues("s", new[] { "abc123def456", "no-numbers" });
        var result = series.Str.CountMatches(@"\d+", literal: false);

        result[0].AsInt32().Should().Be(2);
        result[1].AsInt32().Should().Be(0);
    }

    [Fact]
    public void Str_ByteLengths_UnicodeStrings_ReturnsByteCount()
    {
        var series = Series.FromValues("s", new[] { "hello", "привет", "你好" });
        var result = series.Str.ByteLengths();

        result[0].AsInt32().Should().Be(5);   // ASCII: 1 byte each
        result[1].AsInt32().Should().Be(12);  // Cyrillic: 2 bytes each (6 chars)
        result[2].AsInt32().Should().Be(6);   // Chinese: 3 bytes each (2 chars)
    }

    [Fact]
    public void Str_Concat_MultipleSeries_ConcatenatesStrings()
    {
        var s1 = Series.FromValues("x", new[] { "hello", "good" });
        var s2 = Series.FromValues("y", new[] { " ", "" });
        var s3 = Series.FromValues("z", new[] { "world", "bye" });

        var result = s1.Str.Concat(s2, s3);

        result[0].AsString().Should().Be("hello world");
        result[1].AsString().Should().Be("goodbye");
    }

    [Fact]
    public void Str_Concat_WithNulls_PropagatesNull()
    {
        var s1 = Series.FromValues("x", new string?[] { "hello", null });
        var s2 = Series.FromValues("y", new string?[] { "world", "test" });

        var result = s1.Str.Concat(s2);

        result[0].AsString().Should().Be("helloworld");
        result[1].IsNull.Should().BeTrue();
    }

    [Fact]
    public void Str_Extract_WithGroup_ExtractsGroup()
    {
        var series = Series.FromValues("s", new[] { "hello-123-world" });
        var result = series.Str.Extract(@"(\w+)-(\d+)-(\w+)", groupIndex: 2);

        result[0].AsString().Should().Be("123");
    }

    [Fact]
    public void Str_JsonPathExtract_SimpleProperty_ExtractsValue()
    {
        var series = Series.FromValues("s", new[] { "{\"name\": \"John\", \"age\": 30}" });

        var name = series.Str.JsonPathExtract("$.name");
        var age = series.Str.JsonPathExtract("$.age");

        name[0].AsString().Should().Be("John");
        age[0].AsString().Should().Be("30");
    }

    [Fact]
    public void Str_JsonPathExtract_NestedProperty_ExtractsValue()
    {
        var series = Series.FromValues("s", new[] { "{\"person\": {\"name\": \"John\"}}" });
        var result = series.Str.JsonPathExtract("$.person.name");

        result[0].AsString().Should().Be("John");
    }

    [Fact]
    public void Str_JsonPathExtract_ArrayAccess_ExtractsElement()
    {
        var series = Series.FromValues("s", new[] { "{\"items\": [\"a\", \"b\", \"c\"]}" });
        var result = series.Str.JsonPathExtract("$.items[1]");

        result[0].AsString().Should().Be("b");
    }

    [Fact]
    public void Str_JsonPathExtract_InvalidJson_ReturnsNull()
    {
        var series = Series.FromValues("s", new[] { "not json", "{\"valid\": true}" });
        var result = series.Str.JsonPathExtract("$.valid");

        result[0].IsNull.Should().BeTrue();
        result[1].AsString().Should().Be("true");
    }

    // ============================================================================
    // Consistency Tests
    // ============================================================================

    [Fact]
    public void StringOperations_EmptySeries_ReturnsEmpty()
    {
        var series = Series.FromValues("s", Array.Empty<string>());

        series.Str.ToLowerCase().Length.Should().Be(0);
        series.Str.ToUpperCase().Length.Should().Be(0);
        series.Str.Strip().Length.Should().Be(0);
        series.Str.Reverse().Length.Should().Be(0);
    }

    [Fact]
    public void StringOperations_AllNulls_AllNullResult()
    {
        var series = Series.FromValues("s", new string?[] { null, null, null });

        var result = series.Str.ToLowerCase();

        result.IsNull(0).Should().BeTrue();
        result.IsNull(1).Should().BeTrue();
        result.IsNull(2).Should().BeTrue();
    }

    [Fact]
    public void StringOperations_PreserveSeriesName()
    {
        var series = Series.FromValues("my_column", new[] { "hello" });

        series.Str.ToLowerCase().Name.Should().Be("my_column");
        series.Str.ToUpperCase().Name.Should().Be("my_column");
        series.Str.Strip().Name.Should().Be("my_column");
        series.Str.Reverse().Name.Should().Be("my_column");
    }

    [Fact]
    public void StringOperations_NonStringSeries_ThrowsException()
    {
        var intSeries = Series.FromValues("s", new[] { 1, 2, 3 });

        var act = () => intSeries.Str;

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void StringOperations_Unicode_HandlesCorrectly()
    {
        var series = Series.FromValues("s", new[] { "Привет", "你好" });

        // Case operations on Unicode
        series.Str.ToUpperCase()[0].AsString().Should().Be("ПРИВЕТ");
        series.Str.ToLowerCase()[0].AsString().Should().Be("привет");

        // Length
        series.Str.Lengths()[0].AsInt32().Should().Be(6);  // 6 Cyrillic chars
        series.Str.Lengths()[1].AsInt32().Should().Be(2);  // 2 Chinese chars

        // Reverse
        series.Str.Reverse()[0].AsString().Should().Be("тевирП");
        series.Str.Reverse()[1].AsString().Should().Be("好你");
    }

    [Fact]
    public void Str_Contains_CaseSensitive_ByDefault()
    {
        var series = Series.FromValues("s", new[] { "HELLO", "hello", "Hello" });
        var result = series.Str.Contains("hello");

        result[0].AsBoolean().Should().BeFalse();  // Case sensitive
        result[1].AsBoolean().Should().BeTrue();
        result[2].AsBoolean().Should().BeFalse();
    }

    // ============================================================================
    // Additional String Tests (from Polars test patterns)
    // ============================================================================

    [Fact]
    public void Str_StartsWith_MultiplePatterns()
    {
        var series = Series.FromValues("s", new[] { "apple", "apricot", "banana", "avocado" });

        var result = series.Str.StartsWith("ap");

        result[0].AsBoolean().Should().BeTrue();   // apple
        result[1].AsBoolean().Should().BeTrue();   // apricot
        result[2].AsBoolean().Should().BeFalse();  // banana
        result[3].AsBoolean().Should().BeFalse();  // avocado
    }

    [Fact]
    public void Str_EndsWith_MultiplePatterns()
    {
        var series = Series.FromValues("s", new[] { "hello", "world", "lo", "echo" });

        var result = series.Str.EndsWith("lo");

        result[0].AsBoolean().Should().BeTrue();   // hello
        result[1].AsBoolean().Should().BeFalse();  // world
        result[2].AsBoolean().Should().BeTrue();   // lo
        result[3].AsBoolean().Should().BeFalse();  // echo
    }

    [Fact]
    public void Str_Strip_RemovesWhitespace()
    {
        var series = Series.FromValues("s", new[] { "  hello  ", "\tworld\t", "  test" });

        var result = series.Str.Strip();

        result[0].AsString().Should().Be("hello");
        result[1].AsString().Should().Be("world");
        result[2].AsString().Should().Be("test");
    }

    [Fact]
    public void Str_StripStart_RemovesLeadingWhitespace()
    {
        var series = Series.FromValues("s", new[] { "  hello  ", "\tworld" });

        var result = series.Str.StripStart();

        result[0].AsString().Should().Be("hello  ");
        result[1].AsString().Should().Be("world");
    }

    [Fact]
    public void Str_StripEnd_RemovesTrailingWhitespace()
    {
        var series = Series.FromValues("s", new[] { "  hello  ", "world\t" });

        var result = series.Str.StripEnd();

        result[0].AsString().Should().Be("  hello");
        result[1].AsString().Should().Be("world");
    }

    [Fact]
    public void Str_Lengths_EmptyStrings()
    {
        var series = Series.FromValues("s", new[] { "", "a", "ab", "" });

        var result = series.Str.Lengths();

        result[0].AsInt32().Should().Be(0);
        result[1].AsInt32().Should().Be(1);
        result[2].AsInt32().Should().Be(2);
        result[3].AsInt32().Should().Be(0);
    }

    [Fact]
    public void Str_ReplaceFirst_FirstOccurrence()
    {
        var series = Series.FromValues("s", new[] { "aaa", "aba", "bbb" });

        // ReplaceFirst uses regex to replace only the first occurrence
        var result = series.Str.ReplaceFirst("a", "x");

        // ReplaceFirst replaces first occurrence only
        result[0].AsString().Should().Be("xaa");
        result[1].AsString().Should().Be("xba");
        result[2].AsString().Should().Be("bbb");
    }

    [Fact]
    public void Str_Replace_AllOccurrences()
    {
        var series = Series.FromValues("s", new[] { "aaa", "aba", "bbb" });

        // Replace with literal=true (default) replaces all occurrences
        var result = series.Str.Replace("a", "x");

        result[0].AsString().Should().Be("xxx");
        result[1].AsString().Should().Be("xbx");
        result[2].AsString().Should().Be("bbb");
    }

    [Fact]
    public void Str_Slice_ExtractsSubstring()
    {
        var series = Series.FromValues("s", new[] { "hello", "world", "test" });

        var result = series.Str.Slice(0, 3);

        result[0].AsString().Should().Be("hel");
        result[1].AsString().Should().Be("wor");
        result[2].AsString().Should().Be("tes");
    }

    [Fact]
    public void Str_Slice_NegativeOffset()
    {
        var series = Series.FromValues("s", new[] { "hello", "world" });

        // Negative offset counts from end: Slice(-3) starts 3 from end
        // Slice(-3, null) means from -3 to end
        var result = series.Str.Slice(-3);

        result[0].AsString().Should().Be("llo");
        result[1].AsString().Should().Be("rld");
    }

    [Fact]
    public void Str_PadStart_PadsCorrectly()
    {
        var series = Series.FromValues("s", new[] { "1", "12", "123", "1234" });

        var result = series.Str.PadStart(4, '0');

        result[0].AsString().Should().Be("0001");
        result[1].AsString().Should().Be("0012");
        result[2].AsString().Should().Be("0123");
        result[3].AsString().Should().Be("1234");  // No padding needed
    }

    [Fact]
    public void Str_PadEnd_PadsCorrectly()
    {
        var series = Series.FromValues("s", new[] { "a", "ab", "abc", "abcd" });

        var result = series.Str.PadEnd(4, 'x');

        result[0].AsString().Should().Be("axxx");
        result[1].AsString().Should().Be("abxx");
        result[2].AsString().Should().Be("abcx");
        result[3].AsString().Should().Be("abcd");  // No padding needed
    }

    [Fact]
    public void Str_Concat_JoinsStrings()
    {
        var s1 = Series.FromValues("a", new[] { "hello", "foo" });
        var s2 = Series.FromValues("b", new[] { " world", " bar" });

        var result = s1.Str.Concat(s2);

        result[0].AsString().Should().Be("hello world");
        result[1].AsString().Should().Be("foo bar");
    }

    [Fact]
    public void Str_ToUpperCase_AllCases()
    {
        var series = Series.FromValues("s", new[] { "hello", "WORLD", "MiXeD", "123" });

        var result = series.Str.ToUpperCase();

        result[0].AsString().Should().Be("HELLO");
        result[1].AsString().Should().Be("WORLD");
        result[2].AsString().Should().Be("MIXED");
        result[3].AsString().Should().Be("123");  // Numbers unchanged
    }

    [Fact]
    public void Str_ToLowerCase_AllCases()
    {
        var series = Series.FromValues("s", new[] { "HELLO", "world", "MiXeD", "123" });

        var result = series.Str.ToLowerCase();

        result[0].AsString().Should().Be("hello");
        result[1].AsString().Should().Be("world");
        result[2].AsString().Should().Be("mixed");
        result[3].AsString().Should().Be("123");  // Numbers unchanged
    }

    [Fact]
    public void Str_CountOccurrences_MultipleMatches()
    {
        var series = Series.FromValues("s", new[] { "aaa", "aba", "bbb", "" });

        var result = series.Str.CountMatches("a");

        result[0].AsInt32().Should().Be(3);
        result[1].AsInt32().Should().Be(2);
        result[2].AsInt32().Should().Be(0);
        result[3].AsInt32().Should().Be(0);
    }

    [Fact]
    public void Str_IsEmpty_ChecksEmptyStrings()
    {
        var series = Series.FromValues("s", new[] { "", "hello", "  ", "" });

        // IsEmpty checks if string is empty (not whitespace only)
        // This behavior may depend on implementation
        series.Str.Lengths()[0].AsInt32().Should().Be(0);
        series.Str.Lengths()[2].AsInt32().Should().Be(2);  // "  " has length 2
    }

    [Fact]
    public void Str_Split_ByDelimiter()
    {
        var series = Series.FromValues("s", new[] { "a,b,c", "x,y", "z" });

        var result = series.Str.Split(",");

        // Split returns list/array
        result.Length.Should().Be(3);
    }

    [Fact]
    public void Str_Contains_EmptyPattern()
    {
        var series = Series.FromValues("s", new[] { "hello", "world", "" });

        var result = series.Str.Contains("");

        // Empty string is contained in any string
        result[0].AsBoolean().Should().BeTrue();
        result[1].AsBoolean().Should().BeTrue();
        result[2].AsBoolean().Should().BeTrue();
    }

    [Fact]
    public void Str_Reverse_Palindrome()
    {
        var series = Series.FromValues("s", new[] { "radar", "hello", "level" });

        var result = series.Str.Reverse();

        result[0].AsString().Should().Be("radar");  // Palindrome
        result[1].AsString().Should().Be("olleh");
        result[2].AsString().Should().Be("level");  // Palindrome
    }

    [Fact]
    public void Str_Operations_LargeStrings()
    {
        var longString = new string('a', 10000);
        var series = Series.FromValues("s", new[] { longString });

        series.Str.Lengths()[0].AsInt32().Should().Be(10000);
        series.Str.ToUpperCase()[0].AsString().Should().Be(new string('A', 10000));
    }

    [Fact]
    public void Str_Operations_SpecialCharacters()
    {
        var series = Series.FromValues("s", new[] { "hello\nworld", "foo\tbar", "a\r\nb" });

        // Length includes special characters
        series.Str.Lengths()[0].AsInt32().Should().Be(11);  // "hello\nworld"
        series.Str.Lengths()[1].AsInt32().Should().Be(7);   // "foo\tbar"

        // Contains works with special characters
        series.Str.Contains("\n")[0].AsBoolean().Should().BeTrue();
        series.Str.Contains("\t")[1].AsBoolean().Should().BeTrue();
    }

    [Fact]
    public void Str_ZFill_PadsWithZeros()
    {
        var series = Series.FromValues("s", new[] { "1", "12", "123" });

        // ZFill is specifically for zero padding (handles sign prefix)
        var result = series.Str.ZFill(5);

        result[0].AsString().Should().Be("00001");
        result[1].AsString().Should().Be("00012");
        result[2].AsString().Should().Be("00123");
    }

    [Fact]
    public void Str_ZFill_HandlesNegativeNumbers()
    {
        var series = Series.FromValues("s", new[] { "-1", "+2", "3" });

        var result = series.Str.ZFill(5);

        // ZFill preserves sign at start, pads zeros after
        result[0].AsString().Should().Be("-0001");
        result[1].AsString().Should().Be("+0002");
        result[2].AsString().Should().Be("00003");
    }
}
