using LocalConvert.Core.Conversion;
using LocalConvert.Core.Logging;
using LocalConvert.Core.Parsing;
using Microsoft.Extensions.Logging;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace LocalConvert.Pdf;

public sealed class PdfExtractConverter : IFileConverter
{
    private readonly ILogger<PdfExtractConverter> _logger;

    public PdfExtractConverter(ILogger<PdfExtractConverter> logger)
    {
        _logger = logger;
    }

    public string Id => ConverterIds.PdfExtract;

    public string DisplayNameKey => "Tool_PdfExtract";

    public string DescriptionKey => "Tool_PdfExtract_Desc";

    public string Glyph => "\uE8C8";

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

            var extracted = await Task.Run(() =>
            {
                using var inputDocument = PdfPageCopy.OpenForImport(inputPath);
                request.Options.TryGetValue(ConversionOptionKeys.PageRange, out var range);
                var parsed = PageRangeParser.Parse(range, inputDocument.PageCount);
                if (!parsed.Success || parsed.Pages.Count == 0)
                {
                    return false;
                }

                using var outputDocument = new PdfDocument();
                PdfPageCopy.AddPages(inputDocument, outputDocument, parsed.Pages);
                progress?.Report(ConversionProgress.FromPages(parsed.Pages.Count, parsed.Pages.Count, "pdfExtract"));
                outputDocument.Save(outputPath);
                return true;
            }, cancellationToken).ConfigureAwait(false);

            if (!extracted)
            {
                return ConversionResult.Failed(ConversionErrorCode.InvalidPageRange);
            }

            _logger.LogInformation("Extracted pages from {FileName}.", LogSanitizer.FileNameOnly(inputPath));
            return ConversionResult.Succeeded([outputPath], DateTimeOffset.Now - started);
        }
        catch (OperationCanceledException)
        {
            return ConversionResult.Cancelled();
        }
        catch (PdfReaderException exception)
        {
            _logger.LogError(exception, "PDF extract failed while reading {FileName}.",
                LogSanitizer.FileNameOnly(request.InputPaths.FirstOrDefault()));
            return ConversionResult.Failed(PdfExceptionMapper.FromReader(exception), exception);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "PDF extract failed.");
            return ConversionResult.Failed(ConversionErrorCode.FileCorrupt, exception);
        }
    }
}
