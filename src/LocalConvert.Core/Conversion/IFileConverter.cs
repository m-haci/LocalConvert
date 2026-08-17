namespace LocalConvert.Core.Conversion;

public interface IFileConverter
{
    string Id { get; }

    string DisplayNameKey { get; }

    string DescriptionKey { get; }

    string Glyph { get; }

    ConverterCategory Category { get; }

    IReadOnlyList<string> InputExtensions { get; }

    IReadOnlyList<string> OutputExtensions { get; }

    bool SupportsMultipleInputs { get; }

    bool CanConvert(ConversionInput input);

    Task<ConversionResult> ConvertAsync(
        ConversionRequest request,
        IProgress<ConversionProgress>? progress,
        CancellationToken cancellationToken);
}
