namespace LocalConvert.Core.Files;

public static class OutputFileNameGenerator
{
    public static OutputPathResolution Resolve(
        string directory,
        string fileName,
        ExistingFilePolicy policy,
        Func<string, bool>? fileExists = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var exists = fileExists ?? File.Exists;
        var desiredPath = Path.Combine(directory, fileName);

        if (!exists(desiredPath))
        {
            return new OutputPathResolution
            {
                FullPath = desiredPath,
                RequiresUserConfirmation = false,
                AlreadyExists = false
            };
        }

        return policy switch
        {
            ExistingFilePolicy.Overwrite => new OutputPathResolution
            {
                FullPath = desiredPath,
                RequiresUserConfirmation = false,
                AlreadyExists = true
            },
            ExistingFilePolicy.Ask => new OutputPathResolution
            {
                FullPath = desiredPath,
                RequiresUserConfirmation = true,
                AlreadyExists = true
            },
            _ => new OutputPathResolution
            {
                FullPath = CreateUniquePath(directory, fileName, exists),
                RequiresUserConfirmation = false,
                AlreadyExists = true
            }
        };
    }

    public static string CreateUniquePath(string directory, string fileName, Func<string, bool> fileExists)
    {
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        var candidate = Path.Combine(directory, fileName);
        var suffix = 1;

        while (fileExists(candidate))
        {
            candidate = Path.Combine(directory, $"{nameWithoutExtension} ({suffix}){extension}");
            suffix++;
        }

        return candidate;
    }
}
