using LocalConvert.Core.Conversion;
using LocalConvert.Core.Logging;
using Microsoft.Extensions.Logging;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace LocalConvert.Pdf;

public sealed class PdfRotateConverter : IFileConverter
{
    private const int DefaultRotationDegrees = 90;

    private readonly ILogger<PdfRotateConverter> _logger;

    public PdfRotateConverter(ILogger<PdfRotateConverter> logger)
    {
        _logger = logger;
    }

    public string Id => ConverterIds.PdfRotate;

    public string DisplayNameKey => "Tool_PdfRotate";

    public string DescriptionKey => "Tool_PdfRotate_Desc";

    public string Glyph => "\uE7AD";

    public ConverterCategory Category => ConverterCategory.Pdf;

    public IReadOnlyList<string> InputExtensions => FileExtensions.Pdf;

    public IReadOnlyList<string> OutputExtensions { get; } = [".pdf"];

    public bool SupportsMultipleInputs => true;

    public bool CanConvert(ConversionInput input)
    {
        return input.FilePaths.Count >= 1 && ConverterHelpers.AllExtensionsAre(input, FileExtensions.Pdf);
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
            if (request.InputPaths.Count == 0)
            {
                return ConversionResult.Failed(ConversionErrorCode.InvalidInput);
            }

            foreach (var inputPath in request.InputPaths)
            {
                if (!File.Exists(inputPath))
                {
                    return ConversionResult.Failed(ConversionErrorCode.FileNotFound);
                }
            }

            var degrees = ResolveDegrees(request);
            Directory.CreateDirectory(request.OutputDirectory);

            var outputPaths = await Task.Run(() =>
            {
                var created = new List<string>();
                var total = request.InputPaths.Count;
                for (var index = 0; index < total; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    progress?.Report(ConversionProgress.FromPages(index, total, "pdfRotate"));

                    var inputPath = request.InputPaths[index];
                    using var inputDocument = PdfPageCopy.OpenForImport(inputPath);
                    using var outputDocument = new PdfDocument();
                    for (var pageIndex = 0; pageIndex < inputDocument.PageCount; pageIndex++)
                    {
                        var page = outputDocument.AddPage(inputDocument.Pages[pageIndex]);
                        page.Rotate = (page.Rotate + degrees) % 360;
                    }

                    var fileName = total == 1
                        ? request.OutputFileName
                        : $"{Path.GetFileNameWithoutExtension(inputPath)}-rotated.pdf";
                    var outputPath = Path.Combine(request.OutputDirectory, fileName);
                    outputDocument.Save(outputPath);
                    created.Add(outputPath);
                }

                progress?.Report(ConversionProgress.FromPages(total, total, "pdfRotate"));
                return created;
            }, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Rotated {Count} PDF file(s).", outputPaths.Count);
            return ConversionResult.Succeeded(outputPaths, DateTimeOffset.Now - started);
        }
        catch (OperationCanceledException)
        {
            return ConversionResult.Cancelled();
        }
        catch (PdfReaderException exception)
        {
            _logger.LogError(exception, "PDF rotate failed while reading {FileName}.",
                LogSanitizer.FileNameOnly(request.InputPaths.FirstOrDefault()));
            return ConversionResult.Failed(PdfExceptionMapper.FromReader(exception), exception);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "PDF rotate failed.");
            return ConversionResult.Failed(ConversionErrorCode.FileCorrupt, exception);
        }
    }

    private static int ResolveDegrees(ConversionRequest request)
    {
        if (!request.Options.TryGetValue(ConversionOptionKeys.RotateDegrees, out var text) ||
            !int.TryParse(text, out var degrees))
        {
            return DefaultRotationDegrees;
        }

        return degrees is 90 or 180 or 270 ? degrees : DefaultRotationDegrees;
    }
}
