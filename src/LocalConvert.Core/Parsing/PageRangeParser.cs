using System.Globalization;

namespace LocalConvert.Core.Parsing;

public static class PageRangeParser
{
    public static PageRangeParseResult Parse(string? input, int? totalPages = null)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return PageRangeParseResult.Fail(PageRangeParseError.Empty);
        }

        var trimmed = input.Trim();
        if (trimmed.Contains(",,", StringComparison.Ordinal) ||
            trimmed.StartsWith(',') ||
            trimmed.EndsWith(','))
        {
            return PageRangeParseResult.Fail(PageRangeParseError.DuplicateComma);
        }

        var pages = new List<int>();
        var tokens = trimmed.Split(',', StringSplitOptions.None);

        foreach (var rawToken in tokens)
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
                    return PageRangeParseResult.Fail(ClassifyNumberError(rangeParts[0], rangeParts[1]));
                }

                if (start < 1 || end < 1)
                {
                    return PageRangeParseResult.Fail(PageRangeParseError.PageNumberOutOfRange);
                }

                if (start > end)
                {
                    return PageRangeParseResult.Fail(PageRangeParseError.InvertedRange);
                }

                if (totalPages is int total && end > total)
                {
                    return PageRangeParseResult.Fail(PageRangeParseError.PageNumberOutOfRange);
                }

                for (var page = start; page <= end; page++)
                {
                    pages.Add(page);
                }
            }
            else
            {
                if (!TryParsePageNumber(token, out var page))
                {
                    return PageRangeParseResult.Fail(
                        LooksLikeNumber(token)
                            ? PageRangeParseError.PageNumberOutOfRange
                            : PageRangeParseError.InvalidToken);
                }

                if (page < 1 || (totalPages is int total && page > total))
                {
                    return PageRangeParseResult.Fail(PageRangeParseError.PageNumberOutOfRange);
                }

                pages.Add(page);
            }
        }

        var uniqueOrdered = pages.Distinct().OrderBy(page => page).ToList();
        return PageRangeParseResult.Ok(uniqueOrdered);
    }

    private static bool TryParsePageNumber(string value, out int page)
    {
        return int.TryParse(value.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out page);
    }

    private static bool LooksLikeNumber(string value)
    {
        return value.Trim().Length > 0 && value.Trim().All(ch => char.IsDigit(ch) || ch is '-' or '+');
    }

    private static PageRangeParseError ClassifyNumberError(string startText, string endText)
    {
        if (LooksLikeNumber(startText) || LooksLikeNumber(endText))
        {
            return PageRangeParseError.PageNumberOutOfRange;
        }

        return PageRangeParseError.InvalidToken;
    }
}
