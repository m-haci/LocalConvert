namespace LocalConvert.Core.Conversion;

public sealed class ConversionRequest
{
    public required Guid JobId { get; init; }

    public required IReadOnlyList<string> InputPaths { get; init; }

    public required string OutputDirectory { get; init; }

    public required string OutputFileName { get; init; }

    public required string ConverterId { get; init; }

    public IReadOnlyDictionary<string, string> Options { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
