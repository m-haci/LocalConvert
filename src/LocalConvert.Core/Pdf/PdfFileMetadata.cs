namespace LocalConvert.Core.Pdf;

public sealed class PdfFileMetadata
{
    public string Title { get; init; } = string.Empty;

    public string Author { get; init; } = string.Empty;

    public string Subject { get; init; } = string.Empty;

    public string Creator { get; init; } = string.Empty;

    public string Producer { get; init; } = string.Empty;

    public string Keywords { get; init; } = string.Empty;
}
