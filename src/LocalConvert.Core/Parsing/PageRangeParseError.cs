namespace LocalConvert.Core.Parsing;

public enum PageRangeParseError
{
    Empty = 0,
    InvalidToken = 1,
    PageNumberOutOfRange = 2,
    InvertedRange = 3,
    DuplicateComma = 4
}
