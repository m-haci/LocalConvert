using LocalConvert.Core.Conversion;
using LocalConvert.Core.Files;

namespace LocalConvert.Core.Conversion;

public static class ConverterHelpers
{
    public static bool AllExtensionsAre(ConversionInput input, IReadOnlyCollection<string> allowed)
    {
        if (input.FilePaths.Count == 0)
        {
            return false;
        }

        return input.Extensions.All(extension => allowed.Contains(extension, StringComparer.OrdinalIgnoreCase));
    }

    public static string BuildOutputPath(ConversionRequest request, ExistingFilePolicy policy)
    {
        var resolution = OutputFileNameGenerator.Resolve(
            request.OutputDirectory,
            request.OutputFileName,
            policy);

        return resolution.FullPath;
    }
}
