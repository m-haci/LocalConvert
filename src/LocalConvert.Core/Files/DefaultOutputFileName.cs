using LocalConvert.Core.Conversion;

namespace LocalConvert.Core.Files;

public static class DefaultOutputFileName
{
    public static string For(string converterId, IReadOnlyList<string> inputPaths)
    {
        if (inputPaths.Count == 0)
        {
            return "output.pdf";
        }

        var firstName = Path.GetFileNameWithoutExtension(inputPaths[0]);

        return converterId switch
        {
            ConverterIds.PdfMerge => $"{firstName}-merged.pdf",
            ConverterIds.PdfSplit => $"{firstName}.pdf",
            ConverterIds.PdfExtract => $"{firstName}-extract.pdf",
            ConverterIds.PdfRotate => $"{firstName}-rotated.pdf",
            ConverterIds.PdfReorder => $"{firstName}-reordered.pdf",
            ConverterIds.PdfMetadata => $"{firstName}-clean.pdf",
            ConverterIds.PdfToJpeg => $"{firstName}.jpg",
            ConverterIds.PdfToPng => $"{firstName}.png",
            _ => $"{firstName}.pdf"
        };
    }
}
