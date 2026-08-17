using FluentAssertions;
using LocalConvert.Core.Parsing;
using Xunit;

namespace LocalConvert.Core.Tests;

public sealed class PageOrderParserTests
{
    [Fact]
    public void Parse_PreservesRequestedOrder()
    {
        var result = PageOrderParser.Parse("3,1,2,4", totalPages: 4);

        result.Success.Should().BeTrue();
        result.Pages.Should().Equal(3, 1, 2, 4);
    }

    [Fact]
    public void Parse_RejectsDuplicates()
    {
        var result = PageOrderParser.Parse("1,1,2", totalPages: 3);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public void Parse_RejectsOutOfRange()
    {
        var result = PageOrderParser.Parse("1,5", totalPages: 3);

        result.Success.Should().BeFalse();
        result.Error.Should().Be(PageRangeParseError.PageNumberOutOfRange);
    }
}
