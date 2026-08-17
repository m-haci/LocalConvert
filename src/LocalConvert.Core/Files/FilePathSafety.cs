namespace LocalConvert.Core.Files;

public static class FilePathSafety
{
    public static bool TryGetSafeFullPath(string path, out string fullPath)
    {
        fullPath = string.Empty;

        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
        {
            return false;
        }

        try
        {
            fullPath = Path.GetFullPath(path);
        }
        catch (Exception)
        {
            return false;
        }

        if (!Path.IsPathRooted(fullPath))
        {
            return false;
        }

        var fileName = Path.GetFileName(fullPath);
        if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return false;
        }

        return true;
    }

    public static bool IsUnderRoot(string candidatePath, string rootPath)
    {
        if (!TryGetSafeFullPath(candidatePath, out var fullCandidate) ||
            !TryGetSafeFullPath(rootPath, out var fullRoot))
        {
            return false;
        }

        var normalizedRoot = fullRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                             + Path.DirectorySeparatorChar;
        return fullCandidate.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase)
               || string.Equals(fullCandidate, fullRoot, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsReparsePoint(string path)
    {
        try
        {
            var attributes = File.GetAttributes(path);
            return attributes.HasFlag(FileAttributes.ReparsePoint);
        }
        catch (Exception)
        {
            return false;
        }
    }
}
