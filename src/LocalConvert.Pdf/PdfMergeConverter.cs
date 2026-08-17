using LocalConvert.Core.Conversion;
using LocalConvert.Core.Logging;
using Microsoft.Extensions.Logging;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace LocalConvert.Pdf;

public sealed class PdfMergeConverter : IFileConverter
{
    private readonly ILogger<PdfMergeConverter> _logger;

    public PdfMergeConverter(ILogger<PdfMergeConverter> logger)
    {
        _logger = logger;
    }

    public string Id => ConverterIds.PdfMerge;

    public string DisplayNameKey => "Tool_PdfMerge";

    public string DescriptionKey => "Tool_PdfMerge_Desc";

    public string Glyph => "\uE8AE";

    public ConverterCategory Category => ConverterCategory.Pdf;

    public IReadOnlyList<string> InputExtensions => FileExtensions.Pdf;

    public IReadOnlyList<string> OutputExtensions { get; } = [".pdf"];

    public bool SupportsMultipleInputs => true;

    public bool CanConvert(ConversionInput input)
    {
        return input.FilePaths.Count >= 2 && ConverterHelpers.AllExtensionsAre(input, FileExtensions.Pdf);
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

            if (request.InputPaths.Count < 2)
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

            Directory.CreateDirectory(request.OutputDirectory);
            var outputPath = Path.Combine(request.OutputDirectory, request.OutputFileName);

            await Task.Run(() =>
            {
                using var outputDocument = new PdfDocument();
                var totalFiles = request.InputPaths.Count;

                for (var fileIndex = 0; fileIndex < totalFiles; fileIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    progress?.Report(ConversionProgress.FromPages(fileIndex, totalFiles, "pdfMerge"));

                    using var inputDocument = PdfReader.Open(request.InputPaths[fileIndex], PdfDocumentOpenMode.Import);
                    for (var pageIndex = 0; pageIndex < inputDocument.PageCount; pageIndex++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        outputDocument.AddPage(inputDocument.Pages[pageIndex]);
                    }
                }

                progress?.Report(ConversionProgress.FromPages(totalFiles, totalFiles, "pdfMerge"));
                outputDocument.Save(outputPath);
            }, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Merged {Count} PDF file(s) into {FileName}.",
                request.InputPaths.Count,
                LogSanitizer.FileNameOnly(outputPath));

            return ConversionResult.Succeeded([outputPath], DateTimeOffset.Now - started);
        }
        catch (OperationCanceledException)
        {
            return ConversionResult.Cancelled();
        }
        catch (PdfReaderException exception)
        {
            _logger.LogError(exception, "PDF merge failed while reading {FileName}.",
                LogSanitizer.FileNameOnly(request.InputPaths.FirstOrDefault()));
            return ConversionResult.Failed(MapReaderException(exception), exception);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "PDF merge failed.");
            return ConversionResult.Failed(ConversionErrorCode.FileCorrupt, exception);
        }
    }

    private static ConversionErrorCode MapReaderException(PdfReaderException exception)
    {
        var message = exception.Message ?? string.Empty;
        if (message.Contains("password", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("encrypt", StringComparison.OrdinalIgnoreCase))
        {
            return ConversionErrorCode.FileEncrypted;
        }

        return ConversionErrorCode.FileCorrupt;
    }
}
