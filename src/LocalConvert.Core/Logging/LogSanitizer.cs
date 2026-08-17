namespace LocalConvert.Core.Logging;

public static class LogSanitizer
{
    public static string FileNameOnly(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "(none)";
        }

        try
        {
            return Path.GetFileName(path);
        }
        catch (Exception)
        {
            return "(invalid)";
        }
    }
}
