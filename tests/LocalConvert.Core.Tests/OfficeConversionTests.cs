using FluentAssertions;
using LocalConvert.Core.Conversion;
using LocalConvert.Core.Office;
using LocalConvert.Office;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LocalConvert.Core.Tests;

public sealed class OfficeConversionTests
{
    [Fact]
    public async Task WordToPdf_ReturnsFriendlyError_WhenOfficeIsMissing()
    {
        var converter = new WordToPdfConverter(
            new MissingOfficeDetector(),
            NullLogger<WordToPdfConverter>.Instance);
        var folder = Path.Combine(Path.GetTempPath(), "LocalConvertOfficeTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var inputPath = Path.Combine(folder, "note.docx");
        await File.WriteAllBytesAsync(inputPath, [0x50, 0x4B]);

        try
        {
            var result = await converter.ConvertAsync(
                new ConversionRequest
                {
                    JobId = Guid.NewGuid(),
                    InputPaths = [inputPath],
                    OutputDirectory = folder,
                    OutputFileName = "note.pdf",
                    ConverterId = ConverterIds.WordToPdf
                },
                progress: null,
                CancellationToken.None);

            result.Success.Should().BeFalse();
            result.Error!.Code.Should().Be(ConversionErrorCode.OfficeEngineMissing);
        }
        finally
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
    }

    private sealed class MissingOfficeDetector : IOfficeAvailability
    {
        public OfficeAvailabilityResult Detect()
        {
            return new OfficeAvailabilityResult();
        }
    }
}
