using LocalConvert.Core.Conversion;
using LocalConvert.Core.Logging;
using LocalConvert.Core.Parsing;
using Microsoft.Extensions.Logging;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace LocalConvert.Pdf;

public sealed class PdfSplitConverter : IFileConverter
{
    private readonly ILogger<PdfSplitConverter> _logger;

    public PdfSplitConverter(ILogger<PdfSplitConverter> logger)
    {
        _logger = logger;
    }

    public string Id => ConverterIds.PdfSplit;

    public string DisplayNameKey => "Tool_PdfSplit";

    public string DescriptionKey => "Tool_PdfSplit_Desc";

    public string Glyph => "\uEA37";

    public ConverterCategory Category => ConverterCategory.Pdf;

    public IReadOnlyList<string> InputExtensions => FileExtensions.Pdf;

    public IReadOnlyList<string> OutputExtensions { get; } = [".pdf"];

    public bool SupportsMultipleInputs => false;

    public bool CanConvert(ConversionInput input)
    {
        return input.FilePaths.Count == 1 && ConverterHelpers.AllExtensionsAre(input, FileExtensions.Pdf);
    }

    public async Task<ConversionResult> ConvertAsync(
        ConversionRequest request,
        IProgress<ConversionProgress>? progress,
        CancellationToken cancellationToken)
    {
        var started = DateTimeOffset.Now;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (request.InputPaths.Count != 1)
            {
                return ConversionResult.Failed(ConversionErrorCode.InvalidInput);
            }

            var inputPath = request.InputPaths[0];
            if (!File.Exists(inputPath))
            {
                return ConversionResult.Failed(ConversionErrorCode.FileNotFound);
            }

            Directory.CreateDirectory(request.OutputDirectory);

            var outputPaths = await Task.Run(() =>
            {
                using var inputDocument = PdfReader.Open(inputPath, PdfDocumentOpenMode.Import);
                var selectedPages = ResolvePages(request, inputDocument.PageCount);
                if (selectedPages is null)
                {
                    return (IReadOnlyList<string>?)null;
                }

                var created = new List<string>();
                var baseName = Path.GetFileNameWithoutExtension(request.OutputFileName);
                var total = selectedPages.Count;

                for (var index = 0; index < selectedPages.Count; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var pageNumber = selectedPages[index];
                    progress?.Report(ConversionProgress.FromPages(index, total, "pdfSplit"));

                    using var outputDocument = new PdfDocument();
                    outputDocument.AddPage(inputDocument.Pages[pageNumber - 1]);

                    var fileName = total == 1
                        ? request.OutputFileName
                        : $"{baseName}-p{pageNumber}.pdf";
                    var outputPath = Path.Combine(request.OutputDirectory, fileName);
                    outputDocument.Save(outputPath);
                    created.Add(outputPath);
                }

                progress?.Report(ConversionProgress.FromPages(total, total, "pdfSplit"));
                return (IReadOnlyList<string>?)created;
            }, cancellationToken).ConfigureAwait(false);

            if (outputPaths is null)
            {
                return ConversionResult.Failed(ConversionErrorCode.InvalidPageRange);
            }

            _logger.LogInformation(
                "Split {FileName} into {Count} PDF file(s).",
                LogSanitizer.FileNameOnly(inputPath),
                outputPaths.Count);

            return ConversionResult.Succeeded(outputPaths, DateTimeOffset.Now - started);
        }
        catch (OperationCanceledException)
        {
            return ConversionResult.Cancelled();
        }
        catch (PdfReaderException exception)
        {
            _logger.LogError(exception, "PDF split failed while reading {FileName}.",
                LogSanitizer.FileNameOnly(request.InputPaths.FirstOrDefault()));
            var code = exception.Message.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                       exception.Message.Contains("encrypt", StringComparison.OrdinalIgnoreCase)
                ? ConversionErrorCode.FileEncrypted
                : ConversionErrorCode.FileCorrupt;
            return ConversionResult.Failed(code, exception);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "PDF split failed.");
            return ConversionResult.Failed(ConversionErrorCode.FileCorrupt, exception);
        }
    }

    private static IReadOnlyList<int>? ResolvePages(ConversionRequest request, int totalPages)
    {
        request.Options.TryGetValue(ConversionOptionKeys.SplitMode, out var mode);
        if (string.Equals(mode, ConversionOptionKeys.SplitModes.Range, StringComparison.OrdinalIgnoreCase))
        {
            request.Options.TryGetValue(ConversionOptionKeys.PageRange, out var range);
            var parsed = PageRangeParser.Parse(range, totalPages);
            return parsed.Success ? parsed.Pages : null;
        }

        return Enumerable.Range(1, totalPages).ToList();
    }
}
