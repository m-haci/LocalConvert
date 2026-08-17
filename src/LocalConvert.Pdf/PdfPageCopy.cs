using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace LocalConvert.Pdf;

internal static class PdfPageCopy
{
    public static PdfDocument OpenForImport(string path)
    {
        return PdfReader.Open(path, PdfDocumentOpenMode.Import);
    }

    public static void AddPages(PdfDocument source, PdfDocument destination, IEnumerable<int> oneBasedPageNumbers)
    {
        foreach (var pageNumber in oneBasedPageNumbers)
        {
            destination.AddPage(source.Pages[pageNumber - 1]);
        }
    }
}
