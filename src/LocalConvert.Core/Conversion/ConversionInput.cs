namespace LocalConvert.Core.Conversion;

public sealed class ConversionInput
{
    public required IReadOnlyList<string> FilePaths { get; init; }

    public IReadOnlyList<string> Extensions => FilePaths
        .Select(path => FileExtensions.Normalize(Path.GetExtension(path)))
        .ToList();
}
