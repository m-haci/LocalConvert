using LocalConvert.Core.Files;

namespace LocalConvert.Core.Jobs;

public sealed class ConversionJobRequest
{
    public required IReadOnlyList<string> InputFiles { get; init; }

    public required string ConverterId { get; init; }

    public required string OutputDirectory { get; init; }

    public required string OutputFileName { get; init; }

    public ExistingFilePolicy ExistingFilePolicy { get; init; } = ExistingFilePolicy.CreateNewName;

    public IReadOnlyDictionary<string, string> Options { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
