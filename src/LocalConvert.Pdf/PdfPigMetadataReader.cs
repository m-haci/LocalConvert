using LocalConvert.Core.Pdf;
using UglyToad.PdfPig;

namespace LocalConvert.Pdf;

public sealed class PdfPigMetadataReader : IPdfMetadataReader
{
    public bool TryRead(string filePath, out PdfFileMetadata metadata)
    {
        metadata = new PdfFileMetadata();
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return false;
        }

        try
        {
            using var document = PdfDocument.Open(filePath);
            var info = document.Information;
            metadata = new PdfFileMetadata
            {
                Title = info.Title ?? string.Empty,
                Author = info.Author ?? string.Empty,
                Subject = info.Subject ?? string.Empty,
                Creator = info.Creator ?? string.Empty,
                Producer = info.Producer ?? string.Empty,
                Keywords = info.Keywords ?? string.Empty
            };
            return true;
        }
        catch (Exception)
        {
            metadata = new PdfFileMetadata();
            return false;
        }
    }
}
