using LocalConvert.Core.Conversion;
using LocalConvert.Core.Logging;
using Microsoft.Extensions.Logging;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace LocalConvert.Images;

public sealed class ImageToPdfConverter : IFileConverter
{
    private const double A4WidthPoints = 595.275590551;
    private const double A4HeightPoints = 841.88976378;
    private const double PageMarginPoints = 24;

    private readonly ILogger<ImageToPdfConverter> _logger;

    public ImageToPdfConverter(ILogger<ImageToPdfConverter> logger)
    {
        _logger = logger;
    }

    public string Id => ConverterIds.ImageToPdf;

    public string DisplayNameKey => "Tool_ImageToPdf";

    public string DescriptionKey => "Tool_ImageToPdf_Desc";

    public string Glyph => "\uE91B";

    public ConverterCategory Category => ConverterCategory.Convert | ConverterCategory.Images;

    public IReadOnlyList<string> InputExtensions => FileExtensions.Images;

    public IReadOnlyList<string> OutputExtensions { get; } = [".pdf"];

    public bool SupportsMultipleInputs => true;

    public bool CanConvert(ConversionInput input)
    {
        return ConverterHelpers.AllExtensionsAre(input, FileExtensions.Images);
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

            Directory.CreateDirectory(request.OutputDirectory);
            var outputPath = Path.Combine(request.OutputDirectory, request.OutputFileName);

            await Task.Run(() =>
            {
                using var document = new PdfDocument();
                var total = request.InputPaths.Count;

                for (var index = 0; index < total; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var inputPath = request.InputPaths[index];
                    progress?.Report(ConversionProgress.FromPages(index, total, "imageToPdf"));

                    using var image = XImage.FromFile(inputPath);
                    var page = document.AddPage();
                    FitImageToPage(page, image);

                    using var graphics = XGraphics.FromPdfPage(page);
                    var contentWidth = page.Width.Point - (PageMarginPoints * 2);
                    var contentHeight = page.Height.Point - (PageMarginPoints * 2);
                    graphics.DrawImage(image, PageMarginPoints, PageMarginPoints, contentWidth, contentHeight);
                }

                progress?.Report(ConversionProgress.FromPages(total, total, "imageToPdf"));
                document.Save(outputPath);
            }, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Converted {Count} image(s) to PDF {FileName}.",
                request.InputPaths.Count,
                LogSanitizer.FileNameOnly(outputPath));

            return ConversionResult.Succeeded([outputPath], DateTimeOffset.Now - started);
        }
        catch (OperationCanceledException)
        {
            return ConversionResult.Cancelled();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Image to PDF conversion failed for {FileName}.",
                LogSanitizer.FileNameOnly(request.InputPaths.FirstOrDefault()));
            return ConversionResult.Failed(ConversionErrorCode.FileCorrupt, exception);
        }
    }

    private static void FitImageToPage(PdfPage page, XImage image)
    {
        var imageWidth = image.PixelWidth * 72d / Math.Max(image.HorizontalResolution, 72);
        var imageHeight = image.PixelHeight * 72d / Math.Max(image.VerticalResolution, 72);
        var landscape = imageWidth >= imageHeight;
        var maxWidth = landscape ? A4HeightPoints : A4WidthPoints;
        var maxHeight = landscape ? A4WidthPoints : A4HeightPoints;
        var availableWidth = maxWidth - (PageMarginPoints * 2);
        var availableHeight = maxHeight - (PageMarginPoints * 2);
        var scale = Math.Min(1d, Math.Min(availableWidth / imageWidth, availableHeight / imageHeight));
        var drawnWidth = imageWidth * scale;
        var drawnHeight = imageHeight * scale;

        page.Width = XUnit.FromPoint(drawnWidth + (PageMarginPoints * 2));
        page.Height = XUnit.FromPoint(drawnHeight + (PageMarginPoints * 2));
    }
}
