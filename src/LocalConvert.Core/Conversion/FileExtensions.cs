namespace LocalConvert.Core.Conversion;

public static class FileExtensions
{
    public static readonly IReadOnlyList<string> Jpeg = [".jpg", ".jpeg"];
    public static readonly IReadOnlyList<string> Png = [".png"];
    public static readonly IReadOnlyList<string> Images = [".jpg", ".jpeg", ".png"];
    public static readonly IReadOnlyList<string> Pdf = [".pdf"];
    public static readonly IReadOnlyList<string> Word = [".doc", ".docx"];
    public static readonly IReadOnlyList<string> PowerPoint = [".ppt", ".pptx"];
    public static readonly IReadOnlyList<string> Excel = [".xls", ".xlsx"];
    public static readonly IReadOnlyList<string> Office = [".doc", ".docx", ".ppt", ".pptx", ".xls", ".xlsx"];
    public static readonly IReadOnlyList<string> AllSupported =
    [
        ".jpg", ".jpeg", ".png", ".pdf",
        ".doc", ".docx", ".ppt", ".pptx", ".xls", ".xlsx"
    ];

    public static string Normalize(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return string.Empty;
        }

        var trimmed = extension.Trim().ToLowerInvariant();
        return trimmed.StartsWith('.') ? trimmed : "." + trimmed;
    }
}
