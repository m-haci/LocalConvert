using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Docnet.Core;
using Docnet.Core.Models;
using LocalConvert.Core.Conversion;
using LocalConvert.Core.Logging;
using Microsoft.Extensions.Logging;

namespace LocalConvert.Pdf;

public sealed class PdfToJpegConverter : PdfToImageConverter
{
    public PdfToJpegConverter(ILogger<PdfToJpegConverter> logger)
        : base(logger, ConverterIds.PdfToJpeg, "Tool_PdfToJpeg", ".jpg", ImageFormat.Jpeg)
    {
    }
}

public sealed class PdfToPngConverter : PdfToImageConverter
{
    public PdfToPngConverter(ILogger<PdfToPngConverter> logger)
        : base(logger, ConverterIds.PdfToPng, "Tool_PdfToPng", ".png", ImageFormat.Png)
    {
    }
}

public abstract class PdfToImageConverter : IFileConverter
{
    private const int RenderWidth = 1240;
    private const int RenderHeight = 1754;
    private const long JpegQuality = 90L;
    private static readonly object PdfiumLock = new();

    private readonly ILogger _logger;
    private readonly ImageFormat _imageFormat;
    private readonly string _extension;

    protected PdfToImageConverter(
        ILogger logger,
        string id,
        string displayNameKey,
        string extension,
        ImageFormat imageFormat)
    {
        _logger = logger;
        Id = id;
        DisplayNameKey = displayNameKey;
        DescriptionKey = displayNameKey + "_Desc";
        _extension = extension;
        _imageFormat = imageFormat;
        OutputExtensions = [extension];
        Glyph = id == ConverterIds.PdfToPng ? "\uE91B" : "\uEB9F";
    }

    public string Id { get; }

    public string DisplayNameKey { get; }

    public string DescriptionKey { get; }

    public string Glyph { get; }

    public ConverterCategory Category => ConverterCategory.Convert | ConverterCategory.Images;

    public IReadOnlyList<string> InputExtensions => FileExtensions.Pdf;

    public IReadOnlyList<string> OutputExtensions { get; }

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

            Directory.CreateDirectory(request.OutputDirectory);

            var outputPaths = await Task.Run(() => RenderAll(request, progress, cancellationToken), cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "Rendered {Count} image(s) from PDF {FileName}.",
                outputPaths.Count,
                LogSanitizer.FileNameOnly(request.InputPaths[0]));

            return ConversionResult.Succeeded(outputPaths, DateTimeOffset.Now - started);
        }
        catch (OperationCanceledException)
        {
            return ConversionResult.Cancelled();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "PDF to image conversion failed for {FileName}.",
                LogSanitizer.FileNameOnly(request.InputPaths.FirstOrDefault()));
            return ConversionResult.Failed(ConversionErrorCode.FileCorrupt, exception);
        }
    }

    private List<string> RenderAll(
        ConversionRequest request,
        IProgress<ConversionProgress>? progress,
        CancellationToken cancellationToken)
    {
        var created = new List<string>();
        lock (PdfiumLock)
        {
            foreach (var inputPath in request.InputPaths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var bytes = File.ReadAllBytes(inputPath);
                using var documentReader = DocLib.Instance.GetDocReader(
                    bytes,
                    new PageDimensions(RenderWidth, RenderHeight));

                var pageCount = documentReader.GetPageCount();
                var baseName = request.InputPaths.Count == 1
                    ? Path.GetFileNameWithoutExtension(request.OutputFileName)
                    : Path.GetFileNameWithoutExtension(inputPath);

                for (var pageIndex = 0; pageIndex < pageCount; pageIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    progress?.Report(ConversionProgress.FromPages(pageIndex, pageCount, "pdfToImage"));

                    using var pageReader = documentReader.GetPageReader(pageIndex);
                    var rawBytes = pageReader.GetImage();
                    var width = pageReader.GetPageWidth();
                    var height = pageReader.GetPageHeight();
                    if (width <= 0 || height <= 0 || rawBytes.Length == 0)
                    {
                        throw new InvalidOperationException("The PDF page could not be rasterized.");
                    }

                    var fileName = pageCount == 1 && request.InputPaths.Count == 1
                        ? request.OutputFileName
                        : $"{baseName}-p{pageIndex + 1}{_extension}";
                    var outputPath = Path.Combine(request.OutputDirectory, fileName);
                    SaveImage(rawBytes, width, height, outputPath);
                    created.Add(outputPath);
                }

                progress?.Report(ConversionProgress.FromPages(pageCount, pageCount, "pdfToImage"));
            }
        }

        return created;
    }

    private void SaveImage(byte[] bgraBytes, int width, int height, string outputPath)
    {
        using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        var data = bitmap.LockBits(
            new Rectangle(0, 0, width, height),
            ImageLockMode.WriteOnly,
            PixelFormat.Format32bppArgb);
        try
        {
            var expectedStride = width * 4;
            if (data.Stride == expectedStride)
            {
                Marshal.Copy(bgraBytes, 0, data.Scan0, Math.Min(bgraBytes.Length, data.Stride * height));
            }
            else
            {
                for (var row = 0; row < height; row++)
                {
                    Marshal.Copy(
                        bgraBytes,
                        row * expectedStride,
                        data.Scan0 + (row * data.Stride),
                        expectedStride);
                }
            }
        }
        finally
        {
            bitmap.UnlockBits(data);
        }

        if (_imageFormat.Equals(ImageFormat.Jpeg))
        {
            var codec = ImageCodecInfo.GetImageEncoders()
                .First(item => item.FormatID == ImageFormat.Jpeg.Guid);
            using var encoderParameters = new EncoderParameters(1);
            encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, JpegQuality);
            bitmap.Save(outputPath, codec, encoderParameters);
            return;
        }

        bitmap.Save(outputPath, _imageFormat);
    }
}
