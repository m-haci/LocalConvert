namespace LocalConvert.Core.Parsing;

public sealed class PageRangeParseResult
{
    public bool Success { get; init; }

    public IReadOnlyList<int> Pages { get; init; } = [];

    public PageRangeParseError? Error { get; init; }

    public static PageRangeParseResult Ok(IReadOnlyList<int> pages)
    {
        return new PageRangeParseResult { Success = true, Pages = pages };
    }

    public static PageRangeParseResult Fail(PageRangeParseError error)
    {
        return new PageRangeParseResult { Success = false, Error = error };
    }
}
