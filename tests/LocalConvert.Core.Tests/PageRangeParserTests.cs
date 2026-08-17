using FluentAssertions;
using LocalConvert.Core.Parsing;
using Xunit;

namespace LocalConvert.Core.Tests;

public sealed class PageRangeParserTests
{
    [Theory]
    [InlineData("1", new[] { 1 })]
    [InlineData("1-5", new[] { 1, 2, 3, 4, 5 })]
    [InlineData("1,3,7", new[] { 1, 3, 7 })]
    [InlineData("1-3,8-12", new[] { 1, 2, 3, 8, 9, 10, 11, 12 })]
    [InlineData(" 1, 3 , 7 ", new[] { 1, 3, 7 })]
    public void Parse_AcceptsValidRanges(string input, int[] expected)
    {
        var result = PageRangeParser.Parse(input);

        result.Success.Should().BeTrue();
        result.Pages.Should().Equal(expected);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("5-2")]
    [InlineData("abc")]
    [InlineData("1,,4")]
    [InlineData("")]
    [InlineData("1-")]
    [InlineData(",1")]
    public void Parse_RejectsInvalidRanges(string input)
    {
        var result = PageRangeParser.Parse(input);

        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNull();
    }

    [Fact]
    public void Parse_RejectsPagesBeyondTotal()
    {
        var result = PageRangeParser.Parse("1-8", totalPages: 5);

        result.Success.Should().BeFalse();
        result.Error.Should().Be(PageRangeParseError.PageNumberOutOfRange);
    }
}
