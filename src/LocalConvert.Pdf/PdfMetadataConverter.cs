using LocalConvert.Core.Conversion;
using LocalConvert.Core.Logging;
using Microsoft.Extensions.Logging;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace LocalConvert.Pdf;

public sealed class PdfMetadataConverter : IFileConverter
{
    private readonly ILogger<PdfMetadataConverter> _logger;

    public PdfMetadataConverter(ILogger<PdfMetadataConverter> logger)
    {
        _logger = logger;
    }

    public string Id => ConverterIds.PdfMetadata;

    public string DisplayNameKey => "Tool_PdfMetadata";

    public string DescriptionKey => "Tool_PdfMetadata_Desc";

    public string Glyph => "\uE946";

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

            await Task.Run(() =>
            {
                using var inputDocument = PdfPageCopy.OpenForImport(inputPath);
                using var outputDocument = new PdfDocument();
                for (var pageIndex = 0; pageIndex < inputDocument.PageCount; pageIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    outputDocument.AddPage(inputDocument.Pages[pageIndex]);
                }

                outputDocument.Info.Title = string.Empty;
                outputDocument.Info.Author = string.Empty;
                outputDocument.Info.Subject = string.Empty;
                outputDocument.Info.Keywords = string.Empty;
                outputDocument.Info.Creator = string.Empty;
                progress?.Report(ConversionProgress.FromPages(1, 1, "pdfMetadata"));
                outputDocument.Save(outputPath);
            }, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Cleared metadata for {FileName}.", LogSanitizer.FileNameOnly(inputPath));
            return ConversionResult.Succeeded([outputPath], DateTimeOffset.Now - started);
        }
        catch (OperationCanceledException)
        {
            return ConversionResult.Cancelled();
        }
        catch (PdfReaderException exception)
        {
            _logger.LogError(exception, "PDF metadata clear failed while reading {FileName}.",
                LogSanitizer.FileNameOnly(request.InputPaths.FirstOrDefault()));
            return ConversionResult.Failed(PdfExceptionMapper.FromReader(exception), exception);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "PDF metadata clear failed.");
            return ConversionResult.Failed(ConversionErrorCode.FileCorrupt, exception);
        }
    }
}
