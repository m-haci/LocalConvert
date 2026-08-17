using LocalConvert.Core.Conversion;
using LocalConvert.Core.Logging;
using LocalConvert.Core.Parsing;
using Microsoft.Extensions.Logging;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace LocalConvert.Pdf;

public sealed class PdfReorderConverter : IFileConverter
{
    private readonly ILogger<PdfReorderConverter> _logger;

    public PdfReorderConverter(ILogger<PdfReorderConverter> logger)
    {
        _logger = logger;
    }

    public string Id => ConverterIds.PdfReorder;

    public string DisplayNameKey => "Tool_PdfReorder";

    public string DescriptionKey => "Tool_PdfReorder_Desc";

    public string Glyph => "\uE8CB";

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
            var outputPath = Path.Combine(request.OutputDirectory, request.OutputFileName);

            var reordered = await Task.Run(() =>
            {
                using var inputDocument = PdfPageCopy.OpenForImport(inputPath);
                request.Options.TryGetValue(ConversionOptionKeys.PageOrder, out var orderText);
                var parsed = PageOrderParser.Parse(orderText, inputDocument.PageCount);
                if (!parsed.Success || parsed.Pages.Count == 0)
                {
                    return false;
                }

                using var outputDocument = new PdfDocument();
                PdfPageCopy.AddPages(inputDocument, outputDocument, parsed.Pages);
                progress?.Report(ConversionProgress.FromPages(parsed.Pages.Count, parsed.Pages.Count, "pdfReorder"));
                outputDocument.Save(outputPath);
                return true;
            }, cancellationToken).ConfigureAwait(false);

            if (!reordered)
            {
                return ConversionResult.Failed(ConversionErrorCode.InvalidPageRange);
            }

            _logger.LogInformation("Reordered pages in {FileName}.", LogSanitizer.FileNameOnly(inputPath));
            return ConversionResult.Succeeded([outputPath], DateTimeOffset.Now - started);
        }
        catch (OperationCanceledException)
        {
            return ConversionResult.Cancelled();
        }
        catch (PdfReaderException exception)
        {
            _logger.LogError(exception, "PDF reorder failed while reading {FileName}.",
                LogSanitizer.FileNameOnly(request.InputPaths.FirstOrDefault()));
            return ConversionResult.Failed(PdfExceptionMapper.FromReader(exception), exception);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "PDF reorder failed.");
            return ConversionResult.Failed(ConversionErrorCode.FileCorrupt, exception);
        }
    }
}
