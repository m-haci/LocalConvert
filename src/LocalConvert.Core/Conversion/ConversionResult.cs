namespace LocalConvert.Core.Conversion;

public sealed class ConversionResult
{
    public required bool Success { get; init; }

    public IReadOnlyList<string> OutputPaths { get; init; } = [];

    public ConversionError? Error { get; init; }

    public TimeSpan Duration { get; init; }

    public static ConversionResult Succeeded(IReadOnlyList<string> outputPaths, TimeSpan duration)
    {
        return new ConversionResult
        {
            Success = true,
            OutputPaths = outputPaths,
            Duration = duration
        };
    }

    public static ConversionResult Failed(ConversionErrorCode code, Exception? exception = null)
    {
        return new ConversionResult
        {
            Success = false,
            Error = ConversionError.From(code, exception)
        };
    }

    public static ConversionResult Cancelled()
    {
        return Failed(ConversionErrorCode.Cancelled);
    }
}
