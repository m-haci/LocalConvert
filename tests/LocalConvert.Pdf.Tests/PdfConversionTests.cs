using System.Drawing;
using System.Drawing.Imaging;
using FluentAssertions;
using LocalConvert.Core.Conversion;
using LocalConvert.Images;
using LocalConvert.Pdf;
using Microsoft.Extensions.Logging.Abstractions;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using Xunit;

namespace LocalConvert.Pdf.Tests;

public sealed class PdfConversionTests
{
    [Fact]
    public async Task ImageToPdf_CreatesOnePagePerImage()
    {
        using var folder = new TestFolder();
        var png = folder.CreatePng("one.png", Color.SteelBlue);
        var jpg = folder.CreateJpeg("two.jpg", Color.Orange);
        var converter = new ImageToPdfConverter(NullLogger<ImageToPdfConverter>.Instance);

        var result = await converter.ConvertAsync(
            folder.Request(ConverterIds.ImageToPdf, "images.pdf", png, jpg),
            progress: null,
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.OutputPaths.Should().ContainSingle();
        using var document = PdfReader.Open(result.OutputPaths[0], PdfDocumentOpenMode.Import);
        document.PageCount.Should().Be(2);
    }

    [Fact]
    public async Task Merge_ConcatenatesPagesInOrder()
    {
        using var folder = new TestFolder();
        var first = folder.CreatePdf("first.pdf", pageCount: 2);
        var second = folder.CreatePdf("second.pdf", pageCount: 3);
        var converter = new PdfMergeConverter(NullLogger<PdfMergeConverter>.Instance);

        var result = await converter.ConvertAsync(
            folder.Request(ConverterIds.PdfMerge, "merged.pdf", first, second),
            progress: null,
            CancellationToken.None);

        result.Success.Should().BeTrue();
        using var document = PdfReader.Open(result.OutputPaths[0], PdfDocumentOpenMode.Import);
        document.PageCount.Should().Be(5);
    }

    [Fact]
    public async Task Split_AllPages_WritesOneFilePerPage()
    {
        using var folder = new TestFolder();
        var source = folder.CreatePdf("source.pdf", pageCount: 3);
        var converter = new PdfSplitConverter(NullLogger<PdfSplitConverter>.Instance);

        var result = await converter.ConvertAsync(
            folder.Request(ConverterIds.PdfSplit, "source.pdf", source),
            progress: null,
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.OutputPaths.Should().HaveCount(3);
    }

    [Fact]
    public async Task Split_Range_WritesSelectedPages()
    {
        using var folder = new TestFolder();
        var source = folder.CreatePdf("source.pdf", pageCount: 6);
        var converter = new PdfSplitConverter(NullLogger<PdfSplitConverter>.Instance);
        var request = folder.Request(ConverterIds.PdfSplit, "source.pdf", source);
        request = new ConversionRequest
        {
            JobId = request.JobId,
            InputPaths = request.InputPaths,
            OutputDirectory = request.OutputDirectory,
            OutputFileName = request.OutputFileName,
            ConverterId = request.ConverterId,
            Options = new Dictionary<string, string>
            {
                [ConversionOptionKeys.SplitMode] = ConversionOptionKeys.SplitModes.Range,
                [ConversionOptionKeys.PageRange] = "2-3,6"
            }
        };

        var result = await converter.ConvertAsync(request, progress: null, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.OutputPaths.Should().HaveCount(3);
    }

    [Fact]
    public async Task Split_InvalidRange_Fails()
    {
        using var folder = new TestFolder();
        var source = folder.CreatePdf("source.pdf", pageCount: 2);
        var converter = new PdfSplitConverter(NullLogger<PdfSplitConverter>.Instance);
        var request = new ConversionRequest
        {
            JobId = Guid.NewGuid(),
            InputPaths = [source],
            OutputDirectory = folder.Path,
            OutputFileName = "source.pdf",
            ConverterId = ConverterIds.PdfSplit,
            Options = new Dictionary<string, string>
            {
                [ConversionOptionKeys.SplitMode] = ConversionOptionKeys.SplitModes.Range,
                [ConversionOptionKeys.PageRange] = "5-2"
            }
        };

        var result = await converter.ConvertAsync(request, progress: null, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be(ConversionErrorCode.InvalidPageRange);
    }

    [Fact]
    public async Task Rotate_AppliesPageRotation()
    {
        using var folder = new TestFolder();
        var source = folder.CreatePdf("source.pdf", pageCount: 1);
        var converter = new PdfRotateConverter(NullLogger<PdfRotateConverter>.Instance);
        var request = folder.Request(ConverterIds.PdfRotate, "rotated.pdf", source);
        request = WithOptions(request, new Dictionary<string, string>
        {
            [ConversionOptionKeys.RotateDegrees] = "90"
        });

        var result = await converter.ConvertAsync(request, progress: null, CancellationToken.None);

        result.Success.Should().BeTrue();
        using var document = PdfReader.Open(result.OutputPaths[0], PdfDocumentOpenMode.Import);
        document.Pages[0].Rotate.Should().Be(90);
    }

    [Fact]
    public async Task Extract_WritesSelectedPagesIntoOneFile()
    {
        using var folder = new TestFolder();
        var source = folder.CreatePdf("source.pdf", pageCount: 5, varyPageWidth: true);
        var converter = new PdfExtractConverter(NullLogger<PdfExtractConverter>.Instance);
        var request = WithOptions(
            folder.Request(ConverterIds.PdfExtract, "extract.pdf", source),
            new Dictionary<string, string>
            {
                [ConversionOptionKeys.PageRange] = "2-3"
            });

        var result = await converter.ConvertAsync(request, progress: null, CancellationToken.None);

        result.Success.Should().BeTrue();
        using var document = PdfReader.Open(result.OutputPaths[0], PdfDocumentOpenMode.Import);
        document.PageCount.Should().Be(2);
        document.Pages[0].Width.Point.Should().BeApproximately(250, 0.1);
        document.Pages[1].Width.Point.Should().BeApproximately(300, 0.1);
    }

    [Fact]
    public async Task Reorder_CopiesPagesInRequestedOrder()
    {
        using var folder = new TestFolder();
        var source = folder.CreatePdf("source.pdf", pageCount: 3, varyPageWidth: true);
        var converter = new PdfReorderConverter(NullLogger<PdfReorderConverter>.Instance);
        var request = WithOptions(
            folder.Request(ConverterIds.PdfReorder, "reordered.pdf", source),
            new Dictionary<string, string>
            {
                [ConversionOptionKeys.PageOrder] = "3,1,2"
            });

        var result = await converter.ConvertAsync(request, progress: null, CancellationToken.None);

        result.Success.Should().BeTrue();
        using var document = PdfReader.Open(result.OutputPaths[0], PdfDocumentOpenMode.Import);
        document.PageCount.Should().Be(3);
        document.Pages[0].Width.Point.Should().BeApproximately(300, 0.1);
        document.Pages[1].Width.Point.Should().BeApproximately(200, 0.1);
        document.Pages[2].Width.Point.Should().BeApproximately(250, 0.1);
    }

    [Fact]
    public async Task Metadata_ClearsDocumentInfo()
    {
        using var folder = new TestFolder();
        var source = folder.CreatePdf("source.pdf", pageCount: 1, title: "Secret", author: "Author");
        var converter = new PdfMetadataConverter(NullLogger<PdfMetadataConverter>.Instance);

        var result = await converter.ConvertAsync(
            folder.Request(ConverterIds.PdfMetadata, "clean.pdf", source),
            progress: null,
            CancellationToken.None);

        result.Success.Should().BeTrue();
        var reader = new PdfPigMetadataReader();
        reader.TryRead(result.OutputPaths[0], out var metadata).Should().BeTrue();
        metadata.Title.Should().BeNullOrEmpty();
        metadata.Author.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task PdfToJpeg_WritesOneImagePerPage()
    {
        using var folder = new TestFolder();
        var source = folder.CreatePdf("source.pdf", pageCount: 2);
        var converter = new PdfToJpegConverter(NullLogger<PdfToJpegConverter>.Instance);

        var result = await converter.ConvertAsync(
            folder.Request(ConverterIds.PdfToJpeg, "source.jpg", source),
            progress: null,
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.OutputPaths.Should().HaveCount(2);
        File.Exists(result.OutputPaths[0]).Should().BeTrue();
    }

    private static ConversionRequest WithOptions(ConversionRequest request, Dictionary<string, string> options)
    {
        return new ConversionRequest
        {
            JobId = request.JobId,
            InputPaths = request.InputPaths,
            OutputDirectory = request.OutputDirectory,
            OutputFileName = request.OutputFileName,
            ConverterId = request.ConverterId,
            Options = options
        };
    }

    private sealed class TestFolder : IDisposable
    {
        public TestFolder()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "LocalConvertPdfTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public ConversionRequest Request(string converterId, string outputFileName, params string[] inputs)
        {
            return new ConversionRequest
            {
                JobId = Guid.NewGuid(),
                InputPaths = inputs,
                OutputDirectory = Path,
                OutputFileName = outputFileName,
                ConverterId = converterId
            };
        }

        public string CreatePdf(string fileName, int pageCount, bool varyPageWidth = false, string? title = null, string? author = null)
        {
            var filePath = System.IO.Path.Combine(Path, fileName);
            using var document = new PdfDocument();
            if (!string.IsNullOrWhiteSpace(title))
            {
                document.Info.Title = title;
            }

            if (!string.IsNullOrWhiteSpace(author))
            {
                document.Info.Author = author;
            }

            for (var i = 0; i < pageCount; i++)
            {
                var page = document.AddPage();
                if (varyPageWidth)
                {
                    page.Width = PdfSharp.Drawing.XUnit.FromPoint(200 + (i * 50));
                }
            }

            document.Save(filePath);
            return filePath;
        }

        public string CreatePng(string fileName, Color color)
        {
            var filePath = System.IO.Path.Combine(Path, fileName);
            using var bitmap = new Bitmap(32, 24);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.Clear(color);
            bitmap.Save(filePath, ImageFormat.Png);
            return filePath;
        }

        public string CreateJpeg(string fileName, Color color)
        {
            var filePath = System.IO.Path.Combine(Path, fileName);
            using var bitmap = new Bitmap(40, 40);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.Clear(color);
            bitmap.Save(filePath, ImageFormat.Jpeg);
            return filePath;
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
