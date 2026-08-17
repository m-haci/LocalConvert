using System.Globalization;

namespace LocalConvert.Core.Parsing;

public static class PageOrderParser
{
    public static PageRangeParseResult Parse(string? input, int totalPages)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return PageRangeParseResult.Fail(PageRangeParseError.Empty);
        }

        if (totalPages < 1)
        {
            return PageRangeParseResult.Fail(PageRangeParseError.PageNumberOutOfRange);
        }

        var trimmed = input.Trim();
        if (trimmed.Contains(",,", StringComparison.Ordinal) ||
            trimmed.StartsWith(',') ||
            trimmed.EndsWith(','))
        {
            return PageRangeParseResult.Fail(PageRangeParseError.DuplicateComma);
        }

        var pages = new List<int>();
        foreach (var rawToken in trimmed.Split(',', StringSplitOptions.None))
        {
            var token = rawToken.Trim();
            if (token.Length == 0)
            {
                return PageRangeParseResult.Fail(PageRangeParseError.DuplicateComma);
            }

            if (token.Contains('-', StringComparison.Ordinal))
            {
                var rangeParts = token.Split('-', StringSplitOptions.None);
                if (rangeParts.Length != 2)
                {
                    return PageRangeParseResult.Fail(PageRangeParseError.InvalidToken);
                }

                if (!TryParsePageNumber(rangeParts[0], out var start) ||
                    !TryParsePageNumber(rangeParts[1], out var end))
                {
                    return PageRangeParseResult.Fail(PageRangeParseError.InvalidToken);
                }

                if (start < 1 || end < 1 || start > end || end > totalPages)
                {
                    return start > end
                        ? PageRangeParseResult.Fail(PageRangeParseError.InvertedRange)
                        : PageRangeParseResult.Fail(PageRangeParseError.PageNumberOutOfRange);
                }

                for (var page = start; page <= end; page++)
                {
                    if (pages.Contains(page))
                    {
                        return PageRangeParseResult.Fail(PageRangeParseError.InvalidToken);
                    }

                    pages.Add(page);
                }
            }
            else
            {
                if (!TryParsePageNumber(token, out var page) || page < 1 || page > totalPages)
                {
                    return PageRangeParseResult.Fail(PageRangeParseError.PageNumberOutOfRange);
                }

                if (pages.Contains(page))
                {
                    return PageRangeParseResult.Fail(PageRangeParseError.InvalidToken);
                }

                pages.Add(page);
            }
        }

        return pages.Count == 0
            ? PageRangeParseResult.Fail(PageRangeParseError.Empty)
            : PageRangeParseResult.Ok(pages);
    }

    private static bool TryParsePageNumber(string value, out int page)
    {
        return int.TryParse(value.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out page);
    }
}
