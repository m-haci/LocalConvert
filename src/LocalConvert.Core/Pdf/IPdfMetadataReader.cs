namespace LocalConvert.Core.Pdf;

public interface IPdfMetadataReader
{
    bool TryRead(string filePath, out PdfFileMetadata metadata);
}
