namespace LocalConvert.Core.Conversion;

public sealed class ConversionProgress
{
    public int Percent { get; init; }

    public string? CurrentOperation { get; init; }

    public int? CurrentPage { get; init; }

    public int? TotalPages { get; init; }

    public static ConversionProgress FromPages(int currentPage, int totalPages, string? currentOperation = null)
    {
        var percent = totalPages <= 0
            ? 0
            : (int)Math.Clamp(Math.Round(currentPage * 100d / totalPages), 0, 100);

        return new ConversionProgress
        {
            Percent = percent,
            CurrentOperation = currentOperation,
            CurrentPage = currentPage,
            TotalPages = totalPages
        };
    }
}
