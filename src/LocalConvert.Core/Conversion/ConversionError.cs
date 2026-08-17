namespace LocalConvert.Core.Conversion;

public sealed class ConversionError
{
    public required ConversionErrorCode Code { get; init; }

    public string? DeveloperDetail { get; init; }

    public static ConversionError From(ConversionErrorCode code, Exception? exception = null)
    {
        return new ConversionError
        {
            Code = code,
            DeveloperDetail = exception is null
                ? null
                : $"{exception.GetType().Name}: {exception.Message}"
        };
    }
}
