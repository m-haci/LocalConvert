namespace LocalConvert.Core.Files;

public sealed class OutputPathResolution
{
    public required string FullPath { get; init; }

    public bool RequiresUserConfirmation { get; init; }

    public bool AlreadyExists { get; init; }
}
